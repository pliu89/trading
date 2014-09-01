using System;
using System.Collections.Generic;

namespace VioletAPI.KeltnerSpreader
{
    using Lib.PopUp;
    using Lib.TradingHelper;
    using System.Text;
    using UV.Lib.Hubs;
    using UV.Lib.IO.Xml;

    public class KeltnerTraderLogicManager : TradingLogicManagerBase, IStringifiable
    {

        #region Members
        private string m_TradingStrategyType = "KeltnerTradingStrategy";                                                    // Strategy Type: This is Keltner Trading Strategy.
        private int m_EntryQty;                                                                                             // Enter quantity.
        private int m_FadeQty;                                                                                              // Fade quantity.
        private double m_EntryWidth;                                                                                        // Entry width.
        private double m_FadeWidth;                                                                                         // Fade width.
        private double m_PukeWidth;                                                                                         // Puke width.
        private int m_EMALength;                                                                                            // EMA length.
        private int m_ATRLength;                                                                                            // ATR length.
        private int m_MOMLength;                                                                                            // Momentum length.
        private double m_LastEMA = double.NaN;                                                                              // Last EMA.
        private double m_LastATR = double.NaN;                                                                              // Last ATR.
        private double m_LastMOM = double.NaN;                                                                              // Last MOM.
        private double m_MomentumEntry;                                                                                     // Momentum entry threshold.
        private double m_MomentumPuke;                                                                                      // Momentum puke threshold.
        private double m_LongEntryBand = double.NaN;                                                                        // Long entry band value.
        private double m_LongFadeBand = double.NaN;                                                                         // Long fade band value.
        private double m_LongPukeBand = double.NaN;                                                                         // Long puke band value.
        private double m_ShortEntryBand = double.NaN;                                                                       // Short entry band value.
        private double m_ShortFadeBand = double.NaN;                                                                        // Short fade band value.
        private double m_ShortPukeBand = double.NaN;                                                                        // Short puke band value.

        public KeltnerTradingVariables KeltnerTradingVariables = null;                                                      // Keltner trading variables.
        private TradingNode m_EntryLongNode = null;                                                                         // Long entry level.
        private TradingNode m_EntryShortNode = null;                                                                        // Short entry level.
        private TradingNode m_FadeLongNode = null;                                                                          // Long fade level.
        private TradingNode m_FadeShortNode = null;                                                                         // Short fade level.
        #endregion


        #region Properties
        public int EntryQty
        {
            get { return m_EntryQty; }
            set
            {
                m_EntryQty = value;
                AdjustTradingNodes(m_NetPosition);
            }
        }
        public int FadeQty
        {
            get { return m_FadeQty; }
            set
            {
                m_FadeQty = value;
                AdjustTradingNodes(m_NetPosition);
            }
        }
        public double EntryWidth
        {
            get { return m_EntryWidth; }
        }
        public double FadeWidth
        {
            get { return m_FadeWidth; }
        }
        public double PukeWidth
        {
            get { return m_PukeWidth; }
        }
        public int EMALength
        {
            get { return m_EMALength; }
        }
        public int ATRLength
        {
            get { return m_ATRLength; }
        }
        public int MOMLength
        {
            get { return m_MOMLength; }
        }
        public double MomentumEntry
        {
            get { return m_MomentumEntry; }
            set
            {
                m_MomentumEntry = value;
            }
        }
        public double MomentumPuke
        {
            get { return m_MomentumPuke; }
        }
        public double LongEntryBand
        {
            get { return m_LongEntryBand; }
        }
        public double LongFadeBand
        {
            get { return m_LongFadeBand; }
        }
        public double LongPukeBand
        {
            get { return m_LongPukeBand; }
        }
        public double ShortEntryBand
        {
            get { return m_ShortEntryBand; }
        }
        public double ShortFadeBand
        {
            get { return m_ShortFadeBand; }
        }
        public double ShortPukeBand
        {
            get { return m_ShortPukeBand; }
        }
        #endregion


        #region Constructors
        /// <summary>
        /// Empty constructor loaded by xml.
        /// </summary>
        public KeltnerTraderLogicManager()
            : base()
        {
            
        }
        //
        //
        /// <summary>
        /// Constructor using default trading variables.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="keltnerTradingVariables"></param>
        public KeltnerTraderLogicManager(string name, KeltnerTradingVariables keltnerTradingVariables)
            : base(name)
        {
            KeltnerTradingVariables = keltnerTradingVariables;
            m_EntryQty = KeltnerTradingVariables.EntryQty;
            m_FadeQty = KeltnerTradingVariables.FadeQty;
            m_EntryWidth = KeltnerTradingVariables.EntryWidth;
            m_FadeWidth = KeltnerTradingVariables.FadeWidth;
            m_PukeWidth = KeltnerTradingVariables.PukeWidth;
            m_EMALength = KeltnerTradingVariables.EMALength;
            m_ATRLength = KeltnerTradingVariables.ATRLength;
            m_MOMLength = KeltnerTradingVariables.MomentumLength;
            m_MomentumEntry = KeltnerTradingVariables.MomentumEntryValue;
            m_MomentumPuke = KeltnerTradingVariables.MomentumPukeValue;
            m_NetPosition = KeltnerTradingVariables.CurrentPos;
            SetDefaultWorkings();
        }
        #endregion


