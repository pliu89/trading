using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Bloomberglp.Blpapi;
using MySql.Data.MySqlClient;
using EventHandler = System.EventHandler;

namespace EconomicBloombergProject
{
    public class EconomicBloombergProject
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //     
        // 
        private EconomicDataManager m_EconomicDataManager = null;                                       // Economic data manager.
        private EconomicDataTable m_EconomicDataTable = null;                                           // Pointer to the GUI form.
        private Session m_Session = null;                                                               // Bloomberg related variables.
        private SessionOptions m_SessionOptions = null;                                                 // Bloomberg session option.
        private Writer m_Writer = null;                                                                 // Writer of screen and database.
        private ListenerManager m_ListenerManager = null;                                               // Listener manager.

        // Critical input variables.
        // AP COMMENT: SPECIFIC COUNTRY CODE GOES BELOW
        private string m_CountryCode = "US";
        private string m_StartDate = string.Empty;
        private string m_EndDate = string.Empty;
        private string m_FutureDate = string.Empty;
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        //    
        /// <summary>
        /// This class may operate the GUI form and get economic data table pointer.
        /// </summary>
        /// <param name="economicDataTable"></param>
        public EconomicBloombergProject(EconomicDataTable economicDataTable)
        {
            m_EconomicDataTable = economicDataTable;
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
        /// Operate in order and the last step contains creation of another thread.
        /// </summary>
        public void Run()
        {
            // Initiatelize the logging instrument.
            Logging.InitiateLogging(m_CountryCode);
            // Read configuration parameters that include start date, end date and future date from the txt file.
            ReadConfigFile();
            // Create data base reader connection and read all ticker info and economic data from the database.
            DataBaseReader m_DataBaseReader = new DataBaseReader();
            // Create the tickerlist from what we got from the data base reader and also get tickers by country.
            TickerList m_TickerList = new TickerList(m_DataBaseReader, m_CountryCode);
            // Get all tickers from one specified country and prepare chunks of tickers.
            m_TickerList.SetTickersToChunks(m_CountryCode);
            //// Connect to the Bloomberg to get economic data.
            ConnectToBloomberg();

            ////// Test only one ticker!!!
            //m_TickerList.TryOneTicker("UGRSSOTO Index", true);
            //m_StartDate = "20110101";
            //m_FutureDate = "20140201";
            //m_EconomicDataManager = new EconomicDataManager(m_StartDate, m_EndDate, m_FutureDate);
            //m_ListenerManager = new ListenerManager(m_Session, m_TickerList, m_EconomicDataManager);
            //m_Writer = new Writer(m_EconomicDataManager, m_ListenerManager, m_DataBaseReader);
            //m_EconomicDataManager.SetWriter(m_Writer);
            //m_Writer.StringGeneratingComplete += new EventHandler(WriterManager_StringGeneratingComplete);
            //m_ListenerManager.StartEconomicDataListening();

            //// Assign date range for economic data manager.
            m_EconomicDataManager = new EconomicDataManager(m_StartDate, m_EndDate, m_FutureDate);
            // Create listener manager to do chunk by chunk listening.
            m_ListenerManager = new ListenerManager(m_Session, m_TickerList, m_EconomicDataManager);
            // Create writer and currently there is other thread created.
            m_Writer = new Writer(m_EconomicDataManager, m_ListenerManager, m_DataBaseReader);
            // Economic data series need writer.
            m_EconomicDataManager.SetWriter(m_Writer);
            // Subscribe to the event when string generates complete.
            m_Writer.StringGeneratingComplete += new EventHandler(WriterManager_StringGeneratingComplete);
            // Launch listener manager.
            m_ListenerManager.StartEconomicDataListening();
        }
        #endregion//Public Methods


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        //
        /// <summary>
        /// This function connects to the bloomberg API.
        /// </summary>
        private void ConnectToBloomberg()
        {
            m_SessionOptions = new SessionOptions();
            m_SessionOptions.ServerHost = "localhost";
            m_SessionOptions.ServerPort = 8194;
            m_Session = new Session(m_SessionOptions);
            if (!m_Session.Start())
            {
                string errorInfo = "Started the Session Failed!!!";
                Console.WriteLine(errorInfo);
                Logging.WriteErrorLog(errorInfo);
                return;
            }
            string successInfo = "Started the Session Successfully!";
            Console.WriteLine(successInfo);
            Logging.WriteLog(successInfo);
        }

        /// <summary>
        /// Read configuration file.
        /// </summary>
        private void ReadConfigFile()
        {
            // Get the current working directory and append sub directory and file name.
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(Environment.CurrentDirectory);
            stringBuilder.Append(@"\Parameters.txt");
            string path = stringBuilder.ToString();

            // Read the start date, end date, future date and earliest date from this file.
            StreamReader streamReader = new StreamReader(path);
            string line;

            // The line read from the file contains name : value format.
            while ((line = streamReader.ReadLine()) != null)
            {
                string tickerName = string.Empty;
                char[] delimiter = { ':' };
                string[] words = line.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);

                // The words should only contain two elements.
                if (words.Length == 2)
                {
                    // The first word is the name of variable and the second word is its value.
                    switch (words[0])
                    {
                        case "StartDate":
                            m_StartDate = words[1];
                            break;
                        case "EndDate":
                            if (!string.IsNullOrEmpty(words[1].Trim()))
                                m_EndDate = words[1];
                            else
                                m_EndDate = DateTime.Now.ToString("yyyyMMdd");
                            break;
                        case "FutureDate":
                            if (!string.IsNullOrEmpty(words[1].Trim()))
                                m_FutureDate = words[1];
                            else
                                if (!string.IsNullOrEmpty(m_EndDate))
                                    m_FutureDate = Functions.StringToDateTime(m_EndDate).AddMonths(3).ToString("yyyyMMdd");
                                else
                                {
                                    m_FutureDate = "20140301";
                                }
                            break;
                        case "EarliestDate":
                            // Compare the start date with the earliest date. Make sure the start date is larger than earliest date.
                            if (Functions.StringToDateTime(m_StartDate) < Functions.StringToDateTime(words[1]))
                                m_StartDate = words[1];
                            break;
                    }
                }
            }

            Logging.WriteLog("Read configuration file successfully!");
        }

