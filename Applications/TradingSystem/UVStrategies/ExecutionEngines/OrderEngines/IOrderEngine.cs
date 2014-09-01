using System;


namespace UV.Strategies.ExecutionEngines.OrderEngines
{
    using UV.Lib.OrderBooks;
    using UV.Lib.Products;
    using UV.Lib.Fills;
    using UV.Lib.Engines;
    using UV.Strategies.ExecutionHubs;
    // *********************************************************
    // ****                 IOrderEngine                    ****
    // *********************************************************
    //
    /// <summary>
    /// This exposes the functionality that a execution object must 
    /// call in order for it to send orders and recieve fill information
    /// </summary>
    public interface IOrderEngine : IOrderEngineParameters
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
        /// This is the default fill book used for risk. For a spreader, it should contain
        /// synthetic fills.  For a single leg, the actual fills are sufficient...it is a method to hide
        /// the property from the gui
        /// </summary>
        FillBook GetFillBook();
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
        //
        /// <summary>
        /// Get and Set Methods to expose IExecutionListner without properties so it is not exposed in the gui.
        /// </summary>
        ExecutionListener GetExecutionListener();
        void SetExecutionListener(ExecutionListener executionListener);
        //
        //
        //void ExecutionListener_Intialized(object sender, EventArgs eventArgs);
        ////
        ////
        //void ExecutionListener_Stopping(object sender, EventArgs eventArgs);
        ////
        ////
        //void ExecutionListener_InstrumentsFound(object sender, EventArgs eventArgs);
    }
}
