using System;
using System.Collections.Generic;
using System.Collections.Concurrent;        // NET 4.5
using System.Linq;
using UV.Lib.Hubs;
using UV.Lib.Utilities;

using TradingTechnologies.TTAPI;

namespace UV.TTServices.Markets
{
    using MarketBase = UV.Lib.BookHubs.MarketBase;      // update events
    using UVProd = UV.Lib.Products;

    /// <summary>
    /// Because the thread that receives market updates from TT must be the one that 
    /// instantiates a PriceSubscription() object, this object will hold PriceSubscription objects.
    /// </summary>
    public class PriceListener : IDisposable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // 
        private TTApiService m_TTServices = null;
        private LogHub Log = null;                                  // log I can write to.
        private bool m_isDisposing = false;
        private WorkerDispatcher m_Dispatcher = null;               // TT's WorkerDispatcher
        public MarketTTAPI m_Market = null;
        private string m_Name;

        // TT Subscription objects.
        private Dictionary<InstrumentKey, PriceSubscription> m_PriceSubscriptions = new Dictionary<InstrumentKey, PriceSubscription>();
        private Dictionary<InstrumentKey, TimeAndSalesSubscription> m_TimeAndSalesSubscriptions = new Dictionary<InstrumentKey, TimeAndSalesSubscription>();
        private Dictionary<ProductKey, InstrumentCatalogSubscription> m_InstrumentCatalogs = new Dictionary<ProductKey, InstrumentCatalogSubscription>();
        private Dictionary<InstrumentKey, InstrumentLookupSubscription> m_InstrumentLookups = new Dictionary<InstrumentKey, InstrumentLookupSubscription>();


        // Internal tables
        private Dictionary<UVProd.InstrumentName, InstrumentDetails> m_InstrumentDetails = new Dictionary<UVProd.InstrumentName, InstrumentDetails>();
        private Dictionary<InstrumentKey, UVProd.InstrumentName> m_KeyToInstruments = new Dictionary<InstrumentKey, UVProd.InstrumentName>();
        private Dictionary<InstrumentKey, int[]> m_InstrKeyToVolume = new Dictionary<InstrumentKey, int[]>();                 // this is used for time and sales sub only as a way of aggregating delta.

        // Job queue objects
        private Queue<Job> m_WorkQueue = new Queue<Job>();                      // Completely private, owned by Listener thread.
        private ConcurrentQueue<Job> m_InQueue = new ConcurrentQueue<Job>();    // In-Out queue, Jobs pushed from outsider threads.

        //        
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public PriceListener(string priceListenerName, LogHub aLog)
        {
            this.Log = aLog;
            m_TTServices = TTApiService.GetInstance();
            m_Name = priceListenerName;
        }
        //
        //       
        #endregion//Constructors


        #region Private Properties
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
        //
        //
        #endregion//Properties


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        public void Start()
        {
            System.Threading.Thread thread = new System.Threading.Thread(InitializeThread);
            thread.Name = this.m_Name;
            thread.Start();
        }
        // *************************************************
        // ****             SubscribeTo()               ****
        // *************************************************
        /// <summary>
        /// Subscribe to insturment price updates.
        /// This is called by an *external* thread.
        /// </summary> 
        public void SubscribeTo(InstrumentKey instrKey, PriceSubscriptionSettings settings)
        {
            if (m_isDisposing) return;
            Job job = new Job();
            job.InstrumentKey = instrKey;
            job.Settings = settings;
            m_InQueue.Enqueue(job);  // Net 4.5 concurrent collection - no locking needed!
            m_Dispatcher.BeginInvoke(new Action(ProcessAJob));
            m_Dispatcher.Run();
        }//SubscribeTo()        
        /// <summary>
        /// Get a instrument catalog for a product.
        /// Called by external thread.
        /// </summary>
        public void SubscribeTo(Product product)
        {
            if (m_isDisposing) return;
            Job job = new Job();
            job.Product = product;
            m_InQueue.Enqueue(job);      // Net 4.5 version. No locking needed.
            m_Dispatcher.BeginInvoke(new Action(ProcessAJob));
            m_Dispatcher.Run();
        }//SubscribeTo()
        //
        public void SubscribeTo(Product product, string seriesName)
        {
            if (m_isDisposing) return;
            Job job = new Job();
            job.Product = product;
            job.SeriesName = seriesName;
            m_InQueue.Enqueue(job);      // Net 4.5 version. No locking needed.
            m_Dispatcher.BeginInvoke(new Action(ProcessAJob));
            m_Dispatcher.Run();
        }//SubscribeTo()
        /// <summary>
        /// Get a instrument catalog for single instrument.
        /// Called by external thread.
        /// </summary>

