using System;
using System.Collections.Generic;
using System.Text;


namespace UV.Lib.OrderBookHubs
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
        private object m_Lock = new object();
        private Dictionary<int, Order> m_OrdersById = new Dictionary<int, Order>();
        private SortedList<int, List<int>> m_OrdersByPendingPrice = new SortedList<int, List<int>>();
        private Queue<List<int>> m_RecyclingList = new Queue<List<int>>();  // lists used for OrdersByPendingPrice

        //
        // Recycling Factories
        //
        RecycleFactory<Order> m_OrderRecycleFactory;
        #endregion// members

        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sideOfOrders">Each OrderPage contains either Buy/Sell orders</param>
        public OrderPage(int sideOfOrders, OrderBook parentBook, RecycleFactory<Order> orderRecycleFactory)
        {
            this.OrderSide = sideOfOrders;
            this.OrderSign = UV.Lib.Utilities.QTMath.MktSideToMktSign(this.OrderSide);
            this.m_OrderRecycleFactory = orderRecycleFactory;
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
            lock (m_Lock)
            {
                // Validate newOrder
                if (m_OrdersById.ContainsKey(newOrder.Id))                      // Check that we don't already have entry for this orderId
                    isSuccess = false;
                if (this.OrderSide != newOrder.Side)                            // Check side of mkt is same as this book.
                    isSuccess = false;

                // Accept order
                if (isSuccess)
                {
                    Order myOrder = m_OrderRecycleFactory.Get();
                    newOrder.CopyTo(myOrder);
                    int iPriceLevel = -this.OrderSign * myOrder.IPricePending;    // Puts lowest (highest) priced sell (buy) orders at lowest price level
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
                    myOrder.OrderStatePending = OrderState.Submitted;
                    m_OrdersByPendingPrice[iPriceLevel].Add(myOrder.Id);        // add to price-list
                    m_OrdersById.Add(myOrder.Id, myOrder);                      // add to id-list. 
                }
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
            foundOrder = null;
            lock (m_Lock)
            {
                Order myOrder;
                if (m_OrdersById.TryGetValue(orderId, out myOrder))
                {   // We found the desired order, return a copy of it.
                    foundOrder = m_OrderRecycleFactory.Get();   // call recycle factory for order.
                    myOrder.CopyTo(foundOrder);
                }
            }
            return (foundOrder!=null);
        }// TryGet()
        //
        // *************************************************
        // ****             Contains()                  ****
        // *************************************************
        public bool Contains(int orderId)
        {
            bool isContains = false;
            lock (m_Lock)
            {
                isContains = m_OrdersById.ContainsKey(orderId);
            }
            return isContains;
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
            lock (m_Lock)
            {
                int iPriceLevel = -this.OrderSign * iPrice;
                if (m_OrdersByPendingPrice.ContainsKey(iPriceLevel))
                {
                    Order order;
                    foreach (int id in m_OrdersByPendingPrice[iPriceLevel])
                        if (m_OrdersById.TryGetValue(id, out order))
                        {
                            Order foundOrder = m_OrderRecycleFactory.Get();
                            order.CopyTo(foundOrder);
                            orders.Add(foundOrder);
                        }
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
            lock (m_Lock)
            {
                // Since we remove empty price levels, we can jump directly to index.
                if (rank < m_OrdersByPendingPrice.Count)
                {
                    int iPrice = m_OrdersByPendingPrice.Keys[rank];        // Get key at specific rank               
                    foreach (int id in m_OrdersByPendingPrice[iPrice])
                    {
                        Order foundOrder = m_OrderRecycleFactory.Get();
                        m_OrdersById[id].CopyTo(foundOrder);
                        orders.Add(foundOrder);
                    }
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
        /// <param name="orders"></param>
        public bool TryGetIPriceByRank(int rank, out int iPrice)
        {
            bool isOrderFound = false;
            iPrice = 0;
            lock (m_Lock)
            {
                // Since we remove empty price levels, we can jump directly to index.
                if (rank < m_OrdersByPendingPrice.Count )
                {
                    iPrice = m_OrdersByPendingPrice.Keys[rank];
                    isOrderFound = true;
                }
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
            lock (m_Lock)
            {
                foreach (KeyValuePair<int, Order> pair in m_OrdersById)
                {
                    Order foundOrder = m_OrderRecycleFactory.Get();
                    pair.Value.CopyTo(foundOrder);
                    orders.Add(foundOrder.Id,foundOrder);
                }
            }
        }//GetAll()
        //
        // *************************************************
        // ****          Get By UserDefinedTag()        ****
        // *************************************************
        public void GetByUserDefinedTag(int tag, ref List<Order> orders)
        {
            lock (m_Lock)
            {
                foreach (KeyValuePair<int, Order> pair in m_OrdersById)
                    if (pair.Value.UserDefinedTag == tag)
                    {
                        Order foundOrder = m_OrderRecycleFactory.Get();
                        pair.Value.CopyTo(foundOrder);
                        orders.Add( foundOrder );
                    }
            }
        }//GetByUserDefinedTag()
        //
        // *************************************************
        // ****                 Count()                 ****
        // *************************************************
        public int Count()
        {
            int n = 0;
            lock (m_Lock)
            {
                n = m_OrdersById.Count;
            }
            return n;
        }//Count()
        //
        //
        //
        //        
        // *************************************************
        // ****             Try Remove()                ****
        // *************************************************
        /// <summary>
        /// This is the only function from the entire order page that will give you the 
        /// actual order, and not a copy. Since this is being removed, and is currently
        /// not found anywhere else, it is thread safe since the caller is the only one 
        /// that is not holding the order.
        /// The downside is that, for a moment, while the order is being moved from this
        /// book to its new home, the order will simply not exist.
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="removedOrder"></param>
        /// <returns></returns>
        /*
        public bool TryRemove(int orderId, out Order removedOrder)
        {
            bool isRemoved = false;
            lock (m_Lock)
            {
                if (m_OrdersById.TryGetValue(orderId, out removedOrder))
                {
                    int iPriceLevel = -this.OrderSign * removedOrder.IPricePending;
                    // Remove entries from both lookup tables.
                    m_OrdersById.Remove(orderId);                               // remove from ID lookup table
                    m_OrdersByPendingPrice[iPriceLevel].Remove(orderId);        // remove from price based lookup.
                    if (m_OrdersByPendingPrice[iPriceLevel].Count == 0)         // If this price level completely empty, remove entry completely.
                    {
                        m_RecyclingList.Enqueue(m_OrdersByPendingPrice[iPriceLevel]);  // put the empty list into recycling queue.
                        m_OrdersByPendingPrice.Remove(iPriceLevel);                    // remove the empty list from ByPrice lookup table.
                    }
                    isRemoved = true;
                }
            }
            // Exit.
            return isRemoved;
        }// TryRemove()
        //
        */ 
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

            // Perform move now.
            lock (m_Lock)
            {
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
            lock (m_Lock)
            {
                ChangePendingPriceLevel(orderToChange, newIPrice);
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
        /// /// Threadsafe method to request manipulation of an internal order.
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="orderSide"></param>
        /// <param name="orderReq"></param>
        public void ProcessRequest(int orderId, int orderSide, RequestEventArg<OrderRequestType> orderReq)
        {
            Order order;
            switch (orderReq.RequestType)
            {
                case OrderRequestType.None:
                    break;
                    //
                    // Add
                    //
                case OrderRequestType.AddConfirm:
                    order = (Order)orderReq.Data[3];
                    ProcessAddConfirm(order);
                    break;
                case OrderRequestType.AddReject:
                    ProcessAddReject(orderId);
                    break;
                    //
                    // Delete
                    //
                case OrderRequestType.DeleteConfirm:
                    ProcessDeleteConfirm(orderId);
                    break;
                case OrderRequestType.DeleteRequest:
                    ProcessDeleteRequest(orderId);
                    break;
                case OrderRequestType.DeleteReject:
                    // TODO
                    break;                
                    //
                    // Fills
                    //
                case OrderRequestType.FillConfirm:
                    FillEventArgs fillEvent = (FillEventArgs)orderReq.Data[3];
                    ProcessFillConfirm(fillEvent);
                    break;
                    //
                    // Change
                    //
                case OrderRequestType.ChangeRequest:
                    order = (Order)orderReq.Data[3];
                    ProcessChangeRequest(order);
                    break;
                case OrderRequestType.ChangeConfirm:
                    order = (Order)orderReq.Data[3];
                    ProcessChangeConfirm(order);
                    break;
                case OrderRequestType.ChangeReject:
                    ProcessChangeReject(orderId);
                    break;
                    //
                    // Unknown
                    //
                case OrderRequestType.Unknown:
                    order = (Order)orderReq.Data[3];
                    ProcessUknownOrder(order);
                    break;

                default:
                    break;
            }
        }
        #endregion//Public Methods

        #region Order Manipulation Methods 
        // *****************************************************************
        // ****             Order Manipulation Methods                  ****
        // *****************************************************************
        //
        // *************************************************
        // ****              TryChangeState()           ****
        // *************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="newOrderState"></param>
        /// <param name="isConfirmed"></param>
        /// <returns></returns>
        public bool TryChangeState(int orderId, OrderState newOrderState, bool isConfirmed)
        {
            Order orderToModify;
            if (m_OrdersById.TryGetValue(orderId, out orderToModify))
            {
                lock (m_Lock)
                {
                    if (isConfirmed)
                        orderToModify.OrderStateConfirmed = newOrderState;
                    else
                        orderToModify.OrderStatePending = newOrderState;
                }
                return true;
            }
            else
                return false;
        }
        public bool TryChangeState(Order orderToModify, OrderState newOrderState, bool isConfirmed)
        {
            lock (m_Lock)
            {
                if (isConfirmed)
                    orderToModify.OrderStateConfirmed = newOrderState;
                else
                    orderToModify.OrderStatePending = newOrderState;
            }
            return true;
        }
        #endregion // Order Manipulation methods.

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
        // TODO: 
        //      1. Order recycling.
        // *************************************
        // ****      ProcessAddConfirm      ****
        // *************************************
        private void ProcessAddConfirm(Order order)
        {
            Order orderToModify;
            if (m_OrdersById.TryGetValue(order.Id, out orderToModify))
            {
                lock (m_Lock)
                {
                    orderToModify.OrderStateConfirmed = order.OrderStateConfirmed;
                    orderToModify.IPriceConfirmed = order.IPriceConfirmed;
                    orderToModify.OriginalQtyConfirmed = order.OriginalQtyConfirmed;
                }

                // Trigger event
                OrderEventArgs orderEvent = new OrderEventArgs();
                orderEvent.Order = order;
                orderEvent.IsAddConfirmation = true;
                orderEvent.OrderBookID = m_ParentOrderBook.BookID;
                m_ParentOrderBook.OnOrderSubmitted(orderEvent);
            }
        }//ProcessAddConfirm()
        //
        //
        // *************************************
        // ****     ProcessAddReject        ****
        // *************************************
        private void ProcessAddReject(int orderId)
        {
            Order orderToModify;
            if (m_OrdersById.TryGetValue(orderId, out orderToModify))
            {
                lock (m_Lock)
                {
                    orderToModify.OriginalQtyConfirmed = 0;
                    orderToModify.OrderStateConfirmed = OrderState.Dead;
                }
                m_ParentOrderBook.TryDelete(orderId);
            }
            // Trigger state changed event
            Order orderToReturn = null;
            if (m_ParentOrderBook.TryGet(orderId, out orderToReturn, true))
            {
                OrderEventArgs orderEvent = new OrderEventArgs();
                orderEvent.Order = orderToReturn;
                orderEvent.OrderBookID = m_ParentOrderBook.BookID;
                m_ParentOrderBook.OnOrderStateChanged(orderEvent);
            }

        }
        // *************************************
        // ****     ProcessDeleteRequest    ****
        // *************************************
        private void ProcessDeleteRequest(int orderId)
        {
            Order orderToModify;
            if (m_OrdersById.TryGetValue(orderId, out orderToModify))
            {
                lock (m_Lock)
                {
                    orderToModify.OrderStatePending = OrderState.Dead;
                    orderToModify.OriginalQtyPending = 0;
                }
            }
        }
        //
        // *************************************
        // ****     ProcessDeleteConfirm    ****
        // *************************************
        private void ProcessDeleteConfirm(int orderId)
        {
            Order orderToModify;
            if (m_OrdersById.TryGetValue(orderId, out orderToModify))
            {
                lock (m_Lock)
                {
                    orderToModify.OriginalQtyConfirmed = 0;
                    orderToModify.OrderStateConfirmed = OrderState.Dead;
                }
            }
            // Move into deadbook.
            m_ParentOrderBook.TryDelete(orderId);

            // Trigger event
            Order orderToReturn = null;
            if (this.TryGet(orderId, out orderToReturn))
            {
                OrderEventArgs orderEvent = new OrderEventArgs();
                orderEvent.Order = orderToReturn;
                orderEvent.OrderBookID = m_ParentOrderBook.BookID;
                m_ParentOrderBook.OnOrderStateChanged(orderEvent);
            }
            
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
        // ****     ProcessFillConfirm      ****
        // *************************************
        private void ProcessFillConfirm(FillEventArgs fillEvent)
        {
            Order orderToModify;
            Order orderToReturn = null;
            if (m_OrdersById.TryGetValue(fillEvent.OrderId, out orderToModify))
            {
                lock (m_Lock)
                {
                    orderToModify.ExecutedQty += fillEvent.Fill.Qty;
                    if (fillEvent.isComplete)
                        orderToModify.OrderStateConfirmed = OrderState.Dead;
                }
                if (fillEvent.isComplete)
                    m_ParentOrderBook.TryDelete(fillEvent.OrderId);

                // Trigger fill event
                fillEvent.OrderBookID = m_ParentOrderBook.BookID;           // assign the correct id now that we know it.
                m_ParentOrderBook.OnOrderFilled(fillEvent);                 // fire fill event for my subscribers
                // Trigger order state change
                if (m_ParentOrderBook.TryGet(fillEvent.OrderId, out orderToReturn, true))
                {
                    OrderEventArgs orderEvent = new OrderEventArgs();
                    orderEvent.Order = orderToReturn;
                    orderEvent.Fill = fillEvent.Fill;
                    orderEvent.OrderBookID = m_ParentOrderBook.BookID;
                    m_ParentOrderBook.OnOrderStateChanged(orderEvent);
                }
            }
        }
        //
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
                lock (m_Lock)
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
        }
        //
        // *************************************
        // ****      ProcessChangeConfirm   ****
        // *************************************
        private void ProcessChangeConfirm(Order order)
        {
            Order orderToModify;
            if (m_OrdersById.TryGetValue(order.Id, out orderToModify))
            {
                lock (m_Lock)
                {
                    orderToModify.OrderStateConfirmed = order.OrderStateConfirmed;
                    orderToModify.OrderTIF = order.OrderTIF;
                    orderToModify.OriginalQtyConfirmed = order.OriginalQtyConfirmed;
                    orderToModify.IPriceConfirmed = order.IPriceConfirmed;
                }
            }
        }
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
