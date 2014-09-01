using System;
using System.Collections.Generic;


namespace UV.Lib.MarketHubs
{
    public class MarketStatusChangedEventArg : EventArgs
    {
        public List<string> MarketNameList = new List<string>();
        //public List<bool> IsMarketLive = new List<bool>();
    }

}
