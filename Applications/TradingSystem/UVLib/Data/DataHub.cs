using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UV.Lib;

namespace UV.Lib.Data
{
    using UV.Lib.Hubs;
    using UV.Lib.Application;
    using UV.Lib.Utilities;
    using UV.Lib.Products;
    using UV.Lib.MarketHubs;
    using UV.Lib.Database;
    using UV.Lib.IO.Xml;

    /// <summary>
    /// Market Data Recorder 
    /// 
    /// Currently this schedules itself to shutdown each day at 4:20.  This is due to
    /// TT and the lack of persistence across their server shutdowns.
    /// </summary>
    public class DataHub : Hub , IStringifiable
    {
        // TODO
        // 1. Add handlers for recording market state changes?
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        // external services
        private AppServices m_AppServices;
        private UV.Lib.MarketHubs.MarketHub m_Market;
        public DatabaseWriterHub m_DatabaseWriterHub;
        private QueryBuilderHub m_QueryBuilderHub;

        // Data Controls
        private DateTime m_NextBar;								// time of next bar
        public double m_BarTimeStep = 1;						// in seconds.
        private int m_LastBarTimeStamp = Int32.MaxValue;        // this allows us to ensure we aren't missing any seconds. will be set first time through CreateBar()
        StopFrequency m_StopFrequency = StopFrequency.Daily;    // how often to stop processes
        DateTime m_EndRecordingDateTime;                        // time to automatically shut down
        public string m_EmailAddr = "";                         // email address for reporting

        // Data Base Information
        public DatabaseInfo m_DataBaseInfo;

        // Product and  Instrument Variables
        private List<ProductRequest> m_ProductRequestList = new List<ProductRequest>();
        private List<Product> m_ProductsRequested = new List<Product>();
        private List<InstrumentName> m_InstrumentsRequested = new List<InstrumentName>();
        private Dictionary<Product, List<InstrumentName>> m_InstrumentsByProduct = new Dictionary<Product, List<InstrumentName>>();         // for quick look up and saving!
        private Dictionary<InstrumentName, int> m_InstrToMySQLID = new Dictionary<InstrumentName, int>();
        private Dictionary<int, InstrumentName> m_MySQLIDToInstr = new Dictionary<int, InstrumentName>();
        private List<List<ProductRequest>> m_SplitProdReqs = new List<List<ProductRequest>>();
        private int m_MaxProductsPerRequest = 4;
        private int m_nProductRequested = 0;

        // Recycle Factories
        public RecycleFactory<BarEventArgs> m_BarEventFactory = new RecycleFactory<BarEventArgs>();
        public RecycleFactory<Bar> m_BarFactory = new RecycleFactory<Bar>();
        private RecycleFactory<DataHubRequest> m_RequestFactory = new RecycleFactory<DataHubRequest>();


        // State flags
        private bool m_IsShuttingDown = false;
        private bool m_IsReadyToRequest = false;
        private int m_RequestWaitCount = 40;
        private bool m_IsEmailOnStartStop = true;           // should emails be sent on start up and shutdown
        private bool m_IsDebugMode = false;
        
