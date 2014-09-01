using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Strategies
{
    using UV.Lib.Application;
    using UV.Lib.Engines;       // IEngineContainer
    using UV.Lib.IO.Xml;        // IStringifiable
    using UV.Lib.Products;
    using UV.Lib.BookHubs;
    using UV.Lib.MarketHubs;
    using UV.Strategies.Engines;
    using UV.Lib.Hubs;
    using UV.Lib.FrontEnds.GuiTemplates;

    using UV.Lib.Fills;

    /// <summary>
    /// </summary>
    public class Strategy : IEngineContainer, IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // My identity
        //
        private static int m_LastStrategyID = 0;                    // static counter to create unique strategy ids.
        public AppServices m_Services = AppServices.GetInstance();
        public string Name = string.Empty;                          // user-friendly strategy name
        public readonly int ID;                                     // unique id number for this strategy in this hub.
        public StrategyHub StrategyHub = null;
        
        // Database information
        public UV.Lib.DatabaseReaderWriters.Queries.StrategyQueryItem QueryItem = null;         // query row associated with me.


        //
        // My internal controls
        //
        private bool m_IsStopped = false;                       // set during final shutdown. TODO: Strategies should have state enum!!!
        private bool m_IsInitializeComplete = false;
        private bool m_IsBeginComplete = false;
        public bool IsLaunched = false;                         


        //
        // My Engines
        //
        private List<Engine> m_Engines = new List<Engine>();
        public Engines.PricingEngine m_PricingEngine = null;
        public ZGraphEngine m_GraphEngine = null;
        public IOrderEngine m_OrderEngine = null;
        private EngineContainerGui m_EngineContainerGui = null;


        //
        // Strategy Hub lookup tables for this strategy.
        //
        public List<InstrumentName> m_MarketInstrumentList = new List<InstrumentName>();
        public List<ITimerSubscriber> m_MyTimerSubscribers = new List<ITimerSubscriber>();


        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public Strategy()
        {
            this.ID = System.Threading.Interlocked.Increment(ref Strategy.m_LastStrategyID);
        }
        //
        //
        //
        //
        //       
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        // ****         Sql Id          ****
        /// <summary>
        /// This is the unique row ID number of this strategy.
        /// Or returns -1 if strategy was not created from database entry.
        /// </summary>
        public int SqlId
        {
            get
            {
                if (QueryItem != null)
                    return QueryItem.StrategyId;
                else
                    return -1;
            }
        }
        //
        //
        //
        public bool IsReadyForSetup
        {
            get
            {
                bool isReady = true;
                foreach (Engine engine in m_Engines)
                    if (engine is RemoteEngine)
                    {
                        RemoteEngine remoteEngine = (RemoteEngine)engine;
                        if (remoteEngine.IsReadyForSetup == false)
                        {
                            isReady = false;
                            break;
                        }                        
                    }
                return isReady;
            }
        }
        //
        #endregion//Properties


        #region Public Initialization Methods 
        // *****************************************************************
        // ****                 Public Initialization                   ****
        // *****************************************************************
        //
        //
        // ****************************************
        // ****     Setup Initialize()         ****
        // ****************************************
        /// <summary>
        /// The Strategy has been created, and now we add its engines.
        /// When we call Engine.SetupInitialize() the engine can make NO assumptions
        /// about the Strategy, except that it and its StrategyHub exists.
        /// Other Engines may or may not exist.
        /// What is allowed is that Engines can spawn other Engines and add them 
        /// to the *end* of the Strategy.m_Engines[] list freely.
        /// </summary>
        public void SetupInitialize(IEngineHub myHub)
        {
            StrategyHub = (StrategyHub)myHub;                           // store ptr to new parent hub.

            // First initialize each of my engines.
            int id = 0;
            while (id < m_Engines.Count)                                // Loop using while so that Engines can ADD new engines to end of list.
            {                                                           // Adding new engines spontaneously is allowed here only.
                Engine engine = m_Engines[id];
                engine.SetupInitialize(StrategyHub, this, id);          // Tell Engine his new Hub, and owner, and his new id#.
                
                // Keep track of important engine ptrs we need.
                if (engine is Engines.PricingEngine)                    // using simple "if"s allows one engine to play multiple roles.
                    m_PricingEngine = (Engines.PricingEngine)engine;
                if (engine is IOrderEngine)
                    m_OrderEngine = (IOrderEngine)engine;
                if (engine is ZGraphEngine)
                    m_GraphEngine = (ZGraphEngine)engine;

                id++;
            }//next engine id
            m_IsInitializeComplete = true;                               // after this point if engines are added, they have to manually Initialize
            
            

        }//SetupInitialize()
        //
        //
        // ****************************************
        // ****         Setup Begin()          ****
        // ****************************************
        /// <summary>
        /// The setup phase continues.  
        /// Here, the Strategy and each of its engines have been created, and labeled.
        /// 
        /// Strategy may attempt to find the resources and other dependences it requires to run.
        /// Engine are free to look for each other now.
        /// This method is called after ALL strategies have been created (and their engines), and 
        /// after SetupInitialize().  The purpose of this is that
        /// by this time, engine can be assured that there exists a complete list of all 
        /// strategies and engines available.  They can find specific engines (of other strategies)
        /// that they need to link to, and make those initial linkages.
        /// 
        /// While not ideal, if an engine must add another one during this process it is still
        /// possible however it is much better to add them during SetupInitialize.
        /// TODO: 
        ///     1) To prevent multithread problems in future, Strategy must ask for pointer to 
        ///         another strategy, and this is monitored by hub, and pricing engines are 
        ///         ordered in tiers using this information.
        /// </summary>
        public void SetupBegin(IEngineHub myHub)
        {
            foreach (Engine engine in m_Engines)
                engine.SetupBegin(myHub,this);
            m_IsBeginComplete = true;
        }//end Initialize()
        //
        // 
        //
        // *************************************************
        // ****             Setup Complete()            ****
        // *************************************************
        public void SetupComplete()
        {
            // Next, initialize my EngineContainer template
            m_EngineContainerGui = new EngineContainerGui();
            m_EngineContainerGui.EngineContainerID = this.ID;
            m_EngineContainerGui.DisplayName = this.Name;
            foreach (Engine engine in m_Engines)
            {
                m_EngineContainerGui.m_Engines.AddRange(engine.GetGuiTemplates());
            }

            // Next allow engines to connect to each other.
            // Engines will assume that all other necessary engines are available.
            foreach (Engine engine in m_Engines)
                engine.SetupComplete();
        }
        //
        //
        #endregion//Initialization methods


        #region Public Run-Time Methods 
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        // 
        // ********************************************************************
        // ****                 MarketInstrumentInitialized()              ****
        // ********************************************************************
        /// <summary>
        /// Called during SetupInitialize or  SetupBegin to add engines to containter.
        /// If called at any other time, an exception will be thrown!
        /// 
        /// It is best to call during SetupInitialize if possible if other engines might need to 
        /// find the added engine.
        /// </summary>
        /// <param name="engine"></param>
        /// <returns></returns>
        public bool TryAddEngine(Engine engine, IEngineHub stratHub)
        {
            if (!m_IsInitializeComplete)
            { // we can easily add an engine
                if (!m_Engines.Contains(engine))
                {
                    int id = m_Engines.Count;
                    m_Engines.Add(engine);
                    engine.EngineID = id;
                    return true;
                }
                else
                    return false;
            }
            else if (!m_IsBeginComplete)
            { // we can add an engine but have to be careful to initialize properly
                if (!m_Engines.Contains(engine))
                {
                    int id = m_Engines.Count;
                    m_Engines.Add(engine);
                    engine.EngineID = id;
                    engine.SetupInitialize(StrategyHub, this, id);    // Tell Engine his new Hub, and owner, and his new id#.
                    m_EngineContainerGui.m_Engines.AddRange(engine.GetGuiTemplates());
                    engine.SetupBegin(stratHub, this);
                    return true;
                }
                else
                    return false;
            }
            else
                throw new System.InvalidOperationException("TryAddEngine: Adding Engines is Only Allowed During Initialization");
        }
        //
        //
        // ********************************************************************
        // ****                 MarketInstrumentInitialized()              ****
        // ********************************************************************
        /// <summary>
        /// This function is called only once, just prior to the strategy being launched.
        /// All initial data and instruments have been provided at this point.
        /// </summary>
        /// <param name="marketBook"></param>
        /// <param name="isForceUpdate"></param>
        public void MarketInstrumentInitialized(Book marketBook)
        {
            foreach(Engine engine in m_Engines)
                engine.MarketInstrumentInitialized(marketBook);

        }// MarketInstrumentInitialized()
        //
        //
        //
        // ****************************************************************
        // ****                 MarketInstrumentChanged()              ****
        // ****************************************************************
        /// <summary>
        /// The main pricing update.
        /// </summary>
        /// <param name="marketBook"></param>
        /// <param name="isForceUpdate"></param>
        public void MarketInstrumentChanged(Book marketBook, InstrumentChangeArgs eventArgs, bool isForceUpdate)
        {
            m_PricingEngine.MarketInstrumentChanged(marketBook, eventArgs);

            /*
            if (m_OrderEngine != null && m_OrderEngine.m_OrderInstrument != null)
            {
                m_OrderEngine.UpdateOrders(isForceUpdate);	// TODO: we need not check this if pricing model is not changed recently.
                if (isForceUpdate)
                    m_OrderEngine.SynchronizeRunningState();
            }
            */

        }// MarketChange()
        //
        //
        // **************************************************************
        // ****                 Request Repricing()                  ****
        // **************************************************************
        public void RequestRepricing(bool isForced)
        {
            this.m_PricingEngine.IsUpdateRequired = true;          
            StrategyHub.RequestStrategyUpdate(isForced);
        }//RequestRepricing()
        //
        //
        //
        // *************************************************************
        // ****             GetInstrumentSubscriptions()            ****
        // *************************************************************
        /// <summary>
        /// Called after initialization.  Gets all the instruments get mkt data for.
        /// </summary>
        /// <returns></returns>
        public List<InstrumentName> GetInstrumentSubscriptions()
        {
            List<InstrumentName> instrNames = new List<InstrumentName>();
            if (m_PricingEngine != null && m_PricingEngine is Engines.PricingEngine)
            {
                Engines.PricingEngine eng = (Engines.PricingEngine)m_PricingEngine;
                foreach (PriceLeg leg in eng.m_Legs)            // TODO: this should be part of IPricingEngine
                    instrNames.Add(leg.InstrumentName);
            }
            return instrNames;
        }//GetInstrumentSubscriptions()
        //
        //
        //
        // *************************************************************
        // ****             Add Spontaneous Events()                ****
        // *************************************************************
        /// <summary>
        /// Periodically, this method should be called by the strategy hun thread.
        /// Herein, engines are allowed to generate events and add them to the event list
        /// to be sent to all Engine Event subscribers.  These events are called 
        /// "spontaneous" in that the engines have created them, in contrast to the usual 
        /// case of responding to an outside request.
        /// Called by internal hub thread.
        /// </summary>
        /// <returns></returns>
        public void AddSpontaneousEngineEvents(List<EngineEventArgs> eventList)
        {
            foreach (Engine engine in m_Engines)
                engine.AddSpontaneousEngineEvents(eventList);

        }//AddSpontaneousEngineEvents()
        //
        //
        // *************************************************
        // ****             Try To Stop()               ****
        // *************************************************
        /// <summary>
        /// Called when the strategy hub is in the Stopping state, 
        /// to allow strategies a chance to finalize themselves and 
        /// shutdown nicely.  
        /// This method may be called repeatedly even after the strategy 
        /// has shutdown, so we need a bool flag to bypass calling our engines
        /// repeatedly.
        /// </summary>
        /// <returns>true after strategy has completed all shutdown.</returns>
        public bool TryToStop()
        {
            if (m_IsStopped)                                // Check whether we are already stopped.
                return true;
            foreach (IEngine ieng in m_Engines)             // Query each engine to stop.
            {
                //if (! ieng.TryToStop() )                  // This allows an engine to delay shutdowns.
                //return false;
                if (ieng is IOrderEngine)
                    ((IOrderEngine)ieng).CancelAllOrders(); // try and cancel all orders on shutdown
            }
            // Exit
            m_IsStopped = true;
            return true;                                    // If we make it here, all engines agree that we can stop.
        }// TryToStop()
        //
        //
        // *********************************************
        // ****             Filled()                ****
        // *********************************************
        public bool Filled(FillEventArgs eventArg)
        {
            bool isRepricingRequired = false;
            if (m_OrderEngine != null)
            {
                // Update Fills table with leg fill.
                Fill newFill = null; //m_OrderEngine.Filled(eventArg);  // this is being depercated, in the meantime i am simply commenting out.
                
                // Prepare to write to fill database.
                string attributeString = string.Empty;
                DateTime localTime = StrategyHub.GetLocalTime();
                UV.Lib.DatabaseReaderWriters.Queries.FillsQuery query = new Lib.DatabaseReaderWriters.Queries.FillsQuery();
                
                // Pass the synthetic fill to the pricing engine.                
                if (newFill != null)
                {
                    // Take snapshot prior to filling strategy.
                    attributeString = m_PricingEngine.GetFillAttributeString();     // get internal state of the pricing engine if it wants to send info with this fill.                     
                    query.AddItemToWrite(this.SqlId, -1, localTime, m_Services.User, attributeString, newFill.Qty, newFill.Price);    // here -1 means "strategy fill"                   

                    // Inform the pricing engine for strategy fill.
                    m_PricingEngine.Filled(newFill);                                // pass the fill to the pricing engine.                    
                    m_PricingEngine.IsUpdateRequired = true;                        // mark as needing an update
                    isRepricingRequired = true;                                     // remember to call the strategy update method
                    
                    // Take snapshot of the state after filling strategy.
                    attributeString = m_PricingEngine.GetFillAttributeString();     // get internal state of the pricing engine if it wants to send info with this fill.                    
                    query.AddItemToWrite(this.SqlId, -1, localTime, m_Services.User, attributeString, newFill.Qty, newFill.Price);    // here -1 means "strategy fill"
                    attributeString = string.Empty;                             // if we have written the attribute, lets not write it again.                    
                }
                // Write leg fill to database.                
                int instrumentSqlId = 0;        // TODO: From eventArg.InstrumentName somehow determine the SQLID for this instrument.
                query.AddItemToWrite(SqlId, instrumentSqlId, localTime, m_Services.User, attributeString, eventArg.Fill.Qty, eventArg.Fill.Price);
                StrategyHub.RequestDatabaseWrite(query);

            }
            // Exit
            return isRepricingRequired;
        }// Filled()
        //
        //
        //
        //
        #endregion // Run-time public methods


        #region Public Override Methods 
        //
        //
        // *************************************
        // ****         ToString()          ****
        // *************************************
        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
            s.AppendFormat("#{0} {1}",this.ID,this.Name);
            foreach (IEngine iEngine in m_Engines)
                s.AppendFormat("[{0}]", iEngine.ToString());
            return s.ToString();
        }//ToString()
        //
        //
        //
        #endregion//Public Methods


        #region no Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        #endregion//Private Methods


        #region no Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        #endregion//Event Handlers


        #region IEngineContainer implementation
        // *************************************************
        // ****             IEngine Container           ****
        // *************************************************
        //
        public int EngineContainerID
        {
            get { return this.ID; }
        }

        string IEngineContainer.EngineContainerName
        {
            get { return this.Name; }
        }
        public List<IEngine> GetEngines()
        {
            List<IEngine> engines = new List<IEngine>();
            engines.AddRange(m_Engines);
            return engines;
        }
        public string GetGuiTemplates()     // TODO: Add to IEngineContainer
        {
            return Stringifiable.Stringify(m_EngineContainerGui);
        }
        Lib.FrontEnds.Clusters.Cluster IEngineContainer.GetCluster()
        {
            throw new NotImplementedException();
        }
        Lib.FrontEnds.Clusters.ClusterConfiguration IEngineContainer.ClusterConfiguation
        {
            get { throw new NotImplementedException(); }
        }
        bool IEngineContainer.ProcessEngineEvent(EventArgs e)
        {
            throw new NotImplementedException();
        }
        //
        //
        //
        //
        //
        // ****                 Process Engine Event                ****
        //
        /// <summary>
        /// Processes EngineEvents received from outsiders.  Usually, for a Strategy, this is 
        /// a user-GUI generated request to change a parameter.
        /// </summary>
        /// <param name="e"> EngineEventArgs containing request. </param>
        /// <returns></returns>
        public bool ProcessEngineEvent(EventArgs eventArgs)
        {
            bool isUpdateRequired = false;          // 
            if (eventArgs.GetType() == typeof(EngineEventArgs))
            {
                EngineEventArgs e = (EngineEventArgs)eventArgs;
                int engineID = e.EngineID;                                      // engine that will receive request.
                if (engineID < 0)
                {   // Event is not for any engine.  
                    StrategyHub.Log.NewEntry(LogLevel.Error, "Strategy.ProcessEngineEvent: engineID is invalid.");
                }
                else if (engineID < m_Engines.Count)
                {   // This is for a specific engine.
                    m_Engines[engineID].ProcessEvent(e);                     // pass event along to the engine.
                    isUpdateRequired = m_Engines[engineID].IsUpdateRequired; // mark the engine for updating.
                }
                else
                {   // engineID too large - error.
                    StrategyHub.Log.NewEntry(LogLevel.Error, "Strategy.ProcessEngineEvent: engineID is invalid.");
                }
            }
            /*else if (eventArgs.GetType() == typeof(ClusterEventArgs))
            {
                ClusterEventArgs e = (ClusterEventArgs)eventArgs;
                int engineID = e.ClusterEngineID;
                if (engineID < 0)
                    m_StrategyHub.Log.NewEntry(LogLevel.Error, "Strategy.ProcessEngineEvent: engineID is invalid.");
                else if (engineID < m_Engines.Count)
                {   // This is for a specific engine.
                    m_Engines[engineID].ProcessEvent(e);                     // pass event along to the engine.
                    isUpdateRequired = m_Engines[engineID].IsUpdateRequired; // mark the engine for updating.
                }
                else
                    m_StrategyHub.Log.NewEntry(LogLevel.Error, "Strategy.ProcessEngineEvent: engineID is invalid.");
            }*/
            return isUpdateRequired;
        }//ProcessEngineEvent()
        //
        //
        //
        #endregion // IEngineContainer


        #region IStringifiable implementation
        // *************************************************
        // ****             IStringifiable              ****
        // *************************************************
        /// <summary>
        /// These are often called before everything is connected, so 
        /// you can assume that there is a "StrategyHub" or Log here yet.
        /// </summary>
        /// <returns></returns>
        string IStringifiable.GetAttributes()
        {
            StringBuilder s = new StringBuilder();
            s.AppendFormat("Name={0} ", this.Name);
            return s.ToString();
        }
        List<IStringifiable> IStringifiable.GetElements()
        {
            List<IStringifiable> elems = new List<IStringifiable>();
            foreach (Engine eng in m_Engines)
                if (eng is IStringifiable)
                    elems.Add( (IStringifiable) eng);
            return elems;   
        }
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
            //int n;
            foreach (KeyValuePair<string,string> keyValue in attributes)
            {
                if (keyValue.Key == "Name")
                    this.Name = keyValue.Value;                
            }         
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {
            if (subElement is Engine)
            {   // Add new Engine
                Engine engine = (Engine) subElement;
                TryAddEngine(engine, null);
                //int nId = m_Engines.Count;
                //m_Engines.Add(engine);
            }         
        }
        #endregion // IStringifiable

    }//end class
}
