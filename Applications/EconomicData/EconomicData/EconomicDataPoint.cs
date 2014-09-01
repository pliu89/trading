using System;
using System.Collections.Generic;
using System.Text;

namespace EconomicBloombergProject
{
    public class EconomicDataPoint
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //     
        //
        // members in economic point in order of required schema.
        public string m_CountryCode;                                       // Country code
        public string m_Ticker;                                            // Ticker
        public string m_EcoDate;                                           // Economic release date
        public string m_EcoTime;                                           // Economic release time
        public string m_UnixTimeStamp;                                     // Unix time stamp in seconds
        public string m_EventName;                                         // Economic event description
        public string m_ShortName;                                         // Economic short name
        public string m_SecurityName;                                      // Ticker security name

        public string m_SurveyLastPrice;                                   // Actual release value for historical economic event or last price for latest economic event
        public string m_SurveyHigh;                                        // Survey highest value
        public string m_SurveyLow;                                         // Survey lowest value
        public string m_SurveyMedian;                                      // Survey median value
        public string m_SurveyAverage;                                     // Survey average value
        public string m_SurveyObservations;                                // Survey objects being investigated
        public string m_FutureStandardDeviation;                           // Forward standard deviation of survey values

        public string m_IndexUpdateFrequency;                              // Publishing frequency for the economic event
        public string m_RelevanceValue;                                    // Relevance value for the economic event
        public string m_TimeZoneCode;                                      // Bloomberg time zone code
        public string m_ExchangeLocationName;                              // Exchange location
        public string m_ObservationPeriod;                                 // Observation date for economic event
        private string m_EmptySign = Logging.m_EmptySign;                  // Initial default value for economic variables.
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        //   
        /// <summary>
        /// Set initial default values for the newly created economic data point.
        /// </summary>
        public EconomicDataPoint()
        {
            SetInitialEmptyValue();
        }
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        public string Ticker
        {
            get { return m_Ticker; }
        }
        public string Date
        {
            get { return m_EcoDate; }
        }

