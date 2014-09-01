using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;

namespace Ambre.TTServices.Fills
{
    using TradingTechnologies.TTAPI.Tradebook;
    using TradingTechnologies.TTAPI;

    using InstrumentName = Misty.Lib.Products.InstrumentName;
    using Misty.Lib.Hubs;
    using Misty.Lib.Utilities;
    using Misty.Lib.IO.Xml;                         // for Istringifiable 
    using Misty.Lib.Application;

    using Ambre.TTServices;
    using Ambre.TTServices.Markets;


    /// <summary>
    /// This class monitors a collection of positions, one book for each instrument it receives a fill for.
    /// The hub backs up the fill information within drop files, etc.
    /// Methodology:
    /// After recieving fills from TT (accompanied with InstrumentDetails, and InstrumentKey), this hub automatically
    /// requests the associated Instrument object from the MarketHub.  
    /// TODO:
    ///     1. Automatically close and copy old drop files periodically, and write to new ones.
    ///     2. When neccessary, split this into a general base class and a super class that is TT specific.
    /// </summary>
    public class FillHub : Hub, IStringifiable, IService
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        protected static int FillHubID = 0;

        // Outside services and components
        private TTApiService m_TTService = null;
        protected MarketTTAPI m_MarketHub = null;
        public FillListener m_Listener = null;
        //private DropRules m_DropRules = null;                                             // place to drop our configuration, and raw fills.

        private DropSimple m_DropRules = null;
        protected EventWaitQueue m_EventWaitQueue = null;
        protected bool m_IsShuttingDown = false;

        // Identifications unique to a single FillHub instance.
        protected DateTime m_LocalTimeLastFill = DateTime.MinValue;                           // latest transaction time of fill received.
        public string Name = string.Empty;
        public string m_LastListenAccountFilter = string.Empty;

        // Controls for books.
        public DateTime NextResetDateTime = DateTime.Now;                                   // Next time to reset PnL.
        public TimeSpan ResetTimeOfDay = new TimeSpan(16, 30, 00);                          // 4:30 PM local time each day.

        // Fill book tables -multiple-thread accessed objects.
        protected ConcurrentDictionary<InstrumentKey, BookLifo> m_FillBooks = new ConcurrentDictionary<InstrumentKey, BookLifo>();
        protected ConcurrentDictionary<InstrumentName, IFillBook> m_IFillBooks = new ConcurrentDictionary<InstrumentName, IFillBook>();
        protected ConcurrentDictionary<InstrumentName, InstrumentMapEntry> m_BookNameMap = new ConcurrentDictionary<InstrumentName, InstrumentMapEntry>(); // used by outsiders.
        protected ConcurrentDictionary<InstrumentKey, InstrumentMapEntry> m_BookKeyMap = new ConcurrentDictionary<InstrumentKey, InstrumentMapEntry>();

        // Private objects for fills waiting for books, etc.
        protected Dictionary<InstrumentKey, SortedList<DateTime, FillEventArgs>> m_FillsWaitList = new Dictionary<InstrumentKey, SortedList<DateTime, FillEventArgs>>();
        protected Dictionary<InstrumentKey, SortedList<DateTime, FillEventArgs>> m_NewFillsList = new Dictionary<InstrumentKey, SortedList<DateTime, FillEventArgs>>();
        protected List<InstrumentName> m_InstrumentsWithoutBooks = new List<InstrumentName>(); // instruments for which we will NOT keep books (or they will be deleted later).
        protected bool m_IsInitializingBooks = false;
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public FillHub() : this(string.Format("FillHub{0}", System.Threading.Interlocked.Increment(ref FillHub.FillHubID)), false) { }          // IStringifiable constructor.
        //                  Note: User of FillHubID is to ensure that each log is uniquely labeled.
        //
        //
        public FillHub(string name, bool isLogViewVisible)                                  // name is used to distinguish between multiple fillhubs.
            : base(name, Misty.Lib.Application.AppInfo.GetInstance().LogPath, isLogViewVisible, LogLevel.ShowAllMessages)
        {
            this.m_WaitListenUpdatePeriod = 2000;           // msecs
            m_LocalTimeLastFill = Log.GetTime();
            if (string.IsNullOrEmpty(name) || name.Equals("*"))
                this.Name = string.Empty;
            else
                this.Name = name;

            m_EventWaitQueue = new EventWaitQueue(this.Log);
            m_EventWaitQueue.ResubmissionReady += new EventHandler(this.HubEventEnqueue);
        }
        //
        //       
        #endregion//Constructors



        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        public MarketTTAPI MarketHub
        {
            get { return m_MarketHub; }
            set
            {
                if (m_MarketHub != null)
                {
                    // Disconnect any current hub
                    m_MarketHub.FoundResource -= new EventHandler(this.HubEventEnqueue);
                    m_MarketHub.MarketStatusChanged -= new EventHandler(this.HubEventEnqueue);
                }

                // Connect to new hub.
                m_MarketHub = value;
                m_MarketHub.FoundResource += new EventHandler(this.HubEventEnqueue);
                m_MarketHub.MarketStatusChanged += new EventHandler(this.HubEventEnqueue);
            }
        }
        //
        public string ServiceName
        {
            get { return Name; }
        }
        //
        public TradeSubscriptionFilter ListenerTradeSubscriptionFilter
        {
            get { return m_Listener.TradeSubscriptionFilter; }
            set { m_Listener.TradeSubscriptionFilter = value; }
        }
        //
        //
        //
        #endregion//Properties



        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        // ****                 Connect()                       ****
        //
        /// <summary>
        /// This is called once immediately after the TT API Login has been authenticated.
        /// A listener is created to connect.
        /// </summary>
        public void Connect()
        {
            if (m_TTService.IsRunning)  // TODO: We should check whether ServiceState is connected, then we can connect.
                this.HubEventEnqueue(new Misty.Lib.OrderHubs.OrderHubRequest(Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestConnect));
        }
        public override void Start()
        {
            foreach (IService service in AppServices.GetInstance().GetServices(typeof(Markets.MarketTTAPI)))
            {
                Log.NewEntry(LogLevel.Minor, "Start: Found MarketTTAPI {0}. Using as my market hub.", service.ServiceName);
                m_MarketHub = (Markets.MarketTTAPI)service;
            }
            foreach (IService service in AppServices.GetInstance().GetServices(typeof(TTApiService)))
            {
                m_TTService = (TTApiService)service;
                Log.NewEntry(LogLevel.Minor, "Start: Found TTApiService {0}. ", service.ServiceName);
            }
            // Subscribe
            if (m_TTService != null)
                m_TTService.ServiceStateChanged += new EventHandler(TTService_ServiceStateChanged);

            base.Start();
        }//Start()
        //
        //
        //
        // ****                 Request()                       ****
        //
        public void Request(Misty.Lib.OrderHubs.OrderHubRequest request)
        {
            this.HubEventEnqueue(request);
        }
        //
        public bool TryAddIFillBook(InstrumentName instrumentName, IFillBook book)
        {
            bool isSuccess = false;
            if (!m_IFillBooks.ContainsKey(instrumentName))
            {
                if (m_IFillBooks.TryAdd(instrumentName, book))
                    isSuccess = true;
            }
            return isSuccess;
        }
        //
        //
        // ****             Try Enter Read Book()               ****
        // 
        public bool TryEnterReadBook(InstrumentName instrument, out IFillBook positionBook)
        {
            positionBook = null;
            IFillBook book;
            if (m_IFillBooks.TryGetValue(instrument, out book))
            {
                if (book.Lock.TryEnterReadLock(5))
                {
                    positionBook = book;
                    return true;
                }
                else
                {
                    Log.NewEntry(LogLevel.Major, "TryEnterReadBook: Timed out for read-lock for {0}.", instrument);
                    return false;
                }                                                           // Failed to find book for instrument.
            }
            else
            {
                //Log.NewEntry(LogLevel.Major, "TryEnterReadBook: Instrument {0} is unknown.", instrument);
                return false;
            }
        }//TryEnterReadBook()
        //
        //
        //
        // ****             Exit Read Book()               ****
        // 
        public void ExitReadBook(InstrumentName instrument)
        {
            IFillBook book;
            if (m_IFillBooks.TryGetValue(instrument, out book))
                book.Lock.ExitReadLock();
        }// ExitReadBook()
        public void GetInstrumentNames(ref List<InstrumentName> instrumentNames)
        {
            instrumentNames.AddRange(m_BookNameMap.Keys);
            foreach (InstrumentName instrumentName in m_IFillBooks.Keys)
            {
                IFillBook book = m_IFillBooks[instrumentName];
                if (book is CashBook)
                    instrumentNames.Add(instrumentName);
            }
        }
        public bool TryGetInstrumentKey(InstrumentName instrumentName, out InstrumentKey key)  // this is a TT specific method
        {
            key = new InstrumentKey();
            InstrumentMapEntry map = null;
            if (m_BookNameMap.TryGetValue(instrumentName, out map))
                key = map.Key;
            else
                return false;
            return true;
        }
        //
        public bool TryCheckExistenceOfInstrumentKey(InstrumentKey key)
        {
            foreach (InstrumentKey keyInBook in m_FillBooks.Keys)
            {
                if (TTConvert.IsTwoInstrumentEqual(keyInBook, key))
                    return true;
            }
            return false;
        }
        //
        public void CheckMakeUpFilterArgs(string filterName)
        {
            if (m_Listener.m_TradeFilterArg != filterName || string.IsNullOrEmpty(m_Listener.m_TradeFilterArg))
                m_Listener.m_TradeFilterArg = filterName;
        }
        //
        public void ResetFillListener()
        {
            FilterType filterType = m_Listener.m_FilterType;
            InstrumentKey instrumentKey = new InstrumentKey();
            if (filterType == FilterType.Instrument)
                instrumentKey = m_Listener.m_LastInstrumentKey;

            if (m_Listener != null)
            {
                m_Listener.Dispose();
                m_Listener = null;
            }

            m_Listener = new FillListener("FillListener");

            if (filterType == FilterType.Account)
                m_Listener.FilterByAccount(m_LastListenAccountFilter);
            if (filterType == FilterType.Instrument && instrumentKey != null)
                m_Listener.FilterByInstrument(instrumentKey);

            this.HubEventEnqueue(new Misty.Lib.OrderHubs.OrderHubRequest(Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestFillListenerReset));
        }
        //
        public override void RequestStop()
        {
            this.HubEventEnqueue(new Misty.Lib.OrderHubs.OrderHubRequest(Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestShutdown));
        }//Stop()

