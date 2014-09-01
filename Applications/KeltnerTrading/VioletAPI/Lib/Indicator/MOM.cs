using System;
using System.Collections.Generic;

namespace VioletAPI.Lib.Indicator
{
    using Functions = Lib.TradingHelper.Functions;

    public class MOM : IndicatorBase
    {

        #region Members
        private int m_Length = -1;
        private double[] m_CircularBuffer_Close;
        #endregion


        #region Constructor
        public MOM(int id, string name, int length)
            : base(id, name, IndicatorType.MOM)
        {
            m_Length = length;
            m_CircularBuffer_Close = new double[m_Length];
        }
        #endregion


        #region Public Methods
        /// <summary>
        /// Feed MOM.
        /// </summary>
        /// <param name="localTimeNow"></param>
        /// <param name="newPrice"></param>
        public override void FeedIndicator(DateTime localTimeNow, double newPrice)
        {
            if (localTimeNow > m_ExchangeCloses[0])
            {
                if (m_LastFeedDateTime < m_ExchangeCloses[0])
                {
                    m_IndicatorProgress %= m_Length;
                    m_CircularBuffer_Close[m_IndicatorProgress] = m_LastFeed;
                    m_IndicatorProgress++;
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
                    if (m_IndicatorProgress < m_Length)
                        m_CircularBuffer_Close[m_IndicatorProgress] = newPrice;
                    else if (m_IndicatorProgress == m_Length)
                    {
                        m_ReadyFlag = true;
                        m_PreviousValue = m_LastValue = newPrice - m_CircularBuffer_Close[0];
                        m_IndicatorProgress = 0;
                        m_CircularBuffer_Close[0] = newPrice;
                    }
                }
                else
                {
                    if (m_IndicatorProgress >= m_Length)
                        m_IndicatorProgress %= m_Length;
                    m_PreviousValue = m_LastValue = newPrice - m_CircularBuffer_Close[m_IndicatorProgress];
                    m_CircularBuffer_Close[m_IndicatorProgress] = newPrice;
                }
                m_IndicatorProgress++;
                m_NextBarUpdateDateTime = m_NextBarUpdateDateTime.AddSeconds(m_BarUpdateIntervalSecond);
            }
            else
            {
                if (m_ReadyFlag && m_AllowHighFrequencyUpdate)
                    m_LastValue = newPrice - m_CircularBuffer_Close[m_IndicatorProgress];
            }

            // Record last values.
            m_LastFeedDateTime = localTimeNow;
            m_LastFeed = newPrice;
        }
        #endregion

    }
}