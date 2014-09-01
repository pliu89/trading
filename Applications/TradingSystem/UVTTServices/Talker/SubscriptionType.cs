using System;
using System.Collections.Generic;
using System.Text;

namespace UV.TTServices.Talker
{
    public enum SubscriptionType : uint
    {
        Unknown = 0
        ,Position
        ,AvePositionCost
        ,UnRealPnL                  // unreal pnl from open positions
        ,RealPnL                    // pnl realized since last PnL reset
        ,StartingRealPnL            // cummulative PnL up to last PnL. (So total real PnL is the sum of these two).
        ,Volume                     // Total volume traded today (since last reset).
        ,StartingVolume             // cummulative volumn up to the last time we reset.  Total volume is sum of Volume + StartingVolume


    }
}
