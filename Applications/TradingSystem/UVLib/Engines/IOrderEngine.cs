using System;


namespace UV.Lib.Engines
{
    using UV.Lib.OrderBookHubs;
    using UV.Lib.Products;
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
        /// <summary>
        /// Filled is the function that must be implemented in order to get fill events
        /// from the strategy hub into your strategy.
        /// </summary>
        /// <param name="fillEventArgs"></param>
        void Filled(FillEventArgs fillEventArgs);
        //
        //
        /// <summary>
        /// OrderStateChanged is the functions that must be implemented in order to get major order
        /// state events
        /// </summary>
        /// <param name="orderEventArg"></param>
        void OrderStateChanged(OrderEventArgs orderEventArg);
        //
        //
        /// <summary>
        /// Method to get updates for order submissions (not a major event and not included in 
        /// order state change events)
        /// </summary>
        /// <param name="orderEventArg"></param>
        void OrderSubmitted(OrderEventArgs orderEventArg);

    }
}
