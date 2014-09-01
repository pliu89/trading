using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace UV.Strategies.ExecutionHubs
{
    using UV.Lib.OrderBooks;
    using UV.Lib.Products;
    using UV.Lib.Engines;

    public interface IExecutionListener
    {
        /// <summary>
        /// get and set methods for the Execution Container.
        /// </summary>
        ExecutionContainer ExecutionContainer
        {
            get;
            set;
        }

        /// <summary>
        /// Called to pass in the execution thread to allow needed functionality to start 
        /// using the correct thread.
        /// </summary>
        void InitializeThread();

        /// <summary>
        /// Called to being shutting thread down when an order engine is ready to stop;
        /// </summary>
        void StopThread();

        /// <summary>
        /// Called by the dispatcher one a thread has pushed a new event onto the queue
        /// to process. 
        /// </summary>
        void ProcessEvent(EventArgs e);

        /// <summary>
        /// Called to subscribe to periodic updates.
        /// </summary>
        /// <param name="iTimerSubscriber"></param>
        void SubscribeToTimer(ITimerSubscriber iTimerSubscriber);

        #region Order Methods

        /// <summary>
        /// Caller would like to create an order book, this method should trigger
        /// OrderBookCreated upon completion. 
        /// Note:
        ///     If you are intending to use a Risk Manager with this orderbook,
        ///     it needs to be created AFTER SetUpBegin is called in all engines
        ///     to ensure the RiskManager is listening for the correct events.
        /// </summary>
        /// <param name="instrumentName"></param>
        /// <returns></returns>
        OrderBook CreateOrderBook(InstrumentName instrumentName);

        /// <summary>
        /// Caller would like to recieve all order books that the execution manager
        /// currently knows about.
        /// </summary>
        /// <returns></returns>
        void GetAllOrderBooks(ref List<OrderBook> orderBookList);

        /// <summary>
        /// Create's a UV type Order will all the neccessary fields, which is then handed back to the caller
        /// to be submitted.
        /// </summary>
        /// <param name="instrumentName"></param>
        /// <param name="tradeSide"></param>
        /// <param name="iPrice"></param>
        /// <param name="qty"></param>
        /// <param name="newOrder"></param>
        /// <returns></returns>
        bool TryCreateOrder(InstrumentName instrumentName, int tradeSide, int iPrice, int qty, out Order newOrder);
        
        /// <summary>
        /// Caller would like to submit an order belonging to a specific orderBook id.
        /// </summary>
        /// <param name="orderBookID"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        bool TrySubmitOrder(int orderBookID, Order order);
        
        /// <summary>
        /// Caller would like to try and delete an order from the market
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        bool TryDeleteOrder(Order order);

        /// <summary>
        /// Caller would like to change an existing orders price
        /// </summary>
        /// <param name="orderToModify"></param>
        /// <param name="newIPrice"></param>
        /// <returns></returns>
        bool TryChangeOrderPrice(Order orderToModify, int newIPrice);

        /// <summary>
        /// Caller would like to change an existing orders qty
        /// </summary>
        /// <param name="orderToModify"></param>
        /// <param name="newQty"></param>
        /// <returns></returns>
        bool TryChangeOrderQty(Order orderToModify, int newQty);
        #endregion // Order Methods

        #region Events

        /// <summary>
        /// Event for creation of OrderBooks, usually useful for risk managers
        /// or those monitoring all fills for a given execution container.
        /// </summary>
        event EventHandler OrderBookCreated;

        /// <summary>
        /// Event for api finding an instrument
        /// </summary>
        event EventHandler InstrumentFound;

        /// <summary>
        /// Event for SingleLegExecutor to grab thread to complete set up function
        /// </summary>
        event EventHandler Initialized;

        /// <summary>
        /// Event for SingleLegExecutor to grab thread to complete tear down prior to 
        /// thread disosal.
        /// </summary>
        event EventHandler Stopping;

        #endregion //Events
    }
}
