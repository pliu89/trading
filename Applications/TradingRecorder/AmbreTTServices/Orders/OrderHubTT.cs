using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;

namespace Ambre.TTServices.Orders
{
    using TT = TradingTechnologies.TTAPI;

    using LogLevel = Misty.Lib.Hubs.LogLevel;
    using Misty.Lib.OrderHubs;
    using Misty.Lib.Products;
    using Misty.Lib.IO.Xml;

    public class OrderHubTT : OrderHub, IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // Services
        private TTApiService m_TTService = null;
        private OrderListener m_OrderListener = null;
        private Markets.MarketTTAPI m_Market = null;

        // Look up tables.        
        private ConcurrentDictionary<TT.InstrumentKey, InstrumentName> m_TTKey2Name = new ConcurrentDictionary<TT.InstrumentKey, InstrumentName>();
        private ConcurrentDictionary<TT.InstrumentKey, ConcurrentQueue<EventArgs>> m_TTEventsToProcess = new ConcurrentDictionary<TT.InstrumentKey, ConcurrentQueue<EventArgs>>();

        //
        private ConcurrentQueue<OrderHubRequest> m_WaitingRequests = new ConcurrentQueue<OrderHubRequest>();        // place to store requests to do.


        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public OrderHubTT()
            : base( "OrderHubTT" )
        {


        }
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


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        /// <summary>
        /// This order hub gets a callback from TTServices and automatically collects.
        /// </summary>
        public override void Connect()
        {
        }
        //
        //
        public override void Start()
        {
            // Locate required services
            Misty.Lib.Application.AppServices appServices = Misty.Lib.Application.AppServices.GetInstance();
            foreach (Misty.Lib.Application.IService service in appServices.GetServices(typeof(Markets.MarketTTAPI)))
            {
                this.AddHub((Markets.MarketTTAPI)service);
                break;
            }
            //foreach (Misty.Lib.Application.IService service in appServices.Services.Values)
            //    if (service is Markets.MarketTTAPI)
            //    {
            //        this.AddHub((Markets.MarketTTAPI)service);
            //        break;
            //    }
            if (m_Market == null)
                throw new Exception("Failed to find required service.");
            // Subscribe to TTApi
            if (m_TTService == null)
            {
                m_TTService = TTApiService.GetInstance();
                m_TTService.ServiceStateChanged += new EventHandler(TTService_ServiceStatusChanged);
            }
            base.Start();
        }//Start()
        //
        //
        public void AddHub(Misty.Lib.Hubs.Hub hub)
        {
            if (hub is Markets.MarketTTAPI)
            {
                if (m_Market != null)
                    m_Market.FoundResource -= new EventHandler(HubEventEnqueue);     // disconnect any previous mkt hub.
                m_Market = (Markets.MarketTTAPI)hub;
                m_Market.FoundResource += new EventHandler(HubEventEnqueue);
            }
        }//AddHub()
        //
        #endregion//Public Methods


        #region Private HubEvent Handler 
        // *****************************************************************
        // ****            Private HubEvent Handler                     ****
        // *****************************************************************
        //
        protected override void HubEventHandler(EventArgs[] eventArgs)
        {
            foreach (EventArgs eventArg in eventArgs)
            {
                if (eventArg is TT.OrderAddedEventArgs)
                    ProcessOrderAdded((TT.OrderAddedEventArgs)eventArg);
                else if (eventArg is TT.OrderDeletedEventArgs)
                    ProcessOrderDeleted((TT.OrderDeletedEventArgs)eventArg);
                else if (eventArg is TT.OrderFilledEventArgs)
                    ProcessOrderFilled((TT.OrderFilledEventArgs)eventArg);
                else if (eventArg is TT.OrderBookDownloadEventArgs)
                    ProcessOrderBookDownload((TT.OrderBookDownloadEventArgs)eventArg);
                else if (eventArg is TT.OrderRejectedEventArgs)
                    ProcessOrderRejected((TT.OrderRejectedEventArgs)eventArg);
                else if (eventArg is TT.OrderUpdatedEventArgs)
                    ProcessOrderUpdated((TT.OrderUpdatedEventArgs)eventArg);
                else if (eventArg is OrderHubRequest)
                    ProcessRequest((OrderHubRequest)eventArg);
                else if (eventArg is Misty.Lib.MarketHubs.FoundServiceEventArg)
                    ProcessMarketFoundResource((Misty.Lib.MarketHubs.FoundServiceEventArg)eventArg);
                else
                    Log.NewEntry(LogLevel.Major, "HubEventHandler: Unknown event {0}", eventArg);
            }//next eventArg

        }//HubEventHandler()
        //

