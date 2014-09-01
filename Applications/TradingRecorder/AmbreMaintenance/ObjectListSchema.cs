using System;

namespace AmbreMaintenance
{
    /// <summary>
    /// This is the column IDs for audit trail file.
    /// </summary>
    public static class ObjectListSchema
    {
        public const int LocalTimeStamp = 0;
        public const int ExchangeName = 1;
        public const int OrderStatus = 2;
        public const int OrderAction = 3;
        public const int OrderSide = 4;
        public const int OrderQty = 5;
        public const int Product = 6;
        public const int Contract = 7;
        public const int OrderPrice = 8;
        public const int AccountName = 9;
        public const int UserName = 10;
        public const int ExchangeTime = 11;
        public const int ExchangeDate = 12;
        public const int TradeSource = 13;
        public const int TTOrderKey = 14;
        public const int TTSeriesKey = 15;
    }
}
