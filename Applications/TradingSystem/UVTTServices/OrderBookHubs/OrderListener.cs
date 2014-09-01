using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;

namespace UV.TTServices.OrderBookHubs
{
    using UV.Lib.Hubs;
    using UV.TTServices;
    //using UV.Lib.IO.Xml;
    using UV.Lib.Utilities;
    using UVOrder = UV.Lib.OrderBookHubs.Order;
    using UVInstrument = UV.Lib.Products.InstrumentName;
    using UVFill = UV.Lib.Fills.Fill;
    using UVFillEventArgs = UV.Lib.Fills.FillEventArgs;
    using OrderRequestType = UV.Lib.OrderBookHubs.OrderRequestType;


    using TradingTechnologies.TTAPI;
    using TradingTechnologies.TTAPI.Tradebook;

    /// <summary>
    /// 
    /// </summary>
    public class OrderListener : IDisposable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // 
        // My parent information
        //
        private OrderBookHubTT m_ParentHub = null;
        private LogHub Log = null;                                  // Log that I write to, usually owned by parent hub.
        private string m_Name = string.Empty;                       // Useful identifier this object (for log entries).
        private bool m_isDisposed = false;

        //
        // Internal Lookup tables.
        //
        private Dictionary<string, int> m_MapTT2UV = new Dictionary<string, int>();
        private Dictionary<int, string> m_MapUV2TT = new Dictionary<int, string>();
        private Dictionary<string, Order> m_TTOrders = new Dictionary<string, Order>();

        //
        // External Services
        //
        private TTApiService m_TTService = null;
        private WorkerDispatcher m_Dispatcher = null;               // TT's WorkerDispatcher
        private TradeSubscription m_GlobalTradeSubscription = null;     // Global trade subscription - some applications subscribe to ALL trades.

        // Filtered subscriptions.
        private List<InstrumentKey> m_InstrumentsRequested = new List<InstrumentKey>();         // these are keys we asked for instruments, but haven't received callbacks yet.
        private Dictionary<InstrumentKey, Instrument> m_TTInstrKeyToTTInstr = new Dictionary<InstrumentKey, Instrument>();
        private Dictionary<InstrumentKey, UVInstrument> m_TTInstrKeyToUVInstr = new Dictionary<InstrumentKey, UVInstrument>();
        private List<TradeSubscription> m_TradeSubscriptions = new List<TradeSubscription>();   // place to store trade subscriptions to dispose of later.
        private Dictionary<InstrumentKey, OrderFeed> m_DefaultOrderFeeds = new Dictionary<InstrumentKey, OrderFeed>();

        //
        // Job queue objects
        //
        private Queue<Job> m_WorkQueue = new Queue<Job>();                      // Completely private, owned by Listener thread.
        private ConcurrentQueue<Job> m_InQueue = new ConcurrentQueue<Job>();    // In-Out queue, Jobs pushed from outsider threads.

