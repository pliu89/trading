using System;
using System.Collections.Generic;
using System.Text;

namespace Misty.Lib.TaskHubs
{
    using Misty.Lib.IO.Xml;

    public class TaskEventArg : EventArgs, IStringifiable
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // Internal variables used by TaskHub
        public TaskStatus Status = TaskStatus.New;                  // controlled by TaskHub
        private List<TaskEventArg> m_Children = null;               // Linking of Tasks: parent/children
        private TaskEventArg m_Parent = null;
        

        // Controls
        // These are read and manipulated by IStringifiable 
        // implementation and the TaskHub.
        public string EventHandlerName = string.Empty;              // Name of method to call.
        protected DateTime m_StartTime = DateTime.MinValue;         // (optional) don't start this task until after StartTime
        public DateTime StopTime = DateTime.MinValue;               // (optional) keep re-trying task after each failure until this time, then we give up.
        public bool RequestStop = false;                            // set this task in final list to shutdown application.
        
        



        // User input data.
        // This data list is completely ignored by TaskEventHub.
        // It may be used by the implemented EventHandler, which 
        // must recognize the InData provided and properly recast it.
        public List<object> InData = null;                          // IStringifiable subelements (but not TaskEventArgs) are added here.

        // User output data:
        // This can be loaded by the EventHander method, to be
        // later extracted by another user method.
        // Its completed ignored by the TaskHub;
        // Its a space available for implementations to use.
        public List<object> OutData = null;                            // data returned on success to user
        


        //
        //
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public TaskEventArg()
        {
        }
        public TaskEventArg(string eventHandlerName)
        {
            this.EventHandlerName = eventHandlerName;
        }
        //
        //       
        #endregion//Constructors


        #region Public Properties
        // *****************************************************************
        // ****                Public Properties                        ****
        // *****************************************************************
        //
        /// <summary>
        /// Returns the StartTime of the Task.
        /// If this task has children, will return the later of either the 
        /// next Unsucessful child or its own StartTime.
        /// </summary>
        public DateTime StartTime
        {
            get
            {
                TaskEventArg currentChild;
                bool hasChild = this.TryGetNextUnsuccessfulChild(out currentChild);
                if ( hasChild && m_StartTime.CompareTo(currentChild.StartTime) < 0 )
                    return currentChild.StartTime;  
                else
                    return m_StartTime;
            }
            set
            {
                m_StartTime = value;
            }
        }
        //
        //
        #endregion // public Properties


        #region Public Methods
        // *****************************************************************
        // ****                   Public Methods                        ****
        // *****************************************************************
        //
        //
        public bool IsParent()
        {
            return (this.m_Children != null && this.m_Children.Count > 0);
        }
        public bool IsParentOf(TaskEventArg possibleChild)
        {
            return (this.m_Children != null && this.m_Children.Contains(possibleChild));
        }
        public bool IsChild
        {
            get { return m_Parent != null; }
        }
        public bool TryGetParent(out TaskEventArg parent)
        {
            parent = m_Parent;
            return (parent != null);
        }
        public bool TryToAddChild(TaskEventArg newChild)
        {
            if (m_Children == null)
                m_Children = new List<TaskEventArg>();
            if (m_Children.Contains(newChild))
                return false;
            m_Children.Add(newChild);
            newChild.m_Parent = this;
        return true;
        }// TryToAddChild()
        //
        public List<TaskEventArg> GetChildren()
        {
            if (m_Children == null)
                return null;
            else
                return m_Children;
        }
        //
        //
        // ****                 TryGetNextUnsuccessfulChild()               ****
        /// <summary>
        /// This returns the latest child in the list that is NOT a success, or
        /// it returns the last child regardless.
        /// </summary>
        /// <param name="currentChild"></param>
        /// <returns>True, if currentChild is not null</returns>
        public bool TryGetNextUnsuccessfulChild(out TaskEventArg currentChild)
        {
            currentChild = null;
            if (m_Children == null || m_Children.Count == 0)
                return false;
            int n = 0;
            while (n < m_Children.Count && currentChild == null)    // its the first event that is not "sucessful"
            {
                TaskEventArg child = (TaskEventArg)m_Children[n];
                if (child.Status != TaskStatus.Success)          // look for first child that is not completed!
                    currentChild = child;                          // If found, he is the current working task
                n++;
            }
            if (currentChild == null)
                currentChild = m_Children[m_Children.Count-1];      // return last child at least.
            // Exit.
            return true;
        }//TryGetCurrentChild()
        //
        //
        //
        #endregion // public methods



