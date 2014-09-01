using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

namespace UV.TTServices.Execution
{
    using TradingTechnologies.TTAPI;
    using TradingTechnologies.TTAPI.Tradebook;

    using UV.Strategies.ExecutionHubs;
    using UV.Lib.Hubs;
    using UV.Lib.Engines;
    using UVProd = UV.Lib.Products;
    using UVInstrName = UV.Lib.Products.InstrumentName;
    using UVOrder = UV.Lib.OrderBooks.Order;
    using UVFill = UV.Lib.Fills.Fill;
    using UVFillEventArgs = UV.Lib.Fills.FillEventArgs;
    using UVSyntheticOrder = UV.Lib.OrderBooks.SyntheticOrder;
    using MarketBase = UV.Lib.BookHubs.MarketBase;
    using UVInstrDetails = UV.Lib.Products.InstrumentDetails;
    using UVOrderBook = UV.Lib.OrderBooks.OrderBook;
    using OrderInstrument = UV.Lib.OrderBooks.OrderInstrument;
    using UVOrderType = UV.Lib.OrderBooks.OrderType;
    using UVOrderState = UV.Lib.OrderBooks.OrderState;

    /// <summary>
    /// The execution listener is a combination of a price and order listener.  It also acts 
    /// as the sole dispatcher of the single thread that lives inside an execution containter / order engine.
    /// Currently alot of functionality is stripped down to a bare bones application. For instance the current 
    /// implemnentation doesn't listen to volume or last traded price at all.  
    /// 
    /// 
    /// </summary>
    public class ExecutionListener : IExecutionListener, ITimerSubscriber
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        private TTApiService m_TTService = null;
        private LogHub Log = null;                                  // log I can write to.
        private IEngineHub m_EngineHub;                             // My parent EngineHub.
        private bool m_isDisposing = false;
        private WorkerDispatcher m_Dispatcher = null;               // TT's WorkerDispatcher
        public UV.TTServices.Markets.MarketTTAPI m_Market = null;
        private string m_Name;
        private UV.Strategies.ExecutionHubs.ExecutionContainer m_ExecContainer;


        // TT  Price Subscription objects.
        private Dictionary<InstrumentKey, PriceSubscription> m_PriceSubscriptions = new Dictionary<InstrumentKey, PriceSubscription>();
        private Dictionary<ProductKey, InstrumentCatalogSubscription> m_InstrumentCatalogs = new Dictionary<ProductKey, InstrumentCatalogSubscription>();
        private Dictionary<InstrumentKey, InstrumentLookupSubscription> m_InstrumentLookups = new Dictionary<InstrumentKey, InstrumentLookupSubscription>();


        // Internal Price Related tables
        private Dictionary<UVInstrName, InstrumentDetails> m_TTInstrumentDetails = new Dictionary<UVInstrName, InstrumentDetails>();
        private Dictionary<InstrumentKey, UVProd.InstrumentName> m_KeyToInstruments = new Dictionary<InstrumentKey, UVProd.InstrumentName>();
        private Dictionary<UVInstrName, UVInstrDetails> m_UVInstrumentDetails = new Dictionary<UVInstrName, UVInstrDetails>();

        // TT Order Subscriptions.
        private List<InstrumentKey> m_InstrumentsRequested = new List<InstrumentKey>();         // these are keys we asked for instruments, but haven't received callbacks yet.
        private Dictionary<InstrumentKey, Instrument> m_TTInstrKeyToTTInstr = new Dictionary<InstrumentKey, Instrument>();
        private Dictionary<InstrumentKey, UVInstrName> m_TTInstrKeyToUVInstr = new Dictionary<InstrumentKey, UVInstrName>();
        private List<TradeSubscription> m_TradeSubscriptions = new List<TradeSubscription>();   // place to store trade subscriptions to dispose of later.
        private Dictionary<InstrumentKey, OrderFeed> m_DefaultOrderFeeds = new Dictionary<InstrumentKey, OrderFeed>();
        private Dictionary<UVInstrName, InstrumentKey> m_InstrumentNameToTTKey = new Dictionary<UVInstrName, InstrumentKey>(); // this is redundant but will keep us a tad faster

        // Internal Order Lookup Tables tables.
        private Dictionary<string, int> m_MapTT2UV = new Dictionary<string, int>();
        private Dictionary<int, string> m_MapUV2TT = new Dictionary<int, string>();
        private Dictionary<string, Order> m_TTOrders = new Dictionary<string, Order>();

        // pending order collections
        private Dictionary<UVInstrName, List<UVOrderBook>> m_PendingOrderBooks = new Dictionary<UVInstrName, List<UVOrderBook>>();  // place to store all order books we need instrument details to create
        private List<string> m_TTOrderKeysPendingDelete = new List<string>();   // if we want to delete an order prior to getting the order object from tt, we place the key here for later processing.
        private List<int> m_OrderIdsPendingModification = new List<int>();      // if we aren't able to modify an order, we add it to the list of pending here.

        // workspaces
        private List<UVOrder> m_UvOrderWorkspace = new List<UVOrder>();

        // state flags
        private bool m_IsWaitingForApiConnection = false;

        // Event Queues
        private Queue<EventArgs> m_WorkQueue = new Queue<EventArgs>();                      // Completely private, owned by Listener thread.
        private ConcurrentQueue<EventArgs> m_InQueue = new ConcurrentQueue<EventArgs>();    // threadsafe queue to push events to processed onto.

        // Itimer subscribers
        private List<ITimerSubscriber> m_ITimerSubscribers = new List<ITimerSubscriber>();
        #endregion// members

        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public ExecutionListener(string name, IEngineHub engineHub)
        {
            m_EngineHub = engineHub;
            if (engineHub is Hub)
                this.Log = ((Hub) engineHub).Log;
            m_TTService = TTApiService.GetInstance();
            m_TTService.TryAddExecutionListenerToShutDownList(this);
            m_Name = name;
        }
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
        private string Name
        {
            get { return System.Threading.Thread.CurrentThread.Name; }
        }
        //
        #endregion//Properties

        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        // *****************************************
        // ****         InitializeThread()      ****
        // *****************************************
        /// <summary>
        /// Called to allow dispatcher to be attached to thread.  Once API is present,
        /// OnInitialized is called to allow subscribers to use thread to set up.
        /// </summary>
        public void InitializeThread()
        {
            m_Dispatcher = TradingTechnologies.TTAPI.Dispatcher.AttachWorkerDispatcher();
            if (m_TTService.session == null)
            { // no session available yet, subscribe so we can wait for it to come online
                m_TTService.ServiceStateChanged += new EventHandler(TTApiService_ServiceStateChanged);
                m_IsWaitingForApiConnection = true;
            }
            else
                OnInitialized();  // session is ready, go to work!

            m_Dispatcher.Run();
        }
        //
        //
        //
        // *****************************************
        // ****           StopThread()          ****
        // *****************************************
        /// <summary>
        /// threadsafe call to signal nice shutdown
        /// </summary>
        public void StopThread()
        {
            if (m_isDisposing) return;
            m_isDisposing = true;
            m_Dispatcher.BeginInvoke(new Action(Shutdown));   // allow our order engine to nicely shutdown
            try
            {
                m_Dispatcher.Run();
            }
            catch (Exception)
            {
            }
        }
        //
        //
        // *****************************************************
        // ****             SubscribeToTimer()              ****
        // *****************************************************
        /// <summary>
        /// Caller would like to get periodic updates from the hub.
        /// </summary>
        /// <param name="subscriber"></param>
        public void SubscribeToTimer(ITimerSubscriber subscriber)
        {
            Log.NewEntry(LogLevel.Warning, "SubscribeToTimer: Strategy {0} subscribing.", subscriber.GetType().Name);
            m_ITimerSubscribers.Add(subscriber);
        }
        //
        #endregion// public methods

        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        //
        //***********************************************
        //****             Shutdown()                ****
        //***********************************************
        /// <summary>
        /// Carefully allow everyone to shutdown and remove TT subscriptions.
        /// </summary>
        private void Shutdown()
        {
            OnStopping();           // call subscribed order engines to allow them to stop nicely.
            m_TTService.TryRemoveExecutionListenerFromShutDownList(this);
            if (m_Dispatcher != null && (!m_Dispatcher.IsDisposed))
            {
                m_Dispatcher.BeginInvokeShutdown();
                m_Dispatcher = null;
            }

            foreach (PriceSubscription subscription in m_PriceSubscriptions.Values)
                subscription.Dispose();
            foreach (InstrumentCatalogSubscription sub in m_InstrumentCatalogs.Values)
                sub.Dispose();
            m_PriceSubscriptions.Clear();
            m_InstrumentCatalogs.Clear();
        }//StopThread()
        #endregion//Private Methods

