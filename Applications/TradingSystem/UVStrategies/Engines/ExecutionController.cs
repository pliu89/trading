﻿using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Strategies.Engines
{
    using UV.Lib.Hubs;
    using UV.Lib.Engines;
    using UV.Lib.FrontEnds.GuiTemplates;
    using UV.Lib.IO.Xml;
    using UV.Lib.Application;

    using UV.Strategies.StrategyHubs;

    /// <summary>
    /// </summary>
    public class ExecutionController : Engine
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        // Local Engine Identification
        //
        private Hub m_LocalStrategyHub = null;
        private string m_LocalStrategyHubName = string.Empty;
        private int m_EngineContainerId = -1;
        private bool m_IsSubEngine = false;                     // Is an sub-element of another engine.
        
        // Current state 
        private bool m_IsReadyForSetup = false;                    // indicates that initialization callback was received from remote hub.


        //
        // Description of Execution Strategy counter-part
        //
        private IEngineHub m_RemoteExecutionHub = null;
        private string m_ExecutionEngineClassName = string.Empty;
        private Node m_Node = new Node();

        

        #endregion// members


        #region Constructors & Setup
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public ExecutionController() : base()
        {

        }
        //
        // *********************************************
        // ****         SetUp Initialize()          ****
        // *********************************************
        /// <summary>
        /// </summary>
        protected override void SetupInitialize(IEngineHub myEngineHub, IEngineContainer engineContainer, int engineID, bool setupGui)
        {
            // Save information about local strategy hub.
            m_EngineContainerId = engineContainer.EngineContainerID;
            if (myEngineHub is IService)
                m_LocalStrategyHubName = ((IService)myEngineHub).ServiceName;
            m_LocalStrategyHub = (Hub)myEngineHub;

            // Locate our target remote hub/service.
            StrategyHub strategyHub = ((StrategyHub) myEngineHub);
            IEngineHub remoteEngineHub = null;
            EventHandler del = new EventHandler(ProcessRemoteEngineHubEvents);
            if (strategyHub.SubscribeToRemoteEngineHub("ExecutionHub", m_EngineContainerId, m_EngineID, del, out remoteEngineHub) )
            {   
                m_RemoteExecutionHub = remoteEngineHub;
            }
            else
            {
                throw new Exception("Failed to locate remote hub.");
            }


            // Create the parameter table.  
            Type remoteEngineType;
            if (Stringifiable.TryGetType(m_ExecutionEngineClassName, out remoteEngineType))
                this.m_PInfo = CreateParameterInfo(myEngineHub, engineContainer.EngineContainerID, this.EngineID, remoteEngineType);

            /*
            // Create the gui specs.
            if (setupGui)
            {
                EngineGui engineGui = new EngineGui();
                engineGui.EngineID = this.EngineID;
                engineGui.DisplayName = this.EngineName;
                engineGui.EngineFullName = this.m_EngineClass;
                engineGui.ParameterList.AddRange(this.m_PInfo);
                m_EngineGuis.Add(engineGui);
            }
            */
            //
            // Add sub-engines to the parent Strategy
            //  
            //  They will be automatically initialized outside in StrategyHub loop
            //  that called us, since we will add new engine to the end of the list we are looping thru now.
            List<IStringifiable> subElements = m_Node.GetElements();
            if (subElements != null)
            {
                foreach (IStringifiable iObject in subElements)
                {   // Engines that are beneath non-engines will not be found here.
                    if (iObject is ExecutionController)                            // This remote engine will not need to broad its existance,
                    {
                        ExecutionController subEngine = (ExecutionController)iObject;
                        subEngine.m_IsSubEngine = true;                     // since it will be included in another engine (this one).
                        ((Strategy)engineContainer).TryAddEngine(subEngine, myEngineHub);
                    }
                }
            }

        }//SetupInitialize()
        //
        //
        //
        //
        // ************************************************
        // ****             SetupBegin()               ****
        // ************************************************
        /// <summary>
        /// </summary>
        public override void SetupBegin(IEngineHub myEngineHub, IEngineContainer engineContainer)
        {
            //
            // Master Remote-Engines must send requests to ExecutionHub now.
            //            
            if (m_IsSubEngine == false)
            {
                // Count number of (master) remote engines found in this strategy.
                // Master remote engines are those connected directly to the Strategy,
                // without being held by another engine.  
                // Only masters broadcast their xml to the ExecutionHub, and other sub-engine
                // engines are included as subelements of their master's broadcast.
                // (If each remote engine broadcasted separately, the ExecHub would have to know how
                // to put them all back together again... that is, who owned whom.)
                int remoteMasterEngineCount = 0;
                foreach (IEngine iEng in engineContainer.GetEngines())
                {
                    if (iEng is ExecutionController && ((ExecutionController)iEng).m_IsSubEngine == false)
                        remoteMasterEngineCount++;
                    //if (iEng is RemoteEngine)           // now count all remote engines
                    //    remoteMasterEngineCount++;
                }

                //
                // Create my engine creation request
                //
                Dictionary<Type, string[]> rules = new Dictionary<Type, string[]>();
                rules.Add(this.GetType(), new string[] { "GetClassName", string.Empty, string.Empty });
                string xmlString = Stringifiable.Stringify(this, rules);

                EngineEventArgs e = new EngineEventArgs();
                e.MsgType = EngineEventArgs.EventType.NewEngine;
                e.Status = EngineEventArgs.EventStatus.Request;
                e.EngineID = m_EngineID;
                e.EngineContainerID = m_EngineContainerId;
                e.DataObjectList = new List<object>();
                e.DataObjectList.Add(xmlString);                        // 0 - engine specs
                e.DataObjectList.Add(m_LocalStrategyHubName);             // 1 - engine hub name
                e.DataObjectList.Add(remoteMasterEngineCount.ToString());     // 2 - number of engines remote hub should expect from this EngineContainer
                // Send request to remote.
                if (m_RemoteExecutionHub != null && m_RemoteExecutionHub.HubEventEnqueue(e))
                    m_LocalStrategyHub.Log.NewEntry(LogLevel.Minor, "SetupBegin: Remote-{0} sent creation request to remote.", m_ExecutionEngineClassName);
                else
                    m_LocalStrategyHub.Log.NewEntry(LogLevel.Minor, "SetupBegin: Remote-{0} failed to send creation request to remote.", m_ExecutionEngineClassName);
            }// master remote-engine sends request.
            

        }//SetupBegin()
        //
        //
        //
        // *********************************************
        // ****             GetClassName()          ****
        // *********************************************
        /// <summary>
        /// Used as an override for Stringifiable.Stringify() function.
        /// </summary>
        /// <returns></returns>
        public string GetClassName()
        {
            return m_ExecutionEngineClassName;
        }
        //
        //
        // *****************************************************
        // ****             Setup Complete()                ****
        // *****************************************************
        /// <summary>
        /// Call this after all Strategy Engines have had "SetupBegin()" is called.
        /// Note:
        ///     1) 
        /// </summary>
        //public override void SetupComplete()
        //{
        //}
        //
        //
        //
        //
        //       
        #endregion//Constructors and Setup


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        public bool IsReadyForSetup
        {
            get { return m_IsReadyForSetup; }
            set
            {
                if ( m_IsReadyForSetup == false)
                    m_IsReadyForSetup = value;
            }
        }
        //
        #endregion//Properties


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //
        //
        // *************************************
        // ****         ToString()          ****
        // *************************************
        public override string ToString()
        {
            return string.Format("ExecControl {0}", m_Node);
        }
        //
        //
        //
        //
        #endregion//Public Methods


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        protected void ProcessRemoteEngineHubEvents(object sender, EventArgs eventArgs)
        {
            if (eventArgs is EngineEventArgs)
            {
                EngineEventArgs eventArg = (EngineEventArgs)eventArgs;
                switch(eventArg.MsgType)
                {
                    case EngineEventArgs.EventType.NewEngine:
                        ProcessNewEngine(eventArg);
                        break;
                    default:
                        if (eventArg.Status == EngineEventArgs.EventStatus.Confirm) // TODO: pass fails too?
                        {
                            EngineEventArgs outEventArg = new EngineEventArgs();
                            eventArg.CopyTo(outEventArg);
                            outEventArg.EngineHubName = m_LocalStrategyHub.HubName;
                            ((StrategyHub)m_LocalStrategyHub).OnEngineChanged(outEventArg);
                        }
                        break;
                }                    
            }
            else
                m_LocalStrategyHub.Log.NewEntry(LogLevel.Error,"ProcessRemoteEvents: Unknown event {0}",eventArgs);
        }//ProcessRemoteEngineHubEvents
        //
        //
        // *****************************************************
        // ****             ProcessNewEngine()              ****
        // *****************************************************
        protected void ProcessNewEngine(EngineEventArgs eventArg)
        {
            int ptr = 0;
            m_EngineGuis = new List<EngineGui>();
            while ( eventArg.DataObjectList.Count > ptr)
            {
                if (eventArg.DataObjectList[ptr] is EngineGui)
                    m_EngineGuis.Add((EngineGui)eventArg.DataObjectList[ptr]);
                else
                {

                }
                ptr++;
            }
            this.IsReadyForSetup = true;
            ((StrategyHub)m_LocalStrategyHub).ProcessStrategy(m_EngineContainerId);
        }//ProcessNewEngine()
        //
        #endregion//Private Methods


        #region no Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        #endregion//Event Handlers


        #region IEngine
        // *****************************************************************
        // ****                     IEngine                             ****
        // *****************************************************************
        //public int EngineID
        //{
        //    get { return m_EngineId; }
        //}
        //public string EngineName
        //{
        //    get { return m_EngineName; }
        //}
        public override bool IsUpdateRequired
        {
            get{ return false; }
            set{ ;}
        }
        public override void ProcessEvent(EventArgs eArgs)
        {
            if ((eArgs is EngineEventArgs)==false)
            {
                m_LocalStrategyHub.Log.NewEntry(LogLevel.Error,"ProcessEvent: Unknown event {0}",eArgs);
                return;
            }
            EngineEventArgs eventArgs = (EngineEventArgs)eArgs;
            if ( eventArgs.Status == EngineEventArgs.EventStatus.Request )
            {   
                if (eventArgs.EngineHubName.Equals(m_LocalStrategyHub.HubName))
                {
                    EngineEventArgs outEventArg = new EngineEventArgs();
                    eventArgs.CopyTo(outEventArg);
                    outEventArg.EngineHubName = m_RemoteExecutionHub.ServiceName;
                    m_RemoteExecutionHub.HubEventEnqueue( outEventArg );
                }
            }
            else
                m_LocalStrategyHub.Log.NewEntry(LogLevel.Error,"ProcessEvent: Recieved non-request {0}",eventArgs);
        }//ProcessEvent()
        //
        //
        //
        #endregion // IEngine


        #region IStringifiable
        // *****************************************************************
        // ****                     IStringifiable                      ****
        // *****************************************************************
        //
        /// <summary>
        /// This extension of Node will add a few more attributes.
        /// All SubElements will be contained in the Node base object.
        /// </summary>
        /// <returns></returns>
        public override string GetAttributes()
        {
            StringBuilder s = new StringBuilder();
            if (! string.IsNullOrEmpty(m_ExecutionEngineClassName) )
                s.AppendFormat("EngineClass={0}", this.m_ExecutionEngineClassName);
            //if (!string.IsNullOrEmpty(m_LocalEngineHubName))
            //    s.AppendFormat(" EngineHubName={0}", this.m_LocalEngineHubName);
            //if (m_EngineContainerId >= 0) 
            //    s.AppendFormat(" EngineContainerId={0}", this.m_EngineContainerId);
            if (m_EngineID >= 0) 
                s.AppendFormat(" EngineId={0}", this.m_EngineID);
            s.AppendFormat(" {0}", m_Node.GetAttributes());                   
            return s.ToString();
        }
        public override void SetAttributes(Dictionary<string, string> attributes)
        {
            int n;
            Dictionary<string, string> baseAttributes = new Dictionary<string, string>();
            foreach (KeyValuePair<string,string> a in attributes)
            {
                if (a.Key.Equals("EngineClass"))
                {
                    m_Node.Name = a.Value;                                  // Store class name
                    this.m_ExecutionEngineClassName = a.Value;
                    n = m_ExecutionEngineClassName.LastIndexOf('.');
                    if (n < 0 || n >= m_ExecutionEngineClassName.Length - 2)
                        m_EngineName = m_ExecutionEngineClassName;
                    else
                        m_EngineName = m_ExecutionEngineClassName.Substring(n + 1);      // get last part of classname.
                }
                //else if (a.Key.Equals("EngineHubName"))
                //    this.m_LocalEngineHubName = a.Value;
                //else if (a.Key.Equals("EngineContainerId") && int.TryParse(a.Value, out n))
                //    this.m_EngineContainerId = n;
                else if (a.Key.Equals("EngineId") && int.TryParse(a.Value, out n))
                    this.m_EngineContainerId = n;
                else
                    baseAttributes.Add(a.Key, a.Value);
            }
            // Call base
            m_Node.SetAttributes(baseAttributes);                
        }
        public override List<IStringifiable> GetElements()
        {
            return m_Node.GetElements();
        }
        public override void AddSubElement(IStringifiable subElement)
        {
            m_Node.AddSubElement(subElement);
        }        
        #endregion//IStringifiable

        

    }//end class
}