        #region Public Methods
        /// <summary>
        /// Get the flag of whether any of the entry level is allowed.
        /// </summary>
        /// <param name="tradeSide"></param>
        /// <returns></returns>
        public bool GetEntryAllowFlag(TradeSide tradeSide)
        {
            switch (tradeSide)
            {
                case TradeSide.Unknown:
                    break;
                case TradeSide.Buy:
                    foreach (TradingNode tradingNode in m_WorkingBuyNodes.Values)
                    {
                        if (tradingNode.IsEnterEnable == true)
                            return true;
                    }
                    break;
                case TradeSide.Sell:
                    foreach (TradingNode tradingNode in m_WorkingSellNodes.Values)
                    {
                        if (tradingNode.IsEnterEnable == true)
                            return true;
                    }
                    break;
            }
            return false;
        }
        //
        //
        /// <summary>
        /// Set the entry allowed flag, which means stop/permit entry at all levels.
        /// </summary>
        /// <param name="tradeSide"></param>
        /// <param name="value"></param>
        public void SetEntryAllowFlag(TradeSide tradeSide, bool value)
        {
            switch (tradeSide)
            {
                case TradeSide.Unknown:
                    break;
                case TradeSide.Buy:
                    // Set flags to all the buy levels.
                    foreach (TradingNode tradingNode in m_WorkingBuyNodes.Values)
                    {
                        tradingNode.IsEnterEnable = value;
                    }
                    break;
                case TradeSide.Sell:
                    // Set flags to all the sell levels.
                    foreach (TradingNode tradingNode in m_WorkingSellNodes.Values)
                    {
                        tradingNode.IsEnterEnable = value;
                    }
                    break;
            }
        }
        //
        //
        /// <summary>
        /// Adjust the trading variables for each level based on the correct position.
        /// </summary>
        /// <param name="correctPosition"></param>
        public void AdjustTradingNodes(int correctPosition)
        {
            SetDefaultWorkings();
            TradingNode tradingNode = null;
            var workingNodes = correctPosition > 0 ? m_WorkingBuyNodes : m_WorkingSellNodes;
            correctPosition = Math.Abs(correctPosition);
            int level = 0;

            // Allocate the working quantities for trading levels from lower level to upper level.
            while (correctPosition > 0 && level < workingNodes.Count)
            {
                tradingNode = workingNodes[level];
                if (tradingNode.TradeLevel == level)
                {
                    if (correctPosition <= m_EntryQty)
                    {
                        tradingNode.WorkingEnterQty = m_EntryQty - correctPosition;
                        tradingNode.WorkingExitQty = correctPosition;
                        correctPosition = 0;
                    }
                    else
                    {
                        tradingNode.WorkingEnterQty = 0;
                        tradingNode.WorkingExitQty = m_EntryQty;
                        correctPosition = correctPosition - m_EntryQty;
                    }
                }
                else if (tradingNode.TradeLevel == level)
                {
                    if (correctPosition <= m_FadeQty)
                    {
                        tradingNode.WorkingEnterQty = m_FadeQty - correctPosition;
                        tradingNode.WorkingExitQty = correctPosition;
                        correctPosition = 0;
                    }
                    else
                    {
                        tradingNode.WorkingEnterQty = 0;
                        tradingNode.WorkingExitQty = m_FadeQty;
                        correctPosition = correctPosition - m_FadeQty;
                    }
                }
                level++;
            }
        }
        //
        //
        /// <summary>
        /// Update the trading level prices and flags.
        /// </summary>
        /// <param name="midPrice"></param>
        /// <param name="lastEMA"></param>
        /// <param name="lastATR"></param>
        /// <param name="lastMomentum"></param>
        public void UpdateTradingNodesPricesBools(double midPrice, double lastEMA, double lastATR, double lastMomentum, ref bool[] isNeededUpdate)
        {
            // Basic assignments by the band values.
            if (lastEMA != m_LastEMA || lastATR != m_LastATR || lastMomentum != m_LastMOM)
            {
                isNeededUpdate[OrderSide.BuySide] = true;
                isNeededUpdate[OrderSide.SellSide] = true;
            }

            m_LastEMA = lastEMA;
            m_LastATR = lastATR;
            m_LastMOM = lastMomentum;
            m_LongEntryBand = lastEMA - m_EntryWidth * lastATR;
            m_LongFadeBand = lastEMA - m_FadeWidth * lastATR;
            m_LongPukeBand = lastEMA - m_PukeWidth * lastATR;
            m_ShortEntryBand = lastEMA + m_EntryWidth * lastATR;
            m_ShortFadeBand = lastEMA + m_FadeWidth * lastATR;
            m_ShortPukeBand = lastEMA + m_PukeWidth * lastATR;

            // Basic assignments for the working prices for nodes.
            m_EntryLongNode.WorkingEnterPrice = m_LongEntryBand;
            m_EntryShortNode.WorkingEnterPrice = m_ShortEntryBand;
            m_FadeLongNode.WorkingEnterPrice = m_LongFadeBand;
            m_FadeShortNode.WorkingEnterPrice = m_ShortFadeBand;
            m_EntryLongNode.WorkingStopProfitPrice = lastEMA;
            m_EntryShortNode.WorkingStopProfitPrice = lastEMA;
            m_FadeLongNode.WorkingStopProfitPrice = lastEMA;
            m_FadeShortNode.WorkingStopProfitPrice = lastEMA;

            // Check for momentum entry threshold status.
            if (lastMomentum <= -m_MomentumEntry)
            {
                if (m_EntryLongNode.IsEnterEnable == true)
                {
                    m_EntryLongNode.IsEnterEnable = false;
                    Log.NewEntry(LogLevel.Major, "The long momentum entry threshold is reached, stop long entry level.");
                }
            }
            if (lastMomentum >= m_MomentumEntry)
            {
                if (m_EntryShortNode.IsEnterEnable == true)
                {
                    m_EntryShortNode.IsEnterEnable = false;
                    Log.NewEntry(LogLevel.Major, "The short momentum entry threshold is reached, stop short entry level.");
                }
            }

            // Check for mumentum puke threshold status.
            if (lastMomentum <= -m_MomentumPuke)
            {
                m_EntryLongNode.WorkingStopLossPrice = midPrice;
                m_FadeLongNode.WorkingStopLossPrice = midPrice;
                Log.NewEntry(LogLevel.Major, "The long momentum puke threshold is reached, stop all long entry levels.");
            }
            else
            {
                m_EntryLongNode.WorkingStopLossPrice = m_LongPukeBand;
                m_FadeLongNode.WorkingStopLossPrice = m_LongPukeBand;
            }
            if (lastMomentum >= m_MomentumPuke)
            {
                m_EntryShortNode.WorkingStopLossPrice = midPrice;
                m_FadeShortNode.WorkingStopLossPrice = midPrice;
                Log.NewEntry(LogLevel.Major, "The short momentum puke threshold is reached, stop all short entry levels.");
            }
            else
            {
                m_EntryShortNode.WorkingStopLossPrice = m_ShortPukeBand;
                m_FadeShortNode.WorkingStopLossPrice = m_ShortPukeBand;
            }
        }
        //
        //
        /// <summary>
        /// Log all trading nodes information.
        /// </summary>
        public void LogTradingNodesInfo()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Current Working Nodes and trading nodes:");
            foreach (TradingNode node in m_WorkingBuyNodes.Values)
            {
                stringBuilder.AppendLine(node.GetAttributes());
            }
            foreach (TradingNode node in m_WorkingSellNodes.Values)
            {
                stringBuilder.AppendLine(node.GetAttributes());
            }
            stringBuilder.AppendLine();
            foreach (TradingNode node in m_WorkingBuyNodes.Values)
            {
                stringBuilder.AppendLine(node.GetAttributes());
            }
            foreach (TradingNode node in m_WorkingSellNodes.Values)
            {
                stringBuilder.AppendLine(node.GetAttributes());
            }
            Log.NewEntry(LogLevel.Minor, "{0}", stringBuilder.ToString());
        }
        #endregion


