using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace VioletAPI.Lib
{
    using UV.Lib.Hubs;
    using UV.Strategies .StrategyHubs ;

    public static class Global
    {
        public static Form TradingStrategyMainForm = null;                                                              // Keep a pointer to the mainform.
        public static LogHub TraderLog = null;                                                                          // Create a trader required log.
        public static List<Strategy> Strategies = new List<Strategy>();
    }
}
