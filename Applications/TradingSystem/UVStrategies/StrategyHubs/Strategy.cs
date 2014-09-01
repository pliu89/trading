using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace UV.Strategies.StrategyHubs
{
    using UV.Lib.Application;
    using UV.Lib.Engines;       // IEngineContainer
    using UV.Lib.IO.Xml;        // IStringifiable
    using UV.Lib.Products;
    using UV.Lib.BookHubs;
    using UV.Lib.MarketHubs;
    using UV.Strategies.StrategyEngines;
    using UV.Strategies.StrategyEngines.QuoteEngines;
    using UV.Lib.Hubs;
    using UV.Lib.FrontEnds.GuiTemplates;

    using UV.Lib.Fills;
    using UV.Lib.OrderBooks;

    using UV.Strategies.ExecutionEngines.OrderEngines;

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
        private static int m_LastStrategyID = -1;                   // static counter to create unique strategy ids.
        public AppServices m_Services = AppServices.GetInstance();
        public string Name = string.Empty;                          // user-friendly strategy name
        public readonly int ID;                                     // unique id number for this strategy in this hub.
        public StrategyHub StrategyHub = null;

        // Database information
        public UV.Lib.DatabaseReaderWriters.Queries.StrategyQueryItem QueryItem = null; // query row from Strategy table associated with me.


        //
        // My internal controls
        //
        private bool m_IsStopped = false;                           // set during final shutdown. TODO: Strategies should have state enum!!!
        private bool m_IsInitializeComplete = false;
        private bool m_IsBeginComplete = false;
        public bool IsLaunched = false;


        //
        // My Engines
        //
        private List<Engine> m_Engines = new List<Engine>();
        private EngineContainerGui m_EngineContainerGui = null;
        public List<PricingEngine> PricingEngines = new List<PricingEngine>();
        //public PricingEngine m_PricingEngine = null;
        public ZGraphEngine m_GraphEngine = null;
        public TradeEngine m_OrderEngine = null;
        public QuoteEngine m_QuoteEngine = null;




        //
        // Strategy Hub lookup tables for this strategy.
        //
        //  Strategies can request MarketInstruments be added to the market book, and then either subscribe
        //  for their MarketInstrumentChanges or not.  These instruments are stored in one of the following lists.
        public List<InstrumentName> m_MarketInstrumentSubscriptions = new List<InstrumentName>();       // instruments we want market updates for
        public List<InstrumentName> m_MarketInstrumentNonSubscriptions = new List<InstrumentName>();    // instruments we want to exist in market book.
        public List<ITimerSubscriber> m_MyTimerSubscribers = new List<ITimerSubscriber>();


        #endregion// members


        #region Constructors & Engine Setup
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public Strategy()
        {
            this.ID = System.Threading.Interlocked.Increment(ref Strategy.m_LastStrategyID); // strategies get unique id#s.
        }
        //
        //
        //
        //
        // ****************************************
        // ****     Setup Initialize()         ****
        // ****************************************
        /// <summary>
        /// The Strategy has been created, and now we add its engines.
        /// When we call Engine.SetupInitialize() each engine can make NO assumptions
        /// about the other engines in a Strategy, only that the Strategy and its StrategyHub exists.
        /// Other Engines may or may not exist yet.
        /// What is allowed during the SetupInitialize() call is that Engines can spawn other Engines
        /// and add them to the *end* of the Strategy.m_Engines[] list freely.  (At the end so their
        /// SetupInitialize() methods get called automatically.)
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
                if (engine is PricingEngine)                            // using simple "if"s allows one engine to play multiple roles.
                    PricingEngines.Add((PricingEngine)engine);          //m_PricingEngine = (StrategyEngines.PricingEngine)engine;                                    
                if (engine is TradeEngine)
                    m_OrderEngine = (TradeEngine)engine;
                if (engine is ZGraphEngine)
                    m_GraphEngine = (ZGraphEngine)engine;
                if (engine is FauxQuote)
                    m_QuoteEngine = (FauxQuote) engine;
                id++;
            }//next engine id

            // Create missing basic engines
            if (m_OrderEngine == null)
            {
                m_OrderEngine = new TradeEngine();
                TryAddEngine(m_OrderEngine, myHub);
                m_OrderEngine.SetupInitialize(StrategyHub, this, -1);// Tell Engine his new Hub, and owner, and his new id# (which is already set in TryAddEngine()).
            }
            if (m_GraphEngine == null)
            {
                m_GraphEngine = new ZGraphEngine();
                TryAddEngine(m_GraphEngine, myHub);
                m_GraphEngine.SetupInitialize(StrategyHub, this, -1);
            }
            if (m_QuoteEngine == null)
            {
                QuoteEngine quoteEngine = new QuoteEngine();
                m_QuoteEngine = quoteEngine;
                TryAddEngine(quoteEngine, myHub);
                quoteEngine.SetupInitialize(StrategyHub, this, -1);
            }

            // Exit
            m_IsInitializeComplete = true;                               // Must be last line in this method.
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
        /// Strategy attempt to find resources and other dependences it requires to run.
        /// Engine are free to look for each other now.
        /// This method is called after ALL strategies have been created (and their engines), and 
        /// after SetupInitialize().  The purpose of this is that
        /// by this time, engine can be assured that there exists a complete list of all 
        /// strategies and engines available.  They can find specific engines (of their own (or other) strategies)
        /// that they need to link to, and make those initial linkages.
        /// 
        /// While not ideal, if an engine must add another one during this process it is still
        /// possible however it is much better to add them during SetupInitialize.
        /// To add a new engine now, one need call the Strategy.TryAddEngine(), which will sense that
        /// we are in the SetupBegin phase and do the right thing.
        /// TODO: 
        ///     1) To prevent multithread problems in future, Strategy must ask for pointer to 
        ///         another strategy, and this is monitored by hub, and pricing engines are 
        ///         ordered in tiers using this information.
        /// </summary>
        public void SetupBegin(IEngineHub myHub)
        {
            foreach (Engine engine in m_Engines)
                engine.SetupBegin(myHub, this);
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
        //
        //       
        #endregion//Constructors & Engine Setup


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
        // ****         IsReadyForSetupComplete         ****
        /// <summary>
        /// Strategies may take some time to complete all things
        /// required in SetupBegin() phase.  Once it's engines are
        /// ready the StrategyHub will call SetupComplete, then launch it.
        /// </summary>
        public bool IsReadyForSetupComplete
        {
            get
            {
                bool isReady = true;
                foreach (Engine engine in m_Engines)
                    if (engine is ExecutionRemote)
                    {
                        ExecutionRemote remoteEngine = (ExecutionRemote)engine;
                        if (remoteEngine.IsReadyForSetup == false)
                        {
                            isReady = false;
                            if (StrategyHub.Log != null)
                                StrategyHub.Log.NewEntry(LogLevel.Warning,
                                    "Strategy {0}: waiting for remote engine {1} to become ready for set up", this.Name, remoteEngine.EngineName);
                            break;
                        }
                    }
                return isReady;
            }
        }//IsReadyForSetupComplete
        //
        public bool IsPricingUpdateRequired
        {
            get
            {
                bool isUpdateRequired = false;
                int n = 0;
                while (isUpdateRequired == false && n < this.PricingEngines.Count)
                {
                    isUpdateRequired = PricingEngines[n].IsUpdateRequired || isUpdateRequired;
                    n++;
                }
                return isUpdateRequired;
            }
            set
            {
                foreach (PricingEngine pricingEngine in this.PricingEngines)
                    pricingEngine.IsUpdateRequired = value;
            }
        }
        //
        #endregion//Properties


        #region Public Startup Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        // 
        // *****************************************************
        // ****                 TryAddEngine()              ****
        // *****************************************************
        /// <summary>
        /// Called during SetupInitialize or  SetupBegin to add engines to containter.
        /// If called at any other time, an exception will be thrown!
        /// 
        /// It is best to call during SetupInitialize if possible if other engines might need to 
        /// find the added engine.
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="stratHub"></param>
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
            {   // we can add an engine but have to be careful to initialize properly
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
        }// TryAddEngine()
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
            foreach (PricingEngine pricingEngine in this.PricingEngines)
            {
                foreach (PriceLeg leg in pricingEngine.m_Legs)            // TODO: this should be part of IPricingEngine
                    instrNames.Add(leg.InstrumentName);
            }
            return instrNames;
        }//GetInstrumentSubscriptions()
        //
        //
        // ********************************************************************
        // ****                 MarketInstrumentInitialized()              ****
        // ********************************************************************
        /// <summary>
        /// This function is called only once, just after the strategy is launched, 
        /// but before it receives any market updates.
        /// All initial data and instruments have been provided at this point.
        /// </summary>
        /// <param name="marketBook"></param>
        public void MarketInstrumentInitialized(Book marketBook)
        {
            foreach (Engine engine in m_Engines)
                engine.MarketInstrumentInitialized(marketBook);

        }// MarketInstrumentInitialized()
        //
        //
        #endregion //Startup methods


        #region Public Run-Time methods
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
        /// <param name="eventArgs"></param>
        /// <param name="isForceUpdate"></param>
        public void MarketInstrumentChanged(Book marketBook, InstrumentChangeArgs eventArgs, bool isForceUpdate)
        {
            foreach (PricingEngine pricingEngine in this.PricingEngines)
                pricingEngine.MarketInstrumentChanged(marketBook, eventArgs);
            m_QuoteEngine.MarketInstrumentChanged(marketBook, eventArgs);
        }// MarketInstrumentChanged()
        //
        //
        // **************************************************************
        // ****                 Request Repricing()                  ****
        // **************************************************************
        //public void RequestRepricing(bool isForced)
        //{
        //    this.m_PricingEngine.IsUpdateRequired = true;
        //    StrategyHub.RequestStrategyUpdate(isForced);
        //}//RequestRepricing()
        //
        //
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
            bool canStopNow = true;
            if (m_OrderEngine != null)
                m_OrderEngine.CancelAllOrders();
            // Exit
            m_IsStopped = canStopNow;
            return m_IsStopped;
        }// TryToStop()
        //
        //
        //
        // *****************************************
        // ****             Quote()             ****
        // *****************************************
        /// <summary>
        /// Method to request that an order engine send orders at a particular price and qty.
        /// qty should be a signed int.
        /// 
        /// this overload allows for a quote reason to be sent along with the order.
        /// </summary>
        /// <param name="pricingEngine"></param>
        /// <param name="tradeSide"></param>
        /// <param name="price"></param>
        /// <param name="qty"></param>
        /// <param name="quoteReason">Explanation for logging purposes.</param>
        public void Quote(PricingEngine pricingEngine, int tradeSide, double price, int qty, QuoteReason quoteReason = QuoteReason.None)
        {
            if (!m_IsBeginComplete)
                StrategyHub.Log.NewEntry(LogLevel.Error, "Quote called before complete.");
            if (!IsLaunched)
                StrategyHub.Log.NewEntry(LogLevel.Error, "Quote called before launched.");

            /*
            if (m_OrderEngine != null)
            {
                int tradeId = m_OrderEngine.Quote(tradeSide, price, qty, quoteReason);
                // Save pricing model update snapshot
                m_ModelSnapshots[tradeId] = String.Format("{0} TradeReason={1}", m_PricingEngine.GetFillAttributeString(), quoteReason);   // store snapshot in tradeId we just adjusted.
            }
            */
            m_QuoteEngine.Quote(pricingEngine, tradeSide, price, qty, quoteReason);
        }//Quote()
        private Dictionary<int, string> m_ModelSnapshots = new Dictionary<int, string>();
        //
        //
        //
        //
        // *****************************************************
        // ****             CancelAllOrders()               ****
        // *****************************************************
        /// <summary>
        /// </summary>
        /// <param name="tradeSide">Trade side to cancel (or -1 means both sides).</param>
        public void CancelAllOrders(int tradeSide = -1)
        {
            if (m_OrderEngine != null)
                m_OrderEngine.CancelAllOrders(tradeSide);
        }// CancelAllOrders()
        //
        //
        // *****************************************************
        // ****             UpdateQuotes()                  ****
        // *****************************************************
        public void UpdateQuotes()
        {
            if (m_QuoteEngine != null)
                m_QuoteEngine.UpdateQuotes();
        }
        //
        // *****************************************************
        // ****         ProcessSyntheticOrder()             ****
        // *****************************************************
        /// <summary>
        /// Called by the strategy hub to process a synthetic order for strategy.
        /// </summary>
        /// <param name="syntheticOrder"></param>
        /// <returns></returns>
        public bool ProcessSyntheticOrder(SyntheticOrder syntheticOrder)
        {
            bool isUpdateRequired = false;
            if (m_OrderEngine == null)
            {   // This is an error.  We need this engine!
                return isUpdateRequired;
            }
            List<Fill> newFills = new List<Fill>();
            m_OrderEngine.ProcessSyntheticOrder(syntheticOrder, ref newFills);


            if (m_QuoteEngine != null)
            {
                isUpdateRequired = m_QuoteEngine.ProcessSyntheticOrder(syntheticOrder, newFills);
                if (isUpdateRequired)
                    m_QuoteEngine.UpdateQuotes(true);
            }

            

            /*
             * m_PricingEngine.Filled(syntheticOrder);                         // pass pricing engine raw event arg.
            // Inform the pricing engine if there is a new
            // synthetic fill.
            if (m_PricingEngine != null && newFills != null)
            {
                // Prepare to write to fill database. Pre-fill snapshot.
                string attributeString = string.Empty;
                DateTime localTime = StrategyHub.GetLocalTime();
                UV.Lib.DatabaseReaderWriters.Queries.FillsQuery query = new Lib.DatabaseReaderWriters.Queries.FillsQuery();
                if (m_ModelSnapshots.TryGetValue(syntheticOrder.OrderId, out attributeString) == false)
                    attributeString = m_PricingEngine.GetFillAttributeString();     // get internal state of the pricing engine if it wants to send info with this fill.                     
                query.AddItemToWrite(this.SqlId, -1, localTime, m_Services.User, attributeString, 0, 0);

                // Inform pricing engine        
                foreach (Fill f in newFills)
                {
                    m_PricingEngine.Filled(f);                // pass pricing engine synthetic fill.
                    attributeString = m_PricingEngine.GetFillAttributeString();
                    if(syntheticOrder.TradeReason != null && syntheticOrder.TradeReason != string.Empty)
                        attributeString = String.Format("{0} TradeReason={1}", attributeString, syntheticOrder.TradeReason);
                    query.AddItemToWrite(this.SqlId, -1, localTime, m_Services.User, attributeString, f.Qty, f.Price);    // here -1 means "strategy fill"
                }
                // Prepare a post-trade snapshot.
                StrategyHub.RequestDatabaseWrite(query);            // submit all the queries
            }
            */
            // Exit
            return isUpdateRequired;
        }// ProcessTradeEventArg
        //
        //
        // *****************************************************
        // ****         ProcessAlarmTriggered()             ****
        // *****************************************************
        /// <summary>
        /// Called by the strategy hub to keep alarms threadsafe before the engine 
        /// gets the call.  
        /// </summary>
        /// <param name="engineEventArgs"></param>
        public void ProcessAlarmTriggered(EngineEventArgs engineEventArgs)
        {
            foreach (PricingEngine pricingEngine in this.PricingEngines)
                pricingEngine.AlarmTriggered(engineEventArgs);

            UpdateQuotes();
        }
        //
        //
        // *************************************
        // ****         ToString()          ****
        // *************************************
        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
            s.AppendFormat("#{0} {1}", this.ID, this.Name);
            return s.ToString();
        }//ToString()
        //
        //
        // *************************************
        // ****   TryGetOrderEngineRemote   ****
        // *************************************
        /// <summary>
        /// Caller would like to hand in a list of engines and recieve back a pointer to the remote engine
        /// that controls a IOrderEngine implementation.
        /// </summary>
        /// <param name="iEngineList"></param>
        /// <param name="execRemote"></param>
        /// <returns></returns>
        public static bool TryGetOrderEngineRemote(List<IEngine> iEngineList, out ExecutionRemote execRemote)   // should this be static since the list of engines is already here?
        {
            execRemote = null;
            foreach (Engine eng in iEngineList)
            { // find engines we need to change params of.
                if (eng is ExecutionRemote)
                {
                    ExecutionRemote executionRemoteEng = (ExecutionRemote)eng;
                    string className = executionRemoteEng.GetClassName();               // find the class name of the remote engine we are controlling
                    Type remoteType = typeof(IOrderEngine).Assembly.GetType(className); // find the type of remote...this could possibly fail so check for null
                    if (remoteType != null && remoteType.GetInterfaces().Contains(typeof(IOrderEngine)))
                        execRemote = executionRemoteEng;
                }
            }
            return execRemote != null;
        }
        #endregion // Run-time public methods


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
        bool IEngineContainer.ProcessEvent(EventArgs e)
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
        /// <param name="eventArgs"> EngineEventArgs containing request. </param>
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
            if (isUpdateRequired)
                UpdateQuotes();
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
                    elems.Add((IStringifiable)eng);
            return elems;
        }
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
            //int n;
            foreach (KeyValuePair<string, string> keyValue in attributes)
            {
                if (keyValue.Key == "Name")
                    this.Name = keyValue.Value;
            }
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {
            if (subElement is Engine)
            {   // Add new Engine
                Engine engine = (Engine)subElement;
                TryAddEngine(engine, null);
                //int nId = m_Engines.Count;
                //m_Engines.Add(engine);
            }
        }
        #endregion // IStringifiable

    }//end class
}