        //
        //
        #endregion//Public Methods



        #region Hub Event Handler and Processing
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        //
        // ****                 Hub Event Handler()                     ****
        /// <summary>
        /// Called by this hub thread.
        /// </summary>
        protected override void HubEventHandler(EventArgs[] eventArgList)
        {
            if (eventArgList == null)
                return;
            foreach (EventArgs eventArg in eventArgList)
            {
                if (eventArg == null) continue;
                Type eventType = eventArg.GetType();
                try
                {
                    if (eventType == typeof(FillEventArgs))
                        ProcessFill((FillEventArgs)eventArg);                                                      // Events from FillListener.
                    else if (eventType == typeof(FillListener.StatusChangedEventArgs))
                        ProcessFillListenerStatusChange((FillListener.StatusChangedEventArgs)eventArg);
                    else if (eventType == typeof(Misty.Lib.MarketHubs.FoundServiceEventArg))
                        ProcessFoundMarketServiceResource((Misty.Lib.MarketHubs.FoundServiceEventArg)eventArg);    // MarketHub found resources.
                    else if (eventType == typeof(Misty.Lib.MarketHubs.MarketStatusChangedEventArg))
                        ProcessMarketStatusChanged((Misty.Lib.MarketHubs.MarketStatusChangedEventArg)eventArg);    // Market status change.
                    else if (eventType == typeof(Misty.Lib.OrderHubs.OrderHubRequest))
                        ProcessRequest((Misty.Lib.OrderHubs.OrderHubRequest)eventArg);
                    else if (eventType == typeof(RejectedFills.RejectedFillEventArgs))
                        ProcessRejectedFill((RejectedFills.RejectedFillEventArgs)eventArg);
                    else
                        Log.NewEntry(LogLevel.Warning, "HubEventHandler: Unknown event arg {0} not implemented.", eventArg.ToString());
                }
                catch (Exception ex)
                {
                    this.Log.Flush();
                    System.Windows.Forms.DialogResult result = Misty.Lib.Application.ExceptionCatcher.QueryUserTakeAction(ex, this, eventArg);
                }
            }
        }//HubEventHandler()
        //
        private void TTService_ServiceStateChanged(object sender, EventArgs eventArgs)
        {
            this.HubEventEnqueue(new Misty.Lib.OrderHubs.OrderHubRequest(Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestConnect));
        }
        //
        //
        // ****                 Process Fill()                  ****
        //
        /// <summary>
        /// Called by this hub thread.
        /// </summary>
        protected void ProcessFill(FillEventArgs eventArg)
        {
            //m_DropRules.EnqueueArchiveFill(eventArg);

            if (m_IsInitializingBooks)                                              // we are still processing initial fills.
            {
                if (eventArg.Type == FillType.New)
                    AddToNewFillsList(eventArg);
                return;
            }
            if (eventArg.Fill.LocalTime.CompareTo(m_LocalTimeLastFill) > 0)
                m_LocalTimeLastFill = eventArg.Fill.LocalTime;                      // store the latest transaction time.

            InstrumentKey key = eventArg.TTInstrumentKey;
            BookLifo lifoBook;
            if (m_FillBooks.TryGetValue(key, out lifoBook))                         // Books-dictionary key is TT's InstrumentKey.  Faster since key is in hand.
                AddFillToBook(lifoBook, eventArg);                                  // We have a book for this instrument key.
            else
                AddToNewFillsList(eventArg);                                        // No book for this key?  May be the first fill for this key?

        }//ProcessFill()
        //
        //
        //
        //
        //
        //
        /// <summary>
        /// User has resubmitted a rejected fill for acceptance.
        /// </summary>
        /// <param name="eventArg"></param>
        protected void ProcessRejectedFill(RejectedFills.RejectedFillEventArgs eventArg)
        {
            if (m_IsInitializingBooks)
                m_EventWaitQueue.Add(eventArg);
            else
            {
                FillEventArgs fillEvent = eventArg.OriginalFillEventArg;
                fillEvent.Type = FillType.UserAdjustment;
                InstrumentKey key = fillEvent.TTInstrumentKey;
                BookLifo lifoBook;
                if (m_FillBooks.TryGetValue(key, out lifoBook))
                {
                    AddFillToBook(lifoBook, fillEvent);
                    OnFillRejectionsUpdated(eventArg);                              // inform subscribers of change to rejection lists.
                }
                else
                    AddToNewFillsList(fillEvent);
            }
        }
        //
        //
        //
        //
        // ****                 Process Found Market Service Resource()                ****
        //
        /// <summary>
        /// Whenever the market server discovers a new resource (a market, a product, or instrument), we get call back.
        /// 1) new instruments: When "new instruments" arrive, check to see if we have an associated book already.
        ///     This happens when FillBooks have been loaded from an initialization save file. 
        ///     If so, update/confirm book's tick information.  
        ///     On the other hand, if there's no book for the instrument, it may be for an instrument that we got a fill 
        ///     for and are planning to create a new book.  In this case, the next time we retry RequestCreateBook, 
        ///     we will find the instrument details from the new instrument, and create the book at that point.
        /// 2) new markets: ignore.
        /// 3) new products: ingnore.
        /// Called by hub thread only.  
        /// </summary>
        protected void ProcessFoundMarketServiceResource(Misty.Lib.MarketHubs.FoundServiceEventArg eventArg)
        {
            if (eventArg.FoundInstruments != null && eventArg.FoundInstruments.Count > 0)
            {   // New instruments found.  We are guaranteed by Mkt hub that the InstrumentName is unique.
                Log.BeginEntry(LogLevel.Minor, "MarketResoursesFound: Instruments ");
                foreach (Misty.Lib.Products.InstrumentName instr in eventArg.FoundInstruments)
                {
                    Log.AppendEntry("[{0}", instr);
                    InstrumentDetails details;
                    if (m_MarketHub.TryLookupInstrumentDetails(instr, out details))
                    {   // Mkt has provided all mkt details for this instrument.
                        InstrumentKey ttKey = details.Key;
                        BookLifo book;
                        if (!m_FillBooks.TryGetValue(ttKey, out book))
                        {   // This is a instrument for a book we don't have.  Lets ignore it for the moment. 
                            // If there is a FillBook creation request waiting, we will get to it later.
                            Log.AppendEntry(" expiry={0}", details.ExpirationDate.ToDateTime().ToShortDateString());
                            Log.AppendEntry(" no book, ignore for now]");
                            continue;       // continue to next instrument found.
                        }
                        // We already have a book for this instrument (probably loaded during startup). 
                        // First, verify all the instr-key mappings.  If good, then check/update the mkt details for the book.
                        InstrumentMapEntry mapEntry1;
                        InstrumentMapEntry mapEntry2;
                        if (m_BookKeyMap.TryGetValue(ttKey, out mapEntry1) && m_BookNameMap.TryGetValue(mapEntry1.Name, out mapEntry2))
                        {   // The above conditions should be trivial, since we add entries into the maps and FillBook list to satisfy the above.
                            // However, the only problem may be that InstrumentNames might be inconsistent with the new one we received.
                            // This would happen if TTKey is reused today for a different instrument than it was yesterday.
                            // For example, the on-the-run 2-year cash note 2_Year, is a different instrument each 2-yr auction.
                            if (instr == mapEntry1.Name && mapEntry2.Key == ttKey)
                            {   // Everything is perfectly self-consistent
                                Log.AppendEntry(" consistent book exists");
                                //  Now check/update the book we have.                                
                                Log.AppendEntry(" key={0}", ttKey);
                                double minTickSize = Convert.ToDouble(details.TickSize.Numerator) / Convert.ToDouble(details.TickSize.Denominator);      // .TickSize is the minimum price we might get (like from a spread during roll.)
                                double smallestTickSize = details.SmallestTickIncrement;
                                int actualTickSize = details.ActualTickSize;
                                double dollarAmount = details.TickValue;
                                double dollarPerPoint = dollarAmount / minTickSize;
                                string currencyName = details.Currency.Code;

                                if (book.DollarPerPoint != dollarPerPoint)
                                {
                                    Log.AppendEntry(" DollarPerPt {0} -> {1}", book.DollarPerPoint, dollarPerPoint);
                                    book.DollarPerPoint = dollarPerPoint;
                                }
                                if (book.SmallestFillPriceIncr != minTickSize)
                                {
                                    Log.AppendEntry(" SmallestFillPriceIncr {0} -> {1}", book.SmallestFillPriceIncr, minTickSize);
                                    book.SmallestFillPriceIncr = minTickSize;
                                }
                                if (details.ExpirationDate.ToDateTime().CompareTo(DateTime.Now) < 0)
                                {   // This instrument has expired!
                                    Log.AppendEntry(" Instrument has expired! Expry={0}, mark it for deletion.", details.ExpirationDate.ToDateTime().ToString(Strings.FormatDateTimeZone));
                                    if (!m_InstrumentsWithoutBooks.Contains(book.Name))
                                        m_InstrumentsWithoutBooks.Add(book.Name);                      // mark this book to NOT be saved going forward.
                                }
                            }
                            else
                            {   // Inconsistent mappings.  
                                // Problem: The Market has assigned InstrumentName to TTKey that is different then what we already have.
                                // Since this TTKey now seems to be referring to a different instrument, we must go with it!
                                Log.AppendEntry(" inconsistent mappings!");
                                Log.AppendEntry(" ttKey={0} Name={1}", ttKey, instr);  // from current market listings
                                Log.AppendEntry(" MapEntry1=({0},{1})", mapEntry1.Key, mapEntry1.Name);
                                Log.AppendEntry(" MapEntry2=({0},{1})", mapEntry2.Key, mapEntry2.Name);
                                if (TryDeleteBook(ttKey))
                                {
                                    Log.AppendEntry(" Removed previous empty book.", mapEntry2.Key, mapEntry2.Name);

                                    // Trigger book destruction event - for original book
                                    PositionBookChangedEventArgs newEventArg = new PositionBookChangedEventArgs();
                                    newEventArg.Instrument = mapEntry1.Name;            // original book name.
                                    newEventArg.Sender = this;
                                    OnPositionBookDeleted(newEventArg);

                                    // Keep the old book, if its not empty.                             
                                    if (book.NetPosition != 0)
                                    {
                                        int n = 0;
                                        InstrumentKey newKey = new InstrumentKey(ttKey.ProductKey, string.Format("{0}##{1}", ttKey.SeriesKey, n));
                                        InstrumentName newName = new InstrumentName(book.Name.Product, string.Format("{0}##{1}", book.Name.SeriesName, n));
                                        while (m_BookKeyMap.ContainsKey(newKey) || m_BookNameMap.ContainsKey(newName))
                                        {
                                            n++;
                                            newKey = new InstrumentKey(ttKey.ProductKey, string.Format("{0}##{1}", ttKey.SeriesKey, n));
                                            newName = new InstrumentName(book.Name.Product, string.Format("{0}##{1}", book.Name.SeriesName, n));
                                        }
                                        // Add new book.
                                        if (m_FillBooks.TryAdd(newKey, book) && m_IFillBooks.TryAdd(newName, book))
                                        {
                                            book.Name = newName;
                                            m_BookKeyMap.TryAdd(newKey, new InstrumentMapEntry(newName, newKey));
                                            m_BookNameMap.TryAdd(newName, new InstrumentMapEntry(newName, newKey));
                                        }

                                        // Trigger book creation event - for new copy of original book
                                        newEventArg = new PositionBookChangedEventArgs();
                                        newEventArg.Instrument = newName;
                                        newEventArg.Sender = this;
                                        OnPositionBookCreated(newEventArg);
                                    }
                                }
                            }
                            Log.AppendEntry("]");
                        }
                        else
                            Log.AppendEntry(" error in BookKeyMap.]");
                    }
                    else
                        Log.AppendEntry(" Error: Missing mkt details!]");
                }
                Log.EndEntry();
            }
        }//ProcessFoundMarketServiceResource()   
        //
        //
        // *************************************************************************
        // ****                 Process Market Status Changed()                 ****
        // *************************************************************************
        /// <summary>
        /// When the state of the market changes, we get a call back here.
        /// </summary>
        /// <param name="eventArg"></param>
        protected void ProcessMarketStatusChanged(Misty.Lib.MarketHubs.MarketStatusChangedEventArg eventArg)
        {
            Log.BeginEntry(LogLevel.Major, "ProcessMarketStatusChanged: ");
            foreach (string mktName in eventArg.MarketNameList)
                Log.AppendEntry("{0} ", mktName);
            Log.EndEntry();
        }//ProcessMarketStatusChanged()
        //
        //
        // *************************************************************************
        // ***                      Process Request                             ****
        // *************************************************************************
        /// <summary>
        /// These are request for fill hub resources.  Some can not be completed yet, and these
        /// will be stored in a Queue for retrying later.
        /// </summary>
        protected void ProcessRequest(Misty.Lib.OrderHubs.OrderHubRequest eventArg)
        {
            if (eventArg.Request == Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestDropCopyNow)
            {
                if (m_DropRules != null)
                    m_DropRules.StartNewDropArchive();
            }
            else if (eventArg.Request == Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestDropCopyArchive)
            {
                if (m_DropRules != null)
                    m_DropRules.StartNewDropArchive();                                      // I archive drop copies on every fillbook drop now.
            }
            else if (eventArg.Request == Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestConnect)
            {   // Connecting is first thing we do after TTAPI user is logged in (session exists) and hub thread is started.                
                // Connection procedure: 
                //  1. Read local configuration file, if present
                //  2. Load saved fill books
                //  3. Start listener, we start getting historic fills; sometimes these are slow comming in.
                //LoadConfigFile();

                if (m_Listener == null)                                                     // Used to detect whether we've already connected.
                {
                    m_TTService = TTApiService.GetInstance();                               // Get TT API service.
                    Log.NewEntry(LogLevel.Minor, "Connected to TTAPI user: {0}.", m_TTService.session.UserName);
                    LoadSavedBooks();                                                       // Load saved books, and fills.

                    if (m_Listener == null)
                    {
                        Log.NewEntry(LogLevel.Major, "Loading default style FillListener.");
                        m_Listener = new FillListener("FillListener");                      // create a generic fill listener
                    }

                    m_Listener.Log = Log;
                    m_Listener.Filled += new EventHandler(HubEventEnqueue);
                    m_Listener.StatusChanged += new EventHandler(HubEventEnqueue);          // can get all downloads at end of download here.
                    m_Listener.Start();                                                     // this will initiate the historical download (and new fills).
                    // After the historical downloads are complete, FillListener.StatusChanged will fire, and from that event handler, 
                    // this hub continues to load the initial and historical fills.
                }
            }
            else if (eventArg.Request == Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestFillListenerReset)
            {
                // Reconnect to TT after problem is found.
                m_Listener.Log = Log;
                m_Listener.Filled += new EventHandler(HubEventEnqueue);
                m_Listener.StatusChanged += new EventHandler(HubEventEnqueue);
                m_Listener.Start();
            }
            else if (eventArg.Request == Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestShutdown)
            {
                if (m_DropRules != null)
                    m_DropRules.Stop();
                m_IsShuttingDown = true;                          // shutdown ocurrs during periodic update.
                //Shutdown();
            }
            else if (eventArg.Request == Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestCompleteBookReset)
            {
                ResetAllFillBooks(eventArg.Data, true);           // optionally, user can request RealPnLs to be set to values in Data[], or this can be a null.
                //m_DropRules.Enqueue(this);
            }
            else if (eventArg.Request == Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestRealPnLReset)
            {
                //m_DropRules.Enqueue(this);
                ResetAllFillBooks(eventArg.Data);                 // optionally, user can request RealPnLs to be set to values in Data[], or this can be a null.
                //m_DropRules.Enqueue(this);
            }
            else if (eventArg.Request == Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestCreateUpdateCashBook)
            {
                // The input data object should be respectively cash instrument name, currency code, currency rate, reallized PnL and starting realized PnL.
                InstrumentName newCashInstr = (InstrumentName)eventArg.Data[0];
                string currencyName = (string)eventArg.Data[1];
                double currencyRate = (double)eventArg.Data[2];
                double realizedPnL = (double)eventArg.Data[3];
                double startRealizedPnL = (double)eventArg.Data[4];
                IFillBook adjustedCashBook;

                if (!TryEnterReadBook(newCashInstr, out adjustedCashBook))
                {
                    // This block will try to create a new cash book if there is no before.
                    CashBook newBook = new CashBook(1, 1, newCashInstr);
                    newBook.CurrencyName = currencyName;
                    newBook.CurrencyRate = currencyRate;
                    newBook.RealizedDollarGains += realizedPnL;
                    newBook.RealizedStartingDollarGains += startRealizedPnL;
                    TryAddIFillBook(newCashInstr, newBook);

                    // Triger new cash book created event.
                    FillHub.PositionBookChangedEventArgs newEventArg = new FillHub.PositionBookChangedEventArgs();
                    newEventArg.Instrument = newCashInstr;
                    newEventArg.Sender = this;
                    OnPositionBookCreated(newEventArg);
                }
                else
                {
                    // This block will update/adjust the values in the found cash book if there is one before.
                    adjustedCashBook.RealizedDollarGains += realizedPnL;
                    adjustedCashBook.RealizedStartingDollarGains += startRealizedPnL;

                    // Triger the event that the cash book has been changed.
                    FillHub.PositionBookChangedEventArgs updateEventArg = new FillHub.PositionBookChangedEventArgs();
                    updateEventArg.Instrument = newCashInstr;
                    updateEventArg.Sender = this;
                    OnPositionBookChanged(updateEventArg);
                    ExitReadBook(newCashInstr);
                }
            }
            else if (eventArg.Request == Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestCreateFillBook)
            {
                InstrumentKey instrumentKey = (InstrumentKey)eventArg.Data[0];              // Extract instrKey we want to collect fills for.
                InstrumentName instrumentName;
                if (m_MarketHub.TryLookupInstrument(instrumentKey, out instrumentName))     // See if market knows this instrument.
                {
                    if (IsInstrumentDesired(instrumentName))                                // Determines our interest in this instr from its type, etc.
                    {   // We want to create a book for this instrument.
                        BookLifo book;
                        if (TryCreateNewBook(instrumentName, instrumentKey, out book))
                        {                                                                   // Book was successfully created.
                            Log.BeginEntry(LogLevel.Warning, "ProcessRequest: Created new book {0}.", book.Name);
                            SortedList<DateTime, FillEventArgs> fills;                      // process fills we may have loaded in the NewFills list.
                            if (m_FillsWaitList.TryGetValue(instrumentKey, out fills))
                            {
                                Log.AppendEntry(" {0} init fills.", fills.Count);
                                foreach (FillEventArgs e in fills.Values)                   // Consider historic/initial fills first!
                                    AddFillToBook(book, e);
                                fills.Clear();                                              // All these have been processed.
                            }
                            if (m_NewFillsList.TryGetValue(instrumentKey, out fills))
                            {
                                Log.AppendEntry(" {0} new fills.", fills.Count);
                                foreach (FillEventArgs e in fills.Values)
                                    AddFillToBook(book, e);
                                fills.Clear();
                            }
                            Log.AppendEntry(" Final book {0}.", book);
                            Log.EndEntry();
                            // Fire book creation event
                            PositionBookChangedEventArgs newEventArg = new PositionBookChangedEventArgs();
                            newEventArg.Instrument = instrumentName;
                            newEventArg.Sender = this;
                            OnPositionBookCreated(newEventArg);
                        }
                        else
                        {
                            Log.NewEntry(LogLevel.Warning, "ProcessRequest: TryCreateNewBook failed for {0}.", instrumentName);
                            m_EventWaitQueue.Add(eventArg);                                 // push back and try later.
                        }
                    }
                    else
                    {
                        Log.NewEntry(LogLevel.Warning, "ProcessRequest: No fill book is desired for Instrument {0}. Create book request aborted.", instrumentName);
                        SortedList<DateTime, FillEventArgs> fills;                          // process fills we may have loaded in the NewFills list.
                        if (m_FillsWaitList.TryGetValue(instrumentKey, out fills))
                        {
                            Log.BeginEntry(LogLevel.Warning, "ProcessRequest: Dropping all fills from Wait list for {0}", instrumentName);
                            foreach (FillEventArgs e in fills.Values)
                                Log.AppendEntry(" {0}", e);
                            Log.EndEntry();
                            fills.Clear();                                                  // remove all these from Waiting, but keep the name entry, to remind us that we don't want the book.
                        }
                        if (!m_InstrumentsWithoutBooks.Contains(instrumentName))            // store this to remind that we do not want this instrument.
                            m_InstrumentsWithoutBooks.Add(instrumentName);
                    }
                }
                else
                {
                    // The instrumentKey is unknown to the market.  Request for information about this instrument before we try to build a book for it.
                    ProductType keyType = instrumentKey.ProductKey.Type;
                    if (keyType == ProductType.Algo || keyType == ProductType.AutospreaderSpread)
                    {
                        Log.NewEntry(LogLevel.Minor, "ProcessRequest: Market doesnt know {0}. TT ProductType {1}. We will ignore it.", instrumentKey, keyType);
                    }
                    else
                    {
                        Log.NewEntry(LogLevel.Minor, "ProcessRequest: Market doesnt know {0}.  Requesting it from Market.", instrumentKey);
                        m_MarketHub.RequestInstruments(instrumentKey);
                        m_EventWaitQueue.Add(eventArg);                                     // Store CreateBook request, try again later - will trigger another request for info from Market.
                    }
                }
            }
            else if (eventArg.Request == Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestDeleteBook)
            {
                InstrumentName name = (InstrumentName)eventArg.Data[0];
                if (TryDeleteBook(name))
                {
                    // Fire book destruction event
                    PositionBookChangedEventArgs newEventArg = new PositionBookChangedEventArgs();
                    newEventArg.Instrument = name;
                    newEventArg.Sender = this;
                    OnPositionBookDeleted(newEventArg);
                }
            }
            else
                Log.NewEntry(LogLevel.Major, "ProcessRequest: This request type {0} is not implemented.", eventArg);
        }// ProcessRequest()
        //
        //
        // *********************************************************************
        // ****             Process Fill Listener Status Change             ****
        // *********************************************************************
        /// <summary>
        /// This responds to the FillListener event that its state has changed.  It will 
        /// guess (it may be wrong) that all downloaded historic trades are completed, and fire this event.
        /// Everytime we receive a complete set of historic downloads (even if not the last set) we process them.
        /// </summary>
        /// <param name="eventArg"></param>
        private void ProcessFillListenerStatusChange(FillListener.StatusChangedEventArgs eventArg)
        {
            Log.NewEntry(LogLevel.Minor, "ProcessFillListenerStatusChange: Status change {0}.", eventArg);
            if (eventArg.NewStatus == FillListener.Status.DownLoadingStarted)
            {
                m_IsInitializingBooks = true;
            }
            else if (eventArg.NewStatus == FillListener.Status.DownLoadsCompleted)
            {
                m_IsInitializingBooks = false;                                              // we *think* we are finished with fill downloads

                List<FillEventArgs> historicalFills = null;
                if (eventArg.Data != null)
                    historicalFills = (List<FillEventArgs>)eventArg.Data;
                //foreach (FillEventArgs e in historicalFills)
                //m_DropRules.EnqueueArchiveFill(e);//.AddToDropQueue(Stringifiable.Stringify(e));              // drop copy
                ValidateHistoricFills(historicalFills);
            }
        }// ProcessFillListenerStatusChange()
        //
        //
        //
        /// <summary>
        /// This must be called by the local hub thread.
        /// The data expected is a n sets of 3-tuples = {InstrumentName,dailyRealPnL,startingRealPnL}.
        /// If, the InstrumentName entry is a null, then all books are reset.
        /// If, either of the (double) dailyRealPnL values are null, then that value is left alone.
        /// </summary>
        /// <param name="data">3-dim array of hubname and PnLs (doubles) or null for simple reset.</param>
        private void ResetAllFillBooks(object[] data, bool eraseAllBooks = false)
        {
            // Procedure:  
            if (data != null && data.Length > 0)    // if data is null/empty, user wants to do a usual "daily reset all book"
            {
                int ptr = 0;
                while (ptr < data.Length)
                {
                    //
                    // InstrumentName
                    //
                    bool isInstrumentProvided = false;
                    InstrumentName instrumentName = new InstrumentName();
                    if (data[ptr] != null)
                    {   // Use has provided an instrument name
                        isInstrumentProvided = true;
                        if (data[ptr] is InstrumentName)
                            instrumentName = (InstrumentName)data[ptr];
                        else
                        {
                            Log.NewEntry(LogLevel.Major, "ResetFillBooks: InstrumentName {0} not satisfactory. Exiting.", data[ptr]);
                            return;
                        }
                    }
                    else
                    {   // Use wants to reset all books.
                        isInstrumentProvided = false;
                    }
                    ptr++;

                    //
                    // Extract real values
                    //
                    double realPnL = double.NaN;
                    if (data[ptr] != null)
                        double.TryParse(data[ptr].ToString(), out realPnL);
                    ptr++;

                    double realStartPnL = double.NaN;
                    if (data[ptr] != null)
                        double.TryParse(data[ptr].ToString(), out realStartPnL);
                    ptr++;

                    //
                    // Reset the data
                    //
                    if (isInstrumentProvided)
                    {
                        m_IFillBooks[instrumentName].Lock.EnterWriteLock();
                        if (double.IsNaN(realPnL) && double.IsNaN(realStartPnL))
                            m_IFillBooks[instrumentName].ResetRealizedDollarGains();
                        else
                            m_IFillBooks[instrumentName].ResetRealizedDollarGains(realPnL, realStartPnL);
                        m_IFillBooks[instrumentName].Lock.ExitWriteLock();
                        // Trigger position change events, because we changed the pnl.
                        PositionBookChangedEventArgs newEventArg = new PositionBookChangedEventArgs();
                        newEventArg.Instrument = m_IFillBooks[instrumentName].Name;
                        newEventArg.Sender = this;
                        OnPositionBookChanged(newEventArg);
                    }
                    else
                    {
                        // Reset all books
                        foreach (InstrumentName key in m_IFillBooks.Keys)
                        {
                            m_IFillBooks[key].Lock.EnterWriteLock();
                            if (double.IsNaN(realPnL) || double.IsNaN(realStartPnL))
                                m_IFillBooks[key].ResetRealizedDollarGains();
                            else
                                m_IFillBooks[key].ResetRealizedDollarGains(realPnL, realStartPnL);
                            m_IFillBooks[key].Lock.ExitWriteLock();
                            // Trigger position change events, because we changed the pnl.
                            PositionBookChangedEventArgs newEventArg = new PositionBookChangedEventArgs();
                            newEventArg.Instrument = m_IFillBooks[key].Name;
                            newEventArg.Sender = this;
                            OnPositionBookChanged(newEventArg);
                        }// next InstrumentKey
                        return;
                    }
                }//next ptr
            }
            else if (eraseAllBooks)
            {   // User wants to completely erase the contents of all books.
                foreach (InstrumentKey key in m_FillBooks.Keys)
                {
                    int qty = 0;
                    double price = 0;
                    m_FillBooks[key].Lock.EnterReadLock();
                    qty = m_FillBooks[key].NetPosition;
                    price = m_FillBooks[key].AveragePrice;
                    m_FillBooks[key].Lock.ExitReadLock();

                    if (qty != 0)
                    {
                        // Try to get the current market price.
                        InstrumentName name;
                        Misty.Lib.BookHubs.Book aBook;
                        int ID = 0;
                        if (m_MarketHub.TryLookupInstrument(key, out name) && m_MarketHub.TryLookupInstrumentID(name, out ID) && m_MarketHub.TryEnterReadBook(out aBook))
                        {
                            price = aBook.Instruments[ID].LastPrice;
                            m_MarketHub.ExitReadBook(aBook);    // Need to keey the if-get book is last above!
                        }
                        // Add fills
                        Misty.Lib.OrderHubs.Fill fill = Misty.Lib.OrderHubs.Fill.Create(-qty, price, DateTime.Now, DateTime.Now);
                        FillEventArgs eventArgs = new FillEventArgs(key, FillType.UserAdjustment, fill);
                        this.AddFillToBook(m_FillBooks[key], eventArgs);    // there is a lock in here too!
                    }
                    //m_FillBooks[key].Lock.EnterWriteLock();
                    //m_FillBooks[key].DeleteAllFills();
                    //m_FillBooks[key].Lock.ExitWriteLock();
                    // Trigger position change events, because we changed the pnl.
                    PositionBookChangedEventArgs newEventArg = new PositionBookChangedEventArgs();
                    newEventArg.Instrument = m_FillBooks[key].Name;
                    newEventArg.Sender = this;
                    OnPositionBookChanged(newEventArg);
                }// next InstrumentKey
            }
            else
            {   // User didnt supply a specific instrument to reset.  He wants to reset all daily PnLs.
                foreach (InstrumentName key in m_IFillBooks.Keys)
                {
                    m_IFillBooks[key].Lock.EnterWriteLock();
                    m_IFillBooks[key].ResetRealizedDollarGains();
                    m_IFillBooks[key].Lock.ExitWriteLock();
                    // Trigger position change events, because we changed the pnl.
                    PositionBookChangedEventArgs newEventArg = new PositionBookChangedEventArgs();
                    newEventArg.Instrument = m_IFillBooks[key].Name;
                    newEventArg.Sender = this;
                    OnPositionBookChanged(newEventArg);
                }// next InstrumentKey
                return;
            }
        }// ResetAllFillBooks()
        //
        //        
        //
        //
        //
        #endregion//HubEventHandler and Processing



