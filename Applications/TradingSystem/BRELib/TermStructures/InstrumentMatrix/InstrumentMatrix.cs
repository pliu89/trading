using System;
using System.Collections.Generic;
using System.Text;

namespace BRE.Lib.TermStructures.InstrumentMatrix
{
    using UV.Lib.Hubs;
    using UV.Lib.Products;

    using UV.Lib.DatabaseReaderWriters.Queries;

    /// <summary>
    /// This is an instantiable class and it contains instrument mapping functions.
    /// In the instrument matrix, it also contains map to the resulting instrument.
    /// The instrument matrix is only used for spread composed of two instruments.
    /// It will be changed/improved further to be used for spread composed of more than 2 instruments.
    /// </summary>
    public class InstrumentMatrix
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        private bool m_IsSetup = false;
        private StringBuilder m_StringBuilder = new StringBuilder();
        private List<InstrumentName> m_InstrumentsList;
        private List<List<ResultingInstrument>> m_ResultingInstruments;
        private List<List<TradingRatio>> m_TradingRatios;
        private LogHub m_Log = null;
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        //  
        /// <summary>
        /// Constructor gets all the trading instruments from the user defineds in the config file.
        /// </summary>
        /// <param name="instrumentNames"></param>
        public InstrumentMatrix(List<InstrumentName> instrumentNames, LogHub log)
        {
            m_InstrumentsList = new List<InstrumentName>(instrumentNames);
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
        /// This function will find the resulting instrument based on the quote and hedge instruments and their quantities.
        /// </summary>
        /// <param name="quoteInstrument"></param>
        /// <param name="quoteQty"></param>
        /// <param name="hedgeInstrument"></param>
        /// <param name="hedgeQty"></param>
        /// <param name="resultingInstrument"></param>
        /// <returns></returns>
        public bool TryFindResultingInstrument(InstrumentName quoteInstrument, int quoteQty, InstrumentName hedgeInstrument, int hedgeQty, out ResultingInstrument resultingInstrument)
        {
            bool isSuccess = false;
            resultingInstrument = null;

            // Find the index of the quote instrument and hedge instrument in the list. 
            // They demonstrate the x-y position of these instruments in the matrix.
            // And then find the corresponding resulting instrument in the matrix.

            int quoteInstrumentIndex = m_InstrumentsList.IndexOf(quoteInstrument);
            int hedgeInstrumentIndex = m_InstrumentsList.IndexOf(hedgeInstrument);

            // If the resulting instrument at a certain position in the matrix is empty, it shows that there are no hedge instruments for these quote instruments found in the database.
            // If the index is wrong, it shows the quote or hedge instrument is not a traded instrument that is input to the matrix at the start of the program.
            if (hedgeInstrumentIndex >= 0)
            {
                if (quoteInstrumentIndex >= 0)
                {
                    if (!m_ResultingInstruments[quoteInstrumentIndex][hedgeInstrumentIndex].IsEmpty)
                    {
                        resultingInstrument = m_ResultingInstruments[quoteInstrumentIndex][hedgeInstrumentIndex];
                        isSuccess = true;
                    }
                    else
                    {
                        //m_Log.NewEntry(LogLevel.Error, "Resulting instrument is empty for quote instrument {0} and hedge instrument {1}.",
                        //    quoteInstrument, hedgeInstrument);
                        return isSuccess;
                    }
                }
                else
                {
                    m_Log.NewEntry(LogLevel.Error, "Quote instrument {0} is not a traded instrument.", quoteInstrument);
                    return isSuccess;
                }
            }
            else
            {
                m_Log.NewEntry(LogLevel.Error, "Hedge instrument {0} is not a traded instrument.", hedgeInstrument);
                return isSuccess;
            }
            return isSuccess;
        }
        //
        //
        //
        /// <summary>
        /// This function will find the resulting instrument based on the quote and hedge instruments and their quantities.
        /// The remaining quantities are added because they probably will go to the net open position exposure.
        /// </summary>
        /// <param name="quoteInstrument"></param>
        /// <param name="quoteQty"></param>
        /// <param name="hedgeInstrument"></param>
        /// <param name="hedgeQty"></param>
        /// <param name="resultingInstrument"></param>
        /// <returns></returns>
        public bool TryFindResultingInstrument(InstrumentName quoteInstrument, int quoteQty, InstrumentName hedgeInstrument, int hedgeQty,
            out ResultingInstrument resultingInstrument, out int syntheticFillQty, out int quoteRemainingQty, out int hedgeRemainingQty)
        {
            bool isSuccess = false;
            resultingInstrument = null;
            syntheticFillQty = 0;
            quoteRemainingQty = 0;
            hedgeRemainingQty = 0;

            if (Math.Sign(quoteQty * hedgeQty) >= 0)
            {
                m_Log.NewEntry(LogLevel.Error, "Cannot generate synthetic fill as quote instrument {0} with quote qty {1} has same sign as hedge instrument {2} with hedge qty {3}.", quoteInstrument, quoteQty, hedgeInstrument, hedgeQty);
                return isSuccess;
            }

            // First step: Find the index of the quote instrument and hedge instrument in the list. 
            //             They demonstrate the x-y position of these instruments in the matrix.
            //             And then find the corresponding resulting instrument in the matrix.

            int quoteInstrumentIndex = m_InstrumentsList.IndexOf(quoteInstrument);
            int hedgeInstrumentIndex = m_InstrumentsList.IndexOf(hedgeInstrument);

            // If the resulting instrument at a certain position in the matrix is empty, it shows that there are no hedge instruments for these quote instruments found in the database.
            // If the index is wrong, it shows the quote or hedge instrument is not a traded instrument that is input to the matrix at the start of the program.
            if (hedgeInstrumentIndex >= 0)
            {
                if (quoteInstrumentIndex >= 0)
                {
                    if (!m_ResultingInstruments[quoteInstrumentIndex][hedgeInstrumentIndex].IsEmpty)
                    {
                        resultingInstrument = m_ResultingInstruments[quoteInstrumentIndex][hedgeInstrumentIndex];

                        // Second step: Generate synthetic fill for the resulting instrument.
                        //              This will give correct sign for resulting filled quantity.
                        //              Also it will also output the remaining undistributed fills, which may be added to partial repo later.

                        if (quoteInstrumentIndex < m_TradingRatios.Count &&
                            hedgeInstrumentIndex < m_TradingRatios[quoteInstrumentIndex].Count &&
                            m_TradingRatios[quoteInstrumentIndex][hedgeInstrumentIndex] != null &&
                            m_TradingRatios[quoteInstrumentIndex][hedgeInstrumentIndex].IsSet)
                        {
                            TradingRatio tradingRatio = m_TradingRatios[quoteInstrumentIndex][hedgeInstrumentIndex];
                            int quoteRatio = tradingRatio.QuoteRatio;
                            int hedgeRatio = tradingRatio.HedgeRatio;
                            int resultingRatio = tradingRatio.ResultingRatio;

                            // Calculate synthetic fill information
                            int absQuoteFilledQty = Math.Abs(quoteQty);
                            int absHedgeFilledQty = Math.Abs(hedgeQty);
                            int absQuoteRatio = Math.Abs(quoteRatio);
                            int absHedgeRatio = Math.Abs(hedgeRatio);
                            int absResultingRatio = Math.Abs(resultingRatio);

                            double floatQuoteFilledQty = absQuoteFilledQty / absQuoteRatio;
                            double floatHedgeFilledQty = absHedgeFilledQty / absHedgeRatio;
                            double minFloatQuoteHedgeFilledQty = Math.Min(floatQuoteFilledQty, floatHedgeFilledQty) / absResultingRatio;
                            int minIntQuoteHedgeFilledQty = (int)Math.Floor(minFloatQuoteHedgeFilledQty);
                            if (minIntQuoteHedgeFilledQty > 0)
                            {
                                syntheticFillQty = minIntQuoteHedgeFilledQty * Math.Sign(quoteQty * quoteRatio);
                                int quoteQtyChange = minIntQuoteHedgeFilledQty * absResultingRatio * absQuoteRatio * Math.Sign(quoteQty);
                                int hedgeQtyChange = minIntQuoteHedgeFilledQty * absResultingRatio * absHedgeRatio * Math.Sign(hedgeQty);
                                quoteRemainingQty = quoteQty - quoteQtyChange;
                                hedgeRemainingQty = hedgeQty - hedgeQtyChange;
                            }
                            else
                            {
                                // No synthetic fill generated in this case. The quantity is not enough.
                                syntheticFillQty = 0;
                                quoteRemainingQty = quoteQty;
                                hedgeRemainingQty = hedgeQty;
                                m_StringBuilder.Clear();
                                m_StringBuilder.AppendFormat("No synthetic fill has been generated for quote instrument is {0}, with quote ratio {1} and quote filled qty {2}",
                                    quoteInstrument, quoteRatio, quoteQty);
                                m_StringBuilder.AppendFormat(" and hedge instrument is {0}, with hedge ratio {1} and hedge filled qty {2}. Resulting instrument weight is {3}.",
                                    hedgeInstrument, hedgeRatio, hedgeQty, resultingRatio);
                                m_Log.NewEntry(LogLevel.Warning, m_StringBuilder.ToString());
                            }
                            isSuccess = true;
                        }
                        else
                        {
                            m_Log.NewEntry(LogLevel.Error, "Trading ratio is empty/null or not set for quote instrument {0} and hedge instrument {1}.",
                                quoteInstrument, hedgeInstrument);
                            return isSuccess;
                        }
                    }
                    else
                    {
                        m_Log.NewEntry(LogLevel.Error, "Resulting instrument is empty for quote instrument {0} and hedge instrument {1}.",
                            quoteInstrument, hedgeInstrument);
                        return isSuccess;
                    }
                }
                else
                {
                    m_Log.NewEntry(LogLevel.Error, "Quote instrument {0} is not a traded instrument.", quoteInstrument);
                    return isSuccess;
                }
            }
            else
            {
                m_Log.NewEntry(LogLevel.Error, "Hedge instrument {0} is not a traded instrument.", hedgeInstrument);
                return isSuccess;
            }
            return isSuccess;
        }
        //
        //
        /// <summary>
        /// This function will setup the instrument matrix.
        /// Also it will find all the trading ratios to generate synthetic fills.
        /// </summary>
        /// <param name="hedgeOptionsCollection"></param>
        /// <returns></returns>
        public bool TrySetupInstrumentMatrix(Dictionary<InstrumentName, HedgeOptions> hedgeOptionsCollection)
        {
            bool isSuccess = false;
            if (m_IsSetup)
            {
                m_Log.NewEntry(LogLevel.Error, "Instrument matrix is already setup, and cannot be setup again.");
                return isSuccess;
            }
            m_ResultingInstruments = new List<List<ResultingInstrument>>();
            m_TradingRatios = new List<List<TradingRatio>>();
            for (int x = 0; x < m_InstrumentsList.Count; ++x)
            {
                m_ResultingInstruments.Add(new List<ResultingInstrument>());
                m_TradingRatios.Add(new List<TradingRatio>());
                for (int y = 0; y < m_InstrumentsList.Count; ++y)
                {
                    m_ResultingInstruments[x].Add(new ResultingInstrument());
                    m_TradingRatios[x].Add(new TradingRatio());
                }
            }

            foreach (InstrumentName tradedInstrument in m_InstrumentsList)
            {
                if (hedgeOptionsCollection.ContainsKey(tradedInstrument))
                {
                    HedgeOptions hedgeOptions = hedgeOptionsCollection[tradedInstrument];
                    List<HedgeOption> hedgeOptionList = new List<HedgeOption>();
                    if (!hedgeOptions.TryGetHedgeOptions(ref hedgeOptionList))
                    {
                        m_Log.NewEntry(LogLevel.Error, "Failed to get hedge options for quote instrument {0}.", tradedInstrument);
                        return isSuccess;
                    }
                    foreach (HedgeOption hedgeOption in hedgeOptionList)
                    {
                        InstrumentName hedgeInstrument;
                        int hedgeRatio;
                        if (TryGetHedgeInstrument(hedgeOption, out hedgeInstrument, out hedgeRatio))
                        {
                            int quoteInstrumentIndex = m_InstrumentsList.IndexOf(tradedInstrument);
                            int hedgeInstrumentIndex = m_InstrumentsList.IndexOf(hedgeInstrument);
                            if (hedgeInstrumentIndex >= 0)
                            {
                                m_ResultingInstruments[quoteInstrumentIndex][hedgeInstrumentIndex] = hedgeOption.ResultingInstrument;
                                TradingRatio tradingRatio = m_TradingRatios[quoteInstrumentIndex][hedgeInstrumentIndex];
                                tradingRatio.QuoteInstrument = tradedInstrument;
                                tradingRatio.QuoteRatio = hedgeOption.QuoteWeight;
                                tradingRatio.HedgeInstrument = hedgeInstrument;
                                tradingRatio.HedgeRatio = hedgeRatio;
                                tradingRatio.ResultingInstrument = hedgeOption.ResultingInstrument;
                                tradingRatio.ResultingRatio = hedgeOption.ResultingWeight;
                                if (tradingRatio.QuoteRatio != 0 && tradingRatio.HedgeRatio != 0 && tradingRatio.ResultingRatio != 0)
                                {
                                    m_Log.NewEntry(LogLevel.Minor, "Matrix setup successful at {0}-{1} with instruments {2}-{3}->resulting instrument {4}.",
                                    quoteInstrumentIndex, hedgeInstrumentIndex, tradedInstrument, hedgeInstrument, hedgeOption.ResultingInstrument);
                                    tradingRatio.IsSet = true;
                                }
                                else
                                {
                                    m_Log.NewEntry(LogLevel.Error,
                                        "Trading ratio is 0 for quote instrument {0} or hedge instrument {1} or its resulting instrument {2}",
                                        tradedInstrument, hedgeInstrument, hedgeOption.ResultingInstrument);
                                    //return isSuccess;
                                }
                            }
                            else
                            {
                                m_Log.NewEntry(LogLevel.Error, "Hedge instrument {0} is not a traded instrument for quote instrument {1}.",
                                    hedgeInstrument, tradedInstrument);
                                //return isSuccess;
                            }
                        }
                        else
                        {
                            m_Log.NewEntry(LogLevel.Error, "Failed to get hedge instrument for traded instrument.", tradedInstrument);
                            return isSuccess;
                        }
                    }
                }
                else
                {
                    m_Log.NewEntry(LogLevel.Error, "The traded instrument {0} does not have hedge options.", tradedInstrument);
                    return isSuccess;
                }
            }
            m_IsSetup = true;
            isSuccess = true;
            return isSuccess;
        }
        #endregion//Public Methods


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// By default, in OMA trading framework, it contains only one hedge instrument.
        /// This function is only valid when there are only two instruments.
        /// </summary>
        /// <returns></returns>
        public bool TryGetHedgeInstrument(HedgeOption hedgeOption, out InstrumentName hedgeInstr, out int hedgeRatio)
        {
            bool isSuccess = false;
            hedgeInstr = hedgeOption.QuoteInstrument;
            hedgeRatio = 0;
            if (hedgeOption.InstrumentToWeights.Count < 2)
                return isSuccess;
            foreach (InstrumentName instr in hedgeOption.InstrumentToWeights.Keys)
            {
                if (!instr.Equals(hedgeOption.QuoteInstrument))
                {
                    hedgeInstr = instr;
                    hedgeRatio = hedgeOption.InstrumentToWeights[hedgeInstr];
                    isSuccess = true;
                }
            }
            return isSuccess;
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

    /// <summary>
    /// This is the trading ratio used exclusively for the two instruments spread trading strategy.
    /// </summary>
    public class TradingRatio
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        public InstrumentName QuoteInstrument;
        public InstrumentName HedgeInstrument;
        public ResultingInstrument ResultingInstrument;
        public int QuoteRatio;
        public int HedgeRatio;
        public int ResultingRatio;
        public bool IsSet = false;
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        //   
        public TradingRatio()
        {

        }
        #endregion//Constructors


        #region no Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
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