        #region Price Listening - TT Callback Event Handlers
        // *****************************************************************************
        // ****                     TT Callback Event Handlers                      ****
        // *****************************************************************************
        //
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void InstrumentLookup_InstrumentUpdated(object sender, InstrumentLookupSubscriptionEventArgs eventArgs)
        {
            if (eventArgs.Instrument != null && eventArgs.Error == null)
            {
                UVProd.InstrumentName instrName;
                Instrument ttInstrument = eventArgs.Instrument;
                if (TTConvertNew.TryConvert(ttInstrument, out instrName))
                {   // Success in converting to our internal naming scheme.
                    InstrumentDetails details;
                    if (m_TTInstrumentDetails.TryGetValue(instrName, out details))
                    {   // This instrument was already added!
                        if (!ttInstrument.Key.Equals(details.Key))
                            Log.NewEntry(LogLevel.Warning, "{0}: Instrument {1} found before with non-unique key {2}!", this.Name, instrName.FullName, instrName.SeriesName);
                        else
                            Log.NewEntry(LogLevel.Warning, "{0}: Instrument {1} found before and keys match! Good.", this.Name, instrName.FullName);
                    }
                    else
                    {   // Add new InstrumentDetails and a new market to our container.
                        m_KeyToInstruments.Add(ttInstrument.Key, instrName);
                        m_TTInstrumentDetails.Add(instrName, ttInstrument.InstrumentDetails);
                        Log.NewEntry(LogLevel.Minor, "{0}: Instruments found {1} <---> {2}.", this.Name, instrName, ttInstrument.Key.ToString());

                        if (!m_ExecContainer.m_Markets.ContainsKey(instrName))
                        {
                            UV.Lib.BookHubs.Market newMarket = UV.Lib.BookHubs.Market.Create(instrName);
                            m_ExecContainer.m_Markets.Add(instrName, newMarket);
                        }

                        if (sender is InstrumentLookupSubscription)
                        {
                            InstrumentLookupSubscription instSubscription = (InstrumentLookupSubscription)sender;
                            if (!m_InstrumentLookups.ContainsValue(instSubscription))
                            {   // If user called for instr info using only a series name, and not key, we couldn't store subscription object then.
                                // Store it now!
                                m_InstrumentLookups.Add(ttInstrument.Key, instSubscription);
                                Log.NewEntry(LogLevel.Minor, "{0}: Adding new Instrument Subscription found {1}.", this.Name, instrName);
                            }
                        }
                        ProcessInstrumentsFound(instrName, ttInstrument.InstrumentDetails);
                    }
                    SubscribeTo(ttInstrument.Key);
                }
                else
                {   // Failed to convert TT instrument to a UV Instrument.
                    // This happens because either their name is too confusing to know what it is.
                    // Or, more likely, we are set to ignore the product type (options, equity, swaps).
                    Log.NewEntry(LogLevel.Warning, "{0}: Instrument creation failed for {1}.", this.Name, ttInstrument.Key.ToString());
                }

            }
            else if (eventArgs.IsFinal)
            {   // Instrument was not found and TTAPI has given up on looking.
                if (eventArgs.Instrument != null)
                    Log.NewEntry(LogLevel.Warning, "{0}: TTAPI gave up looking for {1}.", this.Name, eventArgs.Instrument.Key.ToString());
                else
                    Log.NewEntry(LogLevel.Warning, "{0}: TTAPI gave up looking for something. ", this.Name, eventArgs.RequestInfo.ToString());
            }

        }//InstrumentLookup_Callback()
        //
        //
        //
        // Local work space for PriceSubscription_Updated.
        private bool isSnapShot;
        private bool isBidPriceChange;
        private bool isAskPriceChange;
        private bool isBidQtyChange;
        private bool isAskQtyChange;
        private bool isFireMarketChangeEvent;
        private bool isFireBestPriceChangeEvent;
        //
        //
        //******************************************************************
        // ****             PriceSubscription_Updated()                 ****
        //******************************************************************
        /// <summary>
        /// Note: Currently we don't care about last price traded or volume, so for speed don't update those
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void PriceSubscription_Updated(object sender, FieldsUpdatedEventArgs eventArgs)
        {
            if (m_isDisposing) return;
            isSnapShot = (eventArgs.UpdateType == UpdateType.Snapshot);
            isFireMarketChangeEvent = false;

            if (eventArgs.Error != null)
            { // error in price feed
                Log.NewEntry(LogLevel.Warning, "{0}: Error in price subscription {1}.", this.Name, eventArgs.Error.Message);
                return;
            }

            UVProd.InstrumentName instrumentName;
            InstrumentKey key = eventArgs.Fields.Instrument.Key;
            if (!m_KeyToInstruments.TryGetValue(key, out instrumentName))
            { // we aren't aware of this key
                Log.NewEntry(LogLevel.Warning, "{0}: Failed to find instrument for TTKey {1}.", this.Name, key);
                return;
            }
            // 
            // Find our market and update
            //
            UV.Lib.BookHubs.Market market = m_ExecContainer.m_Markets[instrumentName];
            market.BestDepthUpdated[MarketBase.BidSide] = market.DeepestLevelKnown + 1;   // reset our updates
            market.BestDepthUpdated[MarketBase.AskSide] = market.DeepestLevelKnown + 1;
            market.DeepestLevelKnown = Math.Min(MarketBase.MaxDepth, eventArgs.Fields.GetLargestCurrentDepthLevel()); ;

            FieldId[] changedFieldIds;
            for (int changedDepth = 0; changedDepth < market.DeepestLevelKnown; changedDepth++)
            {
                changedFieldIds = eventArgs.Fields.GetChangedFieldIds(changedDepth);
                if (changedFieldIds.Length > 0)
                {
                    if (!isSnapShot)
                    { // if it is a snapshot, everything gets updated anyways
                        isBidPriceChange = changedFieldIds.Contains<FieldId>(FieldId.BestBidPrice);
                        if (!isBidPriceChange) // only check if it isn't a price update on this side
                            isBidQtyChange = changedFieldIds.Contains<FieldId>(FieldId.BestBidQuantity);
                        isAskPriceChange = changedFieldIds.Contains<FieldId>(FieldId.BestAskPrice);
                        if (!isAskPriceChange)
                            isAskQtyChange = changedFieldIds.Contains<FieldId>(FieldId.BestAskQuantity);
                    }
                    // *****************************************************
                    // ****             MarketBase updates              ****
                    // *****************************************************
                    //
                    // Bid side
                    //
                    if (isSnapShot || isBidPriceChange || isBidQtyChange)
                    {
                        isFireMarketChangeEvent = true;
                        Price p = (Price)eventArgs.Fields.GetDirectBidPriceField(changedDepth).Value;
                        Quantity q = (Quantity)eventArgs.Fields.GetDirectBidQuantityField(changedDepth).Value;
                        if (p.IsValid && p.IsTradable)
                        {
                            market.Price[MarketBase.BidSide][changedDepth] = p.ToDouble();
                            market.Qty[MarketBase.BidSide][changedDepth] = q.ToInt();
                            if (changedDepth < market.BestDepthUpdated[MarketBase.BidSide])
                                market.BestDepthUpdated[MarketBase.BidSide] = changedDepth;
                        }
                    }
                    else if (changedFieldIds.Contains<FieldId>(FieldId.DirectBidQuantity) || changedFieldIds.Contains<FieldId>(FieldId.DirectBidPrice))
                    { // As far as I can tell this situation never occurs, I am logging it because it seems odd that everything is labeled "best"
                        Log.NewEntry(LogLevel.Error, "PriceSubscription_Updated: Direct Bid Change Without Best Bid Change Detected");
                    }
                    //
                    // Ask side
                    //
                    if (isSnapShot || isAskPriceChange || isAskQtyChange)
                    {
                        isFireMarketChangeEvent = true;
                        Price p = (Price)eventArgs.Fields.GetDirectAskPriceField(changedDepth).Value;
                        Quantity q = (Quantity)eventArgs.Fields.GetDirectAskQuantityField(changedDepth).Value;
                        if (p.IsValid && p.IsTradable)
                        {
                            market.Price[MarketBase.AskSide][changedDepth] = p.ToDouble();
                            market.Qty[MarketBase.AskSide][changedDepth] = q.ToInt();
                            if (changedDepth < market.BestDepthUpdated[MarketBase.AskSide])
                                market.BestDepthUpdated[MarketBase.AskSide] = changedDepth;
                        }
                    }
                    else if (changedFieldIds.Contains<FieldId>(FieldId.DirectAskQuantity) || changedFieldIds.Contains<FieldId>(FieldId.DirectAskPrice))
                    { // As far as I can tell this situation never occurs, I am logging it because it seems odd that everything is labeled "best"
                        Log.NewEntry(LogLevel.Error, "PriceSubscription_Updated: Direct Ask Change Without Best Ask Change Detected");
                    }

                    if (changedDepth == 0)
                    {
                        // *****************************************************
                        // ****             Top Of Book Price Change        ****
                        // *****************************************************
                        if (isFireMarketChangeEvent)
                        {
                            if (isSnapShot || isBidPriceChange || isAskPriceChange)
                                isFireBestPriceChangeEvent = true;
                        }

                        // *****************************************************
                        // ****             Series Status updates           ****
                        // *****************************************************
                        if (changedFieldIds.Contains<FieldId>(FieldId.SeriesStatus))
                        {
                            TradingStatus status = (TradingStatus)eventArgs.Fields.GetField(FieldId.SeriesStatus).Value;
                            Log.NewEntry(LogLevel.Minor, "PriceListener: SeriesStatus change {0} is {1}.", instrumentName, status.ToString());
                            if (status == TradingStatus.Trading)
                                market.IsMarketGood = true;
                            else if (status == TradingStatus.Closed || status == TradingStatus.ClosingAuction || status == TradingStatus.Expired ||
                                status == TradingStatus.NotTradable || status == TradingStatus.PostTrading)
                                market.IsMarketGood = false;
                            else
                                market.IsMarketGood = false;
                        }

                        // *****************************************************
                        // ****             Session Rollover                ****
                        // *****************************************************
                        if (changedFieldIds.Contains<FieldId>(FieldId.SessionRollover))
                        {
                            TradingStatus status = (TradingStatus)eventArgs.Fields.GetField(FieldId.SeriesStatus).Value;
                            Log.NewEntry(LogLevel.Minor, "PriceListener: SessionRollover change {0} is {1}.", instrumentName, status.ToString());
                            if (status == TradingStatus.Trading)
                                market.IsMarketGood = true;
                            else if (status == TradingStatus.Closed || status == TradingStatus.ClosingAuction || status == TradingStatus.Expired ||
                                status == TradingStatus.NotTradable || status == TradingStatus.PostTrading)
                                market.IsMarketGood = false;
                            else
                                market.IsMarketGood = false;
                        }
                    }
                }// end changed fields lenght
            } // end changedepth loop

            // *****************************************************
            // ****             Fire Events Now                 ****
            // *****************************************************
            if (isFireMarketChangeEvent)
                market.OnMarketChanged();
            if (isFireBestPriceChangeEvent)
                market.OnMarketBestPriceChanged();
        }//PrieceSubscription()
        //
        #endregion//Price Listening - TT Callback Event Handlers

