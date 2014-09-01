using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.MarketHubs.Sims
{
    using UV.Lib.Application;
    using UV.Lib.Products;
    using UV.Lib.IO.Xml;
    using UV.Lib.Utilities;

    using UV.Lib.Hubs;
    using UV.Lib.MarketHubs;
    using DB = UV.Lib.DatabaseReaderWriters;
    using UV.Lib.BookHubs;
    /// <summary>
    /// This is a sim market hub.
    /// </summary>
    public class SimMarketHub : MarketHub , IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        //
        // Simulation controls
        //
        private uint m_UtcNow = 0;                            // This is the basic internal sim clock.
        private bool m_IsSimInitialized = false;
        private bool m_IsSimPlayingUser = true;                 // user switch.

        // Pending Requests
        private List<RequestEventArg<RequestCode>> m_PendingRequests = new List<RequestEventArg<RequestCode>>();
        private bool m_IsResubmitPendingRequests = false;
        private List<InstrumentName> m_PendingInstrumentQueries = new List<InstrumentName>();
        //private List<InstrumentName> m_PendingMarketDataQueries = new List<InstrumentName>();
        private List<DB.Queries.MarketDataQuery> m_PendingMarketDataQueries = new List<DB.Queries.MarketDataQuery>();

        //
        // Database resources
        //
        private DB.DatabaseInfo.DatabaseLocation m_DBLocation = DB.DatabaseInfo.DatabaseLocation.apastor;   // default 
        private DB.DatabaseInfo m_DBInfo = null;
        private DB.DatabaseReaderWriter m_DatabaseReader = null;
        // Database information       
        private DB.Queries.ExchangeInfoQuery m_ExchInfo = null;         // stored copy of table
        private DB.Queries.ProductInfoQuery m_ProductInfo = null;       // store copy of table.
        private bool m_IsDatabaseInitialized = false;                   // true when we can start processing mkt requests from users.
        // Market data item holders
        protected DateTime m_MktDataStartDateTime;                          // time for simulation start.
        protected double m_MktDataBlockSize = 1.0;                      // db query block size (in hours).

        private Dictionary<int, List<DB.Queries.MarketDataItem>> m_MarketItems = new Dictionary<int, List<DB.Queries.MarketDataItem>>();
        private Dictionary<int, int> m_MarketItemPtrs = new Dictionary<int, int>(); // instrID --> ptr

       private List<int>[] m_ChangedDepths = new List<int>[2];

        
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public SimMarketHub()
            : base("SimMarket", AppServices.GetInstance().Info.LogPath, false)
        {
            base.m_WaitListenUpdatePeriod = 1000;

            // Set a default market simulated time.
            m_MktDataStartDateTime = DateTime.Now.Date.AddHours(9.0);      // today at 9AM
            while (m_MktDataStartDateTime.DayOfWeek == DayOfWeek.Saturday || m_MktDataStartDateTime.DayOfWeek == DayOfWeek.Saturday)
                m_MktDataStartDateTime = m_MktDataStartDateTime.AddDays(-1.0);  // back up one more day.
            SetSimulatedTime( m_MktDataStartDateTime );
            m_ChangedDepths[MarketBase.BidSide] = new List<int> { 0 };
            m_ChangedDepths[MarketBase.AskSide] = new List<int> { 0 };
        }
        //
        //       
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        public bool IsSimPlaying
        {
            get { return m_IsSimInitialized && m_IsSimPlayingUser; }
            set 
            {
                m_IsSimPlayingUser = value;
            }
        }
        //
        //
        //
        public override DateTime LocalTime
        {
            get
            {
                double utcTimeNow = m_UtcNow;
                DateTime dt = QTMath.EpochToDateTime(utcTimeNow);
                dt = dt.ToLocalTime();
                return dt;
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
        // *****************************************
        // ****             Start()             ****
        // *****************************************
        /// <summary>
        /// This is called after all services exist, but before any services is connected.
        /// </summary>
        public override void Start()
        {
            this.HubEventEnqueue( m_Requests.Get(RequestCode.RequestServers) );
            base.Start();
        }//Start().
        //
        //
        //i
        //
        //
        #endregion//Public Methods


        #region Private HubEvent Processing
        // *****************************************************************
        // ****              Private HubEvent Processing                ****
        // *****************************************************************
        //
        /// <summary>
        /// This central event processing method.
        /// </summary>
        /// <param name="eventArgList"></param>
        protected override void HubEventHandler(EventArgs[] eventArgList)
        {
            foreach (EventArgs eventArgs in eventArgList)
            {
                Type eventType = eventArgs.GetType();
                if (eventType == typeof(RequestEventArg<RequestCode>))
                    ProcessRequest((RequestEventArg<RequestCode>)eventArgs);
                else if (eventArgs is DB.Queries.QueryBase)
                    ProcessQuery(eventArgs);
                    

            }//next eventArgs
            
        }// HubEventHandler()
        //
        //
        //
        // *************************************************
        // ****             Process Request()           ****
        // *************************************************
        private void ProcessRequest(RequestEventArg<RequestCode> eventArgs)
        {
            switch (eventArgs.RequestType)
            {
                case RequestCode.RequestServers:
                    // This is trigger automatically at Start() request.
                    // Here, we make all the conenctions necessary for collecting market data, info, etc.
                    
                    // Connect to the desired database.
                    if (m_DatabaseReader == null)
                    {
                        // Search for database service.
                        IService iService = null;
                        if (AppServices.GetInstance().TryGetService("DatabaseReaderWriter", out iService))
                        {   // Found database writer already in services.
                            m_DatabaseReader = (DB.DatabaseReaderWriter)iService;
                            m_DatabaseReader.QueryResponse += new EventHandler(this.HubEventEnqueue);
                        }
                        else
                        {   // Create db our own writer.
                            m_DBInfo = DB.DatabaseInfo.Create(m_DBLocation);
                            AppServices.GetInstance().TryAddService(m_DBInfo);
                            m_DatabaseReader = new DB.DatabaseReaderWriter(m_DBInfo);
                            m_DatabaseReader.QueryResponse += new EventHandler(this.HubEventEnqueue);
                            m_DatabaseReader.Start();
                        }
                        // Request info about exchanges and all products.
                        DB.Queries.QueryBase query = new DB.Queries.ExchangeInfoQuery();
                        m_DatabaseReader.SubmitAsync(query);
                        query = new DB.Queries.ProductInfoQuery();
                        m_DatabaseReader.SubmitAsync(query);
                    }
                    else
                        Log.NewEntry(LogLevel.Major, "ProcessRequest: Ignoring additional call to RequestServers.");
                    break;
                case RequestCode.RequestProducts:
                    if (!m_IsDatabaseInitialized)
                        m_PendingRequests.Add(eventArgs);
                    else
                    {
                        
                    }
                    break;
                case RequestCode.RequestInstruments:
                    if (!m_IsDatabaseInitialized)
                        m_PendingRequests.Add(eventArgs);
                    else
                    {
                        foreach (object o in eventArgs.Data)
                        {   // Validate this instrument request
                            if (! (o is InstrumentName))
                                continue;
                            InstrumentName instrName = (InstrumentName)o;
                            InstrumentDetails instrDetails;
                            if ( ! TryGetInstrumentDetails(instrName, out instrDetails) && m_PendingInstrumentQueries.Contains(instrName)!=true)
                            {   // Details not exist yet and we have not requested them yet.  Request them.
                                Log.NewEntry(LogLevel.Minor, "ProcessRequest: Requesting instr details for {0}.", instrName);
                                m_PendingInstrumentQueries.Add(instrName);// add to pending list (so we can't spam db).
                                DB.Queries.InstrumentInfoQuery query = new DB.Queries.InstrumentInfoQuery();
                                query.InstrumentName = instrName;
                                m_DatabaseReader.SubmitAsync(query);
                            }
                        }//next data object o
                    }
                    break;
                case RequestCode.RequestInstrumentPriceSubscription:
                    if (!m_IsDatabaseInitialized)
                        m_PendingRequests.Add(eventArgs);
                    else
                    {
                        foreach (object o in eventArgs.Data)
                        {
                            // Validate this instrument request
                            if (! (o is InstrumentName))
                                continue;
                            InstrumentName instrName = (InstrumentName)o;
                            int instrId = 0;
                            InstrumentDetails instrDetails;
                            if (TryLookupInstrumentID(instrName, out instrId) && instrId > -1)
                                continue;                               // instrument already has a book id!
                            // Get the isntrument details first, and make sure details exist.
                            if (! TryGetInstrumentDetails(instrName, out instrDetails))
                            {   // Details not exist yet.  Request them.
                                Log.NewEntry(LogLevel.Minor, "ProcessRequest: Need instr details for {0}. Delaying mkt subscription.", instrName);
                                m_PendingRequests.Add(eventArgs);       // push onto pending list for later resubmission.
                                base.RequestInstruments(instrName);                                
                            } 
                            else
                            {   
                                // Make sure we have not already requested this data.
                                bool isRequestAlreadyPending = false;
                                foreach (DB.Queries.MarketDataQuery query in m_PendingMarketDataQueries)
                                    if (query.InstrumentName == instrName)
                                    {
                                        isRequestAlreadyPending = true;
                                        break;
                                    }
                                if (isRequestAlreadyPending)
                                    Log.NewEntry(LogLevel.Major, "ProcessRequest: Market data for {0} pending. Ignore", instrName);
                                else
                                {   // This request is NOT already pending, so request data now.
                                    DB.Queries.MarketDataQuery query = new DB.Queries.MarketDataQuery();
                                    query.InstrumentName = instrName;
                                    query.StartDate = m_MktDataStartDateTime.ToUniversalTime(); // Query times in UTC.
                                    query.EndDate = query.StartDate.AddHours(m_MktDataBlockSize);
                                    m_PendingMarketDataQueries.Add(query);// add to pending list (so we can't spam db).
                                    Log.NewEntry(LogLevel.Major, "ProcessRequest: Requesting mkt data for {0} from {1} ({2} hours).", instrName,query.StartDate.ToString("MMM-dd-yyyy HH:mm:ss"),m_MktDataBlockSize);
                                    
                                    m_DatabaseReader.SubmitAsync(query);
                                }                                    
                            }
                        }
                    }

                    break;
                case RequestCode.RequestShutdown:
                    if (m_DatabaseReader != null)
                    {
                        m_DatabaseReader.RequestStop();
                        m_DatabaseReader = null;
                    }
                    base.Stop();
                    break;
                default:
                    Log.NewEntry(LogLevel.Major, "ProcessRequest: Unknown request.");
                    break;
            }//switch on request type.
        }// ProcessRequest()
        //
        //
        // *************************************************
        // ****             Process Query()           ****
        // *************************************************
        private void ProcessQuery(EventArgs eventArgs)
        {
            Type type = eventArgs.GetType();            
            if ( type == typeof(DB.Queries.ExchangeInfoQuery))
                m_ExchInfo = (DB.Queries.ExchangeInfoQuery)eventArgs;   // Just store this info for later.
            else if ( type == typeof(DB.Queries.ProductInfoQuery))
                m_ProductInfo = (DB.Queries.ProductInfoQuery)eventArgs; // just store this info for later.
            else if (type == typeof(DB.Queries.InstrumentInfoQuery))
            {                                                           // Extract instr details from this result.
                DB.Queries.InstrumentInfoQuery query = (DB.Queries.InstrumentInfoQuery)eventArgs;
                
                if (query.Status == DB.Queries.QueryStatus.Failed)
                    Log.NewEntry(LogLevel.Warning, "ProcessQuery: Failed to find instrument {0} in database.", query.InstrumentName);
                else if ( query.Results == null || query.Results.Count == 0)
                    Log.NewEntry(LogLevel.Warning, "ProcessQuery: Failed to create details for {0} from database.", query.InstrumentName);
                else 
                {   // Extract details from the query results                    
                    DB.Queries.InstrumentInfoItem item = query.Results[0];  // I ask for instruments one at a time.
                    InstrumentDetails details;
                    if (item.TryGetInstrumentDetails(query.InstrumentName, out details))
                    {
                        if (TryAddInstrumentDetails(query.InstrumentName, details))
                        {
                            Log.NewEntry(LogLevel.Minor, "ProcessQuery: Added instr details for {0}.", query.InstrumentName);
                            List<InstrumentName> instrNamesFound = new List<InstrumentName>();
                            instrNamesFound.Add(query.InstrumentName);
                            base.OnInstrumentFound(instrNamesFound);
                            m_IsResubmitPendingRequests = true;
                        }
                        else
                            Log.NewEntry(LogLevel.Warning, "ProcessQuery: Failed to add instr details for {0}.", query.InstrumentName);
                    }
                    else
                        Log.NewEntry(LogLevel.Warning, "ProcessQuery: Failed to create instr details for {0}.", query.InstrumentName);

                }
                // Mark this query as completed.
                if (m_PendingInstrumentQueries.Contains(query.InstrumentName))
                    m_PendingInstrumentQueries.Remove(query.InstrumentName);    // remove the query, it's finished.
            }
            else if (type == typeof(DB.Queries.MarketDataQuery))
            {
                DB.Queries.MarketDataQuery query = (DB.Queries.MarketDataQuery)eventArgs;
                int id;
                if (m_PendingMarketDataQueries.Contains(query))
                {
                    m_PendingMarketDataQueries.Remove(query);
                    if (query.Status == DB.Queries.QueryStatus.Failed)
                        Log.NewEntry(LogLevel.Warning, "ProcessQuery: Failed to find instrument {0} in database.", query.InstrumentName);
                    else if (TryLookupInstrumentID(query.InstrumentName, out id))
                    {   // Books for this instrument already exist.
                        // In future, this occurs when we ask for mode data for pre-existing instrument.
                        Log.NewEntry(LogLevel.Warning, "ProcessQuery: Query {1} for existing market {0}.", query.InstrumentName, query.QueryID);
                    }
                    else
                    {   // No book yet exists for this instrument.                     
                        if (TryCreateNewBook(query.InstrumentName))
                        {   // New book created.
                            Log.NewEntry(LogLevel.Major, "ProcessQuery: Created book for instrument {0}.", query.InstrumentName);
                            if (TryLookupInstrumentID(query.InstrumentName, out id))
                            {
                                if (query.Result == null || query.Result.Count < 10)
                                    Log.NewEntry(LogLevel.Warning, "ProcessQuery: Have no market data for instrument {0}.", query.InstrumentName);
                                // Create the MarketDataItem holder
                                m_MarketItems.Add(id, new List<DB.Queries.MarketDataItem>(query.Result));
                                m_MarketItemPtrs.Add(id, 0);                // set starting position to zero.
                                // If this is first instrument data call back, initial the sim variables.
                                if (!m_IsSimInitialized && m_MarketItems[id].Count > 0)
                                {
                                    m_UtcNow = m_MarketItems[id][0].UnixTime;
                                    DateTime dt = this.LocalTime;   // test
                                    m_IsSimInitialized = true;
                                }
                            }
                            // Fire book created event.
                            List<InstrumentName> booksCreated = new List<InstrumentName>();
                            booksCreated.Add(query.InstrumentName);
                            OnMarketBookCreated(booksCreated);
                        }
                        else
                            Log.NewEntry(LogLevel.Major, "ProcessQuery: Failed to create new book for {0}.", query.InstrumentName);
                    }
                }
            }

            //
            // Test for readiness
            //
            if (m_IsDatabaseInitialized == false && m_ExchInfo != null && m_ProductInfo != null)
            {
                m_IsDatabaseInitialized = true;
                Log.NewEntry(LogLevel.Minor, "ProcessQuery: Loading database tables complete. Submitting {0} pending requests.", m_PendingRequests.Count);
                while (m_PendingRequests.Count > 0)
                {
                    RequestEventArg<RequestCode> request = m_PendingRequests[0];
                    m_PendingRequests.RemoveAt(0);
                    this.HubEventEnqueue(request);
                }
            }



        }// ProcessQuery
        //
        //
        //
        //
        // *************************************************c
        // ****             UpdatePeriodic()            ****
        // *************************************************
        protected override void UpdatePeriodic()
        {            
            // Check pending requests.
            if (m_IsResubmitPendingRequests)
            {
                m_IsResubmitPendingRequests = false;
                Log.NewEntry(LogLevel.Minor, "UpdatePeriodic: Submitting {0} pending requests.", m_PendingRequests.Count);
                while (m_PendingRequests.Count > 0)
                {
                    this.HubEventEnqueue(m_PendingRequests[0]);
                    m_PendingRequests.RemoveAt(0);
                }
            }

            // Advance the simulation.            
            if (IsSimPlaying)
            {
                m_UtcNow++;
                PlayNextMarketUpdate(m_UtcNow);
            }
        }//UpdatePeriodic()
        //
        //
        //
        //
        #endregion//Private HubEvent Processing


        #region Private Market Updating
        // *****************************************************************
        // ****                 Private Book Processing                 ****
        // *****************************************************************
        //
        private List<int> m_InstrumentsToUpdate = new List<int>();
        //
        protected override InstrumentChangeArgs ProcessBookEventsForABook(int bookID, List<EventArgs> eArgList)
        {
            BookHubs.Book aBook = m_Book[bookID];
            List<InstrumentName> newMarketsFound = null;
            int ithInstr = 0;
            while (ithInstr < m_InstrumentsToUpdate.Count)
            {
                int instrId = m_InstrumentsToUpdate[ithInstr];
                BookHubs.Market mkt;
                if (!aBook.Instruments.TryGetValue(instrId, out mkt))
                {
                    ithInstr++;
                    continue;
                }
                DB.Queries.MarketDataItem mktItems = m_MarketItems[instrId][m_MarketItemPtrs[instrId]];
                bool isChanged = false;
                double p;
                int q;
                bool firstUpdate = mkt.DeepestLevelKnown < 0;
                for (int side=0; side<2; ++side)
                {
                    p = mktItems.Price[side];
                    q = mktItems.Qty[side];
                    if (p != mkt.Price[side][0] ||  q  != mkt.Qty[side][0] )
                    {
                        mkt.SetMarket(side, 0, p, q, mkt.Volume[side], 0);
                        isChanged = true;
                    }
                }
                if (!isChanged)
                {
                    m_InstrumentsToUpdate.RemoveAt(ithInstr);
                    if (firstUpdate)
                        mkt.DeepestLevelKnown = -1;                 // keep this -1 to remember we are not updated.
                }
                else
                {
                    if (firstUpdate)
                    {
                        if (newMarketsFound == null)
                            newMarketsFound = new List<InstrumentName>();
                        newMarketsFound.Add(mkt.Name);
                    }
                    ithInstr++;
                }
            }
            // New instruments found?
            if ( newMarketsFound != null)
                OnMarketInstrumentFound(newMarketsFound);

            InstrumentChangeArgs instrChangedArgs = new InstrumentChangeArgs();     // create change args for all instruments
            foreach (int instrId in m_InstrumentsToUpdate)
                instrChangedArgs.AppendChangedInstrument(instrId, m_ChangedDepths); // assume all changes are top of book
            return instrChangedArgs;
        }// ProcessBookEventsForABook()
        //
        //
        //
        // *************************************************
        // ****         PlayNextMarketUpdate()          ****
        // *************************************************
        /// <summary>
        /// Advance all timeseries until they are at (the end of) uTimeNow.
        /// </summary>
        /// <param name="uTimeNow"></param>
        private void PlayNextMarketUpdate(uint uTimeNow)
        {
            m_InstrumentsToUpdate.Clear();
            m_InstrumentsToUpdate.AddRange(m_MarketItemPtrs.Keys); // pre-load instrument Ids for updating.

            bool isEndOfData = false;
            int ithInstr = 0;
            while (ithInstr < m_InstrumentsToUpdate.Count )
            {
                int instrId = m_InstrumentsToUpdate[ithInstr];
                List<DB.Queries.MarketDataItem> mktItems = m_MarketItems[instrId];
                
                int nextPtr = m_MarketItemPtrs[instrId] + 1;        // ptr to tick beyond NOW.
                while (nextPtr < mktItems.Count && mktItems[nextPtr].UnixTime <= uTimeNow)
                    nextPtr++;                          // increment until the next tick is at later time.
               
                // Check for end of data exception.
                if (nextPtr >= mktItems.Count)
                    isEndOfData = true;

                // Update our list of which instruments has mkt event.
                int currentPtr = nextPtr - 1;
                if (currentPtr > m_MarketItemPtrs[instrId])
                {   // this instrument has had an update.
                    m_MarketItemPtrs[instrId] = currentPtr;
                    ithInstr++;                               // consider next instrument in list.
                }
                else
                    m_InstrumentsToUpdate.RemoveAt(ithInstr);
            }// next instrId
            if (isEndOfData)
            {
                throw new Exception("End of data.");
            }
            // Write some time for debugging
            TimeSpan ts = this.LocalTime.TimeOfDay;
            if ( ts.Seconds % 10 == 0)
                Log.NewEntry(LogLevel.Minor, "PlayNext [{0}]: ", this.LocalTime.TimeOfDay);
            ProcessBookEvents(null);


        }// PlayNextMarketUpdate()
        //
        // *********************************************
        // ****         Set Current Time()          ****
        // *********************************************
        /// <summary>
        /// Sets the internal sim clock time to dt.
        /// </summary>
        /// <param name="dt"></param>
        private void SetSimulatedTime( DateTime dt )
        {
            DateTime udt = dt.ToUniversalTime();
            double utime = Utilities.QTMath.DateTimeToEpoch(udt);
            m_UtcNow = (uint)Math.Floor(utime);

            DateTime localTime = this.LocalTime;//test

        }// SetCurrentTime()
        //
        //
        //
        #endregion//Private Book Processing


        #region no Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        #endregion//Event Handlers


        #region IStringifiable
        // *********************************************
        // ****             IStringifiable          ****
        // *********************************************
        public string GetAttributes()
        {
            return string.Empty;
        }
        public List<IStringifiable> GetElements()
        {
            return null;
        }
        public void SetAttributes(Dictionary<string, string> attributes)
        {
            bool isTrue;
            DateTime dt;
            foreach (KeyValuePair<string,string> keyVal in attributes)
            {
                if (keyVal.Key.Equals("ShowLog") && bool.TryParse(keyVal.Value, out isTrue))
                    Log.IsViewActive = isTrue;
                else if (keyVal.Key.Equals("Start") && DateTime.TryParse(keyVal.Value, out dt))
                {
                    m_MktDataStartDateTime = dt;
                    SetSimulatedTime( m_MktDataStartDateTime ); 
                }
            }
           
        }
        public void AddSubElement(IStringifiable subElement)
        {

        }
        #endregion// IStringifiable


    }//end class
}
