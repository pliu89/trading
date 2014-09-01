using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BRE.Lib.TermStructures
{
    using BRE.Lib.Utilities;

    using UV.Lib.Hubs;
    using UV.Lib.Products;

    /// <summary>
    /// This is the hedge options writer.
    /// </summary>
    public class HedgeOptionsWriter
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        private int m_MonthCycleNumber = 3;
        private LogHub m_Log = null;
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        //       
        public HedgeOptionsWriter(LogHub log)
        {
            m_Log = log;
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
        /// This function will hedge options for the OMA trading strategy quote instrument.
        /// </summary>
        /// <param name="quoteInstrument"></param>
        /// <param name="hedgeOptionsString"></param>
        /// <returns></returns>
        public bool TryWriteHedgeOptionsForInstrument(InstrumentName quoteInstrument, out string hedgeOptionsString)
        {
            bool isSuccess = false;
            hedgeOptionsString = null;
            ProductTypes productTypes = quoteInstrument.Product.Type;
            string productName = quoteInstrument.Product.ProductName;
            string seriesName = quoteInstrument.SeriesName;
            List<HedgeOption> hedgeOptions = new List<HedgeOption>();
            StringBuilder hedgeOptionsBuilder = new StringBuilder();
            switch (productTypes)
            {
                case ProductTypes.Future:
                    break;
                case ProductTypes.Spread:
                    if (seriesName.Contains("Calendar"))
                    {
                        // Target: Calendar: 1xGE Sep14:-1xDec14
                        List<string> seriesNamesInTheString = new List<string>();
                        string pattern = @"[a-zA-Z]{3}[0-9]{2}";
                        MatchCollection matchCollection = Regex.Matches(seriesName, pattern);
                        foreach (Match match in matchCollection)
                        {
                            if (match.Success)
                                seriesNamesInTheString.Add(match.Value);
                        }
                        if (seriesNamesInTheString.Count == 2)
                        {
                            DateTime frontDateTime;
                            DateTime endDateTime;
                            if (!DateTime.TryParseExact(seriesNamesInTheString[0], "MMMyy", null, DateTimeStyles.None, out frontDateTime))
                            {
                                m_Log.NewEntry(LogLevel.Error, "DateTime parsed failed for this string {0}.", seriesNamesInTheString[0]);
                                return isSuccess;
                            }
                            if (!DateTime.TryParseExact(seriesNamesInTheString[1], "MMMyy", null, DateTimeStyles.None, out endDateTime))
                            {
                                m_Log.NewEntry(LogLevel.Error, "DateTime parsed failed for this string {0}.", seriesNamesInTheString[1]);
                                return isSuccess;
                            }
                            if (frontDateTime.Month % 3 != 0)
                            {
                                m_Log.NewEntry(LogLevel.Error, "Not need to write this hedge option because {0} month index is not multiples of 3.", seriesNamesInTheString[0]);
                                return isSuccess;
                            }
                            int duration = endDateTime.Month - frontDateTime.Month + (endDateTime.Year - frontDateTime.Year) * 12;
                            if (duration % 3 != 0)
                            {
                                m_Log.NewEntry(LogLevel.Error, "Not need to write this hedge option because {0} duration is not multiples of 3.", seriesNamesInTheString[0]);
                                return isSuccess;
                            }
                            if (duration / 3 > 3)
                            {
                                m_Log.NewEntry(LogLevel.Error, "Not need to write this hedge option because {0} duration is too large than 3rd curve.", seriesNamesInTheString[0]);
                                return isSuccess;
                            }
                            int realDuration = duration / m_MonthCycleNumber;
                            if (realDuration > 0)
                            {
                                List<DateTime> hedgeInstrumentStarts = new List<DateTime>();
                                List<DateTime> hedgeInstrumentEnds = new List<DateTime>();
                                List<int> quoteRatios = new List<int>();
                                List<int> hedgeRatios = new List<int>();
                                int caseNumber = 0;
                                switch (realDuration)
                                {
                                    case 1:
                                        // 5 cases in this situation.
                                        // 1: (1,2) to (2,3)    1:-1=1
                                        hedgeInstrumentStarts.Add(frontDateTime.AddMonths(duration));
                                        hedgeInstrumentEnds.Add(endDateTime.AddMonths(duration));
                                        quoteRatios.Add(1);
                                        hedgeRatios.Add(-1);
                                        caseNumber++;

                                        // 2: (1,2) to (0,1)    -1:1=1
                                        hedgeInstrumentStarts.Add(frontDateTime.AddMonths(-duration));
                                        hedgeInstrumentEnds.Add(endDateTime.AddMonths(-duration));
                                        quoteRatios.Add(-1);
                                        hedgeRatios.Add(1);
                                        caseNumber++;

                                        // 3: (1,2) to (1,3)    2:-1=1
                                        hedgeInstrumentStarts.Add(frontDateTime);
                                        hedgeInstrumentEnds.Add(endDateTime.AddMonths(duration));
                                        quoteRatios.Add(2);
                                        hedgeRatios.Add(-1);
                                        caseNumber++;

                                        // 4: (1,2) to (0,2)    -2:1=1
                                        hedgeInstrumentStarts.Add(frontDateTime.AddMonths(-duration));
                                        hedgeInstrumentEnds.Add(endDateTime);
                                        quoteRatios.Add(-2);
                                        hedgeRatios.Add(1);
                                        caseNumber++;

                                        // 5: (1,2) to (0,3)    -3:1=1
                                        hedgeInstrumentStarts.Add(frontDateTime.AddMonths(-duration));
                                        hedgeInstrumentEnds.Add(endDateTime.AddMonths(duration));
                                        quoteRatios.Add(-3);
                                        hedgeRatios.Add(1);
                                        caseNumber++;
                                        break;
                                    case 2:
                                        // 4 cases in this situation.
                                        // 1: (2,4) to (4,6)    1:-1=1
                                        hedgeInstrumentStarts.Add(frontDateTime.AddMonths(duration));
                                        hedgeInstrumentEnds.Add(endDateTime.AddMonths(duration));
                                        quoteRatios.Add(1);
                                        hedgeRatios.Add(-1);
                                        caseNumber++;

                                        // 2: (2,4) to (3,4)    1:-2=1   
                                        hedgeInstrumentStarts.Add(frontDateTime.AddMonths(duration / 2));
                                        hedgeInstrumentEnds.Add(endDateTime);
                                        quoteRatios.Add(1);
                                        hedgeRatios.Add(-2);
                                        caseNumber++;

                                        // 3: (2,4) to (0,2)    -1:1=1
                                        hedgeInstrumentStarts.Add(frontDateTime.AddMonths(-duration));
                                        hedgeInstrumentEnds.Add(endDateTime.AddMonths(-duration));
                                        quoteRatios.Add(-1);
                                        hedgeRatios.Add(1);
                                        caseNumber++;

                                        // 4: (2,4) to (2,3)    -1:2=1
                                        hedgeInstrumentStarts.Add(frontDateTime);
                                        hedgeInstrumentEnds.Add(endDateTime.AddMonths(-duration / 2));
                                        quoteRatios.Add(-1);
                                        hedgeRatios.Add(2);
                                        caseNumber++;
                                        break;
                                    case 3:
                                        // 3 cases in this situation.
                                        // 1: (3,6) to (6,9)    1:-1=1
                                        hedgeInstrumentStarts.Add(frontDateTime.AddMonths(duration));
                                        hedgeInstrumentEnds.Add(endDateTime.AddMonths(duration));
                                        quoteRatios.Add(1);
                                        hedgeRatios.Add(-1);
                                        caseNumber++;

                                        // 2: (3,6) to (0,3)    -1:1=1
                                        hedgeInstrumentStarts.Add(frontDateTime.AddMonths(-duration));
                                        hedgeInstrumentEnds.Add(endDateTime.AddMonths(-duration));
                                        quoteRatios.Add(-1);
                                        hedgeRatios.Add(1);
                                        caseNumber++;

                                        // 3: (3,6) to (4,5)    1:-3=1
                                        hedgeInstrumentStarts.Add(frontDateTime.AddMonths(duration / 3));
                                        hedgeInstrumentEnds.Add(endDateTime.AddMonths(-duration / 3));
                                        quoteRatios.Add(1);
                                        hedgeRatios.Add(-3);
                                        caseNumber++;
                                        break;
                                    default:
                                        break;
                                }
                                bool isFirstHedgeOption = true;
                                for (int hedgeIndex = 0; hedgeIndex < caseNumber; ++hedgeIndex)
                                {
                                    string hedgeInstrumentSeriesName = string.Format("Calendar: 1x{0} {1}:-1x{2}",
                                        productName, hedgeInstrumentStarts[hedgeIndex].ToString("MMMyy"),
                                        hedgeInstrumentEnds[hedgeIndex].ToString("MMMyy"));
                                    InstrumentName hedgeInstrument = new InstrumentName(quoteInstrument.Product, hedgeInstrumentSeriesName);
                                    ResultingInstrument resultingInstrument;
                                    if (QTMath.TryComposeResultingInstrument(quoteInstrument, quoteRatios[hedgeIndex],
                                        hedgeInstrument, hedgeRatios[hedgeIndex], m_Log, out resultingInstrument))
                                    {
                                        HedgeOption hedgeOption = new HedgeOption();
                                        hedgeOption.QuoteInstrument = quoteInstrument;
                                        hedgeOption.QuoteWeight = quoteRatios[hedgeIndex];
                                        hedgeOption.ResultingInstrument = resultingInstrument;
                                        hedgeOption.ResultingWeight = 1;
                                        hedgeOption.TryAddInstrumentAndWeight(hedgeInstrument, hedgeRatios[hedgeIndex]);
                                        hedgeOptions.Add(hedgeOption);
                                    }
                                    else
                                    {
                                        m_Log.NewEntry(LogLevel.Error, "Failed to compose resulting instrument for quote {0} and hedge {1}.", quoteInstrument, hedgeInstrument);
                                        return isSuccess;
                                    }

                                    if (isFirstHedgeOption)
                                    {
                                        isFirstHedgeOption = false;
                                    }
                                    else
                                    {
                                        hedgeOptionsBuilder.Append("|");
                                    }

                                    // Consider the target example: 
                                    // 1x{1xGE_U4.-1xGE_Z4}-1x{1xGE_Z4.-1xGE_H5}=1x{1xGE_U4.-2xGE_Z4.1xGE_H5}|-1x{1xGE_U4.-1xGE_Z4}+1x{1xGE_M4.-1xGE_U4}=1x{1xGE_M4.-2xGE_U4.1xGE_Z4}
                                    // |2x{1xGE_U4.-1xGE_Z4}-1x{1xGE_U4.-1xGE_H5}=1x{1xGE_U4.-2xGE_Z4.1xGE_H5}|-2x{1xGE_U4.-1xGE_Z4}+1x{1xGE_M4.-1xGE_Z4}=1x{1xGE_M4.-2xGE_U4.1xGE_Z4}
                                    // |-3x{1xGE_U4.-1xGE_Z4}+1x{1xGE_M4.-1xGE_H5}=1x{1xGE_M4.-3xGE_U4.3xGE_Z4.-1xGE_H5}
                                    string quoteInstrumentDatabaseName;
                                    string hedgeInstrumentDatabaseName;
                                    if (!QTMath.TryConvertInstrumentNameToInstrumentNameDatabase(quoteInstrument, m_Log, out quoteInstrumentDatabaseName))
                                    {
                                        m_Log.NewEntry(LogLevel.Error, "Failed to get instrument database name for instrument {0}.", quoteInstrument);
                                        return isSuccess;
                                    }
                                    if (!QTMath.TryConvertInstrumentNameToInstrumentNameDatabase(hedgeInstrument, m_Log, out hedgeInstrumentDatabaseName))
                                    {
                                        m_Log.NewEntry(LogLevel.Error, "Failed to get instrument database name for instrument {0}.", hedgeInstrument);
                                        return isSuccess;
                                    }
                                    string hedgeWeightWithSigned = hedgeRatios[hedgeIndex] > 0 ? string.Format("+{0}", hedgeRatios[hedgeIndex]) : (hedgeRatios[hedgeIndex].ToString());
                                    hedgeOptionsBuilder.AppendFormat("{0}x{{{1}}}{2}x{{{3}}}=1x{{{4}}}", quoteRatios[hedgeIndex], quoteInstrumentDatabaseName,
                                        hedgeWeightWithSigned, hedgeInstrumentDatabaseName, resultingInstrument.ResultingInstrumentNameDataBase);
                                }
                                hedgeOptionsString = hedgeOptionsBuilder.ToString();
                                isSuccess = true;
                            }
                            else
                            {
                                m_Log.NewEntry(LogLevel.Error, "Duration smaller or equal than 0 for instrument {0}.", quoteInstrument);
                                return isSuccess;
                            }
                        }
                        else
                        {
                            m_Log.NewEntry(LogLevel.Error, "Series name does not have two month years for this instrument {0}.", quoteInstrument);
                            return isSuccess;
                        }
                    }
                    else
                    {
                        m_Log.NewEntry(LogLevel.Error, "No product type is handled for this instrument {0}.", quoteInstrument);
                        return isSuccess;
                    }
                    break;
                default:
                    break;
            }
            return isSuccess;
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
