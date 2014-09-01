using System;
using System.Collections.Generic;
using System.Text;

namespace Ambre.TTServices.Markets
{
    using Misty.Lib.IO.Xml;
    using Misty.Lib.Hubs;
    using Misty.Lib.MarketHubs;

    using InstrumentName = Misty.Lib.Products.InstrumentName;
    using MistyProds = Misty.Lib.Products;

    using TradingTechnologies.TTAPI;

    /// <summary>
    /// Created: 04 Jan 2013
    /// Version: 0.10 Market Hub for TTAPI price feeds.
    /// Notes: 
    ///     1. Instruments (which contain the market) are threadbound in TTAPI (to a single thread that must have a Dispatcher).
    ///     To accommodate this, we use a Listener object to hold the instruments for this hub.  
    ///     However, it is safe to pass keys (InstrumentKey and ProductKey) across threads, and InstrumentDetails.
    /// /// </summary>
    public class MarketTTAPI : MarketHub, IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // Services
        private TTApiService m_TTService = null;                                // Holds the TT API
        private PriceListener m_PriceListener = null;                           // my workerthread helper.


        // TT Product information
        private Dictionary<Market,ProductCatalogSubscription> m_ProductCatalogSubscriptionList = new Dictionary<Market,ProductCatalogSubscription>();
        private Dictionary<ProductKey, Product> m_Products = new Dictionary<ProductKey, Product>();

        // Product information Locker
        private object m_ProductMapLock = new object();
        private Dictionary<MistyProds.Product, ProductKey> m_ProductMap = new Dictionary<MistyProds.Product, ProductKey>();
        private Dictionary<ProductKey, MistyProds.Product> m_ProductMapKey = new Dictionary<ProductKey, MistyProds.Product>();

        // Instrument information Locker
        private object m_InstrumentLock = new object();
        private Dictionary<MistyProds.InstrumentName, InstrumentDetails> m_InstrumentDetails = new Dictionary<MistyProds.InstrumentName, InstrumentDetails>();
        

        //
        //
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public MarketTTAPI()
            : base("MarketTTAPI", Misty.Lib.Application.AppInfo.GetInstance().LogPath, false)
        {
            Log.AllowedMessages = LogLevel.ShowAllMessages;         // default is to show all messages.            
        }//constructor
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


        #region Public Request / Lookup Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //               
        //
        //
        // *****************************************************
        // ****             Request Instruments()           ****
        // *****************************************************
        /// <summary>
        /// Extends the MarketHub base class requests by allowing TT Users to 
        /// request market information using only the TT InstrumentKey.
        /// </summary>
        /// <param name="key">Requests instrument details for TT InstrumentKey </param>
        /// <returns></returns>
        public bool RequestInstruments(InstrumentKey key)
        {
            MarketHubRequest request = GetRequest(MarketHubRequest.RequestType.RequestInstruments);
            request.Data.Add(key);
            return this.HubEventEnqueue(request);
        }
        //
        //
        // *****************************************************************
        // ****              Try Lookup Instrument()                    ****
        // *****************************************************************
        public bool TryLookupInstrument(InstrumentKey ttInstrumentKey, out InstrumentName instrument)
        {
            bool isFound = false;
            instrument = new MistyProds.InstrumentName();
            lock (m_InstrumentLock)
            {
                foreach (InstrumentName instr in m_InstrumentDetails.Keys)
                {
                    if (m_InstrumentDetails[instr].Key.Equals(ttInstrumentKey))
                    {
                        instrument = instr;
                        isFound = true;
                        break;
                    }
                }
            }//lock
            return isFound;
        }//
        public bool TryLookupInstrumentDetails(MistyProds.InstrumentName instrName, out InstrumentDetails details)
        {
            details = null;
            lock (m_InstrumentLock)
            {
                if (!m_InstrumentDetails.TryGetValue(instrName, out details))
                   details = null;
            }//lock
            return (details != null);
        }//TryGetInstrument()
        //
        // *****************************************************************
        // ****                 Try Get Product()                       ****
        // *****************************************************************
        /// <summary>
        /// This is a thread-safe way to query the Market whether it knows the Misty Product
        /// associated with a particular TT ProductKey.
        /// </summary>
        public bool TryLookupProduct(ProductKey ttProductKey, out Misty.Lib.Products.Product product)
        {
            bool isGood = false;
            lock (m_ProductMapLock)
            {
                if (m_ProductMapKey.TryGetValue(ttProductKey, out product))
                    isGood = true;
            }//lock
            return isGood;
        }//TryGetProduct()
        //
        // *****************************************************************
        // ****                     Start()                             ****
        // *****************************************************************
        /// <summary>
        /// This is called after all services exist, but before any services is connected.
        /// </summary>
        public override void Start()
        {
            m_TTService = TTApiService.GetInstance();
            m_TTService.ServiceStateChanged += new EventHandler(this.HubEventEnqueue); // Once TTAPI is connected, I can connect automatically.
            base.Start();
        }//Start().
        //
        #endregion//Public Methods


