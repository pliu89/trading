using System;

namespace VioletAPI.Lib.StrategyRiskControl
{
    /// <summary>
    /// Stop order event type enumeration.
    /// </summary>
    public enum StopOrderEventType
    {
        TrailingStop,
        StopLoss,
        StopEnter,
        Unknown
    }
}
