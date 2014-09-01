using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using MySql.Data.MySqlClient;


namespace EconomicBloombergProject
{
    public class DataBaseReader
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //     
        // Database related members.
        private MySqlConnection m_MySqlEconomicDataTableConnection = null;                                                              // Connection to the database.
        public string m_Host = "10.10.100.28";                                                                                          // Host Ip address.
        public string m_Login = "brerw";                                                                                                // Login name.
        public string m_Password = "bbgdata";                                                                                           // Password.
        public string m_DataBase = "bbg";                                                                                               // Database name.
        public int m_TimeOutMinutes = 1;                                                                                                // Data base connection time out.

        // Memebers to store everything read from the database.
        private ConcurrentDictionary<int, List<string>> m_EconomicTickerInfoByID = null;                                                // Ticker information table data
        private ConcurrentDictionary<long, List<string>> m_EconomicDataByID = null;                                                     // Ticker economic data table data
        private ConcurrentDictionary<string, ConcurrentDictionary<string, List<string>>> m_DataBaseEconomicDataByTicker = null;         // Economic data dictionary sorted by ticker and date.

        // Table name.
        private string m_EconomicTickerTable = "economicTickers";                                                                       // Table name for ticker info.
        private string m_EconomicDataTable = "economicData";                                                                            // Table name for economic data.
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        //       
        /// <summary>
        /// connect to database using host, login name, password and database name.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="loginName"></param>
        /// <param name="passWord"></param>
        /// <param name="dataBase"></param>
        public DataBaseReader()
        {
            m_EconomicTickerInfoByID = new ConcurrentDictionary<int, List<string>>();                                                   // Economic ticker information table dictionary.
            m_EconomicDataByID = new ConcurrentDictionary<long, List<string>>();                                                        // Economic data dictionary by its ID.
            m_DataBaseEconomicDataByTicker = new ConcurrentDictionary<string, ConcurrentDictionary<string, List<string>>>();            // Database economic data by ticker and ID.
            ConnectToDataBase();                                                                                                        // Connect to the database.
        }

        /// <summary>
        /// Find ticker ID in the ticker info table for corresponding ticker.
        /// </summary>
        /// <param name="ticker"></param>
        /// <returns></returns>
        public int ReportTickerID(string ticker)
        {
            int tickerID = -1;                                                                                                          // The default ticker id is -1.
            foreach (int key in m_EconomicTickerInfoByID.Keys)
            {
                List<string> stringList = null;
                m_EconomicTickerInfoByID.TryGetValue(key, out stringList);

                // Confirm the output for stringList is not null.
                if (stringList != null)
                {
                    // If finding the ticker, break. Otherwise tickerID will remain to be -1.
                    if (stringList[TickerInfoField.TickerName] == ticker)
                    {
                        tickerID = key;
                        break;
                    }
                }
                else
                {
                    // Write to log if stringList is null.
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.AppendFormat("DataBaseReader_ReportTickerID:Could not find row data for the ticker:{0}", ticker);
                    Logging.WriteErrorLog(stringBuilder.ToString());
                }
            }

            // Write to log if there is no corresponding tickerId associate with this ticker.
            if (tickerID == -1)
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendFormat("DataBaseReader_ReportTickerID:Could not find ticker ID for this ticker:{0}", ticker);
                Logging.WriteErrorLog(stringBuilder.ToString());
            }

