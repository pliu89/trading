using System;
using System.Collections.Generic;
using System.Text;
using Bloomberglp.Blpapi;
using System.Timers;
using Timer = System.Timers.Timer;
using EventHandler = System.EventHandler;
using System.Threading;

namespace EconomicBloombergProject
{
    public class LatestDataListener
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
        private const string m_CountryCodeString = "COUNTRY_ISO";
        private const string m_TickerString = "TICKER";
        private const string m_EcoDateString = "ECO_RELEASE_DT";
        private const string m_EcoTimeString = "ECO_RELEASE_TIME";
        private const string m_EventNameString = "NAME";
        private const string m_LongCompNameString = "LONG_COMP_NAME";
        private const string m_ShortNameString = "SHORT_NAME";
        private const string m_SecurityNameString = "SECURITY_NAME";
        private const string m_SurveyLastPriceString = "LAST_PRICE";
        private const string m_SurveyHighString = "BN_SURVEY_HIGH";
        private const string m_SurveyLowString = "BN_SURVEY_LOW";
        private const string m_SurveyMedianString = "BN_SURVEY_MEDIAN";
        private const string m_SurveyAverageString = "BN_SURVEY_AVERAGE";
        private const string m_SurveyObservationsString = "BN_SURVEY_NUMBER_OBSERVATIONS";
        private const string m_FutureStandardDeviationString = "FORECAST_STANDARD_DEVIATION";
        private const string m_IndexUpdateFrequencyString = "INDX_FREQ";
        private const string m_RelevanceValueString = "RELEVANCE_VALUE";
        private const string m_TimeZoneCodeString = "TIME_ZONE_OVERRIDE";
        private const string m_ObservationPeriodString = "OBSERVATION_PERIOD";
        private string m_EmptySign = Logging.m_EmptySign;

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
        /// Set values to session, tickers and economic data manager in latest listener.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="tickers"></param>
        /// <param name="newTickers"></param>
        /// <param name="economicDataManager"></param>
        public LatestDataListener(Session session, EconomicDataManager economicDataManager, Timer timer)
        {
            m_Session = session;
            m_EconomicDataManager = economicDataManager;
            m_Timer = timer;
            m_Timer.Elapsed += new ElapsedEventHandler(OnLatestDataListeningTimeOut);
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
                Service service = m_Session.GetService("//blp/refdata");
                Request request = service.CreateRequest("ReferenceDataRequest");
                Element securities = request.GetElement("securities");

                // Ensure each time the number of tickers that we want to listen to is less than 10.
                if (m_Tickers.Count > m_EconomicDataManager.m_TickerListenLimit)
                {
                    string errorInfo = string.Format("LatestDataListener_RunListening:There are too many tickers! Ticker number is {0}", m_Tickers.Count);
                    Console.WriteLine(errorInfo);
                    Logging.WriteErrorLog(errorInfo);
                    m_Session.Stop();
                    return;
                }

                // If there is no valid tickers in this chunk. Don't need to listen. Triger latest data listen complete event.
                if (m_Tickers.Count == 0)
                {
                    m_Timer.Stop();
                    string text = "No tickers needed to be listened to in this chunk.";
                    Console.WriteLine(text);
                    Logging.WriteLog(text);
                    OnLatestListenComplete();
                }
                else
                {

                    foreach (string ticker in m_Tickers)
                    {
                        securities.AppendValue(ticker);
                    }

                    Element fields = request.GetElement("fields");
                    fields.AppendValue(m_CountryCodeString);
                    fields.AppendValue(m_TickerString);
                    fields.AppendValue(m_EcoDateString);
                    fields.AppendValue(m_EcoTimeString);
                    fields.AppendValue(m_EventNameString);
                    fields.AppendValue(m_LongCompNameString);
                    fields.AppendValue(m_ShortNameString);
                    fields.AppendValue(m_SecurityNameString);
                    fields.AppendValue(m_SurveyLastPriceString);
                    fields.AppendValue(m_SurveyHighString);
                    fields.AppendValue(m_SurveyLowString);
                    fields.AppendValue(m_SurveyMedianString);
                    fields.AppendValue(m_SurveyAverageString);
                    fields.AppendValue(m_SurveyObservationsString);
                    fields.AppendValue(m_FutureStandardDeviationString);
                    fields.AppendValue(m_IndexUpdateFrequencyString);
                    fields.AppendValue(m_RelevanceValueString);
                    fields.AppendValue(m_TimeZoneCodeString);
                    fields.AppendValue(m_ObservationPeriodString);

                    // Send Request.
                    Logging.WriteLog(string.Format("LatestDataListener_RunListening:{0} tickers.", m_Tickers.Count));

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
        /// Wait response and handle messages.
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
                        RecordLatestEconomicData(eventObj);
                        break;
                    case Event.EventType.PARTIAL_RESPONSE:
                        RecordLatestEconomicData(eventObj);
                        break;
                }
            }

