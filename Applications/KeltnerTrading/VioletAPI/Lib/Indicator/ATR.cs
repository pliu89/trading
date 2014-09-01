using System;
using System.Collections.Generic;

namespace VioletAPI.Lib.Indicator
{
    using Functions = Lib.TradingHelper.Functions;

    public class ATR : IndicatorBase
    {

        #region Members
        private int m_Length = -1;
        private double[] m_CircularBuffer_Close;
        private double[] m_CircularBuffer_High;
        private double[] m_CircularBuffer_Low;
        private double m_PreviousClose = double.NaN;
        private double m_Close = double.NaN;
        private double m_High = double.NaN;
        private double m_Low = double.NaN;
        private bool m_InitialPriceSettings = false;
        #endregion


        #region Constructor
        public ATR(int id, string name, int length)
            : base(id, name, IndicatorType.ATR)
        {
            m_Length = length;
            m_CircularBuffer_Close = new double[m_Length + 1];
            m_CircularBuffer_High = new double[m_Length + 1];
            m_CircularBuffer_Low = new double[m_Length + 1];
        }
        #endregion


        #region Public Methods
        /// <summary>
        /// Required to set initial value to the high, low, open and close prices.
        /// </summary>
        /// <param name="newPrice"></param>
        public void SetInitialHLOPValues(double newPrice)
        {
            m_PreviousClose = m_Close = m_High = m_Low = newPrice;
            m_InitialPriceSettings = true;
        }
        //
        //
        /// <summary>
        /// Feed ATR.
        /// </summary>
        /// <param name="localTimeNow"></param>
        /// <param name="newPrice"></param>
        public override void FeedIndicator(DateTime localTimeNow, double newPrice)
        {
            if (!m_InitialPriceSettings)
                SetInitialHLOPValues(newPrice);

            // Exclude the bad data out of range of exchange trading from the database.
            if (localTimeNow > m_ExchangeCloses[0])
            {
                if (m_LastFeedDateTime < m_ExchangeCloses[0])
                {
                    if (!m_ReadyFlag)
                    {
                        m_CircularBuffer_Close[m_IndicatorProgress] = m_Close;
                        m_CircularBuffer_High[m_IndicatorProgress] = m_High;
                        m_CircularBuffer_Low[m_IndicatorProgress] = m_Low;
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
                m_PreviousClose = m_Close;
                m_Close = m_High = m_Low = newPrice;
                m_NextBarUpdateDateTime = Functions.GetNextBarUpdateDateTime(localTimeNow, m_BarUpdateIntervalSecond);
            }

            // Update bar data.
            if (localTimeNow >= m_NextBarUpdateDateTime)
            {
                if (!m_ReadyFlag)
                {
                    m_CircularBuffer_Close[m_IndicatorProgress] = newPrice;
                    m_CircularBuffer_High[m_IndicatorProgress] = m_High;
                    m_CircularBuffer_Low[m_IndicatorProgress] = m_Low;
                    m_IndicatorProgress++;
                    SetFirstIndicatorValue();
                }
                else
                {
                    m_LastValue = m_PreviousValue + (Math.Max(m_High, m_PreviousClose) - Math.Min(m_Low, m_PreviousClose) - m_PreviousValue) / m_Length;
                    m_PreviousValue = m_LastValue;
                }
                m_PreviousClose = m_Close;
                m_Close = m_High = m_Low = newPrice;
                m_NextBarUpdateDateTime = m_NextBarUpdateDateTime.AddSeconds(m_BarUpdateIntervalSecond);
            }
            else
            {
                if (newPrice > m_High)
                    m_High = newPrice;
                if (newPrice < m_Low)
                    m_Low = newPrice;
                m_Close = newPrice;

                if (m_ReadyFlag && m_AllowHighFrequencyUpdate)
                    m_LastValue = m_PreviousValue + (Math.Max(m_High, m_PreviousClose) - Math.Min(m_Low, m_PreviousClose) - m_PreviousValue) / m_Length;
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
            if (m_IndicatorProgress == m_Length + 1)
            {
                m_ReadyFlag = true;
                double meanValueInitial = 0.0;
                for (int iATR = 1; iATR <= m_Length; ++iATR)
                    meanValueInitial += (Math.Max(m_CircularBuffer_High[iATR], m_CircularBuffer_Close[iATR - 1]) - Math.Min(m_CircularBuffer_Low[iATR], m_CircularBuffer_Close[iATR - 1]));
                meanValueInitial /= m_Length;
                m_PreviousValue = m_LastValue = meanValueInitial;
            }
        }
        #endregion

    }
}
