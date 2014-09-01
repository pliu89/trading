using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using Bloomberglp.Blpapi;
using System.Timers;
using Timer = System.Timers.Timer;
using EventHandler = System.EventHandler;
using System.Threading;

namespace EconomicBloombergProject
{
    public class HistoricalDataListener
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
        private string m_RequestStartDate = string.Empty;
        private string m_RequestEndDate = string.Empty;
        private string m_RecentDate = DateTime.Now.ToString("yyyyMMdd");
        private bool m_ContinueListening = true;

        // Field info request constant strings.
        private const string m_EcoDateString = "ECO_RELEASE_DT";
        private const string m_SurveyActualRelease = "ACTUAL_RELEASE";
        private const string m_SurveyHighString = "BN_SURVEY_HIGH";
        private const string m_SurveyLowString = "BN_SURVEY_LOW";
        private const string m_SurveyMedianString = "BN_SURVEY_MEDIAN";
        private const string m_SurveyAverageString = "BN_SURVEY_AVERAGE";
        private const string m_SurveyObservationsString = "BN_SURVEY_NUMBER_OBSERVATIONS";
        private const string m_FutureStandardDeviationString = "FORECAST_STANDARD_DEVIATION";
        private string m_EmptySign = Logging.m_EmptySign;

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
        /// Set values to session, tickers and economic data manager in historical listener.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="tickers"></param>
        /// <param name="newTickers"></param>
        /// <param name="economicDataManager"></param>
        public HistoricalDataListener(Session session, EconomicDataManager economicDataManager, Timer timer)
        {
            m_Session = session;
            m_EconomicDataManager = economicDataManager;
            m_Timer = timer;

            m_RequestStartDate = economicDataManager.StartDate;
            m_RequestEndDate = economicDataManager.EndDate;

            m_Timer.Elapsed += new ElapsedEventHandler(OnHistoricalDataListeningTimeOut);
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
                
                // Check whether the earliest date is reached.
                if (Functions.StringToDateTime(m_RequestStartDate) < m_EconomicDataManager.EarliestDate)
                {
                    string errorInfo = "HistoricalDataListener_RunListening:Earliest date broken!!! Session stop!";
                    Console.WriteLine(errorInfo);
                    Logging.WriteErrorLog(errorInfo);
                    m_Session.Stop();
                    return;
                }

                // Ensure each time the number of tickers that we want to listen to is less than 10.
                if (m_Tickers.Count > m_EconomicDataManager.m_TickerListenLimit)
                {
                    string errorInfo = string.Format("HistoricalDataListener_RunListening:There are too many tickers! Ticker number is {0}", m_Tickers.Count);
                    Console.WriteLine(errorInfo);
                    Logging.WriteErrorLog(errorInfo);
                    m_Session.Stop();
                    return;
                }

                // If there is no valid tickers in this chunk. Don't need to listen. Triger historical data listen complete event.
                if (m_Tickers.Count == 0)
                {
                    m_Timer.Stop();
                    string text = "No tickers needed to be listened to in this chunk.";
                    Console.WriteLine(text);
                    Logging.WriteLog(text);
                    OnHistoricalListenComplete();
                }
                else
                {
                    // Edit request.
                    Service service = m_Session.GetService("//blp/refdata");
                    Request request = service.CreateRequest("HistoricalDataRequest");
                    Element securities = request.GetElement("securities");

                    foreach (string ticker in m_Tickers)
                    {
                        securities.AppendValue(ticker);
                    }

                    Element fields = request.GetElement("fields");
                    fields.AppendValue(m_EcoDateString);
                    fields.AppendValue(m_SurveyActualRelease);
                    fields.AppendValue(m_SurveyHighString);
                    fields.AppendValue(m_SurveyLowString);
                    fields.AppendValue(m_SurveyMedianString);
                    fields.AppendValue(m_SurveyAverageString);
                    fields.AppendValue(m_SurveyObservationsString);
                    fields.AppendValue(m_FutureStandardDeviationString);

                    // Edit request time range.
                    DateTime startDateTime = Functions.StringToDateTime(m_EconomicDataManager.StartDate).AddMonths(-1);
                    int year = startDateTime.Year;
                    int month = startDateTime.Month;
                    int day = 28;
                    startDateTime = new DateTime(year, month, day);
                    m_RequestStartDate = startDateTime.ToString("yyyyMMdd");

                    request.Set("periodicitySelection", "DAILY");
                    request.Set("startDate", m_RequestStartDate);
                    request.Set("endDate", m_RequestEndDate);
                    Logging.WriteLog(string.Format("HistoricalDataListener_RunListening:{0} tickers with date range of {1} to {2}.",
                         m_Tickers.Count, m_RequestStartDate, m_RequestEndDate));

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
            // Loop.
            m_ContinueListening = true;
            while (m_ContinueListening)
            {
                // Handle event.
                Event eventObj = m_Session.NextEvent();
                switch (eventObj.Type)
                {
                    case Event.EventType.RESPONSE:
                        m_ContinueListening = false;
                        RecordHistoricalEconomicData(eventObj);
                        break;
                    case Event.EventType.PARTIAL_RESPONSE:
                        RecordHistoricalEconomicData(eventObj);
                        break;
                }
            }

            m_Timer.Stop();
            // Trigger the event wehn historical listening is complete.
            OnHistoricalListenComplete();
        }

        /// <summary>
        /// Record historical economic data information.
        /// </summary>
        /// <param name="eventObj"></param>
        private void RecordHistoricalEconomicData(Event eventObj)
        {
            StringBuilder stringBuilder = new StringBuilder();

            foreach (Message message in eventObj)
            {
                stringBuilder.AppendLine(message.ToString());
                Element response = message.AsElement;
                if (response.HasElement("responseError"))
                {
                    string errorInfo = string.Format("HistoricalDataListener_RecordHistoricalEconomicData:There is response error here!The message content is {0}.", message.ToString());
                    Console.WriteLine(errorInfo);
                    Logging.WriteErrorLog(errorInfo);
                    continue;
                }

                if (!response.HasElement("securityData"))
                {
                    string errorInfo = string.Format("HistoricalDataListener_RecordHistoricalEconomicData:There is no securityData here!The message content is {0}.", message.ToString());
                    Console.WriteLine(errorInfo);
                    Logging.WriteErrorLog(errorInfo);
                    continue;
                }

                Element securityData = response.GetElement("securityData");

                if (securityData.HasElement("securityError"))
                {
                    string errorInfo = string.Format("HistoricalDataListener_RecordHistoricalEconomicData:There is security error here!The message content is {0}.", message.ToString());
                    Console.WriteLine(errorInfo);
                    Logging.WriteErrorLog(errorInfo);
                    continue;
                }

                if (securityData.HasElement("fieldException"))
                {
                    string errorInfo = string.Format("HistoricalDataListener_RecordHistoricalEconomicData:There is field exception here!The message content is {0}.", message.ToString());
                    Console.WriteLine(errorInfo);
                    Logging.WriteErrorLog(errorInfo);
                    continue;
                }

                string ticker = string.Empty;
                if (securityData.HasElement("security"))
                    ticker = securityData.GetElementAsString("security");
                else
                {
                    string errorInfo = string.Format("HistoricalDataListener_RecordHistoricalEconomicData:There is no ticker!The message content is {0}.", message.ToString());
                    Console.WriteLine(errorInfo);
                    Logging.WriteErrorLog(errorInfo);
                    continue;
                }

                if (!securityData.HasElement("fieldData"))
                {
                    string errorInfo = string.Format("HistoricalDataListener_RecordHistoricalEconomicData:There is no field data!The error ticker is {0}.", ticker);
                    Console.WriteLine(errorInfo);
                    Logging.WriteErrorLog(errorInfo);
                    continue;
                }
                else
                {
                    Element fieldDataArray = securityData.GetElement("fieldData");
                    int numberOfFieldRows = fieldDataArray.NumValues;
                    if (numberOfFieldRows == 0)
                    {
                        string errorInfo = string.Format("HistoricalDataListener_RecordHistoricalEconomicData:There is no field data history! The error ticker name is {0}.", ticker);
                        Console.WriteLine(errorInfo);
                        Logging.WriteErrorLog(errorInfo);
                        continue;
                    }
                    else
                    {
                        EconomicDataSeries economicDataSeries;
                        m_EconomicDataManager.EconomicDataSeriesByTicker.TryGetValue(ticker, out economicDataSeries);

                        if (economicDataSeries != null)
                        {
                            EconomicDataPoint latestEconomicDataPoint = economicDataSeries.LatestEconomicPoint;

                            if (latestEconomicDataPoint == null)
                            {
                                string text = string.Format("HistoricalDataListener_RecordHistoricalEconomicData:There is no latest economic data point for this ticker:{0}.", ticker);
                                Console.WriteLine(text);
                                Logging.WriteErrorLog(text);
                                continue;
                            }

                            if (numberOfFieldRows > 0)
                            {
                                for (int i = 0; i < numberOfFieldRows; ++i)
                                {
                                    Element fieldData = fieldDataArray.GetValueAsElement(i);
                                    string releaseDate = m_EmptySign;
                                    string surveyActualRelease = m_EmptySign;
                                    string surveyHigh = m_EmptySign;
                                    string surveyLow = m_EmptySign;
                                    string surveyMedian = m_EmptySign;
                                    string surveyAverage = m_EmptySign;
                                    string surveyObservations = m_EmptySign;
                                    string futureStandardDeviation = m_EmptySign;

                                    if (fieldData.HasElement(m_EcoDateString))
                                        releaseDate = fieldData.GetElementAsString(m_EcoDateString);
                                    else
                                    {
                                        string text = string.Format("HistoricalDataListener_RecordHistoricalEconomicData:Ticker:{0} does not have field {1}.", ticker, m_EcoDateString);
                                        Console.WriteLine(text);
                                        Logging.WriteErrorLog(text);
                                    }
                                    if (fieldData.HasElement(m_SurveyActualRelease))
                                        surveyActualRelease = fieldData.GetElementAsString(m_SurveyActualRelease);
                                    else
                                    {
                                        string text = string.Format("HistoricalDataListener_RecordHistoricalEconomicData:Ticker:{0} does not have field {1}.", ticker, m_SurveyActualRelease);
                                        Console.WriteLine(text);
                                        Logging.WriteErrorLog(text);
                                    }
                                    if (fieldData.HasElement(m_SurveyHighString))
                                        surveyHigh = fieldData.GetElementAsString(m_SurveyHighString);
                                    else
                                    {
                                        string text = string.Format("HistoricalDataListener_RecordHistoricalEconomicData:Ticker:{0} does not have field {1}.", ticker, m_SurveyHighString);
                                        Console.WriteLine(text);
                                        Logging.WriteErrorLog(text);
                                    }
                                    if (fieldData.HasElement(m_SurveyLowString))
                                        surveyLow = fieldData.GetElementAsString(m_SurveyLowString);
                                    else
                                    {
                                        string text = string.Format("HistoricalDataListener_RecordHistoricalEconomicData:Ticker:{0} does not have field {1}.", ticker, m_SurveyLowString);
                                        Console.WriteLine(text);
                                        Logging.WriteErrorLog(text);
                                    }
                                    if (fieldData.HasElement(m_SurveyMedianString))
                                        surveyMedian = fieldData.GetElementAsString(m_SurveyMedianString);
                                    else
                                    {
                                        string text = string.Format("HistoricalDataListener_RecordHistoricalEconomicData:Ticker:{0} does not have field {1}.", ticker, m_SurveyMedianString);
                                        Console.WriteLine(text);
                                        Logging.WriteErrorLog(text);
                                    }
                                    if (fieldData.HasElement(m_SurveyAverageString))
                                        surveyAverage = fieldData.GetElementAsString(m_SurveyAverageString);
                                    else
                                    {
                                        string text = string.Format("HistoricalDataListener_RecordHistoricalEconomicData:Ticker:{0} does not have field {1}.", ticker, m_SurveyAverageString);
                                        Console.WriteLine(text);
                                        Logging.WriteErrorLog(text);
                                    }
                                    if (fieldData.HasElement(m_SurveyObservationsString))
                                        surveyObservations = fieldData.GetElementAsString(m_SurveyObservationsString);
                                    else
                                    {
                                        string text = string.Format("HistoricalDataListener_RecordHistoricalEconomicData:Ticker:{0} does not have field {1}.", ticker, m_SurveyObservationsString);
                                        Console.WriteLine(text);
                                        Logging.WriteErrorLog(text);
                                    }
                                    if (fieldData.HasElement(m_FutureStandardDeviationString))
                                        futureStandardDeviation = fieldData.GetElementAsString(m_FutureStandardDeviationString);
                                    else
                                    {
                                        string text = string.Format("HistoricalDataListener_RecordHistoricalEconomicData:Ticker:{0} does not have field {1}.", ticker, m_FutureStandardDeviationString);
                                        Console.WriteLine(text);
                                        Logging.WriteErrorLog(text);
                                    }

                                    EconomicDataPoint economicDataPoint = new EconomicDataPoint();
                                    economicDataPoint.CreateHistoricalEconomicPoint(
                                        latestEconomicDataPoint,
                                        releaseDate,
                                        surveyActualRelease,
                                        surveyHigh,
                                        surveyLow,
                                        surveyMedian,
                                        surveyAverage,
                                        surveyObservations,
                                        futureStandardDeviation
                                        );
                                    m_EconomicDataManager.AddHistoricalEconomicPoint(economicDataPoint);
                                    m_EconomicDataManager.m_RowCount++;

                                    // Append message that we record.
                                    string textMessage = string.Format("HistoricalDataListener_RecordHistoricalEconomicData:Bloomberg gave {0} rows already. Message:{1}"
                                        , m_EconomicDataManager.m_RowCount, economicDataPoint.WriteDetaToString());
                                    stringBuilder.AppendLine(textMessage);
                                    Console.WriteLine(textMessage);
                                }
                            }
                            else
                            {
                                string errorInfo = string.Format("HistoricalDataListener_RecordHistoricalEconomicData:The returned array dimension is 0. The error ticker name is {0}.", ticker);
                                Console.WriteLine(errorInfo);
                                Logging.WriteErrorLog(errorInfo);
                                continue;
                            }
                        }
                        else
                        {
                            string errorInfo = string.Format("HistoricalDataListener_RecordHistoricalEconomicData:There is no economic series for this ticker here! The error ticker is {0}.", ticker);
                            Console.WriteLine(errorInfo);
                            Logging.WriteErrorLog(errorInfo);
                            continue;
                        }
                    }
                }
            }

            // Write to log all the message details.
            Logging.WriteLog(stringBuilder.ToString());
        }
        #endregion//Private Methods


        #region Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// Eventhandler for historical economic data listen complete.
        /// </summary>
        public event EventHandler HistoricalListenComplete;

        private void OnHistoricalListenComplete()
        {
            if (this.HistoricalListenComplete != null)
            {
                this.HistoricalListenComplete(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Time out on the timer. Check status.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnHistoricalDataListeningTimeOut(object sender, EventArgs e)
        {
            Logging.m_BloombergConnectionStatus = false;
            string errorInfo;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("HistoricalDataListener_OnHistoricalDataListeningTimeOut:The following tickers get stuck!");
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
                errorInfo = "HistoricalDataListener_OnHistoricalDataListeningTimeOut:ThreadAbortException";
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