        #region Price Listening - Private Price Sub Methods
        // *****************************************************************************
        // ****                Private Price Subscription Methods                   ****
        // *****************************************************************************
        //
        // ************************************************
        // ****             SubscribeTo                ****
        // ************************************************
        /// <summary>
        /// Called after a user requests a book be created, we need to subscribe to 
        /// price events for it.
        /// </summary>
        /// <param name="instrName"></param>
        private void SubscribeTo(UVInstrName instrName)
        {
            Log.NewEntry(LogLevel.Major, "{0}: InstrumentLookup {1} {2}.", this.Name, instrName.Product, instrName.SeriesName);
            TradingTechnologies.TTAPI.ProductKey ttProductKey;
            TTConvertNew.TryConvert(instrName.Product, out ttProductKey);
            InstrumentLookupSubscription subscriber = new InstrumentLookupSubscription(m_TTService.session, m_Dispatcher, ttProductKey, instrName.SeriesName);
            subscriber.Update += new EventHandler<InstrumentLookupSubscriptionEventArgs>(InstrumentLookup_InstrumentUpdated);
            subscriber.Start();
        }
        //
        //
        // ************************************************
        // ****             SubscribeTo                ****
        // ************************************************
        /// <summary>
        /// After we have a TT instrument key we can start the actual subscription
        /// to the prices.  Currently set to always request market depth.
        /// </summary>
        /// <param name="instrKey"></param>
        private void SubscribeTo(InstrumentKey instrKey)
        {
            Instrument instrument = null;
            InstrumentLookupSubscription instrumentSub = null;                      // or find a specific instrument subscription.
            if (m_InstrumentLookups.TryGetValue(instrKey, out instrumentSub))
            {
                instrument = instrumentSub.Instrument;
            }
            else
            {
                Log.NewEntry(LogLevel.Minor, "{0}: I failed to find instrument key {1}.", this.Name, instrKey.ToString());
                return;
            }
            if (instrument != null)
            {
                // Subscribe or update pre-existing subscription.
                PriceSubscription priceSub = null;
                if (!m_PriceSubscriptions.TryGetValue(instrument.Key, out priceSub))
                {   // Can't find a subscription, so create one.
                    Log.NewEntry(LogLevel.Major, "{0}: Creating new market depth subscription for {1}", this.Name, instrument.Name);
                    priceSub = new PriceSubscription(instrument, Dispatcher.Current);
                    m_PriceSubscriptions.Add(instrument.Key, priceSub);                                     // add to our list of subscription objects.
                    priceSub.FieldsUpdated += new FieldsUpdatedEventHandler(PriceSubscription_Updated);     // attach my handler to it.
                    priceSub.Settings = new PriceSubscriptionSettings(PriceSubscriptionType.MarketDepth);
                    priceSub.Start();
                }
            }
        }//SubscribeTo()  
        //
        //
        // *****************************************************************
        // ****             Process Instruments Found()                 ****
        // *****************************************************************
        //
        /// <summary>
        /// When a new instrument is found, this will create the UV instrument details and fire the 
        /// appropiate events to sbuscribers who would like to know about the insturment and details
        /// </summary>
        /// <param name="uvInstrName"></param>
        /// <param name="ttInstrDetails"></param>
        private void ProcessInstrumentsFound(UVInstrName uvInstrName, InstrumentDetails ttInstrDetails)
        {
            UVInstrDetails uvInstrDetails = TTConvertNew.CreateUVInstrumentDetails(uvInstrName, ttInstrDetails); // create our own instrument details and save them.
            m_UVInstrumentDetails[uvInstrName] = uvInstrDetails;
            if (m_PendingOrderBooks.ContainsKey(uvInstrName))
            {
                while (m_PendingOrderBooks[uvInstrName].Count > 0)
                { // remove from list and send to be processed.
                    UVOrderBook orderBookToProcess = m_PendingOrderBooks[uvInstrName][0];
                    m_PendingOrderBooks[uvInstrName].Remove(orderBookToProcess);
                    ProcessCreateBook(orderBookToProcess);
                }
            }
            OnInstrumentsFound(uvInstrDetails);
        }
        //
        #endregion // Price Listening - Private Price Sub Methods

