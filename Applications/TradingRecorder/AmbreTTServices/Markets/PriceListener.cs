using System;
using System.Collections.Generic;
using System.Collections.Concurrent;        // NET 4.5
using System.Linq;

using Misty.Lib.Hubs;
using Misty.Lib.Utilities;

using TradingTechnologies.TTAPI;

namespace Ambre.TTServices.Markets
{

    using MistyProd = Misty.Lib.Products;

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
        private Dictionary<ProductKey, InstrumentCatalogSubscription> m_InstrumentCatalogs = new Dictionary<ProductKey, InstrumentCatalogSubscription>();
        private Dictionary<InstrumentKey, InstrumentLookupSubscription> m_InstrumentLookups = new Dictionary<InstrumentKey, InstrumentLookupSubscription>();


        // Internal tables
        private Dictionary<MistyProd.InstrumentName, InstrumentDetails> m_InstrumentDetails = new Dictionary<MistyProd.InstrumentName, InstrumentDetails>();
        private Dictionary<InstrumentKey, MistyProd.InstrumentName> m_KeyToInstruments = new Dictionary<InstrumentKey, MistyProd.InstrumentName>();
        

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
            m_Dispatcher.Run();        
        }//Dispose()
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
            if (m_Dispatcher != null && (!m_Dispatcher.IsDisposed))
                m_Dispatcher.Dispose();

            foreach (PriceSubscription subscription in m_PriceSubscriptions.Values)
                subscription.Dispose();
            m_PriceSubscriptions.Clear();
            foreach (InstrumentCatalogSubscription sub in m_InstrumentCatalogs.Values)
                sub.Dispose();
            m_InstrumentCatalogs.Clear();
        }//StopThread()
        //
        //
        //
        #endregion // private Methods


        #region Job Processing
        // *****************************************************************
        // ****                     Private Job Class                   ****
        // *****************************************************************
        private class Job
        {
            public TradingTechnologies.TTAPI.Product Product = null;
            public TradingTechnologies.TTAPI.InstrumentKey InstrumentKey;
            public TradingTechnologies.TTAPI.PriceSubscriptionSettings Settings = null;
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
                else if (job.InstrumentKey != null && job.Settings == null)
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
                    // Subscribe to an instrument
                    //
                    Instrument instrument = null;
                    InstrumentCatalogSubscription catalog = null;                               // First, find instrument catalog for this instr.
                    InstrumentLookupSubscription instrumentSub = null;                          // or find a specific instrument subscription.
                    if ( m_InstrumentLookups.TryGetValue(job.InstrumentKey,out instrumentSub) )
                    {
                        instrument = instrumentSub.Instrument;
                    }
                    else if (m_InstrumentCatalogs.TryGetValue(job.InstrumentKey.ProductKey, out catalog) )
                    {
                        catalog.Instruments.TryGetValue(job.InstrumentKey, out instrument);                        
                    }
                    else
                    {
                        Log.NewEntry(LogLevel.Minor, "{0}: I failed to find instrument key {1}.", this.Name, job.InstrumentKey.ToString());
                        return;
                    }
                    if (instrument != null)
                    {
                        // Subscribe or update pre-existing subscription.
                        PriceSubscription priceSub = null;
                        if (!m_PriceSubscriptions.TryGetValue(instrument.Key, out priceSub))
                        {   // Can't find a subscription, so create one.
                            Log.NewEntry(LogLevel.Major, "{0}: Creating new subscription for {1} with settings {2}.", this.Name, instrument.Name, job.Settings.PriceData);
                            priceSub = new PriceSubscription(instrument, Dispatcher.Current);
                            m_PriceSubscriptions.Add(instrument.Key, priceSub);                                      // add to our list of subscription objects.
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
                MistyProd.InstrumentName instrName;
                if (TTConvert.TryConvert(ttInstrument, out instrName))
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
                {   // Failed to convert TT instrument to a Misty Instrument.
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
                MistyProd.InstrumentName instrName;
                Instrument ttInstrument = eventArgs.Instrument;
                if (TTConvert.TryConvert(ttInstrument, out instrName))
                {   // Success in converting to our internal naming scheme.
                    InstrumentDetails details;
                    if (m_InstrumentDetails.TryGetValue(instrName, out details)  )
                    {   // This instrument was already added!
                        if ( ! ttInstrument.Key.Equals(details.Key) )
                            Log.NewEntry(LogLevel.Warning, "{0}: Instrument {1} found before with non-unique key {2}!", this.Name, instrName.FullName, instrName.SeriesName);
                        else
                            Log.NewEntry(LogLevel.Warning, "{0}: Instrument {1} found before and keys match! Good." , this.Name, instrName.FullName);
                    }
                    else
                    {
                        m_KeyToInstruments.Add(ttInstrument.Key, instrName);
                        m_InstrumentDetails.Add(instrName, ttInstrument.InstrumentDetails);
                        Log.NewEntry(LogLevel.Minor, "{0}: Instruments found {1} <---> {2}.", this.Name, instrName, ttInstrument.Key.ToString());
                    }
                }
                else
                {   // Failed to convert TT instrument to a Misty Instrument.
                    // This happens because either their name is too confusing to know what it is.
                    // Or, more likely, we are set to ignore the product type (options, equity, swaps).
                    Log.NewEntry(LogLevel.Warning, "{0}: Instrument creation failed for {1}.", this.Name, ttInstrument.Key.ToString());
                }
                OnInstrumentsFound(); 
            }
            else if (eventArgs.IsFinal)
            {   // Instrument was not found and TTAPI has given up on looking.
                if ( eventArgs.Instrument != null)
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
        //
        // ****             PriceSubscription_Updated()                 ****
        //
        private void PriceSubscription_Updated(object sender, FieldsUpdatedEventArgs eventArgs)
        {
            if (m_isDisposing) return;
            bool isSnapShot = (eventArgs.UpdateType == UpdateType.Snapshot);
            if (eventArgs.Error == null)
            {
                // Analyze
                FieldId[] changedFieldIds = eventArgs.Fields.GetChangedFieldIds();
                if (changedFieldIds.Length > 0)
                {
                    InstrumentKey key = eventArgs.Fields.Instrument.Key;
                    MistyProd.InstrumentName instrKey;
                    if (m_KeyToInstruments.TryGetValue(key, out instrKey))
                    {
                        m_NewEvents.Clear();
                        // Bid side
                        if (isSnapShot || changedFieldIds.Contains<FieldId>(FieldId.BestBidPrice) || changedFieldIds.Contains<FieldId>(FieldId.BestBidQuantity))
                        {
                            Price p = (Price)eventArgs.Fields.GetField(FieldId.BestBidPrice).Value;
                            Quantity q = (Quantity)eventArgs.Fields.GetField(FieldId.BestBidQuantity).Value;
                            if (p.IsValid && q.IsValid)
                            {
                                int qty = q.ToInt();
                                if (qty > 0)
                                {
                                    Misty.Lib.BookHubs.MarketUpdateEventArgs e = new Misty.Lib.BookHubs.MarketUpdateEventArgs();
                                    //e.Instrument = instrument;
                                    e.Name = instrKey;
                                    e.Price = p.ToDouble();
                                    e.Qty = q.ToInt();
                                    e.Side = QTMath.BidSide;
                                    m_NewEvents.Add(e);
                                }
                            }
                        }
                        // Ask side
                        if (isSnapShot || changedFieldIds.Contains<FieldId>(FieldId.BestAskPrice) || changedFieldIds.Contains<FieldId>(FieldId.BestAskQuantity))
                        {
                            Price p = (Price)eventArgs.Fields.GetField(FieldId.BestAskPrice).Value;
                            Quantity q = (Quantity)eventArgs.Fields.GetField(FieldId.BestAskQuantity).Value;
                            if (p.IsValid && q.IsValid)
                            {
                                int qty = q.ToInt();
                                if (qty > 0)
                                {
                                    Misty.Lib.BookHubs.MarketUpdateEventArgs e = new Misty.Lib.BookHubs.MarketUpdateEventArgs();
                                    //e.Instrument = instrument;
                                    e.Name = instrKey;
                                    e.Price = p.ToDouble();
                                    e.Qty = q.ToInt();
                                    e.Side = QTMath.AskSide;
                                    m_NewEvents.Add(e);
                                }
                            }
                        }
                        // Last
                        if (isSnapShot || changedFieldIds.Contains<FieldId>(FieldId.LastTradedPrice) || changedFieldIds.Contains<FieldId>(FieldId.LastTradedQuantity))
                        {
                            Price p = (Price)eventArgs.Fields.GetField(FieldId.LastTradedPrice).Value;
                            Quantity q = (Quantity)eventArgs.Fields.GetField(FieldId.LastTradedQuantity).Value;
                            if (p == 0)
                            {
                                //int nn = 0;
                            }
                            if (p.IsValid && q.IsValid)
                            {

                                int qty = q.ToInt();
                                if (qty > 0)
                                {
                                    Misty.Lib.BookHubs.MarketUpdateEventArgs e = new Misty.Lib.BookHubs.MarketUpdateEventArgs();
                                    //e.Instrument = instrument;
                                    e.Name = instrKey;
                                    e.Price = p.ToDouble();
                                    e.Qty = q.ToInt();
                                    e.Side = QTMath.LastSide;
                                    m_NewEvents.Add(e);
                                }
                            }
                        }
                        // Total Volume
                        /*
                        if (isSnapShot || changedFieldIds.Contains<FieldId>(FieldId.TotalTradedQuantity))
                        {
                            object f = eventArgs.Fields.GetField(FieldId.TotalTradedQuantity).Value;
                            Quantity q = (Quantity)eventArgs.Fields.GetField(FieldId.LastTradedQuantity).Value;
                            if (q.IsValid)
                            {                            
                                Misty.Lib.BookHubs.MarketUpdateEventArgs e = new Misty.Lib.BookHubs.MarketUpdateEventArgs();
                                e.Instrument = instrument;
                                e.Qty = q.ToInt();
                                //e.Side = QTMath.LastSide;
                                //m_NewEvents.Add(e);                            
                            }
                        }
                        if (isSnapShot || changedFieldIds.Contains<FieldId>(FieldId.HighPrice))
                        {
                            object f = eventArgs.Fields.GetField(FieldId.HighPrice).Value;
                        }
                        if (isSnapShot || changedFieldIds.Contains<FieldId>(FieldId.LowPrice))
                        {
                            object f = eventArgs.Fields.GetField(FieldId.LowPrice).Value;
                        }
                        */
                        /*
                        if (changedFieldIds.Contains<FieldId>(FieldId.OpenPrice))
                        {
                            object f = eventArgs.Fields.GetField(FieldId.OpenPrice).Value;
                        }
                        if (isSnapShot || changedFieldIds.Contains<FieldId>(FieldId.SettlementPrice))
                        {
                            object f = eventArgs.Fields.GetField(FieldId.SettlementPrice).Value;
                        }
                        */
                        // Series Status
                        if (changedFieldIds.Contains<FieldId>(FieldId.SeriesStatus))
                        {
                            TradingStatus status = (TradingStatus)eventArgs.Fields.GetField(FieldId.SeriesStatus).Value;
                            Log.NewEntry(LogLevel.Minor, "PriceListener: SeriesStatus change {0} is {1}.", instrKey, status.ToString());
                            Misty.Lib.BookHubs.MarketStatusEventArgs e = new Misty.Lib.BookHubs.MarketStatusEventArgs();
                            //e.Instrument = instrument;
                            e.InstrumentName = instrKey;
                            if (status == TradingStatus.Trading)
                                e.Status = Misty.Lib.BookHubs.MarketStatus.Trading;
                            else if (status == TradingStatus.Closed || status == TradingStatus.ClosingAuction || status == TradingStatus.Expired ||
                                status == TradingStatus.NotTradable || status == TradingStatus.PostTrading)
                                e.Status = Misty.Lib.BookHubs.MarketStatus.NotTrading;
                            else
                                e.Status = Misty.Lib.BookHubs.MarketStatus.Special;
                            m_NewEvents.Add(e);
                        }
                        // Session rollover
                        if (changedFieldIds.Contains<FieldId>(FieldId.SessionRollover))
                        {
                            TradingStatus status = (TradingStatus)eventArgs.Fields.GetField(FieldId.SeriesStatus).Value;
                            Log.NewEntry(LogLevel.Minor, "PriceListener: SessionRollover change {0} is {1}.", instrKey, status.ToString());
                            Misty.Lib.BookHubs.MarketStatusEventArgs e = new Misty.Lib.BookHubs.MarketStatusEventArgs();
                            //e.Instrument = instrument;
                            e.InstrumentName = instrKey;
                            if (status == TradingStatus.Trading)
                                e.Status = Misty.Lib.BookHubs.MarketStatus.Trading;
                            else if (status == TradingStatus.Closed || status == TradingStatus.ClosingAuction || status == TradingStatus.Expired ||
                                status == TradingStatus.NotTradable || status == TradingStatus.PostTrading)
                                e.Status = Misty.Lib.BookHubs.MarketStatus.NotTrading;
                            else
                                e.Status = Misty.Lib.BookHubs.MarketStatus.Special;
                            m_NewEvents.Add(e);
                        }

                        //
                        // Fire events
                        //
                        ProcessPriceChangeEvents(ref m_NewEvents);
                    }// if instrument not found for ttKey.
                    else
                        Log.NewEntry(LogLevel.Warning, "{0}: Failed to find instrument for TTKey {1}.", this.Name, key);                   

                }
            }
            else
                Log.NewEntry(LogLevel.Warning, "{0}: Error in price subscription {1}.", this.Name, eventArgs.Error.Message);
        }//PriceSubscription()
        //
        public ProcessPriceChangeDelegate ProcessPriceChangeEvents;
        public delegate void ProcessPriceChangeDelegate(ref List<EventArgs> newEvents);
        //
        //
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
                foreach (MistyProd.InstrumentName name in m_InstrumentDetails.Keys)
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
            //public List<Misty.Lib.Products.InstrumentBase> Instruments = new List<Misty.Lib.Products.InstrumentBase>();
            //public List<InstrumentDetails> InstrumentDetails = new List<InstrumentDetails>();            
            public Dictionary<MistyProd.InstrumentName, InstrumentDetails> InstrumentDetails = new Dictionary<MistyProd.InstrumentName, InstrumentDetails>();            
        }
        //
        //
        //
        #endregion // my events

    }
}