        #region Public overridden methods
        // *************************************************************
        // ****                 Public Override Methods             ****
        // *************************************************************
        //
        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
            s.Append("[");
            if (this.RequestStop)
                s.Append("StopRequest ");
            else if (m_Children == null || m_Children.Count == 0)
                s.AppendFormat("{0} ", this.EventHandlerName);
            s.AppendFormat("({0}) ",this.Status);
            // Display only is active
            if (this.Status == TaskStatus.WaitAndTryAgain)
            {
                if (this.m_StartTime.CompareTo(DateTime.MinValue) > 0)
                    s.AppendFormat("Start at {0} ", m_StartTime.ToString());
                   if (this.StopTime.CompareTo(DateTime.MinValue) > 0)
                    s.AppendFormat("Stop at {0} ", StopTime.ToString());
            }

            if (m_Children != null && m_Children.Count > 0 )
            {
                s.Append("Children=");
                foreach (TaskEventArg e in m_Children)
                    s.AppendFormat("{0} ", e); 
            }
            s.Remove(s.Length - 1, 1);
            s.Append("]");
            // Exit
            return s.ToString();
        }// ToString()
        //
        //
        #endregion // public methods



        #region IStringifiable 
        // *************************************************************
        // ****                 IStringifiable                      ****
        // *************************************************************
        //
        public virtual string GetAttributes()
        {
            StringBuilder s = new StringBuilder();
            if (this.RequestStop)
                s.Append("RequestStop=true");
            if (string.IsNullOrEmpty(EventHandlerName))
                s.AppendFormat("EventHandler={0}", this.EventHandlerName);
            if (this.m_StartTime.CompareTo(DateTime.MinValue) != 0)
                s.AppendFormat("StartTime={0}", this.m_StartTime.ToString(Misty.Lib.Utilities.Strings.FormatTime));
            if (this.StopTime.CompareTo(DateTime.MinValue) != 0)
                s.AppendFormat("StopTime={0}", this.StopTime.ToString(Misty.Lib.Utilities.Strings.FormatTime));

            return s.ToString();
        }
        public virtual List<IStringifiable> GetElements()
        {
            List<IStringifiable> elements = null;
            // Load any children.
            if (m_Children != null && m_Children.Count > 0)
            {
                if (elements == null)
                    elements = new List<IStringifiable>();
                foreach (TaskEventArg task in m_Children)
                    elements.Add((IStringifiable)task);
            }
            // Pass others now.
            return elements;
        }
        public virtual void SetAttributes(Dictionary<string, string> attributes)
        {
            bool isTrue;
            DateTime dateTime;
            TimeSpan timeSpan;
            foreach (string key in attributes.Keys)
            {
                if (key.Equals("RequestStop") && bool.TryParse(attributes[key], out isTrue))
                    this.RequestStop = isTrue;
                else if (key.Equals("EventHandler"))
                    this.EventHandlerName = attributes[key].Trim();
                else if (key.Equals("StartTime") )
                {
                    timeSpan = TimeSpan.Parse(attributes[key]);
                    dateTime =DateTime.Now.Date.Add(timeSpan);
                    if (dateTime.AddHours(1.0).CompareTo(DateTime.Now) < 0) // Make sure StartTime is within 1hr of past, or in future.
                        dateTime = dateTime.AddDays(1.0);                   // if we're already past that time, then set it to tomorrow.
                    this.m_StartTime = dateTime;
                }
                else if (key.Equals("StopTime"))
                {
                    timeSpan = TimeSpan.Parse(attributes[key]);
                    dateTime = DateTime.Now.Date.Add(timeSpan);
                    if (dateTime.AddHours(1.0).CompareTo(DateTime.Now) < 0)
                        dateTime = dateTime.AddDays(1.0);               // if we're already past that time, then set it to tomorrow.
                    this.StopTime = dateTime;
                }
                //

            }
        }
        public virtual void AddSubElement(IStringifiable subElement)
        {
            if (subElement is TaskEventArg)
                this.TryToAddChild((TaskEventArg)subElement);
            else
            {
                if (this.InData == null)
                    this.InData = new List<object>();
                this.InData.Add(subElement);
            }
        }
        //
        //
        #endregion // IStringifiable





    }
}
