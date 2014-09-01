using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Timers;
using Bloomberglp.Blpapi;
using Timer = System.Timers.Timer;
using EventHandler = System.EventHandler;


namespace EconomicBloombergProject
{
    public class FutureDataListener
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //   
        // Constructor members.
        private Session m_Session = null;
        private List<string> m_Tickers = null;
        private Timer m_Timer = null;
        private EconomicDataManager m_EconomicDataManager = null;

        // Field info request constant strings.
        private const string m_FutureReleaseDateTimeString = "ECO_FUTURE_RELEASE_DATE_LIST";

        // Flag of whether to continue listening.
        private bool m_ContinueListening = true;

        // Current thread.
        private Thread m_Thread = null;
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        //       
        /// <summary>
        /// Set values to session, tickers and economic data manager in future listener.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="tickers"></param>
        /// <param name="newTickers"></param>
        /// <param name="economicDataManager"></param>
        public FutureDataListener(Session session, EconomicDataManager economicDataManager, Timer timer)
        {
            m_Session = session;
            m_EconomicDataManager = economicDataManager;
            m_Timer = timer;
            m_Timer.Elapsed += new ElapsedEventHandler(OnFutureDataListeningTimeOut);
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
        /// Update the member of tickers.
        /// </summary>
        /// <param name="tickers"></param>
        public void UpdateTickers(List<string> tickers)
        {
            m_Tickers = tickers;
        }

        /// <summary>
        /// Send request and wait response.
        /// </summary>
        public void RunListening()
        {
            if (Logging.m_BloombergConnectionStatus)
            {
                m_Thread = Thread.CurrentThread;
                m_Timer.Start();

                // Edit request.
                Service m_RefDataService = m_Session.GetService("//blp/refdata");
                Request request = m_RefDataService.CreateRequest("ReferenceDataRequest");
                Element securities = request.GetElement("securities");

                // Ensure each time the number of tickers that we want to listen to is less than 10.
                if (m_Tickers.Count > m_EconomicDataManager.m_TickerListenLimit)
                {
                    string errorInfo = string.Format("FutureDataListener_RunListening:There are too many tickers! Ticker number is {0}", m_Tickers.Count);
                    Console.WriteLine(errorInfo);
                    Logging.WriteErrorLog(errorInfo);
                    m_Session.Stop();
                    return;
                }

                foreach (string ticker in m_Tickers)
                {
                    securities.AppendValue(ticker);
                }

                Element fields = request.GetElement("fields");
                fields.AppendValue(m_FutureReleaseDateTimeString);

                // Edit overrides to specify then time range.
                Element overrides = request["overrides"];
                Element override1 = overrides.AppendElement();
                override1.SetElement("fieldId", "START_DT");
                override1.SetElement("value", m_EconomicDataManager.StartDate);
                Element override2 = overrides.AppendElement();
                override2.SetElement("fieldId", "END_DT");
                override2.SetElement("value", m_EconomicDataManager.FutureDate);

                // Check whether the earliest date is reached.
                if (Functions.StringToDateTime(m_EconomicDataManager.StartDate) < m_EconomicDataManager.EarliestDate)
                {
                    string errorInfo = "FutureDataListener_RunListening:Earliest date broken!!! Session stop!";
                    Console.WriteLine(errorInfo);
                    Logging.WriteErrorLog(errorInfo);
                    m_Session.Stop();
                    return;
                }

                // Send Request.
                Logging.WriteLog(string.Format("FutureDataListener_RunListening:{0} tickers with date range of {1} to {2}.",
                         m_Tickers.Count, m_EconomicDataManager.StartDate, m_EconomicDataManager.FutureDate));

                // Try sending request to the bloomberg.
                try
                {
                    m_Session.SendRequest(request, null);
                }
                catch (Exception ex)
                {
                    string errorInfo = ex.ToString();
                    Console.WriteLine(errorInfo);
                    Logging.WriteErrorLog(errorInfo);
                }
                finally
                {
                    request = null;
                }

                // Wait response.
                WaitingResponse();
            }
        }
        #endregion//Public Methods


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// Wait response.
        /// </summary>
        private void WaitingResponse()
        {
            // Prepare to add bad tickers.
            m_EconomicDataManager.m_BadTickerList = new List<string>();
            m_ContinueListening = true;
            // Loop.
            while (m_ContinueListening)
            {
                // Handle event.
                Event eventObj = m_Session.NextEvent();
                switch (eventObj.Type)
                {
                    case Event.EventType.RESPONSE:
                        m_ContinueListening = false;
                        RecordFutureEconomicData(eventObj);
                        break;
                    case Event.EventType.PARTIAL_RESPONSE:
                        RecordFutureEconomicData(eventObj);
                        break;
                }
            }

            m_Timer.Stop();

            // Triger finished event.
            m_EconomicDataManager.WriteBadState(m_Tickers);
            OnFutureListenComplete();
        }

        /// <summary>
        /// Record future economic data information.
        /// </summary>
        /// <param name="eventObj"></param>
        private void RecordFutureEconomicData(Event eventObj)
        {
            StringBuilder stringBuilder = new StringBuilder();

            foreach (Message message in eventObj)
            {
                stringBuilder.AppendLine(message.ToString());
                Element response = message.AsElement;
                if (response.HasElement("responseError"))
                {
                    string errorInfo = string.Format("FutureDataListener_RecordFutureEconomicData:There is response error here!The message content is {0}.", message.ToString());
                    Console.WriteLine(errorInfo);
                    Logging.WriteErrorLog(errorInfo);
                    continue;
                }

                if (!response.HasElement("securityData"))
                {
                    string errorInfo = string.Format("FutureDataListener_RecordFutureEconomicData:There is no securityData here!The message content is {0}.", message.ToString());
                    Console.WriteLine(errorInfo);
                    Logging.WriteErrorLog(errorInfo);
                    continue;
                }

                Element securityDataArray = response.GetElement("securityData");
                int numberOfSecurityData = securityDataArray.NumValues;

                if (numberOfSecurityData == 0)
                {
                    string errorInfo = string.Format("FutureDataListener_RecordFutureEconomicData:There is no data for any securities here!The message content is {0}.", message.ToString());
                    Console.WriteLine(errorInfo);
                    Logging.WriteErrorLog(errorInfo);
                    continue;
                }

                for (int i = 0; i < numberOfSecurityData; ++i)
                {
                    Element securityData = securityDataArray.GetValueAsElement(i);
                    string ticker = string.Empty;
                    if (securityData.HasElement("security"))
                        ticker = securityData.GetElementAsString("security");
                    else
                    {
                        string errorInfo = string.Format("FutureDataListener_RecordFutureEconomicData:There is no ticker!The message content is {0}.", message.ToString());
                        Console.WriteLine(errorInfo);
                        Logging.WriteErrorLog(errorInfo);
                        continue;
                    }

                    if (securityData.HasElement("securityError"))
                    {
                        string errorInfo = string.Format("FutureDataListener_RecordFutureEconomicData:There is securityError!The error ticker is {0}.", ticker);
                        Console.WriteLine(errorInfo);
                        Logging.WriteErrorLog(errorInfo);
                        continue;
                    }

                    if (securityData.HasElement("fieldException"))
                    {
                        string errorInfo = string.Format("FutureDataListener_RecordFutureEconomicData:There is field exception!The error ticker is {0}.", ticker);
                        Console.WriteLine(errorInfo);
                        Logging.WriteErrorLog(errorInfo);
                        continue;
                    }

                    if (!securityData.HasElement("fieldData"))
                    {
                        string errorInfo = string.Format("FutureDataListener_RecordFutureEconomicData:There is no field data!The error ticker is {0}.", ticker);
                        Console.WriteLine(errorInfo);
                        Logging.WriteErrorLog(errorInfo);
                        continue;
                    }
                    else
                    {
                        Element fields = securityData.GetElement("fieldData");
                        int numberOfFields = fields.NumElements;
                        if (numberOfFields == 0)
                        {
                            string errorInfo = string.Format("FutureDataListener_RecordFutureEconomicData:There is no field for this security here!The error ticker is {0}.", ticker);
                            Console.WriteLine(errorInfo);
                            Logging.WriteErrorLog(errorInfo);
                            continue;
                        }
                        else
                        {
                            int numberOfField = fields.NumElements;

                            if (numberOfField == 0)
                            {
                                string errorInfo = string.Format("FutureDataListener_RecordFutureEconomicData:There is no field for this security here!The error ticker is {0}.", ticker);
                                Console.WriteLine(errorInfo);
                                Logging.WriteErrorLog(errorInfo);
                                m_EconomicDataManager.m_BadTickerList.Add(ticker);
                                continue;
                            }
                            else
                            {
                                Element field = fields.GetElement(0);

                                if (field.NumValues > 0)
                                {
                                    for (int k = 0; k < field.NumValues; k++)
                                    {
                                        Element temp = field.GetValueAsElement(k);
                                        Element temptemp = temp.GetElement(0);
                                        string accurateTime = temptemp.GetValueAsString();
                                        string futureReleaseDate = TransformDateStringFormat(accurateTime);
                                        string futureReleaseTime = TransformTimeStringFormat(accurateTime);

                                        EconomicDataPoint economicDataPoint = new EconomicDataPoint();
                                        economicDataPoint.CreateFutureEconomicPoint(
                                            ticker,
                                            futureReleaseDate,
                                            futureReleaseTime
                                            );
                                        m_EconomicDataManager.AddFutureEconomicPoint(economicDataPoint);
                                        m_EconomicDataManager.m_RowCount++;
                                        string text = string.Format("FutureDataListener_RecordFutureEconomicData:Bloomberg gave {0} rows already. Message:{1}",
                                            m_EconomicDataManager.m_RowCount, economicDataPoint.WriteDetaToString());
                                        stringBuilder.AppendLine(text);
                                        Console.WriteLine(text);
                                    }
                                }
                                else
                                {
                                    string errorInfo = string.Format("FutureDataListener_RecordFutureEconomicData:The array returned has 0 dimension. The error ticker is {0}.", ticker);
                                    Console.WriteLine(errorInfo);
                                    Logging.WriteErrorLog(errorInfo);
                                    m_EconomicDataManager.m_BadTickerList.Add(ticker);
                                    continue;
                                }
                            }
                        }
                    }
                }
            }

            // Write to log all the message details.
            Logging.WriteLog(stringBuilder.ToString());
        }

        /// <summary>
        /// Transform future release datetime to suitable date format.
        /// </summary>
        /// <param name="futureReleaseDateTime"></param>
        /// <returns></returns>
        private string TransformDateStringFormat(string futureReleaseDateTime)
        {
            // Bloomberg date format is yyyy-MM-dd HH:mm:ss
            DateTime dateValue = DateTime.MaxValue;

            // Try to parse a 19 digits string to system datetime type.
            try
            {
                DateTime.TryParseExact(futureReleaseDateTime, "yyyy/MM/dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dateValue);
            }

            // Catch the parsing format error exception.
            catch (FormatException)
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("FutureDataListener_TransformDateStringFormat:There is mistake in transforming the date string to system date time type in future data listener.");
                stringBuilder.AppendFormat("The input string is {0}", futureReleaseDateTime);
                Console.WriteLine(stringBuilder.ToString());
                Logging.WriteErrorLog(stringBuilder.ToString());
            }
            return dateValue.ToString("yyyyMMdd");
        }

        /// <summary>
        /// Transform future release datetime to suitable time format.
        /// </summary>
        /// <param name="futureReleaseDateTime"></param>
        /// <returns></returns>
        private string TransformTimeStringFormat(string futureReleaseDateTime)
        {
            // Bloomberg date format is yyyy-MM-dd HH:mm:ss
            DateTime dateValue = DateTime.MaxValue;

            // Try to parse a 19 digits string to system datetime type.
            try
            {
                DateTime.TryParseExact(futureReleaseDateTime, "yyyy/MM/dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dateValue);
            }

            // Catch the parsing format error exception.
            catch (FormatException)
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("FutureDataListener_TransformTimeStringFormat:There is mistake in transforming the date string to system date time type in future data listener.");
                stringBuilder.AppendFormat("The input string is {0}", futureReleaseDateTime);
                Console.WriteLine(stringBuilder.ToString());
                Logging.WriteErrorLog(stringBuilder.ToString());
            }
            return dateValue.ToString("HH:mm:ss.fff");
        }
        #endregion//Private Methods


        #region Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// Eventhandler for future economic data listen complete.
        /// </summary>
        public event EventHandler FutureListenComplete;

        private void OnFutureListenComplete()
        {
            if (this.FutureListenComplete != null)
            {
                this.FutureListenComplete(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Time out on the timer. Check status.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnFutureDataListeningTimeOut(object sender, EventArgs e)
        {
            Logging.m_BloombergConnectionStatus = false;
            string errorInfo;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("FutureDataListener_OnFutureDataListeningTimeOut:The following tickers get stuck!");
            foreach (string ticker in m_Tickers)
            {
                stringBuilder.Append(ticker.ToString());
            }
            errorInfo = stringBuilder.ToString();
            Console.WriteLine(errorInfo);
            Logging.WriteErrorLog(errorInfo);

            m_Timer.Stop();
            try
            {
                m_Thread.Abort();
            }
            catch (Exception ex)
            {
                errorInfo = "FutureDataListener_OnFutureDataListeningTimeOut:ThreadAbortException";
                Console.WriteLine(ex.ToString());
                Logging.WriteErrorLog(errorInfo);
            }
            finally
            {
                m_Thread = null;
            }

            //// Create new session.
            //SessionOptions newSessionOptions = new SessionOptions();
            //newSessionOptions.ServerHost = "localhost";
            //newSessionOptions.ServerPort = 8194;
            //Session newSession = new Session(newSessionOptions);
            //m_Session = newSession;
            //if (!m_Session.Start())
            //{
            //    errorInfo = "Started the Session Failed!!!";
            //    Console.WriteLine(errorInfo);
            //    Logging.WriteErrorLog(errorInfo);
            //    return;
            //}
            //string successInfo = "Started the Session Successfully!";
            //Console.WriteLine(successInfo);
            //Logging.WriteLog(successInfo);

            //// Open new service.
            //if (!m_Session.OpenService("//blp/refdata"))
            //{
            //    errorInfo = "Open service failed!!!";
            //    Console.WriteLine(errorInfo);
            //    Logging.WriteErrorLog(errorInfo);
            //}
            //else
            //{
            //    string text = "Open service successfully!";
            //    Console.WriteLine(text);
            //    Logging.WriteLog(text);
            //    Logging.m_BloombergConnectionStatus = true;
            //}

            //RunListening();
        }
        #endregion//Event Handlers
    }
}
