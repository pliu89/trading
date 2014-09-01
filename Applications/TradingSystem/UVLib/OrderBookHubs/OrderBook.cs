using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.OrderBookHubs
{
    using UV.Lib.Products;
    using UV.Lib.Fills;
    using UV.Lib.Utilities;

    /// <summary>
    /// Orderbook for a single instrument.
    /// Notes:
    ///     1) Since outside users will have a copy of the order book, 
    ///         we need to classes; one they see and another where public
    ///         methods that manipulate the book are contained so that illegal 
    ///         operations can't be done by Strategies.
    ///         For now, separate these in two different regions below!
    /// </summary>
    public class OrderBook
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // Identification of this book.
        protected int m_BookID = -1;                                    // -1 means book not fully connected to exchange yet.
        private static int m_NextId = 0;                                // next id
        public readonly InstrumentName Instrument;                      // Each book contains orders for a single instrument.
        //protected bool m_IsReady = false;                             // Flag (set by Hub) when Orders can be sent/received by this book.

        // Book management
        private OrderPage[] m_LiveOrders = null;                        // Orders submitted to market. One page for each side of mkt.
        private OrderPage[] m_DeletedOrders = null;

        // Risk Accounting
        public int m_TotalWorkingOrderQty = 0;                          // a way of keeping track of total working Qty of our orders.

        // Constants 
        private const int NSides = 2;                                   // There are two sides, BuySide, SellSide.

        // State flag
        private bool m_IsReady;

        // temp order workspace
        Dictionary<int, Order> m_OrderWorkspace = new Dictionary<int, Order>(); // clear before each use!
        #endregion// members

        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public OrderBook(InstrumentName instrument, RecycleFactory<Order> orderRecycleFactory)
        {
            // Set Id when order book is connected and initialize by hub.
            this.m_BookID = System.Threading.Interlocked.Increment(ref OrderBook.m_NextId);
            this.Instrument = instrument;

            // Initialize the lists.
            m_LiveOrders = new OrderPage[NSides];                        // a page for each side of mkt.
            m_DeletedOrders = new OrderPage[NSides];
            for (int side = 0; side < NSides; ++side)
            {
                m_LiveOrders[side] = new OrderPage(side, this, orderRecycleFactory);
                m_DeletedOrders[side] = new OrderPage(side, this, orderRecycleFactory);
            }
        }
        //
        //       
        #endregion//Constructors

        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        public bool IsReady
        {
            get { return m_IsReady; }
            set { m_IsReady = value; }
        }
        public int BookID
        {
            get { return m_BookID; }
        }
        //
        #endregion//Properties

        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        // *************************************************
        // ****                 Try Add()               ****
        // *************************************************
        /// <summary>
        /// Adds new order to the order book. 
        /// </summary>
        /// <param name="newOrder"></param>
        /// <returns>False if the add failed.</returns>
        public bool TryAdd(Order newOrder, bool addToDeadBook)
        {
            if (addToDeadBook)
                return m_DeletedOrders[newOrder.Side].TryAdd(newOrder);
            else
            {
                return m_LiveOrders[newOrder.Side].TryAdd(newOrder);
            }
        }
        public bool TryAdd(Order newOrder)
        {
            return m_LiveOrders[newOrder.Side].TryAdd(newOrder);
        }
        // 
        // *************************************************
        // ****             Try Get()                   ****
        // *************************************************
        /// <summary>
        /// This returns the order object *without* removing it from its location.
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="order"></param>
        /// <param name="includeDeletedBook"></param>
        /// <returns></returns>
        public bool TryGet(int orderId, out Order order, bool includeDeletedBook)
        {
            order = null;
            Order foundOrder = null;
            // Search thru live orders
            for (int side = 0; side < NSides; ++side)
                if (m_LiveOrders[side].TryGet(orderId, out foundOrder))
                {
                    order = foundOrder;
                    return true;
                }
            if (includeDeletedBook)
            {   // Search thru deleted orders too.
                for (int side = 0; side < NSides; ++side)
                    if (m_DeletedOrders[side].TryGet(orderId, out foundOrder))
                    {
                        order = foundOrder;
                        return true;
                    }
            }
            // Exit
            return false;
        }//TryGet()
        public bool TryGet(int orderId, out Order order)
        {
            return this.TryGet(orderId, out order, false);
        }
        //
        // *************************************************
        // ****             Try Delete()                ****
        // *************************************************
        /// <summary>
        /// This can fail only if the orderId is not found.
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public bool TryDelete(int orderId)
        {
            bool isSuccess = false;
            // Find page containing this order.
            OrderPage page = null;
            for (int side = 0; side < NSides; ++side)
                if (m_LiveOrders[side].Contains(orderId))
                {
                    page = m_LiveOrders[side];
                    break;
                }

            // Move the order from one page to the other.
            if ( page != null )
            {   // I found the page
                OrderPage deadPage = m_DeletedOrders[page.OrderSide];
                isSuccess = page.TryMove(orderId, deadPage);
                if (isSuccess)
                {   
                    // TODO: COnvert these to requests
                    deadPage.TryChangeState(orderId, OrderState.Dead,false);
                    deadPage.TryChangeState(orderId, OrderState.Dead,true);
                }
            }
            return isSuccess;
        }//TryDelete
        //
        //
        //
        //
        // *************************************************
        // ****             Get Order By Rank()         ****
        // *************************************************
        /// <summary>
        /// Searches for all orders that are rank R away from market
        /// and loads them into orders list (without removing them from book).
        /// Rank R=0 is the price level that is closest to the other side of the market, 
        /// most likely to be filled!
        /// </summary>
        /// <param name="side"></param>
        /// <param name="rank"></param>
        /// <param name="orders"></param>
        public void GetOrdersByRank(int side, int rank, ref List<Order> orders)
        {
            m_LiveOrders[side].GetByRank(rank, ref orders);
        }
        //
        // ******************************************
        // ****     Get IPrice By Rank()       ****
        // ******************************************
        public bool TryGetIPriceByRank(int side, int rank, out int iPrice)
        {
            if (m_LiveOrders[side].TryGetIPriceByRank(rank, out iPrice))
            {
                iPrice = - QTMath.MktSideToMktSign(side) * iPrice;
                return true;
            }
            return false;
        }
        //
        // ******************************************
        // ****     Get PriceQty By Rank()       ****
        // ******************************************
        /// <summary>
        /// Searches thru all orders at rank R away from market and returns
        /// their common price and total quantity.
        /// </summary>
        /// <param name="side"></param>
        /// <param name="rank"></param>
        /// <param name="price"></param>
        /// <param name="qty"></param>
        /*
        public void GetPriceQtyByRank(int side, int rank, out double price, out int qty)
        {
            List<Order> orders = new List<Order>();             // Todo: use an internal workspace in future.
            m_LiveOrders[side].GetByRank(rank, ref orders);            

            // Tally results
            qty = 0;
            price = 0;
            if (orders.Count > 0)
                price = orders[0].Price;
            else 
                return;
            foreach (Order order in orders)
               qty += order.WorkingQty;
        }
        public void GetPriceQtyByRank(int side, int rank, out int iPrice, out int qty)
        {
            List<Order> orders = new List<Order>();             // Todo: use an internal workspace in future.
            m_LiveOrders[side].GetByRank(rank, ref orders);

            // Tally results
            qty = 0;
            iPrice = 0;
            if (orders.Count > 0)
                iPrice = orders[0].IPrice;
            else
                return;
            foreach (Order order in orders)
                qty += order.WorkingQty;
        }
        */
        //
        //
        // *************************************************
        // ****             Get Orders By Side()         ****
        // *************************************************
        /// <summary>
        /// Searches for all order on a given side of the market. Handing them 
        /// back to the caller in a dictionary.
        /// </summary>
        /// <param name="side"></param>
        /// <param name="orders"></param>
        public void GetOrdersBySide(int side, ref Dictionary<int, Order> orders)
        {
            m_LiveOrders[side].GetAll(ref orders);
        }
        //
        //
        // *************************************************
        // ****     Get Orders By UserDefinedTag()      ****
        // *************************************************
        /// <summary>
        /// Searches for al liver  orders on a given side of the market, 
        /// with a specific tag.
        /// </summary>
        /// <param name="side"></param>
        /// <param name="orders"></param>
        public void GetOrdersByUserDefinedTag(int side, int tag, ref List<Order> orders)
        {
            m_LiveOrders[side].GetByUserDefinedTag(tag, ref orders);
        }
        //
        //
        // *************************************************
        // ****                 Count()                 ****
        // *************************************************
        /// <summary>
        /// </summary>
        public int Count(int side)
        {
            return m_LiveOrders[side].Count();
        }
        public int Count()
        {
            return m_LiveOrders[Order.BuySide].Count() + m_LiveOrders[Order.SellSide].Count();
        }

        //
        //
        //
        //
        //
        // *************************************************
        // ****          UpdateTotalWorkingQty()        ****
        // *************************************************
        /// <summary>
        /// Caller would like to book to update its total working quantity for all live orders
        /// in a confirmed submitted state.
        /// </summary>
        public void UpdateTotalWorkingQty()
        {
            m_TotalWorkingOrderQty = 0;
            for (int side = 0; side < NSides; side++)
            {
                m_OrderWorkspace.Clear();
                m_LiveOrders[side].GetAll(ref m_OrderWorkspace);
                foreach (KeyValuePair<int, Order> pair in m_OrderWorkspace)
                {
                    if (pair.Value.OrderStateConfirmed == OrderState.Submitted && pair.Value.OrderStatePending == OrderState.Submitted)
                        m_TotalWorkingOrderQty += Math.Abs(pair.Value.WorkingQtyConfirmed);
                }
            }
        }
        //
        //
        // *************************************************
        // ****              ProcessRequest()           ****
        // *************************************************
        // orderReq.Data[0] = InstrumentName
        // orderReq.Data[1] = orderId
        // orderReq.Data[2] = orderside
        // orderReq.Data[3] = Order/fill/other data?
        /// <summary>
        /// Threadsafe method to request manipulation of an internal order.
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="orderReq"></param>
        public void ProcessRequest(int orderId, RequestEventArg<OrderRequestType> orderReq)
        {
            int orderSide = (int)orderReq.Data[2];
            m_LiveOrders[orderSide].ProcessRequest(orderId, orderSide, orderReq);   // what if the order is already dead?(do we even care if it is)
        }
        #endregion//Public Methods

        #region no Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        #endregion//Private Methods

        #region Event Triggers
        // *****************************************************************
        // ****                     Event Triggers                     ****
        // *****************************************************************
        //
        //
        //
        // ****              Order Filled               ****
        //
        public event EventHandler OrderFilled;
        //
        /// <summary>
        /// After any order status updated and after we have already updated our quoting orders, 
        /// this event is fired first before all others.
        /// </summary>
        public void OnOrderFilled(FillEventArgs fillEventArgs)
        {
            if (this.OrderFilled != null)
            {
                this.OrderFilled(this, fillEventArgs);
            }
        }
        //
        //
        //
        //
        // ****              OrderStateChanged                ****
        //
        public event EventHandler OrderStateChanged;
        //
        /// <summary>
        /// After any order status change and after the order is updated in the book, 
        /// this event is triggered.  
        /// This event is only triggered for serious changes to the order state.
        /// Events include Reject, Cancel, and Fill
        /// </summary>
        public void OnOrderStateChanged(EventArgs orderStatusEventArgs)
        {
            if (this.OrderStateChanged != null)
            {
                this.OrderStateChanged(this, orderStatusEventArgs);
            }
        }
        //
        //
        //
        // ****              OrderSubmitted               ****
        //
        public event EventHandler OrderSubmitted;
        //
        //
        /// <summary>
        /// After the order status is updated in the book and the 
        /// total working quanity of the book has been updated 
        /// this event is triggered for confirmed submitted 
        /// from the exchange.
        /// </summary>
        /// <param name="orderStatusEventArgs"></param>
        public void OnOrderSubmitted(EventArgs orderStatusEventArgs)
        {
            UpdateTotalWorkingQty();
            if (this.OrderSubmitted != null)
            {
                this.OrderSubmitted(this, orderStatusEventArgs);
            }
        }
        #endregion//Event Triggers

    }
}
