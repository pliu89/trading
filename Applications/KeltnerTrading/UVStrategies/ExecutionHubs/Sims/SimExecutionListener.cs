using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace UV.Strategies.ExecutionHubs.Sims
{
    using UV.Lib.OrderBooks;
    using UV.Lib.Hubs;
    using UV.Lib.BookHubs;
    using UV.Lib.Engines;
    using UV.Lib.Products;
    using UV.Lib.MarketHubs;
    using UV.Lib.Application;
    using UV.Lib.Utilities;
    using UV.Lib.Fills;

    public class SimExecutionListener : ExecutionListener
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        // external services
        //
        private AppServices m_AppServices;
        private Dispatcher m_Dispatcher;
        private MarketHub m_Market = null;

        //
        // State
        //
        private bool m_isDisposing = false;

        //
        // collections
        //
        private Dictionary<InstrumentName, List<OrderBook>> m_SimOrderBooks = new Dictionary<InstrumentName, List<OrderBook>>();
        private List<int> m_MarketInstrumentIdChangedList = new List<int>();                     // work space for mkt updates.
        private List<InstrumentName> m_MarketInstrumentChangedList = new List<InstrumentName>(); // work space for mkt updates.
        private List<Order> m_OrdersWorkspace = new List<Order>();                               // work space for orders.
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        public SimExecutionListener(string name, IEngineHub engineHub)
            : base(name, engineHub)
        {
            m_AppServices = UV.Lib.Application.AppServices.GetInstance();       // find our AppServices
            IService service = null;
            if (m_AppServices.TryGetService("SimMarket", out service))          // find the simulated market
            {
                m_Market = (UV.Lib.MarketHubs.MarketHub)service;
                m_Market.FoundResource += new EventHandler(ListenerEventHandler);        // subscribe to found resources.
                m_Market.MarketStatusChanged += new EventHandler(ListenerEventHandler);
                m_Market.InstrumentChanged += new EventHandler(ListenerEventHandler);
            }
        }
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
        //
        // *****************************************
        // ****         InitializeThread()      ****
        // *****************************************
        /// <summary>
        /// Called to allow dispatcher to be attached to thread.  
        /// OnInitialized is called to allow subscribers to use thread to set up.
        /// </summary>
        public override void InitializeThread()
        {
            m_Dispatcher = Dispatcher.CurrentDispatcher;
            OnInitialized();  // session is ready, go to work!
            Dispatcher.Run();
        }
        //
        //
        // *****************************************
        // ****           StopThread()          ****
        // *****************************************
        /// <summary>
        /// threadsafe call to signal nice shutdown
        /// </summary>
        public override void StopThread()
        {
            if (m_isDisposing) return;
            m_isDisposing = true;
            m_Dispatcher.BeginInvoke(new Action(Shutdown));   // allow our order engine to nicely shutdown
        }
        //
        // *********************************************
        // ****         Try Submit Order()          ****
        // *********************************************
        public override bool TrySubmitOrder(int orderBookID, Order order)
        {
            if (base.TrySubmitOrder(orderBookID, order))
            { // successfully submitted
                Log.NewEntry(LogLevel.Minor, "TrySubmitOrder: New order {0}.", order);

                // Create the confirmation request
                order.IPriceConfirmed = order.IPricePending;
                order.OrderStateConfirmed = order.OrderStatePending;
                order.OriginalQtyConfirmed = order.OriginalQtyPending;

                m_ExecContainer.m_OrderInstruments[order.Instrument].ProcessAddConfirm(order);           // this will call appropiate events, and set pending changes flag

                return true;
            }
            return false;
        }// TrySubmitOrder()
        //
        // *********************************************
        // ****         TryDeleteOrder              ****
        // *********************************************
        public override bool TryDeleteOrder(Order order)
        {
            if (base.TryDeleteOrder(order))
            { // successful delete request
                Log.NewEntry(LogLevel.Minor, "TryDeleteOrder: {0}@{1} --> delete for order {2}.", order.WorkingQtyPending, order.PricePending, order);
                m_ExecContainer.m_OrderInstruments[order.Instrument].ProcessDeleteConfirm(order);  // Create the delete confirm
                return true;
            }
            return false;
        }
        //
        //
        // *****************************************
        // ****       TryChangeOrderPrice()     ****
        // *****************************************
        public override bool TryChangeOrderPrice(Order orderToModify, int newIPrice, int newQty)
        {
            if (base.TryChangeOrderPrice(orderToModify, newIPrice, newQty))
            { // our request was submitted correctly.
                Log.NewEntry(LogLevel.Minor, "TryChangeOrderPrice: {0}@{1} --> {2}@{3} for order {4}.", orderToModify.WorkingQtyPending, orderToModify.PricePending, orderToModify.WorkingQtyPending, newIPrice, orderToModify);
                orderToModify.IPriceConfirmed = newIPrice;  // confirm the change here.
                orderToModify.OriginalQtyConfirmed = newQty;     
                return true;
            }
            return false;
        }//TryChangeOrderPrice
        //
        //
        // *****************************************
        // ****       TryChangeOrderQty()       ****
        // *****************************************
        public override bool TryChangeOrderQty(Order orderToModify, int newQty)
        {
            if (base.TryChangeOrderQty(orderToModify, newQty))
            { // request was successful
                Log.NewEntry(LogLevel.Minor, "TryChangeOrderQty: {0}@{1} --> {2}@{3} for order {4}.", orderToModify.WorkingQtyPending, orderToModify.PricePending, newQty, orderToModify.PricePending, orderToModify);
                orderToModify.OriginalQtyConfirmed = newQty;        // confirm the change 
                return true;
            }
            return false;
        }// TryChangeOrderQty()
        //
        //
        // *****************************************
        // ****         ProcessCreateBook()     ****
        // *****************************************
        /// <summary>
        /// Process to initialize a single order book.
        /// </summary>
        /// <param name="orderBook"></param>
        protected override void ProcessCreateBook(OrderBook orderBook)
        {
            // Locate the OrderInstrument for this book.
            OrderInstrument orderInstrument = null;
            if (!m_ExecContainer.m_OrderInstruments.TryGetValue(orderBook.Instrument, out orderInstrument))
            {   // We do not have an order instrument for this Instrument.
                InstrumentDetails instrumentDetails;
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
                        m_PendingOrderBooks[orderBook.Instrument] = new List<OrderBook> { orderBook };
                    }
                    return;
                }
                else
                {   // We have the instrument details, so create the OrderInstrument now
                    Log.NewEntry(LogLevel.Minor, "ProcessCreateBook: Attempting to Process OrderBookID {0}", orderBook.BookID);
                    orderInstrument = new OrderInstrument(orderBook.Instrument, instrumentDetails);
                    m_ExecContainer.m_OrderInstruments.Add(orderBook.Instrument, orderInstrument);
                    Log.NewEntry(LogLevel.Minor, "ProcessCreateBook: Created new OrderInstrument {0}.", orderBook.Instrument);
                    //int instrId = -1;
                    //if (m_Market != null && m_Market.TryLookupInstrumentID(orderBook.Instrument, out instrId) == false)
                    //{   // Subscribe to this market instrument.
                    //    // Order sim needs market updates for each instrument with orders.
                    //    m_Market.RequestInstrumentSubscription(orderBook.Instrument);
                    //}
                }
                // If we get here, lets try to add our new book to the order instrument.
                if (orderInstrument != null)
                {
                    orderInstrument.TryAddBook(orderBook);

                    // Simulator also needs to keep a copy of all books to simulate fills.
                    List<OrderBook> orderBookList = null;
                    if (!m_SimOrderBooks.TryGetValue(orderInstrument.Instrument, out orderBookList))
                    {
                        orderBookList = new List<OrderBook>();
                        m_SimOrderBooks.Add(orderInstrument.Instrument, orderBookList);
                    }
                    orderBookList.Add(orderBook);
                }
            }
            else
            {
                Log.NewEntry(LogLevel.Minor, "ProcessCreateBook: Exisiting OrderInstrument {0} found. Attempting to add OrderBookID {1}",
                    orderInstrument.Instrument, orderBook.BookID);
                orderInstrument.TryAddBook(orderBook);

                // Simulator also needs to keep a copy of all books to simulate fills.
                List<OrderBook> orderBookList = null;
                if (!m_SimOrderBooks.TryGetValue(orderInstrument.Instrument, out orderBookList))
                {
                    orderBookList = new List<OrderBook>();
                    m_SimOrderBooks.Add(orderInstrument.Instrument, orderBookList);
                }
                orderBookList.Add(orderBook);
            }
        }// ProcessCreateBook()
        #endregion//Public Methods


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        //
        //
        //***********************************************
        //****             Shutdown()                ****
        //***********************************************
        /// <summary>
        /// Carefully allow everyone to shutdown
        /// </summary>
        private void Shutdown()
        {
            OnStopping();           // call subscribed order engines to allow them to stop nicely.
            m_Dispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);
        }
        //
        //
        // ************************************************
        // ****             SubscribeTo                ****
        // ************************************************
        /// <summary>
        /// Called after a user requests a book be created, we need to subscribe to 
        /// price events for it.
        /// </summary>
        /// <param name="instrName"></param>
        private void SubscribeTo(InstrumentName instrName)
        {
            Log.NewEntry(LogLevel.Major, "{0}: InstrumentLookup {1} {2}.", this.Name, instrName.Product, instrName.SeriesName);
            // how to subscribe to market?
        }
        #endregion//Private Methods


        #region no Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// simple pass through to our event queue.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArg"></param>
        private void ListenerEventHandler(object sender, EventArgs eventArg)
        {
            ProcessEvent(eventArg);
        }
        #endregion//Event Handlers

        #region Event Processing
        // *****************************************************************
        // ****                    Events Processing                    ****
        // *****************************************************************
        //
        /// <summary>
        /// Threadsafe call to request the execution thread to process an event.
        /// </summary>
        /// <param name="eventArg"></param>
        public override void ProcessEvent(EventArgs eventArg)
        {
            m_InQueue.Enqueue(eventArg);
            if (m_Dispatcher.HasShutdownStarted) { return; }
            m_Dispatcher.Invoke(ProcessEvent);
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
                    if (engEvent.MsgType == EngineEventArgs.EventType.SyntheticOrder)
                        m_ExecContainer.IOrderEngine.ProcessEvent(e);
                }
                else if (e is InstrumentChangeArgs)
                { // this is a market update.  
                    // 1. Fill Orders ( we should do this first)
                    // 2. Update Internal Markets 
                    InstrumentChangeArgs instrChangeArgs = (InstrumentChangeArgs)e;
                    m_MarketInstrumentIdChangedList.Clear();      // load ids for instruments we need to check.
                    foreach (KeyValuePair<int, InstrumentChange> pair in instrChangeArgs.ChangedInstruments)
                    {
                        if (pair.Value.MarketDepthChanged[QTMath.BidSide].Contains(0) || pair.Value.MarketDepthChanged[QTMath.AskSide].Contains(0))
                            m_MarketInstrumentIdChangedList.Add(pair.Value.InstrumentID);
                    }
                    if (m_MarketInstrumentIdChangedList.Count > 0)
                    {
                        SimulateFills(m_MarketInstrumentIdChangedList);
                        SimulateMarketUpdates(m_MarketInstrumentIdChangedList);
                    }
                }
                else if (e is FoundServiceEventArg)
                {
                    FoundServiceEventArg foundServiceEvent = (FoundServiceEventArg)e;
                    if (foundServiceEvent.FoundInstruments != null && foundServiceEvent.FoundInstruments.Count != 0)
                    {
                        foreach (InstrumentName instrName in foundServiceEvent.FoundInstruments)
                        {
                            if (!m_ExecContainer.m_Markets.ContainsKey(instrName))
                            {
                                UV.Lib.BookHubs.Market newMarket = UV.Lib.BookHubs.Market.Create(instrName);
                                m_ExecContainer.m_Markets.Add(instrName, newMarket);
                            }
                            InstrumentDetails instrDetails;
                            if (m_Market.TryGetInstrumentDetails(instrName, out instrDetails))
                                ProcessInstrumentsFound(instrDetails);
                        }
                    }
                }
            }
        }
        //
        //
        //
        // *************************************************
        // ****             SimulateMarketUpdates       ****
        // *************************************************
        /// <summary>
        /// This is a simple method for taking the MarketHub market and copying it over to our
        /// internal markets.
        /// </summary>
        /// <param name="marketInstrList"></param>
        private void SimulateMarketUpdates(List<int> marketInstrList)
        {
            Book aBook;
            if (m_Market.TryEnterReadBook(out aBook))
            {
                foreach (int instrId in marketInstrList)
                {
                    Market marketHubMarket = null;
                    if (!aBook.Instruments.TryGetValue(instrId, out marketHubMarket))
                    {
                        Log.NewEntry(LogLevel.Minor, "SimulateFills: Failed to obtain market for mkt instr ID {0}", instrId);
                        continue;
                    }

                    Market internalMarket = m_ExecContainer.m_Markets[marketHubMarket.Name];
                    marketHubMarket.CopyTo(internalMarket);  // copy the market to our internal market.

                    // *****************************************************
                    // ****             Fire Events Now                 ****
                    // *****************************************************
                    internalMarket.OnMarketChanged();
                    internalMarket.OnMarketBestPriceChanged();
                }
                m_Market.ExitReadBook(aBook);
            }
        }
        //
        // *************************************************
        // ****             Simulate Fills()            ****
        // *************************************************
        private void SimulateFills(List<int> marketInstrList)
        {
            Book aBook;
            if (m_Market.TryEnterReadBook(out aBook))
            {
                foreach (int instrId in marketInstrList)
                {
                    Market market = null;
                    if (!aBook.Instruments.TryGetValue(instrId, out market))
                    {
                        Log.NewEntry(LogLevel.Minor, "SimulateFills: Failed to obtain market for mkt instr ID {0}", instrId);
                        continue;
                    }

                    InstrumentName instrName = market.Name;
                    List<OrderBook> orderBooks = null;
                    if (m_SimOrderBooks.TryGetValue(instrName, out orderBooks))
                    {
                        double tickSize = m_ExecContainer.m_OrderInstruments[instrName].Details.TickSize;
                        foreach (OrderBook orderBook in orderBooks)
                        {   //
                            // Try to fill this order book.
                            //
                            for (int orderSide = 0; orderSide < 2; orderSide++)
                            {
                                int orderSign = QTMath.MktSideToMktSign(orderSide);
                                int otherSide = (orderSide + 1) % 2;
                                int ourMarketPrice = (int)Math.Round(market.Price[orderSide][0] / tickSize);
                                int oppMarketPrice = (int)Math.Round(market.Price[otherSide][0] / tickSize);
                                int orderRank = 0;                      // only fill top for now
                                int orderPrice;
                                if (orderBook.TryGetIPriceByRank(orderSide, orderRank, out orderPrice))
                                {
                                    if ((oppMarketPrice - orderPrice) * orderSign <= 0)
                                    {   // filled by crossing market.
                                        m_OrdersWorkspace.Clear();
                                        orderBook.GetOrdersByRank(orderSide, orderRank, ref m_OrdersWorkspace);
                                        FillTheseOrders(ref m_OrdersWorkspace);
                                    }
                                    else if ((ourMarketPrice - orderPrice) * orderSign > 0)
                                    {   // We are below other orders in market

                                    }
                                    else
                                    {   // We are in the middle of the market (or on the top of book).
                                        //m_OrdersWorkspace.Clear();
                                        //orderBook.GetOrdersByRank(orderSide, orderRank, ref m_OrdersWorkspace);
                                        //FillTheseOrders( ref m_OrdersWorkspace );
                                    }
                                }
                            }
                        }

                    }
                }
                m_Market.ExitReadBook(aBook);
            }// if MarketBook obtained.
        }// SimulateFills()
        //
        //
        // *************************************************
        // ****         Fill These Orders()             ****
        // *************************************************
        private void FillTheseOrders(ref List<Order> ordersToFill)
        {
            foreach (Order order in ordersToFill)
            {
                if (order.OrderStateConfirmed != OrderState.Submitted)
                    continue;

                // Create the fill
                UV.Lib.Fills.Fill aFill = new Fill();
                aFill.ExchangeTime = m_Market.LocalTime;
                aFill.LocalTime = aFill.ExchangeTime;
                aFill.Price = order.PriceConfirmed;
                aFill.Qty = order.WorkingQtyConfirmed;
                Log.NewEntry(LogLevel.Major, "OrderFilled: Fill={1} Order={0}.", order, aFill);
                // Create the fill event
                UV.Lib.Fills.FillEventArgs fillEvent = new UV.Lib.Fills.FillEventArgs(aFill, order.Id, order.Instrument, true);
                m_ExecContainer.m_OrderInstruments[order.Instrument].TryProcessFill(fillEvent);
            }
        }//FillTheseOrders()
        #endregion // Event Processing

        #region ITimer Implementation
        // *****************************************************
        // ****             TimerSubscriberUpdate()         ****
        // *****************************************************
        //
        //
        /// <summary>
        /// Called by the hub thread to update us since we are a iTimerSubscriber.
        /// We will push it onto our thread and then call our own subscribers with the correct thread.
        /// </summary>
        public override void TimerSubscriberUpdate()
        {
            if (m_Dispatcher.HasShutdownStarted) return;
            m_Dispatcher.Invoke(CallITimerSubscribers);
        }
        //
        #endregion //ITimer Implementation

    }
}