        #region Private Methods
        //
        //
        // *********************************************************
        // ****                 UpdatePeriodic()                ****
        // *********************************************************
        protected override void UpdatePeriodic()
        {
            if (m_IsShuttingDown)
            {
                // TODO: Check that all drop files are completed.
                bool isDropCompleted = m_DropRules == null || !m_DropRules.IsBookWriterRunning;
                if (isDropCompleted)
                {
                    Log.NewEntry(LogLevel.Major, "UpdatePeriodic: Shutting down.");
                    Shutdown();
                }
                else
                {
                    Log.NewEntry(LogLevel.Major, "UpdatePeriodic: Waiting to shut down.");
                    if (m_DropRules.IsBookWriterRunning)
                        Log.NewEntry(LogLevel.Minor, "UpdatePeriodic: BookWriting still running.");
                }
                return;                     // short circuit the update.
            }

            // Check to reset the daily PnL.
            DateTime now = Log.GetTime();
            if (now.CompareTo(NextResetDateTime) > 0)
            {   // Lets reset the PnL if we passed the NextResetDateTime.
                // We will request the fill hub to reset the PnL, then it will also request another drop.
                if (m_DropRules != null)    // need drop rules to "reset pnl" - since it saves position.
                {
                    DateTime nextDay = now.AddDays(1.0);
                    NextResetDateTime = nextDay.Subtract(nextDay.TimeOfDay).Add(ResetTimeOfDay);  // time of next reset, tomorrow
                    this.Request(new Misty.Lib.OrderHubs.OrderHubRequest(Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestRealPnLReset));
                }
            }
            // Check to drop
            if (m_DropRules != null && m_DropRules.TryPeriodicBookDrop())
            {   // We are doing a periodic drop. 
                // We need to also drop unprocessed fills that we are holding right now.
                // Typically these will be for instruments for which we do not have books, and are therefore, no counted
                // among those fills dropped in the form of books.
                foreach (SortedList<DateTime, FillEventArgs> fillList in m_FillsWaitList.Values)
                    if (fillList != null)
                        foreach (FillEventArgs aFill in fillList.Values)
                            m_DropRules.Enqueue(aFill);
            }
        }// UpdatePeriodic() 
        //
        //
        //
        //
        // *********************************************************
        // ****                     Shutdown()                  ****
        // *********************************************************
        /// <summary>
        /// Call by hub thread only to shut down our private resources.
        /// </summary>
        protected void Shutdown()
        {
            if (m_Listener != null)
            {
                m_Listener.Dispose();
                m_Listener = null;
            }
            if (m_DropRules != null)
            {
                m_DropRules.Dispose();
                m_DropRules = null;
            }
            if (m_EventWaitQueue != null)
            {
                m_EventWaitQueue.Dispose();
                m_EventWaitQueue = null;
            }
            base.Stop();
        }// Shutdown()
        //
        //
        //
        #endregion//HubEventHandler and Processing