        #region Order Listening - TT CallBack Event Handlers
        // *****************************************************************
        // ****           TT Order CallBack Event Handlers              ****
        // *****************************************************************
        //
        // *********************************************************
        // ****         InstrumentLookUp_Update()               ****
        // *********************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void InstrumentLookup_Update(object sender, InstrumentLookupSubscriptionEventArgs eventArgs)
        {
            if (eventArgs.Error == null && eventArgs.Instrument != null)
            {   // Instrument found - store it.
                Instrument instrument = eventArgs.Instrument;
                InstrumentKey key = instrument.Key;
                if (m_InstrumentsRequested.Contains(key))
                {
                    Log.NewEntry(LogLevel.Minor, "{0}: InstrumentLookup_Update found instrument {1}.", m_Name, instrument.Name);
                    m_InstrumentsRequested.Remove(key);
                }
                m_TTInstrKeyToTTInstr.Add(key, instrument);                                         // save for quick lookup
                UVInstrName uvInstr;                                                               // try and create a uv instrument
                if (UV.TTServices.TTConvertNew.TryConvert(instrument, out uvInstr))
                {
                    m_TTInstrKeyToUVInstr.Add(key, uvInstr);
                }
                else
                    Log.NewEntry(LogLevel.Warning, "{0}: InstrumentLookup_Update failed to convert TT instrument {1} to UV Instrument ", m_Name, instrument);

                // Find the first live trading feed.  If none are enabled, we still want to grab one since this seems to work.
                Log.BeginEntry(LogLevel.Minor, "{0}: Found enabled trading feeds:", m_Name);
                foreach (OrderFeed orderFeed in instrument.GetValidOrderFeeds())
                {
                    Log.AppendEntry(" {0}", orderFeed.Name);
                    if (orderFeed.IsTradingEnabled)
                    {
                        m_DefaultOrderFeeds[instrument.Key] = orderFeed;
                        Log.AppendEntry(" [Default]");
                        break;
                    }
                    else
                    {
                        Log.NewEntry(LogLevel.Error, "Order Feed Is Not Enabled: Using Anyways");
                        m_DefaultOrderFeeds[instrument.Key] = orderFeed;  // testing subscribing anyways to see what happens
                    }
                }
                Log.AppendEntry(". ");
                Log.EndEntry();
            }
            else if (eventArgs.IsFinal)
            {
                if (!eventArgs.RequestInfo.IsByName)
                {   // User supplied the InstrumentKey, so we can remove it from our pending list.
                    InstrumentKey key = eventArgs.RequestInfo.InstrumentKey;
                    Log.NewEntry(LogLevel.Warning, "{0}: InstrumentLookup_Update failed to find instrument with Key={1} {2}.", m_Name, key.ProductKey.Name, key.SeriesKey);
                }

            }
        }//InstrumentLookup_Update()
        //
        //
        // *************************************************************
        // ****                TT_OrderFilled                       ****
        // *************************************************************
        /// <summary>
        /// Procces TT Order Filled Event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void TT_OrderFilled(object sender, TradingTechnologies.TTAPI.OrderFilledEventArgs eventArgs)
        {
            Order order = eventArgs.NewOrder;
            string siteKey = order.SiteOrderKey;
            m_TTOrders[siteKey] = order;        // store this order regardless of whose it is...this probably needs to be changed! only store this execution sets orders!
            int uvOrderId = -1;
            if (m_MapTT2UV.TryGetValue(siteKey, out uvOrderId))
            { // we found our order id
                int mktSide = TTConvertNew.ToMarketSide(eventArgs.Fill.BuySell);
                int mktSign = UV.Lib.Utilities.QTMath.MktSideToMktSign(mktSide);
                UVInstrName uvInstr;
                if (m_TTInstrKeyToUVInstr.TryGetValue(eventArgs.Fill.InstrumentKey, out uvInstr))
                { // we found the uv instrument 

                    UVFill aFill = UVFill.Create(                                // create new fill
                    (mktSign * eventArgs.Fill.Quantity),                                                       
                    eventArgs.Fill.MatchPrice.ToDouble(),
                    uvInstr, Log.GetTime(), eventArgs.Fill.TransactionDateTime);
                    
                    Log.NewEntry(LogLevel.Minor, "TT Order {4} Filled : {0} {1} {2} @ {3}",
                                    uvInstr,                            // 0
                                    eventArgs.Fill.BuySell,             // 1
                                    eventArgs.Fill.Quantity,            // 2
                                    eventArgs.Fill.MatchPrice,          // 3  
                                    eventArgs.NewOrder.SiteOrderKey);   // 4    

                    bool isCompleteFill = eventArgs.FillType == FillType.Full;
                    UVFillEventArgs fillEvent = new UVFillEventArgs(aFill, uvOrderId, uvInstr, isCompleteFill); // create new fill event arg
                    m_ExecContainer.m_OrderInstruments[uvInstr].ProcessFill(fillEvent);                         // this will process the order state changes and send out appropiate events
                    if (isCompleteFill)
                        if (m_OrderIdsPendingModification.Contains(uvOrderId))
                            m_OrderIdsPendingModification.Remove(uvOrderId);
                }
            }
            else
            {
                // this order must not belong to this exeuction thread / system?
            }
        }
        //
        // *************************************************************
        // ****                TT_OrderAdded                        ****
        // *************************************************************
        /// <summary>
        /// Process TT Order Added Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void TT_OrderAdded(object sender, TradingTechnologies.TTAPI.OrderAddedEventArgs eventArgs)
        {
            Order ttOrder = eventArgs.Order;
            string siteKey = ttOrder.SiteOrderKey;
            int uvOrderId = -1;

            if (m_MapTT2UV.TryGetValue(siteKey, out uvOrderId))
            { // we found our order id, this is an order from this execution container
                m_TTOrders[siteKey] = ttOrder;                      // store this order 

                if (m_TTOrderKeysPendingDelete.Contains(siteKey))
                { // this order is pending deletion, who cares about updating it, just immediately send it to be deleted
                    m_TTOrderKeysPendingDelete.Remove(siteKey);     // remove from pending list
                    DeletePendingOrder(ttOrder);                    // delete order 
                }

                UVInstrName uvInstr;
                if (m_TTInstrKeyToUVInstr.TryGetValue(eventArgs.Order.InstrumentKey, out uvInstr))
                { // we found the uv instrumnet 
                    Log.NewEntry(LogLevel.Minor, "TT Order {4} Added : {0} {1} {2} @ {3}",
                                    uvInstr,                            // 0
                                    eventArgs.Order.BuySell,            // 1
                                    eventArgs.Order.OrderQuantity,      // 2
                                    eventArgs.Order.LimitPrice,         // 3
                                    eventArgs.Order.SiteOrderKey);      // 4
                    UVOrder uvOrderToUpdate;
                    if (m_ExecContainer.m_OrderInstruments[uvInstr].TryGetOrder(uvOrderId, out uvOrderToUpdate))
                    { // since we have the order, lets just update everything now.
                        if (ttOrder.BuySell == BuySell.Buy)
                        {
                            uvOrderToUpdate.OriginalQtyConfirmed = ttOrder.OrderQuantity;
                            uvOrderToUpdate.ExecutedQty = ttOrder.FillQuantity;                 // just in case..I don't think this will ever happen
                        }
                        else
                        {
                            uvOrderToUpdate.OriginalQtyConfirmed = ttOrder.OrderQuantity * -1;
                            uvOrderToUpdate.ExecutedQty = ttOrder.FillQuantity * -1;
                        }
                        uvOrderToUpdate.IPriceConfirmed = ttOrder.LimitPrice.ToTicks() / ttOrder.InstrumentDetails.SmallestTickIncrement;
                        uvOrderToUpdate.OrderStateConfirmed = TTConvertNew.ToUVOrderState(ttOrder.TradeState);
                        m_ExecContainer.m_OrderInstruments[uvInstr].ProcessAddConfirm(uvOrderToUpdate);           // this will call appropiate events, and set pending changes flag

                        //
                        // Check if we have pending changes
                        //
                        if (m_OrderIdsPendingModification.Contains(uvOrderToUpdate.Id))
                        { // we have pending modifications to this order, currently we place priority on price rather than qty, 
                            // but maybe in the future we shoud just change both at once.
                            m_OrderIdsPendingModification.Remove(uvOrderToUpdate.Id);
                            if (uvOrderToUpdate.IPriceConfirmed != uvOrderToUpdate.IPricePending)
                                ProcessChangeOrderPrice(uvOrderToUpdate);
                            else if (uvOrderToUpdate.OriginalQtyConfirmed != uvOrderToUpdate.OriginalQtyPending)
                                ProcessChangeOrderQty(uvOrderToUpdate);
                        }
                    }
                    else
                    {
                        Log.NewEntry(LogLevel.Error, "TT_OrderAdded: UVOrder for Id {0} was not found", uvOrderId);
                    }
                }
            }
        }
        //
        // *************************************************************
        // ****                TT_OrderDeleted                      ****
        // *************************************************************
        /// <summary>
        /// Process TT Order Deleted Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void TT_OrderDeleted(object sender, TradingTechnologies.TTAPI.OrderDeletedEventArgs eventArgs)
        {
            Order order = eventArgs.OldOrder;
            string siteKey = order.SiteOrderKey;
            int uvOrderId = -1;

            if (m_MapTT2UV.TryGetValue(siteKey, out uvOrderId))
            { // we found our order id
                m_TTOrders[siteKey] = order;                // store this order 
                int mktSide = TTConvertNew.ToMarketSide(eventArgs.OldOrder.BuySell);
                UVInstrName uvInstr;
                if (m_TTInstrKeyToUVInstr.TryGetValue(eventArgs.OldOrder.InstrumentKey, out uvInstr))
                { // we found the uv instrumnet 
                    Log.NewEntry(LogLevel.Minor, "TT Order {0} Deleted : {1} {2}",
                                    eventArgs.OldOrder.SiteOrderKey,        // 0
                                    uvInstr,                                // 1
                                    eventArgs.Message);                     // 2
                    UVOrder uvOrderToUpdate;
                    if (m_ExecContainer.m_OrderInstruments[uvInstr].TryGetOrder(uvOrderId, out uvOrderToUpdate))
                        m_ExecContainer.m_OrderInstruments[uvInstr].ProcessDeleteConfirm(uvOrderToUpdate);        // this will call events and update order status / qty
                    if (m_OrderIdsPendingModification.Contains(uvOrderId))
                        m_OrderIdsPendingModification.Remove(uvOrderId);
                }
            }
        }
        //
        // *************************************************************
        // ****                TT_OrderRejected                     ****
        // *************************************************************
        /// <summary>
        /// Process TT Order Rejected Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void TT_OrderRejected(object sender, OrderRejectedEventArgs eventArgs)
        {
            Order ttOrder = eventArgs.Order;
            string siteKey = ttOrder.SiteOrderKey;
            if (ttOrder.Action == OrderAction.Add || ttOrder.Action == OrderAction.Change)
            { // rejected order for add.

                int uvOrderId = -1;
                if (m_MapTT2UV.TryGetValue(siteKey, out uvOrderId))
                { // we found our order id
                    m_TTOrders[siteKey] = ttOrder;                // store this order
                    UVInstrName uvInstr;
                    if (m_TTInstrKeyToUVInstr.TryGetValue(eventArgs.Order.InstrumentKey, out uvInstr))
                    { // we found the uv instrumnet 
                        UVOrder uvOrder;
                        if (m_ExecContainer.m_OrderInstruments[uvInstr].TryGetOrder(uvOrderId, out uvOrder))
                        {
                            if (ttOrder.Action == OrderAction.Add)
                            { // adding order was rejected
                                Log.NewEntry(LogLevel.Minor, "TT Order {2} Add Rejected {3}: {0} {1} ",
                                        uvInstr,                            // 0
                                        eventArgs.Message,                  // 1
                                        ttOrder.SiteOrderKey,               // 2
                                        ttOrder.Action);                    // 3
                                m_ExecContainer.m_OrderInstruments[uvInstr].ProcessAddReject(uvOrder);
                            }
                            else if (ttOrder.Action == OrderAction.Change)
                            { // changing order was rejected
                                Log.NewEntry(LogLevel.Minor, "TT Order {2} Change Rejected {3}: {0} {1} ",
                                        uvInstr,                            // 0
                                        eventArgs.Message,                  // 1
                                        ttOrder.SiteOrderKey,               // 2
                                        ttOrder.Action);                    // 3
                            }
                            if (m_OrderIdsPendingModification.Contains(uvOrderId))
                                m_OrderIdsPendingModification.Remove(uvOrderId);
                        }
                    }
                }
            }
            else if (ttOrder.Action == OrderAction.Replace)
            { // replace was rejected, previous order still exists
                Log.NewEntry(LogLevel.Minor, "TT Replace Order {0} Rejected : {1}",
                                       eventArgs.Order.SiteOrderKey,   //0
                                       eventArgs.Order.Message);       //1
            }
            else if (ttOrder.Action == OrderAction.Delete)
            { // TODO : add to pending and figure out how to recall it.
            }
        }
        //
        // *************************************************************
        // ****                TT_OrderUpdated                      ****
        // *************************************************************
        /// <summary>
        /// Process TT Order Changed Events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void TT_OrderUpdated(object sender, OrderUpdatedEventArgs eventArgs)
        {
            Order ttOrder = eventArgs.NewOrder;
            string siteKey = ttOrder.SiteOrderKey;
            int uvOrderId = -1;

            if (m_MapTT2UV.TryGetValue(siteKey, out uvOrderId))
            { // we found our order id
                m_TTOrders[siteKey] = ttOrder;                // store this order regardless of whos it is.
                UVInstrName uvInstr;
                if (m_TTInstrKeyToUVInstr.TryGetValue(eventArgs.NewOrder.InstrumentKey, out uvInstr))
                { // we found the uv instrumnet 
                    Log.NewEntry(LogLevel.Minor, "TT Order {4} Updated : {0} {1} {2} @ {3}",
                                    uvInstr,     // 0
                                    eventArgs.NewOrder.BuySell,           // 1
                                    eventArgs.NewOrder.OrderQuantity,     // 2
                                    eventArgs.NewOrder.LimitPrice,        // 3
                                    eventArgs.NewOrder.SiteOrderKey);     // 4

                    UVOrder uvOrderToUpdate;
                    if (m_ExecContainer.m_OrderInstruments[uvInstr].TryGetOrder(uvOrderId, out uvOrderToUpdate))
                    { // since we have the order, lets just update everything now.
                        if (ttOrder.BuySell == BuySell.Buy)
                        {
                            uvOrderToUpdate.OriginalQtyConfirmed = ttOrder.OrderQuantity;
                            uvOrderToUpdate.ExecutedQty = ttOrder.FillQuantity;                 // just in case..I don't think this will ever happen
                        }
                        else
                        {
                            uvOrderToUpdate.OriginalQtyConfirmed = ttOrder.OrderQuantity * -1;
                            uvOrderToUpdate.ExecutedQty = ttOrder.FillQuantity * -1;
                        }
                        uvOrderToUpdate.IPriceConfirmed = ttOrder.LimitPrice.ToTicks() / ttOrder.InstrumentDetails.SmallestTickIncrement;
                        uvOrderToUpdate.OrderStateConfirmed = TTConvertNew.ToUVOrderState(ttOrder.TradeState);
                        uvOrderToUpdate.ChangesPending = false;
                        //
                        // Check if we have changes waiting to be sent out
                        //
                        if (m_OrderIdsPendingModification.Contains(uvOrderToUpdate.Id))
                        { // we have pending modifications to this order, currently we place priority on price rather than qty, 
                            // but maybe in the future we shoud just change both at once.
                            m_OrderIdsPendingModification.Remove(uvOrderToUpdate.Id);
                            if (uvOrderToUpdate.IPriceConfirmed != uvOrderToUpdate.IPricePending)
                                ProcessChangeOrderPrice(uvOrderToUpdate);
                            else if (uvOrderToUpdate.OriginalQtyConfirmed != uvOrderToUpdate.OriginalQtyPending)
                                ProcessChangeOrderQty(uvOrderToUpdate);
                        }
                    }
                }
            }
        }
        #endregion//TT Order Callback

