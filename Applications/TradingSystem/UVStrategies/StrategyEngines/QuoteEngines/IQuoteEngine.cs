using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
//using System.Threading.Tasks;

namespace UV.Strategies.StrategyEngines.QuoteEngines
{
    using UV.Lib.Engines;
    using UV.Lib.BookHubs;
    using UV.Lib.Fills;
    using UV.Lib.OrderBooks;


    public interface IQuoteEngine : IEngine
    {

        void MarketInstrumentChanged(Book marketBook, InstrumentChangeArgs eventArgs);


        void Quote(PricingEngine pricingEngine, int tradeSide, double price, int qty, QuoteReason quoteReason);
        void UpdateQuotes(bool forceUpdate=false);
        bool ProcessSyntheticOrder(SyntheticOrder syntheticOrder, List<Fill> syntheticFills);

    }
}