            return tickerID;
        }
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        public ConcurrentDictionary<int, List<string>> EconomicTickerInfo
        {
            get { return m_EconomicTickerInfoByID; }
        }
        public ConcurrentDictionary<long, List<string>> EconomicDataCollections
        {
            get { return m_EconomicDataByID; }
        }
        public ConcurrentDictionary<string, ConcurrentDictionary<string, List<string>>> DataBaseEconomicDataByTicker
        {
            get { return m_DataBaseEconomicDataByTicker; }
        }
        public long DataBaseRowCount
        {
            get
            {
                List<string> dataList;
                long maxRow = 0;
                foreach (long pointID in m_EconomicDataByID.Keys)
                {
                    dataList = m_EconomicDataByID[pointID];
                    long tempRow;

                    if (!long.TryParse(dataList[EconomicDataField.EconomicDataPointID], out tempRow))
                    {
                        string errorInfo = string.Format("There is error in reading economic data table and the current row is {0}.", tempRow);
                        Console.WriteLine(errorInfo);
                        Logging.WriteErrorLog(errorInfo);
                        return 0;
                    }
                    else
                    {
                        if (tempRow > maxRow)
                            maxRow = tempRow;
                    }
                }
                return maxRow;
            }
        }
        public MySqlConnection SQLConnection
        {
            get { return m_MySqlEconomicDataTableConnection; }
        }
        #endregion//Properties


        #region no Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //
        //
        //
        //
        #endregion//Public Methods


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        //
        /// <summary>
        /// Connect to database and read all the data necessary.
        /// </summary>
        private void ConnectToDataBase()
        {
            // Connect to the data base.
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("Server={0};User Id={1};Password={2};Database={3};Connection Timeout ={4};", m_Host, m_Login, m_Password, m_DataBase, m_TimeOutMinutes * 60);
            m_MySqlEconomicDataTableConnection = new MySqlConnection(stringBuilder.ToString());
            Logging.WriteLog(string.Format("The data base timeout is set to new value of {0} seconds.", m_MySqlEconomicDataTableConnection.ConnectionTimeout));

            // Read data.
            ReadEconomicTickerFromDataBase();
            ReadEconomicDataFromDataBase();

            // Get all data into dictionary by ticker by date.
            GenerateDictionaryForDataFromDataBase();
        }

        /// <summary>
        /// Read ticker information from first table to ticker info dictionary.
        /// </summary>
        private void ReadEconomicTickerFromDataBase()
        {
            m_MySqlEconomicDataTableConnection.Open();

            // Select all from economic ticker info table.
            string command = string.Format("select * from {0};", m_EconomicTickerTable);
            MySqlCommand mySqlCommand = new MySqlCommand(command, m_MySqlEconomicDataTableConnection);
            MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();

            // Read until end and store into m_EconomicTickerInfoByID dictionary.
            while (mySqlDataReader.Read())
            {
                int fieldSize = mySqlDataReader.FieldCount;

                // There should be 5 columns in the ticker information table.
                if (fieldSize == 7)
                {
                    int tickerID = mySqlDataReader.GetInt32(TickerInfoField.TickerID);

                    // If economic ticker information dictionary does not contain that ticker ID, add this ticker.
                    if (!m_EconomicTickerInfoByID.ContainsKey(tickerID))
                        m_EconomicTickerInfoByID.TryAdd(tickerID, new List<string>());

                    List<string> tickerInfo = null;
                    m_EconomicTickerInfoByID.TryGetValue(tickerID, out tickerInfo);

                    // Find the corresponding value for the tickerId key, and change it.
                    tickerInfo.Add(Convert.ToString(tickerID));
                    tickerInfo.Add(mySqlDataReader.GetString(TickerInfoField.TickerName));
                    tickerInfo.Add(mySqlDataReader.GetString(TickerInfoField.DataSource));
                    tickerInfo.Add(mySqlDataReader.GetString(TickerInfoField.CountryCode));
                    tickerInfo.Add(mySqlDataReader.GetString(TickerInfoField.Country));
                    tickerInfo.Add(mySqlDataReader.GetString(TickerInfoField.Sector));
                    tickerInfo.Add(mySqlDataReader.GetString(TickerInfoField.IsGood));
                }
            }

            m_MySqlEconomicDataTableConnection.Close();
            mySqlDataReader.Close();
        }

        /// <summary>
        /// Read existing data row from economic data table to economic data dictionary.
        /// </summary>
        private void ReadEconomicDataFromDataBase()
        {
            m_MySqlEconomicDataTableConnection.Open();

            // Select all from economic data table.
            string command = string.Format("select * from {0};", m_EconomicDataTable);
            MySqlCommand mySqlCommand = new MySqlCommand(command, m_MySqlEconomicDataTableConnection);
            MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();

            // Read until end and store into m_EconomicDataByID dictionary.
            while (mySqlDataReader.Read())
            {
                int fieldSize = mySqlDataReader.FieldCount;

                // There should be 21 columns in the economic data table.
                if (fieldSize == 21)
                {
                    int pointID = mySqlDataReader.GetInt32(EconomicDataField.EconomicDataPointID);

                    // If economic data dictionary does not contain that point ID, add the data row.
                    if (!m_EconomicDataByID.ContainsKey(pointID))
                        m_EconomicDataByID.TryAdd(pointID, new List<string>());

                    List<string> tickerInfo;
                    m_EconomicDataByID.TryGetValue(pointID, out tickerInfo);

                    // Find the corresponding value for the pointId key, and change it.
                    tickerInfo.Add(Convert.ToString(pointID));
                    tickerInfo.Add(mySqlDataReader.GetString(EconomicDataField.TimeStamp));
                    tickerInfo.Add(mySqlDataReader.GetString(EconomicDataField.EconomicTickerID));
                    tickerInfo.Add(mySqlDataReader.GetString(EconomicDataField.Date));
                    tickerInfo.Add(mySqlDataReader.GetString(EconomicDataField.Time));
                    tickerInfo.Add(mySqlDataReader.GetString(EconomicDataField.UnixTime));
                    tickerInfo.Add(mySqlDataReader.GetString(EconomicDataField.ExchangeLocation));
                    tickerInfo.Add(mySqlDataReader.GetString(EconomicDataField.BBGTimeZone));
                    tickerInfo.Add(mySqlDataReader.GetString(EconomicDataField.EventName));
                    tickerInfo.Add(mySqlDataReader.GetString(EconomicDataField.ShortName));
                    tickerInfo.Add(mySqlDataReader.GetString(EconomicDataField.SecurityName));
                    tickerInfo.Add(mySqlDataReader.GetString(EconomicDataField.SurveyLastPrice));
                    tickerInfo.Add(mySqlDataReader.GetString(EconomicDataField.SurveyHigh));
                    tickerInfo.Add(mySqlDataReader.GetString(EconomicDataField.SurveyLow));
                    tickerInfo.Add(mySqlDataReader.GetString(EconomicDataField.SurveyMedian));
                    tickerInfo.Add(mySqlDataReader.GetString(EconomicDataField.SurveyAverage));
                    tickerInfo.Add(mySqlDataReader.GetString(EconomicDataField.SurveyObservations));
                    tickerInfo.Add(mySqlDataReader.GetString(EconomicDataField.ForwardStandardDeviation));
                    tickerInfo.Add(mySqlDataReader.GetString(EconomicDataField.IndexUpdateFrequency));
                    tickerInfo.Add(mySqlDataReader.GetString(EconomicDataField.RelevanceValue));
                    tickerInfo.Add(mySqlDataReader.GetString(EconomicDataField.ObservationPeriod));
                }
            }

            m_MySqlEconomicDataTableConnection.Close();
            mySqlDataReader.Close();
        }

        /// <summary>
        /// Create dicitonary for database economic data by ticker and date.
        /// </summary>
        private void GenerateDictionaryForDataFromDataBase()
        {
            foreach (long id in m_EconomicDataByID.Keys)
            {
                // Find the data row for that point ID.
                List<string> dataRow;
                m_EconomicDataByID.TryGetValue(id, out dataRow);

                // Find the corresponding ticker name for that point ID, the economic data table only contains ticker ID.
                int tickerID = Convert.ToInt32(dataRow[EconomicDataField.EconomicTickerID]);
                string ticker;
                List<string> stringList = null;
                m_EconomicTickerInfoByID.TryGetValue(tickerID, out stringList);
                if (stringList != null)
                {
                    ticker = stringList[TickerInfoField.TickerName];

                    // Add the ticker as a key to the economic data dictionary.
                    if (!m_DataBaseEconomicDataByTicker.ContainsKey(ticker))
                        m_DataBaseEconomicDataByTicker.TryAdd(ticker, new ConcurrentDictionary<string, List<string>>());

                    // Get the corresponding date from the economic data table.
                    string date = dataRow[EconomicDataField.Date];
                    ConcurrentDictionary<string, List<string>> economicSeries;

                    // Get the value for the economic data dictionary for that ticker and add data point.
                    m_DataBaseEconomicDataByTicker.TryGetValue(ticker, out economicSeries);

                    // Add this data row as value to the date dictionary.
                    if (!economicSeries.ContainsKey(date))
                        economicSeries.TryAdd(date, dataRow);
                }
                else
                {
                    // Write to error log that can not find this ticker.
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.AppendFormat("DataBaseReader_GenerateDictionaryForDataFromDataBase:Could not find ticker for the ticker ID:{0}", tickerID);
                    Logging.WriteErrorLog(stringBuilder.ToString());
                }
            }
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