            m_Timer.Stop();
            // Triger finished event.
            OnLatestListenComplete();
        }

        /// <summary>
        /// Record latest economic data information.
        /// </summary>
        /// <param name="eventObj"></param>
        private void RecordLatestEconomicData(Event eventObj)
        {
            StringBuilder stringBuilder = new StringBuilder();

            foreach (Message message in eventObj)
            {
                stringBuilder.AppendLine(message.ToString());
                Element response = message.AsElement;
                if (response.HasElement("responseError"))
                {
                    string errorInfo = string.Format("LatestDataListener_RecordLatestEconomicData:There is response error here!The message content is {0}.", message.ToString());
                    Console.WriteLine(errorInfo);
                    Logging.WriteErrorLog(errorInfo);
                    continue;
                }

                if (!response.HasElement("securityData"))
                {
                    string errorInfo = string.Format("LatestDataListener_RecordLatestEconomicData:There is no securityData here!The message content is {0}.", message.ToString());
                    Console.WriteLine(errorInfo);
                    Logging.WriteErrorLog(errorInfo);
                    continue;
                }

                Element securityDataArray = response.GetElement("securityData");
                int numberOfSecurityData = securityDataArray.NumValues;

                if (numberOfSecurityData == 0)
                {
                    string errorInfo = string.Format("LatestDataListener_RecordLatestEconomicData:There is no data for any securities here! The message content is {0}.", message.ToString());
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
                        string errorInfo = string.Format("LatestDataListener_RecordLatestEconomicData:There is no ticker! The message content is {0}.", message.ToString());
                        Console.WriteLine(errorInfo);
                        Logging.WriteErrorLog(errorInfo);
                        continue;
                    }

                    if (securityData.HasElement("securityError"))
                    {
                        string errorInfo = string.Format("LatestDataListener_RecordLatestEconomicData:There is securityError!The error ticker is {0}.", ticker);
                        Console.WriteLine(errorInfo);
                        Logging.WriteErrorLog(errorInfo);
                        continue;
                    }

                    if (securityData.HasElement("fieldException"))
                    {
                        string errorInfo = string.Format("LatestDataListener_RecordLatestEconomicData:There is field exception!The error ticker is {0}.", ticker);
                        Console.WriteLine(errorInfo);
                        Logging.WriteErrorLog(errorInfo);
                        continue;
                    }

                    if (!securityData.HasElement("fieldData"))
                    {
                        string errorInfo = string.Format("LatestDataListener_RecordLatestEconomicData:There is no field data!The error ticker is {0}.", ticker);
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
                            string errorInfo = string.Format("LatestDataListener_RecordLatestEconomicData:There is no field for this security here!The error ticker is {0}.", ticker);
                            Console.WriteLine(errorInfo);
                            Logging.WriteErrorLog(errorInfo);
                            continue;
                        }
                        else
                        {
                            string countryID = m_EmptySign;
                            string ecoDate = m_EmptySign;
                            string ecoTime = m_EmptySign;
                            string eventName = m_EmptySign;
                            string shortName = m_EmptySign;
                            string securityName = m_EmptySign;
                            string surveyLast = m_EmptySign;
                            string surveyHigh = m_EmptySign;
                            string surveyLow = m_EmptySign;
                            string surveyMedian = m_EmptySign;
                            string surveyAverage = m_EmptySign;
                            string surveyObservations = m_EmptySign;
                            string futureStandardDeviation = m_EmptySign;
                            string indexUpdateFrequency = m_EmptySign;
                            string relevanceValue = m_EmptySign;
                            string timeZoneCode = m_EmptySign;
                            string observationPeriod = m_EmptySign;

                            if (fields.HasElement(m_CountryCodeString))
                                countryID = fields.GetElementAsString(m_CountryCodeString);
                            else
                            {
                                string text = string.Format("LatestDataListener_RecordLatestEconomicData:Ticker:{0} does not have field {1}.", ticker, m_CountryCodeString);
                                Console.WriteLine(text);
                                Logging.WriteErrorLog(text);
                            }
                            if (fields.HasElement(m_EcoDateString))
                                ecoDate = fields.GetElementAsString(m_EcoDateString);
                            else
                            {
                                string text = string.Format("LatestDataListener_RecordLatestEconomicData:For the ticker:{0}, the Bloomberg does not know field {1}.", ticker, m_EcoDateString);
                                Console.WriteLine(text);
                                Logging.WriteErrorLog(text);
                            }
                            if (fields.HasElement(m_EcoTimeString))
                                ecoTime = fields.GetElementAsString(m_EcoTimeString);
                            else
                            {
                                string text = string.Format("LatestDataListener_RecordLatestEconomicData:For the ticker:{0}, the Bloomberg does not know field {1}.", ticker, m_EcoTimeString);
                                Console.WriteLine(text);
                                Logging.WriteErrorLog(text);
                            }
                            if (fields.HasElement(m_EventNameString))
                                eventName = fields.GetElementAsString(m_EventNameString);
                            else
                            {
                                string text = string.Format("LatestDataListener_RecordLatestEconomicData:Ticker:{0} does not have field {1}.", ticker, m_EventNameString);
                                Console.WriteLine(text);
                                Logging.WriteErrorLog(text);
                            }
                            if (fields.HasElement(m_LongCompNameString))
                            {
                                string longCompName = fields.GetElementAsString(m_LongCompNameString);
                                if (longCompName != null && longCompName != "" && longCompName != string.Empty)
                                    eventName = longCompName;
                            }
                            else
                            {
                                string text = string.Format("LatestDataListener_RecordLatestEconomicData:Ticker:{0} does not have field {1}.", ticker, m_LongCompNameString);
                                Console.WriteLine(text);
                                Logging.WriteErrorLog(text);
                            }
                            if (fields.HasElement(m_ShortNameString))
                                shortName = fields.GetElementAsString(m_ShortNameString);
                            else
                            {
                                string text = string.Format("LatestDataListener_RecordLatestEconomicData:Ticker:{0} does not have field {1}.", ticker, m_ShortNameString);
                                Console.WriteLine(text);
                                Logging.WriteErrorLog(text);
                            }
                            if (fields.HasElement(m_SecurityNameString))
                                securityName = fields.GetElementAsString(m_SecurityNameString);
                            else
                            {
                                string text = string.Format("LatestDataListener_RecordLatestEconomicData:Ticker:{0} does not have field {1}.", ticker, m_SecurityNameString);
                                Console.WriteLine(text);
                                Logging.WriteErrorLog(text);
                            }
                            if (fields.HasElement(m_SurveyLastPriceString))
                                surveyLast = fields.GetElementAsString(m_SurveyLastPriceString);
                            else
                            {
                                string text = string.Format("LatestDataListener_RecordLatestEconomicData:Ticker:{0} does not have field {1}.", ticker, m_SurveyLastPriceString);
                                Console.WriteLine(text);
                                Logging.WriteErrorLog(text);
                            }
                            if (fields.HasElement(m_SurveyHighString))
                                surveyHigh = fields.GetElementAsString(m_SurveyHighString);
                            else
                            {
                                string text = string.Format("LatestDataListener_RecordLatestEconomicData:Ticker:{0} does not have field {1}.", ticker, m_SurveyHighString);
                                Console.WriteLine(text);
                                Logging.WriteErrorLog(text);
                            }
                            if (fields.HasElement(m_SurveyLowString))
                                surveyLow = fields.GetElementAsString(m_SurveyLowString);
                            else
                            {
                                string text = string.Format("LatestDataListener_RecordLatestEconomicData:Ticker:{0} does not have field {1}.", ticker, m_SurveyLowString);
                                Console.WriteLine(text);
                                Logging.WriteErrorLog(text);
                            }
                            if (fields.HasElement(m_SurveyMedianString))
                                surveyMedian = fields.GetElementAsString(m_SurveyMedianString);
                            else
                            {
                                string text = string.Format("LatestDataListener_RecordLatestEconomicData:Ticker:{0} does not have field {1}.", ticker, m_SurveyMedianString);
                                Console.WriteLine(text);
                                Logging.WriteErrorLog(text);
                            }
                            if (fields.HasElement(m_SurveyAverageString))
                                surveyAverage = fields.GetElementAsString(m_SurveyAverageString);
                            else
                            {
                                string text = string.Format("LatestDataListener_RecordLatestEconomicData:Ticker:{0} does not have field {1}.", ticker, m_SurveyAverageString);
                                Console.WriteLine(text);
                                Logging.WriteErrorLog(text);
                            }
                            if (fields.HasElement(m_SurveyObservationsString))
                                surveyObservations = fields.GetElementAsString(m_SurveyObservationsString);
                            else
                            {
                                string text = string.Format("LatestDataListener_RecordLatestEconomicData:Ticker:{0} does not have field {1}.", ticker, m_SurveyObservationsString);
                                Console.WriteLine(text);
                                Logging.WriteErrorLog(text);
                            }
                            if (fields.HasElement(m_FutureStandardDeviationString))
                                futureStandardDeviation = fields.GetElementAsString(m_FutureStandardDeviationString);
                            else
                            {
                                string text = string.Format("LatestDataListener_RecordLatestEconomicData;Ticker:{0} does not have field {1}.", ticker, m_FutureStandardDeviationString);
                                Console.WriteLine(text);
                                Logging.WriteErrorLog(text);
                            }
                            if (fields.HasElement(m_IndexUpdateFrequencyString))
                                indexUpdateFrequency = fields.GetElementAsString(m_IndexUpdateFrequencyString);
                            else
                            {
                                string text = string.Format("LatestDataListener_RecordLatestEconomicData:Ticker:{0} does not have field {1}.", ticker, m_IndexUpdateFrequencyString);
                                Console.WriteLine(text);
                                Logging.WriteErrorLog(text);
                            }
                            if (fields.HasElement(m_RelevanceValueString))
                                relevanceValue = fields.GetElementAsString(m_RelevanceValueString);
                            else
                            {
                                string text = string.Format("LatestDataListener_RecordLatestEconomicData:Ticker:{0} does not have field {1}.", ticker, m_RelevanceValueString);
                                Console.WriteLine(text);
                                Logging.WriteErrorLog(text);
                            }
                            if (fields.HasElement(m_TimeZoneCodeString))
                                timeZoneCode = fields.GetElementAsString(m_TimeZoneCodeString);
                            else
                            {
                                string text = string.Format("LatestDataListener_RecordLatestEconomicData:Ticker:{0} does not have field {1}.", ticker, m_TimeZoneCodeString);
                                Console.WriteLine(text);
                                Logging.WriteErrorLog(text);
                            }
                            if (fields.HasElement(m_ObservationPeriodString))
                                observationPeriod = fields.GetElementAsString(m_ObservationPeriodString);
                            else
                            {
                                string text = string.Format("LatestDataListener_RecordLatestEconomicData:Ticker:{0} does not have field {1}.", ticker, m_ObservationPeriodString);
                                Console.WriteLine(text);
                                Logging.WriteErrorLog(text);
                            }

                            ecoDate = TransformDateStringFormat(ecoDate);
                            EconomicDataPoint economicDataPoint = new EconomicDataPoint();
                            economicDataPoint.CreateLatestEconomicPoint(
                                countryID,
                                ticker,
                                ecoDate,
                                ecoTime,
                                eventName,
                                shortName,
                                securityName,
                                surveyLast,
                                surveyHigh,
                                surveyLow,
                                surveyMedian,
                                surveyAverage,
                                surveyObservations,
                                futureStandardDeviation,
                                indexUpdateFrequency,
                                relevanceValue,
                                timeZoneCode,
                                observationPeriod
                                );
                            m_EconomicDataManager.AddLatestEconomicPoint(economicDataPoint);
                            m_EconomicDataManager.m_RowCount++;
                            string textMessage = string.Format("LatestDataListener_RecordLatestEconomicData:Bloomberg gave {0} rows already. Message:{1}"
                                , m_EconomicDataManager.m_RowCount, economicDataPoint.WriteDetaToString());
                            stringBuilder.AppendLine(textMessage);
                            Console.WriteLine(textMessage);
                        }
                    }
                }
            }

            // Write to log all the message details.
            Logging.WriteLog(stringBuilder.ToString());
        }

        /// <summary>
        /// This function convert bloomberg date format to yyyyMMdd from yyyy-mm-dd.
        /// </summary>
        /// <param name="ecoDate"></param>
        /// <returns></returns>
        private string TransformDateStringFormat(string ecoDate)
        {
            // Bloomberg date format is yyyy-MM-dd.
            DateTime dateValue = new DateTime(1970, 1, 1);

            // Try to parse a 10 digits yyyy-mm-dd to system datetime type.
            try
            {
                DateTime.TryParseExact(ecoDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dateValue);
            }

            // Catch the parsing format error exception.
            catch (FormatException)
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("LatestDataListener_TransformDateStringFormat:There is mistake in transforming the date string to system date time type in latest data listener.");
                stringBuilder.AppendFormat("The input string is {0}", ecoDate);
                Console.WriteLine(stringBuilder.ToString());
                Logging.WriteErrorLog(stringBuilder.ToString());
            }
            return dateValue.ToString("yyyyMMdd");
        }
        #endregion//Private Methods


        #region Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// Eventhandler for latest economic data listen complete.
        /// </summary>
        public event EventHandler LatestListenComplete;

        private void OnLatestListenComplete()
        {
            if (this.LatestListenComplete != null)
            {
                this.LatestListenComplete(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Time out on the timer. Check status.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLatestDataListeningTimeOut(object sender, EventArgs e)
        {
            Logging.m_BloombergConnectionStatus = false;
            string errorInfo;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("LatestDataListener_OnLatestDataListeningTimeOut:The following tickers get stuck!");
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
                errorInfo = "LatestDataListener_OnLatestDataListeningTimeOut:ThreadAbortException";
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
