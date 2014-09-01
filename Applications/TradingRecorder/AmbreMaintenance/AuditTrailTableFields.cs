using System;

namespace AuditTrailReading
{
    /// <summary>
    /// This is the column IDs for audit trail file.
    /// </summary>
    public static class AuditTrailTableFields
    {
        public const int LocalTimeStamp = 0;
        public const int ExchangeName = 1;
        public const int OrderStatus = 2;
        public const int OrderAction = 4;
        public const int OrderSide = 5;
        public const int OrderQty = 8;
        public const int Product = 9;
        public const int Contract = 10;
        public const int OrderPrice = 12;
        public const int AccountName = 33;
        public const int UserName = 38;
        public const int ExchangeTime = 45;
        public const int ExchangeDate = 46;
        public const int TradeSource = 48;
        public const int TTOrderKey = 53;
        public const int TTSeriesKey = 70;
    }
}
