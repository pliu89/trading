using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;

namespace EconomicBloombergProject
{
    public class EconomicDataSeries
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        // EconomicDataSeries contains ticker name, date for latest economic point and latest economic data point.
        private string m_TickerName = string.Empty;                                                                     // Ticker name.
        private string m_LatestDate = string.Empty;                                                                     // Latest economic data point date.
        private EconomicDataPoint m_LatestEconomicDataPoint = null;                                                     // Latest economic data point.

        // EconomicDataSeries contains economic data points for particular ticker with different date.
        private ConcurrentDictionary<string, EconomicDataPoint> m_EconomicDataPointByDate = null;                       // The concurrent dictionary for economic data series by date.

        // Writer.
        private Writer m_Writer = null;                                                                   // Set writer manager.
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        //       
        /// <summary>
        /// Instantiate economic data series.
        /// </summary>
        public EconomicDataSeries()
        {
            m_EconomicDataPointByDate = new ConcurrentDictionary<string, EconomicDataPoint>();                          // Instantiate economic data series.
        }
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        public EconomicDataPoint LatestEconomicPoint
        {
            get { return m_LatestEconomicDataPoint; }
        }
        public ConcurrentDictionary<string, EconomicDataPoint> EconomicDataPointByDate
        {
            get { return m_EconomicDataPointByDate; }
        }
        #endregion//Properties


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //
        //
        //
        //
        /// <summary>
        /// Add the latest economic data to data series.
        /// </summary>
        /// <param name="economicDataPoint"></param>
        public void AddLatestEconomicPoint(EconomicDataPoint economicDataPoint, bool newTickerCompleteFlag)
        {
            // Find this data point in the dictionary if it already exists.
            string date = economicDataPoint.Date;
            EconomicDataPoint dataPoint;
            m_EconomicDataPointByDate.TryGetValue(date, out dataPoint);

            // Add latest economic data point to economic data series for this ticker.
            if (dataPoint != null)
            {
                // It shows that the future data listener has requested the same date. Replace the stored data point with the latest economic data point.
                m_EconomicDataPointByDate[date] = economicDataPoint;
                if (Functions.StringToDateTime(date) != DateTime.MaxValue && Functions.StringToDateTime(date) != DateTime.MinValue && date != Logging.m_EmptySign)
                    m_Writer.TryWritePoint(m_EconomicDataPointByDate[date]);
            }
            else
            {
                // There must be invalid field for the ticker or the date time does't match.
                string text = string.Format("There is no date match for the latest data and future data for ticker:{0}.", economicDataPoint.Ticker);
                Console.WriteLine(text);
                Logging.WriteLog(text);
            }

            // Set latest economic point and its date for this ticker.
            m_LatestEconomicDataPoint = economicDataPoint;
            DateTime dateTime = Functions.StringToDateTime(m_LatestEconomicDataPoint.Date);
            if (dateTime == DateTime.MinValue || dateTime == DateTime.MaxValue)
                m_LatestDate = DateTime.Now.ToString("yyyyMMdd");
            else
                m_LatestDate = dateTime.ToString("yyyyMMdd");

            if (!newTickerCompleteFlag)
            {
                // Update all other missing fields for all economic data points
                foreach (string aDate in m_EconomicDataPointByDate.Keys)
                {
                    // Update those economic data points and also write to the data base.
                    EconomicDataPoint point = m_EconomicDataPointByDate[aDate];
                    if (Functions.StringToDateTime(aDate) != Functions.StringToDateTime(m_LatestDate))
                    {
                        point.UpdateMissingFutureDataPointFields(m_LatestEconomicDataPoint);
                        if (Functions.StringToDateTime(point.Date) != DateTime.MinValue && Functions.StringToDateTime(point.Date) != DateTime.MaxValue)
                            m_Writer.TryWritePoint(point);
                    }
                }
            }
            else
            {
                // Update all other missing fields for all future economic data points
                foreach (string aDate in m_EconomicDataPointByDate.Keys)
                {
                    // Update those economic data points and also write to the data base.
                    EconomicDataPoint point = m_EconomicDataPointByDate[aDate];
                    if (Functions.StringToDateTime(aDate) > Functions.StringToDateTime(m_LatestDate))
                    {
                        point.UpdateMissingFutureDataPointFields(m_LatestEconomicDataPoint);
                        if (Functions.StringToDateTime(point.Date) != DateTime.MaxValue)
                            m_Writer.TryWritePoint(point);
                    }
                }
            }
        }

