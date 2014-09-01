using System;

namespace UV.Lib.OrderBookHubs
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
        MarketOrder = 2


    }



    // *************************************
    // ****         OrderTIF            ****
    // *************************************
    public enum OrderTIF
    {
        GTD = 0,
        GTC = 1
    }
}
