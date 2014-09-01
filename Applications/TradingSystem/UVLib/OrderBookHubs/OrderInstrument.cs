using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
//using System.Linq;
//using System.Text;

namespace UV.Lib.OrderBookHubs
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
        private ConcurrentDictionary<int, int> m_OrderIDToBookMap;  // Map: OrderID --> OrderBookID, where to find a specific order.        
        private OrderBook m_DefaultBook;                            // default order book for uknown orders not owned by a strat(yet)
        #endregion// members

        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        /// <summary>
        /// Main constructor for Order Instrument.  An RecycleFactory<Order> must
        /// be passed in for the deafult order book to be constructed
        /// </summary>
        /// <param name="instrumentName"></param>
        /// <param name="details"></param>
        /// <param name="orderRecycleFactory"></param>
        public OrderInstrument(InstrumentName instrumentName, InstrumentDetails details, RecycleFactory<Order> orderRecycleFactory)
        {
            this.Instrument = instrumentName;
            this.Details = details;
            m_OrderIDToBookMap = new ConcurrentDictionary<int, int>();
            m_DefaultBook = new OrderBook(instrumentName, orderRecycleFactory); // create our default book
            m_OrderBooks.Add(m_DefaultBook.BookID, m_DefaultBook);              // add it to our look up tables
            m_DefaultBook.IsReady = true;
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
                if (m_OrderIDToBookMap.TryAdd(order.Id, orderBookId))
                {
                    orderBook.TryAdd(order);
                    return true;                            // this true means that the Hub should submit order to exchange now.
                }
                else
                    return false;
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
            if (m_OrderIDToBookMap.TryAdd(order.Id, m_DefaultBook.BookID))
            {
                // copy here also!
                return m_DefaultBook.TryAdd(order);
            }
            else
                return false;
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

        //
        //
        //
        //
        // *************************************
        // ****         OrderFilled()       ****
        // *************************************
        public bool OrderFilled(FillEventArgs fillEventArgs)
        {
            int bookId;
            if (m_OrderIDToBookMap.TryGetValue(fillEventArgs.OrderId, out bookId))
            {
                OrderBook orderBook;
                if (m_OrderBooks.TryGetValue(bookId, out orderBook))
                { // we have this order book 
                    if (orderBook.IsReady)
                    {
                        fillEventArgs.OrderBookID = bookId;
                        orderBook.OnOrderFilled(fillEventArgs);
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
        //
        // ****         ToString()          ***
        //
        public override string ToString()
        {
            int n = this.m_OrderBooks.Count;    // TODO: could this cause a thread problem?
            return string.Format("{0} {1} books.", this.Instrument, n);
        }//
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
        public void ProcessRequest(RequestEventArg<OrderRequestType> orderReq)
        {
            int orderId = (int)orderReq.Data[1];
            int orderBookId;
            if (m_OrderIDToBookMap.TryGetValue(orderId, out orderBookId))
            {
                OrderBook orderBook;
                if(m_OrderBooks.TryGetValue(orderBookId, out orderBook))
                {
                    orderBook.ProcessRequest(orderId, orderReq);
                }
            }
        }
        #endregion//Public Methods

        #region no Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
      
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
