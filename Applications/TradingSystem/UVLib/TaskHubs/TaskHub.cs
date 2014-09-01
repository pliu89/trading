using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.TaskHubs
{
    using UV.Lib.Hubs;
    using UV.Lib.IO.Xml;

    //
    /// <summary>
    /// A task hub is a task processing job consumer with its on thread.
    /// TaskEventArgs define the tasks to be performed.  These are expected
    /// to contain a string EventHandlerName of the EventHandler method
    /// that the job is to be passed to. 
    /// A class should inherit this as a base class and implemented whatever
    /// eventhandlers it wants to process.  A List of objects "Data" in the event
    /// args can be used to pass data from one task to the next.
    /// </summary>
    public class TaskHub : Hub, IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        private List<TaskEventArg> m_WaitingTasks = new List<TaskEventArg>();       // tasks that fail, added to this list, will be tried again.
        private List<EventArgs> m_TaskToResubmit = new List<EventArgs>();           // workspace for timer thread.
        private System.Timers.Timer m_Timer = null;

        private const double MillisecondsPerMinute = 60000;
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public TaskHub(string hubName, string logPath, bool showLogs)
            : base(hubName, logPath, showLogs, LogLevel.ShowAllMessages)
        {
            Initialize();
        }
        //
        // Constructor useful for IStringify creation.
        public TaskHub()
            : base("TaskHub", UV.Lib.Application.AppInfo.GetInstance().LogPath, false, LogLevel.ShowAllMessages)
        {
            Initialize();
        }
        //
        // ****             Initialize()                ****
        //
        private void Initialize()
        {
            // Timer set up.
            double minutesInterval = 1;
            m_Timer = new System.Timers.Timer(1000.0 * 60.0 * minutesInterval);
            m_Timer.AutoReset = false;
            m_Timer.Elapsed += new System.Timers.ElapsedEventHandler(Timer_Elapsed); 
        }
        //
        //
        //       
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        public double TimerIntervalMinutes
        {
            get { return m_Timer.Interval / MillisecondsPerMinute; }
            set
            {
                bool isEnabled = m_Timer.Enabled;
                m_Timer.Stop();
                m_Timer.Interval = value * MillisecondsPerMinute;
                if (isEnabled)
                    m_Timer.Start();
            }
        }//TimerIntervalMinutes
        //
        #endregion//Properties


        
        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        public bool AddNewTask(TaskEventArg newTask)
        {
            return this.HubEventEnqueue(newTask);
        }
        //
        //
        public override void Start()
        {
            m_Timer.Start();
            base.Start();
        }
        public override void RequestStop()
        {
            TaskEventArg request = new TaskEventArg();
            request.RequestStop = true;
            this.HubEventEnqueue(request);
        }
        //
        //
        #endregion//Public Methods


        #region HubEventHandler & Processing Methods
        // *****************************************************
        // ****         HubEventHandler                     ****
        // *****************************************************
        /// <summary>
        /// The main event handler, called by the hub thread.
        /// </summary>
        protected override void HubEventHandler(EventArgs[] eventArgList)
        {
            foreach (EventArgs eventArg in eventArgList)
            {
                Type eventType = eventArg.GetType();
                if (eventType == typeof(TaskEventArg))
                    ProcessTask((TaskEventArg)eventArg);
                else
                    Log.NewEntry(LogLevel.Warning, "HubEventHandler: Event type {0} not implemented.", eventArg);
            }
        }// HubEventHandler()
        //
        //
        // *****************************************************
        // ****               ProcessTask()                 ****
        // *****************************************************
        /// <summary>
        /// Processes tasks from user.
        /// </summary>
        private void ProcessTask(TaskEventArg eventArg)
        {
            bool triggerTaskCompletedEvent = false;
                        
            // Process multiple tasks
            if (eventArg.IsParent() )                                            // This task has children to be completed in sequence.
            {
                TaskEventArg currentWorkingRequest = null;                      // the child-request that we are currently working on.
                if (eventArg.TryGetNextUnsuccessfulChild(out currentWorkingRequest))
                {
                    eventArg.Status = TaskStatus.ContinueWorking;               // set parent as "working"
                    if (currentWorkingRequest.Status == TaskStatus.Success)     // Usually, the "current" child is not finished successfully, 
                    {                                                           // but if its the last child, then it signals the end of the task.
                        eventArg.Status = TaskStatus.Success;                   // Mark parent for success...
                        triggerTaskCompletedEvent = true;
                    }
                    else if (currentWorkingRequest.Status == TaskStatus.Failed)  // the last non-successful child says its failed...
                    {
                        eventArg.Status = TaskStatus.Failed;                     // done. Failed request
                        triggerTaskCompletedEvent = true;
                    }
                    else if (DateTime.Now.CompareTo(eventArg.StartTime) >= 0)
                    {   // We are passed the start time, submit request.
                        Log.BeginEntry(LogLevel.Minor, "ProcessTask: {0} ", eventArg);
                        Log.AppendEntry(" ----> {0}", currentWorkingRequest);    // output the current child to work.
                        Log.EndEntry();

                        ProcessTask(currentWorkingRequest);                      // continue trying to work this task.
                    }
                    else
                    {   // We are working on a child task, but not past start time yet.
                        Log.NewEntry(LogLevel.Minor, "ProcessTask: Waiting to start {0}. ", eventArg);
                        if (!m_WaitingTasks.Contains(eventArg))                  // make sure that we will revist this parent event again...
                            m_WaitingTasks.Add(eventArg);                                     
                    }
                }
            }
            else if (eventArg.RequestStop)
            {
                if (DateTime.Now.CompareTo(eventArg.StartTime) >= 0)
                {

                    Log.NewEntry(LogLevel.Major, "ProcessTask: {0} ----> Stop requested.", eventArg);
                    if (m_Timer != null)
                    {
                        m_Timer.Stop();
                        m_Timer.Dispose();
                        m_Timer = null;
                    }
                    eventArg.Status = TaskStatus.Success;
                    base.Stop();
                }
                else
                {
                    Log.NewEntry(LogLevel.Minor, "ProcessTask: Waiting to start {0}. ", eventArg);
                    if (!m_WaitingTasks.Contains(eventArg))                  // make sure that we will revist this parent event again...
                        m_WaitingTasks.Add(eventArg); 
                }

            }
            else
            {   //
                // Process requests for task.
                //
                if ( ! string.IsNullOrEmpty(eventArg.EventHandlerName) )
                {
                    Type thisType = this.GetType();
                    System.Reflection.MethodInfo[] methodInfos = thisType.GetMethods();     // Task EventHandlerName must match super-class method name!
                    System.Reflection.MethodInfo m = null;                                  // Method to do the task.
                    for (int ptr = 0; ptr < methodInfos.Length ; ptr++)
                    {
                        if (methodInfos[ptr].Name.Equals(eventArg.EventHandlerName))
                        {
                            m = methodInfos[ptr];
                            break;                                                          // stop searching
                        }
                    }
                    if (m != null)
                    {   // We found the named method to call.
                        // We ASSUME that the implementation method will set the "Status" of the event properly!
                        // This is critical!
                        m.Invoke(this, new object[] { this, eventArg });
                        if (eventArg.Status == TaskStatus.New)
                        {
                            Log.NewEntry(LogLevel.Major, "ProcessTask: Method {0} has failed to change the TaskEventArg.Status field!", eventArg.EventHandlerName);
                            Log.Flush();
                            eventArg.Status = TaskStatus.Failed;
                            throw(new Exception(string.Format("Method {0}(object,EventArg) must set TaskEventArg.Status field!",eventArg.EventHandlerName)));
                        }
                    }
                    else
                        eventArg.Status = TaskStatus.Failed;
                }
                else
                {   
                    Log.NewEntry(LogLevel.Error,"ProcessTask: No eventHanderName.");
                    eventArg.Status = TaskStatus.Failed;
                }
                // 
                // Check final status of task
                //
                TaskEventArg parent;
                if (eventArg.TryGetParent(out parent) )
                {   // This Task has a parent Task.
                    if (eventArg.Status == TaskStatus.ContinueWorking)    
                    {   // The (child) current task wants to try again.
                        Log.NewEntry(LogLevel.Minor, "ProcessTask: {0} has parent, will wait to try again.", eventArg);
                        if (!m_WaitingTasks.Contains(parent))                  // make sure that we will revist this parent event again...
                            m_WaitingTasks.Add(parent);                            
                    }
                    else
                    {
                        Log.NewEntry(LogLevel.Minor, "ProcessTask: {0} has parent, will flash parent.", eventArg);
                        this.HubEventEnqueue(parent);                       // flash parent to continue working itself.
                    }
                }
                else if (eventArg.Status == TaskStatus.ContinueWorking && DateTime.Now.CompareTo(eventArg.StopTime) < 0)
                {   // This task is not complete, and is allowed to work more.
                    if (!m_WaitingTasks.Contains(eventArg))                  // make sure that we will revist this parent event again...
                    {
                        Log.NewEntry(LogLevel.Minor, "ProcessTask: Adding {0} to waiting queue.", eventArg);
                        m_WaitingTasks.Add(eventArg);
                    }
                    else
                        Log.NewEntry(LogLevel.Minor, "ProcessTask: {0} already in waiting queue.", eventArg);
                }
                else
                {   // This task is complete, either failed, succeeded or event timed out.. whatever.
                    triggerTaskCompletedEvent = true;
                }
            }
            
            // Exit
            if (triggerTaskCompletedEvent)
                OnTaskCompleted(eventArg);
        }//ProcessRequest()
        //
        //
        //
        #endregion//HubEventHandler & Processing Methods


        #region External Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        /// <summary>
        /// This event handler is called by a pool thread; keep it threadsafe!
        /// </summary>
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs eventArgs)
        {
            Log.NewEntry(LogLevel.Minor, "Timer_Elapsed: Tick. ");
            if (m_Timer == null)
            {                
                return;
            }
            m_Timer.Enabled = false;

            // Check for working requests to resubmit.
            m_TaskToResubmit.Clear();
            lock (m_WaitingTasks)
            {
                if (m_WaitingTasks.Count > 0)
                {
                    Log.NewEntry(LogLevel.Minor, "Timer_Elapsed: Working request queue contains {0} waiting requests.  Will resubmit them.",m_WaitingTasks.Count);
                    //m_TaskToResubmit = new List<EventArgs>();
                }
                DateTime now = DateTime.Now;
                int ptr = 0;                                            // waiting tasks ptr
                while (ptr < m_WaitingTasks.Count)
                {
                    if (m_WaitingTasks[ptr].StartTime.CompareTo(now) < 0)
                    {   // This is a task we want to re-try now.
                        m_TaskToResubmit.Add(m_WaitingTasks[ptr]);
                        m_WaitingTasks.RemoveAt(ptr);                   // no need to increment ptr since we removed this item from list.
                    }
                    else
                        ptr++;
                }
            }
            if (m_TaskToResubmit.Count > 0)
                this.HubEventEnqueue(m_TaskToResubmit);

            // Restart timer.
            // TODO: Change the timer period, if its changed
            m_Timer.Enabled = true;
        }// Timer_Elapsed()
        //
        //
        
        //
        #endregion//External event handlers


        #region Events
        // *****************************************************************
        // ****                         Events                          ****
        // *****************************************************************
        //
        public event EventHandler TaskCompleted;
        //
        //
        private void OnTaskCompleted(TaskEventArg completedTask)
        {
            Log.NewEntry(LogLevel.Minor, "OnTaskCompleted: {0}", completedTask);

            if (m_WaitingTasks.Contains(completedTask))
            {
                Log.NewEntry(LogLevel.Minor, "OnTaskCompleted: Removing from wait list.", completedTask);
                m_WaitingTasks.Remove(completedTask);
            }

            if (TaskCompleted != null)
                TaskCompleted(this, completedTask);
        }
        //
        #endregion//Event Handlers


        #region IStringifiable
        // *****************************************************************
        // ****                     IStringifiable                      ****
        // *****************************************************************
        public string GetAttributes()
        {
            return string.Format("TimerIntervalMinutes={0}", this.TimerIntervalMinutes);
        }
        public List<IStringifiable> GetElements()
        {
            List<IStringifiable> elements = null;
            lock (m_WaitingTasks)               // I dunno: The idea here is to store the still waiting tasks..
            {
                if (m_WaitingTasks.Count > 0)
                {
                    elements = new List<IStringifiable>();
                    foreach (TaskEventArg task in m_WaitingTasks)
                        elements.Add((IStringifiable)task);
                }
            }
            return elements;
        }
        public void SetAttributes(Dictionary<string, string> attributes)
        {
            double x;
            foreach (string key in attributes.Keys)
            {
                if (key.Equals("TimerIntervalMinutes") && double.TryParse(attributes[key], out x))
                    this.TimerIntervalMinutes = x;
            }
        }
        public void AddSubElement(IStringifiable subElement)
        {
            if (subElement is TaskEventArg)
                this.AddNewTask((TaskEventArg)subElement);
        } 
        //
        //
        //
        #endregion//IStringifiable



    }
}
