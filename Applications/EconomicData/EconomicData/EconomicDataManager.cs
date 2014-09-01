using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;

namespace EconomicBloombergProject
{
    public class EconomicDataManager
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        // Inputs of request time.
        private string m_StartDate = null;                                                                      // Start date that we are going to listen from.
        private string m_EndDate = null;                                                                        // End date that we are going to listen to.
        private string m_FutureDate = null;                                                                     // A future date that we are going to listen to.
        private DateTime m_EarliestDate = new DateTime(2010, 6, 1);                                             // Built in earliest date that we send request.
        private int m_MonthlyRequestLimit = 50;                                                                 // Month range limit.
        public List<string> m_BadTickerList = null;                                                             // List to store the bad tickers.
        public List<string> m_ValidTickers = null;                                                              // A list of valid tickers.
        public bool m_NewTickerListenComplete = false;                                                          // A flag whether we completes the new ticker listening.

        // Listeners may use the variables below to listen to tickers.
        public long m_RowCount = 0;                                                                             // Count for the total number of data rows that we received from bloomberg.
        public int m_TickerListenLimit = 20;                                                                    // Limit for the number of tickers.                                               

        // Concurrent dictionary for data series by ticker.
        private ConcurrentDictionary<string, EconomicDataSeries> m_EconomicDataSeriesByTicker = null;           // Dictionary to store economic series by ticker and date.