        #endregion// members

        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        /// <summary>
        /// Default Constructor called by the gui thread to start our data hub.
        /// </summary>
        public DataHub()
            : base("DataHub", UV.Lib.Application.AppInfo.GetInstance().LogPath, true, LogLevel.ShowAllMessages)
        {
            Log.AllowedMessages = LogLevel.ShowAllMessages;
            m_AppServices = UV.Lib.Application.AppServices.GetInstance();                   // find our AppServices

            m_AppServices.TryLoadServicesFromFile("UVDataConfig.txt");                      // load our needed services from our config.
            IService service = null;
            if (m_AppServices.TryGetService("MarketTTAPI", out service))                    // find the TTAPI market, but cast it as a generic MarketHub.
            {
                m_Market = (UV.Lib.MarketHubs.MarketHub)service;
                m_Market.Log.AllowedMessages = LogLevel.ShowAllMessages;
                Hub hub = (Hub)service;
                hub.Log.IsViewActive = true;
                //
                Log.NewEntry(LogLevel.Major, "ConnectToServices: Found market {0}. Subscribing to events.", m_Market.ServiceName);
                m_Market.FoundResource += new EventHandler(HubEventEnqueue);
                m_Market.MarketStatusChanged += new EventHandler(HubEventEnqueue);
                m_Market.ServiceStateChanged += new EventHandler(HubEventEnqueue);
            }

            //
            // Rename our app services
            //
            FrontEnds.FrontEndServices frontEnd = FrontEnds.FrontEndServices.GetInstance();
            frontEnd.AppName = "Data Recorder";
            frontEnd.RunName = FrontEnds.FrontEndServices.RunNameType.Sim1;

            m_AppServices.Connect();                                                        // tells all of our services to connect.
            m_AppServices.Start();                                                          // tells all of our services to start.
        }
        //
        //       
        #endregion//Constructors

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
                if (eArgType == typeof(DataHubRequest))
                {
                    DataHubRequest requestArg = (DataHubRequest)eventArg;
                    switch (requestArg.Request)
                    {
                        case RequestCode.ServiceStateChange:
                            // Process this!
                            break;
                        case RequestCode.Connect:
                            // Process this!
                            break;
                        case RequestCode.RequestProductsToRecord:
                            ProcessProductsToRecord(requestArg);
                            break;
                        default:
                            Log.NewEntry(LogLevel.Error, "HubEventHandler: DataHubRequest {0} not implemented", requestArg.ToString());
                            break;
                    }//switch(Request)
                    m_RequestFactory.Recycle(requestArg);
                }
                else if (eArgType == typeof(FoundServiceEventArg))
                    ProcessFoundResources((FoundServiceEventArg)eventArg);
                else if (eArgType == typeof(DatabaseWriterEventArgs))
                    Log.NewEntry(LogLevel.Major, "Write Completed {0}", eArgType.ToString());
                else if (eArgType == typeof(MarketStatusChangedEventArg))
                    ProcessMarketStatusChangedEvent((MarketStatusChangedEventArg)eventArg);
                else if (eArgType == typeof(DatabaseWriterHub.WriteStatusEventArgs))
                    Log.NewEntry(LogLevel.Major, "Write Status {0}", eArgType.ToString());
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
        #endregion// Hub Event Handler overrides

        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //***********************************************
        // ****             Start()                 ****
        //***********************************************
        public override void Start()
        {
            //
            // Create needed hubs
            //
            m_DatabaseWriterHub = new DatabaseWriterHub(m_DataBaseInfo);
            m_DatabaseWriterHub.Log.AllowedMessages = LogLevel.ShowAllMessages;
            m_DatabaseWriterHub.Log.IsViewActive = true;
            m_DatabaseWriterHub.WriteCompleted += new EventHandler(this.HubEventEnqueue);
            m_DatabaseWriterHub.Start();
            m_QueryBuilderHub = new QueryBuilderHub(this, m_DatabaseWriterHub);

            

            //
            // Set datetime bars.
            //
            DateTime dt = Log.GetTime();
            double totalSeconds = dt.Minute * 60.0 + dt.Second;
            dt = dt.AddSeconds(-totalSeconds);					                            // rip off minutes and seconds.
            totalSeconds = Math.Ceiling(totalSeconds / m_BarTimeStep) * m_BarTimeStep;	    // rounding off to nearest bar.
            totalSeconds += m_BarTimeStep;							                        // increment to next bar.
            dt = dt.AddSeconds(totalSeconds);						                        // set bar time.
            dt = dt.AddMilliseconds(-dt.Millisecond);                                       
            m_NextBar = dt;
            base.m_WaitListenUpdatePeriod = 100;                                            // update every n ms

            //
            // Find stop time.
            //
            DateTime today = DateTime.Today;
            DateTime startTime = DateTime.Now;
            m_EndRecordingDateTime = today.AddMinutes(60 * 16 + 20);                        // we want to stop each day at 4:20 pm
            if (m_StopFrequency == StopFrequency.Daily)
            {
                if (startTime > m_EndRecordingDateTime)                                         // it is already past 4:20, so stop the next day at 4:20        
                    m_EndRecordingDateTime = m_EndRecordingDateTime.AddDays(1);
            }
            else if (m_StopFrequency == StopFrequency.Weekly)
            {
                int daysToAdd = ((int)DayOfWeek.Friday - (int)startTime.DayOfWeek + 7) % 7;
                m_EndRecordingDateTime = m_EndRecordingDateTime.AddDays(daysToAdd);
            }
            Log.NewEntry(LogLevel.Major, "DataHub: Scheduled Shutdown for {0} ", m_EndRecordingDateTime);

            //
            List<ProductRequest> startingRequestList;
            ProductRequest.TryCreateFromFile("ProductRequest.txt", out startingRequestList);
            //
            // Split requests into smaller subsets to avoid TT Choking.
            //
            if (startingRequestList.Count > m_MaxProductsPerRequest)
            { // we need to subset.
                int reqCount = 0;   //dummy counter varialble
                while (reqCount < startingRequestList.Count)
                { // iterate through and subset lists until we are completed.
                    List<ProductRequest> subsettedList = new List<ProductRequest>();
                    int endOfList = Math.Min(m_MaxProductsPerRequest, startingRequestList.Count - reqCount);  //this ensures we don't mess up the indexing
                    subsettedList = startingRequestList.GetRange(reqCount, endOfList);
                    m_SplitProdReqs.Add(subsettedList);
                    reqCount += m_MaxProductsPerRequest;
                }
            }
            else
                m_SplitProdReqs.Add(startingRequestList);               // no need to subset, just add them all.


            m_nProductRequested = startingRequestList.Count;            // store for reporting purposes
            //
            // Send email's on startup
            //
            if (m_IsEmailOnStartStop)
            { // we can abuse the DataBaseWrtiter to send start and stop emails 
                DatabaseWriterEventArgs emailEvent = new DatabaseWriterEventArgs();
                emailEvent.Request = DatabaseWriterRequests.SendEmail;
                emailEvent.QueryBase.AppendFormat("Data Hub Starting {0}", DateTime.Now);
                emailEvent.QueryValues.AppendFormat("Data Hub requesting {0} products to be recorded to {1}. Scheduled for shutdown at {2}",
                    m_nProductRequested,
                    m_DataBaseInfo.Location,
                    m_EndRecordingDateTime);
                m_DatabaseWriterHub.HubEventEnqueue(emailEvent);
            }

            base.Start();
            m_IsReadyToRequest = true;
        }// Start()
        //
        //
        //**********************************************
        // ****         RequestStop()              ****
        //**********************************************
        public override void RequestStop()
        {
            m_IsShuttingDown = true;
        }//RequestStop()
        //
        //
        //
        //***********************************************
        // ****       RequestProductsToRecord()      ****
        //***********************************************
        //
        /// <summary>
        /// Threadsafe call to request products to recorded.  
        /// 
        /// Warning this need to be improved to take into account illiquid months!
        /// </summary>
        /// <param name="productRequest"></param>
        /// <returns></returns>
        public bool RequestProductsToRecord(ProductRequest productRequest)
        {
            DataHubRequest request = m_RequestFactory.Get();
            request.Request = RequestCode.RequestProductsToRecord;
            request.Data.Add(productRequest);
            return this.HubEventEnqueue(request);
        }
        //
        public bool RequestProductsToRecord(List<ProductRequest> productRequestList)
        {
            DataHubRequest request = m_RequestFactory.Get();
            request.Request = RequestCode.RequestProductsToRecord;
            foreach (ProductRequest productRequest in productRequestList)
            {
                request.Data.Add(productRequest);
                Log.NewEntry(LogLevel.Major, "Requesting New Product {0}", productRequest.Product.ProductName);
            }
            return this.HubEventEnqueue(request);
        }
        //
        //
        //************************************************************
        //****                 TryCreateFromFile                  ****
        //************************************************************
        /// <returns>false if faled</returns>
        public static bool TryCreateFromFile(string filePath, out DataHub dataHub)
        {
            dataHub = null;
            //DatabaseInfo dbInfo = null;
            try
            {
                string fullFilePath = string.Format("{0}{1}", Application.AppServices.GetInstance().Info.UserConfigPath, filePath);
                List<IStringifiable> iStringObjects;
                using (StringifiableReader reader = new StringifiableReader(fullFilePath))
                {
                    iStringObjects = reader.ReadToEnd();
                }
                foreach (IStringifiable iStrObj in iStringObjects)
                    if (iStrObj is DataHub)
                        dataHub = (DataHub)iStrObj;
                    //else if (iStrObj is DatabaseInfo)
                    //    dbInfo = (DatabaseInfo)iStrObj;
            }
            catch (Exception e)
            {
                StringBuilder msg = new StringBuilder();
                msg.AppendFormat("Exception: {0}\r\nContinue?", e.Message);
                System.Windows.Forms.DialogResult result = System.Windows.Forms.MessageBox.Show(msg.ToString(), "ProductRequest.TryCreateFromFile", System.Windows.Forms.MessageBoxButtons.OKCancel);
                return result == System.Windows.Forms.DialogResult.OK;
            }
            //if(dbInfo != null)
            //    dataHub.m_DataBaseInfo = dbInfo;
            return true;
        }
        #endregion//Public Methods

        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        // *********************************************************
        // ****                Get Request()                    ****
        // *********************************************************
        /// <summary>
        /// This is helpful function to create/recycle request event args 
        /// for us internally.
        /// </summary>
        protected DataHubRequest GetRequest(RequestCode requestCode)
        {
            return GetRequest(requestCode, null);
        }// 
        /// <summary>
        /// Overloaded version allows for data to be added.
        /// </summary>
        protected DataHubRequest GetRequest(RequestCode requestCode, object arg)
        {
            DataHubRequest request = m_RequestFactory.Get();
            request.Clear();
            if (arg != null)
                request.Data.Add(arg);
            request.Request = requestCode;
            return request;
        }// GetRequest()
        //
        //
        //
        // *********************************************************
        // ****            ProcessFoundResources()              ****
        // *********************************************************
        /// <summary>
        /// Called by the hub thread to deal with newly found resources from the market.  
        /// This is where we request subscription to price feeds we need.
        /// </summary>
        /// <param name="arg"></param>
        private void ProcessFoundResources(FoundServiceEventArg arg)
        {
            if (arg.FoundProducts != null)
            {
                foreach (Product product in arg.FoundProducts)
                {
                    if (m_ProductsRequested.Contains(product))     // this is a product we want to record!
                        m_Market.RequestInstruments(product);      // asynchronous call to find out all instruments for this product.
                } // end product
            }
            else if (arg.FoundInstruments != null)
            { // we should now have save instrument details in the market dictionary 
                List<Product> productsToProcess = new List<Product>();                                          // keep a list of any products we need to process for subscription.
                foreach (InstrumentName instrName in arg.FoundInstruments)                                      // check if we want to know about this product
                { // find all the products we care about in these instruments
                    if (m_ProductsRequested.Contains(instrName.Product))
                    {// we have requested this product!
                        if (m_InstrumentsByProduct.ContainsKey(instrName.Product))
                        {// we already have this product 
                            if (!m_InstrumentsByProduct[instrName.Product].Contains(instrName))
                            { // the instrument isn't in our list however
                                m_InstrumentsByProduct[instrName.Product].Add(instrName);
                                if (!productsToProcess.Contains(instrName.Product))
                                { // make sure we add this product to the list to possibly subscribe to below.
                                    productsToProcess.Add(instrName.Product);
                                }
                            }
                        }
                        else
                        {// the product doesn't exist yet in our list.
                            List<InstrumentName> instrListToAdd = new List<InstrumentName>();
                            instrListToAdd.Add(instrName);
                            m_InstrumentsByProduct.Add(instrName.Product, instrListToAdd);
                            productsToProcess.Add(instrName.Product);                                           // add to our list to process!
                        }
                    }
                } // end foreach
                
                // 
                //  tell the market to subscribe here 
                //
                Log.NewEntry(LogLevel.Major, "DataHub: Found new instruments in {0} products", productsToProcess.Count);
                foreach (Product product in productsToProcess)
                {
                    Log.NewEntry(LogLevel.Major, "DataHub: Processing found instruments for {0}", product.ProductName);
                    
                    List<InstrumentName> instrumentsInProduct = new List<InstrumentName>();                     // create list of instruments
                    m_InstrumentsByProduct.TryGetValue(product, out instrumentsInProduct);                      // try and get all the instruments for this product
                    List<InstrumentDetails> instrDetailList = new List<InstrumentDetails>();                    // create list on details for these instruments
                    List<InstrumentDetails> filteredInstrumentDetailList = new List<InstrumentDetails>();       // filtered list of instruments.
                    
                    if (m_Market.TryGetInstrumentDetails(instrumentsInProduct, out instrDetailList))
                    { // we have instrument details to look at
                        instrDetailList.Sort((x, y) => x.ExpirationDate.CompareTo(y.ExpirationDate));           // sort list by expirations...
                        int noOfContracts = 0;
                        foreach (ProductRequest prodRequest in m_ProductRequestList)
                        {
                            if (prodRequest.Product.Equals(product))
                            {// we found a request for this prod
                                if (prodRequest.nInstrumentsToRecord > noOfContracts)                               // we need to request more contracts 
                                    noOfContracts = prodRequest.nInstrumentsToRecord;                               // set the number of contracts
                                if (prodRequest.m_IsStandardInstrumentsOnly)
                                {// we need to filter!
                                    foreach (InstrumentDetails instrDetails in instrDetailList)
                                        if (instrDetails.isStandard)
                                            filteredInstrumentDetailList.Add(instrDetails);                         // add all "standard" contracts
                                }
                                else
                                { //  we don't need to filter, so just assign to use all instrumnets we found.
                                    filteredInstrumentDetailList = instrDetailList;
                                }
                            }
                        } // end prodRequest
                        noOfContracts = Math.Min(noOfContracts, filteredInstrumentDetailList.Count);                // if we are trying to subscribe to more contracts than exist!
                        for (int i = 0; i < noOfContracts; i++)
                        {
                            int mySQLID = -1;
                            //
                            // the following query building should really be pushed on to the querybuilder hub, since it is set up related, and only happens 
                            // once I am leaving it here.  this should be changed eventually.
                            //
                            if (m_InstrToMySQLID.ContainsKey(filteredInstrumentDetailList[i].InstrumentName)) // quick check to make sure we have never dealt with this instrument.
                                continue;

                            if (DBInstrument.TryGetMySQLInstrumentId(m_DataBaseInfo, filteredInstrumentDetailList[i].InstrumentName, out mySQLID))
                            { // we have it now and should save it in both look up tables.
                                m_InstrToMySQLID.Add(filteredInstrumentDetailList[i].InstrumentName, mySQLID);          // create mapping between instrument and ID.
                                m_MySQLIDToInstr.Add(mySQLID, filteredInstrumentDetailList[i].InstrumentName);          // backwards mapping just in case.
                                string instrDetailsToWriteToDB;
                                if (DBInstrument.TryCheckMySQLInstrumentDetails(m_DataBaseInfo,                         // make sure our data matches in the db is correct
                                    filteredInstrumentDetailList[i], out instrDetailsToWriteToDB))
                                { // we succesffuly checked our instr details.
                                    if (instrDetailsToWriteToDB != string.Empty)
                                    {// if our string isn't empty we need to write data 
                                        m_DatabaseWriterHub.ExecuteNonQuery(instrDetailsToWriteToDB);                   // send it to the db writer hub
                                        Log.NewEntry(LogLevel.Minor, "ProcessFoundResources is ammending instruments details for {0} to the database",
                                            filteredInstrumentDetailList[i].InstrumentName);
                                    }
                                }
                                m_Market.RequestInstrumentPriceSubscription(filteredInstrumentDetailList[i].InstrumentName); // subscribe to instrument 
                                m_Market.RequestInstrumentTimeAndSalesSubscription(filteredInstrumentDetailList[i].InstrumentName); // request time and sales
                                m_InstrumentsRequested.Add(filteredInstrumentDetailList[i].InstrumentName);             // add it to our list of subscriptions.
                            }
                            else
                                Log.NewEntry(LogLevel.Error, "ProcessFoundResources cannot find {0} in database - removing from list", filteredInstrumentDetailList[i].InstrumentName);
                        } // end i
                    }
                } // end entry (foreach)

            }
            else
                Log.NewEntry(LogLevel.Error, "DataHub.ProcessFoundResources empty event type recvd {0}", arg.ToString());
        }
        //
        //
        // *********************************************************
        // ****            ProcessProductsToRecord()            ****
        // *********************************************************
        /// <summary>
        /// Called by the hub thread to deal with requests for products to record. If more instruments are 
        /// requested at a later time this should handle it correctly, however this functionality is largely untested.
        /// </summary>
        /// <param name="requestArg"></param>
        private void ProcessProductsToRecord(DataHubRequest requestArg)
        {
            foreach (object o in requestArg.Data)
            {
                ProductRequest prodRequest = (ProductRequest)o;
                if (!m_ProductRequestList.Contains(prodRequest))
                { // we have never seen this exact request 
                    if (!m_ProductsRequested.Contains(prodRequest.Product))
                    { // we have also never requested this product!
                        m_ProductsRequested.Add(prodRequest.Product);
                        m_ProductRequestList.Add(prodRequest);                                                      // add it 
                        m_Market.RequestProducts(prodRequest.Product);                                              // request it
                    }
                    else
                    { // we have request this product previously but may need to request more contracts now.
                        foreach (ProductRequest completedRequest in m_ProductRequestList)
                        {
                            if (completedRequest.Product == prodRequest.Product &&
                                prodRequest.nInstrumentsToRecord > completedRequest.nInstrumentsToRecord)
                            {// this is the same product and we want more contracts (if less we ignore it)
                                List<InstrumentName> instrumentsInProduct;
                                List<InstrumentDetails> instrumentDetailsList;
                                m_InstrumentsByProduct.TryGetValue(prodRequest.Product, out instrumentsInProduct);                  // find all the instruments
                                if (m_Market.TryGetInstrumentDetails(instrumentsInProduct, out instrumentDetailsList))              // get all the details
                                { // we have instrument details to look at
                                    instrumentDetailsList.Sort((x, y) => x.ExpirationDate.CompareTo(y.ExpirationDate));             // sort list by expirations
                                    int noOfInstruments = Math.Min(prodRequest.nInstrumentsToRecord, instrumentDetailsList.Count);  // if we are trying to subscribe to more contracts than exist!
                                    for (int i = completedRequest.nInstrumentsToRecord - 1; i < noOfInstruments; i++)
                                    { // for every instrument past what we already have, subscribe
                                        m_Market.RequestInstrumentPriceSubscription(instrumentDetailsList[i].InstrumentName);           // subscribe to instrument 
                                        //m_Market.RequestInstrumentTimeAndSalesSubscription(instrumentDetailsList[i].InstrumentName);
                                        m_InstrumentsRequested.Add(instrumentDetailsList[i].InstrumentName);                            // add it to our list of subscriptions.
                                    } // end i
                                }
                            }
                        }
                    }
                }
            }
        } // ProcessProductsToRecord
        //
        //
        // *********************************************************
        // ****        ProcessMarketStatusChangedEvent()        ****
        // *********************************************************
        /// <summary>
        /// Not Yet Implemented!
        /// </summary>
        /// <param name="eventArg"></param>
        private void ProcessMarketStatusChangedEvent(MarketStatusChangedEventArg eventArg)
        {
            //TODO: Add Functionality
        }
        //
        //
        //
        //
        //
        // *********************************************************
        //  ****                 CreateBar()                   ****
        // *********************************************************
        /// <summary>
        /// This is called each time a bar must be created from a snapshot of the current market.
        /// New bars are pushed into the BarEventArg.BarList queue and handed to the QueryBuilder
        /// for query creation and writing.
        /// Called by internal hub thread.
        /// </summary>
        private void CreateBar(DateTime barTime)
        {
            //Log.NewEntry(LogLevel.Minor, "DataHub: CreateBar - Bar Creation Started");
            BarEventArgs eArg = m_BarEventFactory.Get();		// new version GetBarEventArg();
            eArg.unixTime = (int)Utilities.QTMath.DateTimeToEpoch(barTime.ToUniversalTime()); //round this to the floor.
            Log.NewEntry(LogLevel.Minor, "CreateBar: Attempting to create bar for timestamp {0} - miliseconds = {1}", eArg.unixTime, barTime.Millisecond);


            if((eArg.unixTime - m_LastBarTimeStamp) > 1)
            { // we have stepped through time in some interval greater than a second....create email alert for debugging purposes
                DatabaseWriterEventArgs emailEvent = new DatabaseWriterEventArgs();
                emailEvent.Request = DatabaseWriterRequests.SendEmail;
                emailEvent.QueryBase.AppendFormat("Data Hub Missed Timestamp.  Current timestamp={0} and last timestamp={1} difference is {2} seconds", eArg.unixTime, m_LastBarTimeStamp, eArg.unixTime - m_LastBarTimeStamp);
                m_DatabaseWriterHub.HubEventEnqueue(emailEvent);
            }
            m_LastBarTimeStamp = eArg.unixTime;


            //
            // Get markets now.
            //
            UV.Lib.BookHubs.Book aBook;
            if (m_Market.TryEnterReadBook(out aBook))
            {
                foreach (KeyValuePair<int, UV.Lib.BookHubs.Market> aBookMarket in aBook.Instruments)
                {
                    int mySQLID = -1;
                    if (m_InstrumentsRequested.Contains(aBookMarket.Value.Name) && m_InstrToMySQLID.TryGetValue(aBookMarket.Value.Name, out mySQLID))
                    { // we would like to record data for this instrument
                        if (aBookMarket.Value.Qty[(int)UV.Lib.Utilities.QTMath.BidSide][0] == 0 ||
                            aBookMarket.Value.Qty[(int)UV.Lib.Utilities.QTMath.AskSide][0] == 0)
                        {// we have bad data this can happen sometimes in between sessions..
                            //Log.NewEntry(LogLevel.Major, "CreateBar: Bid Or Ask qty for {0} is equal To zero, skipping bar", aBookMarket.Value.Name);
                            continue;
                        }
                        Bar aBar = m_BarFactory.Get();                                                      // grab a bar!
                        aBar.mysqlID = mySQLID;                                                             // set instrument id
                        aBar.bidPrice = aBookMarket.Value.Price[(int)UV.Lib.Utilities.QTMath.BidSide][0];   // set best bid
                        aBar.askPrice = aBookMarket.Value.Price[(int)UV.Lib.Utilities.QTMath.AskSide][0];   // set best ask
                        aBar.bidQty = aBookMarket.Value.Qty[(int)UV.Lib.Utilities.QTMath.BidSide][0];       // set best bidqty
                        aBar.askQty = aBookMarket.Value.Qty[(int)UV.Lib.Utilities.QTMath.AskSide][0];       // set best askqty
                        aBar.lastTradePrice = aBookMarket.Value.LastPrice;
                        aBar.sessionVolume = aBookMarket.Value.Volume[(int)UV.Lib.Utilities.QTMath.LastSide];
                        aBar.longVolume = aBookMarket.Value.Volume[(int)UV.Lib.Utilities.QTMath.BidSide];
                        aBar.shortVolume = aBookMarket.Value.Volume[(int)UV.Lib.Utilities.QTMath.AskSide];
                        aBar.totalVolume = aBar.longVolume + aBar.shortVolume + aBookMarket.Value.Volume[(int)UV.Lib.Utilities.QTMath.UnknownSide];
                        aBar.sessionCode = Convert.ToInt32(aBookMarket.Value.IsMarketGood);                 // flag for trading ==1 or not trading==0
                        eArg.BarList.Enqueue(aBar);
                    }
                }
                m_Market.ExitReadBook(aBook);
            }
            else
            { // something went wrong here!
                Log.NewEntry(LogLevel.Error, "  *********  CreateBar: FAILED TO OBTAIN READ FOR BOOK!  *********");
            }

            if (eArg.BarList.Count > 0 && !m_IsDebugMode) // do not write to db in debug mode.
                m_QueryBuilderHub.HubEventEnqueue(eArg);
        }//CreateBar().
        //
        //
        //
        protected override void UpdatePeriodic()
        {
            //
            // Handle Shutdown 
            //
            if (m_IsShuttingDown)
            { // this will make sure everything get shut down before we exit and stop.
                bool isWaitingToShutdown = false;
                if (m_Market != null && (m_Market.ListenState == WaitListenState.Waiting || m_Market.ListenState == WaitListenState.Working))
                {
                    Log.NewEntry(LogLevel.Major, "Waiting for Market to stop.");
                    m_Market.RequestStop();
                    if (m_IsEmailOnStartStop)
                    { // we can abuse the DataBaseWrtiter to send start and stop emails 
                        DatabaseWriterEventArgs emailEvent = new DatabaseWriterEventArgs();
                        emailEvent.Request = DatabaseWriterRequests.SendEmail;
                        emailEvent.QueryBase.AppendFormat("Data Hub Stopping {0}", DateTime.Now);
                        emailEvent.QueryValues.AppendFormat("Data Hub stopping recording for {0} Instruments",
                            m_InstrumentsRequested.Count);
                        m_DatabaseWriterHub.HubEventEnqueue(emailEvent);
                    }
                    isWaitingToShutdown = true;
                    return;
                }
                if (m_QueryBuilderHub != null && (m_QueryBuilderHub.ListenState == WaitListenState.Waiting || m_QueryBuilderHub.ListenState == WaitListenState.Working))
                {
                    Log.NewEntry(LogLevel.Major, "Waiting for QueryBuilder to stop.");
                    m_QueryBuilderHub.RequestStop();
                    isWaitingToShutdown = true;
                    return;
                }
                if (m_DatabaseWriterHub != null && (m_DatabaseWriterHub.ListenState == WaitListenState.Waiting || m_DatabaseWriterHub.ListenState == WaitListenState.Working))
                {
                    Log.NewEntry(LogLevel.Major, "Waiting for DatabaseWriter to stop.");
                    m_DatabaseWriterHub.RequestStop();
                    isWaitingToShutdown = true;
                    return;
                }
                if (!isWaitingToShutdown)
                {	// MrData now stopping.  Inform form of our state change (which will closed itself in response).
                    m_AppServices.Shutdown();
                    Log.NewEntry(LogLevel.Major, "Data Hub is Now Stopping");
                    base.Stop();
                }

                return;			// bypass usual updating.
            }

            DateTime now = DateTime.Now;
            if (now > m_EndRecordingDateTime)                                // if we are past our shut down time
                m_IsShuttingDown = true;
            //
            // Create a bar
            //
            if (now.CompareTo(m_NextBar) >= 0)
            {
                CreateBar(now);
                // Update next time.			
                double totalSeconds = m_NextBar.Second + m_NextBar.Minute * 60;
                m_NextBar = m_NextBar.AddSeconds(-totalSeconds);
                totalSeconds = Math.Floor(totalSeconds / m_BarTimeStep) * m_BarTimeStep;	// rounding off.
                totalSeconds += m_BarTimeStep;
                m_NextBar = m_NextBar.AddSeconds(totalSeconds);
            }

            if (m_IsReadyToRequest && m_SplitProdReqs.Count > 0)
            { // we still have waiting requests.
                if (m_RequestWaitCount > 160)
                { // are wait time is up!
                    RequestProductsToRecord(m_SplitProdReqs[0]);                                                    // submit the request
                    m_SplitProdReqs.Remove(m_SplitProdReqs[0]);                                                     // remove it from our list.
                    m_RequestWaitCount = 0;                                                                         // reset timer
                }
                m_RequestWaitCount++;
            }

        }
        #endregion//Private Methods

        #region Istringifiable Methods
        // *****************************************************************
        // ****               Istringifiable implementation             ****
        // *****************************************************************
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
            StopFrequency stopFreq;
            foreach (KeyValuePair<string, string> attr in attributes)
            {
                bool isTrue;
                if (attr.Key == "StopFrequency" && Enum.TryParse<StopFrequency>(attr.Value, true, out stopFreq))
                    m_StopFrequency = stopFreq;
                else if (attr.Key == "DebugMode" && bool.TryParse(attr.Value, out isTrue))
                    m_IsDebugMode = isTrue;
                else if (attr.Key == "Email")
                    m_EmailAddr = attr.Value;
            }
        }
        public void AddSubElement(IStringifiable subElement)
        {
            if(subElement is DatabaseInfo)
                m_DataBaseInfo = (DatabaseInfo)subElement;
        }
        #endregion

        #region Enums
        public enum StopFrequency
        {
            Weekly,
            Daily,
        }
        #endregion
    } // end class 
}