        #region Order Listening - Private Order Handling Methods
        // *****************************************************************************
        // ****                    Private Order Hanlding Methods                   ****
        // *****************************************************************************
        //
        //
        //*********************************************************************
        //****             SubscribeToOrdersForInstrument()                ****
        //*********************************************************************
        /// <summary>
        /// Called after creating a book to subscribe to orders for an specific instrument
        /// </summary>
        /// <param name="instrKey"></param>
        private void SubscribeToOrdersForInstrument(InstrumentKey instrKey)
        {
            if (instrKey == default(InstrumentKey))
            {
                return;
            }
            //
            // Request the instrument (for order submission)
            //
            if (m_InstrumentsRequested.Contains(instrKey) || m_TTInstrKeyToTTInstr.ContainsKey(instrKey))
            {   // Only get instruments once!
                Log.NewEntry(LogLevel.Warning, "{0}: Duplicate request for instrument {1} {2} will be ignored.", m_Name, instrKey.ProductKey.Name, instrKey.SeriesKey);
            }
            else
            {
                m_InstrumentsRequested.Add(instrKey);                // keep track of this request.
                InstrumentLookupSubscription instrSubscription = new InstrumentLookupSubscription(m_TTService.session, m_Dispatcher, instrKey);
                instrSubscription.Update += new EventHandler<InstrumentLookupSubscriptionEventArgs>(InstrumentLookup_Update);
                instrSubscription.Start();
            }
            //
            // Create the order subscriptions.
            //
            TradeSubscription ts = new TradeSubscription(m_TTService.session, m_Dispatcher);
            ts.OrderFilled += new EventHandler<TradingTechnologies.TTAPI.OrderFilledEventArgs>(TT_OrderFilled);
            ts.OrderAdded += new EventHandler<TradingTechnologies.TTAPI.OrderAddedEventArgs>(TT_OrderAdded);
            ts.OrderDeleted += new EventHandler<TradingTechnologies.TTAPI.OrderDeletedEventArgs>(TT_OrderDeleted);
            ts.OrderUpdated += new EventHandler<TradingTechnologies.TTAPI.OrderUpdatedEventArgs>(TT_OrderUpdated);
            ts.OrderRejected += new EventHandler<OrderRejectedEventArgs>(TT_OrderRejected);

            // now add desired filters...
            TradeSubscriptionInstrumentFilter tsif = new TradeSubscriptionInstrumentFilter(m_TTService.session, instrKey, false, "InstrFilter");
            ts.SetFilter(tsif);

            ts.Start();
        }
        //
        //
        // *****************************************
        // ****         ProcessCreateBook()     ****
        // *****************************************
        /// <summary>
        /// Process to initialize a single order book.
        /// </summary>
        /// <param name="request"></param>
        private void ProcessCreateBook(UVOrderBook orderBook)
        {
            // Locate the OrderInstrument for this book.
            OrderInstrument orderInstrument = null;
            if (!m_ExecContainer.m_OrderInstruments.TryGetValue(orderBook.Instrument, out orderInstrument))
            {   // We do not have an order instrument for this Instrument.
                UVInstrDetails instrumentDetails;
                if (!m_UVInstrumentDetails.TryGetValue(orderBook.Instrument, out instrumentDetails))
                {   // We don't know this instrument yet.
                    // Request information from market, and set this to pending.
                    Log.NewEntry(LogLevel.Minor, "ProcessCreateBook: Requesting instrument details for {0} OrderBookID {1}. Will try again later.",
                        orderBook.Instrument, orderBook.BookID);
                    SubscribeTo(orderBook.Instrument);
                    if (m_PendingOrderBooks.ContainsKey(orderBook.Instrument))
                    { // we have other order books waiting on this instrument details, just add to the list
                        m_PendingOrderBooks[orderBook.Instrument].Add(orderBook);
                    }
                    else
                    { // we don't have anything waiting on this instrument yet, so create a new list.
                        m_PendingOrderBooks[orderBook.Instrument] = new List<UVOrderBook> { orderBook };
                    }
                    return;
                }
                else
                {   // We have the instrument details, so create the OrderInstrument now
                    Log.NewEntry(LogLevel.Minor, "ProcessCreateBook: Attempting to Process OrderBookID {0}", orderBook.BookID);
                    orderInstrument = new OrderInstrument(orderBook.Instrument, instrumentDetails);
                    TradingTechnologies.TTAPI.InstrumentDetails ttDetails;

                    if (m_TTInstrumentDetails.TryGetValue(orderBook.Instrument, out ttDetails))
                    {
                        m_InstrumentNameToTTKey[orderBook.Instrument] = ttDetails.Key;
                    }
                    m_ExecContainer.m_OrderInstruments.Add(orderBook.Instrument, orderInstrument);
                    Log.NewEntry(LogLevel.Minor, "ProcessCreateBook: Created new OrderInstrument {0}.", orderBook.Instrument);
                }
                // If we get here, lets try to add our new book to the order instrument.
                InstrumentKey ttKey;
                if (orderInstrument != null && orderInstrument.TryAddBook(orderBook) &&
                    m_InstrumentNameToTTKey.TryGetValue(orderBook.Instrument, out ttKey))
                {
                    Log.NewEntry(LogLevel.Minor, "ProcessCreateBook: Added book {0} into OrderInstrument {1}.", orderBook.BookID, orderInstrument);
                    SubscribeToOrdersForInstrument(ttKey);
                }
            }
            else
            {
                Log.NewEntry(LogLevel.Minor, "ProcessCreateBook: Exisiting OrderInstrument {0} found. Attempting to add OrderBookID {1}",
                    orderInstrument.Instrument, orderBook.BookID);
                orderInstrument.TryAddBook(orderBook);
            }
        }// ProcessCreateBook()
        //
        //
        // *************************************************************
        // ****                 ProcessSubmitOrder()                ****
        // *************************************************************
        //
        /// <summary>
        /// Private method to process the submission of an order to TT
        /// </summary>
        /// <param name="order"></param>
        /// <param name="instrKey"></param>
        private void ProcessSubmitOrder(UVOrder order, InstrumentKey instrKey)
        {
            bool isSuccess = true;
            // Get Order Feed.
            OrderFeed orderFeed = null;
            if (isSuccess && !m_DefaultOrderFeeds.TryGetValue(instrKey, out orderFeed) || !orderFeed.IsTradingEnabled)
            {
                Log.NewEntry(LogLevel.Warning, "{0}: SendOrder failed.  No enabled order feed.", m_Name);
                isSuccess = false;
            }
            // Get the Instrument
            Instrument instrument = null;
            if (isSuccess && !m_TTInstrKeyToTTInstr.TryGetValue(instrKey, out instrument))
            {
                Log.NewEntry(LogLevel.Warning, "{0}: SendOrder failed.  Not instrument available for {1} {2}.", m_Name, instrKey.ProductKey, instrKey.SeriesKey);
                isSuccess = false;
            }
            if (!isSuccess)
            { // if any of this failed!
                ProcessDeleteOrder(order);
                return;
            }
            //
            // Send order now
            //
            OrderProfile profile = new OrderProfile(orderFeed, instrument);

            profile.AccountType = AccountType.None;
            profile.AccountName = "Acct123";// order.AccountName;

            profile.OrderType = TTConvertNew.ToOrderType(order.OrderType);
            profile.BuySell = TTConvertNew.ToBuySell(order.OriginalQtyPending);
            profile.QuantityToWork = Quantity.FromInt(instrument, Math.Abs(order.OriginalQtyPending));
            profile.LimitPrice = Price.FromDouble(instrument, order.PricePending);
            profile.TimeInForce = new TimeInForce(TTConvertNew.ToTimeInForce(order.OrderTIF));
            string siteOrderKey = profile.SiteOrderKey;

            if (instrument.Session.SendOrder(profile))
            {   // Success
                m_MapTT2UV[siteOrderKey] = order.Id;
                m_MapUV2TT[order.Id] = siteOrderKey;
                Log.NewEntry(LogLevel.Warning, "{0}: Order sent {1}", m_Name, order);
                order.ChangesPending = true;        // flag order as waiting for confirmation
            }
            else
            {
                Log.NewEntry(LogLevel.Warning, "{0}: Order send failed {1} : {2}", m_Name, order, profile.RoutingStatus.Message);
                return;
            }
        }//ProcessSendOrder()
        //
        //
        // *************************************************************
        // ****                 ProcessDeleteOrder()                ****
        // *************************************************************
        /// <summary>
        /// If we haven't recieved the order back from TT, the deletion will
        /// be pended until we recieve the order.
        /// </summary>
        /// <param name="job"></param>
        private void ProcessDeleteOrder(UVOrder order)
        {
            string ttSiteOrderKey;
            if (m_MapUV2TT.TryGetValue(order.Id, out ttSiteOrderKey))
            { // we found a matching tt site key
                Order ttOrder;
                if (m_TTOrders.TryGetValue(ttSiteOrderKey, out ttOrder))
                { // we found the order from that site key
                    OrderProfileBase profile = ttOrder.GetOrderProfile();
                    profile.Action = OrderAction.Delete;
                    if (profile.Session.SendOrder(profile))
                    {
                        order.OrderStatePending = UVOrderState.Dead;                   // set out pending flag to dead while we await the confirm
                        Log.NewEntry(LogLevel.Minor, "Cancelling TT Order : {0}", ttOrder.SiteOrderKey);
                    }
                    else
                    {
                        Log.NewEntry(LogLevel.Minor, "Cancelling TT Order : {0} - FAILED", ttOrder.SiteOrderKey);
                        // TODO: Add to pending deleted and call timer to resubmit? how often and how many times?
                    }
                }
                else
                { // we tried to submit this order, but haven't yet heard back from TT...
                    Log.NewEntry(LogLevel.Error, "Could not find correspsonding TT Order for UV Order {0} with TT Site Key {1} storing for later processing", order, ttSiteOrderKey);
                    m_TTOrderKeysPendingDelete.Add(ttSiteOrderKey);
                    order.OrderStatePending = UVOrderState.Dead;
                }
            }
            else
            { // this mean somehow this order never got correctly submitted to TT.  Since this order is only internal we can deal with it now.
                m_ExecContainer.m_OrderInstruments[order.Instrument].TryDeleteOrder(order.Id);
            }
        }
        //
        //
        // *************************************************************
        // ****             ProcessChangeOrderPrice()               ****
        // *************************************************************
        private void ProcessChangeOrderPrice(UVOrder order)
        {
            if (order.ChangesPending)
            { // this order has outstanding changes, it cannot be changed until we hear back from tt.
                if (!m_OrderIdsPendingModification.Contains(order.Id))
                    m_OrderIdsPendingModification.Add(order.Id);
                return;
            }
            string ttSiteOrderKey;
            if (m_MapUV2TT.TryGetValue(order.Id, out ttSiteOrderKey))
            { // we found a matching tt site key
                Order ttOrder;
                if (m_TTOrders.TryGetValue(ttSiteOrderKey, out ttOrder))
                { // we found the order from that site key
                    OrderProfileBase profile = ttOrder.GetOrderProfile();
                    profile.Action = OrderAction.Change;
                    if (order.IPriceConfirmed != order.IPricePending)
                    {
                        profile.LimitPrice = Price.FromDouble(ttOrder.InstrumentDetails, order.PricePending);
                        if (profile.Session.SendOrder(profile))
                        {
                            Log.NewEntry(LogLevel.Minor, "TT Order {0} price was modified succesffully UV Order : {1}", ttSiteOrderKey, order);
                            order.ChangesPending = true;        // flag order as waiting for confirmation
                        }
                        else
                        {
                            Log.NewEntry(LogLevel.Error, "TT Order {0} failed to allow price modification {1} {2} : Adding to Pending Changes Queue.", ttSiteOrderKey, profile.RoutingStatus.Message, profile.RoutingStatus.State);
                            if (!m_OrderIdsPendingModification.Contains(order.Id))
                                m_OrderIdsPendingModification.Add(order.Id);
                        }
                    }
                }
            }
        }
        //
        //
        // *************************************************************
        // ****             ProcessChangeOrderQty()                 ****
        // *************************************************************
        private void ProcessChangeOrderQty(UVOrder order)
        {
            if (order.ChangesPending)
            { // this order has outstanding changes, it cannot be changed until we hear back from tt.
                if (!m_OrderIdsPendingModification.Contains(order.Id))
                    m_OrderIdsPendingModification.Add(order.Id);
                return;
            }
            string ttSiteOrderKey;
            if (m_MapUV2TT.TryGetValue(order.Id, out ttSiteOrderKey))
            { // we found a matching tt site key
                Order ttOrder;
                if (m_TTOrders.TryGetValue(ttSiteOrderKey, out ttOrder))
                { // we found the order from that site key
                    OrderProfileBase profile = ttOrder.GetOrderProfile();
                    profile.Action = OrderAction.Change;
                    if (order.OriginalQtyConfirmed != order.OriginalQtyPending)
                    {
                        profile.QuantityToWork = Quantity.FromInt(profile, Math.Abs(order.OriginalQtyPending));
                        if (profile.Session.SendOrder(profile))
                        {
                            Log.NewEntry(LogLevel.Minor, "TT Order {0} Qty was modified succesffully UV Order : {1}", ttSiteOrderKey, order);
                            order.ChangesPending = true;        // flag order as waiting for confirmation
                        }
                        else
                        {
                            Log.NewEntry(LogLevel.Error, "TT Order {0} failed to allow Qty modification {1} {2} : Adding to Pending Changes Queue.", ttSiteOrderKey, profile.RoutingStatus.Message, profile.RoutingStatus.State);
                            if (!m_OrderIdsPendingModification.Contains(order.Id))
                                m_OrderIdsPendingModification.Add(order.Id);
                        }
                    }
                }
            }
        }
        //
        //
        // ************************************************
        // ****           DeletePendingOrder()         ****
        // ************************************************
        /// <summary>
        /// We had an order that was pending deletion, but we hadn't 
        /// heard back from TT about the order yet.  Now we have so 
        /// we can go ahead and delete the order.
        /// </summary>
        /// <param name="ttOrder"></param>
        private void DeletePendingOrder(Order ttOrder)
        {
            OrderProfileBase profile = ttOrder.GetOrderProfile();
            profile.Action = OrderAction.Delete;
            if (profile.Session.SendOrder(profile))
            {
                Log.NewEntry(LogLevel.Minor, "Cancelling TT Order : {0}", ttOrder.SiteOrderKey);
            }
            else
            {
                Log.NewEntry(LogLevel.Minor, "Cancelling TT Order : {0} - FAILED", ttOrder.SiteOrderKey);
                // TODO: Add to pending deleted and call timer to resubmit? how often and how many times?
            }
        }
        #endregion // end order handling private

