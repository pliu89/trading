using System;
using System.Collections.Generic;
using System.Text;


namespace UV.Lib.OrderBooks
{
    using UV.Lib.Utilities;
    using UV.Lib.Fills;
    /// <summary>
    /// This is the component of the OrderBook that contains all orders 
    /// from ONE side of the market.  All orders herein are either BuySide, or SellSide etc.
    /// </summary>
    public class OrderPage
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        //
        // Order Page 
        //
        private readonly OrderBook m_ParentOrderBook;
        public readonly int OrderSide;                                     // This book contains orders on this side of mkt only!
        private readonly int OrderSign;                                     // sign associated with orders on this page

        //
        // Order lookup tables
        //
        private Dictionary<int, Order> m_OrdersById = new Dictionary<int, Order>();
        private SortedList<int, List<int>> m_OrdersByPendingPrice = new SortedList<int, List<int>>();
        private Queue<List<int>> m_RecyclingList = new Queue<List<int>>();  // lists used for OrdersByPendingPrice
        #endregion// members

        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sideOfOrders">Each OrderPage contains either Buy/Sell orders</param>
        /// <param name="parentBook"></param>
        public OrderPage(int sideOfOrders, OrderBook parentBook)
        {
            this.OrderSide = sideOfOrders;
            this.OrderSign = UV.Lib.Utilities.QTMath.MktSideToMktSign(this.OrderSide);
            m_ParentOrderBook = parentBook;
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
        // *************************************************
        // ****             Try Add()                   ****
        // *************************************************
        /// <summary>
        /// Adds a copy of newOrder into this OrderPage.
        /// </summary>
        /// <param name="newOrder"></param>
        /// <returns>true, if successful.</returns>
        public bool TryAdd(Order newOrder)
        {
            bool isSuccess = true;
            // Validate newOrder
            if (m_OrdersById.ContainsKey(newOrder.Id))                      // Check that we don't already have entry for this orderId
                isSuccess = false;
            if (this.OrderSide != newOrder.Side)                            // Check side of mkt is same as this book.
                isSuccess = false;

            // Accept order
            if (isSuccess)
            {
                int iPriceLevel = -this.OrderSign * newOrder.IPricePending;    // Puts lowest (highest) priced sell (buy) orders at lowest price level
                if (!m_OrdersByPendingPrice.ContainsKey(iPriceLevel))
                {
                    if (m_RecyclingList.Count > 0)
                    {
                        List<int> recycledList = m_RecyclingList.Dequeue();
                        recycledList.Clear();
                        m_OrdersByPendingPrice.Add(iPriceLevel, recycledList);
                    }
                    else
                        m_OrdersByPendingPrice.Add(iPriceLevel, new List<int>());

                }
                newOrder.OrderStatePending = OrderState.Submitted;          // set pending state to submitted.
                m_OrdersByPendingPrice[iPriceLevel].Add(newOrder.Id);       // add to price-list
                m_OrdersById.Add(newOrder.Id, newOrder);                    // add to id-list. 
            }
            // Exit
            return isSuccess;
        }//Add().
        //
        // *************************************************
        // ****             Try Get()                   ****
        // *************************************************
        public bool TryGet(int orderId, out Order foundOrder)
        {
            return m_OrdersById.TryGetValue(orderId, out foundOrder);

        }// TryGet()
        //
        //
        // *************************************************
        // ****             Try Remove                  ****
        // *************************************************
        /// <summary>
        /// Called if a user would like to complete remove an order
        /// from this page. This will leave no trace of the order here!
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public bool TryRemove(Order order)
        {
            if(m_OrdersById.Remove(order.Id))
            {   // we need to remove it from order's be pending price now as well
                int iPriceLevel = -this.OrderSign * order.IPricePending;

                m_OrdersByPendingPrice[iPriceLevel].Remove(order.Id);           // remove from price based lookup.
                if (m_OrdersByPendingPrice[iPriceLevel].Count == 0)             // If this price level completely empty, remove entry completely.
                {
                    m_RecyclingList.Enqueue(m_OrdersByPendingPrice[iPriceLevel]);  // put the empty list into recycling queue.
                    m_OrdersByPendingPrice.Remove(iPriceLevel);                    // remove the empty list from ByPrice lookup table.
                }
                return true;
            }
            return false;
        }
        //
        // *************************************************
        // ****             Contains()                  ****
        // *************************************************
        public bool Contains(int orderId)
        {
            return m_OrdersById.ContainsKey(orderId);
        }// Contains()
        //
        //
        //
        //
        // *************************************************
        // ****             Get By Price()              ****
        // *************************************************
        public void GetByPrice(int iPrice, ref List<Order> orders)
        {
            int iPriceLevel = -this.OrderSign * iPrice;
            if (m_OrdersByPendingPrice.ContainsKey(iPriceLevel))
            {
                Order order;
                foreach (int id in m_OrdersByPendingPrice[iPriceLevel])
                    if (m_OrdersById.TryGetValue(id, out order))
                    {
                        orders.Add(order);
                    }
            }
        }//GetByPrice()
        //
        //
        // *************************************************
        // ****             Get By Rank()               ****
        // *************************************************
        public void GetByRank(int rank, ref List<Order> orders)
        {
            // Since we remove empty price levels, we can jump directly to index.
            if (rank < m_OrdersByPendingPrice.Count)
            {
                int iPrice = m_OrdersByPendingPrice.Keys[rank];        // Get key at specific rank               
                foreach (int id in m_OrdersByPendingPrice[iPrice])
                {
                    orders.Add(m_OrdersById[id]);
                }
            }
        }//GetByRank()
        //
        //
        // *************************************************
        // ****         Get IPrice By Rank()             ****
        // *************************************************
        /// <summary>
        /// The price at a certain rank.
        /// </summary>
        /// <param name="rank">rank 0 is closest order to market, etc.</param>
        /// <param name="iPrice"></param>
        public bool TryGetIPriceByRank(int rank, out int iPrice)
        {
            bool isOrderFound = false;
            iPrice = 0;
            // Since we remove empty price levels, we can jump directly to index.
            if (rank < m_OrdersByPendingPrice.Count)
            {
                iPrice = m_OrdersByPendingPrice.Keys[rank];
                isOrderFound = true;
            }
            return isOrderFound;
        }//GetByRank()
        //
        //
        // *************************************************
        // ****               Get All()                 ****
        // *************************************************
        public void GetAll(ref Dictionary<int, Order> orders)
        {
            foreach (KeyValuePair<int, Order> pair in m_OrdersById)
            {
                orders.Add(pair.Key, pair.Value);
            }
        }//GetAll()
        //
        // *************************************************
        // ****          Get By UserDefinedTag()        ****
        // *************************************************
        public void GetByUserDefinedTag(int tag, ref List<Order> orders)
        {
            foreach (KeyValuePair<int, Order> pair in m_OrdersById)
                if (pair.Value.UserDefinedTag == tag)
                    orders.Add(pair.Value);
        }//GetByUserDefinedTag()
        //
        // *************************************************
        // ****                 Count()                 ****
        // *************************************************
        public int Count()
        {
            return m_OrdersById.Count;
        }//Count()
        //
        //
        //
        // *********************************************
        // ****             Try Move()              ****
        // *********************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="orderId">Id of order to be moved.</param>
        /// <param name="toOrderPage">OrderPage to receive the order.</param>
        /// <returns></returns>
        public bool TryMove(int orderId, OrderPage toOrderPage)
        {
            bool isSuccess = false;
            if (this.Equals(toOrderPage))
                return true;                                                    // I already contain the order.

            Order removedOrder = null;
            if (m_OrdersById.TryGetValue(orderId, out removedOrder))
            {
                isSuccess = toOrderPage.TryAdd(removedOrder);                // Add order to the other page.
                if (isSuccess)
                {   // If order has been successfully added to other page, 
                    // We complete our removal of it from this book now.
                    int iPriceLevel = -this.OrderSign * removedOrder.IPricePending;

                    m_OrdersById.Remove(orderId);                               // remove from ID lookup table
                    m_OrdersByPendingPrice[iPriceLevel].Remove(orderId);        // remove from price based lookup.
                    if (m_OrdersByPendingPrice[iPriceLevel].Count == 0)         // If this price level completely empty, remove entry completely.
                    {
                        m_RecyclingList.Enqueue(m_OrdersByPendingPrice[iPriceLevel]);  // put the empty list into recycling queue.
                        m_OrdersByPendingPrice.Remove(iPriceLevel);                    // remove the empty list from ByPrice lookup table.
                    }
                }
            }
            return isSuccess;
        }// TryMove()
        //
        //
        //
        // *************************************************
        // ****             ChangePrice()               ****
        // *************************************************
        /// <summary>
        /// Given an order this method will change its price and 
        /// it IPriceLevel in the collections.
        /// </summary>
        /// <param name="orderToChange"></param>
        /// <param name="newIPrice"></param>
        public void ChangePrice(Order orderToChange, int newIPrice)
        {
            ChangePendingPriceLevel(orderToChange, newIPrice);
        }
        //
        //
        #endregion//Public Methods

        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        /// <summary>
        /// Called internally once a lock is obtained only to deal with book keeping surrounding
        /// pending prices 
        /// </summary>
        /// <param name="orderToModify"></param>
        /// <param name="newIPrice"></param>
        private void ChangePendingPriceLevel(Order orderToModify, int newIPrice)
        {
            int iPriceLevel = -this.OrderSign * orderToModify.IPricePending;
            m_OrdersByPendingPrice[iPriceLevel].Remove(orderToModify.Id);               // remove from price based lookup.
            if (m_OrdersByPendingPrice[iPriceLevel].Count == 0)                         // If this price level completely empty, remove entry completely.
            {
                m_RecyclingList.Enqueue(m_OrdersByPendingPrice[iPriceLevel]);           // put the empty list into recycling queue.
                m_OrdersByPendingPrice.Remove(iPriceLevel);                             // remove the empty list from ByPrice lookup table.
            }
            orderToModify.IPricePending = newIPrice;                                    // set the new price
            int newIPriceLevel = -this.OrderSign * orderToModify.IPricePending;         // get the new iPriceLevel

            if (!m_OrdersByPendingPrice.ContainsKey(newIPriceLevel))
            { // we need to make  a list at this level
                if (m_RecyclingList.Count > 0)
                { // we have lists we can recycle.
                    List<int> recycledList = m_RecyclingList.Dequeue();
                    recycledList.Clear();
                    m_OrdersByPendingPrice.Add(newIPriceLevel, recycledList);
                }
                else
                    m_OrdersByPendingPrice.Add(newIPriceLevel, new List<int>());
            }
            m_OrdersByPendingPrice[newIPriceLevel].Add(orderToModify.Id);                 // add to price-list
        }
        #endregion//Private Methods

        #region Private Request Processing Methods - Needs some work!
        // *****************************************************************
        // ****           Private Request Processing Methods            ****
        // *****************************************************************
        // *************************************
        // ****      ProcessAddConfirm      ****
        // *************************************
        public void ProcessAddConfirm(Order order)
        {
            order.ChangesPending = false;
            // Trigger event
            OrderEventArgs orderEvent = new OrderEventArgs();
            orderEvent.Order = order;
            orderEvent.IsAddConfirmation = true;
            orderEvent.OrderBookID = m_ParentOrderBook.BookID;
            m_ParentOrderBook.OnOrderSubmitted(orderEvent);
        }//ProcessAddConfirm()
        //
        //
        // *************************************
        // ****      ProcessFoundOrder      ****
        // *************************************
        public void ProcessFoundOrder(Order order)
        {
            order.ChangesPending = false;
            // Trigger event
            OrderEventArgs orderEvent = new OrderEventArgs();
            orderEvent.Order = order;
            orderEvent.IsAddConfirmation = true;
            orderEvent.OrderBookID = m_ParentOrderBook.BookID;
            m_ParentOrderBook.OnOrderFound(orderEvent);
        }//ProcessFoundOrder()
        //
        //
        // *************************************
        // ****     ProcessAddReject        ****
        // *************************************
        public void ProcessAddReject(Order order)
        {

            order.OriginalQtyConfirmed = 0;
            order.OrderStateConfirmed = OrderState.Dead;
            order.ChangesPending = false;
            m_ParentOrderBook.TryDelete(order.Id);

            // Trigger state changed event
            OrderEventArgs orderEvent = new OrderEventArgs();
            orderEvent.Order = order;
            orderEvent.OrderBookID = m_ParentOrderBook.BookID;
            m_ParentOrderBook.OnOrderStateChanged(orderEvent);

        }
        //
        // *************************************
        // ****      ProcessUpdateConfirm   ****
        // *************************************
        public void ProcessUpdateConfirm(Order order)
        {
            order.ChangesPending = false;
            // Trigger event
            OrderEventArgs orderEvent = new OrderEventArgs();
            orderEvent.Order = order;
            orderEvent.OrderBookID = m_ParentOrderBook.BookID;
            m_ParentOrderBook.OnOrderUpdated(orderEvent);
        }//ProcessUpdateConfirm()
        //
        // *************************************
        // ****     ProcessDeleteRequest    ****
        // *************************************
        private void ProcessDeleteRequest(int orderId)
        {
            Order orderToModify;
            if (m_OrdersById.TryGetValue(orderId, out orderToModify))
            {
                orderToModify.OrderStatePending = OrderState.Dead;
                orderToModify.OriginalQtyPending = 0;
            }
        }
        //
        // *************************************
        // ****     ProcessDeleteConfirm    ****
        // *************************************
        public void ProcessDeleteConfirm(Order order)
        {
            order.ChangesPending = false;
            // Move into deadbook.
            m_ParentOrderBook.TryDelete(order.Id);
            // Trigger event
            OrderEventArgs orderEvent = new OrderEventArgs();
            orderEvent.Order = order;
            orderEvent.OrderBookID = m_ParentOrderBook.BookID;
            m_ParentOrderBook.OnOrderStateChanged(orderEvent);
        }// ProcessDeleteConfirm()
        //
        //
        // *****************************************
        // ****     ProcessDeleteReject()       ****
        // *****************************************
        private void ProcessDeleteReject(int orderId)
        {


        }
        //
        //
        // *************************************
        // ****        TryProcessFill       ****
        // *************************************
        /// <summary>
        /// Caller would like to process a confirmed fill from the exchange
        /// False only if the order isn't found.
        /// </summary>
        /// <param name="fillEvent"></param>
        /// <returns></returns>
        public bool TryProcessFill(FillEventArgs fillEvent)
        {
            Order orderToModify;
            if (m_OrdersById.TryGetValue(fillEvent.OrderId, out orderToModify))
            {
                orderToModify.ExecutedQty += fillEvent.Fill.Qty;
                if (fillEvent.isComplete)
                {
                    orderToModify.ChangesPending = false;
                    m_ParentOrderBook.TryDelete(fillEvent.OrderId);         // this will set state.
                }

                // Trigger fill event
                fillEvent.OrderBookID = m_ParentOrderBook.BookID;           // assign the correct id now that we know it.
                fillEvent.OrderReason = orderToModify.OrderReason;                // also pass along tag.
                m_ParentOrderBook.OnOrderFilled(fillEvent);                 // fire fill event for my subscribers
                
                // Trigger order state change
                OrderEventArgs orderEvent = new OrderEventArgs();
                orderEvent.Order = orderToModify;
                orderEvent.Fill = fillEvent.Fill;
                orderEvent.OrderBookID = m_ParentOrderBook.BookID;
                m_ParentOrderBook.OnOrderStateChanged(orderEvent);
                
                return true;
            }
            return false;
        }
        //
        //
        //
        //
        // *************************************
        // ****      ProcessPriceChange     ****
        // *************************************
        public void ProcessPriceChange(Order order, int newIPrice)
        {
            if (order.IPricePending != newIPrice)
                ChangePendingPriceLevel(order, newIPrice);
        }
        //
        //
        // *************************************
        // ****      ProcessChangeRequest   ****
        // *************************************
        private void ProcessChangeRequest(Order order)
        {
            Order orderToModify;
            if (m_OrdersById.TryGetValue(order.Id, out orderToModify))
            {
                orderToModify.OrderStatePending = order.OrderStatePending;
                //orderToModify.OrderTIF = order.OrderTIF;
                orderToModify.OriginalQtyPending = order.OriginalQtyPending;
                if (orderToModify.IPricePending != order.IPricePending)
                { // price has changed so we need to do some book keeping.  This also sets the new IPrice.
                    ChangePendingPriceLevel(orderToModify, order.IPricePending);
                }
            }
        }
        //
        // *************************************
        // ****      ProcessChangeReject    ****
        // *************************************
        private void ProcessChangeReject(int orderId)
        {
            // currently nothing to be done.
        }
        //
        //
        // *************************************
        // ****      ProcessUknownOrder     ****
        // *************************************
        private void ProcessUknownOrder(Order order)
        {
            // This is an error.  Report an error.
        }
        //
        //
        #endregion//Private Request Processing Methods

        #region no Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        #endregion//Event Handlers

    }
}
