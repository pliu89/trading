using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
//using System.Linq;
//using System.Text;

namespace UV.Lib.OrderBooks
{
    using UV.Lib.Products;
    using UV.Lib.Fills;
    using UV.Lib.Utilities;

    /// <summary>
    /// This manages a collection of Orderbooks for a single InstrumentName.
    /// </summary>
    public class OrderInstrument
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        public readonly InstrumentName Instrument;
        public readonly InstrumentDetails Details;

        // 
        // OrderBook management
        //
        private Dictionary<int, OrderBook> m_OrderBooks = new Dictionary<int, OrderBook>(); // storage of order books by id.
        private Dictionary<int, int> m_OrderIDToBookMap;            // Map: OrderID --> OrderBookID, where to find a specific order.        
        private OrderBook m_DefaultBook;                            // default order book for uknown orders not owned by a strat(yet)
        #endregion// members

        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        /// <summary>
        /// Main constructor for Order Instrument.  
        /// </summary>
        /// <param name="instrumentName"></param>
        /// <param name="details"></param>
        public OrderInstrument(InstrumentName instrumentName, InstrumentDetails details)
        {
            this.Instrument = instrumentName;
            this.Details = details;
            m_OrderIDToBookMap = new Dictionary<int, int>();
            m_DefaultBook = new OrderBook(instrumentName, string.Empty);        // create our default book
            m_OrderBooks.Add(m_DefaultBook.BookID, m_DefaultBook);              // add it to our look up tables
            m_DefaultBook.IsReady = true;
        }
        //
        //       
        #endregion//Constructors

        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        /// <summary>
        /// This allows access to the default order book for this order instrument.  
        /// The default book is typically only used as storage for downloading 
        /// order 
        /// </summary>
        public OrderBook DefaultBook
        {
            get { return m_DefaultBook; }
        }
        //
        #endregion//Properties

        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        // *****************************************
        // ****         TryAddBook()            ****
        // *****************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="newOrderBook"></param>
        /// <returns></returns>
        public bool TryAddBook(OrderBook newOrderBook)
        {
            // Validate book
            if (m_OrderBooks.ContainsKey(newOrderBook.BookID))
                return false;

            // Add book to our list.
            newOrderBook.IsReady = true;
            m_OrderBooks.Add(newOrderBook.BookID, newOrderBook);
            return true;
        }// AddBook()
        //
        //
        // *************************************
        // ****         TryAddOrder()       ****
        // *************************************
        /// <summary>
        /// Called by the OrderBookHub to add an order to book.
        /// If this order is added to book, we return true, which tells the
        /// OrderBookHub to truly submit the order to the exchange also.
        /// Additionally this will set the account for the order based on the account
        /// of the order book
        /// </summary>
        /// <returns></returns>
        public bool TryAddOrder(int orderBookId, Order order)
        {
            // Validate the request.
            if (m_OrderIDToBookMap.ContainsKey(order.Id))
                return false;                           // Duplicate order ID!
            OrderBook orderBook = null;
            if (m_OrderBooks.TryGetValue(orderBookId, out orderBook))
            {
                if (!orderBook.IsReady)
                    return false;                       // order book not yet ready.
                //
                // Add order to desired book.
                //
                m_OrderIDToBookMap.Add(order.Id, orderBookId);
                orderBook.TryAdd(order);
                order.Account = orderBook.Account;
                return true;                            // this true means that the Hub should submit order to exchange now.
            }
            else
                return false;

        }//TryAddOrder()
        //
        //
        // *************************************
        // ****    TryAddToDefaultBook()    ****
        // *************************************
        /// <summary>
        /// Called by the OrderBookHub to add an order to the default book.
        /// This is typically only done on start up when orders are found
        /// that aren't associated with any strategy and just need to be
        /// stored.
        /// </summary>
        /// <returns></returns>
        public bool TryAddToDefaultBook(Order order)
        {
            m_OrderIDToBookMap.Add(order.Id, m_DefaultBook.BookID);
            // copy here also!
            return m_DefaultBook.TryAdd(order);
        }//TryAddToDefaultBook()
        //
        //
        // *************************************
        // ****         TryGetOrder()       ****
        // *************************************
        //
        //
        /// <summary>
        /// Called by the OrderBookHub to try and get a specific order
        /// </summary>
        /// <param name="orderID"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        public bool TryGetOrder(int orderID, out Order order)
        {
            int bookId;
            if (m_OrderIDToBookMap.TryGetValue(orderID, out bookId))
            {
                OrderBook orderBook;
                if (m_OrderBooks.TryGetValue(bookId, out orderBook))
                { // we have this order book 
                    if (orderBook.IsReady)
                    { // the book is ready 
                        return (orderBook.TryGet(orderID, out order));
                    }
                }
            }
            order = null;
            return false;
        }
        //
        //
        // *************************************
        // ****         TryDeleteOrder()    ****
        // *************************************
        /// <summary>
        /// Called on confirmations only!
        /// Caller would like to try and delete an order.
        /// This move the order out of the "Live" collection
        /// and into the deleted orders collection. It also
        /// sets the state to Dead/Dead.
        /// </summary>
        /// <param name="orderID"></param>
        /// <returns>false if orderID isn't found</returns>
        public bool TryDeleteOrder(int orderID)
        {
            int bookId;
            if (m_OrderIDToBookMap.TryGetValue(orderID, out bookId))
            {
                OrderBook orderBook;
                if (m_OrderBooks.TryGetValue(bookId, out orderBook))
                { // we have this order book 
                    if (orderBook.IsReady)
                    {
                        return orderBook.TryDelete(orderID);
                    }
                }
            }
            return false;
        }
        //
        //
        // *************************************
        // ****      TryChangeOrderPrice    ****
        // *************************************
        /// <summary>
        /// Called when a user would like to change the pending price of an order
        /// Since we keep collections of orders by pending price, we need to make 
        /// sure to complete all bookkeeping correctly.
        /// </summary>
        /// <param name="order"></param>
        /// <param name="newIPrice"></param>
        /// <returns></returns>
        public bool TryChangeOrderPrice(Order order, int newIPrice)
        {
            int bookId;
            if (m_OrderIDToBookMap.TryGetValue(order.Id, out bookId))
            {
                OrderBook orderBook;
                if (m_OrderBooks.TryGetValue(bookId, out orderBook))
                { // we have this order book 
                    if (orderBook.IsReady)
                    {
                        orderBook.ProcessPriceChange(order, newIPrice);
                        return true;
                    }
                }
            }
            return false;
        }
        //
        // *****************************************
        // ****     TryTransferOrderToNewBook   ****
        // *****************************************
        /// <summary>
        /// Called to move an order from one book to another book.
        /// </summary>
        public bool TryTransferOrderToNewBook(Order order, OrderBook newOrderBook)
        {
            int currentBookId;
            if (m_OrderIDToBookMap.TryGetValue(order.Id, out currentBookId))
            {
                OrderBook currentOrderBook;
                if (m_OrderBooks.TryGetValue(currentBookId, out currentOrderBook))
                { // we have this order book 
                    if (currentOrderBook.IsReady & newOrderBook.IsReady)
                    {   // both books are ready 
                        if (currentOrderBook.TryRemove(order))
                        {   // we found the order and were able to remove it
                            if(newOrderBook.TryAdd(order))
                            {   // we were able to add it to the new book, make sure to fix mapping of order id to new book
                                m_OrderIDToBookMap[order.Id] = newOrderBook.BookID;
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
        //
        //
        // *****************************************
        // ****       GetAllOrdersBooks()       ****
        // *****************************************
        /// <summary>
        /// Caller would like a list of all order books for this OrderInstrument
        /// </summary>
        /// <param name="orderBookList"></param>
        public void GetAllOrderBooks(ref List<OrderBook> orderBookList)
        {
            orderBookList.AddRange(m_OrderBooks.Values);
        }
        //
        //
        //
        // *************************************
        // ****         TryProcessFill()    ****
        // *************************************
        //
        /// <summary>
        /// Called by the execution listener when an order is filled.
        /// This method will update state, and send out events.
        /// </summary>
        /// <param name="fillEventArgs"></param>
        /// <returns></returns>
        public bool TryProcessFill(FillEventArgs fillEventArgs)
        {
            int bookId;
            if (m_OrderIDToBookMap.TryGetValue(fillEventArgs.OrderId, out bookId))
            {
                OrderBook orderBook;
                if (m_OrderBooks.TryGetValue(bookId, out orderBook))
                { // we have this order book 
                    if (orderBook.IsReady)
                    {
                        return orderBook.TryProcessFill(fillEventArgs);
                    }
                }
            }
            return false;
        }
        //
        //
        // *************************************
        // ****     OrderStateChanged()    ****
        // *************************************
        /// <summary>
        /// Called when an order is filled, rejected, or cancelled only
        /// </summary>
        /// <param name="orderEventArgs"></param>
        /// <returns></returns>
        public bool OrderStateChanged(OrderEventArgs orderEventArgs)
        {
            int bookId;
            if (m_OrderIDToBookMap.TryGetValue(orderEventArgs.Order.Id, out bookId))
            {
                OrderBook orderBook;
                if (m_OrderBooks.TryGetValue(bookId, out orderBook))
                { // we have this order book 
                    if (orderBook.IsReady)
                    {
                        orderEventArgs.OrderBookID = bookId;
                        orderBook.OnOrderStateChanged(orderEventArgs);
                        return true;
                    }
                }
            }
            return false;
        }
        //
        //
        //
        // *************************************
        // ****      OrderSubmitted()       ****
        // *************************************
        /// <summary>
        /// Called when we get a confirmation for an orders submission
        /// </summary>
        /// <param name="orderEventArgs"></param>
        /// <returns></returns>
        public bool OrderSubmitted(OrderEventArgs orderEventArgs)
        {
            int bookId;
            if (m_OrderIDToBookMap.TryGetValue(orderEventArgs.Order.Id, out bookId))
            {
                OrderBook orderBook;
                if (m_OrderBooks.TryGetValue(bookId, out orderBook))
                { // we have this order book 
                    if (orderBook.IsReady)
                    {
                        orderEventArgs.OrderBookID = bookId;
                        orderBook.OnOrderSubmitted(orderEventArgs);
                        return true;
                    }
                }
            }
            return false;
        }
        //
        // ************************************
        // ****         ToString()          ***
        // ************************************
        public override string ToString()
        {
            int n = this.m_OrderBooks.Count;    // TODO: could this cause a thread problem?
            return string.Format("{0} {1} books.", this.Instrument, n);
        }//
        //
        //
        //
        // *************************************
        // ****      ProcessAddConfirm()    ****
        // *************************************
        public void ProcessAddConfirm(Order order)
        {
            OrderBook orderBook;
            if (TryGetOrderBookFromOrder(order, out orderBook))
            {
                orderBook.ProcessAddConfirm(order);
            }
        }
        //
        //
        // *************************************
        // ****      ProcessFoundOrder()    ****
        // *************************************
        /// <summary>
        /// Order has been "found" either at start up
        /// or from outside the system.
        /// </summary>
        /// <param name="order"></param>
        public void ProcessFoundOrder(Order order)
        {
            OrderBook orderBook;
            if (TryGetOrderBookFromOrder(order, out orderBook))
            {
                orderBook.ProcessFoundOrder(order);
            }
        }
        //
        // *************************************
        // ****      ProcessUpdateConfirm   ****
        // *************************************
        public void ProcessUpdateConfirm(Order order)
        {
            OrderBook orderBook;
            if (TryGetOrderBookFromOrder(order, out orderBook))
            {
                orderBook.ProcessUpdateConfirm(order);
            }
        }
        // *************************************
        // ****     ProcessDeleteConfirm()  ****
        // *************************************
        public void ProcessDeleteConfirm(Order order)
        {
            OrderBook orderBook;
            if (TryGetOrderBookFromOrder(order, out orderBook))
            {
                orderBook.ProcessDeleteConfirm(order);
            }
        }
        //
        // *************************************
        // ****      ProcessAddReject()    ****
        // *************************************
        public void ProcessAddReject(Order order)
        {
            OrderBook orderBook;
            if (TryGetOrderBookFromOrder(order, out orderBook))
            {
                orderBook.ProcessAddReject(order);
            }
        }
        #endregion//Public Methods

        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        private bool TryGetOrderBookFromOrder(Order order, out OrderBook orderBook)
        {
            int bookId;
            if (m_OrderIDToBookMap.TryGetValue(order.Id, out bookId))
            {
                if (m_OrderBooks.TryGetValue(bookId, out orderBook))
                { // we have this order book 
                    if (orderBook.IsReady)
                    {
                        return true;
                    }
                }
            }
            orderBook = null;
            return false;
        }
        #endregion//Private Methods

        #region no Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        #endregion//Event Handlers

    }//end class
}