        #region Private Methods
        /// <summary>
        /// Basic settings for the strategy.
        /// </summary>
        private void SetDefaultWorkings()
        {
            m_EntryLongNode = new TradingNode("Enter Long Node");
            m_EntryShortNode = new TradingNode("Enter Short Node");
            m_FadeLongNode = new TradingNode("Fade Long Node");
            m_FadeShortNode = new TradingNode("Fade Short Node");

            m_EntryLongNode.WorkingEnterQty = m_EntryQty;
            m_EntryShortNode.WorkingEnterQty = m_EntryQty;
            m_FadeLongNode.WorkingEnterQty = m_FadeQty;
            m_FadeShortNode.WorkingEnterQty = m_FadeQty;

            m_EntryLongNode.IsEnterEnable = true;
            m_EntryShortNode.IsEnterEnable = true;
            m_FadeLongNode.IsEnterEnable = true;
            m_FadeShortNode.IsEnterEnable = true;

            m_EntryLongNode.TradeLevel = 0;
            m_EntryShortNode.TradeLevel = 0;
            m_FadeLongNode.TradeLevel = 1;
            m_FadeShortNode.TradeLevel = 1;

            m_EntryLongNode.TradeSide = TradeSide.Buy;
            m_EntryShortNode.TradeSide = TradeSide.Sell;
            m_FadeLongNode.TradeSide = TradeSide.Buy;
            m_FadeShortNode.TradeSide = TradeSide.Sell;

            this.TryAddTradingNode(m_EntryLongNode);
            this.TryAddTradingNode(m_EntryShortNode);
            this.TryAddTradingNode(m_FadeLongNode);
            this.TryAddTradingNode(m_FadeShortNode);
        }
        //
        //
        //protected new bool TryGetValidBuys(out double maxPrice, out int buyQty)
        //{
        //    maxPrice = double.NaN;
        //    buyQty = 0;
        //    double entryPrice1 = (m_EntryLongNode.IsEnterEnable && m_EntryLongNode.WorkingEnterQty > 0) ? m_EntryLongNode.WorkingEnterPrice : double.NaN;
        //    double entryPrice2 = (m_FadeLongNode.IsEnterEnable && m_FadeLongNode.WorkingEnterQty > 0) ? m_FadeLongNode.WorkingEnterPrice : double.NaN;
        //    double stopProfit1 = (m_EntryShortNode.WorkingStopProfitQty > 0) ? m_EntryShortNode.WorkingStopProfitPrice : double.NaN;
        //    double stopProfit2 = (m_FadeShortNode.WorkingStopProfitQty > 0) ? m_FadeShortNode.WorkingStopProfitPrice : double.NaN;

