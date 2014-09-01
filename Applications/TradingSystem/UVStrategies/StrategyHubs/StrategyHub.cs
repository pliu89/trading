using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace UV.Strategies.StrategyHubs
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
    /// This is the shared-thread model implentatio of a Strategy Hub.  
    /// All strategies loaded here share a common thread, and so can easily communicate with each other.
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
        public UV.Lib.Utilities.Alarms.Alarm m_Alarm = null;                        // alarm service that is available to any strategy.
        private List<ITimerSubscriber> m_TimeSubscribers = new List<ITimerSubscriber>(); // 1 Hz update callbacks available to any strategy.
        private DateTime m_TimerSubscriptionNextUpdate = DateTime.MaxValue;         // next periodic 1-second model update. (MaxValue essentially disables updates until we are "started"!)

        // My internal work spaces
        private RequestFactory<RequestCode> m_Requests = new RequestFactory<RequestCode>(100); // Recycle Request Event Args here.
        private EventWaitQueueLite m_PendingRequests = null;


        //
        // Market subscriptions
        //
        public MarketHub m_Market = null;
        private List<int> m_StrategiesPricingRanked = new List<int>();                                      // order in which strategies are updated.
        private Dictionary<int, List<int>> m_InstrumentSubscription = new Dictionary<int, List<int>>();		// mkt instr ID --> list of strategy IDs.
        private List<InstrumentName> m_MarketInstrumentsOutstanding = new List<InstrumentName>();     // instruments we are waiting for market callbacks from.

        //
        // Remote EngineHub subscriptions
        //
        private Dictionary<string, IEngineHub> m_RemoteEngineHubs = new Dictionary<string, IEngineHub>();     // remote hubs we subscribe to.
        private Dictionary<string, List<EventHandler>> m_RemoteEngineHubSubsciptions = new Dictionary<string, List<EventHandler>>();
        private const string RemoteEngineKeyFormat = "{0}#{1}#{2}";                                         // format string for remote hub subscriptions.

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
        private List<int> m_StrategyGroupIds = null;                                            // groups of strategies to load.
        private List<int> m_StrategyIds = null;
        private Dictionary<int, Strategy> m_Strategies = new Dictionary<int, Strategy>();		// Main list of strategies - in order they were loaded, indexed by their "ID" number.
        private List<Strategy> m_StrategiesPendingLaunch = new List<Strategy>();                // Created strategies that are waiting for all resources before final launch.
        private Dictionary<int, Strategy> m_StrategiesPendingSetupComplete = new Dictionary<int, Strategy>();// Created strategies that are pending initialization.

        //private List<Strategy> m_StrategiesPricingRanked = new List<Strategy>();              // strategies ranked by pricing engine priority.
        //private List<Strategy> m_StrategiesFillsRanked = new List<Strategy>();				// strategies ranked by Fill engine priority.
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public StrategyHub()
            : base("StrategyHub", UV.Lib.Application.AppInfo.GetInstance().LogPath, false, LogLevel.ShowAllMessages)
        {
            base.m_WaitListenUpdatePeriod = 200;                            // msecs for periodic updates.
            m_AppServices = AppServices.GetInstance();

            m_PendingRequests = new EventWaitQueueLite(this.Log);
            m_PendingRequests.ResubmissionReady += new EventHandler(this.HubEventEnqueue);  // when pending requests ready, resubmit to hub.

            m_Alarm = new Lib.Utilities.Alarms.Alarm();
            m_Alarm.SetTimeDelegate(this.GetLocalTime);                     // tell timer to use my clock.

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
        //
        // *************************************************************
        // ****                 RequestHistoricData()               ****
        // *************************************************************
        /// <summary>
        /// Manages the requests from Strategies (or their engines) for historic data.
        /// This provides a user-friendly wrapper for the more general call to RequestQuery().
        /// </summary>
        /// <param name="instrument"></param>
        /// <param name="startUTC">Start DateTime of desired data.  In UTC time zone!</param>
        /// <param name="endUTC">Ending DateTime of desired data.</param>
        /// <param name="callBack"></param>
        /// <param name="strategy"></param>
        /// <returns></returns>
        public bool RequestHistoricData(InstrumentName instrument, DateTime startUTC, DateTime endUTC, EventHandler callBack, Strategy strategy)
        {
            // Create the query we want to submit.
            Queries.MarketDataQuery instrQ = new Queries.MarketDataQuery();
            instrQ.InstrumentName = instrument;
            //instrQ.MaxRows = 30000;          // Debugging purposes!
            instrQ.StartDate = startUTC;
            instrQ.EndDate = endUTC;
            return RequestQuery(instrQ, callBack, strategy);
        }//RequestHistoricData()
        //
        //
        /// <summary>
        /// Manages request from strategies for historic data.  This overload allows a user to simply request a number of seconds
        /// of trading timestamps for a given instrument ending at a certain time.  As long as the database has the correct data,
        /// the user need not worry about sessions nor weekends.
        /// </summary>
        /// <param name="instrument"></param>
        /// <param name="endUTC"></param>
        /// <param name="nSeconds"></param>
        /// <param name="callBack"></param>
        /// <param name="strategy"></param>
        /// <returns></returns>
        public bool RequestHistoricData(InstrumentName instrument, DateTime endUTC, int nSeconds, EventHandler callBack, Strategy strategy)
        {
            // Create the query we want to submit...this query will be constrained by rows
            Queries.MarketDataQuery instrQ = new Queries.MarketDataQuery();
            instrQ.InstrumentName = instrument;
            instrQ.MaxRows = nSeconds;
            instrQ.EndDate = endUTC;
            instrQ.StartDate = DateTime.MinValue;               // just to be explicit in the fact that we don't have a start date.
            return RequestQuery(instrQ, callBack, strategy);
        }//RequestHistoricData()
        //
        //
        // *************************************************************
        // ****             RequestEconomicData()            ****
        // *************************************************************
        //
        /// <summary>
        /// Manage the request for economic data from a strategy or its engines.
        /// </summary>
        /// <param name="instrument"></param>
        /// <param name="startUTC"></param>
        /// <param name="endUTC"></param>
        /// <param name="callBack"></param>
        /// <param name="strategy"></param>
        /// <returns></returns>
        public bool RequestEconomicData(InstrumentName instrument, DateTime startUTC, DateTime endUTC, EventHandler callBack, Strategy strategy)
        {
            // Create the query we want to submit.
            Queries.EconomicDataQuery economicDataQuery = new Queries.EconomicDataQuery();
            economicDataQuery.InstrumentName = instrument;
            economicDataQuery.MaxRows = 30000;          // Debugging purposes!
            economicDataQuery.StartDate = startUTC;
            economicDataQuery.EndDate = endUTC;
            return RequestQuery(economicDataQuery, callBack, strategy);
        }//RequestHistoricData()
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
        public bool RequestQuery(Queries.QueryBase query, EventHandler callback, Strategy strategy)
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
            if (!m_StrategyPendingQueries.ContainsKey(strategy.ID))
                m_StrategyPendingQueries.Add(strategy.ID, new List<int>());
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
        public bool RequestDatabaseWrite(Queries.QueryBase query)
        {
            if (query.IsRead)
            {
                Log.NewEntry(LogLevel.Error, "RequestDatabaseWrite: Failed.  Read is not implemented yet.");
                return false;
            }
            if (m_AppServices.User.RunType == RunType.Debug)
            {   // We will not write to database in debug mode.
                Log.NewEntry(LogLevel.Major, "RequestDatabaseWrite: Will not write query in {0} mode. Query = {1}", m_AppServices.User.RunType, query);
                return true;                // pretend all is ok.
            }
            if (m_DBReaderWriter != null)
                m_DBReaderWriter.SubmitAsync(query);
            return true;
        }// RequestDatabaseWrite()
        //
        //
        // *************************************************************
        // ****         SubscribeToMarketInstruments()              ****
        // *************************************************************
        /// <summary>
        /// This is called by a Strategy, or more likely its PricingEngine, to request a new 
        /// market subscription.  The StrategyHub 
        /// </summary>
        /// <param name="instrumentList"></param>
        /// <param name="strategy"></param>
        /// <param name="isSubscribeToMarketChanges">True. if you want to get InstrumentChanged callbacks when this instrument changes.</param>
        /// <returns></returns>
        public bool SubscribeToMarketInstruments(List<InstrumentName> instrumentList, Strategy strategy, bool isSubscribeToMarketChanges=true)
        {
            // Validate
            if (m_Market == null)
                return false;
            Log.BeginEntry(LogLevel.Minor, "SubscribeToMarketInstruments: Request {0} instruments for strategy {1}.", instrumentList.Count, strategy.Name);

            // Request markets.
            if (isSubscribeToMarketChanges)
                strategy.m_MarketInstrumentSubscriptions.AddRange(instrumentList);      // Store markets for subscriptions
            else
                strategy.m_MarketInstrumentNonSubscriptions.AddRange(instrumentList);   // Store markets for non-subscriptions
            int nInstrSubscriptionsRequested = 0;
            int instrumentID;
            foreach (InstrumentName instrumentName in instrumentList)
                if (m_Market.TryLookupInstrumentID(instrumentName, out instrumentID) == false && m_MarketInstrumentsOutstanding.Contains(instrumentName) == false)
                {   // This is an instrument unknown to mkt and not already requested!  So request it now.                    
                    Log.AppendEntry(" New request for {0}.", instrumentName);
                    m_Market.RequestInstrumentPriceSubscription(instrumentName);        // Request market updates from instrument. 
                    m_Market.RequestInstrumentTimeAndSalesSubscription(instrumentName); // Request time and sales for more detailed volume info
                    m_MarketInstrumentsOutstanding.Add(instrumentName);                 // add to waiting list.
                    nInstrSubscriptionsRequested++;
                }
            // Write log report
            if (nInstrSubscriptionsRequested > 0)
            {
                Log.AppendEntry(" Outstanding requests:");
                foreach (InstrumentName instrumentName in m_MarketInstrumentsOutstanding)
                    Log.AppendEntry(" {0}", instrumentName);
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
        }// SubscribeToTimer()
        //
        //
        // *************************************************
        // ****     SubscribeToRemoteEngineHub()        ****
        // *************************************************
        /// <summary>
        /// Strategy wants a subscription to all EngineChanged events coming from a specific engineID inside
        /// a foreign/remote EngineHub.
        /// Caller provides the unique remote Hub ServiceName, 
        /// Philosophy:
        /// Usually, an EngineHub (like this one) produces Engine Events that are broadcasted
        /// to Engine Event subscribers, and receives only (EngineEventArg) requests from outside.
        /// However, there are case when a specific strategy wants to communicate with a strategy on another
        /// Hub.  Sending messages to the other strategy is easily accomplished by sending a EngineEventArg 
        /// request.  However, this hub typically does not subscribe to responses from other hubs.
        /// To get these repsonses, a strategy can ask to receive messages from a specific strategy on 
        /// another specific hub.
        /// </summary>
        /// <param name="remoteHubName"></param>
        /// <param name="remoteEngineContainerId"></param>
        /// <param name="remoteEngineId"></param>
        /// <param name="callBack"></param>
        /// <param name="iEngineHub"></param>
        /// <returns></returns>
        public bool SubscribeToRemoteEngineHub(string remoteHubName, int remoteEngineContainerId, int remoteEngineId, EventHandler callBack, out IEngineHub iEngineHub)
        {
            // Locate the desired remote hub.
            iEngineHub = null;
            if (m_RemoteEngineHubs.TryGetValue(remoteHubName, out iEngineHub) == false)
            {   // We have no current subscriptions for this remote hub.
                // Try to start one now.
                IService iService = null;
                if (m_AppServices.TryGetService(remoteHubName, out iService) == false)
                {   // We failed to locate the desired hub in our service list.  
                    return false;                               // inform caller of our failure.
                }
                // Set up new subscription to this remote hub.
                if (iService is IEngineHub)
                {
                    iEngineHub = (IEngineHub)iService;
                    iEngineHub.EngineChanged += new EventHandler(HubEventEnqueue);    // I will accept his event here, and then send them subscriber.
                    m_RemoteEngineHubs.Add(iEngineHub.ServiceName, iEngineHub);
                }
                else
                {
                    Log.NewEntry(LogLevel.Error, "SubscribeToRemoteEngineHub: Service {0} is not EngineHub.", iService.ServiceName);
                    return false;
                }
            }

            // Create new subscription filter.
            string filterKey = string.Format(RemoteEngineKeyFormat, remoteHubName, remoteEngineContainerId, remoteEngineId);
            List<EventHandler> callBackList = null;
            if (m_RemoteEngineHubSubsciptions.TryGetValue(filterKey, out callBackList) == false)
            {
                callBackList = new List<EventHandler>();
                m_RemoteEngineHubSubsciptions.Add(filterKey, callBackList);
            }
            callBackList.Add(callBack);

            // Exit
            return true;
        }//SubscribeToRemoteEngineHub()
        //
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
                    this.m_HubName = this.m_HubName; //ProcessMarketInstrumentChange((MarketStatusChangedEventArg)eventArg);

                //
                // Engine Events
                //
                else if (eArgType == typeof(EngineEventArgs))
                {
                    EngineEventArgs engineEventArg = (EngineEventArgs)eventArg;
                    
                    if (engineEventArg.Status == EngineEventArgs.EventStatus.Request)
                    {   // Process requests
                        if (engineEventArg.EngineHubName.Equals(this.ServiceName))
                        {   // This is a request for an outsider that is intended for us.
                            // Process this in the normal way.
                            if (ProcessEngineEventRequest(engineEventArg))
                            {   // If engine requires updating, above method returns true.
                                InstrumentChangeArgs eventArgs = new InstrumentChangeArgs();
                                eventArgs.Sender = this;
                                UpdateStrategyPricing(eventArgs, false);
                            }
                        }
                        else
                        {   // This should never happen.  Catch this error!
                            // We should not get requests intended for other hubs.
                        }
                    }
                    else if (engineEventArg.Status == EngineEventArgs.EventStatus.Confirm)
                    {   // Process confirmations.
                        // There are only a few cases when a strategy hub gets a confirmation.
                        if (engineEventArg.MsgType == EngineEventArgs.EventType.SyntheticOrder)
                        {   // Theses contain orders confirms, fills.
                            SyntheticOrder synthOrder = (SyntheticOrder)engineEventArg.DataObjectList[0];
                            Strategy strategy;
                            if (engineEventArg.EngineHubName.Equals(this.ServiceName) && m_Strategies.TryGetValue(engineEventArg.EngineContainerID, out strategy))
                            {
                                strategy.ProcessSyntheticOrder(synthOrder);
                            }
                        }
                        else
                        {   // Also, some engines I own may have set up subscription to remote engine confirmations.
                            // Create a filter key from this message, and see if someone has subscribed to this message.
                            string filterKey = string.Format(RemoteEngineKeyFormat, engineEventArg.EngineHubName, engineEventArg.EngineContainerID, engineEventArg.EngineID);
                            List<EventHandler> delegateList = null;
                            if (m_RemoteEngineHubSubsciptions.TryGetValue(filterKey, out delegateList))
                            {   // Engines are subscribed to this event!
                                foreach (EventHandler handler in delegateList)
                                    handler(this, engineEventArg);                      // If we don't copy this, receiver should NOT corrupt it.
                            }
                        }
                    }
                    else if (engineEventArg.MsgType == EngineEventArgs.EventType.AlarmTriggered)
                    {   // this is an alarm event
                        Strategy strategy;
                        if(m_Strategies.TryGetValue(engineEventArg.EngineContainerID, out strategy))
                        {
                            strategy.ProcessAlarmTriggered(engineEventArg);
                        }
                    }
                }
                //
                // Cluster Events
                //
                else if (eArgType == typeof(UV.Lib.FrontEnds.Clusters.ClusterEventArgs))
                {
                    //ProcessClusterEvents(eventArg);
                }
                //
                // Query call backs
                //
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
            //
            // Update Timer Subscriptions
            //
            const double m_BarTimeStep = 1.0;
            DateTime now = this.GetLocalTime();
            DateTime dt = now.AddMilliseconds(base.m_WaitListenUpdatePeriod / 2.0);
            if (dt.CompareTo(m_TimerSubscriptionNextUpdate) >= 0)
            {   // Now is within 1/2 period of ideal update time, so let's update now.
                //Log.NewEntry(LogLevel.Minor, "TimerSubscriberUpdate: {0}", m_TimerSubscriptionNextUpdate.ToString("HH:mm:ss.fff"));
                TimerSubscriptionUpdate();                                               // Call subscribers.

                
                // Compute the next time we should update.
                double totalSeconds = m_TimerSubscriptionNextUpdate.Second + m_TimerSubscriptionNextUpdate.Minute * 60; // seconds past the top of the hour.
                m_TimerSubscriptionNextUpdate = m_TimerSubscriptionNextUpdate.AddSeconds(-totalSeconds);
                totalSeconds = Math.Floor(totalSeconds / m_BarTimeStep) * m_BarTimeStep;	// rounding off.
                totalSeconds += m_BarTimeStep;
                m_TimerSubscriptionNextUpdate = m_TimerSubscriptionNextUpdate.AddSeconds(totalSeconds);
            }

        }// UpdatePeriodic()
        //
        //
        //
        // *************************************************************
        // ****             TimeSubscriptionUpdate()                ****
        // *************************************************************
        protected void TimerSubscriptionUpdate()
        {
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
                        // TODO: Provide the object type, perhaps engine and strategy name of exception throwing object.
                        // Get information about who threw exception.
                        Log.BeginEntry(LogLevel.Error, "UpdatePeriodic: Exception in subscriber");
                        if (m_TimeSubscribers[i] is StrategyEngines.PricingEngine)
                        {
                            StrategyEngines.PricingEngine engine = (StrategyEngines.PricingEngine)m_TimeSubscribers[i];
                            Log.AppendEntry(" Strategy: {1}. PricingEngine: {0}.", engine.EngineName, engine.ParentStrategy.Name);
                        }
                        else if (m_TimeSubscribers[i] is Engine)
                        {
                            Engine engine = (Engine)m_TimeSubscribers[i];
                            Log.AppendEntry(" Engine: {0}.", engine.EngineName);
                        }
                        else
                            Log.AppendEntry(" Type: {0}.", m_TimeSubscribers[i].GetType());
                        Log.AppendEntry(" Exception: {0}", e.Message);
                        Log.EndEntry();
                    }
                    i++;
                }
                m_Market.ExitReadBook(aBook);
            }

            // Check all strategies for updated quotes.
            //  We try to update all strategies here since we don't know whether
            //  any have changed their quoting during their timer update.
            foreach (Strategy strategy in m_Strategies.Values)
                strategy.UpdateQuotes();
            
            // Clear out spontaneously generated engine events from their event queues.
            if (this.EngineChanged != null) 
                CheckSpontaneousEngineEvents();                     // Send any events to subscribers.

            // Hack to ensure we don't get stuck when launching new strategies.
            if (m_StrategiesPendingLaunch.Count > 0)                // Check first strategy waiting to launch.
            {
                Log.NewEntry(LogLevel.Minor, "TimerSubscriptionUpdate: Attempting to launch {0} strategies. ", m_StrategiesPendingLaunch.Count);
                List<Strategy> strategies = new List<Strategy>(m_StrategiesPendingLaunch);
                foreach (Strategy strategy in strategies)
                    TryLaunchStrategy(strategy);                    // but its hear to make sure nothing gets stuck waiting forever.
            }


        }// TimerSubscriptionUpdate()
        //
        //
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
                    SetupBeginStrategies(newStrategies);
                else
                {
                    Log.NewEntry(LogLevel.Error, "ProcessRequestLoadStrategies: Failed to load strategies. Stopping.");
                    AppServices.GetInstance().Shutdown();
                    return;
                }
            }
            else if ((m_StrategyGroupIds!=null && m_StrategyGroupIds.Count>0) || (m_StrategyIds!=null && m_StrategyIds.Count>0))
            {
                Log.BeginEntry(LogLevel.Major, "ProcessRequestLoadStrategies: Loading from database ");
                if (m_StrategyGroupIds != null)
                {
                    Log.AppendEntry(" GroupdIds:");
                    foreach (int n in m_StrategyGroupIds)
                        Log.AppendEntry(" {0}", n.ToString());
                    Log.AppendEntry(". ");
                }
                if (m_StrategyIds != null)
                {
                    Log.AppendEntry(" StrategyIds:");
                    foreach (int n in m_StrategyIds)
                        Log.AppendEntry(" {0}", n.ToString());
                    Log.AppendEntry(". ");
                }
                Log.EndEntry();
                if (m_DBReaderWriter == null)
                {
                    Log.NewEntry(LogLevel.Error, "ProcessRequestLoadStrategies: Failed to locate a database reader. Cannot load strategies!");
                    return;
                }
                if (StrategyMaker.TryCreateFromDatabase(m_StrategyGroupIds, m_StrategyIds, this.Log, m_DBReaderWriter, out newStrategies) && newStrategies.Count > 0)
                {
                    Log.NewEntry(LogLevel.Error, "ProcessRequestLoadStrategies: Created {0} strategies. Starting to Setup Strategies.",newStrategies.Count);
                    SetupBeginStrategies(newStrategies);
                }
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
        // ****         SetupBeginStrategies()              ****
        // *****************************************************
        /// <summary>
        /// Given a collection of Strategies, this method adds them to our list
        /// and carefully initializes their engines.
        /// This has been separated from the LoadStrategies methods so that in the future
        /// we use this to request that Strategies be created on the fly.
        /// </summary>
        /// <param name="newStrategies">list of Strategy objects to accept</param>
        /// <returns></returns>
        private void SetupBeginStrategies(List<Strategy> newStrategies)
        {
            List<Strategy> strategiesToCompleteSetup = null;
            if (newStrategies == null)
            {
                Log.NewEntry(LogLevel.Major, "Setup Begin Strategies: No strategies created.");
                return;
            }

            // TODO: Create a StrategyGroup object that contains multiple strategies that must be initialized together.
            //      If any of the strategies in group are waiting to start, then all must wait to Setup / Launch.
            //      Useful for term structure models that need to know all their relatives are running.
            //      Alternatively, strategy can just wait until they find their relatives.
            // Initial Setup
            foreach (Strategy strategy in newStrategies)                
                strategy.SetupInitialize(this);                                 // Creates parameter tables and gui templates.                                   

            // Begin Setup
            foreach (Strategy strategy in newStrategies)
                strategy.SetupBegin(this);

            // Determine which strategies are ready to Complete Setup phase.
            foreach (Strategy strategy in newStrategies)
            {
                if (strategy.IsReadyForSetupComplete)
                {
                    if (strategiesToCompleteSetup == null)
                        strategiesToCompleteSetup = new List<Strategy>();
                    strategiesToCompleteSetup.Add(strategy);

                }
                else
                {
                    Log.NewEntry(LogLevel.Minor, "Setup Begin Strategies: Strategy #{0} {1} not ready for complete setup.", strategy.ID, strategy.Name);
                    if (!m_StrategiesPendingSetupComplete.ContainsKey(strategy.ID))
                        m_StrategiesPendingSetupComplete.Add(strategy.ID, strategy);
                }
            }


            // Call Setup
            if (strategiesToCompleteSetup != null && strategiesToCompleteSetup.Count > 0)
                SetupCompleteStrategies(strategiesToCompleteSetup);

        }// SetupInitializeStrategies()
        // 
        //
        //
        // *************************************************************
        // ****             SetupCompleteStrategies()               ****
        // *************************************************************
        /// <summary>
        /// Once Strategies are constructed and SetupInitialized(), their setup
        /// is completed and then they are launched.
        /// </summary>
        /// <param name="strategy"></param>
        /// <returns></returns>
        private void SetupCompleteStrategies(Strategy strategy)
        {
            m_Strategies.Add(strategy.ID, strategy);
            strategy.SetupComplete();

            // Setup is complete.  Lets announce our existance
            EngineEventArgs args = EngineEventArgs.ConfirmNewControls(this.ServiceName, strategy.ID);
            if (LoadEngineEventArgResponse(args))
                OnEngineChanged(args);

            Log.NewEntry(LogLevel.Minor, "SetupCompleteStrategies: Trying to launch {0}.", strategy.Name);
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
                EngineEventArgs args = EngineEventArgs.ConfirmNewControls(this.ServiceName, strategy.ID);
                if (LoadEngineEventArgResponse(args))
                    OnEngineChanged(args);
            }

            // Attempt to launch strategies.
            Log.NewEntry(LogLevel.Minor, "SetupCompleteStrategies: Trying to launch {0} strategies.", newStrategies.Count);
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
            bool isReadyToLaunch = true;                    // default is to assume Strategy is ready to launch...
            if (m_Market == null)
            {
                isReadyToLaunch = false;
            }
            else
            {
                // Test: Market instruments exist.
                Log.BeginEntry(LogLevel.Minor, "TryLaunchStrategy: {0}", strategy.Name);
                Book aBook;
                if (m_Market.TryEnterReadBook(out aBook))
                {
                    // Loop thru instruments for which Strategy wants subscriptions - MarketInstrumentChanged events, etc.
                    foreach (InstrumentName instrumentName in strategy.m_MarketInstrumentSubscriptions)
                    {
                        int instrID = -1;
                        // 
                        //if (m_Market.TryLookupInstrumentID(instrumentName, out instrID) && aBook.Instruments[instrID].DeepestLevelKnown >= 0)
                        if (m_Market.TryLookupInstrumentID(instrumentName, out instrID) && m_InstrumentSubscription.ContainsKey(instrID))
                        {   // We found the market instrument, and it has already been added to our subscription management list.                    

                        }
                        else
                        {   // We could not find the desired instrument in the MarketHub
                            Log.AppendEntry(" Waiting for market subscription {0}.", instrumentName);
                            isReadyToLaunch = false;
                        }
                    }// next instrumentName
                    // Loop thru instruments for which Strategy doesn't want subscription callbacks.
                    foreach (InstrumentName instrumentName in strategy.m_MarketInstrumentNonSubscriptions)
                    {
                        int instrID = -1;
                        //if (m_Market.TryLookupInstrumentID(instrumentName, out instrID) && aBook.Instruments[instrID].DeepestLevelKnown >= 0)
                        if (m_Market.TryLookupInstrumentID(instrumentName, out instrID) && m_InstrumentSubscription.ContainsKey(instrID))
                        {   // We found the market instrument, and it has already been added to our subscription management list.

                        }
                        else
                        {   // We could not find the desired instrument in the MarketHub
                            Log.AppendEntry(" Waiting for market {0}.", instrumentName);
                            isReadyToLaunch = false;
                        }
                    }// next instrumentName                  

                    m_Market.ExitReadBook(aBook);
                }
                else
                {   // Sometimes we check before any instruments have been subscribed to in the Market, and 
                    // so the MarketHub has yet to even create its Book.
                    Log.AppendEntry(" Failed to obtain market book.");
                    isReadyToLaunch = false;
                }

                // Test: Data loading subscriptions.
                if (m_StrategyPendingQueries.ContainsKey(strategy.ID) && m_StrategyPendingQueries[strategy.ID].Count > 0)// strategy.m_PendingQueries.Count > 0)
                {
                    Log.AppendEntry(" Waiting for {0} pending queries.", m_StrategyPendingQueries[strategy.ID].Count);// strategy.m_PendingQueries.Count);
                    isReadyToLaunch = false;
                }
            }

            //
            // Launch and exit.
            //
            if (isReadyToLaunch)
            {   // This strategy is ready!
                if (!strategy.IsLaunched)
                {   // it hasn't been launched yet!
                    Log.AppendEntry(" *** Launching! *** ");
                    strategy.IsLaunched = true;

                    if (m_StrategiesPendingLaunch.Contains(strategy))
                        m_StrategiesPendingLaunch.Remove(strategy);                     // remove from pending list, if its there.

                    // Complete market subscriptions.
                    Log.AppendEntry(" Subscriptions:");
                    foreach (InstrumentName instrName in strategy.m_MarketInstrumentSubscriptions)
                    {
                        int instrID = -1;
                        if (m_Market.TryLookupInstrumentID(instrName, out instrID))
                        {
                            if (!m_InstrumentSubscription.ContainsKey(instrID))          // Check whether this instrument has a subscribers list...
                            {
                                Log.AppendEntry(" (Creating InstrumentSubscription list for {0}. Warning!)", instrName);
                                m_InstrumentSubscription.Add(instrID, new List<int>());  // .. create one now.
                            }
                            m_InstrumentSubscription[instrID].Add(strategy.ID);         // Add this strategy to list of subscribers.  These Strategies get MarketInstrumentChanged events!
                            Log.AppendEntry(" {0}", instrName);
                        }
                    }

                    // Complete timer subscriptions
                    foreach (ITimerSubscriber subscriber in strategy.m_MyTimerSubscribers)
                        if(!m_TimeSubscribers.Contains(subscriber))                     // this is probably redudant, with the above check for launch but due to an error we have seen in prod I am adding it!
                            m_TimeSubscribers.Add(subscriber);

                    // Add to strategy pricing update list.
                    if (!m_StrategiesPricingRanked.Contains(strategy.ID))
                    {   // Need to add this to our list.
                        // TODO: insert this into the correct spot, immediately after its masters, for example.
                        m_StrategiesPricingRanked.Add(strategy.ID);
                    }
                    Log.EndEntry();                                                 // end logging our here, so strategy update logs appear after Launch log row, and not before it!

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
            }
            else if (!m_StrategiesPendingLaunch.Contains(strategy))
            {   // This strategy needs to be put into the pending list.
                Log.AppendEntry(" Waiting to launch strategy {0}.", strategy.Name);
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
                    DateTime now = GetLocalTime();
                    m_TimerSubscriptionNextUpdate = now.AddMilliseconds( -1 * now.Millisecond );
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
            if (eventArg.FoundInstrumentMarkets != null && eventArg.FoundInstrumentMarkets.Count > 0)
            {
                // These events are neccessary before Strategies can price themselves.
                Log.BeginEntry(LogLevel.Minor, "ProcessMarketFoundService: Found {0} instrument markets:", eventArg.FoundInstrumentMarkets.Count);
                foreach (InstrumentName instrumentName in eventArg.FoundInstrumentMarkets)
                    Log.AppendEntry(" {0}", instrumentName);
                Log.EndEntry();

                // Mark these instruments as known to us now.
                int instrID;
                foreach (InstrumentName instrumentName in eventArg.FoundInstrumentMarkets)
                    if (m_Market.TryLookupInstrumentID(instrumentName, out instrID))
                        if (m_InstrumentSubscription.ContainsKey(instrID) == false)
                            m_InstrumentSubscription.Add(instrID, new List<int>());

                // Check which Strategies depend on these newly found markets. 
                foreach (InstrumentName instrumentName in eventArg.FoundInstrumentMarkets)
                {
                    FoundMarketInstruments(instrumentName);
                }
            }
            //
            // Check for new mkt books.
            //
            if (eventArg.FoundInstrumentBooks != null && eventArg.FoundInstrumentBooks.Count > 0)
            {
                Log.BeginEntry(LogLevel.Minor, "ProcessMarketFoundService: Found {0} instrument books:", eventArg.FoundInstrumentBooks.Count);
                foreach (InstrumentName instrumentName in eventArg.FoundInstrumentBooks)
                    Log.AppendEntry(" {0}", instrumentName);
                Log.EndEntry();
            }
            //
            // Check for found instrument details.
            //
            if (eventArg.FoundInstruments != null && eventArg.FoundInstruments.Count > 0)
            {
                Log.BeginEntry(LogLevel.Minor, "ProcessMarketFoundService: Found {0} instrument details:", eventArg.FoundInstruments.Count);
                foreach (InstrumentName instrumentName in eventArg.FoundInstruments)
                    Log.AppendEntry(" {0}", instrumentName);
                Log.EndEntry();
            }
            //
            // Found products
            //
            if (eventArg.FoundProducts != null && eventArg.FoundProducts.Count > 0)
            {
                Log.BeginEntry(LogLevel.Minor, "ProcessMarketFoundService: Found {0} products:", eventArg.FoundProducts.Count);
                foreach (Product product in eventArg.FoundProducts)
                    Log.AppendEntry(" {0}", product);
                Log.EndEntry();
            }
        }//ProcessMarketFoundService()
        //
        //
        // ****             Found Market Instruments            ****
        //
        /// <summary>
        /// Called when new MarketInstruments have been created (with their markets) by MarketHub.
        /// This callback means that not only is the Instrument created, but its full initial market snapshot
        /// is already in the market book.
        /// </summary>
        /// <param name="instrumentName"></param>
        private void FoundMarketInstruments(InstrumentName instrumentName)
        {
            if (m_MarketInstrumentsOutstanding.Contains(instrumentName))
            {   // This is an instrument we've been waiting for!
                m_MarketInstrumentsOutstanding.Remove(instrumentName);
                List<Strategy> strategyToRelaunch = new List<Strategy>(m_StrategiesPendingLaunch); // Copy list since TryLaunchStrategy() manipulates m_StrategiesPendingLaunch list.
                foreach (Strategy strategy in strategyToRelaunch)
                {
                    if (strategy.m_MarketInstrumentSubscriptions.Contains(instrumentName) || strategy.m_MarketInstrumentNonSubscriptions.Contains(instrumentName)) 
                        TryLaunchStrategy(strategy);
                }
            }
        }//FoundMarketInstrument
        //
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
                if (!m_InstrumentSubscription.TryGetValue(instrID, out stratIDList))     // First time subscription call-backs.
                {   // This ocurrs when we get a market update prior to getting the market
                    // found event.  This should never happen within our new approach.
                    if (m_Market.TryEnterReadBook(out aBook))
                    {
                        m_InstrumentSubscription.Add(instrID, new List<int>());          // Create a new subscription list for this instrument.
                        InstrumentName instrName = aBook.Instruments[instrID].Name;     // name of instr with event
                        m_Market.ExitReadBook(aBook);
                        FoundMarketInstruments(instrName);
                    }
                }
                else
                {
                    // Mark strategies subscribed to this instrument for price updating.
                    foreach (int stratID in m_InstrumentSubscription[instrID])
                    {
                        m_Strategies[stratID].IsPricingUpdateRequired = true;  // market primary-market strategy changes!
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
        /*
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
        */
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
        /// <param name="eventArgs"></param>
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
                        if (strat.IsPricingUpdateRequired)
                        {
                            strat.MarketInstrumentChanged(aBook, eventArgs, isForceUpdate);     // updates the strategies, and will send orders, if needed.                
                            strat.IsPricingUpdateRequired = false;                              // reset the update required flag now.                            
                            strat.UpdateQuotes();
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
        //
        //
        #endregion// run-time processing methods


        #region Process Utility Events
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
        /// This is called when a query is completed and its result is passed 
        /// back to this StragetyHub inside the associated QueryBase.  
        /// We will pass it to the requesting object by invoking the method 
        /// that was given to us, associated with this particular QueryBase object.
        /// </summary>
        /// <param name="eventArg">Completed QueryBase</param>
        protected void ProcessCompletedQuery(Queries.QueryBase eventArg)
        {
            // We have received a query response.
            // Find if it belongs to one of my strategies.
            int strategyID = -1;
            foreach (KeyValuePair<int, List<int>> keyValue in m_StrategyPendingQueries)
                if (keyValue.Value.Contains(eventArg.QueryID))          // check whether this queryID is in this list of queryIDs.
                {
                    strategyID = keyValue.Key;                          // if it is, remember the strategyId that owns this list.
                    break;
                }
            if (strategyID == -1)
            {   // Query does not belong to my strategies! It may belong to another Hub.
                return;                                                 // Quietly exit.
            }

            // Remove this queryID from list of queries this strategy is waiting for.
            List<int> queryIdList;
            if (m_StrategyPendingQueries.TryGetValue(strategyID, out queryIdList))
            {
                queryIdList.Remove(eventArg.QueryID);                   // Remove queryId from pending list, since its complete.
                if (queryIdList.Count <= 0)                             // If there are none remaining, delete whole list.
                    m_StrategyPendingQueries.Remove(strategyID);
            }

            // The strategy is either in the PendingSetupComplete queue, waiting to launch, 
            // or, perhaps this strategy was already launched, in which case, its in the m_Strategies dict.
            // Get it and hold it in strategy pointer.
            Strategy strategy = null;
            if (m_StrategiesPendingSetupComplete.TryGetValue(strategyID, out strategy) == false)
                if (m_Strategies.TryGetValue(strategyID,out strategy)==false)
                    strategy = null;
            //
            // Fire the call back
            //
            EventHandler h;
            if (m_PendingQueryCallback.TryGetValue(eventArg.QueryID, out h))
            {
                m_PendingQueryCallback.Remove(eventArg.QueryID);        // Remove the call back since its complete.
                if (strategy != null)
                    Log.NewEntry(LogLevel.Minor, "ProcessCompletedQuery: Processing callback for strategy {0}.  StrategyHub has {1} queries outstanding.", strategy.Name, m_PendingQueryCallback.Count);
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
        /// This responds to engine event requests that are pushed onto 
        /// my queue.  These are usually "Requests" from Guis, etc.
        /// Communication (non requests) from other EngineHubs are processed 
        /// elsewhere.
        /// This is called by the internal hub thread.
        /// </summary>
        /// <param name="args"></param>
        private bool ProcessEngineEventRequest(EngineEventArgs args)
        {
            // Validate            
            bool isFireResponse = false;
            bool isUpdateRequired = false;

            // Process the event.
            int strategyID;
            switch (args.MsgType)
            {
                // *****************************
                // ****     Get Controls    ****
                // *****************************
                case EngineEventArgs.EventType.GetControls:
                    //  This is a request for all my strategies' controls.                                        
                    isFireResponse = LoadEngineEventArgResponse(args);
                    break;
                // *****************************
                // **** Parameter Change    ****
                // *****************************
                case EngineEventArgs.EventType.ParameterChange:     // A request from a user to change a parameter's value.                    
                    args.EngineHubName = this.ServiceName;          // tell them who responded
                    strategyID = args.EngineContainerID;            // parameter change requested for this strategy.
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
                    args.EngineHubName = this.ServiceName;      // tell them who responded
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
                    // At present we dont accept NewEngine requests from outsiders.
                    Log.NewEntry(LogLevel.Error, "ProcessEngineEvent: NewEngine request ignored.");
                    /*
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
                    */
                    break;
                // *****************************
                // ****     Save Engines    ****
                // *****************************
                case EngineEventArgs.EventType.SaveEngines:
                    args.EngineHubName = this.ServiceName;              // tell them who responded
                    List<int> strategyIdList = new List<int>();
                    if ( args.EngineContainerID < 0)
                        strategyIdList.AddRange( m_Strategies.Keys );   // user wants all xml
                    else
                        strategyIdList.Add(args.EngineContainerID);     // user wants one strategy.

                    // Here, it would be useful for the StrategyHub to respond
                    // directly to a single caller, handing the results back to him only.
                    // This is not possible in the current setup; responses are broadcasted to all.
                    args.EngineHubName = this.ServiceName;
                    args.Status = EngineEventArgs.EventStatus.Confirm;
                    if (args.DataObjectList == null)
                        args.DataObjectList = new List<object>();
                    args.DataObjectList.Clear();
                    foreach (int i in strategyIdList)
                    {
                        string s = Stringifiable.Stringify(m_Strategies[i], null, true);
                        args.DataObjectList.Add(s);
                    }
                    isFireResponse = true;
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
            if (isFireResponse) OnEngineChanged(args);
            return isUpdateRequired;
        }//ProcessEngineEvent().
        //
        //
        // *****************************************************
        // ****         Request Setup Complete()            ****
        // *****************************************************
        /// <summary>
        /// Strategies (or their engines) that need to do asynchronous calls
        /// during their SetupBegin phase, will inform me when they want
        /// to move on to the SetupComplete phase.  
        /// Of course, before moving on, I will need to check with all of the
        /// other engines to ensure that all are ready to advance.
        /// </summary>
        /// <param name="strategyID"></param>
        public void RequestSetupComplete(int strategyID)
        {
            if (m_StrategiesPendingSetupComplete.ContainsKey(strategyID))
            {
                if (m_StrategiesPendingSetupComplete[strategyID].IsReadyForSetupComplete)
                {
                    Strategy strategy = m_StrategiesPendingSetupComplete[strategyID];
                    m_StrategiesPendingSetupComplete.Remove(strategy.ID);
                    SetupCompleteStrategies(strategy);
                }
            }
        }//RequestSetupComplete()
        //
        //
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
                    args.EngineHubName = this.ServiceName;
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
            args.EngineHubName = this.ServiceName;      // tell them who responded
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
            if (isFireResponse) OnEngineChanged(args);
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
            OnEngineChanged(spontaneousEvents);
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
                else if (keyVal.Key.Equals("StrategyId", StringComparison.CurrentCultureIgnoreCase))
                {
                    string[] elems = keyVal.Value.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    m_StrategyIds = new List<int>();
                    foreach (string s in elems)
                    {
                        if (int.TryParse(s, out n))
                            m_StrategyIds.Add(n);
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
        public string ServiceName
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
        /// <param name="currentState"></param>
        protected void OnServiceStateChanged(ServiceStates prevState, ServiceStates currentState)
        {
            // Report the service change.
            ServiceStateEventArgs eventArg = new ServiceStateEventArgs(this, currentState, prevState);
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


        #region IEnginHub implementation
        // *************************************************************
        // ****                   IEnginHub                         ****
        // *************************************************************
        //
        //
        List<IEngineContainer> IEngineHub.GetEngineContainers()
        {
            List<IEngineContainer> engContainers = new List<IEngineContainer>();
            engContainers.AddRange(m_Strategies.Values);
            return engContainers;
        }
        //
        public event EventHandler EngineChanged;
        protected void OnEngineChanged(List<EngineEventArgs> eList)
        {
            if (EngineChanged != null)
            {
                foreach (EngineEventArgs e in eList) { EngineChanged(this, e); }
            }
        }
        public void OnEngineChanged(EventArgs e)
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
        #endregion//Event


    }//end class
}//end namespace