        // Add data buffer to writer manager while getting data from bloomberg.
        private Writer m_Writer = null;
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        //   
        /// <summary>
        /// Set request start, end and future date for economic data manager so that it should only add economic point in the correct time ranges.
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="futureDate"></param>
        public EconomicDataManager(string startDate, string endDate, string futureDate)
        {
            // Set values to members in this class.
            m_StartDate = startDate;
            m_EndDate = endDate;
            m_FutureDate = futureDate;

            // Check the limit for the ticker number.
            if (CheckListenLimit())
            {
                string errorInfo = "Date out of range because it exceeds three months!";
                Console.WriteLine(errorInfo);
                Logging.WriteErrorLog(errorInfo);
                return;
            }

            // Instantiate the dictionary to store multiple economic data series.
            m_EconomicDataSeriesByTicker = new ConcurrentDictionary<string, EconomicDataSeries>();

            // Record a list of bad tickers.
            m_BadTickerList = new List<string>();
        }
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        public string StartDate
        {
            get { return m_StartDate; }
            set { m_StartDate = value; }
        }
        public string EndDate
        {
            get { return m_EndDate; }
        }
        public string FutureDate
        {
            get { return m_FutureDate; }
            set { m_FutureDate = value; }
        }
        public DateTime EarliestDate
        {
            get { return m_EarliestDate; }
        }
        public ConcurrentDictionary<string, EconomicDataSeries> EconomicDataSeriesByTicker
        {
            get { return m_EconomicDataSeriesByTicker; }
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
        /// Add the packaged latest economic data point from listeners to the corresponding ticker economic data series.
        /// </summary>
        /// <param name="economicDataPoint"></param>
        public void AddLatestEconomicPoint(EconomicDataPoint economicDataPoint)
        {
            // Check existence of ticker.
            EconomicDataSeries economicDataSeries;
            string ticker = economicDataPoint.Ticker;

            // If there is not ticker existing in the dictionary, add it and also note that the added economic point should be latest.
            if (!m_EconomicDataSeriesByTicker.ContainsKey(ticker))
            {
                string errorInfo = string.Format("There is no economic data series for the ticker:{0}.", ticker);
                Console.WriteLine(errorInfo);
                Logging.WriteErrorLog(errorInfo);
            }

            // Add economic data point to corresponding economic data series.
            m_EconomicDataSeriesByTicker.TryGetValue(ticker, out economicDataSeries);
            if (economicDataSeries != null)
                economicDataSeries.AddLatestEconomicPoint(economicDataPoint, m_NewTickerListenComplete);
            else
            {
                string errorInfo = string.Format("The ticker:{0} doesn't have economic series.", ticker);
                Console.WriteLine(errorInfo);
                Logging.WriteErrorLog(errorInfo);
            }
        }

        /// <summary>
        /// Add the packaged historical economic data point from listeners to the corresponding ticker economic data series.
        /// </summary>
        /// <param name="economicDataPoint"></param>
        public void AddHistoricalEconomicPoint(EconomicDataPoint economicDataPoint)
        {
            // Check existence of ticker.
            EconomicDataSeries economicDataSeries;

            // Check whether the latest economic point exists in the series.
            if (!m_EconomicDataSeriesByTicker.ContainsKey(economicDataPoint.Ticker))
            {
                string errorInfo = string.Format("The latest economic data point does not exist in the economic data manager for ticker:{0} with date {1}."
                    , economicDataPoint.Ticker, economicDataPoint.Date);
                Console.WriteLine(errorInfo);
                Logging.WriteErrorLog(errorInfo);
            }

            // Add economic data point to corresponding economic data series.
            m_EconomicDataSeriesByTicker.TryGetValue(economicDataPoint.Ticker, out economicDataSeries);
            if (DateInRange(economicDataPoint.Date))
                economicDataSeries.AddHistoricalEconomicPoint(economicDataPoint, m_NewTickerListenComplete);
        }

        /// <summary>
        /// Add the packaged future economic data point from listeners to the corresponding ticker economic data series.
        /// </summary>
        /// <param name="economicDataPoint"></param>
        public void AddFutureEconomicPoint(EconomicDataPoint economicDataPoint)
        {
            // Future listen is the first to be launched, so instantiate the data series in the dictionary.
            EconomicDataSeries economicDataSeries = new EconomicDataSeries();
            string ticker = economicDataPoint.Ticker;

            // Add the economic data series object to the corresponding ticker series dictionary.
            if (!m_EconomicDataSeriesByTicker.ContainsKey(ticker))
            {
                m_EconomicDataSeriesByTicker.TryAdd(ticker, economicDataSeries);

                // Set the writer manager for that ticker. It is ensured to only set once.
                economicDataSeries.SetWriter(m_Writer);
            }

            // Add economic data point to corresponding economic data series.
            m_EconomicDataSeriesByTicker.TryGetValue(ticker, out economicDataSeries);
            if (DateInRange(economicDataPoint.Date))
                economicDataSeries.AddFutureEconomicPoint(economicDataPoint);
        }

        /// <summary>
        /// Write the bad state to the data base after receiving complete from future economic data listener.
        /// </summary>
        public void WriteBadState(List<string> tickers)
        {
            // Update the valid ticker list.
            m_ValidTickers = new List<string>();
            foreach (string ticker in tickers)
            {
                if (!m_BadTickerList.Contains(ticker))
                    m_ValidTickers.Add(ticker);
            }

            // Write to the data base bad ticker and also flag new good ticker 1.
            if (!m_NewTickerListenComplete)
            {
                m_Writer.WriteBadTickerState();
                m_Writer.WriteValidTickerState();
            }
        }

        /// <summary>
        /// Set writter manager in economic data manager class to run data buffer. The economic data manager is instantiated before writer manager.
        /// </summary>
        /// <param name="writerManager"></param>
        public void SetWriter(Writer writerManager)
        {
            m_Writer = writerManager;
        }
        #endregion//Public Methods


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// Determine whether the date is our target range.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        private bool DateInRange(string date)
        {
            // Compare by first tranform to system datetime.
            DateTime dateTime = Functions.StringToDateTime(date);
            DateTime startDate = Functions.StringToDateTime(m_StartDate);
            DateTime futureDate = Functions.StringToDateTime(m_FutureDate);

            // If the date is in the range of start date and future date, return true.
            bool isDateTrue = false;
            if (startDate <= dateTime && dateTime <= futureDate)
                isDateTrue = true;
            return isDateTrue;
        }

        /// <summary>
        /// This function check the ticker and date limit for the listening.
        /// </summary>
        private bool CheckListenLimit()
        {
            bool isBrokenLimit = true;

            // Check whether the date range is too large.
            TimeSpan dateRange = Functions.StringToDateTime(m_EndDate) - Functions.StringToDateTime(m_StartDate);
            if (dateRange.TotalDays / 30 > m_MonthlyRequestLimit)
            {
                string errorInfo = "EconomicBloombergProject_Run:The date range is more than 4 months, error!!!";
                Logging.WriteErrorLog(errorInfo);
                Console.WriteLine(errorInfo);
                return isBrokenLimit;
            }

            isBrokenLimit = false;
            return isBrokenLimit;
        }
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