        //
        //
        // *****************************************************
        // ****             Process Request()               ****
        // *****************************************************
        private void ProcessRequest(OrderHubRequest eventArg)
        {
            Log.NewEntry(LogLevel.Minor, "ProcessRequest: {0}", eventArg);
            switch (eventArg.Request)
            {
                case OrderHubRequest.RequestType.RequestConnect:
                    // We automatically connect to the TT Api once we get the call back from it.
                    break;
                case OrderHubRequest.RequestType.RequestCreateFillBook:
                    if (eventArg.Data[0] is TT.InstrumentKey)
                    {
                        TT.InstrumentKey ttKey = (TT.InstrumentKey) eventArg.Data[0];
                        InstrumentName name;
                        if (m_TTKey2Name.TryGetValue(ttKey, out name) && m_Books.ContainsKey(name))
                            Log.NewEntry(LogLevel.Minor, "ProcessRequest: Already have book for {1}. Ignoring {0}.", eventArg,name);
                        else
                        {
                            OrderBookTT book;
                            if (TryCreateNewBook(ttKey, out book))
                            {
                                Log.NewEntry(LogLevel.Minor, "ProcessRequest: Create book succeeded {0}.", book);
                                OrderBookEventArgs outgoingEventArg = new OrderBookEventArgs(this, book.m_InstrumentName, OrderBookEventArgs.EventTypes.CreatedBook);                                
                                OnBookCreated(outgoingEventArg);// TODO: Load additional information needed for new book creation.
                                // Push out waiting events.
                                ConcurrentQueue<EventArgs> waitingTTOrders;
                                while (m_TTEventsToProcess.TryRemove(ttKey, out waitingTTOrders))         // this should only execute once, but if new orders are added concurrently could execute more!
                                {
                                    EventArgs eventArg1;
                                    while (waitingTTOrders.Count > 0)
                                        if (waitingTTOrders.TryDequeue(out eventArg1))
                                            this.HubEventEnqueue(eventArg1);
                                        else
                                            break;
                                }
                                                                

                                
                            }
                            else
                            {
                                Log.NewEntry(LogLevel.Minor, "ProcessRequest: Create book failed. Will try again.");
                                m_WaitingRequests.Enqueue(eventArg);                    // Store this request
                            }
                        }
                    }
                    break;
                case OrderHubRequest.RequestType.RequestShutdown:
                    if (m_OrderListener != null)
                    {
                        m_OrderListener.Dispose();
                        m_OrderListener = null;
                    }
                    base.Stop();
                    break;
                default:
                    break;
            }// switch request

        }//ProcessRequest()
        //
        //
        // *****************************************************************
        // ****             ProcessMarketFoundResource()                ****
        // *****************************************************************
        private void ProcessMarketFoundResource(Misty.Lib.MarketHubs.FoundServiceEventArg eventArg)
        {
            if (eventArg.FoundInstruments != null && eventArg.FoundInstruments.Count > 0)
            {   // New instruments found.  We are guaranteed by Mkt hub that the InstrumentName is unique.
                Log.BeginEntry(LogLevel.Minor, "MarketResoursesFound: Instruments: ");
                foreach (InstrumentName instrName in eventArg.FoundInstruments)
                {
                    Log.AppendEntry("[{0}",instrName);
                    TT.InstrumentDetails details;
                    if (m_Market.TryLookupInstrumentDetails(instrName, out details))
                    {                        
                        OrderHubRequest request;
                        while (m_WaitingRequests.TryDequeue(out request))
                            HubEventEnqueue(request);
                    }
                    Log.AppendEntry("]");
                }
                Log.EndEntry();
            }
        }// ProcessMarketFoundResource()
        //
        //
        //
        #endregion//Private HubEvent Handler


