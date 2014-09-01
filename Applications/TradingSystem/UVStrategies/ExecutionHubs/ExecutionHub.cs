using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;

namespace UV.Strategies.ExecutionHubs
{
    using UV.Lib.Hubs;
    using UV.Lib.Application;
    using UV.Lib.Utilities;
    using UV.Lib.IO.Xml;
    using UV.Lib.Engines;

    using UV.Lib.Fills;
    using UV.Lib.OrderBooks;
    using UV.Strategies.StrategyEngines;
    using UV.Strategies.ExecutionHubs.ExecutionContainers;
    using UV.Strategies.ExecutionEngines.OrderEngines;        //temp

    /// <summary>
    /// </summary>
    public abstract class ExecutionHub : Hub, IService, IStringifiable, IEngineHub
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //   
        // IService controls
        private AppServices m_AppServices = null;
        private ServiceStates m_ServiceState = ServiceStates.Unstarted;         // Current state

        //private ServiceStates m_ServiceStatePending = ServiceStates.None;     

        // EngineContainers                                                     // engineHubName, EngineContainerID --> ExecutionContainer
        private string DefaultHubName = string.Empty;
        private Dictionary<string, Dictionary<int, ThreadContainer>> m_ExecutionContainers = new Dictionary<string, Dictionary<int, ThreadContainer>>();


        // Internal work spaces
        private RequestFactory<RequestCode> m_Requests = new RequestFactory<RequestCode>();
        //private EventWaitQueueLite m_PendingRequests = null;

        private List<ITimerSubscriber> m_ITimerSubscribers = new List<ITimerSubscriber>(); // 1 Hz update callbacks available 
        private object m_Lock = new object();
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public ExecutionHub()
            : base("ExecutionHub", AppServices.GetInstance().Info.LogPath, false, LogLevel.ShowAllMessages)
        {
            m_AppServices = AppServices.GetInstance();
            m_WaitListenUpdatePeriod = 1000;        // desired miliseconds between periodic updates()
        }
        //
        //       
        #endregion//Constructors

        #region no Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        #endregion//Properties

        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //
        // *********************************************
        // ****             Start()                 ****
        // *********************************************
        public override void Start()
        {
            if (m_ServiceState == ServiceStates.Unstarted)
            {   // We start thread here, and push onto queue a request to start.
                this.HubEventEnqueue(m_Requests.Get(RequestCode.ServiceStateChange, ServiceStates.Started));
                base.Start();
            }
        }// Start()       
        //
        //
        //
        // *****************************************************
        // ****             SubscribeToTimer()              ****
        // *****************************************************
        /// <summary>
        /// Threadsafe call to add a subscriber to a timer.
        /// </summary>
        /// <param name="subscriber"></param>
        /// <returns></returns>
        public void SubscribeToTimer(ITimerSubscriber subscriber)
        {
            Log.NewEntry(LogLevel.Warning, "SubscribeToTimer: Strategy {0} subscribing.", subscriber.GetType().Name);
            lock (m_Lock)
            {
                m_ITimerSubscribers.Add(subscriber);
            }
        }
        //
        //
        #endregion//Public Methods

