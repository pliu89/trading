using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace BRE.Lib.TermStructures
{
    using UV.Lib.Hubs;
    using UV.Lib.Products;

    /// <summary>
    /// This class is hedge option collections for one quote instrument.
    /// </summary>
    public class HedgeOptions
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //       
        private int m_PossibleHedgeCount;
        private InstrumentName m_QuoteInstrument;
        private string m_InstrumentNameTT;
        private string m_InstrumentNameDatabase;
        private List<HedgeOption> m_HedgeOptions;
        private LogHub m_Log = null;
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        //    
        private HedgeOptions(InstrumentName instrumentName, string instrumentNameTT, string instrumentNameDatabase, LogHub log)
        {
            m_QuoteInstrument = instrumentName;
            m_InstrumentNameTT = instrumentNameTT;
            m_InstrumentNameDatabase = instrumentNameDatabase;
            m_Log = log;

            m_HedgeOptions = new List<HedgeOption>();
        }
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        public int PossibleHedgeCount
        {
            get { return m_PossibleHedgeCount; }
        }
        public InstrumentName QuoteInstrument
        {
            get { return m_QuoteInstrument; }
        }
        public string InstrumentNameTT
        {
            get { return m_InstrumentNameTT; }
        }
        public string InstrumentNameDataBase
        {
            get { return m_InstrumentNameDatabase; }
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
        /// This function will generate hedge options. It checks whether the instrument name TT from the database equals to instrument full name.
        /// This is why that it is made static constructor.
        /// </summary>
        /// <param name="instrumentName"></param>
        /// <param name="instrumentNameTT"></param>
        /// <param name="instrumentNameDatabase"></param>
        /// <returns></returns>
        public static bool TryCreateHedgeOptions(InstrumentName instrumentName, string instrumentNameTT, string instrumentNameDatabase, LogHub log, out HedgeOptions hedgeOptions)
        {
            bool isSuccess = false;
            hedgeOptions = null;
            string instrumentFullName = instrumentName.FullName;
            if (instrumentFullName.Equals(instrumentNameTT))
            {
                hedgeOptions = new HedgeOptions(instrumentName, instrumentNameTT, instrumentNameDatabase, log);
                isSuccess = true;
            }
            else
                log.NewEntry(LogLevel.Error, "TT instrument name from database {0} not same as instrument full name from TT {1}.", instrumentNameTT, instrumentFullName);
            return isSuccess;
        }
        //
        //
        /// <summary>
        /// This function add hedge option to hedge option collection for the quote instrument.
        /// </summary>
        /// <param name="hedgeOption"></param>
        /// <returns></returns>
        public bool TryAddHedgeOption(HedgeOption hedgeOption)
        {
            bool isSuccess = false;
            if (!m_HedgeOptions.Contains(hedgeOption))
            {
                m_HedgeOptions.Add(hedgeOption);
                m_PossibleHedgeCount++;
                isSuccess = true;
            }
            else
            {
                m_Log.NewEntry(LogLevel.Error, "Already added this hedge option for instrument {0}.", m_QuoteInstrument);
                return isSuccess;
            }
            return isSuccess;
        }
        //
        //
        /// <summary>
        /// Return all hedge options for this instrument.
        /// </summary>
        /// <param name="hedgeOptionsList"></param>
        /// <returns></returns>
        public bool TryGetHedgeOptions(ref List<HedgeOption> hedgeOptionsList)
        {
            bool isSuccess = false;
            hedgeOptionsList.Clear();
            if (m_HedgeOptions != null && m_HedgeOptions.Count > 0)
            {
                hedgeOptionsList.AddRange(m_HedgeOptions);
                isSuccess = true;
            }
            else
            {
                m_Log.NewEntry(LogLevel.Error, "Hedge option list is null or empty for quote instrument {0}.", m_QuoteInstrument);
                return isSuccess;
            }
            return isSuccess;
        }
        //
        //
        /// <summary>
        /// This function will return a list of all possible hedge instruments for OMA trading strategy.
        /// It is only used for spread with two legs.
        /// </summary>
        /// <param name="quoteInstrument"></param>
        /// <param name="hedgeInstruments"></param>
        /// <returns></returns>
        public bool TryGetHedgeInstruments(ref List<InstrumentName> hedgeInstruments)
        {
            bool isSuccess = false;
            hedgeInstruments.Clear();
            foreach (HedgeOption hedgeOption in m_HedgeOptions)
            {
                if (hedgeOption.HedgeInstruments.Count == 1)
                {
                    InstrumentName possibleHedgeInstrument = hedgeOption.HedgeInstruments[0];
                    hedgeInstruments.Add(possibleHedgeInstrument);
                }
                else
                {
                    m_Log.NewEntry(LogLevel.Error, "Hedge instruments dimension more than 1, should not use this function for quote instrument {0}.", m_QuoteInstrument);
                    return isSuccess;
                }
            }
            isSuccess = true;
            return isSuccess;
        }
        //
        //
        //
        /// <summary>
        /// This function is different from the function above. It is more generally and it can take into account multiple hedge legs for one quote leg.
        /// Each hedge option might include hedge instruments with dimension larger than 2. So the output become a list of list.
        /// The first list represents a spread generated of more than 2 legs.
        /// This function will be used if more general cases are to be considered.
        /// </summary>
        /// <param name="quoteInstrument"></param>
        /// <param name="hedgeInstruments"></param>
        /// <returns></returns>
        public bool TryGetHedgeInstruments(ref List<List<InstrumentName>> hedgeInstruments)
        {
            bool isSuccess = false;
            hedgeInstruments.Clear();
            foreach (HedgeOption hedgeOption in m_HedgeOptions)
            {
                if (hedgeOption.HedgeInstruments.Count >= 1)
                {
                    List<InstrumentName> possibleHedgeInstruments = hedgeOption.HedgeInstruments;
                    hedgeInstruments.Add(possibleHedgeInstruments);
                }
                else
                {
                    m_Log.NewEntry(LogLevel.Error, "Hedge instruments dimension less than 1, wrong for quote instrument {0}.", m_QuoteInstrument);
                    return isSuccess;
                }
            }
            isSuccess = true;
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
