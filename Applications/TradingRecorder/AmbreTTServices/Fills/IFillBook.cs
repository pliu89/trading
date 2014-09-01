using System;
using System.Collections.Generic;
using System.Threading;

namespace Ambre.TTServices.Fills
{
    using Misty.Lib.OrderHubs;
    using Misty.Lib.Products;
    using Misty.Lib.IO.Xml;

    public interface IFillBook : IStringifiable
    {
        InstrumentName Name { get; set; }
        double DollarPerPoint { get; set; }
        double SmallestFillPriceIncr { get; set; }
        ReaderWriterLockSlim Lock { get; }
        double RealizedDollarGains { get; set; }
        double RealizedStartingDollarGains { get; set; }
        int NetPosition { get; }
        double AveragePrice { get; }
        List<Fill> Fills { get; }
        int Volume { get; }
        int StartingVolume { get; }
        string CurrencyName { get; set; }
        double CurrencyRate { get; set; }

        void Add(Fill aFill);
        //void DeleteAllFills();
        void ResetRealizedDollarGains();
        void ResetRealizedDollarGains(double todaysRealPnL, double openingRealPnL);
        double UnrealizedDollarGains();
        double UnrealizedDollarGains(double midPrices);
        void RecalculateAll();

        bool TryAdd(FillEventArgs eventArg, out RejectedFills.RejectedFillEventArgs rejection);
        bool IsFillNew(FillEventArgs fillEventArgs, out RejectedFills.RejectedFillEventArgs rejectedEventArgs);
    }
}
