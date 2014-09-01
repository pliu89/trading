using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Timer = System.Timers.Timer;
using Bloomberglp.Blpapi;
using EventHandler = System.EventHandler;

namespace EconomicBloombergProject
{
    public class ListenerManager
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        // The members constructing listener manager are bloomberg session, a list of tickers that are going to be listened, and economic data manager for adding economic data points.
        private Session m_Session = null;
        private List<string> m_TickersEachChunk = null;
        private List<string> m_ValidTickers = null;
        private EconomicDataManager m_EconomicDataManager = null;
        private TickerList m_TickerList = null;
        private Thread m_ListeningThread = null;
        private int m_NewTickerChunkIndex;
        private int m_TickerChunkIndex;

        // It also managed latest, historical and future data listeners.
        private EventWaitHandle m_EventWaitHandle = null;
        private LatestDataListener m_LatestDataListener = null;
        private HistoricalDataListener m_HistoricalDataListener = null;
        private FutureDataListener m_FutureDataListener = null;

        // Timer related variables.
        private int m_WaitingSeconds = 1000;
        private Timer m_Timer = null;

        // Updating parameter set.
        private int m_OldTickerLookBack = 10;
        private int m_OldTickerLookForward = 30;
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        //      
        /// <summary>
        /// Set values to the members.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="tickers"></param>
        /// <param name="economicDataManager"></param>
        public ListenerManager(Session session, TickerList tickerList, EconomicDataManager economicDataManager)
        {
            // Set corresponding values to members.
            m_TickerList = tickerList;
            m_Session = session;
            m_EconomicDataManager = economicDataManager;
            m_EventWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

            // After future data listen complete, get valid tickers and do latest and historical listening only to those good tickers.
            m_ValidTickers = new List<string>();

            // Set timer.
            m_Timer = new Timer();
            m_Timer.Interval = m_WaitingSeconds * 1000;
            m_Timer.Enabled = true;
            m_FutureDataListener = new FutureDataListener(m_Session, m_EconomicDataManager, m_Timer);
            m_LatestDataListener = new LatestDataListener(m_Session, m_EconomicDataManager, m_Timer);
            m_HistoricalDataListener = new HistoricalDataListener(m_Session, m_EconomicDataManager, m_Timer);
            m_FutureDataListener.FutureListenComplete += new EventHandler(FutureDataListener_FutureListenComplete);
            m_LatestDataListener.LatestListenComplete += new EventHandler(LatestDataListener_LatestListenComplete);
            m_HistoricalDataListener.HistoricalListenComplete += new EventHandler(HistoricalDataListener_HistoricalListenComplete);
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
        /// Launch latest economic data listener.
        /// </summary>
        public void StartEconomicDataListening()
        {
            // Open the bloomberg service.
            if (!m_Session.OpenService("//blp/refdata"))
            {
                string text = "Open service failed!!!";
                Console.WriteLine(text);
                Logging.WriteErrorLog(text);
            }
            else
            {
                string text = "Open service successfully!";
                Console.WriteLine(text);
                Logging.WriteLog(text);
                Logging.m_BloombergConnectionStatus = true;
            }

            // Send email when we are going to start listening.
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("ListenerManager_StartEconomicDataListening:{0} Start Listening to real data with {1} tickers! And start date is {2}, end date is {3} and future date is {4}",
                DateTime.Now, m_TickerList.NewTickers.Count + m_TickerList.Tickers.Count, m_EconomicDataManager.StartDate, m_EconomicDataManager.EndDate, m_EconomicDataManager.FutureDate);
            Logging.SendingEmail("Start listening to the bloomberg", stringBuilder.ToString(), false);

            m_ListeningThread = new Thread(this.Start);
            m_ListeningThread.Start();
        }
        #endregion//Public Methods


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// Start a new thread different from GUI thread to do listening.
        /// </summary>
        /// <param name="obj"></param>
        private void Start()
        {
            //// Start to listen to the new tickers that do not have history.
            int newTickerChunkCount;
            m_NewTickerChunkIndex = 0;
            if (m_TickerList.NewTickers.Count > 0)
            {
                newTickerChunkCount = m_TickerList.NewTickerChunkList.Count;
                for (; m_NewTickerChunkIndex < newTickerChunkCount; ++m_NewTickerChunkIndex)
                {
                    m_TickersEachChunk = m_TickerList.NewTickerChunkList[m_NewTickerChunkIndex];
                    StartListening();
                    m_EventWaitHandle.WaitOne();
                }
            }
            else
            {
                string text = "No new tickers that are needed to be listened to.";
                newTickerChunkCount = 0;
                Console.WriteLine(text);
                Logging.WriteLog(text);
            }

            //// Continue to listen to the old good tickers and update them. Set new parameters for the new tickers.
            m_EconomicDataManager.m_NewTickerListenComplete = true;
            m_EconomicDataManager.StartDate = DateTime.Now.AddDays(-m_OldTickerLookBack).ToString("yyyyMMdd");
            m_EconomicDataManager.FutureDate = DateTime.Now.AddDays(m_OldTickerLookForward).ToString("yyyyMMdd");
            int tickerChunkCount;
            m_TickerChunkIndex = 0;
            if (m_NewTickerChunkIndex == newTickerChunkCount && m_TickerList.Tickers.Count > 0)
            {
                tickerChunkCount = m_TickerList.TickerChunkList.Count;
                for (; m_TickerChunkIndex < tickerChunkCount; ++m_TickerChunkIndex)
                {
                    m_TickersEachChunk = m_TickerList.TickerChunkList[m_TickerChunkIndex];
                    StartListening();
                    m_EventWaitHandle.WaitOne();
                }
            }
            else
            {
                string text = "No old good tickers that are needed to be listened to.";
                tickerChunkCount = 0;
                Console.WriteLine(text);
                Logging.WriteLog(text);
            }

            // Trigger the event when complete.
            if (m_NewTickerChunkIndex == newTickerChunkCount && m_TickerChunkIndex == tickerChunkCount)
                OnListenComplete();
            else
            {
                string errorInfo = "There is still some tickers not complete";
                Console.WriteLine(errorInfo);
                Logging.WriteErrorLog(errorInfo);
            }
        }

        /// <summary>
        /// Start to listen to the future release date list data.
        /// </summary>
        private void StartListening()
        {
            m_FutureDataListener.UpdateTickers(m_TickersEachChunk);
            m_FutureDataListener.RunListening();
        }

        /// <summary>
        /// Write the input ticker list to string to get info about which tickers we are going to listen.
        /// </summary>
        /// <param name="tickers"></param>
        /// <returns></returns>
        private string WriteTickerListToString(List<string> tickers)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (string ticker in tickers)
            {
                stringBuilder.AppendFormat("{0} ", ticker);
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
        // Event trigger for listen complete.
        public event EventHandler ListenComplete;

        /// <summary>
        /// Chunks by Chunks listening is complete.
        /// </summary>
        private void OnListenComplete()
        {
            if (this.ListenComplete != null)
            {
                this.ListenComplete(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// If future listener completes, change the data base ticker table and store data. Then launch latest economic data listener.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FutureDataListener_FutureListenComplete(object sender, EventArgs e)
        {
            m_LatestDataListener.UpdateTickers(m_EconomicDataManager.m_ValidTickers);
            m_LatestDataListener.RunListening();
        }

        /// <summary>
        /// If received complete information from latest economic data listener, launch historical data listener.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LatestDataListener_LatestListenComplete(object sender, EventArgs e)
        {
            // Launch historical economic data listener.
            m_HistoricalDataListener.UpdateTickers(m_EconomicDataManager.m_ValidTickers);
            m_HistoricalDataListener.RunListening();
        }

        /// <summary>
        /// All complete if the historical listener completes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HistoricalDataListener_HistoricalListenComplete(object sender, EventArgs e)
        {
            // Set success flag to be true and also trigger complete event.
            m_EventWaitHandle.Set();
        }
        #endregion//Event Handlers
    }
}
