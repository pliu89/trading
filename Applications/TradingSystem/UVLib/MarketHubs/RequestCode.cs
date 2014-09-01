using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

namespace UV.Lib.MarketHubs
{
    /// <summary>
    /// Request code for MarketHubs.
    /// </summary>
    public enum RequestCode
    {

        None = 0
            //
            // Instrument, products, markets requests
            //
        ,RequestServers
        ,RequestProducts
        ,RequestInstruments
        ,RequestInstrumentPriceSubscription
        ,RequestInstrumentTimeAndSalesSubscription
            //
            // Server control requests
            //
        ,RequestStart                   // request by calling Start()
        ,RequestRun                     // 
        ,RequestShutdown                // requested by calling Stop()



    }
}
