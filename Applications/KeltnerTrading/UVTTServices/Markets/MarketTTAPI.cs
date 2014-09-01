using System;
using System.Collections.Generic;
using System.Text;

namespace UV.TTServices.Markets
{
    using UV.Lib.IO.Xml;
    using UV.Lib.Hubs;
    using UVBookHubs = UV.Lib.BookHubs;
    using UV.Lib.MarketHubs;
    using UV.Lib.Utilities;
    using UV.TTServices;
    using InstrumentName = UV.Lib.Products.InstrumentName;
    using UVProds = UV.Lib.Products;
    using InstrumentChangeArgs = UV.Lib.BookHubs.InstrumentChangeArgs;

    using TradingTechnologies.TTAPI;

    /// <summary>
    /// Market Hub for TTAPI price feeds.
    /// Created: 04 Jan 2013, Version 0.10
    /// Version: 26 Dec 2013, Version 1.00
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
        public RecycleFactory<UVBookHubs.MarketBase> m_MarketBaseFactory = null;

        // TT Product information
        private Dictionary<Market, ProductCatalogSubscription> m_ProductCatalogSubscriptionList = new Dictionary<Market, ProductCatalogSubscription>();
        private Dictionary<ProductKey, Product> m_Products = new Dictionary<ProductKey, Product>();
        private Dictionary<ProductKey, ProductLookupSubscription> m_ProductLookupSubscriptionList = new Dictionary<ProductKey, ProductLookupSubscription>();

        // Product information Locker
        private object m_ProductMapLock = new object();
        private Dictionary<UVProds.Product, ProductKey> m_ProductMap = new Dictionary<UVProds.Product, ProductKey>();
        private Dictionary<ProductKey, UVProds.Product> m_ProductMapKey = new Dictionary<ProductKey, UVProds.Product>();

        // Instrument information Locker
        private object m_InstrumentLock = new object();
        private Dictionary<UVProds.InstrumentName, InstrumentDetails> m_InstrumentDetails = new Dictionary<UVProds.InstrumentName, InstrumentDetails>();
        private List<InstrumentName> m_TimeAndSalesInstrumentsRequested = new List<InstrumentName>();

        // Pending Requests
        private List<RequestEventArg<RequestCode>> m_PendingRequests = new List<RequestEventArg<RequestCode>>();
        //

        //user variables
        public bool m_IsTopOfBookOnly = false;
        
