using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
//using System.Linq;
//using System.Text;

namespace UV.Lib.OrderBooks
{
    using UV.Lib.Hubs;
    using UV.Lib.Utilities;
    using UV.Lib.Products;
    //using UV.Lib.IO.Xml;            // IStringify

    using UV.Lib.Application;

    /// <summary>
    ///
    /// Notes: 
    ///     1) This class is abstract because it does not implement HubEventHandler().
    /// </summary>
    public abstract class OrderBookHub : Hub, IService
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      

        //
        // OrderBook management
        //
        protected ConcurrentDictionary<InstrumentName, OrderInstrument> m_OrderInstruments;

        //
        // Internal workspace.
        //
        protected ServiceStates m_ServiceState = ServiceStates.Unstarted;
        protected RequestFactory<RequestCode> m_Requests = new RequestFactory<RequestCode>(100);
        protected EventWaitQueueLite m_PendingQueue = null;                 // place for events we want to resubmit later.
        public RequestFactory<OrderRequestType> m_OrderRequests = new RequestFactory<OrderRequestType>(100);  // public so can be called from listener
        public RecycleFactory<Order> m_OrderRecycleFactory = new RecycleFactory<Order>(100);   // public so order listener can also utilize.
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public OrderBookHub(string hubName)
            : base(hubName, AppServices.GetInstance().Info.LogPath, false, LogLevel.ShowAllMessages)
        {
            m_OrderInstruments = new ConcurrentDictionary<InstrumentName, OrderInstrument>();

            m_PendingQueue = new EventWaitQueueLite(this.Log);
            m_PendingQueue.ResubmissionReady += new EventHandler(this.HubEventEnqueue);
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
        //
        //
        // *****************************************
        // ****         Create OrderBook()      ****
        // *****************************************
        /// <summary>
        /// External caller request for an order book for a specific instrument.
        /// Once he has the order book, he can subscribe to its events for updates.
        /// Procedure:
        ///     The creation of books must be done asynchronously since we must
        ///     request for instrument details and subscribe to order callbacks
        ///     (in the implementing subclass).
        ///     The OrderBook is created instantly, returned to the caller, so that
        ///     he will know when its initialized.  Also, the subclass can return any
        ///     sort of order book it wants to implement, as long as it inherits from 
        ///     OrderBook.
        /// </summary>
        /// <param name="instrumentName"></param>
        public virtual OrderBook CreateOrderBook(InstrumentName instrumentName)
        {
            OrderBook orderBook = new OrderBook(instrumentName);
            this.HubEventEnqueue(m_Requests.Get(RequestCode.CreateBook, orderBook));
            return orderBook;
        }//CreateOrderBook()
        //
        //
        // *****************************************
        // ****         TryCreateOrder          ****
        // *****************************************
        /// <summary>
        /// The caller wants to create an order.  After creation, the order must be submitted 
        /// to the specific OrderBook to be managed.
        /// </summary>
        /// <param name="instrumentName"></param>
        /// <returns></returns>
        public virtual bool TryCreateOrder(InstrumentName instrumentName, int tradeSide, int iPrice, int qty, out Order newOrder)
        {
            newOrder = null;
            if ((UV.Lib.Utilities.QTMath.MktSideToMktSign(tradeSide) * qty) < 0)
            {   // this means our signs are incorrect!
                Log.NewEntry(LogLevel.Error, "Attempt to Create Order For Instrument {0} Failed, Mismatched Sides and Qtys", instrumentName);
                return false;
            }
            OrderInstrument orderInstrument;
            if (m_OrderInstruments.TryGetValue(instrumentName, out orderInstrument))
            {
                newOrder = m_OrderRecycleFactory.Get();
                newOrder.Instrument = instrumentName;
                newOrder.Id = Order.GetNextId();
                newOrder.Side = tradeSide;
                newOrder.OriginalQtyPending = qty;
                newOrder.IPricePending = iPrice;
                newOrder.TickSize = orderInstrument.Details.TickSize;
                newOrder.OrderType = OrderType.LimitOrder;
            }
            // Exit.
            return (newOrder != null);
        }//CreateOrderBook()
        //
        //
        //
        // *****************************************
        // ****         Try Submit Order()      ****
        // *****************************************
        /// <summary>
        /// Called by user to submit an order to the market.
        /// 
        /// This method is slight different then all others. 
        /// Since the orders doesn't exist yet, we have to directly call 
        /// OrderInstrument here and add it.  This completes all states
        /// and nothing else needs to be done to the order besides submit
        /// to the market.
        /// </summary>
        /// <param name="orderBookID"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        public virtual bool TrySubmitOrder(int orderBookID, Order order)
        {
            OrderInstrument orderInstrument;
            if (!m_OrderInstruments.TryGetValue(order.Instrument, out orderInstrument))
                return false;
            return orderInstrument.TryAddOrder(orderBookID, order); 
        }
        //
        //
        //
        // *****************************************
        // ****         Try Delete Order()      ****
        // *****************************************
        /// <summary>
        /// called by user to delete an order.
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public virtual bool TryDeleteOrder(Order order)
        {
            if (order.OrderStatePending == OrderState.Dead)
            {
                Log.NewEntry(LogLevel.Warning, "TryCancelOrder: {0} - Was already processed to be cancelled", order);
                return false;
            }
            return TryProcessOrderUpdateRequest(
                m_OrderRequests.Get(OrderRequestType.DeleteRequest, order.Instrument, order.Id, order.Side, order));
        }
        //
        //
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
        public virtual bool TryChangeOrderPrice(Order orderToModify, int newIPrice)
        {
            orderToModify.IPricePending = newIPrice;            // assign new IPrice to pending.
            return TryProcessOrderUpdateRequest(
                m_OrderRequests.Get(OrderRequestType.ChangeRequest, orderToModify.Instrument, orderToModify.Id, orderToModify.Side, orderToModify)); // sent order to be processed.
        }
        //
        //
        //
        // *****************************************
        // ****       TryChangeOrderQty()       ****
        // *****************************************
        /// <summary>
        /// Called user to change the pending qty of a given order.
        /// If the newqty submitted will result in this order working a zero qty,
        /// false will be returned and the order will be deleted!
        /// </summary>
        /// <param name="orderToModify"></param>
        /// <param name="newQty">SIGNED QTY</param>
        /// <returns>false if sign is incorrect or order is now zero qty and deleted</returns>
        public virtual bool TryChangeOrderQty(Order orderToModify, int newQty)
        {
            if ((UV.Lib.Utilities.QTMath.MktSideToMktSign(orderToModify.Side) * newQty) < 0)
            {   //this means our signs are incorrect!
                Log.NewEntry(LogLevel.Error, "Attempt to change Order {0} qty to the opposite sign, rejecting attempted change", orderToModify);
                return false;
            }
            orderToModify.OriginalQtyPending = newQty;
            return TryProcessOrderUpdateRequest(
                m_OrderRequests.Get(OrderRequestType.ChangeRequest, orderToModify.Instrument, orderToModify.Id, orderToModify.Side, orderToModify));
        }// TryChangeOrderQty() 
        //
        //
        // *****************************************
        // ****    TryChangeOrderPriceAndQty()  ****
        // *****************************************
        /// <summary>
        /// Caller by user to change the the price and qty of an order.
        /// </summary>
        /// <param name="orderToModify"></param>
        /// <param name="newQty"></param>
        /// <param name="newIPrice"></param>
        /// <returns></returns>
        public virtual bool TryChangeOrderPriceAndQty(Order orderToModify, int newQty, int newIPrice)
        {
            if ((UV.Lib.Utilities.QTMath.MktSideToMktSign(orderToModify.Side) * newQty) < 0)
            {   //this means our signs are incorrect!
                Log.NewEntry(LogLevel.Error, "Attempt to change Order {0} qty to the opposite sign, rejecting attempted change", orderToModify);
                return false;
            }
            orderToModify.OriginalQtyPending = newQty;
            orderToModify.IPricePending = newIPrice;
            return TryProcessOrderUpdateRequest(
                m_OrderRequests.Get(OrderRequestType.ChangeRequest, orderToModify.Instrument, orderToModify.Id, orderToModify.Side, orderToModify)); // we succesfully sent order to be processed
        }//TryChangeOrderPriceAndQty()
        //
        // *********************************************
        // **** Try Process Order Update Request()  ****
        // *********************************************
        //
        /// <summary>
        /// Called by an external thread to process an update request for one 
        /// of our orders.
        /// </summary>
        /// <param name="orderUpdateReq"></param>
        /// <returns></returns>
        public virtual bool TryProcessOrderUpdateRequest(EventArgs orderUpdateReq)
        {
            bool isSuccess = false;
            if (orderUpdateReq is RequestEventArg<OrderRequestType>)
            {
                RequestEventArg<OrderRequestType> request = (RequestEventArg<OrderRequestType>)orderUpdateReq;
                InstrumentName instrName = (InstrumentName)request.Data[0];
                OrderInstrument orderInstr;
                if (m_OrderInstruments.TryGetValue(instrName, out orderInstr))
                { // we found the order instrument
                    //orderInstr.ProcessRequest(request);
                    isSuccess = true;
                }
                else
                {
                    Log.NewEntry(LogLevel.Warning, "TryProcessOrderUpdateRequest: Order Instrument for {0} not found", instrName);

                }
                if(request.Data.Count > 3 && request.Data[3] is Order) // this mean an order is attached to this request.
                    m_OrderRecycleFactory.Recycle((Order)request.Data[3]);  // recycle it here prior to recycling the request
                m_OrderRequests.Recycle(request);                           // recycle all requests
            }
            return isSuccess;
        }
        //
        //
        // *****************************************************
        // ****         RecycleOrderList()             ****
        // *****************************************************
        /// <summary>
        /// Called by a user who would like a list of orders to be recycled using a hub's
        /// order recycling factory properly by recycling all of the copied orders 
        /// and then clearing the list.
        /// </summary>
        public void RecycleOrderList(List<Order> orderList)
        {
            foreach(Order order in orderList)
                m_OrderRecycleFactory.Recycle(order);
            orderList.Clear();
        }
        // *****************************************************
        // ****         RecycleOrderDictionry()             ****
        // *****************************************************
        /// <summary>
        /// Called by a user who would like a dictionary of orders to be recycled using a hub's
        /// order recycling factory properly by recycling all of the copied orders 
        /// and then clearing the dictionary.
        /// </summary>
        /// <param name="orderDictionary"></param>
        public void RecycleOrderDictionry(Dictionary<int, Order> orderDictionary)
        {
            foreach (KeyValuePair<int, Order> pair in orderDictionary)
                m_OrderRecycleFactory.Recycle(pair.Value);
            orderDictionary.Clear();
        }
        #endregion//Public Methods


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
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


        #region IService
        // *****************************************************************
        // ****                     IService                            ****
        // *****************************************************************
        public string ServiceName
        {
            get { return m_HubName; }
        }
        //
        //
        // *****************************************
        // ****     On ServiceState Changed()   ****
        // *****************************************
        //
        public event EventHandler ServiceStateChanged;
        //
        /// <summary>
        /// This needs to be called by the implementing subclass after the m_ServiceState 
        /// is changed.
        /// </summary>
        /// <param name="prevState"></param>
        /// <param name="currentState"></param>
        protected void OnServiceStateChanged(ServiceStates prevState, ServiceStates currentState)
        {
            // Report the service change.
            Log.NewEntry(LogLevel.Major, "OnServiceStateChanged: {0} -> {1}", prevState, currentState);
            if (this.ServiceStateChanged != null)
            {
                ServiceStateEventArgs eventArg = new ServiceStateEventArgs(this, currentState, prevState);
                ServiceStateChanged(this, eventArg);
            }
        }//OnServiceStateChange()
        //
        //
        //
        // *********************************************
        // ****             Start()                 ****
        // *********************************************
        /// <summary>
        /// Called by outside thread to start our hub thread.
        /// </summary>
        public override void Start()
        {
            this.HubEventEnqueue(m_Requests.Get(RequestCode.ServiceStateChange, ServiceStates.Started));
            base.Start();
        }//Start()
        //
        //
        // *********************************************
        // ****             Connect()               ****
        // *********************************************
        /// <summary>
        /// This called after all fundamental services have been created and started.
        /// This request to connect signals to us that we should search for any 
        /// services that we will need, and connect to them.
        /// </summary>
        public virtual void Connect()
        {
            this.HubEventEnqueue(m_Requests.Get(RequestCode.ServiceStateChange, ServiceStates.Running));
        }
        //
        // *********************************************
        // ****             RequestStop()           ****
        // *********************************************
        /// <summary>
        /// External call for this hub to shutdown.
        /// </summary>
        public override void RequestStop()
        {
            this.HubEventEnqueue(m_Requests.Get(RequestCode.ServiceStateChange, ServiceStates.Stopped));
        }
        //
        //
        //
        //
        //
        //
        #endregion//Event Handlers







    }//end class
}
