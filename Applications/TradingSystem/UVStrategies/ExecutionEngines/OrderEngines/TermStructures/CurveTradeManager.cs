using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UV.Strategies.ExecutionEngines.OrderEngines.TermStructures
{

    using UV.Lib.Engines;
    using UV.Lib.IO.Xml;
    using UV.Lib.Hubs;
    using UV.Lib.Fills;
    
    using UV.Strategies.ExecutionHubs;
    using UV.Strategies.ExecutionEngines;
    using UV.Strategies.ExecutionHubs.ExecutionContainers;

    /// <summary>
    /// This is currently a place holder for the "manager" object that will sit on top off all the curve traders to provide matching across the threads
    /// that are executing as well as helping with messaging across them.
    /// 
    /// Notes:
    ///     Current design plan is to have this TradeManager have a thread with a tt dispatched attached since all OrderEngine use the listener model.
    ///     However, the thread will probably not do anything with TT.  This manager will create all of the CurveTrader objects below it, registering each container
    ///     with the above execution hub.  It will need to keep a list of these id's and remember how it broke apart each of these chunks to know who to call for which events.
    ///     also it will help to manage "internal matching" of all the threads that are unable to communicate with one another due to threading. 
    ///     
    ///     This object will be the only official order engine that the strategy hub directly has messaging with.  All messaging will enter into this object/thread container, be parsed 
    ///     and then pushed onto the queue's of execution units below it.  The container associated with this object must the multi threaded version which will allow mapping to occur
    ///     among the threads beneath this manager
    ///     
    ///     Creating engines and subengines requires a bit of though in making sure they are correctly set up with the a thread mapping
    /// </summary>
    public class CurveTradeManager : Engine, IOrderEngine, IOrderEngineParameters, IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        // External Services
        public ExecutionListener m_ExecutionListener = null;
        public ThreadContainer m_ExecutionContainer = null;
        public Hub m_Hub = null;
        public LogHub m_Log = null;

        // Collections 
        public List<CurveTrader> m_CurveTraders = new List<CurveTrader>();      // collection of order engines beneath this "manager"
        private List<int> m_CurveTraderPendingLaunch = new List<int>();         // collection of id's of curve traders waiting to be launched    


        // Engine Variables
        private string m_DefaultAccount;                                    // default account to send orders
        
        private int m_DripQty = 1;                                          // default order size to show the market
        
        
        
        private double m_QuoteTickSize = 1;                                 // Minimum fluctuation for the Order Engine to consider a price change.

        private bool m_IsRiskCheckPassed;                                   // is engine currently in a okay to trade state based on risk checks
        private bool m_IsUserTradingEnabled;                                // is engine allowed to trade base on user preference
        private bool m_UseGTCQuoteOrders;                                   // should quote orders be GTC
        private bool m_UseGTCHedgerOders;                                   // should hedge orders be GTC
        
        

        // Books
        public FillBook m_SyntheticSpreadFillBook;                          // Fill book just for the spread fills

        // ID's
        private static int m_LastContainerId = -1;                          // static counter to create unique execution container id's that I will control
        #endregion// members
        
        #region Constructors
        // *****************************************************************
        // ****               Constructors And Initialization           ****
        // *****************************************************************
        //
        public CurveTradeManager()
            : base()
        {
        }
        //
        //
        /// <summary>
        /// Called by the execution hub thread, not the execution listener!
        /// </summary>
        /// <param name="myEngineHub"></param>
        /// <param name="engineContainer"></param>
        /// <param name="engineID"></param>
        /// <param name="setupGui"></param>
        protected override void SetupInitialize(Lib.Engines.IEngineHub myEngineHub, Lib.Engines.IEngineContainer engineContainer, int engineID, bool setupGui)
        {
            base.SetupInitialize(myEngineHub, engineContainer, engineID, setupGui);
            m_Hub = (Hub)myEngineHub;
            ExecutionHub executionHub = (ExecutionHub)myEngineHub;
            m_ExecutionContainer = (ThreadContainer)engineContainer;           // this must be a multithreaded container for this execution to work
            m_Log = m_Hub.Log;

            //
            // Set up engines translation with manager container
            //
            MultiThreadContainer multiThreadContainer = (MultiThreadContainer)m_ExecutionContainer;
            multiThreadContainer.TryAddEngineIdToManagingContainer(multiThreadContainer, this.EngineID);


            //
            // Create new execution containers to be managed on seperate threads for each curve trader
            //

            foreach(CurveTrader curveTrader in m_CurveTraders)
            {
                ThreadContainer container = new ThreadContainer();
                container.EngineContainerID = System.Threading.Interlocked.Increment(ref m_LastContainerId);    // increment our id and save our engine id
                container.EngineContainerName = string.Format("CurveTrader{0}", container.EngineContainerID);   // Add basic name
                container.RemoteEngineHub = executionHub;                                                       // pointer to the local hub, a remote strategy will not control this
                container.TotalEngineCount = 1;                                                                 // this is only the number of "super engines" not sub engines!

                m_ExecutionContainer.TryAddEngine(curveTrader);                                                 // add this engine to both containers!
                container.TryAddEngine(curveTrader);                                                            // need to be careful here with "double" launching
                curveTrader.m_CurveTradeManager = this;                                                         // give pointer to manager to each trader...careful with threads!
                m_CurveTraderPendingLaunch.Add(curveTrader.EngineID);                                           // engine id's we are waiting on to launch properly

                multiThreadContainer.TryAddEngineIdToManagingContainer(container, curveTrader.EngineID);

                // now that all engines are added, sent off the container to the hub to be finish processing.
                EngineEventArgs engineEventArgs = new EngineEventArgs();
                engineEventArgs.MsgType = EngineEventArgs.EventType.AddContainer;
                engineEventArgs.Status = EngineEventArgs.EventStatus.Request;
                engineEventArgs.EngineContainerID = container.EngineContainerID;
                engineEventArgs.DataObjectList = new List<object>();
                engineEventArgs.DataObjectList.Add("ExecutionHub");
                engineEventArgs.DataObjectList.Add(container);

                m_Hub.HubEventEnqueue(engineEventArgs);
            }
            
            //foreach (CurveLegList curveLegs in m_CurveLegLists)
            //{   // call hub to create the execution container for each of these sub stratgies, once it is created and done, assign the list
            //    // to the object, the hub will call the set up functions afterwards
            //    ThreadContainer container = new ThreadContainer();
            //    container.EngineContainerID = System.Threading.Interlocked.Increment(ref m_LastContainerId);     // increment our id and save our engine id
            //    container.EngineContainerName = string.Format("CurveTrader{0}", container.EngineContainerID);   // Add basic name
            //    container.RemoteEngineHub = executionHub;                                                       // pointer to the local hub, a remote strategy will not control this
            //    container.TotalEngineCount = 2;                                                                 // this is only the number of "super engines" not sub engines!
            //    // Add all Engines to container here 

            //    CurveTrader curveTrader = new CurveTrader();                                                    // create new curve trader execution unit
            //    curveTrader.Id = container.EngineContainerID;                                                   // use same id as container, (they are 1:1)
            //    curveTrader.EngineID = 0;                                                                       // I am the first engine, so assign me 0

            //    m_CurveTraderPendingLaunch.Add(curveTrader.Id);                                                 // save id to make sure we have everything before we start
            //    m_CurveTraders.Add(curveTrader);                                                                // keep pointer to curve trader, carefuly using it due to threading!
                
            //    curveTrader.m_CurveLegs = curveLegs;                                                            // assign the needed legs to the new unit
            //    curveTrader.m_CurveTradeManager = this;                                                         // give curve trader pointer so he can call back up to me.
                
            //    // TODO: create any other engines here?
            //    // TODO: subscribe to these engines events? when they are launched we want to launch ourselves, etc
            //    if(!container.TryAddEngine((Engine)curveTrader))
            //    {
            //        m_Log.NewEntry(LogLevel.Error, "CurveTradeManager: Unable to create execution unit!");  
            //        continue;
            //    }

            //    m_ExecutionContainer.TryAddEngine(curveLegs);
                
            //    // now that all engines are added, sent off the container to the hub to be finish processing.
            //    EngineEventArgs engineEventArgs = new EngineEventArgs();
            //    engineEventArgs.MsgType = EngineEventArgs.EventType.AddContainer;
            //    engineEventArgs.Status = EngineEventArgs.EventStatus.Request;
            //    engineEventArgs.EngineContainerID = container.EngineContainerID;
            //    engineEventArgs.DataObjectList = new List<object>();
            //    engineEventArgs.DataObjectList.Add("ExecutionHub");
            //    engineEventArgs.DataObjectList.Add(container);

            //    m_Hub.HubEventEnqueue(engineEventArgs);
                
            //}
            
        }
        //
        //
        /// <summary>
        /// Called by our internal listener thread
        /// </summary>
        /// <param name="myEngineHub"></param>
        /// <param name="engineContainer"></param>
        public override void SetupBegin(Lib.Engines.IEngineHub myEngineHub, Lib.Engines.IEngineContainer engineContainer)
        {
            base.SetupBegin(myEngineHub, engineContainer);                      // call base class
            
            //
            // Subscribe to events
            //
            //m_ExecutionListener.InstrumentFound += new EventHandler(ExecutionListener_InstrumentsFound);

            //
            // Find all needed pointers to engines
            //
            //foreach (IEngine iEng in engineContainer.GetEngines())      // find the user created risk manager
            //    if (iEng is RiskManagerSpreader)
            //        m_RiskManager = (RiskManagerSpreader)iEng;
            //    else if (iEng is HedgeManager)
            //        m_HedgeManager = (HedgeManager)iEng;


            //if (m_RiskManager == null)
            //    throw new NotImplementedException("All Strategies Must Have a Risk Manager, Please Add One To Your User Config - Must Be UV.Execution.Risk.RiskManagerQuoter type");
            //if (m_HedgeManager == null)
            //    throw new NotImplementedException("All Quoters Must Have a Hedge Manager, Please Add One To Your User Config - Must Be UV.Execution.HedgeManager type");

            //
            // Set up all legs 
            //
            //StringBuilder spreadName = new StringBuilder();                                             // synthetic spread name for reporting spread fills
            //for (int leg = 0; leg < m_SpreaderLegs.Count; ++leg)
            //{
            //    SpreaderLeg spreadLeg = m_SpreaderLegs[leg];
            //    spreadLeg.LeanablePriceChanged += new EventHandler(Instrument_LeanablePriceChanged);    // subscribe to pricing updates.
            //    spreadLeg.MarketStateChanged += new EventHandler(Instrument_MarketStateChanged);        // subscribe to market state changes.
            //    spreadLeg.ParameterChanged += new EventHandler(Instrument_ParameterChanged);            // param changes need to make the quoter update.

            //    spreadLeg.UpdateDripQty(1);
            //    m_Instruments.Add(spreadLeg.m_PriceLeg.InstrumentName);

            //    FillBook fillBook = new FillBook(spreadLeg.m_PriceLeg.InstrumentName.ToString(), 0);    // Multiplier will need to be set once we have instrument details!
            //    m_LegFillBooks[leg] = fillBook;                                                         // add book to array to pass to spread fill generator

            //    m_IsQuotingLeg[leg] = spreadLeg.m_QuotingEnabled;
            //    m_PendingQuoteOrders[leg] = new Order[2];                                               // 2-sides of the mkt
            //    m_QuotingLegPrices[leg] = new double[2];
            //    m_IsQuotingLegPriceOffMarket[leg] = new bool[2];
            //    m_IsLegLeanable[leg] = new bool[2];

            //    m_LegRatios[leg] = spreadLeg.m_PriceLeg.Weight;                                         // add ratio and multipliers to array to pass to spread fill generator
            //    m_LegPriceMultipliers[leg] = spreadLeg.m_PriceLeg.PriceMultiplier;

            //    m_QuoteFillCount[leg] = new int[2];                                                     // Quote Fill counts to start are 0 for bid side  
            //    m_QuotePartialCount[leg] = new int[2];                                                  // Quote partial counts to start are 0 for bid side  

            //    if (leg != (m_SpreaderLegs.Count - 1))  //append spread name
            //        spreadName.AppendFormat("{0}.", spreadLeg.m_PriceLeg.InstrumentName);
            //    else
            //        spreadName.Append(spreadLeg.m_PriceLeg.InstrumentName);
            //}
            //m_SyntheticSpreadFillBook = new FillBook(spreadName.ToString(), 0);                         // create synthetic fill book for our synthetics fills. no mult.
            //Product syntheticProduct = new Product("SyntheticExch", spreadName.ToString(), ProductTypes.Synthetic);
            //InstrumentName syntheticInstrName = new InstrumentName(syntheticProduct, string.Empty);
            //m_SpreadFillGenerator = new ExecutionEngines.SpreaderFills.SpreaderFillGenerator(           // instantiate the spread fill generator passing an array of legRatios, and an array of books to it.
            //    syntheticInstrName, m_LegRatios, m_LegPriceMultipliers, m_LegFillBooks);
            //m_SpreadFillGenerator.SyntheticSpreadFilled += new EventHandler(SpreadFillGenerator_SyntheticSpreadFilled);
        }
        //
        //
        //
        /// <summary>
        /// Called by our internal listener thread
        /// </summary>
        public override void SetupComplete()
        {
            base.SetupComplete();
            
            // if we create order books here we know risk is already listening for the events.
            //for (int leg = 0; leg < m_SpreaderLegs.Count; ++leg)
            //{
            //    SpreaderLeg spreadLeg = m_SpreaderLegs[leg];
            //    OrderBook orderBook = m_ExecutionListener.CreateOrderBook(spreadLeg.m_PriceLeg.InstrumentName, spreadLeg.DefaultAccount);
            //    m_OrderBooks.Add(leg, orderBook);
            //    orderBook.OrderFilled += new EventHandler(OrderBook_OrderFilled);
            //    orderBook.OrderStateChanged += new EventHandler(OrderBook_OrderStateChanged);
            //}

        }
        #endregion//Constructors

        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// Default order size to show the market
        /// </summary>
        public int DripQty
        {
            get {return m_DripQty;}
            set { m_DripQty = value; }
        }
        //
        //
        /// <summary>
        /// Minimum fluctuation for the Order Engine to consider a price change.
        /// </summary>
        public double QuoteTickSize
        {
            get { return m_QuoteTickSize; }
            set { m_QuoteTickSize = value; }
        }
        //
        //
        
        //
        /// <summary>
        /// Has risk verified that is order engine is okay to send orders.
        /// </summary>
        public bool IsRiskCheckPassed
        {
            get { return m_IsRiskCheckPassed; }
            set { m_IsRiskCheckPassed = value; }
        }
        //
        //
        /// <summary>
        /// User defined flag for allow order submission
        /// </summary>
        public bool IsUserTradingEnabled
        {
            get { return m_IsUserTradingEnabled; }
            set
            {
                m_IsUserTradingEnabled = value;
                //OnQuoterStateChange();
                if (m_Log != null)
                    m_Log.NewEntry(LogLevel.Error, "CurveTrader: Turning On Trading is Not Implemented Yet!");
            }
        }
        //
        //
        /// <summary>
        /// If no specific account is specifed elsewhere all orders will be sent with this 
        /// </summary>
        public string DefaultAccount
        {
            get { return m_DefaultAccount; }
            set
            {
                if (value.Length > 15) // 15 char limit!
                    value = value.Substring(0, 15);
                m_DefaultAccount = value;
            }
        }
        //
        //
        //
        // Property like methods...hidden from the gui (our gui automatically displays all properties to the user, these do not need to be displayed.
        //
        public ExecutionListener GetExecutionListener()
        {
            return m_ExecutionListener;
        }
        //
        //
        public void SetExecutionListener(ExecutionListener executionListener)
        {
            m_ExecutionListener = executionListener;
            m_ExecutionListener.ExecutionContainer = m_ExecutionContainer;      // assign my execution container to the listener 
        }
        //
        //
        public FillBook GetFillBook()
        {
            return m_SyntheticSpreadFillBook;
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
        public override void ProcessEvent(EventArgs e)
        {
            base.ProcessEvent(e);
            if(e is EngineEventArgs)
            {
                EngineEventArgs engineEventArg = (EngineEventArgs)e;
                if (engineEventArg.MsgType == EngineEventArgs.EventType.SyntheticOrder)
                {

                }
            }
            else if (e is CurveTraderEventArgs)
            {
                CurveTraderEventArgs curveTraderEventArg = (CurveTraderEventArgs)e;
                switch (curveTraderEventArg.MsgType)
                {
                    case CurveTraderEventArgs.EventType.Launched:
                        if(curveTraderEventArg.Status == CurveTraderEventArgs.EventStatus.Confirm)
                        {   // confirmation that a curve trader has been launched fully.
                            if(m_CurveTraderPendingLaunch.Contains(curveTraderEventArg.EngineId))
                            {   // we were waiting on this to launch
                                m_CurveTraderPendingLaunch.Remove(curveTraderEventArg.EngineId);
                                if(m_CurveTraderPendingLaunch.Count == 0)
                                {   // we have no more remaining curve traders, we can launch!
                                    m_ExecutionContainer.ConfirmStrategyLaunched();
                                }
                                else
                                {
                                    m_Log.NewEntry(LogLevel.Minor, "CurveTraderManager: Awaiting launch of {0} CurveTraders", m_CurveTraderPendingLaunch.Count);
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }
                
            }
        }
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

        #region Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        // *****************************************************************
        // ****           ExecutionListener_Initialized()               ****
        // *****************************************************************
        /// <summary>
        /// Called once dispatcher is attached to thread allowing us to finish our setup.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void ExecutionListener_Initialized(object sender, EventArgs eventArgs)
        {
            IEngineHub iEngHub = (IEngineHub)m_Hub;     // cast our generic hub to an Ieng hub

            //foreach (Engine eng in m_ExecutionContainer.EngineList.Values)  // call all engine's begin, now we are on the right thread!
            //    eng.SetupBegin(iEngHub, m_ExecutionContainer);
            //foreach (Engine eng in m_ExecutionContainer.EngineList.Values) // call all engine's complete
            //    eng.SetupComplete();
            this.SetupBegin(iEngHub, m_ExecutionContainer);
            this.SetupComplete();
        }
        //
        //
        // *****************************************************************
        // ****             ExecutionListener_Stopping()                ****
        // *****************************************************************
        /// <summary>
        /// Called by the execution listener thread when it has gotten the signal to try and 
        /// shutdown nicely.  As soon as we give back the thread in this function we can asusme 
        /// that we will not get any more callbacks.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void ExecutionListener_Stopping(object sender, EventArgs eventArgs)
        {
            m_Log.NewEntry(LogLevel.Major, "Quoter is shutting down, attemption to cancel all quoter orders");
            m_IsUserTradingEnabled = false; // stops new order from going out
            CancelAllOrders();              // just to be sure lets cancel all quote orders
        }
        //
        //
        //
        //
        #endregion//Event Handlers

        #region IOrderEngine Implementation
        // ***********************************************************************
        // ****                IOrderEngine Implementation                    ****
        // ***********************************************************************
        // *****************************************************************
        // ****                   CancelAllOrders()                     ****
        // *****************************************************************
        /// <summary>
        /// Called by Risk Manager or User who would like to cancel all exisiting orders.
        /// </summary>
        /// 
        public void CancelAllOrders()
        {
            if (m_Log != null)
                m_Log.NewEntry(LogLevel.Error, "CruveTrader: CancelAllOrders Not Implement Yet");
        }
        //
        //
        // *************************************************************
        // ****                        Start                        ****
        // *************************************************************
        //
        /// <summary>
        /// Start will be called by the execution hub thread.  At this point we
        /// will spin off our own thread that will be the only thread used from now on.
        /// </summary>
        public void Start()
        {
            m_ExecutionListener.Initialized += new EventHandler(ExecutionListener_Initialized);
            m_ExecutionListener.Stopping += new EventHandler(ExecutionListener_Stopping);
            System.Threading.Thread thread = new System.Threading.Thread(m_ExecutionListener.InitializeThread);
            thread.Name = this.GetType().Name.ToString();
            thread.Start();
        }
        //
        //
        // *************************************************************
        // ****                        Stop                         ****
        // *************************************************************
        /// <summary>
        /// Threadsafe call to being shutdown procedures.
        /// </summary>
        public void Stop()
        {
            m_ExecutionListener.StopThread();
        }
        #endregion

        #region IStringifiable Implementation
        // *****************************************************************
        // ****               IStringifiable Implementation             ****
        // *****************************************************************
        //
        //
        public override string GetAttributes()
        {
            StringBuilder s = new StringBuilder(base.GetAttributes());
            return s.ToString();
        }
        public override List<IStringifiable> GetElements()
        {
            List<IStringifiable> elements = base.GetElements();
            if (elements == null)
                elements = new List<IStringifiable>();
            foreach (CurveTrader curveTrader in m_CurveTraders)
                elements.Add(curveTrader);
            return elements;
        }
        public override void SetAttributes(Dictionary<string, string> attributes)
        {
            base.SetAttributes(attributes);
            bool isTrue;
            foreach (KeyValuePair<string, string> attr in attributes)
            {
                if (attr.Key.Equals("UseGTCQuoteOrders", StringComparison.OrdinalIgnoreCase) && bool.TryParse(attr.Value, out isTrue))
                    this.m_UseGTCQuoteOrders = isTrue;
                else if (attr.Key.Equals("UseGTCHedgeOrders", StringComparison.OrdinalIgnoreCase) && bool.TryParse(attr.Value, out isTrue))
                    this.m_UseGTCHedgerOders = isTrue;
            }
        }
        public override void AddSubElement(IStringifiable subElement)
        {
            base.AddSubElement(subElement);
            if(subElement is CurveTrader)
            {   // find all curve traders
                CurveTrader curveTrader = (CurveTrader)subElement;
                m_CurveTraders.Add(curveTrader);
            }
        }
        #endregion

    }//end class
}