        #region Book manipulation
        //
        // *********************************************************************
        // ****                     Try Create New Book                     ****
        // *********************************************************************
        /// <summary>
        /// We have enough information to create a new book for an instrument.  The requirement
        /// is that the Market hub has the appropriate "detail" object for this instrument.
        /// Note: 
        /// The final object is added to KeyMap() look up table, at which point, outside threads will be aware
        /// that another book is available.  A new book event should also be fired somewhere.
        /// Called by hub thread.
        /// </summary>
        /// <param name="instrumentName"></param>
        /// <param name="key"></param>
        /// <param name="book">Newly created book</param>
        /// <returns>True if new book was created (and added to accessible list of books)</returns>
        private bool TryCreateNewBook(InstrumentName instrumentName, InstrumentKey key, out BookLifo book)
        {
            // TT uses both: "SmallestTickIncrement" and "TickSize"
            // They say that SmallestTickIncrment is the visible size of the contract.
            // While the TickSize is the smallest increment that one might be filled on.
            // On some instruments during the calendar roll, for example, the ES and treasuries spreads trade 
            // with a smaller SmallestTickIncrement than the outrights, therefore you may get a leg fill that is between
            // what is expected from the SmallestTickIncrement of the outright.
            //double minTickSize = detail.SmallestTickIncrement;  
            double minTickSize = 1.0;
            double dollarAmountOfSmallestIncr = 0.0;
            double x = 0.0;
            InstrumentDetails detail = null;
            if (m_MarketHub.TryLookupInstrumentDetails(instrumentName, out detail))
            {
                minTickSize = Convert.ToDouble(detail.TickSize.Numerator) / Convert.ToDouble(detail.TickSize.Denominator);
                x = detail.SmallestTickIncrement;
                dollarAmountOfSmallestIncr = detail.TickValue;
                BookLifo lifoBook;
                lifoBook = new BookLifo(minTickSize, dollarAmountOfSmallestIncr, instrumentName);
                // Concurrent threading model. This is the only place where things are added to these collections.
                // Note that InstrumentMapInv dictionary is created last- since other threads use this to find desired
                // position books, by creating this last we ensure all other concurrent collections already exist!
                if (!m_FillBooks.ContainsKey(key))
                    m_FillBooks.TryAdd(key, lifoBook);                              // Can only fail if key already exists.                
                else
                    m_FillBooks[key] = lifoBook;
                if (!m_BookNameMap.ContainsKey(instrumentName))
                    m_BookNameMap.TryAdd(instrumentName, new InstrumentMapEntry(instrumentName, key));
                else
                    m_BookNameMap[instrumentName] = new InstrumentMapEntry(instrumentName, key);
                if (!m_BookKeyMap.ContainsKey(key))
                    m_BookKeyMap.TryAdd(key, new InstrumentMapEntry(instrumentName, key));  // update this last! Its the lookup for outside threads.                
                else
                    m_BookKeyMap[key] = new InstrumentMapEntry(instrumentName, key);        // update this last! Its the lookup for outside threads.      

                if (!m_IFillBooks.ContainsKey(m_BookKeyMap[key].Name))
                    m_IFillBooks.TryAdd(m_BookKeyMap[key].Name, lifoBook);                              // Can only fail if key already exists.          
                else
                    m_IFillBooks[m_BookKeyMap[key].Name] = lifoBook;

                //Exit
                book = lifoBook;
                return true;
            }
            else
            {
                book = null;
                Log.NewEntry(LogLevel.Warning, "TryCreateNewBook: No instrument details found for {0}.", instrumentName.FullName);
                return false;
            }
        } // TryCreateNewBook()
        //
        //
        //
        // *************************************************************
        // ****                 TryDeleteBook()                     ****
        // *************************************************************
        /// <summary>
        /// Forcefully removes the fill book associated with the key provided.
        /// </summary>
        /// <param name="key"></param>
        /// <returns>True, if book is deleted</returns>
        private bool TryDeleteBook(InstrumentKey key)
        {
            InstrumentMapEntry mapEntry;
            if (m_BookKeyMap.TryGetValue(key, out mapEntry))
            {
                bool isGood = m_FillBooks.ContainsKey(key) && m_BookNameMap.ContainsKey(mapEntry.Name);
                if (!isGood)
                {
                    Log.NewEntry(LogLevel.Major, "TryDeleteBook: Failed to find entries for {0} {1}", key, mapEntry.Name);
                    return false;
                }

                // First delete the entry from the Ibook dicionary.
                IFillBook iBook = null;
                if (!m_IFillBooks.TryRemove(mapEntry.Name, out iBook))
                {
                    Log.NewEntry(LogLevel.Major, "TryDeleteBook: Failed to remove book from FillBooks for {0}.", mapEntry.Name);
                }

                // Delete book now.  First we remove the look-up map used by outsiders, once this is gone, outsiders will 
                // not be able to locate the fillBook.  IF this is successful, we remove the other mappings and the book.
                InstrumentMapEntry mapEntry2;
                if (!m_BookNameMap.TryRemove(mapEntry.Name, out mapEntry2))
                {
                    Log.NewEntry(LogLevel.Major, "TryDeleteBook: Failed to remove MapNameEntry for {0}.  Book remains.", mapEntry.Name);
                    return false;
                }
                // Now remove the remaining items - as soon as BookNameMap entry is removed, outsiders will not find the book, so its deleted...
                BookLifo book = null;
                if (!m_BookKeyMap.TryRemove(key, out mapEntry2))
                {
                    Log.NewEntry(LogLevel.Major, "TryDeleteBook: Failed to remove MapKeyEntry for key {0}", key);
                    return true;
                }

                if (!m_FillBooks.TryRemove(key, out book))
                {
                    Log.NewEntry(LogLevel.Major, "TryDeleteBook: Failed to remove book from FillBooks for {0}.", mapEntry.Name);
                    return true;
                }
                // Success!
                Log.NewEntry(LogLevel.Major, "TryDeleteBook: Successfully removed book {0}.  Book final state: {1}", mapEntry.Name, Stringifiable.Stringify(book));
                return true;

            }
            else
            {
                Log.NewEntry(LogLevel.Major, "TryDeleteBook: No book found for key {0}.", key);
                return false;
            }
        }// TryDeleteBook()
        //
        //
        private bool TryDeleteBook(InstrumentName name)
        {
            //InstrumentKey key = new InstrumentKey();
            InstrumentMapEntry mapEntry;
            if (m_BookNameMap.TryGetValue(name, out mapEntry))
            {
                InstrumentKey key = mapEntry.Key;
                bool isGood = m_FillBooks.ContainsKey(key) && m_BookNameMap.ContainsKey(mapEntry.Name);
                if (!isGood)
                {
                    Log.NewEntry(LogLevel.Major, "TryDeleteBook: Failed to find entries for {0} {1}", key, mapEntry.Name);
                    return false;
                }

                // First delete the entry from the Ibook dicionary.
                IFillBook iBook = null;
                if (!m_IFillBooks.TryRemove(name, out iBook))
                {
                    Log.NewEntry(LogLevel.Major, "TryDeleteBook: Failed to remove book from FillBooks for {0}.", mapEntry.Name);
                }

                // Delete book now.  First we remove the look-up map used by outsiders, once this is gone, outsiders will 
                // not be able to locate the fillBook.  IF this is successful, we remove the other mappings and the book.
                InstrumentMapEntry mapEntry2;
                if (!m_BookNameMap.TryRemove(mapEntry.Name, out mapEntry2))
                {
                    Log.NewEntry(LogLevel.Major, "TryDeleteBook: Failed to remove MapNameEntry for {0}.  Book remains.", mapEntry.Name);
                    return false;
                }
                // Now remove the remaining items
                BookLifo book = null;
                if (!m_BookKeyMap.TryRemove(key, out mapEntry2))
                {
                    Log.NewEntry(LogLevel.Major, "TryDeleteBook: Failed to remove MapKeyEntry for key {0}", key);
                    return true;
                }
                if (!m_FillBooks.TryRemove(key, out book))
                {
                    Log.NewEntry(LogLevel.Major, "TryDeleteBook: Failed to remove book from FillBooks for {0}.", mapEntry.Name);
                    return true;
                }
                // Success!
                Log.NewEntry(LogLevel.Major, "TryDeleteBook: Successfully removed book {0}.  Book final state: {1}", mapEntry.Name, Stringifiable.Stringify(book));
                //m_DropRules.Enqueue(this);

                return true;

            }
            else
            {
                Log.NewEntry(LogLevel.Major, "TryDeleteBook: No book found for name {0}.", name);
                return false;
            }
        }// TryDeleteBook()
        //
        //
        //
        //
        // *****************************************************************
        // ****                     AddFillToBook()                     ****
        // *****************************************************************
        /// <summary>
        /// Main method for adding fills to a book.  Each book has a filter that 
        /// allows it to reject the fill on various grounds.
        /// </summary>
        /// <param name="lifoBook">book to add fill to</param>
        /// <param name="eventArg">fill event</param>
        protected void AddFillToBook(BookLifo lifoBook, FillEventArgs eventArg)
        {
            // Add fill to book          
            lifoBook.Lock.EnterWriteLock();                                     // Write LOCK
            RejectedFills.RejectedFillEventArgs rejection;
            bool isFillAccepted = lifoBook.TryAdd(eventArg, out rejection);
            lifoBook.Lock.ExitWriteLock();                                      // LOCK release

            // Report results of add.
            if (isFillAccepted)
            {
                if (eventArg.Type != FillType.InitialPosition && m_DropRules != null)                  // Recall "initial position" are those already loaded from a drop file.
                    m_DropRules.Enqueue(eventArg);                              // Need to write out all but initial fills which come from drop files only, right?
            }
            else
            {
                if (m_DropRules != null)
                    m_DropRules.Enqueue(rejection);
                OnFillRejectionsUpdated(rejection);
            }

            // Write log if we are not currently working on one. (Happens during initialization.)
            if (!Log.IsWorkingMessage && Log.BeginEntry(LogLevel.Major, "AddFillToBook: {0} ", eventArg.Type))
            {
                if (isFillAccepted)
                    Log.AppendEntry("accepted. ");
                else
                    Log.AppendEntry("rejected. ");
                Log.AppendEntry("{0} {1} --> {2}", lifoBook.Name, eventArg.Fill, lifoBook);
                Log.EndEntry();
            }

            // Fire event for PositionBook subscribers
            PositionBookChangedEventArgs newEventArg = new PositionBookChangedEventArgs();
            newEventArg.Instrument = lifoBook.Name;
            newEventArg.Sender = this;
            OnPositionBookChanged(newEventArg);
        } // AddFillToBook();
        //
        //
        //
        // *****************************************************************
        // ****                 AddToNewFillsList()                     ****
        // *****************************************************************
        protected void AddToNewFillsList(FillEventArgs eventArg)
        {
            Log.BeginEntry(LogLevel.Minor, "AddToNewFillsList: {0} ", eventArg);
            SortedList<DateTime, FillEventArgs> fillList;
            if (!m_NewFillsList.TryGetValue(eventArg.TTInstrumentKey, out fillList))
            {   // This is the first fill we have gotten for this key, need to request new book.
                // Since we know NOTHING about this instrument, store fill, request a fill book.
                // Once we learn more, we may decide we don't accept fills from this instrument.
                fillList = new SortedList<DateTime, FillEventArgs>();
                m_NewFillsList.Add(eventArg.TTInstrumentKey, fillList);         // existence of this entry tells us we've requested a book for it.

                Log.AppendEntry("Requesting new book. ");
                Misty.Lib.OrderHubs.OrderHubRequest requestEvent = new Misty.Lib.OrderHubs.OrderHubRequest(Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestCreateFillBook);
                requestEvent.Data = new object[] { eventArg.TTInstrumentKey };  //
                this.HubEventEnqueue(requestEvent);
            }
            // Store fill now.
            DateTime dt = eventArg.Fill.ExchangeTime;                           // we order fills by their exchange time stamp.
            while (fillList.ContainsKey(dt))
                dt = dt.AddTicks(1L);
            fillList.Add(dt, eventArg);
            Log.AppendEntry("NewFillList count = {0}. ", fillList.Count);
            Log.EndEntry();
        } // AddFillToWaitList()
        //
        //
        //
        // *****************************************************************
        // ****                 Is Instrument Desired()                 ****
        // *****************************************************************
        /// <summary>
        /// Determines whether this is an instrument for which we want to maintain a fill book.
        /// </summary>
        /// <returns>true if this instrument should have a fill book</returns>
        protected bool IsInstrumentDesired(InstrumentName instrumentName)
        {
            Misty.Lib.Products.ProductTypes type = instrumentName.Product.Type;

            bool isNotGood = (type == Misty.Lib.Products.ProductTypes.Spread)
                    || (type == Misty.Lib.Products.ProductTypes.Unknown)
                    || (type == Misty.Lib.Products.ProductTypes.AutoSpreaderSpread)
                    || (type == Misty.Lib.Products.ProductTypes.Strategy);

            return !isNotGood;
        } // IsInstrumentDesired()
        //
        //
        //
        //
        //
        //
        #endregion // Book manipulations.