        #region Process Hub Events
        // *****************************************************************
        // ****                 Hub Event Handler                       ****
        // *****************************************************************
        //
        //
        // *****************************************************
        // ****             HubEventHandler()               ****
        // *****************************************************
        protected override void HubEventHandler(EventArgs[] eventArgList)
        {
            foreach (EventArgs baseEventArgs in eventArgList)
            {
                Type eArgType = baseEventArgs.GetType();
                if (eArgType == typeof(RequestEventArg<RequestCode>))
                {
                    //
                    // Requests
                    //
                    RequestEventArg<RequestCode> requestArg = (RequestEventArg<RequestCode>)baseEventArgs;
                    switch (requestArg.RequestType)
                    {
                        case RequestCode.ServiceStateChange:
                            ProcessServiceStateRequests(requestArg);
                            break;
                        default:
                            Log.NewEntry(LogLevel.Warning, "Ignoring unknown RequestCode: {0}", requestArg.RequestType);
                            break;
                    }
                }

                else if (eArgType == typeof(EngineEventArgs))
                {
                    EngineEventArgs eventArg = (EngineEventArgs)baseEventArgs;
                    if (eventArg.Status == EngineEventArgs.EventStatus.Request)
                    {
                        // Process Requests
                        switch (eventArg.MsgType)
                        {
                            case EngineEventArgs.EventType.NewEngine:
                                ProcessCreateStrategyRequest(eventArg);
                                break;
                            case EngineEventArgs.EventType.AddContainer:
                                ProcessAddContainerRequest(eventArg);
                                break;
                            case EngineEventArgs.EventType.SyntheticOrder:
                                ProcessSyntheticOrderRequest(eventArg);
                                break;
                            default:
                                ProcessEngineEvent(eventArg);
                                break;
                        }
                    }
                }
            }//next eventArg            
        }// HubEventHandler()
        //
        //
        //
        //
        // *****************************************************************
        // ****             ProcessServiceStateRequests()               ****
        // *****************************************************************
        private void ProcessServiceStateRequests(RequestEventArg<RequestCode> eventArg)
        {
            ServiceStates prevState = m_ServiceState;                       // store the current state.
            ServiceStates requestedState = (ServiceStates)eventArg.Data[0];// get state requested
            Log.NewEntry(LogLevel.Minor, "ProcessServiceStateRequests: Current={0} Requested={1}", m_ServiceState, requestedState);

            switch (m_ServiceState)                         // switch on CURRENT state.         
            {
                case ServiceStates.Unstarted:               // We are starting for first time.
                    // Complete what ever initialization is needed to move to Started state.
                    //      * Create (or find) communication services needed.
                    bool isReadyToStart = true;
                    if (isReadyToStart)
                    {
                        m_ServiceState = ServiceStates.Started;
                        HubEventEnqueue(m_Requests.Get(RequestCode.ServiceStateChange, ServiceStates.Running)); // automatically try to advance
                    }
                    break;
                case ServiceStates.Started:
                    // Complete initialization needed to switch to running state.
                    if (requestedState == ServiceStates.Running)
                    {   // We are trying to advance to full running state.
                        // TODO: Check whatever conditions we need to run.
                        bool isReadyToRun = true;
                        if (isReadyToRun)
                        {
                            m_ServiceState = ServiceStates.Running;

                            // TEMP FOR DEBUGGING ONLY
                            //object[] p = new object[2];
                            //p[0] = ExecutionHub.tempXML;            // objects to make
                            //p[1] = string.Empty;
                            //HubEventEnqueue(m_Requests.Get(RequestCode.CreateNewEngineContainer, p)); // automatically try to advance
                            // END OF DEBUG
                        }
                    }
                    else if (requestedState == ServiceStates.Stopped || requestedState == ServiceStates.Stopping)
                    {   // Something in start up procedure failed.  We are trying to stop.
                        m_ServiceState = ServiceStates.Stopping;
                        HubEventEnqueue(m_Requests.Get(RequestCode.ServiceStateChange, ServiceStates.Stopped)); // automatically try to advance
                    }
                    break;
                case ServiceStates.Running:
                    if (requestedState == ServiceStates.Stopped || requestedState == ServiceStates.Stopping)
                    {   // We want to move to Stopped state.
                        m_ServiceState = ServiceStates.Stopping;            // mark our desire to stop.
                        HubEventEnqueue(m_Requests.Get(RequestCode.ServiceStateChange, ServiceStates.Stopped)); // automatically try to advance
                    }
                    break;
                case ServiceStates.Stopping:
                    // Complete whatever shutdown procedure is needed.
                    // Dispose of resources, etc.
                    bool isReadyToStop = true;
                    if (isReadyToStop)
                    {
                        m_ServiceState = ServiceStates.Stopped;
                        // this could cause an issue, we probably need to do some checking after the stop in the order engine
                        // has been called to make sure we don't recieve and fills at the last second or something that goes 
                        // unreported.  For now this works however.
                        foreach (Dictionary<int, ThreadContainer> execContainerDict in m_ExecutionContainers.Values)
                            foreach (ThreadContainer execContainer in execContainerDict.Values)
                                execContainer.IOrderEngine.Stop();
                        base.Stop();                                    // In this state we stop immediately.
                    }
                    break;
                case ServiceStates.Stopped:
                    // This state will never be called.
                    break;
                default:
                    break;
            }//switch ServiceState
            //
            // Exit
            //
            // Trigger state change.
            if (m_ServiceState != prevState)
                OnServiceStateChanged(prevState, m_ServiceState);
            m_Requests.Recycle(eventArg);
        }//ProcessServiceStateRequests()
        //
        //
        // *****************************************************************
        // ****            ProcessCreateStrategyRequest()               ****
        // *****************************************************************
        /// <summary>
        /// Process request to create new Engine.  All the operations here
        /// are performed by the Hub thread.  Once the Execution Strategy is
        /// ready to be launched, it is passed to its own thread, and then 
        /// never again touched by the hub thread.
        /// </summary>
        /// <param name="eventArg">contains data</param>
        private void ProcessCreateStrategyRequest(EngineEventArgs eventArg)
        {   //
            // Validate data in eventArg
            //
            if (eventArg.Status != EngineEventArgs.EventStatus.Request)
            {   // I only respond to a request.
                return;
            }
            if (eventArg.DataObjectList == null || eventArg.DataObjectList.Count < 2)
            {
                Log.NewEntry(LogLevel.Warning, "ProcessCreateNewContainer: Failed to extract data.");
                return;
            }
            string xmlString = (string)eventArg.DataObjectList[0];
            string strategyHubName = (string)eventArg.DataObjectList[1];
            int engineCount = 0;
            if (!int.TryParse((string)eventArg.DataObjectList[2], out engineCount))
                engineCount = -1;
            string containerTypeString = (string)eventArg.DataObjectList[3];

            //
            // Obtain the EngineContainer
            //
            Dictionary<int, ThreadContainer> executionContainers = null;
            if (!m_ExecutionContainers.TryGetValue(strategyHubName, out executionContainers))
            {   // This is first container for this particular hub.  Create a place for it.
                executionContainers = new Dictionary<int, ThreadContainer>();
                if (string.IsNullOrEmpty(DefaultHubName))
                    DefaultHubName = strategyHubName;
                m_ExecutionContainers.Add(strategyHubName, executionContainers);
            }
            ThreadContainer container = null;
            if (!executionContainers.TryGetValue(eventArg.EngineContainerID, out container))
            {
                Type containerType = (typeof(ThreadContainer).Assembly).GetType(containerTypeString); // convert string to exact type.
                container = (ThreadContainer)Activator.CreateInstance(containerType);                 // create instance 
                container.EngineContainerID = eventArg.EngineContainerID;
                executionContainers.Add(container.EngineContainerID, container);
                if (engineCount >= 0)
                    container.TotalEngineCount = engineCount;
                container.EngineContainerName = "need ContainerName";
                // Locate the Strategy server this request came from
                IService iService;
                if (AppServices.GetInstance().TryGetService(strategyHubName, out iService) && iService is IEngineHub)
                    container.RemoteEngineHub = (IEngineHub)iService;


                // TODO: Continue initializing the container
            }

            //
            // Create the Engine.
            //
            bool isSuccess = true;
            byte[] byteArray = Encoding.ASCII.GetBytes(xmlString);
            using (System.IO.MemoryStream stream = new System.IO.MemoryStream(byteArray))
            {
                try
                {
                    StringifiableReader stringReader = new StringifiableReader(stream);
                    List<IStringifiable> objects = stringReader.ReadToEnd();
                    foreach (IStringifiable iObject in objects)
                        if (iObject is Engine)
                            if (container.TryAddEngine((Engine)iObject))
                            {
                                //Engine engine = (Engine)iObject;
                                //int engineID = engine.EngineID;     // These are created via attributes (so they coincide with values set by StrategyHub).
                                //((Engine)iObject).SetupInitialize(this, container, engineID);
                            }
                            else
                            {
                                isSuccess = false;
                                Log.NewEntry(LogLevel.Warning, "ProcessCreateEngine: Failed to add {0} to container {1}.", iObject, container);
                            }
                }
                catch (Exception ex)
                {
                    isSuccess = false;
                    Log.NewEntry(LogLevel.Warning, "ProcessCreateEngine: {0}", ex.Message);
                }
            }//using


            if (isSuccess == false)
                return;

            // Finalize
            if (engineCount == container.EngineList.Count && container.IOrderEngine != null)
            {
                SetupAndStartContainer(container);          // finish initialization, add listener and start
            }
            else
            {

            }
        }//ProcessCreateEngine()
        //
        //
        // *****************************************************************
        // ****              ProcessAddContainerRequest()               ****
        // *****************************************************************
        /// <summary>
        /// A request from a local execution container that would like to add containers
        /// to this hub and have listeners added to them.
        /// </summary>
        /// <param name="eventArg"></param>
        private void ProcessAddContainerRequest(EngineEventArgs eventArg)
        {
            string strategyHubName = (string)eventArg.DataObjectList[0];
            
            Dictionary<int, ThreadContainer> executionContainers = null;
            
            if (!m_ExecutionContainers.TryGetValue(strategyHubName, out executionContainers))
            {   // This is first container for this particular hub.  Create a place for it.
                executionContainers = new Dictionary<int, ThreadContainer>();
                if (string.IsNullOrEmpty(DefaultHubName))
                    DefaultHubName = strategyHubName;
                m_ExecutionContainers.Add(strategyHubName, executionContainers);
            }
            
            ThreadContainer container = (ThreadContainer)eventArg.DataObjectList[1];
            if (!executionContainers.ContainsKey(eventArg.EngineContainerID))
            {   // double check we haven't added this container before.
                executionContainers.Add(container.EngineContainerID, container);
            }
            else
                Log.NewEntry(LogLevel.Error, "ProcessAddContainerRequest: Duplicate request to add container - {0}", container.EngineContainerName);

            SetupAndStartContainer(container);
        }
        //
        // *****************************************
        // ****     ProcessEngineEvent          ****
        // *****************************************
        /// <summary>
        /// A request from outside to change a parameter value.
        /// </summary>
        protected void ProcessEngineEvent(EngineEventArgs eventArgs)
        {
            ThreadContainer strategy = null;
            int strategyID = eventArgs.EngineContainerID;        // parameter change requested for this strategy.
            if (strategyID < 0)
            {   // This request is for all strategies
                Log.NewEntry(LogLevel.Error, "ProcessEngineEvent: Negative EngineContainerId not allowed in {0}.", eventArgs);
                eventArgs.EngineHubName = this.ServiceName;
                eventArgs.Status = EngineEventArgs.EventStatus.Failed;
                OnEngineChanged(eventArgs);
            }
            else if (m_ExecutionContainers[DefaultHubName].TryGetValue(strategyID, out strategy))
            {   // Found the strategy, pass it the request now.   
                // He is on another thread, so give him a thread safe copy.
                // He will be allowed to modify this object to compose his response.
                //EngineEventArgs copyEventArgs = eventArgs.Copy();
                strategy.ProcessEvent(eventArgs.Copy());
            }
            else
            {   // Unknown strategy
                Log.NewEntry(LogLevel.Error, "ProcessEngineEvent: Unknown EngineContainerId {0}", eventArgs);
                eventArgs.EngineHubName = this.ServiceName;
                eventArgs.Status = EngineEventArgs.EventStatus.Failed;
                OnEngineChanged(eventArgs);
            }
        }// ProcessParameterChangeRequest()
        //
        //
        // *****************************************
        // ****  ProcessSyntheticOrderRequest   ****
        // *****************************************
        /// <summary>
        /// A request for submission of a synthetic order.
        /// </summary>
        /// <param name="engineEventArg"></param>
        private void ProcessSyntheticOrderRequest(EngineEventArgs engineEventArg)
        {
            SyntheticOrder syntheticOrder = (SyntheticOrder)engineEventArg.DataObjectList[0];
            ThreadContainer strategy = null;
            int strategyID = engineEventArg.EngineContainerID;
            if (strategyID < 0)
            {   // This request is for all strategies
                Log.NewEntry(LogLevel.Error, "ProcessEngineEvent: Negative EngineContainerId not allowed in {0}.", syntheticOrder);
                engineEventArg.EngineHubName = this.ServiceName;
                engineEventArg.Status = EngineEventArgs.EventStatus.Failed;
                OnEngineChanged(engineEventArg);
            }
            else if (m_ExecutionContainers[DefaultHubName].TryGetValue(strategyID, out strategy))
            {   // Found the strategy, pass it the request now.   
                // He is on another thread, so give him a thread safe copy.
                strategy.ProcessEvent(engineEventArg.Copy());
            }
            else
            {   // Unknown strategy
                Log.NewEntry(LogLevel.Error, "ProcessEngineEvent: Unknown EngineContainerId {0}", syntheticOrder);
                engineEventArg.EngineHubName = this.ServiceName;
                engineEventArg.Status = EngineEventArgs.EventStatus.Failed;
                OnEngineChanged(engineEventArg);
            }
        }
        //
        //
        //
        // *********************************************************
        // ****                 UpdatePeriodic()                ****
        // *********************************************************
        /// <summary>
        /// This method is called on periodic intervals by the hub thread.
        /// </summary>
        protected override void UpdatePeriodic()
        {
            if (this.EngineChanged != null) CheckSpontaneousEngineEvents(); // Send any events to subscribers.
            int i = 0;
            lock (m_Lock)
            {
                while (i < m_ITimerSubscribers.Count)
                {
                    try
                    {
                        m_ITimerSubscribers[i].TimerSubscriberUpdate();
                    }
                    catch (Exception e)
                    {
                        Log.NewEntry(LogLevel.Error, "UpdatePeriodic: Failed to update TimeSubscibers", e.Message);
                    }
                    i++;
                }
            }
        }// UpdatePeriodic()
        //
        //
        //
        // ****************************************************************************
        // ****                 Check Spontaneous Engine Events()                  ****
        // ****************************************************************************
        //
        /// <summary>
        /// Triggers events for engines that have changed due to Strategy.MarketChange() calls; 
        /// This call should be throttled.
        /// </summary>
        private void CheckSpontaneousEngineEvents()
        {
            // Send spontaneous (Strategy initiated) engine events.
            List<EngineEventArgs> spontaneousEvents = new List<EngineEventArgs>();
            foreach(Dictionary<int,ThreadContainer> execDict in m_ExecutionContainers.Values)
                foreach (ThreadContainer execContainer in execDict.Values)
                    execContainer.AddSpontaneousEngineEvents(spontaneousEvents);
            OnEngineChanged(spontaneousEvents);
        }// CheckSpontaneousEngineEvents()
        //
        //
        // *****************************************************************
        // ****                 SetupAndStartContainer()                ****
        // *****************************************************************
        /// <summary>
        /// Caller would like to complete the container creation process adding an execution
        /// listener and starting the container.
        /// </summary>
        /// <param name="container"></param>
        private void SetupAndStartContainer(ThreadContainer container)
        {
            container.SetupInitialize(this);

            // The new engine created call back is now called on listener threads, 
            // after the execution strategy is ready to run.  He must call his 
            // container function to broadcast his readiness!

            // Connect the appropriate listener and START!
            ExecutionListener listener = CreateListener(string.Format("Listener{0}", container.EngineContainerID));
            container.AddExecutionListener(listener);
            container.Start();
        }
        #endregion// Hub Event Handler

