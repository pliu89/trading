using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using MySql.Data.MySqlClient;
using System.Threading;

namespace EconomicBloombergProject
{
    public class WriterManager
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        // Database information.
        private DataBaseReader m_DataBaseReader = null;
        private string m_EconomicDataTable = "economicData";
        private MySqlConnection m_MySqlEconomicDataTableConnection = null;                                                              // Connection to the database.
        private long m_Row;

        // Ticker information.
        private List<string> m_Tickers = null;
        private EconomicDataManager m_EconomicDataManager = null;

        // Output strings for recent, historical, future and historical recent future merged data to GUI.
        public string m_SchemaString = string.Empty;
        public string m_LatestString = string.Empty;
        public string m_RecentString = string.Empty;
        public string m_HistoricalString = string.Empty;
        public string m_FutureString = string.Empty;
        public string m_AllDataMergedString = string.Empty;

        // Working status for writter.
        private bool m_ContinueWorking = true;
        private EventWaitHandle m_WaitHandle = null;

        // Buffer for the data that should be written to the database.
        private Queue<EconomicDataPoint> m_EconomicDataPointQueue = null;

        // Lock Object.
        private object m_DataBaseWriteLock = new object();
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        //    
        /// <summary>
        /// Set values to members of writer manager by ticker list, economic data manager, listener manager and connect to the database.
        /// </summary>
        /// <param name="tickers"></param>
        /// <param name="economicDataManager"></param>
        /// <param name="listenerManager"></param>
        public WriterManager(List<string> tickers, EconomicDataManager economicDataManager, ListenerManager listenerManager, DataBaseReader dataBaseReader)
        {
            // Set values to members.
            m_Tickers = tickers;
            m_EconomicDataManager = economicDataManager;
            listenerManager.ListenComplete += new EventHandler(WriterManager_ListenComplete);
            m_DataBaseReader = dataBaseReader;

            // Instantiate other members.
            m_EconomicDataPointQueue = new Queue<EconomicDataPoint>();
            m_WaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

            // Get the current row count in the database.
            m_Row = m_DataBaseReader.DataBaseRowCount;

            // Connect to the data base.
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("Server={0};User Id={1};Password={2};Database={3};Connection Timeout ={4};"
                , m_DataBaseReader.m_Host, m_DataBaseReader.m_Login, m_DataBaseReader.m_Password, m_DataBaseReader.m_DataBase, m_DataBaseReader.m_TimeOutMinutes * 60);
            m_MySqlEconomicDataTableConnection = new MySqlConnection(stringBuilder.ToString());
        }
        #endregion//Constructors


        #region no Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
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
        /// Use another independant thread to write to database.
        /// </summary>
        public void CreateWritingThread()
        {
            Thread writerManagerThread = new Thread(new ThreadStart(this.RunWritting));
            writerManagerThread.Start();
        }

        /// <summary>
        /// Add economic data to the queue.
        /// </summary>
        /// <param name="economicDataPoint"></param>
        public void AddEconomicPointToQueue(EconomicDataPoint economicDataPoint)
        {
            lock (m_EconomicDataPointQueue)
            {
                m_EconomicDataPointQueue.Enqueue(economicDataPoint);
            }
            m_WaitHandle.Set();
        }

