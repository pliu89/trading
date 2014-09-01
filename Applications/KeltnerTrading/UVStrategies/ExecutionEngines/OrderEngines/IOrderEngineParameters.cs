using System;


namespace UV.Strategies.ExecutionEngines.OrderEngines
{
    using UV.Lib.OrderBooks;
    using UV.Lib.Fills;
    using UV.Lib.Engines;
    using UV.Strategies.ExecutionHubs;
    // *********************************************************
    // ****            IOrderEngineParameters               ****
    // *********************************************************
    //
    /// <summary>
    /// This exposes the functionality from an OrderEngine that we would also 
    /// like to be exposed to the Strategy side of things.  This will allow
    /// the pricing engine to change remote parameters.
    /// </summary>
    public interface IOrderEngineParameters : IEngine
    {
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
        //
        /// <summary>
        /// If legs of a strategy don't have default acct's assigned this will be for all order's sent.
        /// </summary>
        string DefaultAccount
        {
            get;
        }
    }
}
