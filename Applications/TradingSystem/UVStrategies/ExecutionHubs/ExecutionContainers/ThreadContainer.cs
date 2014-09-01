using System;
using System.Collections.Generic;
//using System.Linq;
//using System.Text;

namespace UV.Strategies.ExecutionHubs.ExecutionContainers
{
    using UV.Lib.IO.Xml;
    using UV.Lib.Engines;
    using UV.Lib.BookHubs;
    using UV.Lib.Hubs;
    using UV.Lib.OrderBooks;
    using UV.Lib.Products;
    using UV.Lib.Application;
    using UV.Lib.Utilities;
    
    using UV.Strategies.StrategyHubs;
    using UV.Strategies.ExecutionEngines.OrderEngines;
    using UV.Strategies.StrategyEngines;
    
    
    
    /// <summary>
    /// This is a container for all object pertaining to single "execution unit" that contains only one thread.
    /// </summary>
    public class ThreadContainer : IStringifiable, IEngineContainer
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // 
        // Remote strategy hub pointer
        public IEngineHub RemoteEngineHub = null;

        // Local Hub identity
        private IEngineHub m_LocalEngineHub = null;
        protected LogHub Log = null;
        private string m_LocalEngineHubName = string.Empty;
        private int m_EngineContainerId = -1;                       // not assigned
        private string m_EngineContainerName = string.Empty;        // user-friendly name
        protected bool m_IsLaunched = false;                          // flag is true after start has been called.
        //        
        // My Engines
        //
        public IOrderEngine IOrderEngine = null;                     // the order engine we would like to send trade objects to be processed.
        public Dictionary<int, Engine> EngineList = new Dictionary<int, Engine>();        // my engines
        protected List<IEngine> m_IEngineList = new List<IEngine>();  // my IEngines (same as above)
        public int TotalEngineCount = 0;                            // Engines we expect to create. 

        //
        // My Markets and Orders (public so anyone can have direct access who needs it
        //
        public Dictionary<InstrumentName, Market> m_Markets = new Dictionary<InstrumentName, Market>();
        public Dictionary<InstrumentName, OrderInstrument> m_OrderInstruments = new Dictionary<InstrumentName, OrderInstrument>();
        public Dictionary<InstrumentName, InstrumentDetails> m_InstrDetails;            // this will be a pointer to the dictionary held by the ExecutionListener.

        //
        // ExecutionListener - kept for acess to the main thread.
        //
        public ExecutionListener m_ExecutionListener = null;

        //
        // Objects to reuse
        //
        private EngineEventArgs m_ConfirmSynthOrderEventArg = new EngineEventArgs(); // reusable confirmation event arg.
        #endregion// members