        public void SubscribeTo(InstrumentKey instrKey)
        {
            if (m_isDisposing) return;
            Job job = new Job();
            job.InstrumentKey = instrKey;
            m_InQueue.Enqueue(job);      // Net 4.5 version. No locking needed.
            m_Dispatcher.BeginInvoke(new Action(ProcessAJob));
            m_Dispatcher.Run();
        }//SubscribeTo()   
        //
        //
        //
        public void Dispose()
        {
            if (m_isDisposing) return;
            m_isDisposing = true;
            m_Dispatcher.BeginInvoke(new Action(StopThread));
            try
            {
                m_Dispatcher.Run();
            }
            catch (Exception)
            {
            }
        }//Dispose()
        //
        //
        public void SubscribeToTimeAndSales(InstrumentKey instrKey)
        {
            if (m_isDisposing) return;
            Job job = new Job();
            job.InstrumentKey = instrKey;
            job.IsTimeAndSales = true;
            m_InQueue.Enqueue(job);  // Net 4.5 concurrent collection - no locking needed!
            m_Dispatcher.BeginInvoke(new Action(ProcessAJob));
            m_Dispatcher.Run();
        }//SubscribeTo()  
        //
        //
        #endregion//Public Methods


        #region Private Startup and Shutdown Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        /// <summary>
        /// Called from constructor to capture the PriceListener thread.  He is captured once
        /// he creates a Dispatcher and calls an invoke on himself.
        /// </summary>
        private void InitializeThread()
        {
            m_Dispatcher = TradingTechnologies.TTAPI.Dispatcher.AttachWorkerDispatcher();
            m_Dispatcher.BeginInvoke(new Action(InitComplete));

            m_Dispatcher.Run();                                                 // this tells thread to do this and wait.
        }
        private void InitComplete()
        {
            //this.Log.NewEntry(LogLevel.Major, "{0}: Price listener started.",this.Name);
        }
        //
        // ****             Stop Thread()               ****
        /// <summary>
        /// Carefully release all TT objects for disposal.
        /// </summary>
        private void StopThread()
        {
            try
            {
                if (m_Dispatcher != null && (!m_Dispatcher.IsDisposed))
                {
                    m_Dispatcher.BeginInvokeShutdown();
                }
            }
            catch (Exception)
            {
            }

            foreach (PriceSubscription subscription in m_PriceSubscriptions.Values)
                subscription.Dispose();
            foreach (InstrumentCatalogSubscription sub in m_InstrumentCatalogs.Values)
                sub.Dispose();
            foreach (TimeAndSalesSubscription subscription in m_TimeAndSalesSubscriptions.Values)
                subscription.Dispose();
            m_PriceSubscriptions.Clear();
            m_InstrumentCatalogs.Clear();
            m_TimeAndSalesSubscriptions.Clear();
        }//StopThread()
        //
        //
        //
        #endregion // private Methods


