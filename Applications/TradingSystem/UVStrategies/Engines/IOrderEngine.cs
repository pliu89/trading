using System;


namespace UV.Strategies.Engines
{
    using UV.Lib.OrderBooks;
    using UV.Lib.Products;
    using UV.Lib.Fills;
    using UV.Lib.Engines;
    // *********************************************************
    // ****                 IOrderEngine                    ****
    // *********************************************************
    //
    /// <summary>
    /// This exposes the functionality that a Strategy object must 
    /// call in order for it to send orders and recieve fill information
    /// </summary>
    public interface IOrderEngine : IEngine
    {
        //
        //
        /// <summary>
        /// Method to cancel all orders that an order engine is controlling.
        /// Can be used for risk or other clean up procedures.
        /// </summary>
        void CancelAllOrders();
        //
        //
        /// <summary>
        /// Method to request that an order engine send orders at a particular price and qty.
        /// qty should be a signed int.
        /// </summary>
        /// <param name="tradeSide"></param>
        /// <param name="price"></param>
        /// <param name="qty"></param>
        /// <param name="aBook"></param>
        void Quote(int tradeSide, double price, int qty, UV.Lib.BookHubs.Book aBook);
        //
        /// <summary>
        /// qty to show to the market.
        /// </summary>
        int DripQty
        {
            get;
            set;
        }
        //
        //
        /// <summary>
        /// This is the minimum difference allowed for an Order Engine to change prices.  
        /// For a single leg strategy this should be the minimum 
        /// </summary>
        double QuoteTickSize
        {
            get;
            set;
        }
        //
        /// <summary>
        /// Has risk verified that is order engine is okay to send orders.
        /// </summary>
        bool IsRiskCheckPassed
        {
            get;
            set;
        }
        //
        //
        /// <summary>
        /// User defined flag for allow live orders to go out into the market.
        /// </summary>
        bool IsUserTradingEnabled
        {
            get;
            set;
        }
        //
        /// <summary>
        /// This is the default fill book used for risk. For a spreader, it should contain
        /// synthetic fills.  For a single leg, the actual fills are sufficient.
        /// </summary>
        FillBook FillBook
        {
            get;
            set;
        }
        //
        //
        /// <summary>
        /// Method called from execution hub to allow order engine to create its thread and start set up.
        /// </summary>
        void Start();
        //
        /// <summary>
        /// Method called from execution hub to allow order engine and its thread to shutdown nicely.
        /// </summary>
        void Stop();
    }
}