        #region IExectuionListener Implementation
        // *****************************************************************
        // ****           IExectuionListener Implementation             ****
        // *****************************************************************
        //
        // *****************************************
        // ****       CreateOrderBook()         ****
        // *****************************************
        /// <summary>
        /// Caller would like to created an Order Book. This will handle all neccesarry subscriptions.
        /// The user can directly subscribe to events from ths book.
        /// </summary>
        /// <param name="instrumentName"></param>
        /// <returns></returns>
        public UVOrderBook CreateOrderBook(UVInstrName instrumentName)
        {
            UVOrderBook orderBook = new UVOrderBook(instrumentName);
            ProcessCreateBook(orderBook);
            OnOrderBookCreated(orderBook);          // trigger events for any subscriber
            return orderBook;
        }
        //
        //
        // *****************************************
        // ****       GetAllOrdersBooks()       ****
        // *****************************************
        /// <summary>
        /// Caller would like to add all books managed by all order instruments
        /// to the referenced list.
        /// </summary>
        /// <param name="orderBookList"></param>
        public void GetAllOrderBooks(ref List<UVOrderBook> orderBookList)
        {
            foreach (OrderInstrument orderInst in m_ExecContainer.m_OrderInstruments.Values)
            {
                orderInst.GetAllOrderBooks(ref orderBookList);
            }
        }
        //
        //
        // *****************************************
        // ****       ExecutionContainer()      ****
        // *****************************************
        /// <summary>
        /// Get and Set the ExecutionContainer for the listener.  If setting the instrument details
        /// dictionary contained in this objet will be assigned and shared by ExecutionContainer 
        /// </summary>
        public ExecutionContainer ExecutionContainer
        {
            get { return m_ExecContainer; }
            set
            {
                m_ExecContainer = value;
                m_ExecContainer.m_InstrDetails = m_UVInstrumentDetails;
            }
        }