        // There is overlap between future listener and historical listener, and only the time for economic data point should be updated.
        public string Time
        {
            get { return m_EcoTime; }
            set { m_EcoTime = value; }
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
        /// Get input from the latest economic data listener and create data point.
        /// </summary>
        /// <param name="countryCode"></param>
        /// <param name="ticker"></param>
        /// <param name="releaseDate"></param>
        /// <param name="releaseTime"></param>
        /// <param name="eventName"></param>
        /// <param name="shortName"></param>
        /// <param name="securityName"></param>
        /// <param name="surveyLastPrice"></param>
        /// <param name="surveyHigh"></param>
        /// <param name="surveyLow"></param>
        /// <param name="surveyMedian"></param>
        /// <param name="surveyAverage"></param>
        /// <param name="surveyobservations"></param>
        /// <param name="futureStandardDeviation"></param>
        /// <param name="indexUpdateFrequency"></param>
        /// <param name="relevanceValue"></param>
        /// <param name="timeZone"></param>
        /// <param name="exchangestring"></param>
        /// <param name="observationPeriod"></param>
        /// <returns></returns>
        public EconomicDataPoint CreateLatestEconomicPoint(string countryCode, string ticker, string releaseDate, string releaseTime, string eventName, string shortName, string securityName, string surveyLastPrice, string surveyHigh, string surveyLow, string surveyMedian, string surveyAverage, string surveyobservations, string futureStandardDeviation, string indexUpdateFrequency, string relevanceValue, string timeZone, string observationPeriod)
        {
            // Set corresponding latest economic values to members.
            m_CountryCode = countryCode;
            m_Ticker = ticker;
            m_EcoDate = releaseDate;
            m_EcoTime = releaseTime;
            m_EventName = eventName;
            m_ShortName = shortName;
            m_SecurityName = securityName;
            m_SurveyLastPrice = surveyLastPrice;
            m_SurveyHigh = surveyHigh;
            m_SurveyLow = surveyLow;
            m_SurveyMedian = surveyMedian;
            m_SurveyAverage = surveyAverage;
            m_SurveyObservations = surveyobservations;
            m_FutureStandardDeviation = futureStandardDeviation;
            m_IndexUpdateFrequency = indexUpdateFrequency;
            m_RelevanceValue = relevanceValue;
            m_TimeZoneCode = timeZone;
            m_ObservationPeriod = observationPeriod;

            // The following variables should be calculated using variables from above.
            try
            {
                m_UnixTimeStamp = Convert.ToString(Functions.ConvertToUnixTimeStamp(m_EcoDate, m_EcoTime, Convert.ToInt32(m_TimeZoneCode)));
                m_ExchangeLocationName = Functions.ConvertToExchangeLocationName(Convert.ToInt32(m_TimeZoneCode));
            }
            catch(Exception ex)
            {
                string errorInfo = ex.ToString();
                Console.WriteLine(errorInfo);
                Logging.WriteErrorLog(errorInfo);
            }

            if (m_EcoDate.Length < 8)
            {
                string errorInfo = string.Format("EconomicDataPoint_CreateLatestEconomicPoint:The economic release date for the ticker {0} has some problem and the date is {1}", m_Ticker, m_EcoDate);
                Console.WriteLine(errorInfo);
                Logging.WriteErrorLog(errorInfo);
            }

            if (m_EcoTime.Length <12)
            {
                string errorInfo = string.Format("EconomicDataPoint_CreateLatestEconomicPoint:The economic release time for the ticker {0} has some problem and the time is {1}", m_Ticker, m_EcoTime);
                Console.WriteLine(errorInfo);
                Logging.WriteErrorLog(errorInfo);
            }

            return this;
        }

        /// <summary>
        /// Get input from the historical economic data listener and create data point.
        /// </summary>
        /// <param name="economicDataPoint"></param>
        /// <param name="releaseDate"></param>
        /// <param name="surveyLastPrice"></param>
        /// <param name="surveyHigh"></param>
        /// <param name="surveyLow"></param>
        /// <param name="surveyMedian"></param>
        /// <param name="surveyAverage"></param>
        /// <param name="surveyobservations"></param>
        /// <param name="futureStandardDeviation"></param>
        /// <returns></returns>
        public EconomicDataPoint CreateHistoricalEconomicPoint(EconomicDataPoint economicDataPoint, string releaseDate, string surveyLastPrice, string surveyHigh, string surveyLow, string surveyMedian, string surveyAverage, string surveyobservations, string futureStandardDeviation)
        {
            // Copy constant members from the latest economic data point as the first input for this function.
            m_CountryCode = economicDataPoint.m_CountryCode;
            m_Ticker = economicDataPoint.m_Ticker;
            m_EcoDate = releaseDate;
            m_EcoTime = economicDataPoint.m_EcoTime;
            m_EventName = economicDataPoint.m_EventName;
            m_ShortName = economicDataPoint.m_ShortName;
            m_SecurityName = economicDataPoint.m_SecurityName;
            m_IndexUpdateFrequency = economicDataPoint.m_IndexUpdateFrequency;
            m_RelevanceValue = economicDataPoint.m_RelevanceValue;
            m_TimeZoneCode = economicDataPoint.m_TimeZoneCode;
            m_ObservationPeriod = m_EmptySign;

            // Calculate the following economic variables.
            try
            {
                m_UnixTimeStamp = Convert.ToString(Functions.ConvertToUnixTimeStamp(m_EcoDate, m_EcoTime, Convert.ToInt32(m_TimeZoneCode)));
                m_ExchangeLocationName = Functions.ConvertToExchangeLocationName(Convert.ToInt32(m_TimeZoneCode));
            }
            catch (Exception ex)
            {
                string errorInfo = ex.ToString();
                Console.WriteLine(errorInfo);
                Logging.WriteErrorLog(errorInfo);
            }

            // Set corresponding historical economic values to other members.
            m_SurveyLastPrice = surveyLastPrice;
            m_SurveyHigh = surveyHigh;
            m_SurveyLow = surveyLow;
            m_SurveyMedian = surveyMedian;
            m_SurveyAverage = surveyAverage;
            m_SurveyObservations = surveyobservations;
            m_FutureStandardDeviation = futureStandardDeviation;

            if (m_EcoDate.Length < 8)
            {
                string errorInfo = string.Format("EconomicDataPoint_CreateHistoricalEconomicPoint:The economic release date for the ticker {0} has some problem and the date is {1}", m_Ticker, m_EcoDate);
                Console.WriteLine(errorInfo);
                Logging.WriteErrorLog(errorInfo);
            }

            if (m_EcoTime.Length < 12)
            {
                string errorInfo = string.Format("EconomicDataPoint_CreateHistoricalEconomicPoint:The economic release time for the ticker {0} has some problem and the time is {1}", m_Ticker, m_EcoTime);
                Console.WriteLine(errorInfo);
                Logging.WriteErrorLog(errorInfo);
            }

            return this;
        }

        /// <summary>
        /// Get input from the future economic data listener and create data point.
        /// </summary>
        /// <param name="economicDataPoint"></param>
        /// <param name="releaseDate"></param>
        /// <param name="releaseTime"></param>
        /// <returns></returns>
        public EconomicDataPoint CreateFutureEconomicPoint(string ticker, string releaseDate, string releaseTime)
        {
            // Copy constant members from the latest economic data point as the first input for this function.
            SetInitialEmptyValue();
            m_Ticker = ticker;
            m_EcoDate = releaseDate;
            m_EcoTime = releaseTime;

            if (m_EcoDate.Length < 8)
            {
                string errorInfo = string.Format("EconomicDataPoint_CreateFutureEconomicPoint:The economic release date for the ticker {0} has some problem and the date is {1}", m_Ticker, m_EcoDate);
                Console.WriteLine(errorInfo);
                Logging.WriteErrorLog(errorInfo);
            }

            if (m_EcoTime.Length < 8)
            {
                string errorInfo = string.Format("EconomicDataPoint_CreateFutureEconomicPoint:The economic release time for the ticker {0} has some problem and the time is {1}", m_Ticker, m_EcoTime);
                Console.WriteLine(errorInfo);
                Logging.WriteErrorLog(errorInfo);
            }

            return this;
        }

        /// <summary>
        /// This function updates missing fields for the economic data point.
        /// </summary>
        /// <param name="latestPoint"></param>
        public void UpdateMissingFutureDataPointFields(EconomicDataPoint latestPoint)
        {
            // Copy constant members from the latest economic data point as the first input for this function.
            m_CountryCode = latestPoint.m_CountryCode;
            m_Ticker = latestPoint.m_Ticker;
            m_EventName = latestPoint.m_EventName;
            m_ShortName = latestPoint.m_ShortName;
            m_SecurityName = latestPoint.m_SecurityName;
            m_IndexUpdateFrequency = latestPoint.m_IndexUpdateFrequency;
            m_RelevanceValue = latestPoint.m_RelevanceValue;
            m_TimeZoneCode = latestPoint.m_TimeZoneCode;

            // Calculate the following economic variables.
            try
            {
                m_UnixTimeStamp = Convert.ToString(Functions.ConvertToUnixTimeStamp(m_EcoDate, m_EcoTime, Convert.ToInt32(m_TimeZoneCode)));
                m_ExchangeLocationName = Functions.ConvertToExchangeLocationName(Convert.ToInt32(m_TimeZoneCode));
            }
            catch (Exception ex)
            {
                string errorInfo = ex.ToString();
                Console.WriteLine(errorInfo);
                Logging.WriteErrorLog(errorInfo);
            }
        }

        /// <summary>
        /// This function only updates the unix time stamp.
        /// </summary>
        public void UpdateUnixTimeStamp()
        {
            // Calculate the following economic variables.
            try
            {
                m_UnixTimeStamp = Convert.ToString(Functions.ConvertToUnixTimeStamp(m_EcoDate, m_EcoTime, Convert.ToInt32(m_TimeZoneCode)));
                m_ExchangeLocationName = Functions.ConvertToExchangeLocationName(Convert.ToInt32(m_TimeZoneCode));
            }
            catch (Exception ex)
            {
                string errorInfo = ex.ToString();
                Console.WriteLine(errorInfo);
                Logging.WriteErrorLog(errorInfo);
            }
        }

        /// <summary>
        /// Write the economic data point members to comma separated format.
        /// </summary>
        /// <returns></returns>
        public string WriteDetaToString()
        {
            // Report members of the economic data point.
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("{0},", m_CountryCode);
            stringBuilder.AppendFormat("{0},", m_Ticker);
            stringBuilder.AppendFormat("{0},", m_EcoDate);
            stringBuilder.AppendFormat("{0},", m_EcoTime);
            stringBuilder.AppendFormat("{0},", m_UnixTimeStamp);
            stringBuilder.AppendFormat("{0},", m_EventName);
            stringBuilder.AppendFormat("{0},", m_ShortName);
            stringBuilder.AppendFormat("{0},", m_SecurityName);
            stringBuilder.AppendFormat("{0},", m_SurveyLastPrice);
            stringBuilder.AppendFormat("{0},", m_SurveyHigh);
            stringBuilder.AppendFormat("{0},", m_SurveyLow);
            stringBuilder.AppendFormat("{0},", m_SurveyMedian);
            stringBuilder.AppendFormat("{0},", m_SurveyAverage);
            stringBuilder.AppendFormat("{0},", m_SurveyObservations);
            stringBuilder.AppendFormat("{0},", m_FutureStandardDeviation);
            stringBuilder.AppendFormat("{0},", m_IndexUpdateFrequency);
            stringBuilder.AppendFormat("{0},", m_RelevanceValue);
            stringBuilder.AppendFormat("{0},", m_TimeZoneCode);
            stringBuilder.AppendFormat("{0},", m_ExchangeLocationName);
            stringBuilder.AppendFormat("{0}.", m_ObservationPeriod);
            return stringBuilder.ToString();
        }

        /// <summary>
        /// This function update the transferred UNIX time stamp.
        /// </summary>
        /// <param name="economicDataPoint"></param>
        public void UpdateMatchedTimeDataPoint()
        {
            try
            {
                m_UnixTimeStamp = Convert.ToString(Functions.ConvertToUnixTimeStamp(m_EcoDate, m_EcoTime, Convert.ToInt32(m_TimeZoneCode)));
            }
            catch (Exception ex)
            {
                string errorInfo = ex.ToString();
                Console.WriteLine(errorInfo);
                Logging.WriteErrorLog(errorInfo);
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
        /// Set intial default value to each member.
        /// </summary>
        private void SetInitialEmptyValue()
        {
            // Set default null value to each member.
            m_CountryCode = m_EmptySign;
            m_Ticker = m_EmptySign;
            m_EcoDate = m_EmptySign;
            m_EcoTime = m_EmptySign;
            m_UnixTimeStamp = m_EmptySign;
            m_EventName = m_EmptySign;
            m_ShortName = m_EmptySign;
            m_SecurityName = m_EmptySign;
            m_SurveyLastPrice = m_EmptySign;
            m_SurveyHigh = m_EmptySign;
            m_SurveyLow = m_EmptySign;
            m_SurveyMedian = m_EmptySign;
            m_SurveyAverage = m_EmptySign;
            m_SurveyObservations = m_EmptySign;
            m_FutureStandardDeviation = m_EmptySign;
            m_IndexUpdateFrequency = m_EmptySign;
            m_RelevanceValue = m_EmptySign;
            m_TimeZoneCode = m_EmptySign;
            m_ExchangeLocationName = m_EmptySign;
            m_ObservationPeriod = m_EmptySign;
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
