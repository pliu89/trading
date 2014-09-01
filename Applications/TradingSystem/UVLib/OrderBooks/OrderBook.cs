using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.OrderBooks
{
    using UV.Lib.Products;
    using UV.Lib.Fills;
    using UV.Lib.Utilities;

    /// <summary>
    /// Orderbook for a single instrument.
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

        // account for these orders
        public string Account;                                          // all orders in this book should be tagged with the same account

        // temp order workspace
        Dictionary<int, Order> m_OrderWorkspace = new Dictionary<int, Order>(); // clear before each use!
        #endregion// members

        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public OrderBook(InstrumentName instrument, string account)
        {
            // Set Id when order book is connected and initialize by hub.
            this.m_BookID = System.Threading.Interlocked.Increment(ref OrderBook.m_NextId);
            this.Instrument = instrument;
            this.Account = account;

            // Initialize the lists.
            m_LiveOrders = new OrderPage[NSides];                        // a page for each side of mkt.
            m_DeletedOrders = new OrderPage[NSides];
            for (int side = 0; side < NSides; ++side)
            {
                m_LiveOrders[side] = new OrderPage(side, this);
                m_DeletedOrders[side] = new OrderPage(side, this);
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
        /// <param name="addToDeadBook"></param>
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
        //
        // *************************************************
        // ****                 Try Remove              ****
        // *************************************************
        public bool TryRemove(Order order)
        {
            for (int side = 0; side < NSides; ++side)
                if (m_LiveOrders[side].TryRemove(order))
                    return true;
            return false;
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
            for (int side = 0; side < NSides; ++side)
                if (m_LiveOrders[side].TryGet(orderId, out order))
                    return true;
            if (includeDeletedBook)     // Search thru deleted orders too.
                for (int side = 0; side < NSides; ++side)
                    if (m_DeletedOrders[side].TryGet(orderId, out order))
                        return true;
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
        /// This should only be called when we are confirmed dead!
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public bool TryDelete(int orderId)
        {
            // Find page containing this order.
            OrderPage page = null;
            for (int side = 0; side < NSides; ++side)
            {
                if (m_LiveOrders[side].Contains(orderId))
                {
                    page = m_LiveOrders[side];
                    break;
                }
            }
            // Move the order from one page to the other.
            if (page != null)
            {   // I found the page
                OrderPage deadPage = m_DeletedOrders[page.OrderSide];
                if (page.TryMove(orderId, deadPage))
                {
                    Order deadOrder;
                    if (deadPage.TryGet(orderId, out deadOrder))
                    {
                        deadOrder.OrderStateConfirmed = OrderState.Dead;
                        deadOrder.OrderStatePending = OrderState.Dead;
                        deadOrder.OriginalQtyConfirmed = 0;
                        return true;
                    }
                }
            }
            return false;
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
        //
        // *************************************************
        // ****              GetOrdersByIPrice         ****
        // *************************************************
        /// <summary>
        /// Get All Orders for a give side and integerized price.
        /// </summary>
        /// <param name="side"></param>
        /// <param name="iPrice"></param>
        /// <param name="orders"></param>
        public void GetOrdersByIPrice(int side, int iPrice, ref List<Order> orders)
        {
            m_LiveOrders[side].GetByPrice(iPrice, ref orders);
        }
        //
        // ******************************************
        // ****       Get IPrice By Rank()       ****
        // ******************************************
        /// <summary>
        /// For a given "rank" of orders, try and get the corresponding iPrice
        /// for those orders.
        /// </summary>
        /// <param name="side"></param>
        /// <param name="rank"></param>
        /// <param name="iPrice"></param>
        /// <returns></returns>
        public bool TryGetIPriceByRank(int side, int rank, out int iPrice)
        {
            if (m_LiveOrders[side].TryGetIPriceByRank(rank, out iPrice))
            {
                iPrice = -QTMath.MktSideToMktSign(side) * iPrice;
                return true;
            }
            return false;
        }
        //
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
        /// <param name="tag"></param>
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
        // ****            TryProcessFill()             ****
        // *************************************************
        public bool TryProcessFill(FillEventArgs fillEventArgs)
        {
           return m_LiveOrders[QTMath.MktSignToMktSide(fillEventArgs.Fill.Qty)].TryProcessFill(fillEventArgs);
        }
        //
        // *************************************************
        // ****           ProcessAddConfirm()           ****
        // *************************************************
        public void ProcessAddConfirm(Order order)
        {
            m_LiveOrders[order.Side].ProcessAddConfirm(order);
        }
        //
        // *************************************************
        // ****           ProcessFoundOrder()           ****
        // *************************************************
        public void ProcessFoundOrder(Order order)
        {
            m_LiveOrders[order.Side].ProcessFoundOrder(order);
        }
        //
        // *************************************************
        // ****           ProcessUpdateConfirm()           ****
        // *************************************************
        public void ProcessUpdateConfirm(Order order)
        {
            m_LiveOrders[order.Side].ProcessUpdateConfirm(order);
        }   
        //
        // *************************************************
        // ****          ProcessDeleteConfirm()         ****
        // *************************************************
        public void ProcessDeleteConfirm(Order order)
        {
            m_LiveOrders[order.Side].ProcessDeleteConfirm(order);
        }
        //
        // *************************************************
        // ****            ProcessAddReject()           ****
        // *************************************************
        public void ProcessAddReject(Order order)
        {
            m_LiveOrders[order.Side].ProcessAddReject(order);
        }
        //
        // *************************************************
        // ****           ProcessPriceChange()          ****
        // *************************************************
        public void ProcessPriceChange(Order order, int newIPrice)
        {
            m_LiveOrders[order.Side].ProcessPriceChange(order, newIPrice);
        }
        //
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
        /// <summary>
        /// After any order status change and after the order is updated in the book, 
        /// this event is triggered.  
        /// This event is only triggered for serious changes to the order state.
        /// Events include Reject, Cancel, and Fill
        /// </summary>
        // ****              OrderStateChanged                ****
        //
        public event EventHandler OrderStateChanged;
        //
        //
        public void OnOrderStateChanged(EventArgs orderStatusEventArgs)
        {
            if (this.OrderStateChanged != null)
            {
                this.OrderStateChanged(this, orderStatusEventArgs);
            }
        }
        //
        //
        /// <summary>
        /// This event is triggered on any update confirmation from the exchange.  This could include
        /// changes to price or qty.
        /// </summary>
        public event EventHandler OrderUpdated;
        //
        //
        public void OnOrderUpdated(EventArgs orderStatusEventArgs)
        {
            if(this.OrderUpdated != null)
            {
                this.OrderUpdated(this, orderStatusEventArgs);
            }
        }
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
        //
        /// <summary>
        /// Typically only triggered on startup when existing orders are downloaded
        /// and added to the default book.
        /// </summary>
        public event EventHandler OrderFound;
        //
        //
        public void OnOrderFound(EventArgs orderStatusEventArgs)
        {
        
            UpdateTotalWorkingQty();
            if(this.OrderFound != null)
            {
                this.OrderFound(this, orderStatusEventArgs);
            }

        }
        #endregion//Event Triggers

    }
}
