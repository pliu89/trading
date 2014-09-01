using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;

namespace UV.Lib.OrderBookHubs.Sims
{
    using UV.Lib.Application;
    using UV.Lib.Hubs;
    using UV.Lib.Products;
    using UV.Lib.IO.Xml;
    using UV.Lib.Utilities;

    using UVMkt = UV.Lib.MarketHubs;
    using UVBooks = UV.Lib.BookHubs;

    /// <summary>
    /// Implementation of simulated order hub.
    /// </summary>
    public class OrderBookHubSim : OrderBookHub, IService, IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // External Services
        //
        private AppServices m_Services = null;
        private MarketHubs.MarketHub m_Market = null;


        // Internal simulation variables
        private List<int> m_MarketInstrumentIdChangedList = new List<int>();        // work space for mkt updates.
        private Dictionary<InstrumentName, List<OrderBook>> m_SimOrderBooks = new Dictionary<InstrumentName, List<OrderBook>>();
        private List<Order> m_OrdersWorkspace = new List<Order>();                  // work space for orders.
        //private List<InstrumentName> m_OrderInstrumentsToUpdate = new List<InstrumentName>();
        //private RequestFactory<RequestCodeSim> m_SimRequests = new RequestFactory<RequestCodeSim>();

        

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public OrderBookHubSim()
            : base("OrderHubSim")
        {
            m_Services = AppServices.GetInstance();


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
        //
        // *********************************************
        // ****         Try Submit Order()          ****
        // *********************************************
        public override bool TrySubmitOrder(int orderBookID, Order order)
        {
            if(base.TrySubmitOrder(orderBookID, order))
            { // successfully submitted
                Log.NewEntry(LogLevel.Minor, "TrySubmitOrder: New order {0}.", order);

                // Create the confirmation request
                order.IPriceConfirmed = order.IPricePending;
                order.OrderStateConfirmed = order.OrderStatePending;
                order.OriginalQtyConfirmed = order.OriginalQtyPending;
                RequestEventArg<OrderRequestType> orderRequest =  
                    m_OrderRequests.Get(OrderRequestType.AddConfirm, order.Instrument, order.Id, order.Side, order);
                HubEventEnqueue(orderRequest);
                //TryProcessOrderUpdateRequest(
                return true;
            }
            return false;
        }// TrySubmitOrder()
        //
        //
        // *********************************************
        // ****         TryDeleteOrder              ****
        // *********************************************
        public override bool TryDeleteOrder(Order order)
        {
            if (base.TryDeleteOrder(order))
            { // successful delete request
                Log.NewEntry(LogLevel.Minor, "TryDeleteOrder: {0}@{1} --> delete for order {2}.", order.WorkingQtyPending, order.PricePending, order);

                // Create the confirmation request
                //TryProcessOrderUpdateRequest(
                RequestEventArg<OrderRequestType> orderRequest = 
                    m_OrderRequests.Get(OrderRequestType.DeleteConfirm, order.Instrument, order.Id, order.Side, order);
                HubEventEnqueue(orderRequest);
                return true;
            }
            return false;
        }
        // *****************************************
        // ****       TryChangeOrderPrice()     ****
        // *****************************************
        /// <summary>
        /// Called by a strategy or user to change the pending price of a given order.
        /// Called by external threads.
        /// </summary>
        /// <param name="orderToModify"></param>
        /// <param name="newIPrice"></param>
        /// <returns></returns>
        public override bool TryChangeOrderPrice(Order orderToModify, int newIPrice)
        {
            if (base.TryChangeOrderPrice(orderToModify, newIPrice))
            { // our request was submitted correctly.
                Log.NewEntry(LogLevel.Minor, "TryChangeOrderPrice: {0}@{1} --> {2}@{3} for order {4}.", orderToModify.WorkingQtyPending, orderToModify.PricePending, orderToModify.WorkingQtyPending, newIPrice, orderToModify);

                // Create the confirmation request
                orderToModify.IPriceConfirmed = newIPrice;
                //TryProcessOrderUpdateRequest(
                RequestEventArg<OrderRequestType> orderRequest = 
                    m_OrderRequests.Get(OrderRequestType.ChangeConfirm, orderToModify.Instrument, orderToModify.Id, orderToModify.Side, orderToModify);
                HubEventEnqueue(orderRequest);
                return true;
            }
            return false;
        }//TryChangeOrderPrice
        //
        //
        //
        // *****************************************
        // ****       TryChangeOrderQty()       ****
        // *****************************************
        /// <summary>
        /// Called by a strategy or user to change the pending qty of a given order.
        /// If the newqty submitted will result in this order working a zero qty,
        /// false will be returned and the order will be deleted!
        /// </summary>
        /// <param name="orderToModify"></param>
        /// <param name="newQty">SIGNED QTY</param>
        /// <returns>false if sign is incorrect or order is now zero qty and deleted</returns>
        public override bool TryChangeOrderQty(Order orderToModify, int newQty)
        {
            if(base.TryChangeOrderQty(orderToModify, newQty)) 
            { // request was successful
                Log.NewEntry(LogLevel.Minor, "TryChangeOrderQty: {0}@{1} --> {2}@{3} for order {4}.", orderToModify.WorkingQtyPending, orderToModify.PricePending, newQty, orderToModify.PricePending, orderToModify);
                //Log.NewEntry(LogLevel.Minor, "TryChangeOrderPrice: New Qty {1} for order {0}.", orderToModify, newQty);

                // Create the confirmation request
                orderToModify.OriginalQtyConfirmed = newQty;
                //TryProcessOrderUpdateRequest(
                RequestEventArg<OrderRequestType> orderRequest = 
                    m_OrderRequests.Get(OrderRequestType.ChangeConfirm, orderToModify.Instrument, orderToModify.Id, orderToModify.Side, orderToModify );
                HubEventEnqueue(orderRequest);
                return true;
            }
            return false;
        }// TryChangeOrderQty()
        //
        //
        //
        // *****************************************
        // ****    TryChangeOrderPriceAndQty()  ****
        // *****************************************
        public override bool TryChangeOrderPriceAndQty(Order orderToModify, int newQty, int newIPrice)
        {
            if(base.TryChangeOrderPriceAndQty(orderToModify, newQty, newIPrice))
            { // successfuly requested changed
                Log.NewEntry(LogLevel.Minor, "TryChangeOrderPriceAndQty: {0}@{1} --> {2}@{3} for order {4}.",orderToModify.WorkingQtyPending,orderToModify.PricePending, newQty, newIPrice, orderToModify);

                // Create the confirmation request
                orderToModify.OriginalQtyConfirmed = newQty;
                orderToModify.IPriceConfirmed = newIPrice;
                //TryProcessOrderUpdateRequest(
                RequestEventArg<OrderRequestType> orderRequest = 
                    m_OrderRequests.Get(OrderRequestType.ChangeConfirm, orderToModify.Instrument, orderToModify.Id, orderToModify.Side, orderToModify);
                HubEventEnqueue(orderRequest);
                return true;
            }
            return false;
        }//TryChangeOrderPriceAndQty()
        //
        //
        //
        //
        #endregion//Public Methods


        #region HubEventHandler and Processing
        // *****************************************
        // ****         HubEventHandler()       ****
        // *****************************************
        protected override void HubEventHandler(EventArgs[] eventArgList)
        {
            //RequestEventArg<RequestCodeSim> simUpdate = null;

            foreach (EventArgs eventArgs in eventArgList)
            {
                //
                // Process Requests
                //
                Type eventType = eventArgs.GetType();
                if (eventType == typeof(RequestEventArg<RequestCode>))
                {
                    RequestEventArg<RequestCode> request = (RequestEventArg<RequestCode>)eventArgs;
                    switch (request.RequestType)
                    {
                        case RequestCode.ServiceStateChange:
                            ProcessServiceStateChange(request);
                            break;
                        case RequestCode.CreateBook:
                            ProcessCreateBook(request);
                            break;
                        default:
                            break;
                    }       
                }
                //
                // Process Confirmation messages
                //
                else if (eventType == typeof(RequestEventArg<OrderRequestType>))
                {
                    RequestEventArg<OrderRequestType> request = (RequestEventArg<OrderRequestType>) eventArgs;
                    switch(request.RequestType)
                    {
                        case OrderRequestType.AddConfirm:
                            TryProcessOrderUpdateRequest(request);
                            break;
                        case OrderRequestType.ChangeConfirm:
                            TryProcessOrderUpdateRequest(request);
                            break;
                        case OrderRequestType.DeleteConfirm:
                            TryProcessOrderUpdateRequest(request);
                            break;
                        default:
                            break;
                    }
                }
                //
                // Market events
                //
                else if (eventType == typeof(UVBooks.InstrumentChangeArgs))
                {
                    // TODO: Check to see if the market change will fill any of our orders.                    
                    UVBooks.InstrumentChangeArgs e = (UVBooks.InstrumentChangeArgs)eventArgs;
                    m_MarketInstrumentIdChangedList.Clear();      // load ids for instruments we need to check.
                    foreach (KeyValuePair<int,UVBooks.InstrumentChange> pair in e.ChangedInstruments)
                    {
                        if (pair.Value.MarketDepthChanged[QTMath.BidSide].Contains(0) || pair.Value.MarketDepthChanged[QTMath.AskSide].Contains(0))
                            m_MarketInstrumentIdChangedList.Add(pair.Value.InstrumentID);
                    }
                    if (m_MarketInstrumentIdChangedList.Count > 0)
                        SimulateFills(m_MarketInstrumentIdChangedList);
                }
                else if (eventType == typeof(UVBooks.MarketStatusEventArgs))
                {
                    UVBooks.MarketStatusEventArgs mktStatusEventArg = (UVBooks.MarketStatusEventArgs)eventArgs;
                }
                else if ( eventType == typeof(UVMkt.FoundServiceEventArg))
                {

                }
                else if ( eventType == typeof(UVMkt.MarketStatusChangedEventArg) )
                {

                }
            }//next eventArgs
        }// HubEventHandler()
        //
        //
        /// <summary>
        /// Process the request to change the service state.
        /// </summary>
        /// <param name="request"></param>
        private void ProcessServiceStateChange(RequestEventArg<RequestCode> request)
        {
            ServiceStates nextState = m_ServiceState;                   // change this if you want to move to a new state.
            ServiceStates requestedState = (ServiceStates)request.Data[0];
            Log.NewEntry(LogLevel.Major, "ProcessStateChange: [{1}] Processing {0}.", request, m_ServiceState);
            if (m_ServiceState >= ServiceStates.Stopped)
            {   // Ok.  We must shut down now.
                m_Requests.Recycle(request);
                base.Stop();
            }
            else if (m_ServiceState >= ServiceStates.Stopping)
            {   // We are trying to stop. But havent stopped yet.                
                // Check that we can stop now.
                bool isReadyToStop = true;                      // todo: do the needed checks here.
                if (isReadyToStop)
                {
                    nextState = ServiceStates.Stopped;          // Okay. We can stop - set my state.
                    this.HubEventEnqueue(request);              // pulse this request again immediately.
                }
                else
                    m_PendingQueue.AddPending(request, 1);           // wait, then try to stop again.
            }
            else if (m_ServiceState >= ServiceStates.Running)
            {   // Here we are in Running state or better.
                if (requestedState >= ServiceStates.Stopping)
                {   // user wants to stop now.
                    nextState = ServiceStates.Stopping;
                    this.HubEventEnqueue(m_Requests.Get(RequestCode.ServiceStateChange, ServiceStates.Stopped));
                }
                else
                {   // Right now this is the only active state.
                    // However, we could go onto other states.. like paused, if desired in future.
                    Log.NewEntry(LogLevel.Warning, "ProcessStateChange: Ignoring request {0}", request);
                    m_Requests.Recycle(request);
                }
            }
            else if (m_ServiceState >= ServiceStates.Started)
            {   // We are atleast started now.
                if (requestedState >= ServiceStates.Stopping)
                {   // User wants to stop.  Thats okay.
                    nextState = ServiceStates.Stopping;
                    this.HubEventEnqueue(m_Requests.Get(RequestCode.ServiceStateChange, ServiceStates.Stopped));
                }
                else if (requestedState >= ServiceStates.Running)
                {   // Try to connect all services we need to be running.
                    // Locate services now.
                    List<IService> iServices;                    
                    if (m_Market == null)
                    {
                        Log.NewEntry(LogLevel.Major, "ProcessStateChange: Starting.  Found Market. ");
                        m_Services.GetServices(typeof(MarketHubs.MarketHub));
                        iServices = m_Services.GetServices(typeof(MarketHubs.MarketHub));
                        if (iServices.Count > 0)
                        {
                            m_Market = (MarketHubs.MarketHub)iServices[0];
                            m_Market.FoundResource += new EventHandler(HubEventEnqueue);        // subscribe to found resources.
                            m_Market.MarketStatusChanged += new EventHandler(HubEventEnqueue);
                            m_Market.InstrumentChanged += new EventHandler(HubEventEnqueue);
                        }
                        else
                            Log.NewEntry(LogLevel.Error, "ProcessStateChange: Failed to locate Market hub.");
                    }

                    // Try to connect.
                    bool isReadyToRun = true;
                    isReadyToRun = (m_Market != null) && isReadyToRun;

                    if (isReadyToRun)
                    {   // Transition to Running state!
                        nextState = ServiceStates.Running;
                        if (requestedState > ServiceStates.Running)
                            HubEventEnqueue(request);                   // resubmit request since its more than Running.
                        else
                            m_Requests.Recycle(request);
                    }
                    else
                    {
                        Log.NewEntry(LogLevel.Major, "ProcessStateChange: Cannot proceed to state {0}. Not connected. Wait and try again.", requestedState);
                        m_PendingQueue.AddPending(request, 2);
                    }
                }
                else
                {

                }

            }
            else if (m_ServiceState >= ServiceStates.Unstarted)
            {   // We are marked as unstarted, but since we are here, we must at least be started.
                nextState = ServiceStates.Started;
                if (requestedState > ServiceStates.Started)
                    this.HubEventEnqueue(request);             // resubmit this request since user requested more.
                else
                    m_Requests.Recycle(request);
            }
            else
                Log.NewEntry(LogLevel.Major, "ProcessStateChange: Unknown service state {0}", m_ServiceState);// This should never happen

            // Exit - report service state change if any.
            if (m_ServiceState != nextState)
            {                                                       // Our service state has changed.                
                ServiceStates prevState = m_ServiceState;           // save previous state.
                m_ServiceState = nextState;                         // accept new state.
                OnServiceStateChanged(prevState, m_ServiceState);
            }
        }//ProcessServiceStateChange()
        //
        //
        //
        // *****************************************
        // ****         ProcessCreateBook()     ****
        // *****************************************
        /// <summary>
        /// Process to initialize a single order book.
        /// </summary>
        /// <param name="request"></param>
        private void ProcessCreateBook(RequestEventArg<RequestCode> request)
        {
            // Extract order book and validate request.
            if (request.Data.Count < 1)
            {
                Log.NewEntry(LogLevel.Warning, "ProcessCreateBook: Fail to find book in request.");
                return;
            }
            OrderBook orderBook = request.Data[0] as OrderBook;
            if (orderBook == null)
            {
                Log.NewEntry(LogLevel.Warning, "ProcessCreateBook: Fail to find valid order book in request.");
                return;
            }

            // Locate the OrderInstrument for this book.
            OrderInstrument orderInstrument = null;
            if (!m_OrderInstruments.TryGetValue(orderBook.Instrument, out orderInstrument))
            {   // We do not have an order instrument for this Instrument.
                InstrumentDetails instrumentDetails;
                if (!m_Market.TryGetInstrumentDetails(orderBook.Instrument, out instrumentDetails))
                {   // We don't know this instrument yet.
                    // Request information from market, and set this to pending.
                    Log.NewEntry(LogLevel.Minor, "ProcessCreateBook: Requesting instrument details for {0} OrderBookID {1}. Will try again later.",
                        orderBook.Instrument, orderBook.BookID);
                    m_Market.RequestInstruments(orderBook.Instrument);
                    m_PendingQueue.AddPending(request, 2);
                    return;
                }
                else
                {   // We have the instrument details, so create the OrderInstrument now
                    Log.NewEntry(LogLevel.Minor, "ProcessCreateBook: Attempting to Process OrderBookID {0}", orderBook.BookID);
                    orderInstrument = new OrderInstrument(orderBook.Instrument, instrumentDetails, m_OrderRecycleFactory);
                    if (m_OrderInstruments.TryAdd(orderBook.Instrument, orderInstrument))
                    {
                        Log.NewEntry(LogLevel.Minor, "ProcessCreateBook: Created new OrderInstrument {0}.", orderBook.Instrument);
                        int instrId = -1;
                        if ( m_Market!=null && m_Market.TryLookupInstrumentID(orderBook.Instrument, out instrId) == false )
                        {   // Subscribe to this market instrument.
                            // Order sim needs market updates for each instrument with orders.
                            m_Market.RequestInstrumentSubscription(orderBook.Instrument);
                        }

                    }
                    else
                    {   // This could only happen if the orderInstrument somehow just now showed up spontaneously.
                        // That could happen if we allow other threads to add new OrderInstruments to list.
                        Log.NewEntry(LogLevel.Minor, "ProcessCreateBook: Failed to add new OrderInstrument {0}. Will try again later.",
                            orderBook.Instrument);
                        m_PendingQueue.AddPending(request, 1);
                        return;
                    }
                }
                // If we get here, lets try to add our new book to the order instrument.
                if (orderInstrument != null)
                {
                    orderInstrument.TryAddBook(orderBook);

                    // Simulator also needs to keep a copy of all books to simulate fills.
                    List<OrderBook> orderBookList = null;
                    if ( ! m_SimOrderBooks.TryGetValue(orderInstrument.Instrument,out orderBookList))
                    {
                        orderBookList = new List<OrderBook>();
                        m_SimOrderBooks.Add(orderInstrument.Instrument,orderBookList);
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
        //
        //
        #endregion//HubEventHandler and processing methods


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        //
        //
        //
        // *************************************************
        // ****             Simulate Fills()            ****
        // *************************************************
        private void SimulateFills( List<int> marketInstrList )
        {
            UVBooks.Book aBook;
            if ( m_Market.TryEnterReadBook(out aBook) )
            {
                foreach (int instrId in marketInstrList)
                {
                    UVBooks.Market market = null;
                    if (! aBook.Instruments.TryGetValue(instrId, out market))
                    {
                        Log.NewEntry(LogLevel.Minor,"SimulateFills: Failed to obtain market for mkt instr ID {0}",instrId);
                        continue;
                    }

                    InstrumentName instrName = market.Name;                                        
                    List<OrderBook> orderBooks = null;
                    if ( m_SimOrderBooks.TryGetValue(instrName,out orderBooks))
                    {
                        double tickSize = m_OrderInstruments[instrName].Details.TickSize;
                        foreach (OrderBook orderBook in orderBooks)
                        {   //
                            // Try to fill this order book.
                            //
                            for (int orderSide=0; orderSide < 2; orderSide++)
                            {
                                int orderSign = QTMath.MktSideToMktSign( orderSide );
                                int otherSide = (orderSide + 1) % 2;
                                int ourMarketPrice = (int) Math.Round( market.Price[orderSide][0] / tickSize );
                                int oppMarketPrice = (int) Math.Round( market.Price[otherSide][0] / tickSize);
                                int orderRank = 0;                      // only fill top for now
                                int orderPrice;
                                if ( orderBook.TryGetIPriceByRank(orderSide, orderRank, out orderPrice) )
                                {
                                    if ( (oppMarketPrice - orderPrice) * orderSign  <= 0 ) 
                                    {   // filled by crossing market.
                                        m_OrdersWorkspace.Clear();
                                        orderBook.GetOrdersByRank(orderSide, orderRank, ref m_OrdersWorkspace);
                                        FillTheseOrders( ref m_OrdersWorkspace );
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
                                    foreach (Order order in m_OrdersWorkspace)
                                        base.m_OrderRecycleFactory.Recycle(order);                                    

                                }

                            }
                        }

                    }
                    /*
                    OrderInstrument orderInstr = null;
                    if (!m_OrderInstruments.TryGetValue(instrName, out orderInstr))
                    {
                        Log.NewEntry(LogLevel.Minor, "SimulateFills: Failed to obtain order instr for {0}",instrName);
                        continue;
                    }
                    orderInstr.
                    */

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
            foreach(Order order in ordersToFill)
            {
                if (order.OrderStateConfirmed != OrderState.Submitted)
                    continue;

                // Create the fill
                UV.Lib.Fills.Fill aFill = new Fills.Fill();
                aFill.ExchangeTime = m_Market.LocalTime;
                aFill.LocalTime = aFill.ExchangeTime;
                aFill.Price = order.PriceConfirmed;
                aFill.Qty = order.WorkingQtyConfirmed;
                Log.NewEntry(LogLevel.Major, "OrderFilled: Fill={1} Order={0}.", order, aFill);
                // Create the fill event
                UV.Lib.Fills.FillEventArgs fillEvent = new UV.Lib.Fills.FillEventArgs(aFill, order.Id, order.Instrument, true);
                // Create update event
                RequestEventArg<OrderRequestType> request;
                request = base.m_OrderRequests.Get(OrderRequestType.FillConfirm, order.Instrument, order.Id, order.Side, fillEvent);
                TryProcessOrderUpdateRequest(request);
            }
        }//FillTheseOrders()
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
        #endregion//Event Handlers


        #region IStringifiable
        // *****************************************************************
        // ****                     IStringifiable                      ****
        // *****************************************************************
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
            foreach (KeyValuePair<string, string> keyVal in attributes)
            {
                if (keyVal.Key.Equals("ShowLog") && bool.TryParse(keyVal.Value, out isTrue))
                    Log.IsViewActive = isTrue;
            }
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {
        }
        //
        //
        #endregion//Event Handlers

    }//end class
}