        /// <summary>
        /// Write bad ticker state flag to be 0.
        /// </summary>
        public void WriteBadTickerState()
        {
            // Append updating query.
            StringBuilder stringBuilder = new StringBuilder();
            foreach (string ticker in m_EconomicDataManager.m_BadTickerList)
            {
                string query = string.Format("update economicTickers set isGood = 0 where ticker = '{0}';", ticker);
                stringBuilder.Append(query);
            }

            string command = stringBuilder.ToString();
            if (command != string.Empty)
            {
                MySqlCommand mySqlCommand = new MySqlCommand(command, m_MySqlEconomicDataTableConnection);
                Logging.WriteQueryLog(command);
                mySqlCommand.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Stop session working.
        /// </summary>
        public void Stop()
        {
            m_ContinueWorking = false;
        }
        #endregion//Public Methods


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// Run thread to keep writing.
        /// </summary>
        private void RunWritting()
        {
            m_MySqlEconomicDataTableConnection.Open();
            while (m_ContinueWorking)
            {
                m_WaitHandle.WaitOne();
                lock (m_EconomicDataPointQueue)
                {
                    while (m_EconomicDataPointQueue.Count > 0)
                    {
                        EconomicDataPoint economicDataPoint = m_EconomicDataPointQueue.Dequeue();
                        TryWritePoint(economicDataPoint);
                    }
                }
            }

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("WriterManager_RunWriting:{0} Writing Database complete with {1} tickers with {2} rows given by bloomberg!!!", 
                DateTime.Now, m_EconomicDataManager.EconomicDataSeriesByTicker.Keys.Count, m_EconomicDataManager.m_RowCount);
            stringBuilder.AppendLine(Logging.LogToString());
            Logging.SendingEmail("Write data base and display data complete", stringBuilder.ToString(), true);

            m_MySqlEconomicDataTableConnection.Close();
        }

        /// <summary>
        /// Write information for this data point to the database.
        /// </summary>
        /// <param name="economicDataPoint"></param>
        public void TryWritePoint(EconomicDataPoint economicDataPoint)
        {
            lock (m_DataBaseWriteLock)
            {
                ConcurrentDictionary<string, ConcurrentDictionary<string, List<string>>> economicDataCollections = m_DataBaseReader.DataBaseEconomicDataByTicker;

                string ticker = economicDataPoint.Ticker;
                string date = economicDataPoint.Date;

                // Check the existence of the data in the database.
                bool NeedUpdate = CheckExistenceOfRow(economicDataCollections, ticker, date);

                if (NeedUpdate)
                {
                    // Only update all the data in that row.
                    string updateQuery = GetUpdateQueryCode(ticker, date);
                    MySqlCommand mySqlCommand = new MySqlCommand(updateQuery, m_MySqlEconomicDataTableConnection);
                    Logging.WriteQueryLog(updateQuery);
                    mySqlCommand.ExecuteNonQuery();
                }
                else
                {
                    // Only Insert the data row.
                    string insertQuery = GetInsertQueryCode(ticker, date);
                    MySqlCommand mySqlCommand = new MySqlCommand(insertQuery, m_MySqlEconomicDataTableConnection);
                    Logging.WriteQueryLog(insertQuery);
                    mySqlCommand.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Update the future economic point that has date historically.
        /// </summary>
        /// <param name="economicDataPoint"></param>
        public void UpdatePoint(EconomicDataPoint economicDataPoint)
        {
            lock (m_DataBaseWriteLock)
            {
                ConcurrentDictionary<string, ConcurrentDictionary<string, List<string>>> economicDataCollections = m_DataBaseReader.DataBaseEconomicDataByTicker;

                string ticker = economicDataPoint.Ticker;
                string date = economicDataPoint.Date;

                // Only update all the data in that row.
                string updateQuery = GetUpdateQueryCode(ticker, date);
                MySqlCommand mySqlCommand = new MySqlCommand(updateQuery, m_MySqlEconomicDataTableConnection);
                Logging.WriteQueryLog(updateQuery);
                mySqlCommand.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Generate insert query code for particular ticker and date.
        /// </summary>
        /// <param name="ticker"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        private string GetInsertQueryCode(string ticker, string date)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("insert into {0} (ts,tickerId,dateStr,timeStr,unixT,tzStr,tzBbg,eventName,shortName,securityName,surveyLastPrice,surveyHigh", m_EconomicDataTable);
            stringBuilder.Append(",surveyLow,surveyMedian,surveyAverage,surveyObservations,surveyStandardDeviation,indexUpdateFrequency,relevanceValue,observationPeriod) values (");
            EconomicDataPoint economicDataPoint = m_EconomicDataManager.EconomicDataSeriesByTicker[ticker].EconomicDataPointByDate[date];
            stringBuilder.AppendFormat("{0});", WriteDataToInsertQueryString(economicDataPoint));
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Generate update query code for particular ticker and date.
        /// </summary>
        /// <param name="ticker"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        private string GetUpdateQueryCode(string ticker, string date)
        {
            StringBuilder stringBuilder = new StringBuilder();
            EconomicDataPoint economicDataPoint = m_EconomicDataManager.EconomicDataSeriesByTicker[ticker].EconomicDataPointByDate[date];
            stringBuilder.AppendFormat("update {0} set {1}", m_EconomicDataTable, WriteDataToUpdateQueryString(economicDataPoint));
            stringBuilder.AppendFormat("where tickerId = '{0}' and dateStr = '{1}';", m_DataBaseReader.ReportTickerID(ticker), date);
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Check whether the ticker and date exists in the database.
        /// </summary>
        /// <param name="economicDataCollections"></param>
        /// <param name="ticker"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        private bool CheckExistenceOfRow(ConcurrentDictionary<string, ConcurrentDictionary<string, List<string>>> economicDataCollections, string ticker, string date)
        {
            bool result = false;
            // Only check that whether the data collections contain those keys.
            if (economicDataCollections.ContainsKey(ticker))
            {
                ConcurrentDictionary<string, List<string>> dataSeries;
                economicDataCollections.TryGetValue(ticker, out dataSeries);
                if (dataSeries.ContainsKey(date))
                    result = true;
            }
            return result;
        }

        /// <summary>
        /// Write insert query value sentence.
        /// </summary>
        /// <param name="economicDataPoint"></param>
        /// <returns></returns>
        private string WriteDataToInsertQueryString(EconomicDataPoint economicDataPoint)
        {
            // Output info of the economic data point in query format.
            ++m_Row;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("'{0}',", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            stringBuilder.AppendFormat("'{0}',", m_DataBaseReader.ReportTickerID(economicDataPoint.m_Ticker));
            stringBuilder.AppendFormat("'{0}',", economicDataPoint.m_EcoDate);
            stringBuilder.AppendFormat("'{0}',", economicDataPoint.m_EcoTime);
            stringBuilder.AppendFormat("'{0}',", economicDataPoint.m_UnixTimeStamp);
            stringBuilder.AppendFormat("'{0}',", economicDataPoint.m_ExchangeLocationName);
            stringBuilder.AppendFormat("'{0}',", economicDataPoint.m_TimeZoneCode);
            stringBuilder.AppendFormat("'{0}',", DropSpecialCharForString(economicDataPoint.m_EventName));
            stringBuilder.AppendFormat("'{0}',", DropSpecialCharForString(economicDataPoint.m_ShortName));
            stringBuilder.AppendFormat("'{0}',", DropSpecialCharForString(economicDataPoint.m_SecurityName));
            stringBuilder.AppendFormat("'{0}',", economicDataPoint.m_SurveyLastPrice);
            stringBuilder.AppendFormat("'{0}',", economicDataPoint.m_SurveyHigh);
            stringBuilder.AppendFormat("'{0}',", economicDataPoint.m_SurveyLow);
            stringBuilder.AppendFormat("'{0}',", economicDataPoint.m_SurveyMedian);
            stringBuilder.AppendFormat("'{0}',", economicDataPoint.m_SurveyAverage);
            stringBuilder.AppendFormat("'{0}',", economicDataPoint.m_SurveyObservations);
            stringBuilder.AppendFormat("'{0}',", economicDataPoint.m_FutureStandardDeviation);
            stringBuilder.AppendFormat("'{0}',", economicDataPoint.m_IndexUpdateFrequency);
            stringBuilder.AppendFormat("'{0}',", economicDataPoint.m_RelevanceValue);
            stringBuilder.AppendFormat("'{0}'", economicDataPoint.m_ObservationPeriod);
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Write update query value sentence.
        /// </summary>
        /// <param name="economicDataPoint"></param>
        /// <returns></returns>
        private string WriteDataToUpdateQueryString(EconomicDataPoint economicDataPoint)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("ts='{0}', ", DateTime.Now.ToString("yyyy-MM-dd HH;mm:ss"));
            stringBuilder.AppendFormat("dateStr='{0}', ", economicDataPoint.m_EcoDate);
            stringBuilder.AppendFormat("timeStr='{0}', ", economicDataPoint.m_EcoTime);
            stringBuilder.AppendFormat("unixT='{0}', ", economicDataPoint.m_UnixTimeStamp);
            stringBuilder.AppendFormat("tzStr='{0}', ", economicDataPoint.m_ExchangeLocationName);
            stringBuilder.AppendFormat("tzBbg='{0}', ", economicDataPoint.m_TimeZoneCode);
            stringBuilder.AppendFormat("eventName='{0}', ", DropSpecialCharForString(economicDataPoint.m_EventName));
            stringBuilder.AppendFormat("shortName='{0}', ", DropSpecialCharForString(economicDataPoint.m_ShortName));
            stringBuilder.AppendFormat("securityName='{0}', ", DropSpecialCharForString(economicDataPoint.m_SecurityName));
            stringBuilder.AppendFormat("surveyLastPrice='{0}', ", economicDataPoint.m_SurveyLastPrice);
            stringBuilder.AppendFormat("surveyHigh='{0}', ", economicDataPoint.m_SurveyHigh);
            stringBuilder.AppendFormat("surveyLow='{0}', ", economicDataPoint.m_SurveyLow);
            stringBuilder.AppendFormat("surveyMedian='{0}', ", economicDataPoint.m_SurveyMedian);
            stringBuilder.AppendFormat("surveyAverage='{0}', ", economicDataPoint.m_SurveyAverage);
            stringBuilder.AppendFormat("surveyObservations='{0}', ", economicDataPoint.m_SurveyObservations);
            stringBuilder.AppendFormat("surveyStandardDeviation='{0}', ", economicDataPoint.m_FutureStandardDeviation);
            stringBuilder.AppendFormat("indexUpdateFrequency='{0}', ", economicDataPoint.m_IndexUpdateFrequency);
            stringBuilder.AppendFormat("relevanceValue='{0}', ", economicDataPoint.m_RelevanceValue);
            stringBuilder.AppendFormat("observationPeriod='{0}' ", economicDataPoint.m_ObservationPeriod);
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Exclude the special characters not permitted in the SQL database.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private string DropSpecialCharForString(string text)
        {
            // Convert space%space to space first.
            string changedText = text.Replace(" % ", " ");
            StringBuilder stringBuilder = new StringBuilder();

            // Drop %, which may be at the end of a string.
            for (int i = 0; i < changedText.Length; ++i)
            {
                if (changedText[i] != '%')
                    stringBuilder.Append(changedText[i]);
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Start generating strings after listen complete.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WriterManager_ListenComplete(object sender, EventArgs e)
        {
            // Try to write the final needed update ticker information to database.
            UpdateFutureEconomicData();
            UpdateEconomicDataTable();

            // Do all the required writing for GUI.
            m_SchemaString = WriteSchemaToString();
            m_LatestString = WriteLatestDataToString();
            m_RecentString = WriteRecentDataToString();
            m_HistoricalString = WriteHistoryDataToString();
            m_FutureString = WriteFutureDataToString();
            m_AllDataMergedString = WriteRecentHistoryFutureMergedDataToString();
            OnStringGeneratingComplete();
        }

        /// <summary>
        /// Update the future economic data field besides the release date and release time.
        /// </summary>
        private void UpdateFutureEconomicData()
        {
            // The future data is first listened to and so they need to be updated finally.
            foreach (string ticker in m_EconomicDataManager.EconomicDataSeriesByTicker.Keys)
            {
                // Find all the points in series that have date in the future and do the change accordingly.
                EconomicDataSeries dataSeries = m_EconomicDataManager.EconomicDataSeriesByTicker[ticker];
                foreach (string date in dataSeries.EconomicDataPointByDate.Keys)
                {
                    if (Functions.StringToDateTime(date) < DateTime.Now)
                    {
                        continue;
                    }
                    else
                    {
                        EconomicDataPoint dataPoint = dataSeries.EconomicDataPointByDate[date];
                        m_EconomicDataManager.UpdateFutureDataPoint(dataPoint);
                    }
                }
            }
        }

        /// <summary>
        /// Write the needed economic data update information to data base.
        /// </summary>
        private void UpdateEconomicDataTable()
        {
            foreach (string ticker in m_Tickers)
            {
                EconomicDataSeries economicDataSeries;
                m_EconomicDataManager.EconomicDataSeriesByTicker.TryGetValue(ticker, out economicDataSeries);
                if (economicDataSeries != null)
                {
                    if (economicDataSeries.NeededUpdateTickerList.Count > 0)
                    {
                        foreach (EconomicDataPoint dataPoint in economicDataSeries.NeededUpdateTickerList)
                            TryWritePoint(dataPoint);
                    }
                }
                else
                {
                    string errorInfo = string.Format("WriterManager_UpdateEconomicDataTable:Could not find the ticker:{0}.", ticker);
                    Console.WriteLine(errorInfo);
                    Logging.WriteErrorLog(errorInfo);
                }
            }
        }

        /// <summary>
        /// Write schema of economic data.
        /// </summary>
        /// <returns></returns>
        private string WriteSchemaToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("countryCode,ticker,economicDate,economicTime,unixTimeStamp,eventName,shortName,securityName,surveyLastPrice,surveyHigh,surveyLow,surveyMedian");
            stringBuilder.Append(",surveyAverage,surveyObservations,surveyStandardDeviation,indexUpdateFrequency,relevanceValue,bbgTimeZone,exchangeLocation,observationPeriod");
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Write latest economic data points for each ticker to string.
        /// </summary>
        /// <returns></returns>
        private string WriteLatestDataToString()
        {
            EconomicDataSeries dataSeries;
            StringBuilder stringBuilder = new StringBuilder();

            // Loop to find latest economic data point in the dictionary.
            foreach (string ticker in m_EconomicDataManager.EconomicDataSeriesByTicker.Keys)
            {
                m_EconomicDataManager.EconomicDataSeriesByTicker.TryGetValue(ticker, out dataSeries);
                EconomicDataPoint economicDataPoint = dataSeries.LatestEconomicPoint;
                if (economicDataPoint != null)
                    stringBuilder.AppendLine(economicDataPoint.WriteDetaToString());
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Write recent economic data points for each ticker to string.
        /// </summary>
        /// <returns></returns>
        private string WriteRecentDataToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            EconomicDataSeries dataSeries;
            EconomicDataPoint dataPoint;
            DateTime recentDate = Functions.StringToDateTime(m_EconomicDataManager.EndDate);

            // Loop to find the most recent historical data.
            foreach (string ticker in m_EconomicDataManager.EconomicDataSeriesByTicker.Keys)
            {
                // Find the economic data series for the ticker.
                List<DateTime> DateTimeList = new List<DateTime>();
                m_EconomicDataManager.EconomicDataSeriesByTicker.TryGetValue(ticker, out dataSeries);
                foreach (string date in dataSeries.EconomicDataPointByDate.Keys)
                {
                    DateTime tempDateTime = Functions.StringToDateTime(date);
                    // Determine whether the date time is in range.
                    if (tempDateTime <= recentDate)
                        DateTimeList.Add(tempDateTime);
                }
                DateTimeList.Sort();
                DateTimeList.Reverse();

                // Find the largest date time in the list that is smaller than recent date time.
                if (DateTimeList.Count > 0)
                {
                    string date = DateTimeList[0].ToString("yyyyMMdd");
                    dataSeries.EconomicDataPointByDate.TryGetValue(date, out dataPoint);
                    if (dataPoint != null)
                        stringBuilder.AppendLine(dataPoint.WriteDetaToString());
                }
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Write historical economic data points for each ticker to string.
        /// </summary>
        /// <returns></returns>
        private string WriteHistoryDataToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            EconomicDataSeries dataSeries;
            EconomicDataPoint dataPoint;

            // Loop to find the historical data.
            foreach (string ticker in m_EconomicDataManager.EconomicDataSeriesByTicker.Keys)
            {
                // Find the economic data series for the ticker.
                List<DateTime> DateTimeList = new List<DateTime>();
                m_EconomicDataManager.EconomicDataSeriesByTicker.TryGetValue(ticker, out dataSeries);
                foreach (string date in dataSeries.EconomicDataPointByDate.Keys)
                    DateTimeList.Add(Functions.StringToDateTime(date));
                
                DateTimeList.Sort();
                DateTimeList.Reverse();
                foreach (DateTime dateTime in DateTimeList)
                {
                    // Determine whether the date time is in range. Output the historical data as string.
                    if (dateTime < Functions.StringToDateTime(m_EconomicDataManager.EndDate))
                    {
                        string date = dateTime.ToString("yyyyMMdd");
                        dataSeries.EconomicDataPointByDate.TryGetValue(date, out dataPoint);
                        if (dataPoint != null)
                            stringBuilder.AppendLine(dataPoint.WriteDetaToString());
                    }
                }
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Write future economic data points for each ticker to string.
        /// </summary>
        /// <returns></returns>
        private string WriteFutureDataToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            EconomicDataSeries dataSeries;
            EconomicDataPoint dataPoint;

            // Loop to find the future data.
            foreach (string ticker in m_EconomicDataManager.EconomicDataSeriesByTicker.Keys)
            {
                // Find the economic data series for the ticker.
                List<DateTime> DateTimeList = new List<DateTime>();
                m_EconomicDataManager.EconomicDataSeriesByTicker.TryGetValue(ticker, out dataSeries);
                foreach (string date in dataSeries.EconomicDataPointByDate.Keys)
                    DateTimeList.Add(Functions.StringToDateTime(date));
                DateTimeList.Sort();
                DateTimeList.Reverse();
                foreach (DateTime dateTime in DateTimeList)
                {
                    // Determine whether the date time is in range. Output the future data as string.
                    if (dateTime > Functions.StringToDateTime(m_EconomicDataManager.EndDate))
                    {
                        string date = dateTime.ToString("yyyyMMdd");
                        dataSeries.EconomicDataPointByDate.TryGetValue(date, out dataPoint);
                        if (dataPoint != null)
                            stringBuilder.AppendLine(dataPoint.WriteDetaToString());
                    }
                }
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Write latest and historical economic data points for each ticker to string.
        /// </summary>
        /// <returns></returns>
        private string WriteRecentHistoryFutureMergedDataToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            EconomicDataSeries dataSeries;
            EconomicDataPoint dataPoint;

            // Loop to find all data.
            foreach (string ticker in m_EconomicDataManager.EconomicDataSeriesByTicker.Keys)
            {
                // Find the economic data series for the ticker.
                List<DateTime> DateTimeList = new List<DateTime>();
                m_EconomicDataManager.EconomicDataSeriesByTicker.TryGetValue(ticker, out dataSeries);

                foreach (string date in dataSeries.EconomicDataPointByDate.Keys)
                    DateTimeList.Add(Functions.StringToDateTime(date));
                DateTimeList.Sort();
                DateTimeList.Reverse();

                // Write all economic data out to string in order of date time.
                foreach (DateTime dateTime in DateTimeList)
                {
                    string date = dateTime.ToString("yyyyMMdd");
                    dataSeries.EconomicDataPointByDate.TryGetValue(date, out dataPoint);
                    if (dataPoint != null)
                        stringBuilder.AppendLine(dataPoint.WriteDetaToString());
                }
            }

            return stringBuilder.ToString();
        }
        #endregion//Private Methods


        #region Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// Eventhandler when all the strings required to write are generated.
        /// </summary>
        public event EventHandler StringGeneratingComplete;

        private void OnStringGeneratingComplete()
        {
            if (this.StringGeneratingComplete != null)
            {
                this.StringGeneratingComplete(this, EventArgs.Empty);
            }
        }
        #endregion//Event Handlers
    }
}
