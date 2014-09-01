using System;

namespace UV.Lib.OrderBooks
{


    // *************************************
    // ****        OrderState           ****
    // *************************************
    public enum OrderState
    {
        Unsubmitted = 0,
        Submitted = 1,
        Dead = 2
    }



    // *************************************
    // ****         OrderType           ****
    // *************************************
    public enum OrderType
    {
        Unknown = 0,
        LimitOrder = 1,
        MarketOrder = 2,
        StopLimitOrder = 3,

    }



    // *************************************
    // ****         OrderTIF            ****
    // *************************************
    public enum OrderTIF
    {
        GTD = 0,
        GTC = 1
    }

    // *************************************
    // ****         OrderReason         ****
    // *************************************
    /// <summary>
    /// User defined enum for order reasons
    /// </summary>
    public enum OrderReason
    {
        Unknown = 0,
        Quote = 1,
        Hedge = 2,
        Scratch = 3,
        Squeeze = 4,
        LayOff = 5,
    }
}