        #region Abstract methods
        //
        //
        // *********************************************************
        // ****                 CreateListener()                ****
        // *********************************************************
        protected abstract ExecutionListener CreateListener(string listenerName);
        //
        //
        
        #endregion//Abstract methods

        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //        
        //
        //
        //
        #endregion//Private Methods

        #region Events
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        #endregion//Events

        #region IEngineHub
        public List<IEngineContainer> GetEngineContainers()
        {
            throw new NotImplementedException();
        }
        public event EventHandler EngineChanged;
        public void OnEngineChanged(EventArgs eventArgs)
        {
            if (this.EngineChanged != null)
                EngineChanged(this, eventArgs);
        }
        protected void OnEngineChanged(List<EngineEventArgs> eList)
        {
            if (EngineChanged != null)
            {
                foreach (EngineEventArgs e in eList) { EngineChanged(this, e); }
            }
        }
        #endregion // IEngineHub

        #region IService
        // *****************************************************************
        // ****                     IService                            ****
        // *****************************************************************
        public string ServiceName
        {
            get { return m_HubName; }
        }
        public void Connect()
        {
        }
        public override void RequestStop()
        {
            this.HubEventEnqueue(m_Requests.Get(RequestCode.ServiceStateChange, ServiceStates.Stopped));
        }//RequestStop().
        //
        // ****             Service State Changed               ****
        //
        public event EventHandler ServiceStateChanged;
        //
        /// <summary>
        /// Call this when we want to inform our subscribers of a state change.
        /// </summary>
        protected void OnServiceStateChanged(ServiceStates prevState, ServiceStates currentState)
        {
            if (ServiceStateChanged != null)
            {
                ServiceStateEventArgs eventArgs = new ServiceStateEventArgs(this, currentState, prevState);
                this.ServiceStateChanged(this, eventArgs);
            }
        }
        //
        #endregion//IService

        #region IStringifiable
        // *****************************************************************
        // ****                 IStringifiable                          ****
        // *****************************************************************
        string IStringifiable.GetAttributes()
        {
            throw new NotImplementedException();
        }
        List<IStringifiable> IStringifiable.GetElements()
        {
            throw new NotImplementedException();
        }
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
            bool b;
            foreach (KeyValuePair<string, string> a in attributes)
            {
                if (a.Key.Equals("ShowLog") && bool.TryParse(a.Value, out b))
                    this.Log.IsViewActive = b;
            }
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {

        }
        #endregion//IStringifiable



    }//end class
}