        #region Hub Processing Methods
        // *****************************************************************
        // ****             Hub Processing Methods                      ****
        // *****************************************************************
        private List<EventArgs> m_WorkList = new List<EventArgs>();                     // private workspace for HubEventHandler()
        //
        /// <summary>
        /// This is the master Hub responding method. From here we call specific processing methods, 
        /// one for each type of event that we can process; some from user, some from exchange API.
        /// This is called by the hub thread.
        /// </summary>
        protected override void HubEventHandler(EventArgs[] eventArgList)
        {
            m_WorkList.Clear();     
            foreach (EventArgs eventArg in eventArgList)
            {
                if (eventArg == null) continue;
                try
                {

                    Type eventType = eventArg.GetType();
                    if (eventType == typeof(PriceListener.InstrumentsFoundEventArgs))
                        ProcessInstrumentsFound((PriceListener.InstrumentsFoundEventArgs)eventArg);     // PriceListener catalog of instruments updated
                    else if (eventType == typeof(Misty.Lib.BookHubs.MarketUpdateEventArgs))
                        m_WorkList.Add(eventArg);
                    else if (eventType == typeof(Misty.Lib.BookHubs.MarketStatusEventArgs))
                        m_WorkList.Add(eventArg);
                    else if (eventType == typeof(MarketCatalogUpdatedEventArgs))
                        ProcessMarketCatalogUpdate((MarketCatalogUpdatedEventArgs)eventArg);            // TT catalog of markets updated
                    else if (eventType == typeof(ProductCatalogUpdatedEventArgs))
                        ProcessProductCatalogUpdate((ProductCatalogUpdatedEventArgs)eventArg);          // TT catalog of products updated
                    else if (eventType == typeof(MarketHubRequest))
                    {
                        MarketHubRequest request = (MarketHubRequest)eventArg;                          // User requests
                        switch (request.Request)
                        {
                            case MarketHubRequest.RequestType.RequestServers:
                                // this is done automatically at start.
                                break;
                            case MarketHubRequest.RequestType.RequestProducts:
                                ProcessHubRequestProducts(request);
                                break;
                            case MarketHubRequest.RequestType.RequestInstruments:
                                ProcessHubRequestInstruments(request);
                                break;
                            case MarketHubRequest.RequestType.RequestInstrumentSubscription:
                                ProcessHubRequestInstrumentPrice(request);
                                break;
                            case MarketHubRequest.RequestType.RequestShutdown:
                                ProcessHubRequestShutdown(request);
                                break;
                            default:
                                Log.NewEntry(LogLevel.Warning, "HubEventHandler: Request not recognized. {0}", request.ToString());
                                break;
                        }//switch user request type
                    }
                    else if (eventType == typeof(FeedStatusChangedEventArgs))
                        ProcessFeedStatusChange((FeedStatusChangedEventArgs)eventArg);                      // TT feed status changed
                    else if (eventType == typeof(TTServices.TTApiService.ServiceStatusChangeEventArgs))
                        ProcessTTApiServiceChange((TTServices.TTApiService.ServiceStatusChangeEventArgs)eventArg);
                    else
                        Log.NewEntry(LogLevel.Warning, "HubEventHandler: Unknown event received: {0} ", eventArg.GetType().Name);
                }
                catch (Exception ex)
                {
                    Misty.Lib.Application.ExceptionCatcher.QueryUserTakeAction(ex, this, eventArg);
                }
            }//next eventArg in list   
            if (m_WorkList.Count > 0)
                base.ProcessBookEvents(m_WorkList);

        }//HubEventHandler()
        // 
        //        
        //
        //
        // *************************************************************************
        // ****                 Process Market Hub Requests()                   ****
        // *************************************************************************
        /// <summary>
        /// This processes requests from the user.
        /// This is called by the hub thread.
        /// </summary>
        private void ProcessHubRequestProducts(Misty.Lib.MarketHubs.MarketHubRequest request)
        {
            Market market = null;
            IDictionary<MarketKey, Market> marketList = null;
            List<string> marketNameList = new List<string>();       // Collect all markets we are interested in.
            foreach (object o in request.Data)
            {
                string marketName = null;
                if (o is Misty.Lib.Products.Product)
                    marketName = ((Misty.Lib.Products.Product)o).ServerName; // user gave us product objects.
                else if (o is string)
                    marketName = (string)o;                        // user gave us strings (which must be server names)
                if (!string.IsNullOrEmpty(marketName) && !marketNameList.Contains(marketName))
                    marketNameList.Add(marketName);
            }
            marketList = m_TTService.session.MarketCatalog.Markets;             // markets we know
            foreach (string marketName in marketNameList)                       // search for match of name to given by user.
            {
                market = null;
                foreach (Market mkt in marketList.Values)                       // search for mkt with correct name.
                {
                    if (mkt.Name.Equals(marketName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        market = mkt;
                        break;
                    }
                }
                // 
                if (market != null)
                {   // We found a recognized market.
                    ProductCatalogSubscription productCatalog = null;
                    if (m_ProductCatalogSubscriptionList.TryGetValue(market, out productCatalog))
                    {   // We have already subscribed to these products, so fire our current list of known products.
                        // As more arrive we will fire another event.
                        List<Misty.Lib.Products.Product> productList = new List<Misty.Lib.Products.Product>();
                        lock (m_ProductMapLock)
                        {
                            foreach (Misty.Lib.Products.Product prod in m_ProductMap.Keys)      // search thru product list created during TT callbacks.
                                if (prod.ServerName.Equals(market.Name))
                                    productList.Add(prod);
                        }
                        OnMarketFoundResource(productList);                              // trigger event for subscribers
                    }
                    else
                    {   // Create new product catalog subscription for this market.
                        productCatalog = market.CreateProductCatalogSubscription(m_TTService.m_Dispatcher);
                        productCatalog.ProductsUpdated += new EventHandler<ProductCatalogUpdatedEventArgs>(ProductCatalog_Updated);
                        productCatalog.Start();
                        m_ProductCatalogSubscriptionList.Add(market, productCatalog);
                        Log.NewEntry(LogLevel.Minor, "ProcessMarketHubRequests: Created new ProductCatalog subscription service for {0}.  Have {1} in total.", market.Name, m_ProductCatalogSubscriptionList.Count);
                    }
                }
                else
                    Log.NewEntry(LogLevel.Error, "ProcessMarketHubRequests: Failed to find market {0}.", marketName);
            }
        }// ProcessHubRequestProducts().
        //
        //
        // *************************************************************************
        // ****                 Process Hub Request Instruments()               ****
        // *************************************************************************
        /// <summary>
        /// Process users request for a list of Misty Instruments associated with a collection
        /// of user-provided Misty Products.
        /// </summary>
        private void ProcessHubRequestInstruments(Misty.Lib.MarketHubs.MarketHubRequest request)
        {
            if (request.Data == null || request.Data.Count < 1) { return; }
            foreach (object o in request.Data)
            {
                Type dataType = o.GetType();
                if (dataType == typeof(Misty.Lib.Products.Product))
                {   // User has provided a Product object.
                    Misty.Lib.Products.Product mistyProduct = (Misty.Lib.Products.Product)o;
                    Product ttProduct = null;
                    lock (m_ProductMapLock)
                    {
                        ProductKey productKey;
                        if (m_ProductMap.TryGetValue(mistyProduct, out productKey))
                            ttProduct = m_Products[productKey];
                    }
                    if (ttProduct != null)
                        m_PriceListener.SubscribeTo(ttProduct);                     // we have this product in our lists.
                    else
                    {   // User has given us a one-off specific product.
                        Log.NewEntry(LogLevel.Warning, "ProcessHubRequestInstruments: Failed to find Product {0}.", mistyProduct.ToString());
                    }
                }
                else if (dataType == typeof(InstrumentName))
                {
                    InstrumentName instrumentName = (InstrumentName)o;
                    InstrumentDetails details;
                    lock (m_InstrumentLock)
                    {
                        if (!m_InstrumentDetails.TryGetValue(instrumentName, out details))
                        {   // Could not find desired instrument in our list of details. Try to request it.
                            
                        }
                    }
                }
                else if (dataType == typeof(InstrumentKey))
                {
                    InstrumentKey instrKey = (InstrumentKey)o;
                    lock (m_InstrumentLock)
                    {
                        bool isFound = false;
                        foreach (InstrumentDetails detail in m_InstrumentDetails.Values)
                        {
                            if (detail.Key == instrKey)
                            {
                                isFound = true;
                                break;
                            }
                        }
                        if (! isFound)
                        {
                            m_PriceListener.SubscribeTo(instrKey);
                        }
                    }                    
                }
                else
                    Log.NewEntry(LogLevel.Warning, "ProcessHubRequestInstruments: Failed to recognize type of Data {0} in {1}.", dataType.Name, request.ToString());
            }//next data
        }//ProcessHubRequestInstruments()
        //
        //
        //
        // *************************************************************************
        // ****                 Process Hub Request Instruments()               ****
        // *************************************************************************
        private void ProcessHubRequestInstrumentPrice(Misty.Lib.MarketHubs.MarketHubRequest request)
        {
            if (request.Data == null || request.Data.Count < 1) { return; }
            foreach (object dataObj in request.Data)
            {
                Type dataType = dataObj.GetType();
                if (dataType == typeof(InstrumentName))
                {
                    InstrumentName instr = (InstrumentName)dataObj;
                    InstrumentDetails details;
                    lock (m_InstrumentLock)
                    {
                        if (m_InstrumentDetails.TryGetValue(instr, out details))
                        {   // found desired instrument.
                            if (m_InstrumentMarkets.ContainsKey(instr))
                            {   // We have already subscribed to this instrument.
                                Log.NewEntry(LogLevel.Minor, "ProcessHubRequestInstrumentPrice: We already have a subscription to {0}. Ignore request {1}.", instr.SeriesName, request.ToString());
                            }
                            else
                            {   // We do not have a subscription to this instrument.
                                TryCreateNewBook(instr);    // m_InstrumentDetails[instr].Key.MarketKey.Name);
                                m_PriceListener.SubscribeTo(details.Key, new PriceSubscriptionSettings(PriceSubscriptionType.InsideMarket));                                
                            }
                        }
                        else
                        {   // The user has not previously request info about this Product family.
                            //Log.NewEntry(LogLevel.Warning, "ProcessHubRequestInstrumentPrice: Failed to locate TT version of {0} for request {1}.", instr.SeriesName, request.ToString());
                        }
                    }
                }
                else if (dataType == typeof(InstrumentKey))
                {
                    InstrumentKey instrKey = (InstrumentKey) dataObj;                   
                    lock (m_InstrumentLock)
                    {
                        bool isFoundInstrument = false;
                        InstrumentName instrName = new MistyProds.InstrumentName();
                        foreach (InstrumentName i in m_InstrumentDetails.Keys)
                        {
                            if (m_InstrumentDetails[i].Key.Equals(instrKey))
                            {
                                instrName = i;
                                isFoundInstrument = true;
                                break;
                            }
                        }
                        if (! isFoundInstrument )
                        {   // Unknown instrument.
                            Log.NewEntry(LogLevel.Warning, "ProcessHubRequestInstrumentPrice: Price subscription request with unknown instrKey = {0}", instrKey.ToString());
                        }
                        else
                        {   // We actually know this instrument.
                            if (!m_InstrumentMarkets.ContainsKey(instrName))
                            {
                                TryCreateNewBook(instrName);     //, m_InstrumentDetails[instrName].Key.MarketKey.Name);
                                m_PriceListener.SubscribeTo(instrKey, new PriceSubscriptionSettings(PriceSubscriptionType.InsideMarket));                                
                            }
                        }
                    }
                }
                else
                    Log.NewEntry(LogLevel.Warning, "ProcessHubRequestInstrumentPrice: Failed to recognize data object {0} for request {1}.",dataType.Name, request.ToString());
            }//next data
        }// ProcessHubRequestInstrumentPrice()
        //
        //
        //
        //
        // *****************************************************************
        // ****             ProcessMarketCatalogUpdate()                ****
        // *****************************************************************
        /// <summary>
        /// Called by hub thread to process changes because TT told me the Catalog updated.
        /// </summary>
        /// <param name="eventArg"></param>
        private void ProcessMarketCatalogUpdate(MarketCatalogUpdatedEventArgs eventArg)
        {
            if (eventArg.Error == null)
            {
                List<string> mktNames = new List<string>();
                foreach (Market mkt in m_TTService.session.MarketCatalog.Markets.Values)
                    mktNames.Add(mkt.Name);
                OnMarketStatusChanged(mktNames);
            }
            else
                Log.NewEntry(LogLevel.Error, "ProcessMarketCatalogUpdate: MarketCatalogUpdated error = {0}.");
        }//ProcessMarketCatalogUpdate()
        //
        // *****************************************************************
        // ****             ProcessProductCatalogUpdate()               ****
        // *****************************************************************
        /// <summary>
        /// TT sent us a ProductCatalogUpdate event. Update our list of internal products.
        /// Called by hub thread.
        /// </summary>
        /// <param name="eventArg"></param>
        private void ProcessProductCatalogUpdate(ProductCatalogUpdatedEventArgs eventArg)
        {
            List<Misty.Lib.Products.Product> productList = new List<MistyProds.Product>();
            foreach (Product ttProduct in eventArg.Added)                    // loop thru each ttProduct found in event.
            {
                Misty.Lib.Products.Product ourProduct;
                if (TTConvert.TryConvert(ttProduct, out ourProduct))        // Create one of our product objects for each.                
                {   // Success!
                    productList.Add(ourProduct);
                    Log.NewEntry(LogLevel.Minor, "ProcessProductCatalogUpdate: Found product {0} -> {1}. ", ttProduct.Name, ourProduct);
                    if (!m_Products.ContainsKey(ttProduct.Key))
                    {
                        m_Products.Add(ttProduct.Key, ttProduct);           // store the actual TT product.
                        lock (m_ProductMapLock)
                        {
                            m_ProductMap.Add(ourProduct, ttProduct.Key);   // my product -->  productKey map
                            m_ProductMapKey.Add(ttProduct.Key, ourProduct);   // product key --> my product map
                        }
                    }
                    else
                        Log.NewEntry(LogLevel.Warning, "ProcessProductCatalogUpdate: Received duplicate product! Received {0} <--> {1}", ttProduct.Name, ourProduct);
                }
                else
                {   // Since products include server name, there should never be two duplicates!
                    Log.NewEntry(LogLevel.Minor, "ProcessProductCatalogUpdate: Unknown product type {0}. ", ttProduct.Name);
                }                
            }
            // Fire the event to our subscribers.                        
            OnMarketFoundResource(productList);                      // TODO: Send all products, all for this server, or just the new ones?
        }//ProcessProductCatalogUpdate()
        //
        //
        //
        // *****************************************************************
        // ****             Process Instruments Found()                 ****
        // *****************************************************************
        private void ProcessInstrumentsFound(PriceListener.InstrumentsFoundEventArgs e)
        {
            List<MistyProds.InstrumentName> instrFound = new List<MistyProds.InstrumentName>();
            lock (m_InstrumentLock)
            {
                foreach (MistyProds.InstrumentName name in e.InstrumentDetails.Keys)
                    if (!m_InstrumentDetails.ContainsKey(name))
                    {
                        InstrumentDetails detail =  e.InstrumentDetails[name];
                        m_InstrumentDetails.Add(name,detail);
                        // TODO: Here, we may need to create our own Misty InstrumentInfo entry in lieu of using the TT InstrumentDetails.
                        instrFound.Add(name);
                        // TODO: ? Update product list if its a new product.
                    }//if new instrument.               
            }
            OnMarketFoundResource(instrFound);
        }// ProcessInstrumentsFound()   
        //
        // *****************************************************************
        // ****             Process Feed Status Change()                ****
        // *****************************************************************
        private void ProcessFeedStatusChange(FeedStatusChangedEventArgs eventArg)
        {
            Log.NewEntry(LogLevel.Minor, "ProcessFeedStatusChange: {0} {1} {2}",eventArg.Feed.Name,eventArg.Feed.Market.Name,eventArg.Feed.Status);            
            
        } // ProcessFeedStatusChange()
        //
        //
        // *****************************************************************
        // ****             Process Feed Status Change()                ****
        // *****************************************************************
        private void ProcessTTApiServiceChange(TTServices.TTApiService.ServiceStatusChangeEventArgs eventArg)
        {
            if (eventArg.IsConnected)
            {
                if (m_PriceListener == null)            // use the existance of a listner to recall if we've already connected to TTservice.
                {
                    Session session = m_TTService.session;
                    session.MarketCatalog.MarketsUpdated += new EventHandler<MarketCatalogUpdatedEventArgs>(MarketCatalog_Updated);
                    session.MarketCatalog.FeedStatusChanged += new EventHandler<FeedStatusChangedEventArgs>(MarketCatalog_FeedStatusChanged);

                    m_PriceListener = new PriceListener("PriceListener", base.Log);
                    m_PriceListener.InstrumentsFound += new EventHandler(this.HubEventEnqueue);
                    m_PriceListener.m_Market = this;
                    m_PriceListener.ProcessPriceChangeEvents = new PriceListener.ProcessPriceChangeDelegate(this.PriceListener_ProcessPriceChangeEvents);
                    m_PriceListener.Start();
                }
            }
        }//ProcessTTApiServiceChange()
        //
        //
        // *****************************************************************
        // ****             Process Hub Request Shutdown()              ****
        // *****************************************************************
        private void ProcessHubRequestShutdown(MarketHubRequest request)
        {
            // Dispose of TT subscription objects.
            foreach (ProductCatalogSubscription subscription in m_ProductCatalogSubscriptionList.Values)
                subscription.Dispose();
            m_ProductCatalogSubscriptionList.Clear(); 

            if (m_PriceListener != null)
                m_PriceListener.Dispose();
            base.Stop();
        }// ProcessHubRequestShutdown().
        //
        //
        #endregion//Processing


        #region PriceListener Code 
        // *****************************************************************                               
        // ****                 Price Listener Code                     ****
        // *****************************************************************                               
        /// <summary>
        /// This is called by the external (PriceListener) thread. Note that the eventList is "ref",
        /// because the external thread wants it back to reuse. So we must take the EventArgs and release its pointer.
        /// </summary>
        /// <param name="eventList"></param>
        public void PriceListener_ProcessPriceChangeEvents(ref List<EventArgs> eventList)          // this is how PriceListener enters.
        {
            HubEventEnqueue(eventList);                 // push each event onto the queue.
        }
        protected override List<int> ProcessBookEventsForABook(int bookID, List<EventArgs> eArgList)
        {
            List<int> updatedInstrList = new List<int>();
            foreach (EventArgs eArg in eArgList)
            {
                Type eArgType = eArg.GetType();
                /*
                if (eArgType == typeof(PriceListener.PriceUpdateEventArgs))
                {
                    PriceListener.PriceUpdateEventArgs e = (PriceListener.PriceUpdateEventArgs)eArg;
                    int Id;
                    if (m_InstrumentMarkets.TryGetValue(e.Instrument, out Id))
                    {
                        m_Book[bookID].Instruments[Id].SetMarket(0, 0, ((Price)e.BidPrice).ToDouble(), ((Quantity)e.BidQty).ToInt(), 0);
                        m_Book[bookID].Instruments[Id].SetMarket(1, 0, ((Price)e.AskPrice).ToDouble(), ((Quantity)e.AskQty).ToInt(), 0);
                        //m_Book[bookID].Instruments[Id].LastPrice = (e
                    }
                }
                */
                if (eArgType == typeof(Misty.Lib.BookHubs.MarketUpdateEventArgs))
                {
                    Misty.Lib.BookHubs.MarketUpdateEventArgs e = (Misty.Lib.BookHubs.MarketUpdateEventArgs)eArg;
                    int Id;
                    Misty.Lib.BookHubs.Market mktInstrument;
                    // TODO: Figure out how we can get here, with ID=0 and no Instrument yet in dictionary with that Id.
                    if (m_InstrumentMarkets.TryGetValue(e.Name, out Id) && m_Book[bookID].Instruments.TryGetValue(Id,out mktInstrument) )
                    {
                        mktInstrument.SetMarket(e.Side, 0, e.Price, e.Qty, 0);
                    }
                }
                else if ( eArgType == typeof(Misty.Lib.BookHubs.MarketStatusEventArgs) )
                {
                    Misty.Lib.BookHubs.MarketStatusEventArgs e = (Misty.Lib.BookHubs.MarketStatusEventArgs)eArg;
                    int Id;
                    Misty.Lib.BookHubs.Market mktInstrument;
                    if (m_InstrumentMarkets.TryGetValue(e.InstrumentName, out Id) && m_Book[bookID].Instruments.TryGetValue(Id, out mktInstrument))
                    {
                        if (e.Status == Misty.Lib.BookHubs.MarketStatus.Trading)
                            m_Book[bookID].Instruments[Id].IsMarketGood = true;
                        else
                            m_Book[bookID].Instruments[Id].IsMarketGood = false;
                    }
                }
                else
                    Log.NewEntry(LogLevel.Warning, string.Format("Error unexpected event of type {0} : {1}", eArgType.Name, eArg.ToString()));
            }
            return new List<int>();
        }
        //
        #endregion // price listener


        #region Event Handlers for TT 
        // *****************************************************************
        // ****                 Event Handlers for TT                   ****
        // *****************************************************************
        //
        /// <summary>
        /// Callbacks from TT API as markets are discoverd, or their feeds change.  
        /// The thread calling these is my TTService thread.
        /// </summary>
        private void MarketCatalog_Updated(object sender, MarketCatalogUpdatedEventArgs eventArg)
        {
            //Log.NewEntry(LogLevel.Minor, "MarketCatalog_Updated: Callback by thread {0}", System.Threading.Thread.CurrentThread.Name);
            this.HubEventEnqueue(eventArg);
        }
        //
        private void MarketCatalog_FeedStatusChanged(object sender, FeedStatusChangedEventArgs eventArg)
        {
            this.HubEventEnqueue(eventArg);           
        }
        //
        private void ProductCatalog_Updated(object sender, ProductCatalogUpdatedEventArgs eventArg)
        {
            if (eventArg.Error == null)
                HubEventEnqueue(eventArg);
            else
                Log.NewEntry(LogLevel.Major, "ProductCatalog error. {0}",eventArg.Error.Message);
        }
        //
        #endregion//Event Handlers





        #region IStringifiable
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
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {         
        }
        #endregion//IStringifiable


    }
}