        #region Book Initialization & Historic fills
        //
        //
        // *************************************************************************
        // ****                     Load Saved Books()                          ****
        // *************************************************************************
        private void LoadSavedBooks()
        {
            m_IsInitializingBooks = true;

            // Create a drop rule object.
            //m_DropRules = new DropRules(this);
            m_DropRules = new DropSimple(this);
            if (m_DropRules.TryLoadBooks())
            {
                foreach (FillBookLifo book in m_FillBooks.Values)
                {
                    book.RecalculateAll();
                    PositionBookChangedEventArgs eventArgs = new PositionBookChangedEventArgs();
                    eventArgs.Instrument = book.Name;
                    eventArgs.Sender = this;
                    OnPositionBookCreated(eventArgs);
                    m_MarketHub.RequestInstruments(m_BookNameMap[book.Name].Key);
                }
            }
            m_DropRules.Start();

            /*
            string dropDirPath = string.Format("{0}{1:yyyy-MMM-dd}\\", Misty.Lib.Application.AppInfo.GetInstance().DropPath,DateTime.Now);
            string dropFileName = string.Format("{0:HHmmss}_{1}_FillBooks.txt", DateTime.Now, userName);
            if (!DropRules.TryCreate(dropDirPath, dropFileName, this, out m_DropRules))
                Log.NewEntry(LogLevel.Error, "ProcessRequest: Failed to create DropRules.");
            if (m_DropRules.IsFileExists)
            {
                List<IStringifiable> objectList = m_DropRules.Load();               // Found new drop file for FillBooks.
                foreach (IStringifiable obj in objectList)                          // load all InstrumentMapEntries first - needed to create books
                    if ( obj is InstrumentMapEntry )
                        ((IStringifiable)this).AddSubElement(obj);
                foreach (IStringifiable obj in objectList)                          // load everything else now.
                    if ( ! (obj is InstrumentMapEntry) )
                        ((IStringifiable)this).AddSubElement(obj);
                foreach (Misty.Lib.OrderHubs.FillBookLifo book in m_FillBooks.Values)
                {
                    book.RecalculateAll();
                    PositionBookChangedEventArgs eventArgs = new PositionBookChangedEventArgs();
                    eventArgs.Instrument = book.Name;                    
                    OnPositionBookCreated(eventArgs);
                    m_MarketHub.RequestInstruments(m_BookNameMap[book.Name].Key);
                }                                                            
            }
            */

            // Create drop for raw fills   
            ValidateInitialFills();                                                 // Validate fills that were added to the WaitList   
        }// LoadSavedBooks()
        //
        //
        // ****                     ValidateInitialFills()                      ****
        //
        /// <summary>
        /// 
        /// </summary>
        protected void ValidateInitialFills()
        {
            Log.BeginEntry(LogLevel.Minor, "ValidateInitialFills: Validating InitialFills for {0} instrs.", m_FillsWaitList.Count);
            foreach (InstrumentKey instrumentKey in m_FillsWaitList.Keys)
            {
                SortedList<DateTime, FillEventArgs> initList;
                if (m_FillsWaitList.TryGetValue(instrumentKey, out initList) && initList.Count > 0)
                {
                    BookLifo book;
                    if (m_FillBooks.TryGetValue(instrumentKey, out book))  // If we have a book for these fills, lets place them into book.
                    {
                        Log.AppendEntry("[{0}, {1} initial fills ", book.Name, initList.Count);
                        int ptr = initList.Count - 1;                       // point to last fill (most recent)
                        while (ptr >= 0)
                        {
                            RejectedFills.RejectedFillEventArgs rejection;
                            if (!book.IsFillNew(initList.Values[ptr], out rejection))
                            {
                                if (rejection != null)
                                    Log.AppendEntry("(rejected {0} {1})", initList.Values[ptr].Fill, rejection.Reason);
                                initList.RemoveAt(ptr);                     // This fill was already added to book, delete it.                                
                                //#if (ShowInitialRejections)
                                //    OnFillRejectionsUpdated(rejection);   // TODO: ??? Weird that we've already included our initial fills twice.
                                //#endif

                            }
                            ptr--;                                          // consider earlier fill.  Note we are not adding fills to book yet.
                        }// wend ptr
                    }// if we have a book
                    else
                        Log.AppendEntry("[{0}, {1} initial fills, no book", instrumentKey, initList.Count);
                    Log.AppendEntry(", {0} survive.]", initList.Count);
                }
            }
            Log.EndEntry();
        }// ValidateInitialFills()
        //
        //
        private static int CompareFillsByExchangeDate(FillEventArgs argA, FillEventArgs argB)
        {
            if (argA == null)
            {
                if (argB == null)
                    return 0;
                else
                    return -1;                  // x-y = -1 < 0 ==> nulls are earlier
            }
            else
            {
                if (argB == null)
                    return 1;
                else
                    return argA.Fill.ExchangeTime.CompareTo(argB.Fill.ExchangeTime);
            }
        }
        //
        //
        //
        //
        // ****                 Validate Historic Fills()                   ****
        //
        private void ValidateHistoricFills(List<FillEventArgs> historicalRawFillList)
        {
            // Fill procedure:
            // 1. Load saved books and initial fills (received after startup snapshot was made).
            //      Confirm initial fills are consistent with the starting fill book, if we have one...
            //      But, note that no fills are loaded until all analysis is complete at end of this routine.
            // 2. Load Historic fills.  Locate fills received after snapshot time.
            //      Confirm that earlier fills have been absorbed into book, using unique ID. Work backwards from most recent.
            //      Discard all fills earlier than book's EchangeTimeLast.
            //      Compare to Initial fills and discard duplicates.
            //      Drop remaining fills and process them into books.
            // 3. Delete fill storage lists for this each instrument key filled.
            // 4. Request InstrumentDetails for any remaining unknown fills...  These requests are put into WaitingQueue until complete.

            // Validate historic fills.
            // HistoricFills are received upon connecting to our Order server (added by FillListener).
            // There may be some new fills or repeated fills here.  We need to separate unknown fills from those already processed.
            Log.BeginEntry(LogLevel.Minor, "ValidateHistoricFills: Serparating {0} HistoricFills by instrument.", historicalRawFillList.Count);
            Dictionary<InstrumentKey, List<FillEventArgs>> historicalFillLists = new Dictionary<InstrumentKey, List<FillEventArgs>>();
            foreach (FillEventArgs eventArgs in historicalRawFillList)
            {
                List<FillEventArgs> fillList = null;
                if (!historicalFillLists.TryGetValue(eventArgs.TTInstrumentKey, out fillList))
                {
                    fillList = new List<FillEventArgs>();
                    historicalFillLists.Add(eventArgs.TTInstrumentKey, fillList);
                }
                fillList.Add(eventArgs);
            }
            Log.AppendEntry(" Sorting HistoricFills for {0} instruments.", historicalFillLists.Count);
            Log.EndEntry();

            // Do initial processing for the raw historical fill list.
            foreach (InstrumentKey key in historicalFillLists.Keys)
            {
                if (m_FillBooks.ContainsKey(key))
                {
                    // Get unqualified fills and it is designed specifically for audit trail reader project.
                    List<FillEventArgs> neededRemoveHistoricalFills = new List<FillEventArgs>();
                    foreach (FillEventArgs fillEventArgs in historicalFillLists[key])
                    {
                        if (fillEventArgs.Fill.ExchangeTime <= m_FillBooks[key].ExchangeTimeLast)
                        {
                            neededRemoveHistoricalFills.Add(fillEventArgs);
                        }
                    }

                    // Delete those fills loaded from listeners that are too old.
                    foreach (FillEventArgs removedfillEventArgs in neededRemoveHistoricalFills)
                    {
                        bool removedState = false;
                        FillEventArgs removedTarget = null;
                        foreach (FillEventArgs fillEventArgs in historicalFillLists[key])
                        {
                            if (fillEventArgs.IsSameAs(removedfillEventArgs))
                            {
                                removedState = true;
                                removedTarget = fillEventArgs;
                                break;
                            }
                        }
                        if (removedState && removedTarget != null)
                            historicalFillLists[key].Remove(removedTarget);
                    }
                }

                // Sort the fill event args for each instrument.
                historicalFillLists[key].Sort(CompareFillsByExchangeDate);
            }

            // Start rejecting some historic fills.
            const int HistoricMaxRejectionStreak = 25;
            //Log.NewEntry(LogLevel.Minor, "ValidateHistoricFills: Validating HistoricFills.");

            foreach (InstrumentKey instrumentKey in historicalFillLists.Keys)
            {
                Log.BeginEntry(LogLevel.Minor, "ValidateHistoricFills: {0}", instrumentKey);
                List<FillEventArgs> historicList;
                if (historicalFillLists.TryGetValue(instrumentKey, out historicList) && historicList.Count > 0)
                {
                    BookLifo book;
                    if (m_FillBooks.TryGetValue(instrumentKey, out book))
                    {
                        book.Lock.EnterReadLock();
                        Log.AppendEntry(" BookTest: {0} starting with {1} fills, totalQty={2}. ", book.Name, book.Fills.Count, book.NetPosition);
                        int nRejectedCount = 0;
                        int ptr = historicList.Count - 1;                       // point to last fill (most recent)
                        while (ptr >= 0)
                        {
                            RejectedFills.RejectedFillEventArgs rejection;
                            if (nRejectedCount > HistoricMaxRejectionStreak)  // we are stepping thru backwards, once we reject say, 25 fills, lets assume the rest are bad!
                            {
                                Log.AppendEntry("(REJECT {0} exceded max failed count)", historicList[ptr]);
                                historicList.RemoveAt(ptr);
                                nRejectedCount++;
                            }
                            else if (!book.IsFillNew(historicList[ptr], out rejection))
                            {
                                Log.AppendEntry("(REJECT {0} reason {1})", historicList[ptr], rejection);
                                historicList.RemoveAt(ptr);                     // delete historic fills already in our books.  
                                nRejectedCount++;
                            }
                            else
                            {
                                Log.AppendEntry("(ACCEPT {0})", historicList[ptr]);
                                nRejectedCount = 0;
                            }
                            ptr--;
                        }// wend ptr
                        book.Lock.ExitReadLock();
                        Log.AppendEntry(" {0} survived. ", historicList.Count);
                    }// if book
                    else
                        Log.AppendEntry(" No BookTest.");

                    // Compare historic fills to fill already in waiting list.
                    Log.AppendEntry(" WaitingList Test: ");
                    SortedList<DateTime, FillEventArgs> waitList;
                    if (m_FillsWaitList.TryGetValue(instrumentKey, out waitList))
                    {                                                           // Compare historic versus initial fills!
                        int ptr = historicList.Count - 1;                       // point to last historic fill (most recent)
                        while (ptr >= 0)
                        {
                            FillEventArgs historic = historicList[ptr];
                            FillEventArgs init = null;
                            for (int n = 0; n < waitList.Count; ++n)
                            {
                                init = waitList.Values[n];
                                if (init.IsSameAs(historic))
                                {
                                    Log.AppendEntry("(REJECT {0} same as {1})", historic, init);
                                    historicList.RemoveAt(ptr);                 // delete this element
                                    break;
                                }
                                else
                                    Log.AppendEntry("(ACCEPT {0})", historic);
                            }
                            ptr--;                                              // now, consider previous fill...
                        }
                    }//wend ptr
                    Log.AppendEntry(" {0} survive WaitList test.", historicList.Count);

                    // Load surviving historic fills into FillsWaitList
                    // This will sort (in time) them relative to each other.
                    if (!m_FillsWaitList.TryGetValue(instrumentKey, out waitList))
                    {
                        waitList = new SortedList<DateTime, FillEventArgs>();
                        m_FillsWaitList.Add(instrumentKey, waitList);
                    }
                    while (historicList.Count > 0)
                    {
                        DateTime dt = historicList[0].Fill.ExchangeTime;
                        while (waitList.ContainsKey(dt))
                            dt = dt.AddTicks(1L);    //dt = dt.AddMilliseconds(SmallIncrement);
                        waitList.Add(dt, historicList[0]);
                        historicList.RemoveAt(0);
                    }
                }// if historic fills exist.                
                Log.EndEntry();
            }// next instr
            //
            // Load fills in InitialFills into our books, if book exists.
            //
            // Question: Can it ever happen that we have historic fills with timestamps after our last snapshot, but before
            // the initial fills.  If so, then multiple calls to this function could reject some of those historic fills as
            // appearing after the initial fills.
            Log.BeginEntry(LogLevel.Minor, "ValidateHistoricFills: Adding starting fills for {0} instrs. ", m_FillsWaitList.Count);
            List<InstrumentKey> keyList = new List<InstrumentKey>(m_FillsWaitList.Keys);
            foreach (InstrumentKey instrumentKey in keyList)
            {
                SortedList<DateTime, FillEventArgs> fillList;
                if (m_FillsWaitList.TryGetValue(instrumentKey, out fillList) && fillList.Count > 0)
                {
                    BookLifo book;
                    if (m_FillBooks.TryGetValue(instrumentKey, out book))   // We have a book for this instrument.
                    {
                        Log.AppendEntry("[{0} book adding ", book.Name);
                        while (fillList.Count > 0)
                        {
                            Log.AppendEntry("{0}", fillList.Values[0]);
                            AddFillToBook(book, fillList.Values[0]);
                            fillList.RemoveAt(0);                           // must remove fill after consumption...to avoid double counting later, possibly.
                        }

                        PositionBookChangedEventArgs eventArgs = new PositionBookChangedEventArgs();
                        eventArgs.Instrument = book.Name;
                        eventArgs.Sender = this;
                        OnPositionBookChanged(eventArgs);
                    }
                    else
                    {   // We don't have a book for this instrument.  Request the creation of new books.
                        Log.AppendEntry("[{0} has no book, RequestCreateBook for {1} fills.", instrumentKey, fillList.Count);

                        Misty.Lib.OrderHubs.OrderHubRequest hubRequest = new Misty.Lib.OrderHubs.OrderHubRequest(Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestCreateFillBook);
                        hubRequest.Data = new object[1];
                        hubRequest.Data[0] = instrumentKey;
                        this.HubEventEnqueue(hubRequest);                                   // Request new fill book.                        
                    }
                    Log.AppendEntry("]");
                }//if there are initial fills.                
            }
            Log.EndEntry();

        }// ValidateHistoricFills()
        //
        //
        //
        //
        //       
        //
        //
        //
        #endregion// Initialization & book loading