        #region Constructors and Engine Setup
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public ThreadContainer()
        {

        }
        //
        // ****         Setup Initialize()          ****
        // 
        /// <summary>
        /// This is called by the local execution hub thread.
        /// </summary>
        /// <param name="hub"></param>
        public void SetupInitialize(IEngineHub hub)
        {
            // Keep pointers to my local hub.
            m_LocalEngineHub = hub;
            m_LocalEngineHubName = hub.ServiceName;
            Log = ((Hub)m_LocalEngineHub).Log;
            // Initialize all my engines.
            int ptr = 0;
            while (ptr < m_IEngineList.Count)
            {
                if (m_IEngineList[ptr] is Engine)
                {
                    Engine engine = (Engine)m_IEngineList[ptr];
                    engine.SetupInitialize(hub, this, engine.EngineID);
                }
                ptr++;
            }

            m_ConfirmSynthOrderEventArg.Status = EngineEventArgs.EventStatus.Confirm;   // create reusable event arg.
            m_ConfirmSynthOrderEventArg.EngineHubName = this.RemoteEngineHub.ServiceName;
            m_ConfirmSynthOrderEventArg.EngineContainerID = this.EngineContainerID;
            m_ConfirmSynthOrderEventArg.MsgType = EngineEventArgs.EventType.SyntheticOrder;
            m_ConfirmSynthOrderEventArg.DataObjectList = new List<object>();

        }// SetupInitialize()
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
        //
        //
        // *****************************************************
        // ****            AddExecutionListener()            ****
        // *****************************************************
        /// <summary>
        /// </summary>
        public virtual void AddExecutionListener(ExecutionListener listener)
        {
            m_ExecutionListener = listener;
            if (m_IsLaunched == false)
            {
                this.IOrderEngine.SetExecutionListener(listener);
            }
        }//AddExecutionListener()
        //
        //
        //
        // *****************************************************
        // ****                 Start()                     ****
        // *****************************************************
        /// <summary>
        /// </summary>
        public void Start()
        {
            if (m_IsLaunched == false)
            {
                m_IsLaunched = true;
                IOrderEngine.Start();
            }
        }//Start()
        //
        //
        //
        // *****************************************************
        // ****                 TryAddEngine()              ****
        // *****************************************************
        /// <summary>
        /// A nice way for hub to add engines to this container.
        /// </summary>
        /// <param name="oEngine"></param>
        /// <returns></returns>
        public virtual bool TryAddEngine(Engine oEngine)
        {
            if (m_IsLaunched)
                return false;
            if (oEngine is IOrderEngine)
            {
                if (this.IOrderEngine != null)
                    return false;
                else
                    this.IOrderEngine = (IOrderEngine)oEngine;
            }
            if (oEngine is Engine)
            {
                Engine engine = (Engine)oEngine;
                this.EngineList.Add(engine.EngineID, engine);

                this.m_IEngineList.Add(engine);
            }
            return true;
        }// TryAddEngine()
        //
        //
        //
        // *********************************************
        // ****       ConfirmStrategyLaunched()     ****
        // *********************************************
        /// <summary>
        /// Broadcast to RemoteEngineHub that this execution strategy is ready.
        /// </summary>
        public void ConfirmStrategyLaunched()
        {
            Log.NewEntry(LogLevel.Minor, "ExecutionContainer {1} #{0}.  Confirming launch of {2} engines.", this.EngineContainerID, this.EngineContainerName, this.EngineList.Count);
            foreach (Engine engine in this.EngineList.Values)
            {
                EngineEventArgs e = EngineEventArgs.ConfirmNewEngine(m_LocalEngineHub.ServiceName, this.EngineContainerID, engine);
                this.RemoteEngineHub.HubEventEnqueue(e);
            }
        }
        //
        //
        // *********************************************
        // ****      SendSyntheticOrderToRemote()   ****
        // *********************************************
        /// <summary>
        /// Called by an order engine who would like to send back to the strategy hub
        /// a synthetic order that has been updated. - This should probably be renamed.
        /// but just going with this for now.
        /// </summary>
        /// <param name="syntheticOrder"></param>
        public void SendSyntheticOrderToRemote(SyntheticOrder syntheticOrder)
        {
            m_ConfirmSynthOrderEventArg.DataObjectList.Clear(); // clear before each reuse.
            m_ConfirmSynthOrderEventArg.DataObjectList.Add(syntheticOrder);
            this.RemoteEngineHub.HubEventEnqueue(m_ConfirmSynthOrderEventArg.Copy());  
        }
        //
        //
        // *********************************************
        // ****             ToString()              ****
        // *********************************************
        public override string ToString()
        {
            return string.Format("{0}", m_EngineContainerId);
        }
        //
        //
        //
        // *************************************************************
        // ****             Add Spontaneous Events()                ****
        // *************************************************************
        /// <summary>
        /// Periodically, this method should be called by the strategy hub thread.
        /// Herein, engines are allowed to generate events and add them to the event list
        /// to be sent to all Engine Event subscribers.  These events are called 
        /// "spontaneous" in that the engines have created them, in contrast to the usual 
        /// case of responding to an outside request.
        /// Called by internal hub thread.
        /// </summary>
        /// <returns></returns>
        public void AddSpontaneousEngineEvents(List<EngineEventArgs> eventList)
        {
            foreach (Engine engine in EngineList.Values)
                engine.AddSpontaneousEngineEvents(eventList);

        }//AddSpontaneousEngineEvents()
        #endregion//Public Methods


        #region no Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        #endregion//Private Methods


        #region IEngineContainer
        // *****************************************************************
        // ****                     IEngineContainer                    ****
        // *****************************************************************
        public int EngineContainerID
        {
            get { return m_EngineContainerId; }
            set { m_EngineContainerId = value; }
        }
        public string EngineContainerName
        {
            get { return m_EngineContainerName; }
            set { m_EngineContainerName = value; }
        }
        public List<IEngine> GetEngines()
        {
            return m_IEngineList;
        }
        public Lib.FrontEnds.Clusters.Cluster GetCluster()
        {
            throw new NotImplementedException();
        }
        public Lib.FrontEnds.Clusters.ClusterConfiguration ClusterConfiguation
        {
            get { throw new NotImplementedException(); }
        }
        //
        //
        // *************************************************
        // ****         Process Engine Event()          ****
        // *************************************************
        public virtual bool ProcessEvent(EventArgs e)
        {
            m_ExecutionListener.ProcessEvent(e);
            return true;
        }
        //
        #endregion // IEngineContainer


        #region IStringifiable
        string IStringifiable.GetAttributes()
        {
            return string.Empty;
        }
        System.Collections.Generic.List<IStringifiable> IStringifiable.GetElements()
        {
            return null;
        }
        void IStringifiable.SetAttributes(System.Collections.Generic.Dictionary<string, string> attributes)
        {
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {
            if (subElement is IOrderEngine)
            {   // This is my primary order engine.  
                // Perhaps there should be only one of these.  Here is where I send
                // order requests.
                IOrderEngine = (IOrderEngine)subElement;
            }
            if (subElement is Engine)
            {   // Here we store all engines (including order engine).
                Engine engine = (Engine)subElement;
                EngineList.Add(engine.EngineID, engine);
            }
        }
        #endregion//IStringifiable

    }//end class
}