        //
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public MarketTTAPI()
            : base("MarketTTAPI", UV.Lib.Application.AppInfo.GetInstance().LogPath, false)
        {
            Log.AllowedMessages = LogLevel.ShowAllMessages;         // default is to show all messages.            
            base.m_WaitListenUpdatePeriod = 2000;

            // Create the book factory
            UVBookHubs.MarketBase.MaxDepth = 5;
            m_MarketBaseFactory = new RecycleFactory<UVBookHubs.MarketBase>(100);
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
        /// request market information using the TT InstrumentKey.
        /// </summary>
        /// <param name="key">Requests instrument details for TT InstrumentKey </param>
        /// <returns></returns>
        public bool RequestInstruments(InstrumentKey key)
        {
            RequestEventArg<RequestCode> request = m_Requests.Get(RequestCode.RequestInstruments);
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
            instrument = new UVProds.InstrumentName();
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
        public bool TryLookupInstrumentDetails(UVProds.InstrumentName instrName, out InstrumentDetails details)
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
        /// This is a thread-safe way to query the Market whether it knows the UV Product
        /// associated with a particular TT ProductKey.
        /// </summary>
        public bool TryLookupProduct(ProductKey ttProductKey, out UV.Lib.Products.Product product)
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
        //
        public List<UVProds.Product> GetProducts()
        {
            List<UVProds.Product> products = new List<UVProds.Product>();
            lock (m_ProductMapLock)
            {
                products.AddRange(m_ProductMap.Keys);
            }
            return products;
        }//GetProducts()
        //
        //
        //
        //
        // *****************************************************************
        // ****            CreateUVInstrumentDetails()                  ****
        // *****************************************************************
        /// <summary>
        /// Create UV Instrument Details from TT Instrument Details and try to add it to our Market Hub
        /// concurrent dictionary.
        /// </summary>
        /// <param name="instrName"></param>
        /// <param name="instrDetails"></param>
        private void CreateUVInstrumentDetails(UV.Lib.Products.InstrumentName instrName, InstrumentDetails instrDetails)
        { 
            UVProds.ProductTypes instrType;                                                 // finds the correct product type 
            if (instrDetails.Key.ProductKey.Type.Equals(ProductType.Future))
                instrType = UVProds.ProductTypes.Future;
            else if (instrDetails.Key.ProductKey.Type.Equals(ProductType.Spread))
                instrType = UVProds.ProductTypes.Spread;
            else if (instrDetails.Key.ProductKey.Type.Equals(ProductType.Option))
                instrType = UVProds.ProductTypes.Option;
            else if (instrDetails.Key.ProductKey.Type.Equals(ProductType.AutospreaderSpread))
                instrType = UVProds.ProductTypes.AutoSpreaderSpread;
            else if (instrDetails.Key.ProductKey.Type.Equals(ProductType.Bond))
                instrType = UVProds.ProductTypes.Bond;
            else if (instrDetails.Key.ProductKey.Type.Equals(ProductType.Stock))
                instrType = UVProds.ProductTypes.Equity;
            else if (instrDetails.Key.ProductKey.Type.Equals(ProductType.Strategy))
                instrType = UVProds.ProductTypes.Synthetic;
            else
                instrType = UVProds.ProductTypes.Unknown;

            UV.Lib.Products.InstrumentDetails uvInstrDetails;
            uvInstrDetails = new UVProds.InstrumentDetails(instrName,                                                                       // create the UV Instr details
                                                           instrDetails.Currency.Code,
                                                           TTConvertNew.ToUVTickSize(instrDetails),
                                                           TTConvertNew.ToUVExecTickSize(instrDetails),
                                                           TTConvertNew.ToUVMultiplier(instrDetails),
                                                           instrDetails.ExpirationDate.ToDateTime(),
                                                           instrType);
            
            base.TryAddInstrumentDetails(instrName, uvInstrDetails);                                                                        // try and add them to our look up dictionary

        }
        //
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
                    if ( IsMarketRunning(eventArg) )
                    {
                        //
                        // Process TTAPI call backs.
                        //
                        if (eventType == typeof(PriceListener.InstrumentsFoundEventArgs))
                            ProcessInstrumentsFound((PriceListener.InstrumentsFoundEventArgs)eventArg);     // PriceListener catalog of instruments updated
                        else if (eventType == typeof(UVBookHubs.MarketBase))
                            m_WorkList.Add(eventArg);
                        else if (eventType == typeof(UVBookHubs.MarketUpdateEventArgs))
                            m_WorkList.Add(eventArg);
                        else if (eventType == typeof(UVBookHubs.MarketStatusEventArgs))
                            m_WorkList.Add(eventArg);
                        else if (eventType == typeof(MarketCatalogUpdatedEventArgs))
                            ProcessMarketCatalogUpdate((MarketCatalogUpdatedEventArgs)eventArg);            // TT catalog of markets updated
                        else if (eventType == typeof(ProductCatalogUpdatedEventArgs))
                            ProcessProductCatalogUpdate((ProductCatalogUpdatedEventArgs)eventArg);          // TT catalog of products updated
                        else if (eventType == typeof(ProductLookupSubscriptionEventArgs))
                            ProcessProductLookUp((ProductLookupSubscriptionEventArgs)eventArg);             // TT Product Lookup 
                        else if (eventType == typeof(RequestEventArg<RequestCode>))
                        {
                            RequestEventArg<RequestCode> request = (RequestEventArg<RequestCode>)eventArg;  // User requests
                            switch (request.RequestType)
                            {
                                case RequestCode.RequestServers:
                                    // this is done automatically at start.
                                    break;
                                case RequestCode.RequestProducts:
                                    ProcessHubRequestProducts(request);
                                    break;
                                case RequestCode.RequestInstruments:
                                    ProcessHubRequestInstruments(request);
                                    break;
                                case RequestCode.RequestInstrumentPriceSubscription:
                                    ProcessHubRequestInstrumentPrice(request);
                                    break;
                                case RequestCode.RequestInstrumentTimeAndSalesSubscription:
                                    ProcessHubRequestInstrumentTimeAndSales(request);
                                    break;
                                case RequestCode.RequestShutdown:
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
                    }//if m_Servicestat
                }
                catch (Exception ex)
                {
                    UV.Lib.Application.ExceptionCatcher.QueryUserTakeAction(ex, this, eventArg);
                }
            }//next eventArg in list   
            if (m_WorkList.Count > 0)
                base.ProcessBookEvents(m_WorkList);

        }//HubEventHandler()
        // 
        //
        protected override void UpdatePeriodic()
        {
            ProcessAllPendingRequests();
        }
        //
        //
        //
        //
        // *********************************************************
        // *****                IsMarketRunning()               ****
        // *********************************************************
        /// <summary>
        /// This returns true if all initalization is complete.
        /// In future, this might all be moved to a DB Manager class
        /// that inherits from the dbReaderWriter itself!
        /// </summary>
        /// <param name="eventArg"></param>
        /// <returns></returns>
        private bool IsMarketRunning(EventArgs eventArg)
        {
            if ( m_ServiceState == UV.Lib.Application.ServiceStates.Running)
                return true;
            
            // Continue startup procedure.
            bool isSwitchToRunning = false;
            if ( m_ServiceState == UV.Lib.Application.ServiceStates.Unstarted)
            {   // Server is not yet marked as started. Mark it as started now (since thread is started).
            
                Type eventType = eventArg.GetType();
                OnServiceStateChanged(UV.Lib.Application.ServiceStates.Started);            // Tell subscribers we are starting.
                if (eventType == typeof(RequestEventArg<RequestCode>))
                {   // This is a request.
                    RequestEventArg<RequestCode> request = (RequestEventArg<RequestCode>) eventArg;
                    if (request.RequestType == RequestCode.RequestStart)
                    {   // Process the start request
   
                        List<UV.Lib.Application.IService> list = UV.Lib.Application.AppServices.GetInstance().GetServices(typeof(UV.Lib.DatabaseReaderWriters.DatabaseReaderWriter));
                        if (list != null && list.Count > 0)
                        {   // Database found. 
                            DateTime queryStart = Log.GetTime();
                            UV.Lib.DatabaseReaderWriters.DatabaseReaderWriter db = (UV.Lib.DatabaseReaderWriters.DatabaseReaderWriter)list[0];
                            //
                            // Query Exchange info
                            //
                            UV.Lib.DatabaseReaderWriters.Queries.ExchangeInfoQuery query1 = new UV.Lib.DatabaseReaderWriters.Queries.ExchangeInfoQuery();                            
                            db.SubmitSync(query1);          // request all instrument info
                            Dictionary<int,UV.Lib.DatabaseReaderWriters.Queries.ExchangeInfoItem> m_ExchangeTable = new Dictionary<int,UV.Lib.DatabaseReaderWriters.Queries.ExchangeInfoItem>();
                            foreach (UV.Lib.DatabaseReaderWriters.Queries.ExchangeInfoItem item in query1.Results)
                            {
                                m_ExchangeTable.Add(item.ExchID, item);
                            }

                            //
                            // Query instrument info.
                            //
                            UV.Lib.DatabaseReaderWriters.Queries.InstrumentInfoQuery query = new UV.Lib.DatabaseReaderWriters.Queries.InstrumentInfoQuery();
                            db.SubmitSync(query);          // request all instrument info
                            // Extract info for each instrument.
                            Dictionary<InstrumentName, UV.Lib.DatabaseReaderWriters.Queries.InstrumentInfoItem> m_InstrumentInfo = new Dictionary<InstrumentName, UV.Lib.DatabaseReaderWriters.Queries.InstrumentInfoItem>();
                            foreach (UV.Lib.DatabaseReaderWriters.Queries.InstrumentInfoItem item in query.Results)
                            {
                                // Extract the series name
                                string expiryCode = item.ProductExpiry;
                                int yy = 10 + Convert.ToInt16(expiryCode.Substring(1));
                                int mo = UV.Lib.Utilities.QTMath.GetMonthNumberFromCode(expiryCode.Substring(0, 1));                                
                                string seriesName = string.Format("{0}{1}", UV.Lib.Utilities.QTMath.GetMonthName(mo),yy);
                                // Create product                                
                                UV.Lib.DatabaseReaderWriters.Queries.ExchangeInfoItem exItem = m_ExchangeTable[item.ExchangeId];
                                UVProds.Product product = new UVProds.Product(exItem.ExchNameTT, item.ProductName, item.Type);
                                InstrumentName instrumentName = new InstrumentName(product, seriesName);
                                if (m_InstrumentInfo.ContainsKey(instrumentName))
                                {   // This is an error!
                                    Log.NewEntry(LogLevel.Error, "Duplicate instrumentnames in instrument info table.");
                                    
                                }
                                else
                                {
                                    m_InstrumentInfo.Add(instrumentName, item);
                                }
                            }

                            TimeSpan ts = Log.GetTime().Subtract(queryStart);
                            Log.NewEntry(LogLevel.Minor, "Database table loaded in {0} seconds", ts.TotalSeconds);
                            isSwitchToRunning = true;
                        }
                        else
                        {
                            Log.NewEntry(LogLevel.Major, "IsMarketRunning: Failed to find a database server.  STarting without instr info.");
                            isSwitchToRunning = true;
                        }
                        
                    }
                    else
                        m_PendingRequests.Add((RequestEventArg<RequestCode>)eventArg);          // All other requests must wait.
                }
                else
                {   // The event is not a request.
                    // TODO: Right now we have no place to store this?!
                }


            }
            else if (m_ServiceState == UV.Lib.Application.ServiceStates.Started)
            {   // Server has started, but we are waiting for StartUpProcess to complete.
                Type eventType = eventArg.GetType();
                if (eventType == typeof(RequestEventArg<RequestCode>))
                {   // This is a request.
                    RequestEventArg<RequestCode> request = (RequestEventArg<RequestCode>)eventArg;
                    m_PendingRequests.Add((RequestEventArg<RequestCode>)eventArg);
                }
                else
                {   // The event is not a request.
                    // TODO: Right now we have no place to store this?!
                }
            }
            else
            {
            }
            //
            // Exit
            //
            if (isSwitchToRunning)
                OnServiceStateChanged(UV.Lib.Application.ServiceStates.Running);
            return isSwitchToRunning;
        }
        //
        //
        // *************************************************************************
        // ****                 Process Market Hub Requests()                   ****
        // *************************************************************************
        /// <summary>
        /// There are several ways to obtain the products from the market.
        /// 1) User should provide List of UV-Poducts.
        /// 2) User should provide a marketName, and then all products in this market are requested.
        /// This is called by the hub thread.
        /// </summary>
        private void ProcessHubRequestProducts(RequestEventArg<RequestCode> request)
        {

            List<UVProds.Product> productsFound = new List<UVProds.Product>();      // place here products already known.

            // Process each requested object in request.Data list
            foreach (object o in request.Data)
            {
                //
                // Product request
                //
                if (o is UV.Lib.Products.Product)
                {   // User has provided a Product for us to look for.
                    UV.Lib.Products.Product uvProduct = (UV.Lib.Products.Product)o;
                    ProductKey prodKey = new ProductKey();
                    bool isKnownKey = false;
                    lock (m_ProductMapLock)
                    {
                        isKnownKey = m_ProductMap.TryGetValue(uvProduct, out prodKey);  // See whether key already known for this product
                    }
                    if (isKnownKey && m_Products.ContainsKey(prodKey))
                    {   // This product is completely know, and we have the TT Product already.
                        if (!productsFound.Contains(uvProduct))
                            productsFound.Add(uvProduct);
                    }
                    else if (TTConvertNew.TryConvert(uvProduct, out prodKey))
                    {   // We do not have this TT Product already.  Request TT for it.
                        // We have successfully created a plausible TT ProductKey for this UV-Product.
                        ProductLookupSubscription prodLookup;
                        if (!m_ProductLookupSubscriptionList.TryGetValue(prodKey, out prodLookup))
                        {
                            if (m_TTService.IsRunning)
                            {
                                prodLookup = new ProductLookupSubscription(m_TTService.session, m_TTService.m_Dispatcher, prodKey);
                                prodLookup.Update += new EventHandler<ProductLookupSubscriptionEventArgs>(ProductLookup_Updated);
                                m_ProductLookupSubscriptionList.Add(prodKey, prodLookup);
                                prodLookup.Start();
                            }
                            else
                            {
                                Log.NewEntry(LogLevel.Minor, "No TTService not running.");
                                if (!m_PendingRequests.Contains(request))
                                {// we don't have this request yet.
                                    m_PendingRequests.Add(request);         // add request 
                                    Log.NewEntry(LogLevel.Warning, "ProcessHubRequestProducts. TT API Not available yet, adding to pending requests.", uvProduct, prodKey);
                                }
                            }
                        }
                        else
                            Log.NewEntry(LogLevel.Warning, "ProcessHubRequestProducts. Product {0} becomes TTKey={1} is already subscribed to, waiting for callback.", uvProduct, prodKey);
                    }
                    else
                        Log.NewEntry(LogLevel.Warning, "ProcessHubRequestProducts. Failed to create a TT Product from Product {0}", uvProduct);
                }
                else if (o is string)
                {   // User wants entire product catalog for this exchange.
                    //
                    // Complete product catalog request
                    //
                    string marketName = (string)o;
                    List<Market> marketList = new List<Market>(m_TTService.session.MarketCatalog.Markets.Values);
                    foreach (Market market in marketList)
                    {
                        if (market.Name.Equals(marketName, StringComparison.CurrentCultureIgnoreCase))
                        {   // We recognize the desired market - do this because I fear multiple markets with same "exchange name"
                            // But I think that thats not how TT works... TT has multiple "feeds" for a single market, not multiple markets. 
                            // TODO: Confirm that!
                            ProductCatalogSubscription productCatalog = null;
                            if (m_ProductCatalogSubscriptionList.TryGetValue(market, out productCatalog))
                            {   // We have already subscribed to these products, so fire our current list of known products.
                                // As more arrive, we will fire another event later, since we remain subscribed.
                                // Collect the UV-products in this catalog.
                                lock (m_ProductMapLock)
                                {
                                    UVProds.Product product;
                                    foreach (ProductKey productKey in productCatalog.Products.Keys)
                                        if (m_ProductMapKey.TryGetValue(productKey, out product) && !productsFound.Contains(product))
                                            productsFound.Add(product);
                                }//unlock

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
                    }//next market
                }// data entry is string (that is, market name)
            }//next object o in Data
            // Trigger whatever we discovered as already known.
            if (productsFound.Count > 0)
                OnProductFound(productsFound);                              // trigger event for subscribers

        }// ProcessHubRequestProducts().
        //
        //
        // *************************************************************************
        // ****                 Process Hub Request Instruments()               ****
        // *************************************************************************
        /// <summary>
        /// Process users request for UV Instruments.  This request must be made before 
        /// one can subscribe to market data for instruments.
        /// </summary>
        private void ProcessHubRequestInstruments(RequestEventArg<RequestCode> request)
        {
            List<UVProds.InstrumentName> instrumentsFound = new List<InstrumentName>(); // place for already known instruments.


            if (request.Data == null || request.Data.Count < 1) { return; }
            foreach (object o in request.Data)
            {
                Type dataType = o.GetType();
                //
                // Complete instrument family for a product.
                //
                if (dataType == typeof(UV.Lib.Products.Product))
                {   // User has provided a Product object.
                    // In this case he wants to know about specific product. 
                    // This will request *all* the instruments for the product!
                    UV.Lib.Products.Product UVProduct = (UV.Lib.Products.Product)o;
                    Product ttProduct = null;
                    lock (m_ProductMapLock)
                    {
                        ProductKey productKey;
                        if (m_ProductMap.TryGetValue(UVProduct, out productKey))
                            ttProduct = m_Products[productKey];
                    }
                    if (ttProduct != null)
                        m_PriceListener.SubscribeTo(ttProduct);                     // we have this product in our lists.
                    else
                    {   // User has given us a one-off specific product and we need to know about the product before we get the instruments
                        if (!m_PendingRequests.Contains(request))
                        {

                            m_PendingRequests.Add(request);
                            RequestProducts(UVProduct);
                        }
                    }
                }
                else if (dataType == typeof(InstrumentName))
                {   //
                    // Specific instrument request
                    //
                    InstrumentName instrumentName = (InstrumentName)o;
                    InstrumentDetails details;
                    bool isFound = false;
                    lock (m_InstrumentLock)
                    {
                        if (m_InstrumentDetails.TryGetValue(instrumentName, out details))
                        {   // Found this instrument already.
                            if (!instrumentsFound.Contains(instrumentName))
                                instrumentsFound.Add(instrumentName);
                            isFound = true;
                        }
                    }
                    if (isFound)
                        continue;
                    // See if we at least know the product for this instrument.
                    isFound = false;
                    ProductKey productKey;
                    lock (m_ProductMapLock)
                    {
                        if (m_ProductMap.TryGetValue(instrumentName.Product, out productKey))
                            isFound = true;
                    }
                    if (isFound)
                    {   // We found the productKey which we need to ask for the Instrument from TT.
                        m_PriceListener.SubscribeTo(m_Products[productKey], instrumentName.SeriesName);
                    }
                    else
                    { // user has given us a specific instrument name and we need to know about the product first.
                        if (m_PendingRequests.Contains(request))
                            continue;
                        m_PendingRequests.Add(request);                 // put it in the pending list
                        RequestProducts(instrumentName.Product);        // and request it
                    }
                }
                else if (dataType == typeof(InstrumentKey))
                {
                    //
                    // Specific instrument request via TT Key
                    //
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
                        if (!isFound)
                        {
                            m_PriceListener.SubscribeTo(instrKey);
                        }
                    }
                }
                else
                    Log.NewEntry(LogLevel.Warning, "ProcessHubRequestInstruments: Failed to recognize type of Data {0} in {1}.", dataType.Name, request.ToString());
            }//next data
            // Exit
            if (instrumentsFound != null && instrumentsFound.Count > 0)
                OnMarketBookCreated(instrumentsFound);
        }//ProcessHubRequestInstruments()
        //
        //
        //
        // *************************************************************************
        // ****              Process Hub Request Instruments Price              ****
        // *************************************************************************
        private void ProcessHubRequestInstrumentPrice(RequestEventArg<RequestCode> request)
        {
            if (request.Data == null || request.Data.Count < 1) { return; }
            List<UVProds.InstrumentName> instrumentsFound = new List<InstrumentName>(); // place for already known instruments.
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
                        {   // Found desired instrument.
                            if (m_InstrumentMarkets.ContainsKey(instr))
                            {   // We have already subscribed to this instrument.
                                Log.NewEntry(LogLevel.Minor, "ProcessHubRequestInstrumentPrice: We already have a subscription to {0}. Ignore request {1}.", instr.SeriesName, request.ToString());
                            }
                            else
                            {   // We do not have a subscription to this instrument.
                                if (TryCreateNewBook(instr))    // m_InstrumentDetails[instr].Key.MarketKey.Name);
                                    instrumentsFound.Add(instr);
                                PriceSubscriptionSettings settings;
                                if(!m_IsTopOfBookOnly)
                                    settings = new PriceSubscriptionSettings(PriceSubscriptionType.MarketDepth);
                                else
                                    settings = new PriceSubscriptionSettings(PriceSubscriptionType.InsideMarket);

                                m_PriceListener.SubscribeTo(details.Key, settings);
                            }
                        }
                        else
                        {   // The user has not previously request info about this instrument. We need it before we subscribe to prices.
                            RequestInstruments(instr);
                            if (m_PendingRequests.Contains(request))
                                continue;
                            m_PendingRequests.Add(request);
                        }
                    }
                }
                else if (dataType == typeof(InstrumentKey))
                {
                    InstrumentKey instrKey = (InstrumentKey)dataObj;
                    lock (m_InstrumentLock)
                    {
                        bool isFoundInstrument = false;
                        InstrumentName instrName = new UVProds.InstrumentName();
                        foreach (InstrumentName i in m_InstrumentDetails.Keys)
                        {
                            if (m_InstrumentDetails[i].Key.Equals(instrKey))
                            {
                                instrName = i;
                                isFoundInstrument = true;
                                break;
                            }
                        }
                        if (!isFoundInstrument)
                        {   // Unknown instrument.
                            Log.NewEntry(LogLevel.Warning, "ProcessHubRequestInstrumentPrice: Price subscription request with unknown instrKey = {0}", instrKey.ToString());
                        }
                        else
                        {   // We actually know this instrument.
                            if (!m_InstrumentMarkets.ContainsKey(instrName))
                            {
                                if (TryCreateNewBook(instrName))     //, m_InstrumentDetails[instrName].Key.MarketKey.Name);
                                    instrumentsFound.Add(instrName);
                                PriceSubscriptionSettings settings;
                                if (!m_IsTopOfBookOnly)
                                    settings = new PriceSubscriptionSettings(PriceSubscriptionType.MarketDepth);
                                else
                                    settings = new PriceSubscriptionSettings(PriceSubscriptionType.InsideMarket);
                                m_PriceListener.SubscribeTo(instrKey, settings);
                            }
                        }
                    }
                }
                else
                    Log.NewEntry(LogLevel.Warning, "ProcessHubRequestInstrumentPrice: Failed to recognize data object {0} for request {1}.", dataType.Name, request.ToString());
            }//next data
            // Fire events
            if (instrumentsFound != null && instrumentsFound.Count > 0)
                OnMarketBookCreated(instrumentsFound);

        }// ProcessHubRequestInstrumentPrice()
        //
        //
        // *************************************************************************
        // ****            Process Hub Request Instruments Time And Sales       ****
        // *************************************************************************
        private void ProcessHubRequestInstrumentTimeAndSales(RequestEventArg<RequestCode> request)
        {
            if (request.Data == null || request.Data.Count < 1) { return; }
            List<UVProds.InstrumentName> instrumentsFound = new List<InstrumentName>(); // place for already known instruments.
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
                        {   // Found desired instrument.
                            if (m_InstrumentMarkets.ContainsKey(instr))
                            {   // We have already create a book for this instrument
                                if (!m_TimeAndSalesInstrumentsRequested.Contains(instr))
                                {
                                    m_TimeAndSalesInstrumentsRequested.Add(instr);
                                    m_PriceListener.SubscribeToTimeAndSales(details.Key);
                                }
                                else
                                    Log.NewEntry(LogLevel.Minor, "ProcessHubRequestInstrumentPrice: We already have a time and sales subscription to {0}", instr.SeriesName);
                            }
                            else
                            {   // We do not have a subscription to this instrument.
                                if (TryCreateNewBook(instr))    // m_InstrumentDetails[instr].Key.MarketKey.Name);
                                    instrumentsFound.Add(instr);
                                m_TimeAndSalesInstrumentsRequested.Add(instr);
                                m_PriceListener.SubscribeToTimeAndSales(details.Key);
                            }
                        }
                        else
                        {   // The user has not previously request info about this instrument. We need it before we subscribe to time and sales
                            RequestInstruments(instr);
                            if (m_PendingRequests.Contains(request))
                                continue;
                            m_PendingRequests.Add(request);
                        }
                    }
                }
                else if (dataType == typeof(InstrumentKey))
                {
                    InstrumentKey instrKey = (InstrumentKey)dataObj;
                    lock (m_InstrumentLock)
                    {
                        bool isFoundInstrument = false;
                        InstrumentName instrName = new UVProds.InstrumentName();
                        foreach (InstrumentName i in m_InstrumentDetails.Keys)
                        {
                            if (m_InstrumentDetails[i].Key.Equals(instrKey))
                            {
                                instrName = i;
                                isFoundInstrument = true;
                                break;
                            }
                        }
                        if (!isFoundInstrument)
                        {   // Unknown instrument.
                            Log.NewEntry(LogLevel.Warning, "ProcessHubRequestInstrumentPrice: Price subscription request with unknown instrKey = {0}", instrKey.ToString());
                        }
                        else
                        {   // We actually know this instrument.
                            if (!m_InstrumentMarkets.ContainsKey(instrName))
                            { //no book for this instrument yet so create book and subscribe
                                if (TryCreateNewBook(instrName))     //, m_InstrumentDetails[instrName].Key.MarketKey.Name);
                                    instrumentsFound.Add(instrName);
                                m_PriceListener.SubscribeToTimeAndSales(instrKey);
                            }
                            else if (!m_TimeAndSalesInstrumentsRequested.Contains(instrName))
                            { // we already have a book for this instrument, but no time and sales yet.
                                m_TimeAndSalesInstrumentsRequested.Add(instrName);
                                m_PriceListener.SubscribeToTimeAndSales(instrKey);
                            }
                        }
                    }
                }
                else
                    Log.NewEntry(LogLevel.Warning, "ProcessHubRequestInstrumentPrice: Failed to recognize data object {0} for request {1}.", dataType.Name, request.ToString());
            }//next data
            // Fire events
            if (instrumentsFound != null && instrumentsFound.Count > 0)
                OnMarketBookCreated(instrumentsFound);
        }// ProcessHubRequestInstrumentPrice()
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
                // 
                // Check if we have pending requests we can now process
                //
                ProcessAllPendingRequests();
                OnMarketStatusChanged(mktNames);
            }
            else
                Log.NewEntry(LogLevel.Error, "ProcessMarketCatalogUpdate: MarketCatalogUpdated error = {0}.", eventArg.Error.Message);
        }//ProcessMarketCatalogUpdate()
        //
        //
        //
        // *****************************************************************
        // ****                 ProcessProductLookUp()                  ****
        // *****************************************************************
        /// <summary>
        /// Called by hub thread to process a Product Lookup event from TT.  
        /// If we were waiting to subscribe to a instrument from this product
        /// this will handle re-requesting the instrument now that we have 
        /// found the product.
        /// </summary>
        /// <param name="eventArg"></param>
        private void ProcessProductLookUp(ProductLookupSubscriptionEventArgs eventArg)
        {
            // 
            // Add new products to our tables.
            //
            UV.Lib.Products.Product ourProduct;
            List<UVProds.Product> productsFound = new List<UVProds.Product>();
            if (TTConvertNew.TryConvert(eventArg.Product, out ourProduct))
            {
                // Update our product tables
                lock (m_ProductMapLock)
                {
                    if (!m_ProductMap.ContainsKey(ourProduct))
                    {   // Discovered a new product!
                        m_ProductMap.Add(ourProduct, eventArg.Product.Key);
                        m_ProductMapKey.Add(eventArg.Product.Key, ourProduct);
                        m_Products.Add(eventArg.Product.Key, eventArg.Product);
                        productsFound.Add(ourProduct);
                    }
                }
                //
                // Checking pending requests for requests awaiting a product (instrument requests)
                //                
                List<RequestEventArg<RequestCode>> toReprocess = new List<RequestEventArg<RequestCode>>();      // list of our events we can now process!
                foreach (RequestEventArg<RequestCode> request in m_PendingRequests)
                {
                    if (request.RequestType == RequestCode.RequestInstruments)
                    { // all the requests for an instrument.
                        foreach (object dataObj in request.Data)
                        {
                            Type dataType = dataObj.GetType();
                            if (dataType == typeof(UV.Lib.Products.Product))
                            { //check if we have a product match and if so we can add to reprocess list.
                                if (ourProduct == (UVProds.Product)dataObj)
                                    toReprocess.Add(request);
                            }
                            else if (dataType == typeof(InstrumentName))
                            {//check if we have a instrument match and if so we can add to reprocess list.
                                InstrumentName instr = (InstrumentName)dataObj;
                                if (ourProduct == instr.Product)
                                    toReprocess.Add(request);
                            }
                        }
                    }
                }
                foreach (RequestEventArg<RequestCode> toProcessandRemove in toReprocess)
                { // for each request, delete it and reprocess it.
                    m_PendingRequests.Remove(toProcessandRemove);
                    this.HubEventEnqueue(toProcessandRemove);
                }
                // Trigger events for our subscribers
                if (productsFound.Count > 0)
                    this.OnProductFound(productsFound);
            }
        }
        //
        // *****************************************************************
        // ****             ProcessProductCatalogUpdate()               ****
        // *****************************************************************
        /// <summary>
        /// TT sent us a ProductCatalogUpdate event. Update our list of internal products.
        /// Procedure:  
        ///     TT informs us of ProductCatalog update. Providing TT Products.
        ///     UV-Products are created from these, and both are loading into m_Products, m_ProductMaps.
        /// Called by hub thread.
        /// </summary>
        private void ProcessProductCatalogUpdate(ProductCatalogUpdatedEventArgs eventArg)
        {
            List<UV.Lib.Products.Product> productList = new List<UVProds.Product>();
            foreach (Product ttProduct in eventArg.Added)                   // loop thru each ttProduct found in event.
            {
                UV.Lib.Products.Product ourProduct;
                if (TTConvertNew.TryConvert(ttProduct, out ourProduct))        // Create one of our product objects for each.                
                {
                    productList.Add(ourProduct);                            // Report our discovery
                    Log.NewEntry(LogLevel.Minor, "ProcessProductCatalogUpdate: Found product {0} -> {1}. ", ttProduct.Name, ourProduct);
                    if (!m_Products.ContainsKey(ttProduct.Key))           // This is a new TT Product Key
                    {
                        m_Products.Add(ttProduct.Key, ttProduct);           // store the actual TT product.
                        lock (m_ProductMapLock)                             // and update mappings btwn our product and theirs
                        {
                            m_ProductMap.Add(ourProduct, ttProduct.Key);   // my product -->  productKey map
                            m_ProductMapKey.Add(ttProduct.Key, ourProduct);   // product key --> my product map
                        }
                    }
                    else
                    {   // Would this eve
                        Log.NewEntry(LogLevel.Warning, "ProcessProductCatalogUpdate: Received duplicate product! Received {0} <--> {1}", ttProduct.Name, ourProduct);
                    }

                }
                else
                {   // Since products include server name, there should never be two duplicates!
                    Log.NewEntry(LogLevel.Minor, "ProcessProductCatalogUpdate: Unknown product type {0}. ", ttProduct.Name);
                }
            }
            // Fire the event to our subscribers.                        
            OnProductFound(productList);                      // TODO: Send all products, all for this server, or just the new ones?
        }//ProcessProductCatalogUpdate()
        //
        //
        //
        // *****************************************************************
        // ****             Process Instruments Found()                 ****
        // *****************************************************************
        private void ProcessInstrumentsFound(PriceListener.InstrumentsFoundEventArgs e)
        {
            List<UVProds.InstrumentName> instrFound = new List<UVProds.InstrumentName>();
            lock (m_InstrumentLock)
            {
                foreach (UVProds.InstrumentName name in e.InstrumentDetails.Keys)
                {
                    if (!m_InstrumentDetails.ContainsKey(name))
                    {   // This is a new instrument.  Add to our list of details.
                        InstrumentDetails detail = e.InstrumentDetails[name];
                        m_InstrumentDetails.Add(name, detail);
                        CreateUVInstrumentDetails(name, detail);                          // create our own instrument details and save them.
                        instrFound.Add(name);
                        // TODO: ? Update product list if its a new product.
                    }//if new instrument.               
                    List<RequestEventArg<RequestCode>> toReprocess = new List<RequestEventArg<RequestCode>>();      // list of our events we can now process!
                    foreach (RequestEventArg<RequestCode> request in m_PendingRequests)
                    {   // look through all our pending requests
                        if (request.RequestType == RequestCode.RequestInstrumentPriceSubscription)
                        { // find all the ones that were waiting on an instrument.
                            foreach (InstrumentName instrName in request.Data)
                            { // find if we have a match
                                if (instrName == name)
                                    toReprocess.Add(request);
                            }
                        }
                    }
                    foreach (RequestEventArg<RequestCode> toProcessandRemove in toReprocess)
                    { // if we found any pending requests to reprocess remove them and reprocess them.
                        m_PendingRequests.Remove(toProcessandRemove);
                        this.HubEventEnqueue(toProcessandRemove);
                    }
                }

            }
            OnInstrumentFound(instrFound);
        }// ProcessInstrumentsFound()   
        //
        // *****************************************************************
        // ****             Process Feed Status Change()                ****
        // *****************************************************************
        private void ProcessFeedStatusChange(FeedStatusChangedEventArgs eventArg)
        {
            Log.NewEntry(LogLevel.Minor, "ProcessFeedStatusChange: {0} {1} {2}", eventArg.Feed.Name, eventArg.Feed.Market.Name, eventArg.Feed.Status);


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
                // Resubmit any requests we have pending due to the TT API being down.
                ProcessAllPendingRequests();
            }
        }//ProcessTTApiServiceChange()
        //
        //
        // *****************************************************************
        // ****             Process Hub Request Shutdown()              ****
        // *****************************************************************
        private void ProcessHubRequestShutdown(RequestEventArg<RequestCode> request)
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
        // *****************************************************************
        // ****               ProcessAllPendingRequests()              ****
        // *****************************************************************
        //
        /// <summary>
        /// Caller would like to requeue all pending requests to try to process
        /// them again.
        /// </summary>
        private void ProcessAllPendingRequests()
        {
            while (m_PendingRequests.Count > 0)
            {
                RequestEventArg<RequestCode> req = m_PendingRequests[0];// find the first pending request.
                m_PendingRequests.Remove(req);                          // remove it 
                this.HubEventEnqueue(req);                              // resubmit it
            }
        }
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
        //
        /// <summary>
        /// This is called by the hub thread.
        /// </summary>
        /// <param name="bookID"></param>
        /// <param name="eventArgList"></param>
        /// <returns></returns>
        protected override InstrumentChangeArgs ProcessBookEventsForABook(int bookID, List<EventArgs> eventArgList)
        {

            InstrumentChangeArgs instrChangeArgs = null;
            List<int> marketUpdateList = new List<int>();                       // place instrument IDs of updated markets here.
            List<InstrumentName> newMarketsFoundList = null;                    // place instruments with their first mkt updates here.

            foreach (EventArgs eventArg in eventArgList)
            {
                Type eventArgType = eventArg.GetType();
                if (eventArgType == typeof(UVBookHubs.MarketBase))
                {   //
                    // Process complete market update.
                    //
                    UVBookHubs.MarketBase e = (UVBookHubs.MarketBase)eventArg;
                    int Id;
                    UV.Lib.BookHubs.Market mktInstrument;                    
                    if (m_InstrumentMarkets.TryGetValue(e.Name, out Id) && m_Book[bookID].Instruments.TryGetValue(Id, out mktInstrument))
                    {
                        // 
                        if (mktInstrument.DeepestLevelKnown < 0)
                        {   // This market seems to have never been updated before.
                            // So fire the resource found event for it - signalling its first market update.
                            mktInstrument.SetMarket(e);                     // first update the market.
                            InstrumentName instrName = mktInstrument.Name;
                            if (mktInstrument.DeepestLevelKnown >= 0)
                            {   // Okay.  Update was successful, so fire the event.
                                if (newMarketsFoundList == null)
                                    newMarketsFoundList = new List<InstrumentName>();
                                if (!newMarketsFoundList.Contains(instrName))
                                    newMarketsFoundList.Add(instrName);
                            }
                        }
                        else
                        {   // This is not the first update for this instrument.
                            mktInstrument.SetMarket(e);
                            instrChangeArgs = new InstrumentChangeArgs();
                            instrChangeArgs.AppendChangedInstrument(Id, e.ChangedIndices); 
                        }
                    }
                    m_MarketBaseFactory.Recycle(e);

                }
                else if (eventArgType == typeof(UV.Lib.BookHubs.MarketStatusEventArgs))
                {
                    UV.Lib.BookHubs.MarketStatusEventArgs e = (UV.Lib.BookHubs.MarketStatusEventArgs)eventArg;
                    int Id;
                    UV.Lib.BookHubs.Market mktInstrument;
                    if (m_InstrumentMarkets.TryGetValue(e.InstrumentName, out Id) && m_Book[bookID].Instruments.TryGetValue(Id, out mktInstrument))
                    {
                        if (e.Status == UV.Lib.BookHubs.MarketStatus.Trading)
                            m_Book[bookID].Instruments[Id].IsMarketGood = true;
                        else
                            m_Book[bookID].Instruments[Id].IsMarketGood = false;

                        if (mktInstrument.DeepestLevelKnown >= 0)
                        {
                            instrChangeArgs = new InstrumentChangeArgs();
                            List<int>[] changedDepth =  new List<int>[2];
                            instrChangeArgs.AppendChangedInstrument(Id, changedDepth); 
                        }
                    }
                }
                else
                    Log.NewEntry(LogLevel.Warning, string.Format("Error unexpected event of type {0} : {1}", eventArgType.Name, eventArg.ToString()));
            }
            // Exit
            if (newMarketsFoundList != null)
                OnMarketInstrumentFound(newMarketsFoundList);

            return instrChangeArgs;
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
                Log.NewEntry(LogLevel.Major, "ProductCatalog error. {0}", eventArg.Error.Message);
        }
        private void ProductLookup_Updated(object sender, ProductLookupSubscriptionEventArgs eventArg)
        {
            if (eventArg.Error == null)
                HubEventEnqueue(eventArg);
            else
                Log.NewEntry(LogLevel.Major, "ProductLookup error. {0}", eventArg.Error.Message);
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
            bool isTrue;
            foreach (KeyValuePair<string, string> attr in attributes)
            {
                if (attr.Key == "TopOfBookOnly" && bool.TryParse(attr.Value, out isTrue))
                    this.m_IsTopOfBookOnly = isTrue;
                else if (attr.Key.Equals("ShowLog") && bool.TryParse(attr.Value, out isTrue))
                    this.Log.IsViewActive = isTrue;
            }
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {
        }
        #endregion//IStringifiable

    }
}