        #region IStringifiable Implementation
        // *****************************************************************************
        // ****                     IStringifiable Implementation                   ****
        // *****************************************************************************
        //
        string IStringifiable.GetAttributes()
        {
            StringBuilder msg = new StringBuilder();
            if (!string.IsNullOrEmpty(this.Name))
                msg.AppendFormat("Name={0}", this.Name);
            return msg.ToString();
        }
        public string GetAttributesDrop()
        {
            StringBuilder msg = new StringBuilder();
            msg.AppendFormat("LocalTime={0}", m_LocalTimeLastFill.ToString(Strings.FormatDateTimeZone));
            if (!string.IsNullOrEmpty(this.Name))
                msg.AppendFormat(" Name={0}", this.Name);
            //msg.AppendFormat(" NextReset={0}",m_DropRules.NextResetDateTime.ToString(Strings.FormatDateTimeZone));
            msg.AppendFormat(" NextReset={0}", this.NextResetDateTime.ToString(Strings.FormatDateTimeZone));
            return msg.ToString();
        }
        List<IStringifiable> IStringifiable.GetElements()
        {
            return null;
        }
        public List<IStringifiable> GetElementsDrop()
        {
            List<IStringifiable> elements = new List<IStringifiable>();
            elements.Add(this.m_Listener);              // Write FillListener specs
            // Write books
            // We need to include all books.  If we don't write a flat book, we forget its last fills.
            // Then if we stop and start we get exchange-historic fills, and think they are fills we missed!
            foreach (InstrumentName instrumentName in m_BookNameMap.Keys)
            {
                InstrumentMapEntry mapEntry = m_BookNameMap[instrumentName];
                BookLifo fillBook;
                if (m_FillBooks.TryGetValue(mapEntry.Key, out fillBook))
                {
                    bool deleteBook = m_InstrumentsWithoutBooks.Contains(fillBook.Name) && fillBook.NetPosition == 0;
                    if (!deleteBook)
                    {
                        elements.Add(mapEntry);                     // NEED to write map entry first, before associated book!!!
                        elements.Add(fillBook);
                    }
                }
            }

            // Drop cash books too to the file.
            foreach (InstrumentName instrumentName in m_IFillBooks.Keys)
            {
                IFillBook book = m_IFillBooks[instrumentName];
                if (book is CashBook)
                {
                    CashBook cashBook = (CashBook)book;
                    elements.Add(cashBook);
                }
            }
            return elements;
        }
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
            DateTime dt;
            foreach (string key in attributes.Keys)
                if (key.Equals("LocalTime") && DateTime.TryParse(attributes[key], out dt))
                    this.m_LocalTimeLastFill = dt;
                else if (key.Equals("Name"))
                    this.Name = attributes[key];
                else if (key.Equals("NextReset") && DateTime.TryParse(attributes[key], out dt))
                    this.NextResetDateTime = dt;
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {
            Type elementType = subElement.GetType();
            if (elementType == typeof(BookLifo))// if (subElement is BookLifo)
            {
                BookLifo book = (BookLifo)subElement;
                InstrumentMapEntry mapEntry;
                book.RecalculateAll();
                if (m_BookNameMap.TryGetValue(book.Name, out mapEntry))
                {
                    if (m_FillBooks.TryAdd(mapEntry.Key, book) && m_IFillBooks.TryAdd(mapEntry.Name, book))
                        Log.NewEntry(LogLevel.Minor, "AddSubElement: IFillBook {0}.", book);
                    else
                        Log.NewEntry(LogLevel.Error, "AddSubElement: IFillBook {0} wasn't loaded. Another book with same key is present.", book);
                }
                else
                {
                    Log.NewEntry(LogLevel.Error, "AddSubElement: FillBook {0} wasn't loaded. No instrument map was found.", book);
                }
            }
            else if (elementType == typeof(CashBook))
            {
                CashBook book = (CashBook)subElement;
                Misty.Lib.OrderHubs.OrderHubRequest cashBookCreateRequest = new Misty.Lib.OrderHubs.OrderHubRequest(Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestCreateUpdateCashBook);
                cashBookCreateRequest.Data = new object[5] { book.Name, book.CurrencyName, book.CurrencyRate, book.RealizedDollarGains, book.RealizedStartingDollarGains };
                Request(cashBookCreateRequest);
            }
            else if (elementType == typeof(InstrumentMapEntry))// else if (subElement is InstrumentMapEntry)
            {
                InstrumentMapEntry mapEntry = (InstrumentMapEntry)subElement;
                if (!m_BookNameMap.TryAdd(mapEntry.Name, mapEntry))
                    Log.NewEntry(LogLevel.Error, "AddSubElement: Failed to add name lookup for {0}", mapEntry);
                else if (!m_BookKeyMap.TryAdd(mapEntry.Key, mapEntry))
                    Log.NewEntry(LogLevel.Error, "AddSubElement: Failed to add key lookup for {0}", mapEntry);
                else
                    Log.NewEntry(LogLevel.Minor, "AddSubElement: Added key {0}", mapEntry);
            }
            else if (elementType == typeof(FillEventArgs))// else if (subElement is FillListener.FillEventArgs)
            {
                FillEventArgs eventArgs = (FillEventArgs)subElement;
                eventArgs.Type = FillType.InitialPosition;
                Log.NewEntry(LogLevel.Minor, "Destringified initial fill. {0}", eventArgs.ToString());
                // Lets load these initial fills into a place to study later.
                SortedList<DateTime, FillEventArgs> fillList;
                if (!m_FillsWaitList.TryGetValue(eventArgs.TTInstrumentKey, out fillList))
                {
                    fillList = new SortedList<DateTime, FillEventArgs>();
                    m_FillsWaitList.Add(eventArgs.TTInstrumentKey, fillList);
                }
                DateTime dt = eventArgs.Fill.ExchangeTime;          // store, ordered by Exch time.
                while (fillList.ContainsKey(dt))
                    dt = dt.AddTicks(1L);
                fillList.Add(dt, eventArgs);
            }
            else if (elementType == typeof(FillListener))
            {
                m_Listener = (FillListener)subElement;
                if (m_Listener.m_FilterType == FilterType.Account)
                {
                    HubName = m_Listener.m_TradeFilterArg;
                    m_LastListenAccountFilter = m_Listener.m_TradeFilterArg;
                }
                Log.NewEntry(LogLevel.Minor, "AddSubElement: Added {0}", m_Listener);
            }
        }
        //
        //
        #endregion//IStringifiable Implementation



