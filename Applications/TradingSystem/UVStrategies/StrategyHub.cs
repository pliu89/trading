using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace UV.Strategies
{
    using UV.Lib.Application;   // IService
    using UV.Lib.IO.Xml;        // IStringifiable
    using UV.Lib.Hubs;          // Hub
    using UV.Lib.Engines;
    using UV.Lib.Utilities;
    using UV.Lib.Products;

    using UV.Lib.BookHubs;
    using UV.Lib.MarketHubs;
    using UV.Lib.OrderBooks;
    using UV.Lib.Fills;

    using UV.Lib.DatabaseReaderWriters;
    using Queries = UV.Lib.DatabaseReaderWriters.Queries;


    /// <summary>
    /// Base class that all strategy hubs must inherit.  
    /// The class manages a collection of strategies, passes to them results from their subscriptions,
    /// and reprices when requested.
    /// A StrategyHub is instantiated for a single user/trader with multiple strategies - all of which
    /// are computed on a single thread.
    /// </summary>
    public class StrategyHub : Hub, IService, IStringifiable, IEngineHub
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        //
        // Strategy Hub service states
        //
        private AppServices m_AppServices = null;
        private ServiceStates m_ServiceState = ServiceStates.Unstarted;             // Current state
        private ServiceStates m_ServiceStatePending = ServiceStates.None;           // State we want to get to.

        // Services
        public UV.Lib.Utilities.Alarms.Alarm m_Alarm = null;

        // My internal work spaces
        private RequestFactory<RequestCode> m_Requests = new RequestFactory<RequestCode>(100); // Recycle Request Event Args here.
        private EventWaitQueueLite m_PendingRequests = null;


        //
        // Market subscriptions
        //
        public MarketHub m_Market = null;
        private List<int> m_StrategiesPricingRanked = new List<int>();                                      // order in which strategies are updated.
        private Dictionary<int, List<int>> m_InstrumentSubsciption = new Dictionary<int, List<int>>();		// mkt instr ID --> list of strategy IDs.
        private List<InstrumentName> m_InstrumentSubscriptionsOutstanding = new List<InstrumentName>();     // instruments we are waiting for market callbacks from.

        //
        //  Subscriptions
        //
        private Dictionary<string, List<int>> m_FillSubscription = new Dictionary<string, List<int>>();	    // order book key --> list of strategy IDs.
        private string m_FillSubscriptionFormat = "{0}#{1}";                                                // instrumentName#bookID
        private Dictionary<string, List<int>> m_MajorOrderStatusSubscription = new Dictionary<string, List<int>>();	    // order book key --> list of strategy IDs.
        private Dictionary<string, List<int>> m_OrderSubmissionSubscription = new Dictionary<string, List<int>>();	    // order book key --> list of strategy IDs.

        //
        // Periodic timer subscribers:  Engines, Strategies can subscribe for periodic updates.
        //
        private List<ITimerSubscriber> m_TimeSubscribers = new List<ITimerSubscriber>();

        //
        // Database subscriptions
        //
        private DatabaseReaderWriter m_DBReaderWriter = null;
        private Dictionary<int, List<int>> m_StrategyPendingQueries = new Dictionary<int, List<int>>();         // strategies and their outstanding queries.
        private Dictionary<int, EventHandler> m_PendingQueryCallback = new Dictionary<int, EventHandler>();     // delegate to be called when query is complete.   

        //
        // Stratgies
        //
        private string m_StrategyFileName = string.Empty;                                       // fileName "Strategies.txt"
        private List<int> m_StrategyGroupIds = null;                                            // space delim list of id#s
        private Dictionary<int, Strategy> m_Strategies = new Dictionary<int, Strategy>();		// Main list of strategies - in order they were loaded, indexed by their "ID" number.
        private List<Strategy> m_StrategiesPendingLaunch = new List<Strategy>();                // Created strategies that are waiting for all resources before final launch.
        private Dictionary<int, Strategy> m_StrategiesPendingSetupComplete = new Dictionary<int, Strategy>();// Created strategies that are pending initialization.

        //private List<Strategy> m_Strategies = new List<Strategy>();							// Main list of strategies - in order they were loaded, indexed by their "ID" number.
        //private List<IEngineContainer> m_EngineContainers = new List<IEngineContainer>();     // list of strategies as IEng.Containers.
        //private List<Strategy> m_StrategiesPricingRanked = new List<Strategy>();              // strategies ranked by pricing engine priority.
        //private List<Strategy> m_StrategiesFillsRanked = new List<Strategy>();				// strategies ranked by Fill engine priority.
        //private bool m_AutoLoadPreviousPosition = false;

        // State of this hub
        //public ServiceState m_State = ServiceState.Unstarted;
        //public event EventHandler StateChanged;
        //private int m_StickyStateCounter = 0;
        //private int m_StickyStateThreshold = 4;

        // number of GUIs  for the Strategy Hub
        //public int NumGUIs = -1;

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public StrategyHub()
            : base("StrategyHub", UV.Lib.Application.AppInfo.GetInstance().LogPath, false, LogLevel.ShowAllMessages)
        {
            base.m_WaitListenUpdatePeriod = 1000;                                           // msecs for periodic updates.
            m_AppServices = AppServices.GetInstance();

            m_PendingRequests = new EventWaitQueueLite(this.Log);
            m_PendingRequests.ResubmissionReady += new EventHandler(this.HubEventEnqueue);  // when pending requests ready, resubmit to hub.

            m_Alarm = new Lib.Utilities.Alarms.Alarm();
            m_Alarm.SetTimeDelegate( this.GetLocalTime );                                   // tell timer to use my clock.
            // Initialize database reader/writer.
            //m_DBReaderWriter = new DatabaseReaderWriter(DatabaseInfo.Create(DatabaseInfo.DatabaseLocation.apastor));
            //m_DBReaderWriter.QueryResponse += new EventHandler(this.HubEventEnqueue);
            //m_DBReaderWriter.Start();


            //m_TimeSeriesCollector = new BGTLib.Database.TimeSeries.Collector(mktDB, base.Log);
        }//constructor
        //       
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        public DateTime GetLocalTime()
        {
            if (m_Market != null)
                return m_Market.LocalTime;
            else
                return Log.GetTime();        
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
        // *********************************************
        // ****             Start()                 ****
        // *********************************************
        public override void Start()
        {
            if (m_ServiceState == ServiceStates.Unstarted)
            {   // We start thread here, and request a state change.
                this.HubEventEnqueue(m_Requests.Get(RequestCode.ServiceStateChange, ServiceStates.Started));
                base.Start();
            }
            //m_DBWriter.Start();
        }// Start()
        //
        //
        // *********************************************
        // ****         RequestStop()               ****
        // *********************************************
        public override void RequestStop()
        {
            Log.NewEntry(LogLevel.Minor, "RequestStop(): Stop requested.");
            this.HubEventEnqueue(m_Requests.Get(RequestCode.ServiceStateChange, ServiceStates.Stopped));
        }//Stop()
        //
        //
        //
        // *********************************************************
        // ****				Request Strategy Update()			****
        // *********************************************************
        /// <summary>
        /// An external thread may request the strategy hub updates and reprices all 
        /// its strategies.
        /// Strategies are automatically updated after fills, market changes, and when they
        /// receive Engine request events.
        /// </summary>
        /// <param name="isForced"></param>
        /// <returns></returns>
        public bool RequestStrategyUpdate(bool isForced)
        {
            // TODO: Why do we need forced and unforced repricing?
            Log.NewEntry(LogLevel.Minor, "RequestStrategyUpdate: ");
            return this.HubEventEnqueue(m_Requests.Get(RequestCode.StrategyReprice));
        }//RequestStrategyUpdate()
        //
        //
        //
        //
        // *********************************************************
        // ****             SubscribeToFills()                  ****
        // *********************************************************
        public void SubscribeToFills(Strategy strategy, OrderBook orderBook)
        {
            Log.NewEntry(LogLevel.Minor, "SubscribeToFills: {0} for Strategy {1}", orderBook, strategy.Name);
            // First create a quick way to lookup events that come from the 
            // order book, and see which strategy we want to send the event to
            InstrumentName instr = orderBook.Instrument;
            string key = string.Format(m_FillSubscriptionFormat, instr.FullName, orderBook.BookID);
            if (!m_FillSubscription.ContainsKey(key))
                m_FillSubscription.Add(key, new List<int>());
            m_FillSubscription[key].Add(strategy.ID);
            // Now tell the book to send events to my thread.
            orderBook.OrderFilled += new EventHandler(this.HubEventEnqueue);
        }//SubscribeToFills()
        //
        //
        //
        // *********************************************************
        // ****        SubscribeToMajorOrderStatusEvents()      ****
        // *********************************************************
        /// <summary>
        /// Called by a strategy who would like to susbsribe to updates for 
        /// orders in a specific book.
        /// </summary>
        /// <param name="strategy"></param>
        /// <param name="orderBook"></param>
        public void SubscribeToMajorOrderStatusEvents(Strategy strategy, OrderBook orderBook)
        {
            Log.NewEntry(LogLevel.Minor, "SubscribeToMajorOrderStatusEvents: {0} for Strategy {1}", orderBook, strategy.Name);
            InstrumentName instr = orderBook.Instrument;
            string key = string.Format(m_FillSubscriptionFormat, instr.FullName, orderBook.BookID);
            if (!m_MajorOrderStatusSubscription.ContainsKey(key))
                m_MajorOrderStatusSubscription.Add(key, new List<int>());
            m_MajorOrderStatusSubscription[key].Add(strategy.ID);
            // Now tell the book to send events to my thread.
            orderBook.OrderStateChanged += new EventHandler(this.HubEventEnqueue);
        }
        //
        //
        // *********************************************************
        // ****        SubscribeToMajorOrderStatusEvents()      ****
        // *********************************************************
        /// <summary>
        /// Method allows a strategy to subscribe to oder submission confirmations
        /// for a specific order book.
        /// </summary>
        /// <param name="strategy"></param>
        /// <param name="orderBook"></param>
        public void SubscribeToOrderSubmitted(Strategy strategy, OrderBook orderBook)
        {
            Log.NewEntry(LogLevel.Minor, "SubscribeToOrderSubmitted: {0} for Strategy {1}", orderBook, strategy.Name);
            InstrumentName instr = orderBook.Instrument;
            string key = string.Format(m_FillSubscriptionFormat, instr.FullName, orderBook.BookID);
            if (!m_OrderSubmissionSubscription.ContainsKey(key))
                m_OrderSubmissionSubscription.Add(key, new List<int>());
            m_OrderSubmissionSubscription[key].Add(strategy.ID);
            orderBook.OrderSubmitted += new EventHandler(this.HubEventEnqueue);  // Now tell the book to send events to my thread.
        }
        //
        // *************************************************************
        // ****                 RequestHistoricData()               ****
        // *************************************************************
        /// <summary>
        /// Manages the requests from Strategies (or their engines) for historic data.
        /// This provides a user-friendly wrapper for the more general call to RequestQuery().
        /// </summary>
        /// <param name="instrument"></param>
        /// <param name="startUTC"></param>
        /// <param name="endUTC"></param>
        /// <param name="callBack"></param>
        /// <param name="strategy"></param>
        /// <returns></returns>
        public bool RequestHistoricData(InstrumentName instrument, DateTime startUTC, DateTime endUTC, EventHandler callBack, Strategy strategy)
        {
            // Create the query we want to submit.
            Queries.MarketDataQuery instrQ = new Queries.MarketDataQuery();
            instrQ.InstrumentName = instrument;
            instrQ.MaxRows = 30000;          // Debugging purposes!
            instrQ.StartDate = startUTC;
            instrQ.EndDate = endUTC;
            return RequestQuery(instrQ, callBack, strategy);
        }//RequestHistoricData()
        //
        //
        //
        //
        // *********************************************
        // ****             RequestQuery()          ****
        // *********************************************
        /// <summary>
        /// The general method that allows any strategy to submit any query.
        /// </summary>
        /// <param name="query">Query to be submitted.</param>
        /// <param name="callback">Method to receive completed query.</param>
        /// <param name="strategy">Strategy making request.</param>
        /// <returns></returns>
        public bool RequestQuery( Queries.QueryBase query , EventHandler callback, Strategy strategy)
        {
            // Validate
            if (m_DBReaderWriter == null)
                return false;
            Log.NewEntry(LogLevel.Minor, "RequestQuery: Strategy {1} -> Query {0}", query, strategy.Name);

            if (m_PendingQueryCallback.ContainsKey(query.QueryID))
            {   // this should never happen unless QueryBase is corrupted!
                // This is here to ensure that we don't crash in case the impossible happens.
                Log.NewEntry(LogLevel.Error, "RequestQuery: Non-unique query ID!");
                return false;
            }

            //
            // Store info who has requested this query.
            //
            // Note that queries received during pre-launch phase, we guarentee the strategy
            // that he will not be launched until all of his queries are completed.
            // This list is stored in StrategyPendingQueries dictionary keyed by Strategy Id.
            if ( ! m_StrategyPendingQueries.ContainsKey(strategy.ID) )
                m_StrategyPendingQueries.Add(strategy.ID,new List<int>());
            m_StrategyPendingQueries[strategy.ID].Add(query.QueryID);              // Remember all queries owned by this strategy.
            m_PendingQueryCallback.Add(query.QueryID, callback);                   // Store delegate to call.
                                                                                    
            //
            // Submit request.
            //
            m_DBReaderWriter.SubmitAsync(query);

            return true;
        }//RequestHistoricData()
        //
        //
        // *************************************************************
        // ****                 RequestDatabaseWrite()               ****
        // *************************************************************
        /// <summary>
        /// This allows strategies to write to database.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public bool RequestDatabaseWrite( Queries.QueryBase query)
        {
            if (query.IsRead)
            {
                Log.NewEntry(LogLevel.Error, "RequestDatabaseWrite: Failed.  Read is not implemented yet.");
                return false;
            }
            if (m_AppServices.User.RunType == RunType.Debug)
            {   // We will not write to database in debug mode.
                Log.NewEntry(LogLevel.Major, "RequestDatabaseWrite: Will not write query in {0} mode. Query = {1}",m_AppServices.User.RunType,query);
                return true;                // pretend all is ok.
            }
            if ( m_DBReaderWriter != null)
                m_DBReaderWriter.SubmitAsync(query);
            return true;
        }
        //
        //
        // *************************************************************
        // ****         SubscribeToMarketInstruments()              ****
        // *************************************************************
        public bool SubscribeToMarketInstruments(List<InstrumentName> instrumentList, Strategy strategy)
        {
            // Validate
            if (m_Market == null)
                return false;
            //if (strategy.m_MarketInstrumentList.Count > 0)
            //{   // Only this method changes this list in the strategy.
            //    // So I use this to know if the Strategy has repeatedly called this method.  He is only allowed to call once now.
            //    Log.NewEntry(LogLevel.Warning, "SubscribeToMarketInstruments: Called multiple times for Strategy {0}. Ignoring!", strategy.Name);
            //    return false;
            //}
            Log.BeginEntry(LogLevel.Minor, "SubscribeToMarketInstruments: Subscribing to {0} instruments by Strategy {1}.", instrumentList.Count, strategy.Name);

            // Request markets.
            strategy.m_MarketInstrumentList.AddRange(instrumentList);           // Store the markets he requested
            int nInstrSubscriptionsRequested = 0;
            int instrumentID;
            foreach (InstrumentName instrumentName in instrumentList)
                if (m_Market.TryLookupInstrumentID(instrumentName, out instrumentID) == false && m_InstrumentSubscriptionsOutstanding.Contains(instrumentName) == false)
                {   // This is an instrument we don't have, and not already waiting on. 
                    m_Market.RequestInstrumentSubscription(instrumentName);     // Request market updates from instrument. 
                    m_InstrumentSubscriptionsOutstanding.Add(instrumentName);   // add to waiting list.
                    nInstrSubscriptionsRequested++;
                }
            // Write log report
            if (nInstrSubscriptionsRequested > 0)
            {
                Log.AppendEntry(" Requesting {0} *new* instruments:", nInstrSubscriptionsRequested);
                while (nInstrSubscriptionsRequested < m_InstrumentSubscriptionsOutstanding.Count)
                {
                    Log.AppendEntry(" {0}", m_InstrumentSubscriptionsOutstanding[nInstrSubscriptionsRequested]);
                    nInstrSubscriptionsRequested++;
                }

            }
            Log.EndEntry();

            return true;
        }//SubscribeToMarketInstruments()
        //
        //
        //
        // *****************************************************
        // ****             SubscribeToTimer()              ****
        // *****************************************************
        public bool SubscribeToTimer(Strategy strategy, ITimerSubscriber subscriber)
        {
            Log.NewEntry(LogLevel.Warning, "SubscribeToTimer: Strategy {0} subscribing.", strategy.Name);
            strategy.m_MyTimerSubscribers.Add(subscriber);                      // Remember that this strategy wants a timer subscription.
            if (strategy.IsLaunched)
            {   // If the strategy has already been launched, then just add the time subscription.
                // Otherwise, we will hold off on beginning its subscription until its launched.
                m_TimeSubscribers.Add(subscriber);
            }
            return true;
        }
        //
        //
        //
        public bool SubscribeToRemoteEngineHub(string hubName, IEngineContainer engineContainer, out Hub remoteHub)
        {
            remoteHub = null;
            AppServices services = AppServices.GetInstance();
            IService iService = null;
            if (services.TryGetService(hubName, out iService))  // in future, name of remote service could be attribute!
            {
                if (iService is Hub)
                    remoteHub = (Hub)iService;
                if (iService is IEngineHub)
                {
                    ((IEngineHub)remoteHub).EngineChanged += new EventHandler(RemoteHub_EngineChanged);
                }
                //if ()
                return true;
            }
            else
                return false;
        }//SubscribeToRemoteEngineHub()
        //
        #endregion//Public Methods


        #region Hub Event Handler Overrides
        // *****************************************************************
        // ****                 Hub Event Handler                      ****
        // *****************************************************************
        //
        //
        //
        //
        // *******************************************************
        // ****                 HubEvent Handler              ****
        // *******************************************************
        protected override void HubEventHandler(EventArgs[] eventArgList)
        {
            foreach (EventArgs eventArg in eventArgList)
            {
                Type eArgType = eventArg.GetType();
                if (eArgType == typeof(RequestEventArg<RequestCode>))
                {
                    //
                    // Requests
                    //
                    RequestEventArg<RequestCode> requestArg = (RequestEventArg<RequestCode>)eventArg;
                    switch (requestArg.RequestType)
                    {
                        case RequestCode.ServiceStateChange:
                            ProcessServiceStateRequests(requestArg);
                            break;
                        case RequestCode.LoadStrategies:
                            ProcessRequestLoadStrategies(requestArg);
                            break;
                        case RequestCode.StrategyReprice:
                            Log.NewEntry(LogLevel.Minor, "HubEventHandler: StrategyReprice.");
                            InstrumentChangeArgs fakeEventArgs = new InstrumentChangeArgs();
                            fakeEventArgs.Sender = this;
                            UpdateStrategyPricing(fakeEventArgs, false);
                            break;
                        //case RequestArgs.RequestCode.ForceStrategyReprice:
                        //    Log.NewEntry(LogLevel.Minor, "HubEventHandler: Forced StrategyUpdate.");
                        //UpdateStrategies(true);
                        //    break;
                        //case RequestArgs.RequestCode.RequestEngineParameterSave:
                        //SaveEngineParameters();// This is a request to save to a file all Engine parameters.
                        //    break;
                        //case RequestArgs.RequestCode.RequestEngineParameterLoad:
                        //LoadEngineParameters();// This is a request to save to a file all Engine parameters.
                        //    break;
                        //case RequestArgs.RequestCode.TimeEvent:
                        //ProcessTimeEvent();
                        //    break;
                        //case RequestArgs.RequestCode.SaveStrategyPosition:
                        //StrategyPositionDBSave();
                        //    break;
                        default:
                            Log.NewEntry(LogLevel.Error, "HubEventHandler: Request {0} not implemented", requestArg.ToString());
                            break;
                    }//switch(Request)
                    m_Requests.Recycle(requestArg);             // Note how all RequestEventArgs are recycled here!  
                }
                //
                // Market Updates
                //
                else if (eArgType == typeof(InstrumentChangeArgs))
                    ProcessMarketInstrumentChange((InstrumentChangeArgs)eventArg);  // Events from a Market BookHub
                else if (eArgType == typeof(FoundServiceEventArg))
                    ProcessMarketFoundService((FoundServiceEventArg)eventArg);      // Events when new products/instruments found. 
                else if (eArgType == typeof(MarketStatusChangedEventArg))
                    this.m_HubName = this.m_HubName;
                //ProcessMarketInstrumentChange((MarketStatusChangedEventArg)eventArg);
                //
                // OrderHub Updates
                //
                else if (eArgType == typeof(FillEventArgs))
                    ProcessFillEventArgs((FillEventArgs)eventArg);
                else if (eArgType == typeof(OrderEventArgs))
                {
                    OrderEventArgs orderEventArgs = (OrderEventArgs)eventArg;
                    if (orderEventArgs.IsAddConfirmation)       // this is a confirmation
                        ProcessOrderSubmittedEventArgs(orderEventArgs);
                    else
                        ProcessOrderEventArgs(orderEventArgs);
                }
                //
                // Engine Events and Cluster events
                //
                else if (eArgType == typeof(EngineEventArgs))
                {
                    if (ProcessEngineEvent((EngineEventArgs)eventArg))          // if engine requires updating, request updating.
                    {
                        InstrumentChangeArgs eventArgs = new InstrumentChangeArgs();
                        eventArgs.Sender = this;
                        UpdateStrategyPricing(eventArgs, false);
                    }
                }
                else if (eArgType == typeof(UV.Lib.FrontEnds.Clusters.ClusterEventArgs))
                {
                    //ProcessClusterEvents(eventArg);
                }
                else if (eventArg is Queries.QueryBase)                         // Pass thru any QueryBase event.
                    ProcessCompletedQuery((Queries.QueryBase)eventArg);
                else if (eventArg is ServiceStateEventArgs)
                {   // triggered by some Service that we are interested in.

                }
                else
                {   // unknown event.
                    Log.NewEntry(LogLevel.Error, "Unknown event type {0}", eArgType.ToString());
                }
            }
        }//end HubEvent
        //
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
            // Clear out spontaneously generated engine events from their event queues.
            if (this.EngineChanged != null) CheckSpontaneousEngineEvents(); // Send any events to subscribers.

            // Check periodic subscribers.
            Book aBook = null;
            if (m_Market != null && m_Market.TryEnterReadBook(out aBook))
            {
                int i = 0;
                while (i < m_TimeSubscribers.Count)
                {
                    try
                    {
                        m_TimeSubscribers[i].TimerSubscriberUpdate(aBook);
                    }
                    catch (Exception e)
                    {
                        Log.NewEntry(LogLevel.Error, "UpdatePeriodic: Failed to update TimeSubscibers with market book: {0}", e.Message);
                    }
                    i++;
                }
                m_Market.ExitReadBook(aBook);
            }

        }// UpdatePeriodic()
        //
        //
        //
        #endregion// Hub Event Handler overrides


        #region Process Start-up Events
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        //
        //
        //
        // *********************************************************
        // ****             Connect To Services()               ****
        // *********************************************************
        /// <summary>
        /// Locates all available services that we need, and subscribes
        /// to relevent events.
        /// </summary>
        private void ConnectToServices()
        {
            foreach (IService iService in m_AppServices.GetServices())
            {
                if (iService is MarketHub)
                    m_Market = (MarketHub)iService;
                else if (iService is DatabaseReaderWriter)
                {
                    
                    m_DBReaderWriter = (DatabaseReaderWriter)iService;
                    m_DBReaderWriter.QueryResponse += new EventHandler(this.HubEventEnqueue);
                    Log.NewEntry(LogLevel.Minor, "ConnectToServices: Found database writer {0}.", m_DBReaderWriter.ServiceName);
                }
                // TODO: collect order hubs too etc.
            }
            // Connect to market
            if (m_Market != null)
            {
                Log.NewEntry(LogLevel.Minor, "ConnectToServices: Found market {0}. Subscribing to events.", m_Market.ServiceName);
                m_Market.FoundResource += new EventHandler(HubEventEnqueue);
                m_Market.InstrumentChanged += new EventHandler(HubEventEnqueue);
                m_Market.MarketStatusChanged += new EventHandler(HubEventEnqueue);
                m_Market.ServiceStateChanged += new EventHandler(HubEventEnqueue);
            }


        }//ConnectToServices()
        //
        //
        //
        // *********************************************************
        // ****         ProcessRequest Load Stategies           ****
        // *********************************************************
        /// <summary>
        /// Create new strategies, and add them to managed list of strategies.
        /// The requestArg.Data[] list should contain a list of file name that contain
        /// Strategy definitions.
        /// TODO: 
        ///     1) Can this be carried out on another thread during run time?
        ///         So new strategies can be created on the fly without performance sacrifice?
        /// </summary>
        private void ProcessRequestLoadStrategies(RequestEventArg<RequestCode> requestArg)
        {
            // Read strategy file and create them.
            List<Strategy> newStrategies = null;
            if (!string.IsNullOrEmpty(m_StrategyFileName))
            {
                Log.NewEntry(LogLevel.Major, "ProcessRequestLoadStrategies: Loading strategies from file {0}.", m_StrategyFileName);
                string filePath = string.Format("{0}{1}", m_AppServices.Info.UserConfigPath, m_StrategyFileName);
                if (StrategyMaker.TryCreateFromFile(filePath, this.Log, out newStrategies))
                    InitializeStrategies(newStrategies);
                else
                {
                    Log.NewEntry(LogLevel.Error, "ProcessRequestLoadStrategies: Failed to load strategies. Stopping.");
                    AppServices.GetInstance().Shutdown();
                    return;
                }
            }
            else if (m_StrategyGroupIds != null && m_StrategyGroupIds.Count > 0)
            {
                Log.BeginEntry(LogLevel.Major, "ProcessRequestLoadStrategies: Loading from database groups:");
                foreach (int n in m_StrategyGroupIds)
                    Log.AppendEntry(" {0}", n.ToString());
                Log.AppendEntry(". ");
                Log.EndEntry();
                if (m_DBReaderWriter == null)
                {
                    Log.NewEntry(LogLevel.Error, "ProcessRequestLoadStrategies: Failed to locate a database reader. Cannot load strategies!");
                    return;
                }
                if (StrategyMaker.TryCreateFromDatabase(m_StrategyGroupIds, this.Log, m_DBReaderWriter, out newStrategies))
                    InitializeStrategies(newStrategies);
                else
                {   // Failed to load from database
                    Log.NewEntry(LogLevel.Error, "ProcessRequestLoadStrategies: Failed to load strategies. Stopping.");
                    AppServices.GetInstance().Shutdown();
                    return;
                }
            }
            else
            {
                Log.NewEntry(LogLevel.Error, "ProcessRequestLoadStrategies: Failed to understand request.");
            }
        }//ProcessRequestLoadStrategies()
        //
        //
        //
        //
        // *****************************************************
        // ****         InitializeStrategies()              ****
        // *****************************************************
        /// <summary>
        /// Given a collection of Strategies, this method adds them to our list
        /// and carefully initializes their engines.
        /// This has been separated from the LoadStrategies methods so that in the future
        /// we use this to request that Strategies be created on the fly.
        /// </summary>
        /// <param name="newStrategies">list of Strategy objects to accept</param>
        /// <returns></returns>
        private void InitializeStrategies(List<Strategy> newStrategies)
        {
            List<Strategy> strategiesToCompleteSetup = null;
            if (newStrategies == null)
            {
                Log.NewEntry(LogLevel.Major, "ProcessRequestLoadStrategies: No strategies created.");
                return;
            }

            // TODO: Create a StrategyGroup object that contains multiple strategies that must be initialized together.
            //      If any of the strategies in group are waiting to start, then all must wait to Setup / Launch.
            //      Useful for term structure models that need to know all their relatives are running.
            foreach (Strategy strategy in newStrategies)
            {
                //
                // Load default engines if missing.
                //
                List<IEngine> engineList = strategy.GetEngines();
                Engines.ZGraphEngine zGraphEng = null;                          // this is engine we want to include
                foreach (IEngine iEng in engineList)
                {
                    if (iEng is Engines.ZGraphEngine)
                        zGraphEng = (Engines.ZGraphEngine)iEng;
                    // search for others here.
                }
                if (zGraphEng == null)
                {
                    zGraphEng = new Engines.ZGraphEngine();
                    ((IStringifiable)strategy).AddSubElement(zGraphEng);
                }

                // Initialize strategy
                strategy.SetupInitialize(this);                                 // Creates parameter tables and gui templates.                                   
            }// next strategy

            // Begin Setup
            foreach (Strategy strategy in newStrategies)
                strategy.SetupBegin(this);

            // Determine which strategies are ready to Complete Setup phase.
            foreach (Strategy strategy in newStrategies)
            {
                
                if (strategy.IsReadyForSetup)
                {
                    if (strategiesToCompleteSetup == null)
                        strategiesToCompleteSetup = new List<Strategy>();
                    strategiesToCompleteSetup.Add(strategy);
                    
                }
                else
                {
                    Log.NewEntry(LogLevel.Minor, "InitializeStrategies: Strategy {0} not ready for complete setup.", strategy.ID);
                    if (!m_StrategiesPendingSetupComplete.ContainsKey(strategy.ID))
                        m_StrategiesPendingSetupComplete.Add(strategy.ID, strategy);
                }
            }


            // Call Setup
            if (strategiesToCompleteSetup != null && strategiesToCompleteSetup.Count > 0)
                SetupCompleteStrategies(strategiesToCompleteSetup);

        }// InitializeStrategies()
        // 
        /// <summary>
        /// Once Strategies are constructed and SetupInitialized(), their setup
        /// is completed and then they are launched.
        /// </summary>
        /// <param name="newStrategies">list of strategies</param>
        /// <returns></returns>
        private void SetupCompleteStrategies(Strategy strategy)
        {
            m_Strategies.Add(strategy.ID, strategy);
            strategy.SetupComplete();

            // Setup is complete.  Lets announce our existance
            EngineEventArgs args = EngineEventArgs.ConfirmNewControls(strategy.ID);
            if (LoadEngineEventArgResponse(args))
                OnEngineChange(args);


            TryLaunchStrategy(strategy);
        }
        private void SetupCompleteStrategies(List<Strategy> newStrategies)
        {
            foreach (Strategy strategy in newStrategies)
                m_Strategies.Add(strategy.ID, strategy);

            // Complete setup for strategies and engines.
            // This call is done only now that all strategies, engines are initialized.
            // Here is where strategies are allowed to discover each other.
            foreach (Strategy strategy in newStrategies)
            {
                strategy.SetupComplete();

                // Setup is complete.  Lets announce our existance
                EngineEventArgs args = EngineEventArgs.ConfirmNewControls(strategy.ID);
                if (LoadEngineEventArgResponse(args))
                    OnEngineChange(args);
            }

            // Attempt to launch strategies.
            foreach (Strategy strategy in newStrategies)
                TryLaunchStrategy(strategy);

        }//TryAddStrategies()
        //
        //
        //
        // *********************************************************
        // ****             Try Launch Strategy()               ****
        // *********************************************************
        /// <summary>
        /// StrategyHub tests the Strategy and if its ready to be launched, 
        /// completes all its subscriptions, removes it from StrategiesPending lists, and 
        /// launches it.
        /// Thereafter, it receives market updates as they occur.
        /// </summary>
        /// <param name="strategy"></param>
        /// <returns></returns>
        private bool TryLaunchStrategy(Strategy strategy)
        {
            bool isReadyToLaunch = true;
            if (m_Market == null)
            {
                isReadyToLaunch = false;
            }
            else
            {
                // Test: Market instruments exist.
                Log.BeginEntry(LogLevel.Minor, "TryLaunchStrategy: {0} ", strategy.Name);
                Book aBook;
                if (m_Market.TryEnterReadBook(out aBook))
                {
                    foreach (InstrumentName instrumentName in strategy.m_MarketInstrumentList)
                    {
                        int instrID = -1;
                        if (m_Market.TryLookupInstrumentID(instrumentName, out instrID) && aBook.Instruments[instrID].DeepestLevelKnown >= 0)
                        {

                        }
                        else
                        {
                            Log.AppendEntry(" Waiting for {0}.", instrumentName);
                            isReadyToLaunch = false;
                        }
                    }// next instrumentName
                    m_Market.ExitReadBook(aBook);
                }
                else
                {
                    Log.AppendEntry(" Failed to obtain read book.");
                    isReadyToLaunch = false;
                }

                // Test: Data loading subscriptions.
                if (m_StrategyPendingQueries.ContainsKey(strategy.ID) && m_StrategyPendingQueries[strategy.ID].Count > 0)// strategy.m_PendingQueries.Count > 0)
                {
                    Log.AppendEntry(" Still waiting for {0} pending queries.", m_StrategyPendingQueries[strategy.ID].Count);// strategy.m_PendingQueries.Count);
                    isReadyToLaunch = false;
                }
            }

            //
            // Launch and exit.
            //
            if (isReadyToLaunch)
            {   // This strategy is ready!
                Log.AppendEntry(" *** Launching! *** ");
                strategy.IsLaunched = true;

                if (m_StrategiesPendingLaunch.Contains(strategy))
                    m_StrategiesPendingLaunch.Remove(strategy);           // remove from pending list, if its there.

                // Complete market subscriptions.
                Log.AppendEntry(" Subscriptions:");
                foreach (InstrumentName instrName in strategy.m_MarketInstrumentList)
                {
                    int instrID = -1;
                    if (m_Market.TryLookupInstrumentID(instrName, out instrID))
                    {
                        if (!m_InstrumentSubsciption.ContainsKey(instrID))         // Create a subscription handler for this instrId
                            m_InstrumentSubsciption.Add(instrID, new List<int>());
                        m_InstrumentSubsciption[instrID].Add(strategy.ID);             // Add this strategy to subscription list.
                        Log.AppendEntry(" {0}", instrName);
                    }
                }

                // Complete timer subscriptions
                foreach (ITimerSubscriber subscriber in strategy.m_MyTimerSubscribers)
                    m_TimeSubscribers.Add(subscriber);

                // Add to strategy pricing update list.
                if (!m_StrategiesPricingRanked.Contains(strategy.ID))
                {   // Need to add this to our list.
                    // TODO: insert this into the correct spot, immediately after its masters, for example.
                    m_StrategiesPricingRanked.Add(strategy.ID);
                }
                Log.EndEntry();

                //
                // Trigger initial market events.
                //
                Book aBook;
                if (m_Market.TryEnterReadBook(out aBook))
                {
                    strategy.MarketInstrumentInitialized(aBook);

                    InstrumentChangeArgs eventArgs = new InstrumentChangeArgs();
                    eventArgs.Sender = this;
                    strategy.MarketInstrumentChanged(aBook, eventArgs, false);

                    m_Market.ExitReadBook(aBook);
                }
            }
            else if (!m_StrategiesPendingLaunch.Contains(strategy))
            {   // This strategy needs to be put into the pending list.
                Log.AppendEntry(" Waiting to launch strategy {0}", strategy.Name);
                m_StrategiesPendingLaunch.Add(strategy);
                Log.EndEntry();
            }
            else
            {
                Log.EndEntry();
            }

            return isReadyToLaunch;
        }// TryLaunchStrategy()
        //
        //
        //
        //
        //
        #endregion//Start-up Methods


        #region Process Run-Time Events
        // *********************************************************************
        // ****                     Private Methods                         ****
        // *********************************************************************
        //
        //
        //
        //
        // *****************************************************************
        // ****             Process Service State Requests              ****
        // *****************************************************************
        /// <summary>
        /// A request from outside or inside this hub to change the current service state.  
        /// Since some states are only attainable through other intermediate states, 
        /// one request to "Running" may require multiple stages to complete.
        /// Notes:
        ///     1. Moving thru multiple intermediate states is handled by storing 
        /// the desired state "Running" in m_ServiceStatePending and then "strobing" this method 
        /// whenever something occurs that suggests we might be able to proceed to the next
        /// state on our way to the final "Running" state.  
        ///     2. To strobe this method, submit a request with empty request.Data.
        /// </summary>
        /// <param name="request"></param>
        private void ProcessServiceStateRequests(RequestEventArg<RequestCode> request)
        {
            // Accept requested state as new pending state.
            if ((ServiceStates)request.Data.Count > 0)              // Allows for empty requests that strobe this method.
            {   // TODO: Validate that this request is allowed?
                ServiceStates requestedState = (ServiceStates)request.Data[0];
                if (requestedState != ServiceStates.None)
                    m_ServiceStatePending = (ServiceStates)request.Data[0];     // This is new target state.
            }
            if (m_ServiceStatePending == m_ServiceState)
                return;                                             // we are where we are supposed to be.

            // Try to move closer to new Pending state.
            ServiceStates newState = this.m_ServiceState;           // State that will become new state, default is unchanged.
            bool isAdvancingState = (int)m_ServiceStatePending > (int)m_ServiceState;
            bool isRetardingState = (int)m_ServiceStatePending < (int)m_ServiceState;
            //ServiceStates nextState = NextServiceState(m_ServiceState, m_ServiceStatePending);            
            bool forceStrategyUpdate = false;                       // Force strategies to reprice.
            switch (this.m_ServiceState)                             // Switch on state we are *IN*
            {
                case ServiceStates.Unstarted:
                    newState = ServiceStates.Started;               // hub thread is obviously stated.                    
                    break;
                //
                // ****     Started         ****
                //
                case ServiceStates.Started:
                    if (m_ServiceStatePending == ServiceStates.Stopped)
                    {   // We always allow us to try to stop the hub.
                        newState = ServiceStates.Stopping;
                        this.HubEventEnqueue(m_Requests.Get(RequestCode.ServiceStateChange, ServiceStates.Stopped));   // Strobe this method again.
                    }
                    else if (isAdvancingState)
                    {   // Locate services that we will need before entering the Running state.
                        ConnectToServices();
                        // Load initial collection of strategies.                        
                        ProcessRequestLoadStrategies(m_Requests.Get(RequestCode.LoadStrategies)); // Do this synchronously.
                        newState = ServiceStates.Running;           // once initial strategies are loaded, move to Running state.
                    }
                    break;
                //
                // ****     Running         ****
                //
                case ServiceStates.Running:
                    // In this state, some strategies exist (perhaps) and neccessary services have been found.
                    if (m_ServiceStatePending == ServiceStates.Stopped)
                    {
                        newState = ServiceStates.Stopping;
                        this.HubEventEnqueue(m_Requests.Get(RequestCode.ServiceStateChange, ServiceStates.Stopped)); // strobe
                    }
                    break;
                //
                // ****     Stopping         ****
                //
                case ServiceStates.Stopping:
                    int nStrategiesReadyToStop = m_Strategies.Count;
                    foreach (Strategy strategy in m_Strategies.Values)
                        if (strategy.TryToStop())
                            nStrategiesReadyToStop--;
                    if (nStrategiesReadyToStop <= 0)
                    {
                        Log.NewEntry(LogLevel.Major, "Stopping: All {0} strategies are ready to stop.", m_Strategies.Count);
                        // Now stop all other resources (which Strategies may have still been accessing on their way to stopping).
                        //if (m_DBReaderWriter != null)
                        //{
                        //    m_DBReaderWriter.RequestStop();
                        //    m_DBReaderWriter = null;
                        //}

                        base.Stop();
                        newState = ServiceStates.Stopped;
                    }
                    else
                    {   // We are not ready to stop yet.  Wait some more, then try again.
                        Log.NewEntry(LogLevel.Major, "Stopping: Only {0} of {1} strategies are ready to stop.  Wait more.", nStrategiesReadyToStop, m_Strategies.Count);
                        m_PendingRequests.AddPending(m_Requests.Get(RequestCode.ServiceStateChange, ServiceStates.Stopped), 1);
                    }
                    break;
                //
                // ****     Stopping         ****
                //
                case ServiceStates.Stopped:
                    base.Stop();                        // Our thread is always dead before we get here - 
                    break;
                default:
                    break;
            }// switch for service state

            // Set new state if changed.
            if (m_ServiceState != newState)
            {
                //Log.NewEntry(LogLevel.Major, "ProcessServiceStateRequests: State changed.  {1}.", this.m_ServiceState.ToString(), newState.ToString());
                ServiceStates prevState = this.m_ServiceState;      // store previous state
                m_ServiceState = newState;                          // change
                if (forceStrategyUpdate)
                {   // A state change is so important, we flag all strategies to reprice themselves.
                    // We force the update so orders are updated even if market wouldn't normally lead to an order update.
                    //foreach (Strategy strat in m_Strategies) // { strat.IsPricingEngineUpdateRequired = true; }
                    //    strat.m_Pricing.IsUpdateRequired = true;
                    //UpdateStrategies(true);
                }
                OnServiceStateChanged(prevState, m_ServiceState);
            }
            else
                Log.NewEntry(LogLevel.Major, "ProcessStateChange: Unchanged states from {0} to {1}.", this.m_ServiceState.ToString(), this.m_ServiceStatePending.ToString());
        }//ProcessServiceStateRequests()
        //
        //
        //
        // *****************************************************************
        // ****                 ProcessMarketFoundService()             ****
        // *****************************************************************
        private void ProcessMarketFoundService(FoundServiceEventArg eventArg)
        {
            //
            // Check for found "market instruments"  - these occur when a book is created.
            //
            if (eventArg.FoundInstrumentMarkets != null)
            {
                Log.BeginEntry(LogLevel.Minor, "ProcessMarketFoundService: Found {0} instrument markets:", eventArg.FoundInstrumentMarkets.Count);
                foreach (InstrumentName instrumentName in eventArg.FoundInstrumentMarkets)
                    Log.AppendEntry(" {0}", instrumentName);
                Log.EndEntry();

                // Mark these instruments as known to us now.
                int instrID;
                foreach (InstrumentName instrumentName in eventArg.FoundInstrumentMarkets)
                    if (m_Market.TryLookupInstrumentID(instrumentName, out instrID))
                        if (m_InstrumentSubsciption.ContainsKey(instrID) == false)
                            m_InstrumentSubsciption.Add(instrID, new List<int>());

                // Check which Strategies depend on these newly found markets. 
                foreach (InstrumentName instrumentName in eventArg.FoundInstrumentMarkets)
                {
                    FoundInstrumentMarkets(instrumentName);
                }
            }
            //
            // Check for new mkt books.
            //
            if (eventArg.FoundInstrumentBooks != null)
            {
                Log.BeginEntry(LogLevel.Minor, "ProcessMarketFoundService: Found {0} instrument books:", eventArg.FoundInstrumentBooks.Count);
                foreach (InstrumentName instrumentName in eventArg.FoundInstrumentBooks)
                    Log.AppendEntry(" {0}", instrumentName);
                Log.EndEntry();
            }
            //
            // Check for found instrument details.
            //
            if (eventArg.FoundInstruments != null)
            {
                Log.BeginEntry(LogLevel.Minor, "ProcessMarketFoundService: Found {0} instrument details:", eventArg.FoundInstruments.Count);
                foreach (InstrumentName instrumentName in eventArg.FoundInstruments)
                    Log.AppendEntry(" {0}", instrumentName);
                Log.EndEntry();
            }
            //
            // Found products
            //
            if (eventArg.FoundProducts != null)
            {
                Log.BeginEntry(LogLevel.Minor, "ProcessMarketFoundService: Found {0} products:", eventArg.FoundProducts.Count);
                foreach (Product product in eventArg.FoundProducts)
                    Log.AppendEntry(" {0}", product);
                Log.EndEntry();
            }
        }//ProcessMarketFoundService()
        //
        //
        //
        private void FoundInstrumentMarkets(InstrumentName instrumentName)
        {
            if (m_InstrumentSubscriptionsOutstanding.Contains(instrumentName))
            {   // This is a subscription we have been waiting for!
                m_InstrumentSubscriptionsOutstanding.Remove(instrumentName);
                List<Strategy> strategyToRelaunch = new List<Strategy>(m_StrategiesPendingLaunch);
                foreach (Strategy strategy in strategyToRelaunch)
                {
                    if (strategy.m_MarketInstrumentList.Contains(instrumentName))
                        TryLaunchStrategy(strategy);
                }
            }
        }//FoundMarketInstrument

        //
        //
        //
        // *****************************************************************
        // ****             Process MarketInstrument Change()           ****
        // *****************************************************************
        private void ProcessMarketInstrumentChange(InstrumentChangeArgs eventArgs)
        {
            Book aBook;                                                                 // market server book
            bool isUpdateRequired = false;									            // flag indicates one or more strategies need update.			
            foreach (KeyValuePair<int, InstrumentChange> instrChange in eventArgs.ChangedInstruments)// Loop thru each instrID with a changed mkt.
            {
                int instrID = instrChange.Key;

                List<int> stratIDList;
                if (!m_InstrumentSubsciption.TryGetValue(instrID, out stratIDList))     // First time subscription call-backs.
                {   // This ocurrs when we get a market update prior to getting the market
                    // found event.  This should never happen within our new approach.
                    if (m_Market.TryEnterReadBook(out aBook))
                    {
                        m_InstrumentSubsciption.Add(instrID, new List<int>());          // Create a new subscription list for this instrument.
                        InstrumentName instrName = aBook.Instruments[instrID].Name;     // name of instr with event
                        m_Market.ExitReadBook(aBook);
                        FoundInstrumentMarkets(instrName);
                    }
                }
                else
                {
                    // Mark strategies subscribed to this instrument for price updating.
                    foreach (int stratID in m_InstrumentSubsciption[instrID])
                    {
                        m_Strategies[stratID].m_PricingEngine.IsUpdateRequired = true;  // market primary-market strategy changes!
                        isUpdateRequired = true;
                    }
                }
            }//next instrument ID
            // Now update all Strategies (that are marked for updating).
            if (isUpdateRequired)
                UpdateStrategyPricing(eventArgs, false);                                // perform a typical UpdateStrategies().

        }//Process Market Instrument Change()
        //
        //
        //
        //
        //
        //
        //
        //
        // *****************************************************************
        // ****                 ProcessOrderEventArgs()                 ****
        // *****************************************************************
        private void ProcessOrderEventArgs(OrderEventArgs orderEventArg)
        {
            // Create key to determine which Strategies want to know about this event.
            InstrumentName instrName = orderEventArg.Order.Instrument;
            int bookId = orderEventArg.OrderBookID;
            string key = string.Format(m_FillSubscriptionFormat, instrName.FullName, bookId);   //use same format as fills
            List<int> subscribingStrategies = null;
            if (m_MajorOrderStatusSubscription.TryGetValue(key, out subscribingStrategies))
            {
                bool isUpdateRequired = false;
                foreach (int stratID in subscribingStrategies)
                {
                    if (m_Strategies[stratID].m_OrderEngine != null)
                    {
                        //m_Strategies[stratID].m_OrderEngine.OrderStateChanged(orderEventArg); //this is being deprecated, commenting it out for the time being.
                        isUpdateRequired = true;
                    }
                }
                // Mark strategies for updating.
                foreach (int stratID in subscribingStrategies)
                {
                    m_Strategies[stratID].m_PricingEngine.IsUpdateRequired = true;  // market primary-market strategy changes!
                    isUpdateRequired = true;
                }
                // Now update all Strategies.
                if (isUpdateRequired)
                {
                    InstrumentChangeArgs eventArgs = new InstrumentChangeArgs();
                    eventArgs.Sender = this;
                    UpdateStrategyPricing(eventArgs, false);    // perform a typical UpdateStrategies().
                }
            }
        }//ProcessOrderEventArgs()
        //
        //
        // *****************************************************************
        // ****           ProcessOrderSubmittedEventArgs()              ****
        // *****************************************************************
        private void ProcessOrderSubmittedEventArgs(OrderEventArgs orderEventArg)
        {
            // Create key to determine which Strategies want to know about this event.
            InstrumentName instrName = orderEventArg.Order.Instrument;
            int bookId = orderEventArg.OrderBookID;
            string key = string.Format(m_FillSubscriptionFormat, instrName.FullName, bookId);   //use same format as fills
            List<int> subscribingStrategies = null;
            if (m_OrderSubmissionSubscription.TryGetValue(key, out subscribingStrategies))
            {
                bool isUpdateRequired = false;
                foreach (int stratID in subscribingStrategies)
                {
                    if (m_Strategies[stratID].m_OrderEngine != null)
                    {
                        //m_Strategies[stratID].m_OrderEngine.OrderSubmitted(orderEventArg);
                        isUpdateRequired = true;
                    }
                }
                // Mark strategies for updating.
                foreach (int stratID in subscribingStrategies)
                {
                    m_Strategies[stratID].m_PricingEngine.IsUpdateRequired = true;  // market primary-market strategy changes!
                    isUpdateRequired = true;
                }
                // Now update all Strategies.
                if (isUpdateRequired)
                {
                    InstrumentChangeArgs eventArgs = new InstrumentChangeArgs();
                    eventArgs.Sender = this;
                    UpdateStrategyPricing(eventArgs, false);    // perform a typical UpdateStrategies().
                }
            }

        }
        //
        //
        //
        // *****************************************************************
        // ****                 ProcessFillEventArgs()                  ****
        // *****************************************************************
        private void ProcessFillEventArgs(FillEventArgs eventArg)
        {
            // Create key to determine which Strategies want to know about this event.
            InstrumentName instrName = eventArg.InstrumentName;
            int bookId = eventArg.OrderBookID;
            string key = string.Format(m_FillSubscriptionFormat, instrName.FullName, bookId);
            List<int> subscribingStrategies = null;

            if (m_FillSubscription.TryGetValue(key, out subscribingStrategies))
            {
                //List<int> updateStrategies = new List<int>();       // Here is where strategies who need updating are listed.
                bool isUpdateRequired = false;

                // Fill Strategies that are subscribed (usually only one is).
                foreach (int stratID in subscribingStrategies)
                {
                    isUpdateRequired = m_Strategies[stratID].Filled(eventArg) || isUpdateRequired;
                }
                // Now update all Strategies.
                if (isUpdateRequired)
                {
                    InstrumentChangeArgs eventArgs = new InstrumentChangeArgs();
                    eventArgs.Sender = this;
                    UpdateStrategyPricing(eventArgs, false);    // perform a typical UpdateStrategies().
                }
            }
        }//ProcessFillEventArgs()
        //
        //
        //
        //
        //
        //
        //
        // *********************************************************
        // ****             Update Strategy Pricing             ****
        // *********************************************************
        //
        /// <summary>
        /// This method is always called only after: fills, mkt changes, engine parameter changes, etc.
        /// In each of these cases, Strategies/Engines are marked as IsUpdateRequired = True.
        /// This method then calls strategies in proper order for updating.
        /// Note: We should try call this method as little as possible.
        /// </summary>
        /// <param name="isForceUpdate"></param>
        private void UpdateStrategyPricing(InstrumentChangeArgs eventArgs, bool isForceUpdate)
        {
            // Update positions.
            //foreach (Strategy strat in m_Strategies) //m_StrategiesFillsRanked
            //    if (strat.m_FillEngine != null && strat.m_FillEngine.IsUpdateRequired) strat.m_FillEngine.UpdatePosition();

            // Update pricing now.  
            //  Notes: 
            //      1) A slave pricing engine flags the (fill engine of) its master upon receiving 
            //          a fill event; the master's fill engine should mark itself for repricing, and so should already
            //          be ready for updating now.
            Book aBook;
            if (m_Market.TryEnterReadBook(out aBook))
            {
                //foreach (Strategy strat in m_Strategies.Values) //m_StrategiesPricingRanked
                foreach (int strategyID in m_StrategiesPricingRanked)
                {
                    Strategy strat;
                    if (!m_Strategies.TryGetValue(strategyID, out strat))
                        continue;
                    try
                    {
                        if (strat.m_PricingEngine.IsUpdateRequired)
                        {
                            strat.MarketInstrumentChanged(aBook, eventArgs, isForceUpdate);     // updates the strategies, and will send orders, if needed.                
                            strat.m_PricingEngine.IsUpdateRequired = false;                     // reset the update required flag now.
                        }
                    }
                    catch (Exception e)
                    {
                        Log.NewEntry(LogLevel.Error, "UpdateStrategies: MarketChange exception for strategy {0}. Exception = {1} {2}", strat.Name, e.Message, e.StackTrace);
                    }
                }
                m_Market.ExitReadBook(aBook);					// Release current market book.
            }
        }//end UpdateStrategyPricing().
        //
        //
        //
        //
        //
        #endregion// run-time processing methods


        #region Process utility events
        // *****************************************************************
        // ****             Process utility events                      ****
        // *****************************************************************
        //
        //
        //
        // 
        // *****************************************************************
        // ****             Process Completed Query                     ****
        // *****************************************************************
        /// <summary>
        /// We invoke the method that requested the Query.
        /// </summary>
        /// <param name="eventArg"></param>
        protected void ProcessCompletedQuery(Queries.QueryBase eventArg)
        {
            //
            // We have received a query response.
            // Find if it belongs to one our my strategies.
            //
            int strategyID = -1;
            Strategy strategy = null;
            foreach (KeyValuePair<int,List<int>> keyValue in m_StrategyPendingQueries)      
                if (keyValue.Value.Contains(eventArg.QueryID))                  // check whether this queryID is in this list of queryIDs.
                {
                    strategyID = keyValue.Key;                                  // if it is, remember the strategyId that owns this list.
                    break;
                }
            if (strategyID == -1)
            {   // This query does not belong to one of my strategies!
                return;
            }

            // Remove this queryID from list of queries this strategy is waiting for.
            List<int> queryIdList;
            if (m_StrategyPendingQueries.TryGetValue(strategyID, out queryIdList))
            {
                queryIdList.Remove(eventArg.QueryID);                   
                if (queryIdList.Count <= 0)                                 // if there are none remaining, delete whole list.
                    m_StrategyPendingQueries.Remove(strategyID);
            }

            //
            // Fire the call back
            //
            EventHandler h;
            if (m_PendingQueryCallback.TryGetValue(eventArg.QueryID, out h))
            {
                m_PendingQueryCallback.Remove(eventArg.QueryID);       // Remove the call back since its complete.
                Log.NewEntry(LogLevel.Minor, "ProcessDBReaderWriter: Processing callback .");
                h.Invoke(this, eventArg);                
            }

            //
            // Check whether this strategy is waiting to be launched.
            //
            if (strategy != null && strategy.IsLaunched == false)
                TryLaunchStrategy(strategy);
        }//ProcessDBReaderWriter()
        //
        //
        //
        //
        //
        #endregion//Process utilities events


        #region Process Engine Events
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        //
        //
        // *************************************************************
        // ****                 Process Engine Event                ****
        // *************************************************************
        /// <summary>
        /// This responds to engine events that were pushed onto my queue.
        /// This is called by the internal hub thread.
        /// </summary>
        /// <param name="args"></param>
        private bool ProcessEngineEvent(EngineEventArgs args)
        {
           


            // Validate            
            bool isFireResponse = false;
            bool isUpdateRequired = false;

            // Process the event.
            int strategyID ;
            switch (args.MsgType)
            {
                // *****************************
                // ****     Get Controls    ****
                // *****************************
                case EngineEventArgs.EventType.GetControls:
                    //  This is a request for all my strategies' controls.                    
                    if (args.Status != EngineEventArgs.EventStatus.Request) { return false; }// we only read requests.
                    isFireResponse = LoadEngineEventArgResponse(args);
                    break;
                // *****************************
                // **** Parameter Change    ****
                // *****************************
                case EngineEventArgs.EventType.ParameterChange:
                    if (args.Status != EngineEventArgs.EventStatus.Request) 
                    {
                        if ((args.EngineHubResponding is IService) && ((IService)args.EngineHubResponding).ServiceName == "ExecutionHub")
                            return ProcessRemoteEngineEvent(args);
                        else
                            return false; 
                    }
                    // A request from a user to change a parameter's value.
                    args.EngineHubResponding = this;                // tell them who responded
                    strategyID = args.EngineContainerID;        // parameter change requested for this strategy.
                    if (strategyID < 0)
                    {   // This request is for all strategies
                        Log.NewEntry(LogLevel.Error, "ProcessEngineEvent: Error in strategyID.");
                        args.Status = EngineEventArgs.EventStatus.Failed;
                        isFireResponse = true;
                    }
                    else if (m_Strategies.ContainsKey(strategyID))
                    {   // strategy ID is within range of allowed IDs
                        isUpdateRequired = m_Strategies[strategyID].ProcessEngineEvent(args) || isUpdateRequired;
                        isFireResponse = true;
                    }
                    else
                    {   // strategy ID out of range!
                        Log.NewEntry(LogLevel.Error, "ProcessEngineEvent: Error in strategyID.");
                        args.Status = EngineEventArgs.EventStatus.Failed;
                        isFireResponse = true;
                    }
                    break;
                // *****************************
                // **** Parameter Value     ****
                // *****************************
                case EngineEventArgs.EventType.ParameterValue:
                    if (args.Status != EngineEventArgs.EventStatus.Request) 
                    {
                        if ((args.EngineHubResponding is IService) && ((IService)args.EngineHubResponding).ServiceName == "ExecutionHub")
                            return ProcessRemoteEngineEvent(args);
                        else
                            return false; 
                    }// we only read requests.
                    args.EngineHubResponding = this;            // tell them who responded
                    strategyID = args.EngineContainerID;        // parameter change requested for this strategy.
                    if (strategyID < 0)
                    {   // This request is for all strategies
                        Log.NewEntry(LogLevel.Error, "ProcessEngineEvent: Error in strategyID.");
                        args.Status = EngineEventArgs.EventStatus.Failed;
                        isFireResponse = true;
                    }
                    else if (m_Strategies.ContainsKey(strategyID))
                    {   // strategy ID is within range of allowed IDs
                        //m_Strategies[strategyID].ProcessEngineEvent(args);
                        isUpdateRequired = m_Strategies[strategyID].ProcessEngineEvent(args) || isUpdateRequired;
                        isFireResponse = true;
                    }
                    else
                    {   // strategy ID is unknown!
                        Log.NewEntry(LogLevel.Error, "ProcessEngineEvent: Error unknown strategyID={0}.", strategyID);
                        args.Status = EngineEventArgs.EventStatus.Failed;
                        isFireResponse = true;
                    }

                    break;
                // *****************************
                // ****     New Engine      ****
                // *****************************
                case EngineEventArgs.EventType.NewEngine:
                    if (args.Status != EngineEventArgs.EventStatus.Confirm) 
                        return false;                           // we only read requests.
                    strategyID = args.EngineContainerID;       
                    if (m_Strategies.ContainsKey(strategyID))
                    {   
                        m_Strategies[strategyID].ProcessEngineEvent(args);
                        //isUpdateRequired = m_Strategies[strategyID].ProcessEngineEvent(args) || isUpdateRequired;
                        isFireResponse = false;
                    }
                    else if (m_StrategiesPendingSetupComplete.ContainsKey(strategyID))
                    {
                        
                        m_StrategiesPendingSetupComplete[strategyID].ProcessEngineEvent(args);
                        if ( m_StrategiesPendingSetupComplete[strategyID].IsReadyForSetup )
                        {
                            Strategy strategy = m_StrategiesPendingSetupComplete[strategyID];
                            m_StrategiesPendingSetupComplete.Remove(strategy.ID);
                            SetupCompleteStrategies(strategy);
                        }
                    }
                    else
                    {   // strategy ID is unknown!
                        Log.NewEntry(LogLevel.Error, "ProcessEngineEvent: Error unknown strategyID={0}.", strategyID);
                        args.Status = EngineEventArgs.EventStatus.Failed;
                        isFireResponse = false;
                    }

                    break;
                //
                // Default
                //
                // *****************************
                // **** unknown             ****
                // *****************************
                default:
                    Log.NewEntry(LogLevel.Warning, "ProcessEngineEvent: unknown request {0}.", args);
                    args.Status = EngineEventArgs.EventStatus.Failed;
                    break;
            }
            // trigger response.
            if (isFireResponse) OnEngineChange(args);
            return isUpdateRequired;
        }//ProcessEngineEvent().
        //
        //
        private bool LoadEngineEventArgResponse(EngineEventArgs args)
        {
            // Initialize the response.
            bool isFireResponse = true;
            bool error = false;
            switch (args.MsgType)
            { 
                case EngineEventArgs.EventType.GetControls:
                    if (args.DataObjectList == null)
                        args.DataObjectList = new List<object>();
                    else
                        args.DataObjectList.Clear();
                    if (args.EngineContainerID < 0)
                    {   // caller wants all strategy controls.
                        foreach (Strategy strategy in m_Strategies.Values)
                            args.DataObjectList.Add(strategy.GetGuiTemplates());
                    }
                    else if (m_Strategies.ContainsKey(args.EngineContainerID))
                    {
                        args.DataObjectList.Add(m_Strategies[args.EngineContainerID].GetGuiTemplates());
                    }
                    else
                    {
                        Log.NewEntry(LogLevel.Minor, "ProcessEngineEvent: GetControls failed. Unknown StrategyID={0}", args.EngineContainerID);
                        error = true;// This may mean that the strategy is not yet ready. Its not really a problem.
                    }
                    // Respond
                    args.EngineHubResponding = this;
                    if (error)
                        args.Status = EngineEventArgs.EventStatus.Failed;
                    else
                        args.Status = EngineEventArgs.EventStatus.Confirm;
                    break;
                default:
                    throw new Exception("Not implemented.");
            }
            return isFireResponse;
        }
        //
        //
        //
        protected bool ProcessRemoteEngineEvent(EngineEventArgs args)
        {
            bool isUpdateRequired = false;
            bool isFireResponse = false;
            int strategyID;
            args.EngineHubResponding = this;            // tell them who responded
            strategyID = args.EngineContainerID;        // parameter change requested for this strategy.
            if (strategyID < 0)
            {   // This request is for all strategies
                Log.NewEntry(LogLevel.Error, "ProcessEngineEvent: Error in strategyID.");
                args.Status = EngineEventArgs.EventStatus.Failed;
                isFireResponse = true;
            }
            else if (m_Strategies.ContainsKey(strategyID))
            {   // strategy ID is within range of allowed IDs
                //m_Strategies[strategyID].ProcessEngineEvent(args);
                isUpdateRequired = m_Strategies[strategyID].ProcessEngineEvent(args) || isUpdateRequired;
                isFireResponse = true;
            }
            else
            {   // strategy ID is unknown!
                Log.NewEntry(LogLevel.Error, "ProcessEngineEvent: Error unknown strategyID={0}.", strategyID);
                args.Status = EngineEventArgs.EventStatus.Failed;
                isFireResponse = true;
            }
            if (isFireResponse) OnEngineChange(args);
            return isUpdateRequired;
        }
        //
        //
        //
        //
        // ****                 Check Spontaneous Engine Events()                  ****
        //
        /// <summary>
        /// Triggers events for engines that have changed due to Strategy.MarketChange() calls; 
        /// This call should be throttled.
        /// </summary>
        private void CheckSpontaneousEngineEvents()
        {
            // Send spontaneous (Strategy initiated) engine events.
            List<EngineEventArgs> spontaneousEvents = new List<EngineEventArgs>();
            foreach (Strategy strat in m_Strategies.Values)
                strat.AddSpontaneousEngineEvents(spontaneousEvents);
            OnEngineChange(spontaneousEvents);
        }// CheckSpontaneousEngineEvents()
        //
        //
        //
        #endregion//Process Engine Events


        #region IStringifiable Implementation
        // *************************************************************
        // ****                IStringifiable                       ****
        // *************************************************************
        //
        string IStringifiable.GetAttributes()
        {
            return string.Empty;
        }
        List<IStringifiable> IStringifiable.GetElements()
        {
            return null;
        }
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
            bool isTrue;
            int n;
            foreach (KeyValuePair<string, string> keyVal in attributes)
            {
                if (keyVal.Key.Equals("ShowLog") && bool.TryParse(keyVal.Value, out isTrue))
                    Log.IsViewActive = isTrue;
                else if (keyVal.Key.Equals("Filename", StringComparison.CurrentCultureIgnoreCase))
                    m_StrategyFileName = keyVal.Value;
                else if (keyVal.Key.Equals("GroupId", StringComparison.CurrentCultureIgnoreCase))
                {
                    string[] elems = keyVal.Value.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    m_StrategyGroupIds = new List<int>();
                    foreach (string s in elems)
                    {
                        if (int.TryParse(s, out n))
                            m_StrategyGroupIds.Add(n);
                    }
                }
            }//next attribute
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {
        }
        #endregion// IStringifiable


        #region IService
        // *****************************************************************
        // ****                     IService                            ****
        // *****************************************************************
        /// <summary>
        /// This implements the IService interface.
        /// Note:
        ///     1) Methods Start(), RequestStop() are implemented above
        ///     2) Events Stopping are implemented in base class.
        /// </summary>
        string IService.ServiceName
        {
            get { return base.m_HubName; }
        }// ServiceName()
        //
        /// <summary>
        /// This is called by an external thread.
        /// </summary>
        void IService.Connect()
        {
            this.HubEventEnqueue(m_Requests.Get(RequestCode.ServiceStateChange, ServiceStates.Running));
        }// Connect()
        //
        //
        // ****             Service State Changed()             ****
        //
        //
        public event EventHandler ServiceStateChanged;
        //
        /// <summary>
        /// This method reports the current state, and the previous one.
        /// </summary>
        /// <param name="prevState"></param>
        protected void OnServiceStateChanged(ServiceStates prevState, ServiceStates currentState)
        {
            // Report the service change.
            ServiceStateEventArgs eventArg = new ServiceStateEventArgs(this,currentState,prevState);
            Log.NewEntry(LogLevel.Major, "OnServiceStateChanged: {0}", eventArg);
            if (this.ServiceStateChanged != null)
            {
                ServiceStateChanged(this, eventArg);
            }
        }
        // This is how we can create our own events.
        //event EventHandler IService.Stopping
        //{
        //add { throw new NotImplementedException(); }
        //remove { throw new NotImplementedException(); }
        //}
        //
        #endregion//IService


        #region EngineChange Event and triggers
        // *************************************************************
        // ****                   Engine Change                     ****
        // *************************************************************
        //
        public event EventHandler EngineChanged;
        //
        //
        protected void OnEngineChange(List<EngineEventArgs> eList)
        {
            if (EngineChanged != null)
            {
                foreach (EngineEventArgs e in eList) { EngineChanged(this, e); }
            }
        }
        public void OnEngineChange(EngineEventArgs e)
        {
            if (EngineChanged != null) { EngineChanged(this, e); }
        }
        //
        //
        //
        public void RemoteHub_EngineChanged(object sender, EventArgs eventArgs)
        {
            if (eventArgs is EngineEventArgs)
            {
                this.HubEventEnqueue(eventArgs);                
            }
        }
        //
        //
        #endregion//Event


        #region IEnginHub implementation
        // *************************************************************
        // ****                   IEnginHub                         ****
        // *************************************************************
        //
        List<IEngineContainer> IEngineHub.GetEngineContainers()
        {
            List<IEngineContainer> engContainers = new List<IEngineContainer>();
            engContainers.AddRange(m_Strategies.Values);
            return engContainers;
        }
        //
        //
        //
        #endregion//Event


    }//end class
}//end namespace
