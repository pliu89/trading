using System;
using System.Collections.Generic;

namespace VioletAPI.KeltnerSpreader
{
    using VioletAPI.Lib.Indicator;
    using Functions = Lib.TradingHelper.Functions;

    public class KeltnerIndicatorManager : IndicatorManagerBase
    {

        #region Members
        private KeltnerTradingVariables m_KeltnerTradingVariables = null;                                                               // Trading variables information.
        private int m_BarUpdateIntervalSecond = 300;                                                                                    // Trading bar interval.

        private EMA m_EMA = null;                                                                                                       // EMA.
        private string m_EMASeriesName = "EMA";                                                                                         // EMA Name.
        private int m_EMASeriesID = 0;                                                                                                  // EMA ID.
        private int m_EMALength;                                                                                                        // EMA Length.

        private ATR m_ATR = null;                                                                                                       // ATR.
        private string m_ATRSeriesName = "ATR";                                                                                         // ATR Name.
        private int m_ATRSeriesID = 1;                                                                                                  // ATR ID.
        private int m_ATRLength;                                                                                                        // ATR Length.

        private MOM m_MOM = null;                                                                                                       // MOM.
        private string m_MOMSeriesName = "MOM";                                                                                         // MOM Name.
        private int m_MOMSeriesID = 2;                                                                                                  // MOM ID.
        private int m_MOMLength;                                                                                                        // MOM Length.
        #endregion


        #region Properties
        public double LastEMA
        {
            get { return m_EMA.Last; }
        }
        public double LastATR
        {
            get { return m_ATR.Last; }
        }
        public double LastMomentum
        {
            get { return m_MOM.Last; }
        }
        #endregion


        #region Constructor
        /// <summary>
        /// keltner constructor.
        /// </summary>
        /// <param name="initialNextUpdateDateTime"></param>
        /// <param name="keltnerTradingVariables"></param>
        /// <param name="marketOpens"></param>
        /// <param name="marketCloses"></param>
        public KeltnerIndicatorManager(DateTime initialNextUpdateDateTime, KeltnerTradingVariables keltnerTradingVariables, List<DateTime> marketOpens, List<DateTime> marketCloses)
            : base()
        {
            // Load keltner trading parameters.
            m_KeltnerTradingVariables = keltnerTradingVariables;
            m_EMALength = keltnerTradingVariables.EMALength;
            m_ATRLength = keltnerTradingVariables.ATRLength;
            m_MOMLength = keltnerTradingVariables.MomentumLength;
            m_BarUpdateIntervalSecond = keltnerTradingVariables.BarIntervalInSeconds;

            // Create indicators.
            m_EMA = new EMA(m_EMASeriesID, m_EMASeriesName, m_EMALength);
            m_ATR = new ATR(m_ATRSeriesID, m_ATRSeriesName, m_ATRLength);
            m_MOM = new MOM(m_MOMSeriesID, m_MOMSeriesName, m_MOMLength);
            this.AddIndicator(m_EMA, m_EMASeriesName);
            this.AddIndicator(m_ATR, m_ATRSeriesName);
            this.AddIndicator(m_MOM, m_MOMSeriesName);

            // Initial setup for the trading indicators.
            initialNextUpdateDateTime = Functions.GetNextBarUpdateDateTime(initialNextUpdateDateTime, m_BarUpdateIntervalSecond);
            m_EMA.SetupIndicator(m_BarUpdateIntervalSecond, initialNextUpdateDateTime, marketOpens, marketCloses);
            m_ATR.SetupIndicator(m_BarUpdateIntervalSecond, initialNextUpdateDateTime, marketOpens, marketCloses);
            m_MOM.SetupIndicator(m_BarUpdateIntervalSecond, initialNextUpdateDateTime, marketOpens, marketCloses);
        }
        #endregion


        #region Public Methods
        /// <summary>
        /// Get the last values for indicators only when all the indicators are ready.
        /// </summary>
        /// <param name="lastEMA"></param>
        /// <param name="lastATR"></param>
        /// <param name="lastMOM"></param>
        /// <returns></returns>
        public bool TryGetIndicatorValues(out double lastEMA, out double lastATR, out double lastMOM)
        {
            lastEMA = m_EMA.Last;
            lastATR = m_ATR.Last;
            lastMOM = m_MOM.Last;

            // Check if the indicators are ready to use.
            if (m_EMA.IsReady && m_ATR.IsReady && m_MOM.IsReady)
            {
                // Check whether there is NaN values for the indicators.
                if (!double.IsNaN(lastEMA) && !double.IsNaN(lastATR) && !double.IsNaN(lastMOM))
                    return true;
            }
            return false;
        }
        #endregion

    }
}