        //        
        // pending collections
        //
        private List<string> m_TTOrderKeysPendingDelete = new List<string>();   // if we want to delete an order prior to getting the order object from tt, we place the key here for later processing.
        private ConcurrentDictionary<int, UVOrder> m_PendingModifyOrders = new ConcurrentDictionary<int, UVOrder>(); // if we aren't able to modify an order, we add it to the list of pending here.
        private Dictionary<InstrumentKey, List<UVOrder>> m_FoundOrdersPendingInstruments = new Dictionary<InstrumentKey, List<UVOrder>>();
        // Recylce Spaces
        //
        private RecycleFactory<Job> m_JobRecycleFactory = new RecycleFactory<Job>(100);
        private List<UVOrder> m_UvOrderWorkspace = new List<UVOrder>();
        #endregion// members

        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        /// <summary>
        /// Immediately starts the thread.  A TT Session must already exist when this is created.
        /// </summary>
        /// <param name="listenerName"></param>
        /// <param name="aLog"></param>
        public OrderListener(string listenerName, OrderBookHubTT parentHub)
        {
            m_ParentHub = parentHub;
            this.Log = m_ParentHub.Log;
            this.m_Name = listenerName;
            m_TTService = TTApiService.GetInstance();
        }
        //
        //       
        #endregion//Constructors

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
        }// Start()
        //
        //
        // ****         SubmitOrder()       ****
        /// <summary>
        /// Request to create TT OrderProfile and submit order.
        /// Called by any thread.
        /// </summary>
        public void SubmitOrder(InstrumentKey instrKey, UVOrder order)
        {
            if (m_isDisposed) return;
            Job job = m_JobRecycleFactory.Get();
            job.JobType = JobTypes.SendOrder;
            job.InstrumentKey = instrKey;                 // Add desired key
            job.Data = order;
            m_InQueue.Enqueue(job);
            m_Dispatcher.BeginInvoke(new Action(ProcessAJob));
            m_Dispatcher.Run();
        }//SubscribeTo()  
        //
        //
        // ****             DeleteOrder()         ****
        /// <summary>
        /// Request to Delete a TT Order in the market.
        /// </summary>
        /// <param name="order"></param>
        public void DeleteOrder(UVOrder order)
        {
            if (m_isDisposed) return;
            Job job = m_JobRecycleFactory.Get();
            job.JobType = JobTypes.DeleteOrder;
            job.Data = order;
            m_InQueue.Enqueue(job);
            m_Dispatcher.BeginInvoke(new Action(ProcessAJob));
            m_Dispatcher.Run();
        }
        //
        //
        // ****           DeletePendingOrder()         ****
        /// <summary>
        /// Request to delete a TT order that we were waiting on confirmation for
        /// </summary>
        /// <param name="ttOrder"></param>
        public void DeletePendingOrder(Order ttOrder)
        {
            if (m_isDisposed) return;
            Job job = m_JobRecycleFactory.Get();
            job.JobType = JobTypes.DeletePendingOrder;
            job.Data = ttOrder;
            m_InQueue.Enqueue(job);
            m_Dispatcher.BeginInvoke(new Action(ProcessAJob));
            m_Dispatcher.Run();
        }
        //
        //
        //
        //****          Modify Order()           ****
        /// <summary>
        /// Request to change an order. 
        /// If this order has an outstanding change, it will queue until 
        /// we can process this one.
        /// </summary>
        /// <param name="orderToModify"></param>
        public void ModifyOrder(UVOrder orderToModify)
        {
            if (m_isDisposed) return;
            if (m_PendingModifyOrders.ContainsKey(orderToModify.Id))
            { // save pending changes
                m_PendingModifyOrders[orderToModify.Id] = orderToModify;
                return;
            }
            Job job = m_JobRecycleFactory.Get();
            job.JobType = JobTypes.ModifyOrder;
            job.Data = orderToModify;
            m_InQueue.Enqueue(job);
            m_Dispatcher.BeginInvoke(new Action(ProcessAJob));
            m_Dispatcher.Run();
        }
        // ****         SubscribeTo()       ****
        /// <summary>
        /// Requests that we obtain an Instrument object from TTAPI allowing us
        /// to submit orders for the instrument and we also 
        /// Called by external thread.
        /// </summary>
        public void SubscribeToInstrument(InstrumentKey instrKey)
        {
            if (m_isDisposed) return;
            Job job = m_JobRecycleFactory.Get();
            job.JobType = JobTypes.SubscribeToInstrument;
            job.InstrumentKey = instrKey;                 // Add desired key
            m_InQueue.Enqueue(job);
            m_Dispatcher.BeginInvoke(new Action(ProcessAJob));
            m_Dispatcher.Run();
        }//SubscribeTo()  
        //
        //
        // ****         SubscribeTo()       ****
        /// <summary>
        /// Requests that we listen to ALL order events for this user login.
        /// This will NOT allow us to submit orders, but does allow for us to listen 
        /// to all orders/trades etc.
        /// Called by external thread.
        /// </summary>
        public void SubscribeToAll()
        {
            if (m_isDisposed) return;
            Job job = m_JobRecycleFactory.Get();
            job.JobType = JobTypes.SubscribeToAllInstruments;
            m_InQueue.Enqueue(job);
            m_Dispatcher.BeginInvoke(new Action(ProcessAJob));
            m_Dispatcher.Run();
        }//SubscribeTo()  
        //
        //
        //
        #endregion//Public Methods

        #region IDisposable implementation
        // *********************************************
        // ****             IDisposable             ****
        // *********************************************
        //
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        //
        //
        protected virtual void Dispose(bool disposing)
        {
            if (m_isDisposed)
                return;
            m_isDisposed = true;                    // set this now, or at end of this method?

            // Release IDisposable objects.
            if (disposing)
            {   // Here we Dipose of objects we are holding that
                // are IDisposable.

            }

            // Tell our thread to shutdown.
            m_Dispatcher.BeginInvoke(new Action(StopThread));       // TT objects will be disposed of in StopThread method.
            try
            {
                m_Dispatcher.Run();                                 // TODO: Fix that this sometimes this can throw an exception during shutdown
                // Here, we release all other objects.

            }
            catch (Exception)
            {
            }


        }// Dispose().
        //
        #endregion//IDisposible

        #region Private Startup/Shutdown Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        /// <summary>
        /// This is the first method called by the FillListener's worker thread.
        /// To capture itself permanantly, it will call its own dispatcher and complete its
        /// initialization.
        /// </summary>
        private void InitializeThread()
        {
            m_Dispatcher = TradingTechnologies.TTAPI.Dispatcher.AttachWorkerDispatcher();
            m_Dispatcher.BeginInvoke(new Action(InitComplete));
            m_Dispatcher.Run();                                                 // this tells thread to do this and wait.
        }
        /// <summary>
        /// Called via my thread dispatcher to create the TradeSubscription object and start it.
        /// After which, the thread will sleep until further dispatcher actions are invoked.
        /// </summary>
        private void InitComplete()
        {
            this.Log.NewEntry(LogLevel.Major, "{0}: Trade listener started.", m_Name);

        }//InitComplete()
        /// <summary>
        /// Called by the Dispose() function.
        /// </summary>
        private void StopThread()
        {
            if (m_Dispatcher != null && (!m_Dispatcher.IsDisposed))
                m_Dispatcher.Dispose();
            // Shutdown the global tradesubscription.
            if (m_GlobalTradeSubscription != null)
            {
                Log.NewEntry(LogLevel.Minor, "{0}: Shutting down global trade subscription.", m_Name);
                m_GlobalTradeSubscription.OrderFilled -= new EventHandler<TradingTechnologies.TTAPI.OrderFilledEventArgs>(TT_OrderFilled);
                m_GlobalTradeSubscription.OrderAdded -= new EventHandler<TradingTechnologies.TTAPI.OrderAddedEventArgs>(TT_OrderAdded);
                m_GlobalTradeSubscription.OrderDeleted -= new EventHandler<TradingTechnologies.TTAPI.OrderDeletedEventArgs>(TT_OrderDeleted);
                m_GlobalTradeSubscription.OrderUpdated -= new EventHandler<TradingTechnologies.TTAPI.OrderUpdatedEventArgs>(TT_OrderUpdated);
                m_GlobalTradeSubscription.OrderBookDownload -= new EventHandler<TradingTechnologies.TTAPI.OrderBookDownloadEventArgs>(TT_OrderBookDownload);
                m_GlobalTradeSubscription.OrderRejected -= new EventHandler<OrderRejectedEventArgs>(TT_OrderRejected);
                m_GlobalTradeSubscription.Dispose();
            }
            m_GlobalTradeSubscription = null;

            // Shutdown filtered trade subscriptions
            Log.BeginEntry(LogLevel.Minor, "{0}: Shutting down {1} filtered trade subscriptions: ", m_Name, m_TradeSubscriptions.Count);
            foreach (TradeSubscription ts in m_TradeSubscriptions)
            {
                //Log.AppendEntry("[{0}] ",ts.Orders.)      // log something here.
                ts.OrderFilled -= new EventHandler<TradingTechnologies.TTAPI.OrderFilledEventArgs>(TT_OrderFilled);
                ts.OrderAdded -= new EventHandler<TradingTechnologies.TTAPI.OrderAddedEventArgs>(TT_OrderAdded);
                ts.OrderDeleted -= new EventHandler<TradingTechnologies.TTAPI.OrderDeletedEventArgs>(TT_OrderDeleted);
                ts.OrderUpdated -= new EventHandler<TradingTechnologies.TTAPI.OrderUpdatedEventArgs>(TT_OrderUpdated);
                ts.OrderBookDownload -= new EventHandler<TradingTechnologies.TTAPI.OrderBookDownloadEventArgs>(TT_OrderBookDownload);
                ts.OrderRejected -= new EventHandler<OrderRejectedEventArgs>(TT_OrderRejected);
                ts.Dispose();
            }
            m_TradeSubscriptions.Clear();
            Log.EndEntry();

        }//StopThread()
        //
        //
        //
        //
        //
        //
        #endregion // private methods

        #region Job Processing Class
        // *****************************************************************
        // ****                     Private Job Class                   ****
        // *****************************************************************
        private class Job
        {
            public JobTypes JobType = JobTypes.None;
            // Data for job
            public InstrumentKey InstrumentKey;
            public object Data = null;

        }//Job
        private enum JobTypes
        {
            None = 0,
            SubscribeToAllInstruments,      // Subscribes to all Order events for all instruments.  User only when NO orders will be sent.
            SubscribeToInstrument,          // get an instrument object from TT.  Required for order submission.
            SendOrder,
            DeleteOrder,
            DeletePendingOrder,             // if we have attempted to delete the order, but TT doens't know about it yet we call this job type.
            ModifyOrder,
            CancelReplace

        }
        //
        //
        //
        //
        // *****************************************************************
        // ****             Job Processing Methods                      ****
        // *****************************************************************
        /// <summary>
        /// Called by the thread's dispatcher after an outside thread has 
        /// pushed a new Job.
        /// </summary>
        private void ProcessAJob()
        {
            if (m_isDisposed)
                return;
            Job aJob;
            while (m_InQueue.TryDequeue(out aJob))              // NET 4.5 concurrent queue. No locking needed.
                m_WorkQueue.Enqueue(aJob);                      // push these requested jobs onto my private queue to process them.

            //
            // Process the jobs now.
            //
            while (m_WorkQueue.Count > 0)
            {
                Job job = m_WorkQueue.Dequeue();
                switch (job.JobType)
                {
                    case JobTypes.SubscribeToAllInstruments:
                        SubscribeToAllInstruments();
                        break;
                    case JobTypes.SubscribeToInstrument:
                        SubscribeToInstrument(job);
                        break;
                    case JobTypes.SendOrder:
                        ProcessSendOrder(job);
                        break;
                    case JobTypes.DeleteOrder:
                        ProcessDeleteOrder(job);
                        break;
                    case JobTypes.DeletePendingOrder:
                        ProcessPendingDeletion(job);
                        break;
                    case JobTypes.ModifyOrder:
                        ProcessModifyOrder(job);
                        break; 
                    case JobTypes.CancelReplace:
                        ProcessCancelReplace(job);
                        break;
                    default:
                        break;
                }
                m_JobRecycleFactory.Recycle(job);       // recylcle all jobs as we finish up with them
            }//wend Job in WorkQueue.
        }//ProcessAJob()
        //
        //
        //
        // *************************************************************
        // ****                 ProcessSendOrder()                  ****
        // *************************************************************
        //
        private void ProcessSendOrder(Job job)
        {
            bool isSuccess = true;
            // Get Instrument Key.
            if (job.InstrumentKey == default(InstrumentKey))
            {
                Log.NewEntry(LogLevel.Warning, "{0}: SendOrder failed.  No InstrumentKey provided.", m_Name);
                isSuccess = false;
            }
            InstrumentKey key = job.InstrumentKey;

            // Get Order Feed.
            OrderFeed orderFeed = null;
            if (isSuccess && !m_DefaultOrderFeeds.TryGetValue(key, out orderFeed) || !orderFeed.IsTradingEnabled)
            {
                Log.NewEntry(LogLevel.Warning, "{0}: SendOrder failed.  No enabled order feed.", m_Name);
                isSuccess = false;
                // TODO: in future, we can search thru all order feeds.
                // TODO: allow the OrderTT object to set the OrderFeed to submit to.
            }

            // Get the Instrument
            Instrument instrument = null;
            if (isSuccess && !m_TTInstrKeyToTTInstr.TryGetValue(key, out instrument))
            {
                Log.NewEntry(LogLevel.Warning, "{0}: SendOrder failed.  Not instrument available for {1} {2}.", m_Name, key.ProductKey, key.SeriesKey);
                isSuccess = false;
            }

            UVOrder order = (UVOrder)job.Data;
            if (!isSuccess)
            { // if any of this failed!
                ProcessDeleteOrder(job);
                return;
            }
            //
            // Send order now
            //
            OrderProfile profile = new OrderProfile(orderFeed, instrument);

            profile.AccountType = AccountType.None;
            profile.AccountName = "Acct123";// order.AccountName;

            profile.OrderType = TTConvert.ToOrderType(order.OrderType);
            profile.BuySell = TTConvert.ToBuySell(order.OriginalQtyPending);
            profile.QuantityToWork = Quantity.FromInt(instrument, Math.Abs(order.OriginalQtyPending));
            profile.LimitPrice = Price.FromDouble(instrument, order.PricePending);
            profile.TimeInForce = new TimeInForce(TTConvert.ToTimeInForce(order.OrderTIF));
            string siteOrderKey = profile.SiteOrderKey;

            if (instrument.Session.SendOrder(profile))
            {   // Success
                m_MapTT2UV[siteOrderKey] = order.Id;
                m_MapUV2TT[order.Id] = siteOrderKey;
                Log.NewEntry(LogLevel.Warning, "{0}: Order sent {1}", m_Name, order);
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
        /// Called by dispatcher thread to process the deletion of a TT order.
        /// If we haven't recieved the order back from TT, the deletion will
        /// be pended until we recieve the order.
        /// </summary>
        /// <param name="job"></param>
        private void ProcessDeleteOrder(Job job)
        {
            UVOrder uvOrder = (UVOrder)job.Data;
            string ttSiteOrderKey;
            if (m_MapUV2TT.TryGetValue(uvOrder.Id, out ttSiteOrderKey))
            { // we found a matching tt site key
                Order ttOrder;
                if (m_TTOrders.TryGetValue(ttSiteOrderKey, out ttOrder))
                { // we found the order from that site key
                    OrderProfileBase profile = ttOrder.GetOrderProfile();
                    profile.Action = OrderAction.Delete;
                    if (profile.Session.SendOrder(profile))
                    {
                        uvOrder.OrderStatePending = UV.Lib.OrderBookHubs.OrderState.Dead;                   // set out pending flag to dead while we await the confirm
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
                    Log.NewEntry(LogLevel.Error, "Could not find correspsonding TT Order for UV Order {0} with TT Site Key {1} storing for later processing", uvOrder, ttSiteOrderKey);
                    m_TTOrderKeysPendingDelete.Add(ttSiteOrderKey);
                    uvOrder.OrderStatePending = UV.Lib.OrderBookHubs.OrderState.Dead;
                }
            }
            else
            { // this mean somehow this order never got correctly submitted to TT.  Since this order is only internal we can deal with it now.
                Log.NewEntry(LogLevel.Error, "Could not find correspsonding TT Site Key for UV Order {0}", uvOrder);
                m_ParentHub.TryProcessOrderUpdateRequest(
                        m_ParentHub.m_OrderRequests.Get(OrderRequestType.DeleteConfirm, uvOrder.Instrument , uvOrder.Id, uvOrder.Side));
            }
        }
        //
        //
        // *************************************************************
        // ****               ProcessPendingDeletion()              ****
        // *************************************************************
        /// <summary>
        /// Called by the dispatcher thread to delete an order that 
        /// was previously requested to be deleted but was pended since we hadn't
        /// gotten the order back from TT.
        /// </summary>
        /// <param name="job"></param>
        private void ProcessPendingDeletion(Job job)
        {
            Order ttOrder = (Order)job.Data;
            OrderProfileBase profile = ttOrder.GetOrderProfile();
            profile.Action = OrderAction.Delete;
            if (profile.Session.SendOrder(profile))
            {
                Log.NewEntry(LogLevel.Minor, "TT Order {0} was found in pending cancellation list cancelling now", ttOrder.SiteOrderKey);
            }
            else
                Log.NewEntry(LogLevel.Error, "TT Order {0} was found in pending cancellation list and cancellation failed.", ttOrder.SiteOrderKey);
        }
        //
        //
        //
        // *************************************************************
        // ****                 ProcessModifyOrder()                ****
        // *************************************************************
        private void ProcessModifyOrder(Job job)
        {
            UVOrder orderToModify = (UVOrder)job.Data;
            string ttSiteOrderKey;
            if (m_MapUV2TT.TryGetValue(orderToModify.Id, out ttSiteOrderKey))
            { // we found a matching tt site key
                Order ttOrder;
                if (m_TTOrders.TryGetValue(ttSiteOrderKey, out ttOrder))
                { // we found the order from that site key
                    OrderProfileBase profile = ttOrder.GetOrderProfile();
                    profile.Action = OrderAction.Change;
                    if (orderToModify.IPriceConfirmed != orderToModify.IPricePending)
                        profile.LimitPrice = Price.FromDouble(ttOrder.InstrumentDetails, orderToModify.PricePending);
                    if (orderToModify.OriginalQtyConfirmed != orderToModify.OriginalQtyPending)
                        profile.QuantityToWork = Quantity.FromInt(profile, Math.Abs(orderToModify.OriginalQtyPending));
                    if (profile.Session.SendOrder(profile))
                        Log.NewEntry(LogLevel.Minor, "TT Order {0} was Modified Succesffully UV Order : {1}", ttSiteOrderKey, orderToModify);
                    else
                    {
                        Log.NewEntry(LogLevel.Error, "TT Order {0} failed to allow modification {1} {2} : Adding to Pending Changes Queue.", ttSiteOrderKey, profile.RoutingStatus.Message, profile.RoutingStatus.State);
                        m_PendingModifyOrders[orderToModify.Id] = orderToModify;
                    }
                }
            }
        }
        //
        //
        // *************************************************************
        // ****                    ProcessCancelReplace()           ****
        // *************************************************************
        private void ProcessCancelReplace(Job job)
        { // TODO FIX ME
           
        }
        // *************************************************************
        // ****             SubscribeToAllInstruments()             ****
        // *************************************************************
        private void SubscribeToAllInstruments()
        {

            if (m_GlobalTradeSubscription == null)
            {   // User wants to subscribe to all order events (for all instruments).
                Log.NewEntry(LogLevel.Minor, "{0}: Global trade subscription started.", m_Name);

                m_GlobalTradeSubscription = new TradeSubscription(m_TTService.session, m_Dispatcher);
                m_GlobalTradeSubscription.OrderFilled += new EventHandler<TradingTechnologies.TTAPI.OrderFilledEventArgs>(TT_OrderFilled);
                m_GlobalTradeSubscription.OrderAdded += new EventHandler<TradingTechnologies.TTAPI.OrderAddedEventArgs>(TT_OrderAdded);
                m_GlobalTradeSubscription.OrderUpdated += new EventHandler<TradingTechnologies.TTAPI.OrderUpdatedEventArgs>(TT_OrderUpdated);
                m_GlobalTradeSubscription.OrderDeleted += new EventHandler<TradingTechnologies.TTAPI.OrderDeletedEventArgs>(TT_OrderDeleted);
                m_GlobalTradeSubscription.OrderBookDownload += new EventHandler<TradingTechnologies.TTAPI.OrderBookDownloadEventArgs>(TT_OrderBookDownload);
                m_GlobalTradeSubscription.OrderRejected += new EventHandler<OrderRejectedEventArgs>(TT_OrderRejected);

                m_GlobalTradeSubscription.Start();
            }
            else
            {   // This request should only be done once!
                Log.NewEntry(LogLevel.Minor, "{0}: Request for another global trade subscription will be ignored!", m_Name);
            }
        }//SubscribeToAllInstruments()
        //
        //
        //
        //
        // *************************************************************
        // ****         ProcessJobSubscribeToInstrument             ****
        // *************************************************************
        private void SubscribeToInstrument(Job job)
        {
            InstrumentKey key = job.InstrumentKey;
            if (key == default(InstrumentKey))
            {
                return;
            }
            //
            // Request the instrument (for order submission)
            //
            if (m_InstrumentsRequested.Contains(key) || m_TTInstrKeyToTTInstr.ContainsKey(key))
            {   // Only get instruments once!
                Log.NewEntry(LogLevel.Warning, "{0}: Duplicate request for instrument {1} {2} will be ignored.", m_Name, key.ProductKey.Name, key.SeriesKey);
            }
            else
            {
                m_InstrumentsRequested.Add(key);                // keep track of this request.
                InstrumentLookupSubscription instrSubscription = new InstrumentLookupSubscription(m_TTService.session, m_Dispatcher, key);
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
            ts.OrderBookDownload += new EventHandler<TradingTechnologies.TTAPI.OrderBookDownloadEventArgs>(TT_OrderBookDownload);
            ts.OrderRejected += new EventHandler<OrderRejectedEventArgs>(TT_OrderRejected);

            // now add desired filters...
            TradeSubscriptionInstrumentFilter tsif = new TradeSubscriptionInstrumentFilter(m_TTService.session, key, false, "InstrFilter");
            ts.SetFilter(tsif);

            ts.Start();

        }// ProcessJob_SubscribeToInstrument
        //
        // *************************************************************
        // *************************************************************
        //
        //
        //
        //
        #endregion// private methods

        #region TT Event Handlers - Mike These Need to Be Cleaned Up Still
        // *********************************************************************************
        // ****                             Events From TT                              ****
        // *********************************************************************************
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
            m_TTOrders[siteKey] = order;            // store this order regardless of whos it is.
            int uvOrderId = -1;
            if (m_MapTT2UV.TryGetValue(siteKey, out uvOrderId))
            { // we found our order id
                int mktSide = TTConvert.ToMarketSide(eventArgs.Fill.BuySell);
                int mktSign = UV.Lib.Utilities.QTMath.MktSideToMktSign(mktSide);
                UVFill aFill = UVFill.Create(
                    (mktSign * eventArgs.Fill.Quantity),                                   // create new fill
                    eventArgs.Fill.MatchPrice.ToDouble(),
                    Log.GetTime(), eventArgs.Fill.TransactionDateTime);
                UVInstrument uvInstr;
                if (m_TTInstrKeyToUVInstr.TryGetValue(eventArgs.Fill.InstrumentKey, out uvInstr))
                { // we found the uv instrument 
                    Log.NewEntry(LogLevel.Minor, "TT Order {4} Filled : {0} {1} {2} @ {3}",
                                    uvInstr,                            // 0
                                    eventArgs.Fill.BuySell,             // 1
                                    eventArgs.Fill.Quantity,            // 2
                                    eventArgs.Fill.MatchPrice,          // 3  
                                    eventArgs.NewOrder.SiteOrderKey);   // 4    
                    UVFillEventArgs fillEvent = new UVFillEventArgs(aFill, uvOrderId, uvInstr,
                                                                    eventArgs.FillType == FillType.Full);       // create new fill event arg
                    m_ParentHub.TryProcessOrderUpdateRequest(
                        m_ParentHub.m_OrderRequests.Get(OrderRequestType.FillConfirm, uvInstr, uvOrderId, mktSide, fillEvent));                // send it to be processed.    
                }
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
            Order order = eventArgs.Order;
            string siteKey = order.SiteOrderKey;
            m_TTOrders[siteKey] = order;                        // store this order regardless of whos it is.
            int uvOrderId = -1;
            if (m_TTOrderKeysPendingDelete.Contains(siteKey))
            { // this order is pending deletion!
                m_TTOrderKeysPendingDelete.Remove(siteKey);     // remove from pending list
                DeletePendingOrder(order);                      // delete order 
            }
            if (m_MapTT2UV.TryGetValue(siteKey, out uvOrderId))
            { // we found our order id
                UVInstrument uvInstr;
                if (m_TTInstrKeyToUVInstr.TryGetValue(eventArgs.Order.InstrumentKey, out uvInstr))
                { // we found the uv instrumnet 
                    UVOrder convertedUVOrder;
                    Log.NewEntry(LogLevel.Minor, "TT Order {4} Added : {0} {1} {2} @ {3}",
                                    uvInstr,                            // 0
                                    eventArgs.Order.BuySell,            // 1
                                    eventArgs.Order.OrderQuantity,      // 2
                                    eventArgs.Order.LimitPrice,         // 3
                                    eventArgs.Order.SiteOrderKey);      // 4
                    if (TTConvert.TryConvert(order, m_ParentHub.m_OrderRecycleFactory, out convertedUVOrder))       // translate to one of our orders
                    {
                        convertedUVOrder.Instrument = uvInstr;
                        convertedUVOrder.Id = uvOrderId;
                        m_ParentHub.TryProcessOrderUpdateRequest(
                            m_ParentHub.m_OrderRequests.Get(OrderRequestType.AddConfirm, uvInstr, uvOrderId, convertedUVOrder.Side, convertedUVOrder));                             // send the order to be processed
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
            m_TTOrders[siteKey] = order;                // store this order regardless of whose it is.
            int uvOrderId = -1;
            if (m_MapTT2UV.TryGetValue(siteKey, out uvOrderId))
            { // we found our order id
                int mktSide = TTConvert.ToMarketSide(eventArgs.OldOrder.BuySell);
                UVInstrument uvInstr;
                if (m_TTInstrKeyToUVInstr.TryGetValue(eventArgs.OldOrder.InstrumentKey, out uvInstr))
                { // we found the uv instrumnet 
                    Log.NewEntry(LogLevel.Minor, "TT Order {0} Deleted : {1} {2}",
                                    eventArgs.OldOrder.SiteOrderKey,        // 0
                                    uvInstr,                                // 1
                                    eventArgs.Message);                     // 2
                    m_ParentHub.TryProcessOrderUpdateRequest(
                        m_ParentHub.m_OrderRequests.Get(OrderRequestType.DeleteConfirm, uvInstr, uvOrderId, mktSide));  // send the order id to be processed
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
            Order order = eventArgs.Order;
            string siteKey = order.SiteOrderKey;
            if (order.Action == OrderAction.Add || order.Action == OrderAction.Change)
            { // rejected order for add.
                m_TTOrders[siteKey] = order;                // store this order regardless of whos it is.
                int uvOrderId = -1;
                if (m_MapTT2UV.TryGetValue(siteKey, out uvOrderId))
                { // we found our order id
                    UVInstrument uvInstr;
                    if (m_TTInstrKeyToUVInstr.TryGetValue(eventArgs.Order.InstrumentKey, out uvInstr))
                    { // we found the uv instrumnet 
                        Log.NewEntry(LogLevel.Minor, "TT Order {2} Rejected {3}: {0} {1} ",
                                        uvInstr,                            // 0
                                        eventArgs.Message,                  // 1
                                        order.SiteOrderKey,                 // 2
                                        order.Action);                      // 3

                        if (order.WorkingQuantity == 0 && order.Action == OrderAction.Change)
                        { // order was filled prior to us being able to change it.
                            UVOrder uvOrderToReprocess;
                            m_PendingModifyOrders.TryRemove(uvOrderId, out uvOrderToReprocess);
                        }
                        int mktSide = TTConvert.ToMarketSide(eventArgs.Order.BuySell);
                        if(order.Action == OrderAction.Add)
                            m_ParentHub.TryProcessOrderUpdateRequest(
                            m_ParentHub.m_OrderRequests.Get(OrderRequestType.AddReject, uvInstr, uvOrderId, mktSide));           // send the order id to be processed
                        else if( order.Action == OrderAction.Change)
                            m_ParentHub.TryProcessOrderUpdateRequest(
                            m_ParentHub.m_OrderRequests.Get(OrderRequestType.ChangeReject, uvInstr, uvOrderId, mktSide));        // send the order id to be processed
                    }
                }
            }
            else if (order.Action == OrderAction.Replace)
            { // replace was rejected, previous order still exists
                Log.NewEntry(LogLevel.Minor, "TT Replace Order {0} Rejected : {1}",
                                       eventArgs.Order.SiteOrderKey,   //0
                                       eventArgs.Order.Message);       // 1
            }
            else if (order.Action == OrderAction.Delete)
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
            Order order = eventArgs.NewOrder;
            string siteKey = order.SiteOrderKey;
            m_TTOrders[siteKey] = order;                // store this order regardless of whos it is.
            int uvOrderId = -1;
            if (m_MapTT2UV.TryGetValue(siteKey, out uvOrderId))
            { // we found our order id
                UVInstrument uvInstr;
                if (m_TTInstrKeyToUVInstr.TryGetValue(eventArgs.NewOrder.InstrumentKey, out uvInstr))
                { // we found the uv instrumnet 
                    Log.NewEntry(LogLevel.Minor, "TT Order {4} Updated : {0} {1} {2} @ {3}",
                                    uvInstr,     // 0
                                    eventArgs.NewOrder.BuySell,           // 1
                                    eventArgs.NewOrder.OrderQuantity,     // 2
                                    eventArgs.NewOrder.LimitPrice,        // 3
                                    eventArgs.NewOrder.SiteOrderKey);     // 4
                    UVOrder uvOrder;
                    TTConvert.TryConvert(order, m_ParentHub.m_OrderRecycleFactory, out uvOrder);    // translate to one of our orders
                    uvOrder.Id = uvOrderId;                                                         // overwrite the id
                    uvOrder.Instrument = uvInstr;
                    m_ParentHub.TryProcessOrderUpdateRequest(
                        m_ParentHub.m_OrderRequests.Get(OrderRequestType.ChangeConfirm, uvInstr, uvOrderId, uvOrder.Side, uvOrder));      // send the order id to be processed
                    UVOrder uvOrderToReprocess;
                    if (m_PendingModifyOrders.TryRemove(uvOrderId, out uvOrderToReprocess))
                    {
                        Log.NewEntry(LogLevel.Minor, "Found Pending Order Changes To Process for UV order {0}, resubmitting", uvOrderId);
                        ModifyOrder(uvOrderToReprocess);
                    }
                }
            }
        }
        //
        //
        // *************************************************************
        // ****                TT_OrderBookDownload                 ****
        // *************************************************************
        /// <summary>
        /// Process the download of all TT Orders at the beggining of the listener start.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void TT_OrderBookDownload(object sender, TradingTechnologies.TTAPI.OrderBookDownloadEventArgs eventArgs)
        {
            Log.NewEntry(LogLevel.Minor, "TT OrderBook Downloaded, found {0} orders, attempting to match to UV orders", eventArgs.Orders.Count);
            foreach (Order order in eventArgs.Orders)
            { // similiar to update methods.  We don't know what if anything has changed since last time we were running, so get all info.
                string siteKey = order.SiteOrderKey;
                m_TTOrders[siteKey] = order;                // store this order regardless of whos it is.
                int uvOrderId = -1;
                if (m_MapTT2UV.TryGetValue(siteKey, out uvOrderId))
                { // we found our order id
                    UVInstrument uvInstr;
                    if (m_TTInstrKeyToUVInstr.TryGetValue(order.InstrumentKey, out uvInstr))
                    { // we found the uv instrumnet 
                        Log.NewEntry(LogLevel.Minor, "TT_OrderBookDownload: TT Order {0} Matched to UV Order {1}: Updating UV Order",
                                        order.SiteOrderKey,                   // 0  
                                        uvOrderId);                           // 2     
                        UVOrder uvOrder;
                        TTConvert.TryConvert(order, m_ParentHub.m_OrderRecycleFactory, out uvOrder);   // translate to one of our orders
                        uvOrder.Id = uvOrderId;                                                        // overwrite the id
                        m_ParentHub.TryProcessOrderUpdateRequest(
                            m_ParentHub.m_OrderRequests.Get(OrderRequestType.ChangeConfirm, uvInstr, uvOrderId, uvOrder.Side, uvOrder));              // send the order id to be processed
                    }
                }
                else
                { // we didn't find this order and need to add it it to the default book just in case we need it later
                    Log.NewEntry(LogLevel.Major, "TT_OrderBookDownload: TT Order {0} not found in UV system, adding to default book for {1}", order.SiteOrderKey, order.InstrumentKey);
                    UVOrder uvOrder;
                    TTConvert.TryConvert(order, m_ParentHub.m_OrderRecycleFactory, out uvOrder);                        // translate to one of our orders
                    uvOrder.IPricePending = uvOrder.IPriceConfirmed;                                                    // set the pending qty's correctly.
                    uvOrder.OriginalQtyPending = uvOrder.OriginalQtyConfirmed;
                    UVInstrument uvInstr;
                    if (m_TTInstrKeyToUVInstr.TryGetValue(order.InstrumentKey, out uvInstr))
                    {
                        uvOrder.Instrument = uvInstr;
                        m_ParentHub.TryProcessOrderUpdateRequest(
                            m_ParentHub.m_OrderRequests.Get(OrderRequestType.Unknown, uvInstr, uvOrderId, uvOrder.Side, uvOrder));     // send the order id to be processed
                    }
                    else
                    { // we don't have information about this instrument yet.  
                        if (m_FoundOrdersPendingInstruments.ContainsKey(order.InstrumentKey))
                            m_FoundOrdersPendingInstruments[order.InstrumentKey].Add(uvOrder);
                        else
                        {
                            List<UVOrder> uvOrderList = new List<UVOrder>();
                            uvOrderList.Add(uvOrder);
                            m_FoundOrdersPendingInstruments[order.InstrumentKey] = uvOrderList;
                        }
                    }
                }
            }
        }
        //
        //
        private string Show(Order o)
        {
            return string.Format("[{1} {0} {2} {3} {4}]", o.Action, o.InstrumentKey, o.BuySell, o.LimitPrice.ToDouble(), o.WorkingQuantity.ToInt());
        }
        //
        //
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
                UVInstrument uvInstr;                                                               // try and create a uv instrument
                if (UV.TTServices.TTConvert.TryConvert(instrument, out uvInstr))
                {
                    m_TTInstrKeyToUVInstr.Add(key, uvInstr);
                    if (m_FoundOrdersPendingInstruments.ContainsKey(key))
                    { // we have orders waiting for this info! need to process them now.
                        foreach (UVOrder uvOrder in m_FoundOrdersPendingInstruments[key])
                        { // since now we know about the instrument, we can add them to the default book
                            uvOrder.Instrument = uvInstr;
                            m_ParentHub.TryProcessOrderUpdateRequest(
                            m_ParentHub.m_OrderRequests.Get(OrderRequestType.Unknown, uvInstr, uvOrder.Id, uvOrder.Side, uvOrder));   // send the order to be processed
                        }
                        m_FoundOrdersPendingInstruments.Remove(key);                                    // clear the pending list.
                    }
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
        #endregion//Private Methods

        #region My Events and Triggers
        // ******************************************************************
        // ****                 My Events                                ****
        // ******************************************************************
        //
        //
        //public event EventHandler<Order> OrderAdded;
        //public event EventHandler<Order> OrderDeleted;
        //public event EventHandler<Order> OrderRejected;
        //
        //
        //
        #endregion//My Events

    }
}