        /// <summary>
        /// Add the historical economic data to data series.
        /// </summary>
        /// <param name="economicDataPoint"></param>
        public void AddHistoricalEconomicPoint(EconomicDataPoint economicDataPoint, bool newTickerCompleteFlag)
        {
            string date = economicDataPoint.Date;

            // Find this data point in the dictionary if it already exists.
            EconomicDataPoint dataPoint;
            m_EconomicDataPointByDate.TryGetValue(date, out dataPoint);

            // Add historical economic data point to economic data series for this ticker.
            if (!newTickerCompleteFlag)
            {
                if (Functions.StringToDateTime(m_LatestDate) != Functions.StringToDateTime(date))
                {
                    if (dataPoint != null)
                    {
                        // Match everything of historical data.
                        if (dataPoint.Time != Logging.m_EmptySign)
                            economicDataPoint.Time = dataPoint.Time;
                        economicDataPoint.UpdateUnixTimeStamp();
                        m_EconomicDataPointByDate[date] = economicDataPoint;
                        m_Writer.UpdatePoint(m_EconomicDataPointByDate[date]);
                    }
                    else
                    {
                        // Add to the data manager.
                        m_EconomicDataPointByDate.TryAdd(date, economicDataPoint);
                        EconomicDataPoint point = m_EconomicDataPointByDate[date];
                        m_Writer.TryWritePoint(point);
                    }
                }
            }
            else
            {
                if (Functions.StringToDateTime(m_LatestDate) != Functions.StringToDateTime(date) && Functions.StringToDateTime(date) <= DateTime.Now)
                {
                    if (dataPoint != null)
                    {
                        // Match everything of historical data.
                        if (dataPoint.Time != Logging.m_EmptySign)
                            economicDataPoint.Time = dataPoint.Time;
                        economicDataPoint.UpdateUnixTimeStamp();
                        m_EconomicDataPointByDate[date] = economicDataPoint;
                        m_Writer.UpdatePoint(m_EconomicDataPointByDate[date]);
                    }
                    else
                    {
                        // Add to the data manager.
                        m_EconomicDataPointByDate.TryAdd(date, economicDataPoint);
                        EconomicDataPoint point = m_EconomicDataPointByDate[date];
                        m_Writer.TryWritePoint(point);
                    }
                }
            }
        }

        /// <summary>
        /// Add the future economic data to data series.
        /// </summary>
        /// <param name="economicDataPoint"></param>
        public void AddFutureEconomicPoint(EconomicDataPoint economicDataPoint)
        {
            // Add economic data point to economic data series for this ticker as future economic data point.
            string date = economicDataPoint.Date;
            if (Functions.StringToDateTime(date) != DateTime.MaxValue && Functions.StringToDateTime(date) != DateTime.MinValue && date != Logging.m_EmptySign)
                m_EconomicDataPointByDate.TryAdd(date, economicDataPoint);
        }

        /// <summary>
        /// Set writter manager in economic data series class to run data buffer.
        /// </summary>
        /// <param name="writerManager"></param>
        public void SetWriter(Writer writerManager)
        {
            m_Writer = writerManager;
        }
        #endregion//Public Methods


        #region no Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        #endregion//Private Methods


        #region no Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        #endregion//Event Handlers
    }
}