        // *****************************************
        // ****         TryCreateOrder          ****
        // *****************************************
        /// <summary>
        /// The caller wants to create an order.  After creation, the order must be submitted 
        /// to the specific OrderBook to be managed using TrySubmiteOrder
        /// </summary>
        /// <param name="instrumentName"></param>
        /// <returns></returns>
        public virtual bool TryCreateOrder(UVInstrName instrumentName, int tradeSide, int iPrice, int qty, out UVOrder newOrder)
        {
            newOrder = null;
            if ((UV.Lib.Utilities.QTMath.MktSideToMktSign(tradeSide) * qty) < 0)
            {   // this means our signs are incorrect!
                Log.NewEntry(LogLevel.Error, "Attempt to Create Order For Instrument {0} Failed, Mismatched Sides and Qtys", instrumentName);
                return false;
            }
            OrderInstrument orderInstrument;
            if (m_ExecContainer.m_OrderInstruments.TryGetValue(instrumentName, out orderInstrument))
            {
                newOrder = new UVOrder();
                newOrder.Instrument = instrumentName;
                newOrder.Id = UVOrder.GetNextId();
                newOrder.Side = tradeSide;
                newOrder.OriginalQtyPending = qty;
                newOrder.IPricePending = iPrice;
                newOrder.TickSize = orderInstrument.Details.TickSize;
                newOrder.OrderType = UVOrderType.LimitOrder;
            }
            // Exit.
            return (newOrder != null);
        }//CreateOrderBook()
        //
        //
        // *****************************************
        // ****         Try Submit Order()      ****
        // *****************************************
        /// <summary>
        /// Called by user to submit an order to the market.
        /// </summary>
        /// <param name="orderBookID"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        public virtual bool TrySubmitOrder(int orderBookID, UVOrder order)
        {
            OrderInstrument orderInstrument;
            if (!m_ExecContainer.m_OrderInstruments.TryGetValue(order.Instrument, out orderInstrument))
                return false;
            if (orderInstrument.TryAddOrder(orderBookID, order))
            {
                InstrumentKey ttKey;
                if (m_InstrumentNameToTTKey.TryGetValue(order.Instrument, out ttKey))
                {
                    ProcessSubmitOrder(order, ttKey);
                    return true;
                }
            }
            return false;
        }
        //
        // *****************************************
        // ****         Try Delete Order()      ****
        // *****************************************
        /// <summary>
        /// called by user to delete an order.
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public bool TryDeleteOrder(UVOrder order)
        {
            if (order.OrderStatePending == UVOrderState.Dead)
            {
                Log.NewEntry(LogLevel.Warning, "TryDeleteOrder: {0} - Was already processed to be cancelled", order);
                return false;
            }
            OrderInstrument orderInstrument;
            if (m_ExecContainer.m_OrderInstruments.TryGetValue(order.Instrument, out orderInstrument))
            {
                if (orderInstrument.TryDeleteOrder(order.Id))
                {
                    ProcessDeleteOrder(order);
                    return true;
                }
            }
            else
            {
                Log.NewEntry(LogLevel.Error, "TryDeleteOrder: {0} - {1} Matching TT Order Instrument was not found", order, order.Instrument);

            }
            return false;
        }
        //
        // *****************************************
        // ****       TryChangeOrderPrice()     ****
        // *****************************************
        /// <summary>
        /// Called by user to request a price change for a given order. 
        /// </summary>
        /// <param name="orderToModify"></param>
        /// <param name="newIPrice"></param>
        /// <returns></returns>
        public virtual bool TryChangeOrderPrice(UVOrder order, int newIPrice)
        {
            OrderInstrument orderInstrument;
            if (m_ExecContainer.m_OrderInstruments.TryGetValue(order.Instrument, out orderInstrument))
            {
                if (orderInstrument.TryChangeOrderPrice(order, newIPrice))
                {
                    ProcessChangeOrderPrice(order);
                    return true;
                }
            }
            else
            {
                Log.NewEntry(LogLevel.Error, "TryChangeOrderPrice: {0} - {1} Matching TT Order Instrument was not found", order, order.Instrument);
            }
            return false;
        }
        //
        //
        // *****************************************
        // ****       TryChangeOrderQty()       ****
        // *****************************************
        /// <summary>
        /// Called by a strategy or user to change the pending qty of a given order.
        /// </summary>
        /// <param name="order"></param>
        /// <param name="newQty">SIGNED QTY</param>
        /// <returns>false if sign is incorrect</returns>
        public virtual bool TryChangeOrderQty(UVOrder order, int newQty)
        {
            if ((UV.Lib.Utilities.QTMath.MktSideToMktSign(order.Side) * newQty) < 0)
            {   //this means our signs are incorrect!
                Log.NewEntry(LogLevel.Error, "Attempt to change Order {0} qty to the opposite sign, rejecting attempted change", order);
                return false;
            }
            order.OriginalQtyPending = newQty;
            ProcessChangeOrderQty(order);
            return true;
        }
        //
        //
        #endregion

