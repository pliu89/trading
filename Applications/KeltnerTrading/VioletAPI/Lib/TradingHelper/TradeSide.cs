using System;

namespace VioletAPI.Lib.TradingHelper
{
    public enum TradeSide
    {
        Buy,
        Sell,
        Unknown
    }

    public class OrderSide
    {
        public const int BuySide = 0;
        public const int SellSide = 1;
        public const int Unknown = 2;
    }
}
