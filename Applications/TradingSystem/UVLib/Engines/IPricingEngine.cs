using System;


namespace UV.Lib.Engines
{
    using UV.Lib.BookHubs;


    // *********************************************************
    // ****                 I Pricing Engine                ****
    // *********************************************************
    //
    /// <summary>
    /// This exposes the functionality that a Strategy object must 
    /// call in order for it to re-price itself.
    /// Notes:
    /// 1. This interface is in the engine library so that Engine.cs base class
    /// knows of its existance, and the exposed properties of this interface will 
    /// be ignored by the automatic control creation.
    /// </summary>
    public interface IPricingEngine : IEngine
    {

        // Market events
        void MarketInstrumentInitialized(Book marketBook);
        bool MarketInstrumentChanged(Book marketBook, InstrumentChangeArgs eventArgs);

        // Fill events
        void Filled(UV.Lib.OrderBooks.SyntheticOrder tradeEventArgs);
        void Filled(UV.Lib.Fills.Fill fill);

        //void ProcessClusterEvent(BGTLib.FrontEnds.Clusters.ClusterEventArgs eventArgs);
    
    }
}