        /// <summary>
        /// Load new tickers from the listeners directory.
        /// </summary>
        //private void AddNewTickers()
        //{
        //    // Get the current working directory and append sub directory and file name.
        //    StringBuilder stringBuilder = new StringBuilder();
        //    stringBuilder.Append(Environment.CurrentDirectory);
        //    stringBuilder.Append(@"\newTickers.txt");

        //    // Read the file in the specified path above.
        //    StreamReader streamReader = new StreamReader(stringBuilder.ToString());
        //    string line;

        //    // Each line contains one ticker.(Example: CPI Index)
        //    while ((line = streamReader.ReadLine()) != null)
        //    {
        //        string tickerName = line;
        //        m_NewTickers.Add(tickerName);
        //    }

        //    // Check whether these new tickers are already existing in the data base.
        //    foreach (string ticker in m_NewTickers)
        //    {
        //        int tickerID = m_DataBaseReader.ReportTickerID(ticker);

        //        // Ticker Id larger than 0 means that there already exists this ticker, so remove it.
        //        if (tickerID > 0)
        //            m_NewTickers.Remove(ticker);
        //    }
        //}
        #endregion//Private Methods


        #region Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// After listen complete, start writing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WriterManager_StringGeneratingComplete(object sender, EventArgs e)
        {
            // Transfer to the GUI thread and write to the textboxes after writer manager has generated strings suceessfully.
            if (m_EconomicDataTable.InvokeRequired)
            {
                m_EconomicDataTable.Invoke(new EventHandler(WriterManager_StringGeneratingComplete), new object[] { sender, e });
            }
            else
            {
                m_EconomicDataTable.Column.Text = m_Writer.m_SchemaString;
                m_EconomicDataTable.RecentEconomicData.Text = m_Writer.m_RecentString;
                m_EconomicDataTable.LatestEconomicData.Text = m_Writer.m_LatestString;
                m_EconomicDataTable.HistoricalEconomicData.Text = m_Writer.m_HistoricalString;
                m_EconomicDataTable.FutureEconomicData.Text = m_Writer.m_FutureString;
                m_EconomicDataTable.AllDataCombined.Text = m_Writer.m_AllDataMergedString;
                m_Writer.Stop();
            }
        }
        #endregion//Event Handlers
    }
}