        #region Job Processing
        // *****************************************************************
        // ****                     Private Job Class                   ****
        // *****************************************************************
        /// <summary>
        /// Helper class for creation of jobs to be processed. 
        /// </summary>
        private class Job
        {
            public TradingTechnologies.TTAPI.Product Product = null;
            public TradingTechnologies.TTAPI.InstrumentKey InstrumentKey;
            public TradingTechnologies.TTAPI.PriceSubscriptionSettings Settings = null;
            public string SeriesName = string.Empty;
            public bool IsTimeAndSales = false;
        }//Job
        // *****************************************************************
        // ****             Job Processing Methods                      ****
        // *****************************************************************
        /// <summary>
        /// Called by the thread's dispatcher after an outside thread has pushed a new Job.
        /// </summary>
        private void ProcessAJob()
        {
            if (m_isDisposing)
                return;
            Job aJob;
            while (m_InQueue.TryDequeue(out aJob))              // NET 4.5 concurrent queue. No locking needed.
                m_WorkQueue.Enqueue(aJob);                      // push onto my private queue.

            //
            // Process the jobs now.
            //
            while (m_WorkQueue.Count > 0)
            {
                Job job = m_WorkQueue.Dequeue();
                if (job.Product != null)
                {   //
                    if (string.IsNullOrEmpty(job.SeriesName))
                    {   // User wants all instruments assoc with this product.
                        //
                        // Process Instrument Catalog requests
                        //
                        InstrumentCatalogSubscription instrumentSub = null;
                        if (!m_InstrumentCatalogs.TryGetValue(job.Product.Key, out instrumentSub))
                        {   // Failed to find a subscription.  Create a new one!
                            instrumentSub = new InstrumentCatalogSubscription(job.Product, m_Dispatcher);
                            instrumentSub.InstrumentsUpdated += InstrumentCatalog_InstrumentsUpdated;
                            instrumentSub.Start();                                                  // submit the request.
                            m_InstrumentCatalogs.Add(job.Product.Key, instrumentSub);               // store the catalog object
                            Log.NewEntry(LogLevel.Minor, "{0}: Subscribing to instr catalog for {1}.", this.Name, job.Product.Name);
                        }
                    }
                    else
                    {   // User requested Instrument info using the ProductKey and a series Name only. (Not instr Key).
                        //InstrumentLookupSubscription lookup = null;
                        Log.NewEntry(LogLevel.Major, "{0}: InstrumentLookup {1} {2}.", this.Name, job.Product, job.SeriesName);
                        InstrumentLookupSubscription subscriber = new InstrumentLookupSubscription(m_TTServices.session, m_Dispatcher, job.Product.Key, job.SeriesName);
                        subscriber.Update += new EventHandler<InstrumentLookupSubscriptionEventArgs>(InstrumentLookup_InstrumentUpdated);
                        //m_InstrumentLookupsUnknown.Add(job.InstrumentKey, subscriber);
                        subscriber.Start();
                    }
                }
                else if (job.InstrumentKey != null && job.Settings == null && !job.IsTimeAndSales)
                {   //
                    // Process an Instrument information request
                    //
                    InstrumentLookupSubscription lookup = null;
                    if (!m_InstrumentLookups.TryGetValue(job.InstrumentKey, out lookup))
                    {
                        Log.NewEntry(LogLevel.Major, "{0}: InstrumentLookup {1}.", this.Name, job.InstrumentKey);
                        InstrumentLookupSubscription subscriber = new InstrumentLookupSubscription(m_TTServices.session, m_Dispatcher, job.InstrumentKey);
                        subscriber.Update += new EventHandler<InstrumentLookupSubscriptionEventArgs>(InstrumentLookup_InstrumentUpdated);
                        m_InstrumentLookups.Add(job.InstrumentKey, subscriber);
                        subscriber.Start();
                    }
                    else
                    {
                        Log.NewEntry(LogLevel.Major, "{0}: InstrumentLookup {1} already submitted.", this.Name, job.InstrumentKey);
                    }

                }
                else if (job.InstrumentKey != null)
                {   //
                    // Subscribe to an instrument price
                    //                    
                    Instrument instrument = null;
                    InstrumentCatalogSubscription catalog = null;                               // First, find instrument catalog for this instr.
                    InstrumentLookupSubscription instrumentSub = null;                          // or find a specific instrument subscription.
                    //InstrumentDetails instrumentDetails = null;
                    //UVProd.InstrumentName instrumentName ;
                    if (m_InstrumentLookups.TryGetValue(job.InstrumentKey, out instrumentSub))
                    {
                        instrument = instrumentSub.Instrument;
                    }
                    else if (m_InstrumentCatalogs.TryGetValue(job.InstrumentKey.ProductKey, out catalog))
                    {
                        catalog.Instruments.TryGetValue(job.InstrumentKey, out instrument);
                    }
                    //else if (m_KeyToInstruments.TryGetValue(job.InstrumentKey, out instrumentName) && m_InstrumentDetails.TryGetValue(instrumentName, out instrumentDetails))
                    //{
                    //    m_InstrumentLookups.TryGetValue(job.InstrumentKey, out instrumentSub)
                    //}
                    else
                    {
                        Log.NewEntry(LogLevel.Minor, "{0}: I failed to find instrument key {1}.", this.Name, job.InstrumentKey.ToString());
                        return;
                    }
                    if (instrument != null)
                    {
                        if (!job.IsTimeAndSales)
                        { // this is a market data subscription request  - Subscribe or update pre-existing subscription.
                            PriceSubscription priceSub = null;
                            if (!m_PriceSubscriptions.TryGetValue(instrument.Key, out priceSub))
                            {   // Can't find a subscription, so create one.
                                Log.NewEntry(LogLevel.Major, "{0}: Creating new subscription for {1} with settings {2}.", this.Name, instrument.Name, job.Settings.PriceData);
                                priceSub = new PriceSubscription(instrument, Dispatcher.Current);
                                m_PriceSubscriptions.Add(instrument.Key, priceSub);                                      // add to our list of subscription objects.
                                if (!m_InstrKeyToVolume.ContainsKey(instrument.Key))
                                    m_InstrKeyToVolume.Add(instrument.Key, new int[4]);                                 // create a new array for volume aggregations    
                                priceSub.FieldsUpdated += new FieldsUpdatedEventHandler(PriceSubscription_Updated);     // attach my handler to it.
                                priceSub.Settings = job.Settings;
                                priceSub.Start();
                            }
                            else
                            {
                                Log.NewEntry(LogLevel.Major, "{0}: Found old subscription for {1}.  Overwriting settings {2}.", this.Name, instrument.Name, job.Settings.ToString());
                                priceSub.Settings = job.Settings;
                                priceSub.Start();
                            }
                        }
                        else
                        { // this is a time and sales data request
                            TimeAndSalesSubscription timeAndSalesSub = null;
                            if (!m_TimeAndSalesSubscriptions.TryGetValue(instrument.Key, out timeAndSalesSub))
                            {   // Can't find a subscription, so create one.
                                Log.NewEntry(LogLevel.Major, "{0}: Creating new time and sales subscription for {1}", this.Name, instrument.Name);
                                timeAndSalesSub = new TimeAndSalesSubscription(instrument, Dispatcher.Current);
                                m_TimeAndSalesSubscriptions.Add(instrument.Key, timeAndSalesSub);                       // add to our list of subscription objects.
                                if (!m_InstrKeyToVolume.ContainsKey(instrument.Key))
                                    m_InstrKeyToVolume.Add(instrument.Key, new int[4]);                                 // create a new array for volume aggregations    
                                timeAndSalesSub.Update += new EventHandler<TimeAndSalesEventArgs>(TimeAndSalesSubscription_Updated);     // attach my handler to it.
                                timeAndSalesSub.Start();
                            }
                            else
                            {
                                Log.NewEntry(LogLevel.Major, "{0}: Found existing time and sales subscription for {1}.", this.Name, instrument.Name, job.Settings.ToString());
                            }
                        }
                    }
                }
            }//wend Job in WorkQueue.
        }//ProcessAJob()
        //

