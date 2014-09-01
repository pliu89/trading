using System;

namespace VioletAPI.Lib.StrategyRiskControl
{
    using Lib.TradingHelper;

    public class StopOrderEventArgs : EventArgs
    {

        #region Members
        public StopOrderEventType StopOrderEventType = StopOrderEventType.Unknown;
        public double StopPrice = double.NaN;
        public int StopQty = 0;
        public TradeSide TradeSide = TradeSide.Unknown;
        #endregion


        #region Constructor
        public StopOrderEventArgs()
        {

        }
        #endregion
        
    }
}