        #region FillHub Events and Triggers
        // *****************************************************************
        // ****                     Events                              ****
        // *****************************************************************
        //
        //
        // *****************************************************************
        // ****                     PnL Changed                         ****
        // *****************************************************************
        //public event EventHandler PositionBookPnLChanged;
        //
        //private void OnPositionBookPnLChanged(PositionBookChangedEventArgs eventArgs)
        //{
        //    if (PositionBookPnLChanged != null)
        //        PositionBookPnLChanged(this, eventArgs);
        //}
        //
        //
        // *****************************************************************
        // ****                Position Book Created                    ****
        // *****************************************************************
        public event EventHandler PositionBookCreated;
        //
        protected void OnPositionBookCreated(PositionBookChangedEventArgs eventArgs)
        {
            if (PositionBookCreated != null)
                PositionBookCreated(this, eventArgs);
        }
        //       
        // *****************************************************************
        // ****                Position Book Deleted                    ****
        // *****************************************************************
        public event EventHandler PositionBookDeleted;
        //
        protected void OnPositionBookDeleted(PositionBookChangedEventArgs eventArgs)
        {
            if (PositionBookDeleted != null)
                PositionBookDeleted(this, eventArgs);
        }
        //       
        //
        //
        // *****************************************************************
        // ****                Position Book Changed                    ****
        // *****************************************************************
        public event EventHandler PositionBookChanged;
        //
        protected void OnPositionBookChanged(PositionBookChangedEventArgs eventArgs)
        {
            if (PositionBookChanged != null)
                PositionBookChanged(this, eventArgs);
        }
        //
        public class PositionBookChangedEventArgs : EventArgs
        {
            public InstrumentName Instrument;
            public PositionBookEventArgs EventArg;
            public FillHub Sender;
            public PositionBookChangedEventArgs()
            {
            }

            public override string ToString()
            {
                return string.Format("{0} {1}", Instrument, EventArg);
            }

        }
        //
        //
        // *****************************************************************
        // ****                     Rejected Fills                      ****
        // *****************************************************************
        public event EventHandler FillRejectionsUdated;
        //
        protected void OnFillRejectionsUpdated(RejectedFills.RejectedFillEventArgs eventArgs)
        {
            Log.NewEntry(LogLevel.Major, "OnFillRejectionsUpdated: {0}", eventArgs);
            if (FillRejectionsUdated != null)
                FillRejectionsUdated(this, eventArgs);
        }
        //
        //
        //
        public event EventHandler ServiceStateChanged;
        //
        private void OnServiceStateChanged()
        {
            if (ServiceStateChanged != null)
                ServiceStateChanged(this, EventArgs.Empty);
        }
        #endregion//Events
    }//end class
}
