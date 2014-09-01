using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UV.Strategies.StrategyEngines
{
    using UV.Lib.BookHubs;

    public interface IMarketSubscriber
    {

        //bool MarketInstrumentInitialized(Book marketBook);

        bool MarketInstrumentChanged(Book marketBook, InstrumentChangeArgs eventArgs);

    }
}
