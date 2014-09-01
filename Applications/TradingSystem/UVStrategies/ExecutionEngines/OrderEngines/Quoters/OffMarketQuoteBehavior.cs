using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UV.Strategies.ExecutionEngines.OrderEngines
{
    public enum OffMarketQuoteBehavior : int
    {
        Quote = 0,      // normal behavior we always quote.
        NoQuote = 1,    // If we are "Off Market" we pull our quote
        NoUpdate = 2    // if we are "Off Market" we don't update our quote
    }
}
