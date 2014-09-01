using System;
using System.Collections.Generic;

namespace VioletAPI.Lib.Indicator
{
    public class IndicatorBase
    {

        #region Members
        protected int m_ID = -1;                                                                                    // Indicator ID.
        protected string m_Name = null;                                                                             // Indicator name.
        protected IndicatorType m_IndicatorType;                                                                    // Indicator type.

        protected int m_IndicatorProgress = 0;                                                                      // Indicator progress.
        protected bool m_ReadyFlag = false;                                                                         // Flag of progress. True if the first value is available.
        protected DateTime m_NextBarUpdateDateTime = DateTime.MinValue;                                             // Next bar update date time.
        protected List<DateTime> m_ExchangeOpens = null;                                                            // Exchange open times.
        protected List<DateTime> m_ExchangeCloses = null;                                                           // Exchange close times.
        protected int m_BarUpdateIntervalSecond = 60;                                                               // Bar update interval.
        protected bool m_AllowHighFrequencyUpdate = false;                                                          // Flag of high frequency update.

        protected DateTime m_LastFeedDateTime = DateTime.MinValue;                                                  // Last data time to feed the indicator.
        protected double m_LastFeed = double.NaN;                                                                   // Last data to feed the indicator.

        protected double m_PreviousValue = double.NaN;                                                              // Previous bar indicator value.
        protected double m_LastValue = double.NaN;                                                                  // Current bar indicator value.
        #endregion


        #region Properties
        public int ID
        {
            get { return m_ID; }
        }
        public bool IsReady
        {
            get { return m_ReadyFlag; }
        }
        public double Last
        {
            get { return m_LastValue; }
        }
        public bool AllowHighFrequencyUpdate
        {
            get { return m_AllowHighFrequencyUpdate; }
            set { m_AllowHighFrequencyUpdate = value; }
        }
        #endregion


        #region Constructor
        /// <summary>
        /// Constructor taking indicator ID, name and indicator type.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="indicatorType"></param>
        public IndicatorBase(int id, string name, IndicatorType indicatorType)
        {
            // Initial Setup.
            m_ID = id;
            m_Name = name;
            m_IndicatorType = indicatorType;
        }
        #endregion


        #region Public Methods
        /// <summary>
        /// Setup the indicator at the beginning.
        /// </summary>
        /// <param name="updateInterval"></param>
        /// <param name="firstIncomingDateTime"></param>
        /// <param name="exchangeOpens"></param>
        /// <param name="exchangeCloses"></param>
        public void SetupIndicator(int updateInterval, DateTime firstIncomingDateTime, List<DateTime> exchangeOpens, List<DateTime> exchangeCloses)
        {
            m_BarUpdateIntervalSecond = updateInterval;                                                         // Specify indicator update interval.
            m_NextBarUpdateDateTime = firstIncomingDateTime;                                                    // Specify next bar update datetime.
            m_ExchangeOpens = new List<DateTime>(exchangeOpens);                                                // Get exchange open times.
            m_ExchangeCloses = new List<DateTime>(exchangeCloses);                                              // Get exchange close times.

            // Get the next bar update datetime.
            int totalSeconds = (int)m_NextBarUpdateDateTime.TimeOfDay.TotalSeconds + 1;
            while (totalSeconds % m_BarUpdateIntervalSecond != 0)
                totalSeconds++;
            m_NextBarUpdateDateTime = m_NextBarUpdateDateTime.Date.AddSeconds(totalSeconds);
            m_IndicatorProgress = 0;
        }
        #endregion


        #region Virtual Functions
        /// <summary>
        /// Feed indicator virtual function.
        /// </summary>
        /// <param name="localTimeNow"></param>
        /// <param name="newPrice"></param>
        public virtual void FeedIndicator(DateTime localTimeNow, double newPrice) { }
        #endregion

    }
}