        #region Events
        // *****************************************************************
        // ****                         Events                          ****
        // *****************************************************************
        // *************************************************************
        // ****                 Instrument Found                   ****
        // *************************************************************
        /// <summary>
        /// Event for subscribers who are looking for instruments.  When this is fired,
        /// the instrument details have been discovered as well as a market object created.
        /// </summary>
        public event EventHandler InstrumentFound;
        //
        private void OnInstrumentsFound(UVInstrDetails instrDetails)
        {
            if (this.InstrumentFound != null)
            {
                UV.Lib.Products.InstrumentsFoundEventArgs e = new UV.Lib.Products.InstrumentsFoundEventArgs();
                e.InstrumentDetails = instrDetails;
                this.InstrumentFound(this, e);
            }
        }
        //
        //
        //
        // *************************************************************
        // ****                 OrderBookCreated                    ****
        // *************************************************************
        /// <summary>
        /// Event for subscribers who would like to know each and every time an order book is created.
        /// Usually useful fo risk managers whou would like to be able to be handed an order book 
        /// so they can subscribe to appropiate events.
        /// </summary>
        public event EventHandler OrderBookCreated;
        //
        private void OnOrderBookCreated(UVOrderBook orderBook)
        {
            if (this.OrderBookCreated != null)
            {
                this.OrderBookCreated(orderBook, EventArgs.Empty);
            }
        }
        //
        //
        // *************************************************************
        // ****                     Initialized                     ****
        // *************************************************************
        /// <summary>
        /// Called to allow SingleLegExecutor to finish set up prior to calling Dispatch.Run
        /// </summary>
        public event EventHandler Initialized;
        //
        private void OnInitialized()
        {
            if (this.Initialized != null)
            {
                this.Initialized(this, EventArgs.Empty);
            }
        }
        //
        //
        // *************************************************************
        // ****                     Stopping                        ****
        // *************************************************************
        /// <summary>
        /// Called to allow SingleLegExecutor to finish and clean up prior to thread teardown
        /// </summary>
        public event EventHandler Stopping;
        //
        private void OnStopping()
        {
            if (this.Stopping != null)
            {
                this.Stopping(this, EventArgs.Empty);
            }
        }
        #endregion // my events

        #region Event Handlers
        // *****************************************************************
        // ****                    Events Handlers                      ****
        // *****************************************************************
        /// <summary>
        /// Subscribe to events for service state changes to the api. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void TTApiService_ServiceStateChanged(object sender, EventArgs eventArgs)
        {
            if (m_IsWaitingForApiConnection && m_TTService.session != null)
            { // we are waiting to initialize until we have a connection.
                m_IsWaitingForApiConnection = false;     // we are no longer waiting
                m_Dispatcher.BeginInvoke(OnInitialized); // call our thread to fire event
                m_Dispatcher.Run();                      // allow our dispatcher to run
            }
        }
        #endregion // event handlers

        #region Event Processing
        // *****************************************************************
        // ****                    Events Processing                    ****
        // *****************************************************************
        //
        /// <summary>
        /// Threadsafe call to request the execution thread to process an event.
        /// </summary>
        /// <param name="eventArg"></param>
        public void ProcessEvent(EventArgs eventArg)
        {
            m_InQueue.Enqueue(eventArg);
            m_Dispatcher.BeginInvoke(ProcessEvent);
            m_Dispatcher.Run();
        }
        //
        //
        /// <summary>
        /// Called by the executionListener thread to process an event on the queue.
        /// </summary>
        private void ProcessEvent()
        {
            if (m_isDisposing)
                return;
            EventArgs e;
            while (m_InQueue.TryDequeue(out e))   //remove from threadsafe queue
                m_WorkQueue.Enqueue(e);           // place on my current work stack
            //
            // Process all events now
            //
            while (m_WorkQueue.Count > 0)
            {
                e = m_WorkQueue.Dequeue();
                if (e is EngineEventArgs)
                { 
                    EngineEventArgs engEvent = (EngineEventArgs)e;
                    if (engEvent.EngineID >= 0)
                    {
                        m_ExecContainer.EngineList[engEvent.EngineID].ProcessEvent(engEvent);
                        if (engEvent.Status == EngineEventArgs.EventStatus.Confirm || engEvent.Status == EngineEventArgs.EventStatus.Failed)
                            m_EngineHub.OnEngineChanged(engEvent);
                    }
                    if(engEvent.MsgType == EngineEventArgs.EventType.SyntheticOrder)
                        m_ExecContainer.IOrderEngine.ProcessEvent(e);
                } 
                // this is meant to be expanded once we have more event types to deal with 
            }
        }
        #endregion // Event Processing

        #region ITimer Implementation
        // *****************************************************
        // ****             TimerSubscriberUpdate()         ****
        // *****************************************************
        /// <summary>
        /// Called by the hub thread to update us since we are a iTimerSubscriber.
        /// We will push it onto our thread and then call our own subscribers with the correct thread.
        /// </summary>
        public void TimerSubscriberUpdate()
        {
            if (m_Dispatcher.IsDisposed) return;
            m_Dispatcher.Invoke(CallITimerSubscribers);
            m_Dispatcher.Run();
        }
        //
        public void CallITimerSubscribers()
        {
            int i = 0;
            while (i < m_ITimerSubscribers.Count)
            {
                try
                {
                    m_ITimerSubscribers[i].TimerSubscriberUpdate();
                }
                catch (Exception e)
                {
                    Log.NewEntry(LogLevel.Error, "CallITimerSubscribers: Failed to update TimeSubscibers: {0}", e.Message);
                }
                i++;
            }
        }
        #endregion // ITimerImplementation
    }//end class
}
