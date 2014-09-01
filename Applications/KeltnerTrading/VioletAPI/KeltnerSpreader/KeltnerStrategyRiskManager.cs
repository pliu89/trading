using System;

namespace VioletAPI.KeltnerSpreader
{
    using Lib.StrategyRiskControl;

    public class KeltnerStrategyRiskManager : StrategyRiskManagerBase
    {

        #region Constructor
        /// <summary>
        /// Initialize the risk check variables in the constructor.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="keltnerTradingVariables"></param>
        public KeltnerStrategyRiskManager(string name, KeltnerTradingVariables keltnerTradingVariables)
            : base(name)
        {
            this.m_MaxNetPosition = keltnerTradingVariables.MaxNetPosition;
            this.m_MaxTotalFills = keltnerTradingVariables.MaxTotalFills;
            this.m_StopLossTrackingTime = keltnerTradingVariables.StopLossTimeTrack;
        }
        #endregion


        #region Public Methods
        /// <summary>
        /// Detect risk events based on fills. Output error information if possible.
        /// </summary>
        /// <param name="qty"></param>
        /// <param name="errorInfo"></param>
        /// <returns></returns>
        public override bool DetectFillsRiskEvent(int qty, out string errorInfo)
        {
            if (base.DetectFillsRiskEvent(qty, out errorInfo))
                return true;
            else
                return false;
        }
        #endregion

    }
}