        //    if (double.IsNaN(stopProfit1) && double.IsNaN(stopProfit2))
        //    {
        //        maxPrice = entryPrice1;
        //    }
        //}
        ////
        ////
        //protected new bool TryGetValidSells(out double minPrice, out int sellQty)
        //{
            
        //}
        #endregion


        #region IStringifiable Implementation
        public override string GetAttributes()
        {
            string text = string.Format("{0} TradingStrategy={1}", base.GetAttributes(), m_TradingStrategyType);
            return text;
        }
        public override List<IStringifiable> GetElements()
        {
            List<IStringifiable> nodesList = new List<IStringifiable>();
            nodesList.AddRange(base.GetElements());
            nodesList.Add(KeltnerTradingVariables);
            return nodesList;
        }
        public override void SetAttributes(Dictionary<string, string> attributes)
        {
            base.SetAttributes(attributes);
            foreach (KeyValuePair<string, string> att in attributes)
            {
                if (att.Key.Equals("TradingStrategy"))
                    m_TradingStrategyType = att.Value;
            }
        }
        public override void AddSubElement(IStringifiable subElement)
        {
            base.AddSubElement(subElement);
            Type elementType = subElement.GetType();
            if (elementType == typeof(TradingNode))
            {
                TradingNode tradingNode = (TradingNode)subElement;
                if (tradingNode.TradeLevel == 0)
                {
                    switch (tradingNode.TradeSide)
                    {
                        case TradeSide.Unknown:
                            break;
                        case TradeSide.Buy:
                            m_EntryLongNode = tradingNode;
                            break;
                        case TradeSide.Sell:
                            m_EntryShortNode = tradingNode;
                            break;
                    }
                }
                if (tradingNode.TradeLevel == 1)
                {
                    switch (tradingNode.TradeSide)
                    {
                        case TradeSide.Unknown:
                            break;
                        case TradeSide.Buy:
                            m_FadeLongNode = tradingNode;
                            break;
                        case TradeSide.Sell:
                            m_FadeShortNode = tradingNode;
                            break;
                    }
                }
            }
            if (elementType == typeof(KeltnerTradingVariables))
            {
                KeltnerTradingVariables = (KeltnerTradingVariables)subElement;
                m_EntryQty = KeltnerTradingVariables.EntryQty;
                m_FadeQty = KeltnerTradingVariables.FadeQty;
                m_EntryWidth = KeltnerTradingVariables.EntryWidth;
                m_FadeWidth = KeltnerTradingVariables.FadeWidth;
                m_PukeWidth = KeltnerTradingVariables.PukeWidth;
                m_EMALength = KeltnerTradingVariables.EMALength;
                m_ATRLength = KeltnerTradingVariables.ATRLength;
                m_MOMLength = KeltnerTradingVariables.MomentumLength;
                m_MomentumEntry = KeltnerTradingVariables.MomentumEntryValue;
                m_MomentumPuke = KeltnerTradingVariables.MomentumPukeValue;
            }
        }
        #endregion

    }
}
