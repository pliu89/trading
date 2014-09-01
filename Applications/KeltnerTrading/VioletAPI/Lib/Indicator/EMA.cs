using System;
using System.Collections.Generic;

namespace VioletAPI.Lib.Indicator
{
    using Functions = Lib.TradingHelper.Functions;

    public class EMA : IndicatorBase
    {

        #region Members
        private int m_Length = -1;                                                                                  // EMA length
        private double[] m_CircularBuffer_Close;                                                                    // Circular buffer for EMA, only need close price.
        #endregion


        #region Constructor
        public EMA(int id, string name, int length)
            : base(id, name, IndicatorType.EMA)
        {
            m_Length = length;
            m_CircularBuffer_Close = new double[m_Length];
        }
        #endregion


        #region Public Methods
        /// <summary>
        /// Feed EMA.
        /// </summary>
        /// <param name="localTimeNow"></param>
        /// <param name="newPrice"></param>
        public override void FeedIndicator(DateTime localTimeNow, double newPrice)
        {
            if (localTimeNow > m_ExchangeCloses[0])
            {
                if (m_LastFeedDateTime < m_ExchangeCloses[0])
                {
                    if (!m_ReadyFlag)
                    {
                        m_CircularBuffer_Close[m_IndicatorProgress] = m_LastFeed;
                        m_IndicatorProgress++;
                        SetFirstIndicatorValue();
                    }
                    m_LastFeedDateTime = localTimeNow;
                }

                List<DateTime> tempExchangeOpens = new List<DateTime>(m_ExchangeOpens);
                List<DateTime> tempExchangeCloses = new List<DateTime>(m_ExchangeCloses);
                while (localTimeNow > tempExchangeCloses[0])
                {
                    tempExchangeCloses[0] = tempExchangeCloses[0].AddDays(1);
                    tempExchangeOpens[0] = tempExchangeOpens[0].AddDays(1);
                }
                tempExchangeCloses.Sort();
                tempExchangeOpens.Sort();

                if (localTimeNow <= tempExchangeOpens[0])
                    return;

                m_ExchangeOpens = tempExchangeOpens;
                m_ExchangeCloses = tempExchangeCloses;
                m_NextBarUpdateDateTime = Functions.GetNextBarUpdateDateTime(localTimeNow, m_BarUpdateIntervalSecond);
            }

            // Update bar data.
            if (localTimeNow >= m_NextBarUpdateDateTime)
            {
                if (!m_ReadyFlag)
                {
                    m_CircularBuffer_Close[m_IndicatorProgress] = newPrice;
                    m_IndicatorProgress++;
                    SetFirstIndicatorValue();
                }
                else
                {
                    m_LastValue = m_PreviousValue + (newPrice - m_PreviousValue) * 2 / (1 + m_Length);
                    m_PreviousValue = m_LastValue;
                }
                m_NextBarUpdateDateTime = m_NextBarUpdateDateTime.AddSeconds(m_BarUpdateIntervalSecond);
            }
            else
            {
                if (m_ReadyFlag && m_AllowHighFrequencyUpdate)
                    m_LastValue = m_PreviousValue + (newPrice - m_PreviousValue) * 2 / (1 + m_Length);
            }

            // Record last values.
            m_LastFeedDateTime = localTimeNow;
            m_LastFeed = newPrice;
        }
        #endregion


        #region Private Methods
        /// <summary>
        /// Called when the first value of the indicator is calculated.
        /// </summary>
        private void SetFirstIndicatorValue()
        {
            if (m_IndicatorProgress == m_Length)
            {
                m_ReadyFlag = true;
                double meanValueInitial = 0.0;
                foreach (double dataPoint in m_CircularBuffer_Close)
                {
                    meanValueInitial += dataPoint;
                }
                meanValueInitial /= m_Length;
                m_PreviousValue = m_LastValue = meanValueInitial;
            }
        }
        #endregion

    }
}