        //
        #endregion// private methods



        #region TT Callback Event Handlers
        // *****************************************************************************
        // ****                     TT Callback Event Handlers                      ****
        // *****************************************************************************
        /// <summary>
        /// Using TT's dispatcher model, the thread in these methods is my local thread.
        /// </summary>
        private void InstrumentCatalog_InstrumentsUpdated(object sender, InstrumentCatalogUpdatedEventArgs eventArgs)
        {
            if (m_isDisposing) return;
            if (eventArgs.Error != null)
            {
                Log.NewEntry(LogLevel.Warning, "{0}: Error in instrument catalog {1}.", this.Name, eventArgs.Error.Message);
                return;
            }
            //
            foreach (Instrument ttInstrument in eventArgs.Added)
            {
                UVProd.InstrumentName instrName;
                if (TTConvertNew.TryConvert(ttInstrument, out instrName))
                {   // Success in converting to our internal type.
                    InstrumentDetails details;
                    if (m_InstrumentDetails.TryGetValue(instrName, out details))
                    {                                                               // This instrument was already added!
                        if (!ttInstrument.Key.Equals(details.Key))
                            Log.NewEntry(LogLevel.Warning, "{0}: Instrument {1} found before with non-unique key {2}!", this.Name, instrName.FullName, instrName.SeriesName);
                        else
                            Log.NewEntry(LogLevel.Warning, "{0}: Instrument {1} found before and keys match! Good.", this.Name, instrName.FullName);
                    }
                    else
                    {
                        m_KeyToInstruments.Add(ttInstrument.Key, instrName);
                        m_InstrumentDetails.Add(instrName, ttInstrument.InstrumentDetails);
                        Log.NewEntry(LogLevel.Minor, "{0}: Instruments found {1} <---> {2}.", this.Name, instrName, ttInstrument.Key.ToString());
                    }
                }
                else
                {   // Failed to convert TT instrument to a UV Instrument.
                    // This happens because either their name is too confusing to know what it is.
                    // Or, more likely, we are set to ignore the product type (options, equity, swaps).
                    Log.NewEntry(LogLevel.Warning, "{0}: Instrument creation failed for {1}.", ttInstrument.Key.ToString());
                }
            }// next instr added
            OnInstrumentsFound();                                                       // Trigger event for subscribers
        }//InstrumentCatalog_InstrumentsUpdated()
        //
        //
        private void InstrumentLookup_InstrumentUpdated(object sender, InstrumentLookupSubscriptionEventArgs eventArgs)
        {
            if (eventArgs.Instrument != null && eventArgs.Error == null)
            {
                UVProd.InstrumentName instrName;
                Instrument ttInstrument = eventArgs.Instrument;
                if (TTConvertNew.TryConvert(ttInstrument, out instrName))
                {   // Success in converting to our internal naming scheme.
                    InstrumentDetails details;
                    if (m_InstrumentDetails.TryGetValue(instrName, out details))
                    {   // This instrument was already added!
                        if (!ttInstrument.Key.Equals(details.Key))
                            Log.NewEntry(LogLevel.Warning, "{0}: Instrument {1} found before with non-unique key {2}!", this.Name, instrName.FullName, instrName.SeriesName);
                        else
                            Log.NewEntry(LogLevel.Warning, "{0}: Instrument {1} found before and keys match! Good.", this.Name, instrName.FullName);
                    }
                    else
                    {   // Add new InstrumentDetails
                        m_KeyToInstruments.Add(ttInstrument.Key, instrName);
                        m_InstrumentDetails.Add(instrName, ttInstrument.InstrumentDetails);
                        Log.NewEntry(LogLevel.Minor, "{0}: Instruments found {1} <---> {2}.", this.Name, instrName, ttInstrument.Key.ToString());
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
                    }
                }
                else
                {   // Failed to convert TT instrument to a UV Instrument.
                    // This happens because either their name is too confusing to know what it is.
                    // Or, more likely, we are set to ignore the product type (options, equity, swaps).
                    Log.NewEntry(LogLevel.Warning, "{0}: Instrument creation failed for {1}.", this.Name, ttInstrument.Key.ToString());
                }
                OnInstrumentsFound();
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
        private List<EventArgs> m_NewEvents = new List<EventArgs>();
        //
        //
        //******************************************************************
        //****              PriceSubscription_Updated()                 ****
        //******************************************************************
        /// <summary>
        /// NOTE : Currently this system only worries about "direct" or non implied qty's
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void PriceSubscription_Updated(object sender, FieldsUpdatedEventArgs eventArgs)
        {
            if (m_isDisposing) return;
            bool isSnapShot = (eventArgs.UpdateType == UpdateType.Snapshot);
            bool isFireMarketBaseEvent = false;
            if (eventArgs.Error == null)
            {
                UVProd.InstrumentName instrumentName;
                InstrumentKey key = eventArgs.Fields.Instrument.Key;
                if (m_KeyToInstruments.TryGetValue(key, out instrumentName))
                {
                    // Analyze by finding the first depth at which a field has changed
                    m_NewEvents.Clear();
                    MarketBase newEvent = m_Market.m_MarketBaseFactory.Get();        // Get an event arg.
                    newEvent.Clear();                                                // make sure our event is clean.
                    newEvent.Name = instrumentName;
                    int maxDepth = Math.Min(MarketBase.MaxDepth, eventArgs.Fields.GetLargestCurrentDepthLevel());
                    newEvent.DeepestLevelKnown = maxDepth;
                    FieldId[] changedFieldIds;
                    for (int changedDepth = 0; changedDepth < maxDepth; changedDepth++)
                    {
                        changedFieldIds = eventArgs.Fields.GetChangedFieldIds(changedDepth);
                        if (changedFieldIds.Length > 0)
                        {
                            // *****************************************************
                            // ****             MarketBase updates              ****
                            // *****************************************************
                            //
                            // Bid side
                            //
                            if (isSnapShot || changedFieldIds.Contains<FieldId>(FieldId.DirectBidPrice) || changedFieldIds.Contains<FieldId>(FieldId.DirectBidQuantity))
                            {
                                isFireMarketBaseEvent = true;
                                newEvent.ChangedIndices[MarketBase.BidSide].Add(changedDepth);

                                Price p = (Price)eventArgs.Fields.GetDirectBidPriceField(changedDepth).Value;
                                Quantity q = (Quantity)eventArgs.Fields.GetDirectBidQuantityField(changedDepth).Value;
                                if (p.IsValid && p.IsTradable)
                                {
                                    newEvent.Price[MarketBase.BidSide][changedDepth] = p.ToDouble();
                                    newEvent.Qty[MarketBase.BidSide][changedDepth] = q.ToInt(); ;
                                }
                            }
                            //
                            // Ask side
                            //
                            if (isSnapShot || changedFieldIds.Contains<FieldId>(FieldId.DirectAskPrice) || changedFieldIds.Contains<FieldId>(FieldId.DirectAskQuantity))
                            {
                                isFireMarketBaseEvent = true;
                                newEvent.ChangedIndices[MarketBase.AskSide].Add(changedDepth);

                                Price p = (Price)eventArgs.Fields.GetDirectAskPriceField(changedDepth).Value;
                                Quantity q = (Quantity)eventArgs.Fields.GetDirectAskQuantityField(changedDepth).Value;
                                if (p.IsValid && p.IsTradable)
                                {
                                    newEvent.Price[MarketBase.AskSide][changedDepth] = p.ToDouble();
                                    newEvent.Qty[MarketBase.AskSide][changedDepth] = q.ToInt(); ;
                                }
                            }
                            //
                            // Last
                            //
                            // Here, we do not distinguish between buy/sell side volume.  Hence only total volume is 
                            // counted here.  The total volume is indexed as "MarketBase.LastSide".
                            // If we ever decide we want more information than this we can use a TimeAndSalesSubscription which will report side
                            // to help sorting out the side of the market
                            //
                            if (isSnapShot || changedFieldIds.Contains<FieldId>(FieldId.LastTradedPrice) || changedFieldIds.Contains<FieldId>(FieldId.LastTradedQuantity))
                            {
                                isFireMarketBaseEvent = true; // i think we need to fire this for strategies that care about volume
                                Price p = (Price)eventArgs.Fields.GetField(FieldId.LastTradedPrice).Value;
                                Quantity q = (Quantity)eventArgs.Fields.GetField(FieldId.LastTradedQuantity).Value;
                                Quantity totalVolume = (Quantity)eventArgs.Fields.GetField(FieldId.TotalTradedQuantity).Value;
                                if (p.IsValid && q.IsValid)
                                {
                                    newEvent.Price[MarketBase.LastSide][0] = p.ToDouble();
                                    newEvent.Qty[MarketBase.LastSide][0] = q.ToInt(); ;
                                    newEvent.ChangedIndices[MarketBase.LastSide].Add(0);            // last price is always top of book
                                    newEvent.Volume[MarketBase.LastSide] = totalVolume.ToInt();     // Only identify total volume.
                                    m_InstrKeyToVolume[key][QTMath.LastSide] = totalVolume.ToInt(); // save total volume in array. 
                                }
                            }

                            if (changedDepth == 0)
                            {
                                // *****************************************************
                                // ****             Series Status updates           ****
                                // *****************************************************
                                if (changedFieldIds.Contains<FieldId>(FieldId.SeriesStatus))
                                {
                                    TradingStatus status = (TradingStatus)eventArgs.Fields.GetField(FieldId.SeriesStatus).Value;
                                    Log.NewEntry(LogLevel.Minor, "PriceListener: SeriesStatus change {0} is {1}.", instrumentName, status.ToString());
                                    UV.Lib.BookHubs.MarketStatusEventArgs e = new UV.Lib.BookHubs.MarketStatusEventArgs();
                                    //e.Instrument = instrument;
                                    e.InstrumentName = instrumentName;
                                    if (status == TradingStatus.Trading)
                                        e.Status = UV.Lib.BookHubs.MarketStatus.Trading;
                                    else if (status == TradingStatus.Closed || status == TradingStatus.ClosingAuction || status == TradingStatus.Expired ||
                                        status == TradingStatus.NotTradable || status == TradingStatus.PostTrading)
                                    { // we have entered into a "non trading" state, so we need to reset our volume counts
                                        e.Status = UV.Lib.BookHubs.MarketStatus.NotTrading;
                                        foreach(int[] volumeArray in m_InstrKeyToVolume.Values)
                                        { // for every volume array 
                                            for(int i = 0; i < volumeArray.Length; i++)
                                            { // set volume back to zero for all sides except last, since we use session volume as reported by TT
                                                if (i == QTMath.LastSide)
                                                    continue;
                                                volumeArray[i] = 0;
                                            }
                                        }
                                    }
                                    else
                                        e.Status = UV.Lib.BookHubs.MarketStatus.Special;
                                    m_NewEvents.Add(e);
                                }

                                // *****************************************************
                                // ****             Session Rollover                ****
                                // *****************************************************
                                if (changedFieldIds.Contains<FieldId>(FieldId.SessionRollover))
                                {
                                    TradingStatus status = (TradingStatus)eventArgs.Fields.GetField(FieldId.SeriesStatus).Value;
                                    Log.NewEntry(LogLevel.Minor, "PriceListener: SessionRollover change {0} is {1}.", instrumentName, status.ToString());
                                    UV.Lib.BookHubs.MarketStatusEventArgs e = new UV.Lib.BookHubs.MarketStatusEventArgs();
                                    //e.Instrument = instrument;
                                    e.InstrumentName = instrumentName;
                                    if (status == TradingStatus.Trading)
                                        e.Status = UV.Lib.BookHubs.MarketStatus.Trading;
                                    else if (status == TradingStatus.Closed || status == TradingStatus.ClosingAuction || status == TradingStatus.Expired ||
                                        status == TradingStatus.NotTradable || status == TradingStatus.PostTrading)
                                        e.Status = UV.Lib.BookHubs.MarketStatus.NotTrading;
                                    else
                                        e.Status = UV.Lib.BookHubs.MarketStatus.Special;
                                    m_NewEvents.Add(e);
                                }
                            }
                        }// end changed fields lenght
                    } // end changedepth loop

                    // *****************************************************
                    // ****             Fire Events Now                 ****
                    // *****************************************************
                    if (isFireMarketBaseEvent)
                        m_NewEvents.Add(newEvent);
                    else
                        m_Market.m_MarketBaseFactory.Recycle(newEvent);
                    ProcessPriceChangeEvents(ref m_NewEvents);
                } // end if instrument key not found
                else
                    Log.NewEntry(LogLevel.Warning, "{0}: Failed to find instrument for TTKey {1}.", this.Name, key);
            }
            else
                Log.NewEntry(LogLevel.Warning, "{0}: Error in price subscription {1}.", this.Name, eventArgs.Error.Message);
        }//PriceSubscription()
        //
        public ProcessPriceChangeDelegate ProcessPriceChangeEvents;
        public delegate void ProcessPriceChangeDelegate(ref List<EventArgs> newEvents);
        //
        //
        //******************************************************************
        // ****          TimeAndSalesSubscription_Updated()             ****
        //******************************************************************
        //
        /// <summary>
        /// Time and Sales data is uncoalesced trade data.  This allows us to a better job 
        /// of diffrentiating sides volume traded on while recording data for analysis. 
        /// This update Volume on all sides but last. The price subscription has a good last traded volume
        /// field, this is used only for more in depth analysis when needed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void TimeAndSalesSubscription_Updated(object sender, TimeAndSalesEventArgs eventArgs)
        {
            if (m_isDisposing) return;
            if (eventArgs.Error == null)
            {
                UVProd.InstrumentName instrumentName;
                InstrumentKey key = eventArgs.Instrument.Key;
                if (m_KeyToInstruments.TryGetValue(key, out instrumentName))
                {
                    if (eventArgs.Data.Count != 0)
                    { // is any data packed with this event
                        m_NewEvents.Clear();
                        MarketBase newEvent = m_Market.m_MarketBaseFactory.Get();        // Get an event arg.
                        newEvent.ClearVolume();                                          // make sure our event is clean.
                        newEvent.Name = instrumentName;
                        int[] instrumentVolumeArray = m_InstrKeyToVolume[key];

                        foreach (TimeAndSalesData timeAndSalesData in eventArgs.Data)
                        { // for each trade
                            if (!timeAndSalesData.IsOverTheCounter)
                            { // this trade was not OTC
                                int tradeSide = TTConvertNew.ToUVMarketSide(timeAndSalesData.Direction); //long, short or unknown 
                                if (timeAndSalesData.TradeQuantity.IsValid)
                                { // qty is valid so aggregate all qty's by the direction of the trade
                                    instrumentVolumeArray[tradeSide] += timeAndSalesData.TradeQuantity.ToInt();
                                }
                            }
                        }

                        for (int side = 0; side < newEvent.Volume.Length; side++)
                        { // update all sides
                            newEvent.Volume[side] = instrumentVolumeArray[side];
                        }

                        newEvent.IsIncludesTimeAndSales = true;
                        m_NewEvents.Add(newEvent);
                        ProcessPriceChangeEvents(ref m_NewEvents);
                    }
                }
            }
        }
        //
        #endregion//TT callback event handlers


        #region InstrumentsFound
        //
        //
        //
        // *************************************************************
        // ****                 Instruments Found                   ****
        // *************************************************************
        public event EventHandler InstrumentsFound;
        //
        private void OnInstrumentsFound()
        {
            if (this.InstrumentsFound != null)
            {
                InstrumentsFoundEventArgs e = new InstrumentsFoundEventArgs();
                foreach (UVProd.InstrumentName name in m_InstrumentDetails.Keys)
                {
                    //e.Instruments.Add(m_Instruments[name]);
                    //e.InstrumentDetails.Add(m_InstrumentDetails[name]);
                    e.InstrumentDetails.Add(name, m_InstrumentDetails[name]);
                }
                this.InstrumentsFound(this, e);
            }
        }
        public class InstrumentsFoundEventArgs : EventArgs
        {
            //public List<UV.Lib.Products.InstrumentBase> Instruments = new List<UV.Lib.Products.InstrumentBase>();
            //public List<InstrumentDetails> InstrumentDetails = new List<InstrumentDetails>();            
            public Dictionary<UVProd.InstrumentName, InstrumentDetails> InstrumentDetails = new Dictionary<UVProd.InstrumentName, InstrumentDetails>();
        }
        //
        //
        //
        #endregion // my events

    }
}