        #region Private Order Update Handlers
        // *****************************************************************
        // ****             Private Order Update Handlers               ****
        // *****************************************************************
        //
        //
        private void ProcessOrderAdded(TT.OrderAddedEventArgs eventArg)
        {
            Log.BeginEntry(LogLevel.Minor, "ProcessOrderAdded:");
            OrderBookEventArgs outgoingEventArg = null;
            TT.Order ttOrder = eventArg.Order;
            InstrumentName name;
            OrderBook book;
            if (m_TTKey2Name.TryGetValue(ttOrder.InstrumentKey, out name) && this.TryEnterBookWrite(name, out book))
            {   // We know this TTKey, and have an associated book!
                OrderBookTT booktt = (OrderBookTT)book;
                Order newOrder = booktt.CreateNewOrder();
                int mktSign = Misty.Lib.Utilities.QTMath.BuySign;
                newOrder.Side = Misty.Lib.Utilities.QTMath.BuySide;
                if (ttOrder.BuySell == TT.BuySell.Sell)
                {
                    mktSign = Misty.Lib.Utilities.QTMath.SellSign;
                    newOrder.Side = Misty.Lib.Utilities.QTMath.SellSide;
                }
                newOrder.Qty = mktSign * ttOrder.WorkingQuantity.ToInt();
                newOrder.IPrice = (int)Math.Round(ttOrder.LimitPrice.ToDouble() / book.MinimumPriceTick);
                int intPrice = ttOrder.LimitPrice.ToInt();  // for debugging
                if (book.TryAddOrder(newOrder))
                {
                    booktt.m_TagBre2TT.Add(newOrder.Tag, ttOrder.SiteOrderKey);
                    booktt.m_TagTT2Bre.Add(ttOrder.SiteOrderKey,newOrder.Tag);                    
                    outgoingEventArg = new OrderBookEventArgs(this,name,OrderBookEventArgs.EventTypes.NewOrder);      // Load out-going eventArg
                    outgoingEventArg.Order = newOrder;
                    Log.AppendEntry(" Added new order {0} to book {2}.", newOrder, ttOrder, book);
                }
                else
                {
                    Log.AppendEntry(" Failed to add order {0} to book {2}. Store order for later.", newOrder, ttOrder, book);
                    StoreTTEvent(eventArg, ttOrder.InstrumentKey);
                }
                this.ExitBookWrite(book);
            }
            else
            {   // We have no book for this instrument.
                StoreTTEvent(eventArg, ttOrder.InstrumentKey);                        // Store order and request mkt details.
            }
            Log.EndEntry();
            if (outgoingEventArg != null)
                OnBookChanged(outgoingEventArg);                
        }// ProcessOrderAdded
        //
        //
        private void ProcessOrderDeleted(TT.OrderDeletedEventArgs eventArg)
        {
            Log.BeginEntry(LogLevel.Minor, "ProcessOrderDeleted:");
            OrderBookEventArgs outgoingEventArg = null;                 // outgoing event, if needed.
            TT.Order ttOrder = eventArg.OldOrder;
            InstrumentName name;
            OrderBook baseBook;
            if (m_TTKey2Name.TryGetValue(ttOrder.InstrumentKey, out name) && this.TryEnterBookWrite(name, out baseBook))
            {
                OrderBookTT book = (OrderBookTT)baseBook;
                string tag;
                if (book.m_TagTT2Bre.TryGetValue(ttOrder.SiteOrderKey, out tag))
                {
                    Order deletedOrder;
                    if (book.TryDeleteOrder(tag, out deletedOrder))
                    {
                        outgoingEventArg = new OrderBookEventArgs(this,name,OrderBookEventArgs.EventTypes.DeletedOrder);      // Load out-going eventArg
                        outgoingEventArg.Order = deletedOrder;
                        Log.AppendEntry(" Deleted order {0} to book {1}.", deletedOrder, book);
                    }
                }
                this.ExitBookWrite(baseBook);
            }
            Log.EndEntry();
            if (outgoingEventArg != null)
                OnBookChanged(outgoingEventArg);   

        }// ProcessOrderDelete()
        //
        //
        //
        private void ProcessOrderFilled(TT.OrderFilledEventArgs eventArg)
        {
            Log.BeginEntry(LogLevel.Minor, "ProcessOrderFilled:");
            Log.EndEntry();
        }
        private void ProcessOrderBookDownload(TT.OrderBookDownloadEventArgs eventArg)
        {
            Log.BeginEntry(LogLevel.Minor, "ProcessOrderBookDownload:");
            Log.EndEntry();
        }
        private void ProcessOrderRejected(TT.OrderRejectedEventArgs eventArg)
        {
            Log.BeginEntry(LogLevel.Minor, "ProcessOrderRejected:");
            Log.EndEntry();
        }
        private void ProcessOrderUpdated(TT.OrderUpdatedEventArgs eventArg)
        {
            Log.BeginEntry(LogLevel.Minor, "ProcessOrderUpdated:");
            Log.EndEntry();
        }
        //
        //
        // ****                 Store Order()               ****
        //
        private void StoreTTEvent(EventArgs ttEVentArgs, TT.InstrumentKey ttKey)
        {
            ConcurrentQueue<EventArgs> eventQueue;
            if (m_TTEventsToProcess.TryGetValue(ttKey, out eventQueue))
                eventQueue.Enqueue(ttEVentArgs);
            else if (m_TTEventsToProcess.TryAdd(ttKey, new ConcurrentQueue<EventArgs>()) && m_TTEventsToProcess.TryGetValue(ttKey, out eventQueue))
            {
                eventQueue.Enqueue(ttEVentArgs);
                OrderHubRequest request = new OrderHubRequest(OrderHubRequest.RequestType.RequestCreateFillBook);
                request.Data = new object[] {ttKey};
                this.HubEventEnqueue(request);
            }
            else
            {   // TODO: Check whether perhaps we can fail while the orderbook is being created.
                Log.NewEntry(LogLevel.Warning, "Listener_OrderAdded: Failed to store order in OrdersToProcess list.");
            }
        }//StoreOrder()   
        //
        //
        //
        #endregion // order update handlers


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        //
        //
        // *****************************************************
        // ****             Try Create New Book()           ****
        // *****************************************************
        private bool TryCreateNewBook(TT.InstrumentKey ttKey, out OrderBookTT book)
        {
            book = null;
            InstrumentName name;
            TT.InstrumentDetails details;
            if (m_Market.TryLookupInstrument(ttKey, out name) && m_Market.TryLookupInstrumentDetails(name, out details))
            {   // Market knows this instrument already.
                Log.BeginEntry(LogLevel.Minor, "TryCreateNewBook: Creating book.");
                book = new OrderBookTT(this, name);                                           // Create book.
                double minTickSize = Convert.ToDouble(details.TickSize.Numerator) / Convert.ToDouble(details.TickSize.Denominator);
                book.MinimumPriceTick = minTickSize;
                if (m_Books.TryAdd(book.m_InstrumentName, book))
                {
                    m_TTKey2Name.TryAdd(ttKey, book.m_InstrumentName);
                    
                    Log.AppendEntry(" New book created {0}.", book);
                    Log.EndEntry();
                    return true;
                }
                else
                {
                    Log.AppendEntry(" Failed to add book to Books.");
                    Log.EndEntry();
                    return false;
                }
            }
            else
            {   // Market doesnt know this instrument yet.
                Log.NewEntry(LogLevel.Minor, "TryCreateNewBook: Market instrument unknown.  Requesting details from market for {0}.", TTConvert.ToString(ttKey) );
                m_Market.RequestInstruments(ttKey);                 // request to look it up.
                return false;
            }
        }//TryCreateNewBook()
        //
        //
        //
        #endregion//Private Methods


        #region no Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// Called by the TTService object once the API has connected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArg"></param>
        private void TTService_ServiceStatusChanged(object sender, EventArgs eventArg)
        {
            TTServices.TTApiService.ServiceStatusChangeEventArgs ttServiceEvent = (TTServices.TTApiService.ServiceStatusChangeEventArgs)eventArg;
            if (ttServiceEvent.IsConnected)
            {
                if (m_OrderListener == null)                            // use this to recall whether we've already connected or not.
                {
                    m_OrderListener = new OrderListener("OrderListener", this);
                    m_OrderListener.Start();
                }
            }
        }
        //
        #endregion//Event Handlers


        #region no External Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        //
        //
        //
        //
        //
        #endregion//External Event Handlers


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
        #endregion IStringifiable


    }
}
