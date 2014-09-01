using System;

namespace VioletAPI.Lib.DataValidator
{
    public static class MarketDataSchema
    {
        // Basic output data type.
        public static int MarketSymbol = 0;
        public static int LocalDateTime = 1;
        public static int LastPrice = 2;
        public static int LastQty = 3;
        public static int BidPrice = 4;
        public static int BidQty = 5;
        public static int AskPrice = 6;
        public static int AskQty = 7;

        // Bar Type data.
        public static int Open = 8;
        public static int Close = 9;
        public static int High = 10;
        public static int Low = 11;

        // Indicator data type.
        public static int EMA = 12;
        public static int ATR = 13;
        public static int Momentum = 14;

        // Variable total count.
        public static int Count = 15;

        /// <summary>
        /// Write the header of the csv
        /// </summary>
        /// <returns></returns>
        public static string WriteSchema()
        {
            return "Instrument,LocalTime,LastPrice,LastQty,BidPrice,BidQty,AskPrice,AskQty,Open,Close,High,Low,EMA,ATR,Momentum";
        }
    }
}
