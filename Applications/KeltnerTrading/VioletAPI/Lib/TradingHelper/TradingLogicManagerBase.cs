using System;
using System.Collections.Generic;
using System.Text;

namespace VioletAPI.Lib.TradingHelper
{
    using UV.Lib.Hubs;
    using UV.Lib.IO.Xml;

    using Lib.StrategyRiskControl;

    public class TradingLogicManagerBase : IStringifiable
    {

        #region Members
        protected string m_Name = null;
        public LogHub Log = null;
        protected bool m_IsLogViewable = false;
        protected Dictionary<int, TradingNode> m_WorkingBuyNodes = null;
        protected Dictionary<int, TradingNode> m_WorkingSellNodes = null;
        protected List<TradingNode> m_CurrentBuyNodes = null;
        protected List<TradingNode> m_CurrentSellNodes = null;
        protected double m_BidPrice = double.NaN;
        protected double m_AskPrice = double.NaN;
        protected double m_MidPrice = double.NaN;
        protected int m_NetPosition = 0;
        #endregion


        #region Properties
        public double BidPrice
        {
            get { return m_BidPrice; }
        }
        public double AskPrice
        {
            get { return m_AskPrice; }
        }
        public double MidPrice
        {
            get { return m_MidPrice; }
        }
        #endregion


        #region Constructor
        public TradingLogicManagerBase()
        {
            m_WorkingBuyNodes = new Dictionary<int, TradingNode>();
            m_WorkingSellNodes = new Dictionary<int, TradingNode>();
            m_CurrentBuyNodes = new List<TradingNode>();
            m_CurrentSellNodes = new List<TradingNode>();
        }

        public TradingLogicManagerBase(string name)
        {
            m_Name = name;
            if (Log == null)
                Log = new LogHub(name, UV.Lib.Application.AppInfo.GetInstance().LogPath, true, LogLevel.Minor);
            m_WorkingBuyNodes = new Dictionary<int, TradingNode>();
            m_WorkingSellNodes = new Dictionary<int, TradingNode>();
            m_CurrentBuyNodes = new List<TradingNode>();
            m_CurrentSellNodes = new List<TradingNode>();
        }
        #endregion


        #region Core Functions
        public bool TryAddTradingNode(TradingNode tradingNode)
        {
            bool isSuccess = false;
            switch (tradingNode.TradeSide)
            {
                case TradeSide.Unknown:
                    break;
                case TradeSide.Buy:
                    if (m_WorkingBuyNodes.ContainsKey(tradingNode.TradeLevel))
                        m_WorkingBuyNodes[tradingNode.TradeLevel] = tradingNode;
                    else
                        m_WorkingBuyNodes.Add(tradingNode.TradeLevel, tradingNode);
                    isSuccess = true;
                    break;
                case TradeSide.Sell:
                    if (m_WorkingSellNodes.ContainsKey(tradingNode.TradeLevel))
                        m_WorkingSellNodes[tradingNode.TradeLevel] = tradingNode;
                    else
                        m_WorkingSellNodes.Add(tradingNode.TradeLevel, tradingNode);
                    isSuccess = true;
                    break;
            }
            return isSuccess;
        }

        public void TryGetCurrentWorkingFlagQtyPrice(double[] marketPrices, out bool[] flags, out int[] qtys, out double[] prices)
        {
            m_BidPrice = marketPrices[0];
            m_AskPrice = marketPrices[1];
            m_MidPrice = marketPrices[2];

            flags = new bool[2];
            qtys = new int[2];
            prices = new double[2];
            prices[OrderSide.BuySide] = double.NaN;
            prices[OrderSide.SellSide] = double.NaN;

            m_CurrentBuyNodes.Clear();
            m_CurrentSellNodes.Clear();

            StopOrderEventType stopOrderEventType;
            TradeSide tradeSide;
            int stopQty;
            double stopPrice = double.NaN;

            if (TryDetectStopOrderTriggers(out stopOrderEventType, out tradeSide, out stopQty, out stopPrice))
            {
                StopOrderEventArgs stopOrderEventArgs = new StopOrderEventArgs();
                stopOrderEventArgs.StopOrderEventType = stopOrderEventType;
                stopOrderEventArgs.TradeSide = tradeSide;
                stopOrderEventArgs.StopQty = stopQty;
                stopOrderEventArgs.StopPrice = stopPrice;
                OnStopOrderTriggered(stopOrderEventArgs);

                switch (tradeSide)
                {
                    case TradeSide.Unknown:
                        break;
                    case TradeSide.Buy:
                        flags[OrderSide.SellSide] = true;
                        qtys[OrderSide.SellSide] = stopQty;
                        prices[OrderSide.SellSide] = stopPrice;
                        break;
                    case TradeSide.Sell:
                        flags[OrderSide.BuySide] = true;
                        qtys[OrderSide.BuySide] = stopQty;
                        prices[OrderSide.BuySide] = stopPrice;
                        break;
                }

                if (prices[OrderSide.SellSide] <= m_BidPrice)
                    prices[OrderSide.SellSide] = m_AskPrice;
                if (prices[OrderSide.BuySide] >= m_AskPrice)
                    prices[OrderSide.BuySide] = m_BidPrice;

                Log.NewEntry(LogLevel.Major, "Stop Orders Triggered for {0} on {1} side with {2} @ {3}:", stopOrderEventType, tradeSide, stopQty, stopPrice);
                Log.NewEntry(LogLevel.Major, DumpCurrentNodesInfo());
            }
            else
            {
                double maxBuyPrice = double.NaN;
                int buyQty;
                if (TryGetValidBuys(out maxBuyPrice, out buyQty))
                {
                    flags[OrderSide.BuySide] = true;
                    qtys[OrderSide.BuySide] = buyQty;
                    prices[OrderSide.BuySide] = maxBuyPrice;
                }

                double minSellPrice = double.NaN;
                int sellQty;
                if (TryGetValidSells(out minSellPrice, out sellQty))
                {
                    flags[OrderSide.SellSide] = true;
                    qtys[OrderSide.SellSide] = sellQty;
                    prices[OrderSide.SellSide] = minSellPrice;
                }
            }
        }

        protected bool TryDetectStopOrderTriggers(out StopOrderEventType stopOrderEventType, out TradeSide tradeSide, out int stopQty, out double stopPrice)
        {
            bool isTrigger = false;
            stopOrderEventType = StopOrderEventType.Unknown;
            tradeSide = TradeSide.Unknown;
            stopQty = 0;
            stopPrice = double.NaN;

            foreach (TradingNode tradingNode in m_WorkingBuyNodes.Values)
            {
                if (m_MidPrice <= tradingNode.WorkingStopLossPrice && tradingNode.WorkingExitQty > 0)
                {
                    isTrigger = true;
                    m_CurrentSellNodes.Add(tradingNode);
                    stopOrderEventType = StopOrderEventType.StopLoss;
                    tradeSide = TradeSide.Buy;
                    stopQty += tradingNode.WorkingExitQty;
                    stopPrice = m_AskPrice;
                    tradingNode.IsEnterEnable = false;
                }
            }
            foreach (TradingNode tradingNode in m_WorkingSellNodes.Values)
            {
                if (m_MidPrice >= tradingNode.WorkingStopLossPrice && tradingNode.WorkingExitQty > 0)
                {
                    isTrigger = true;
                    m_CurrentBuyNodes.Add(tradingNode);
                    stopOrderEventType = StopOrderEventType.StopLoss;
                    tradeSide = TradeSide.Sell;
                    stopQty += tradingNode.WorkingExitQty;
                    stopPrice = m_BidPrice;
                    tradingNode.IsEnterEnable = false;
                }
            }

            foreach (TradingNode tradingNode in m_WorkingBuyNodes.Values)
            {
                if (m_MidPrice <= tradingNode.WorkingTrailingStopPrice && tradingNode.WorkingExitQty > 0)
                {
                    isTrigger = true;
                    m_CurrentSellNodes.Add(tradingNode);
                    stopOrderEventType = StopOrderEventType.TrailingStop;
                    tradeSide = TradeSide.Buy;
                    stopQty += tradingNode.WorkingExitQty;
                    stopPrice = m_AskPrice;
                    tradingNode.IsEnterEnable = false;
                }
            }
            foreach (TradingNode tradingNode in m_WorkingSellNodes.Values)
            {
                if (m_MidPrice >= tradingNode.WorkingTrailingStopPrice && tradingNode.WorkingExitQty > 0)
                {
                    isTrigger = true;
                    m_CurrentBuyNodes.Add(tradingNode);
                    stopOrderEventType = StopOrderEventType.TrailingStop;
                    tradeSide = TradeSide.Sell;
                    stopQty += tradingNode.WorkingExitQty;
                    stopPrice = m_BidPrice;
                    tradingNode.IsEnterEnable = false;
                }
            }

            foreach (TradingNode tradingNode in m_WorkingBuyNodes.Values)
            {
                if (double.IsNaN(tradingNode.WorkingStopEnterPrice))
                    continue;

                if (m_MidPrice >= tradingNode.WorkingStopEnterPrice && tradingNode.IsEnterEnable && tradingNode.WorkingEnterQty > 0)
                {
                    isTrigger = true;
                    m_CurrentBuyNodes.Add(tradingNode);
                    stopOrderEventType = StopOrderEventType.StopEnter;
                    tradeSide = TradeSide.Buy;
                    stopQty += tradingNode.WorkingEnterQty;
                    stopPrice = m_AskPrice;
                }
            }
            foreach (TradingNode tradingNode in m_WorkingSellNodes.Values)
            {
                if (double.IsNaN(tradingNode.WorkingStopEnterPrice))
                    continue;

                if (m_MidPrice <= tradingNode.WorkingStopEnterPrice && tradingNode.IsEnterEnable && tradingNode.WorkingEnterQty > 0)
                {
                    isTrigger = true;
                    m_CurrentSellNodes.Add(tradingNode);
                    stopOrderEventType = StopOrderEventType.StopEnter;
                    tradeSide = TradeSide.Sell;
                    stopQty += tradingNode.WorkingEnterQty;
                    stopPrice = m_BidPrice;
                }
            }

            return isTrigger;
        }
        #endregion


        #region Process Fill
        public void TryProcessFills(UV.Lib.Fills.Fill fill)
        {
            int filledQty = fill.Qty;
            m_NetPosition += filledQty;
            Log.NewEntry(LogLevel.Minor, "****Filled {0} @ {1} at {2}. Start Level:{3} / {4} Net Position:{5}****", filledQty, fill.Price, fill.LocalTime, m_CurrentBuyNodes.Count, m_CurrentSellNodes.Count, m_NetPosition);
            Log.NewEntry(LogLevel.Minor, "Start : {0}", DumpCurrentNodesInfo());
            TradeSide tradeSide = (filledQty > 0) ? TradeSide.Buy : TradeSide.Sell;
            int absFilledQty = Math.Abs(filledQty);
            TradingNode tradingNode = null;
            if (filledQty > 0)
            {
                int buyLevel = m_CurrentBuyNodes.Count - 1;
                while (absFilledQty > 0 && buyLevel >= 0)
                {
                    tradingNode = m_CurrentBuyNodes[buyLevel];
                    switch (tradingNode.TradeSide)
                    {
                        case TradeSide.Unknown:
                            break;
                        case TradeSide.Buy:
                            if (tradingNode.WorkingEnterQty >= absFilledQty)
                            {
                                tradingNode.WorkingExitQty += absFilledQty;
                                tradingNode.WorkingEnterQty -= absFilledQty;
                                absFilledQty = 0;
                            }
                            else
                            {
                                tradingNode.WorkingExitQty += tradingNode.WorkingEnterQty;
                                absFilledQty -= tradingNode.WorkingEnterQty;
                                tradingNode.WorkingEnterQty = 0;
                            }
                            break;
                        case TradeSide.Sell:
                            if (tradingNode.WorkingExitQty >= absFilledQty)
                            {
                                tradingNode.WorkingEnterQty += absFilledQty;
                                tradingNode.WorkingExitQty -= absFilledQty;
                                absFilledQty = 0;
                            }
                            else
                            {
                                absFilledQty -= tradingNode.WorkingExitQty;
                                tradingNode.WorkingEnterQty += tradingNode.WorkingExitQty;
                                tradingNode.WorkingExitQty = 0;
                            }
                            break;
                    }
                    buyLevel--;
                }
            }
            else
            {
                int sellLevel = m_CurrentSellNodes.Count - 1;
                while (absFilledQty > 0 && sellLevel >= 0)
                {
                    tradingNode = m_CurrentSellNodes[sellLevel];
                    switch (tradingNode.TradeSide)
                    {
                        case TradeSide.Unknown:
                            break;
                        case TradeSide.Buy:
                            if (tradingNode.WorkingExitQty >= absFilledQty)
                            {
                                tradingNode.WorkingEnterQty += absFilledQty;
                                tradingNode.WorkingExitQty -= absFilledQty;
                                absFilledQty = 0;
                            }
                            else
                            {
                                absFilledQty -= tradingNode.WorkingExitQty;
                                tradingNode.WorkingEnterQty += tradingNode.WorkingExitQty;
                                tradingNode.WorkingExitQty = 0;
                            }
                            break;
                        case TradeSide.Sell:
                            if (tradingNode.WorkingEnterQty >= absFilledQty)
                            {
                                tradingNode.WorkingExitQty += absFilledQty;
                                tradingNode.WorkingEnterQty -= absFilledQty;
                                absFilledQty = 0;
                            }
                            else
                            {
                                tradingNode.WorkingExitQty += tradingNode.WorkingEnterQty;
                                absFilledQty -= tradingNode.WorkingEnterQty;
                                tradingNode.WorkingEnterQty = 0;
                            }
                            break;
                    }
                    sellLevel--;
                }
            }
            if (absFilledQty > 0)
            {
                Log.NewEntry(LogLevel.Minor, "There is overfill at {0} @ {1}", absFilledQty, tradeSide);
                switch (tradeSide)
                {
                    case TradeSide.Unknown:
                        break;
                    case TradeSide.Buy:
                        break;
                    case TradeSide.Sell:
                        break;
                }
            }
            Log.NewEntry(LogLevel.Minor, "End : {0}", DumpCurrentNodesInfo());
        }

        protected string DumpCurrentNodesInfo()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Current Working Nodes:");
            foreach (TradingNode node in m_CurrentBuyNodes)
            {
                stringBuilder.AppendLine(node.GetAttributes());
            }
            foreach (TradingNode node in m_CurrentSellNodes)
            {
                stringBuilder.AppendLine(node.GetAttributes());
            }
            return stringBuilder.ToString();
        }
        #endregion


        #region Update Current Trading Node
        protected bool TryGetValidBuys(out double maxPrice, out int buyQty)
        {
            buyQty = 0;
            maxPrice = double.NaN;
            TradingNode node = null;

            int sellLevel = m_WorkingSellNodes.Count - 1;
            int buyLevel = 0;
            while (sellLevel >= 0 && double.IsNaN(maxPrice))
            {
                node = m_WorkingSellNodes[sellLevel];
                if (node.WorkingExitQty > 0)
                {
                    maxPrice = node.WorkingStopProfitPrice;
                    buyQty = node.WorkingExitQty;
                    m_CurrentBuyNodes.Add(node);
                }
                sellLevel--;
            }
            if (double.IsNaN(maxPrice))
            {
                while (buyLevel < m_WorkingBuyNodes.Count && double.IsNaN(maxPrice))
                {
                    node = m_WorkingBuyNodes[buyLevel];
                    if (node.IsEnterEnable && node.WorkingEnterQty > 0)
                    {
                        maxPrice = node.WorkingEnterPrice;
                        buyQty = node.WorkingEnterQty;
                        m_CurrentBuyNodes.Add(node);
                    }
                    buyLevel++;
                }
                while (buyLevel < m_WorkingBuyNodes.Count)
                {
                    node = m_WorkingBuyNodes[buyLevel];
                    if (node.IsEnterEnable && node.WorkingEnterQty > 0 && node.WorkingEnterPrice > maxPrice)
                    {
                        m_CurrentBuyNodes.Clear();
                        maxPrice = node.WorkingEnterPrice;
                        buyQty = node.WorkingEnterQty;
                        m_CurrentBuyNodes.Add(node);
                    }
                    else if (node.IsEnterEnable && node.WorkingEnterQty > 0 && node.WorkingEnterPrice == maxPrice)
                    {
                        buyQty += node.WorkingEnterQty;
                        m_CurrentBuyNodes.Add(node);
                    }
                    buyLevel++;
                }
            }
            else
            {
                while (sellLevel >= 0)
                {
                    node = m_WorkingSellNodes[sellLevel];
                    if (node.WorkingExitQty > 0 && node.WorkingStopProfitPrice > maxPrice)
                    {
                        m_CurrentBuyNodes.Clear();
                        maxPrice = node.WorkingStopProfitPrice;
                        buyQty = node.WorkingExitQty;
                        m_CurrentBuyNodes.Add(node);
                    }
                    else if (node.WorkingExitQty > 0 && node.WorkingStopProfitPrice == maxPrice)
                    {
                        buyQty += node.WorkingExitQty;
                        m_CurrentBuyNodes.Add(node);
                    }
                    sellLevel--;
                }
                while (buyLevel < m_WorkingBuyNodes.Count)
                {
                    node = m_WorkingBuyNodes[buyLevel];
                    if (node.IsEnterEnable && node.WorkingEnterQty > 0 && node.WorkingEnterPrice > maxPrice)
                    {
                        m_CurrentBuyNodes.Clear();
                        maxPrice = node.WorkingEnterPrice;
                        buyQty = node.WorkingEnterQty;
                        m_CurrentBuyNodes.Add(node);
                    }
                    else if (node.IsEnterEnable && node.WorkingEnterQty > 0 && node.WorkingEnterPrice == maxPrice)
                    {
                        buyQty += node.WorkingEnterQty;
                        m_CurrentBuyNodes.Add(node);
                    }
                    buyLevel++;
                }
            }
            return (!double.IsNaN(maxPrice) && buyQty > 0);
        }

        protected bool TryGetValidSells(out double minPrice, out int sellQty)
        {
            sellQty = 0;
            minPrice = double.NaN;
            TradingNode node = null;

            int buyLevel = m_WorkingBuyNodes.Count - 1;
            int sellLevel = 0;
            while (buyLevel >= 0 && double.IsNaN(minPrice))
            {
                node = m_WorkingBuyNodes[buyLevel];
                if (node.WorkingExitQty > 0)
                {
                    minPrice = node.WorkingStopProfitPrice;
                    sellQty = node.WorkingExitQty;
                    m_CurrentSellNodes.Add(node);
                }
                buyLevel--;
            }
            if (double.IsNaN(minPrice))
            {
                while (sellLevel < m_WorkingSellNodes.Count && double.IsNaN(minPrice))
                {
                    node = m_WorkingSellNodes[sellLevel];
                    if (node.IsEnterEnable && node.WorkingEnterQty > 0)
                    {
                        minPrice = node.WorkingEnterPrice;
                        sellQty = node.WorkingEnterQty;
                        m_CurrentSellNodes.Add(node);
                    }
                    sellLevel++;
                }
                while (sellLevel < m_WorkingSellNodes.Count)
                {
                    node = m_WorkingSellNodes[sellLevel];
                    if (node.IsEnterEnable && node.WorkingEnterQty > 0 && node.WorkingEnterPrice < minPrice)
                    {
                        m_CurrentSellNodes.Clear();
                        minPrice = node.WorkingEnterPrice;
                        sellQty = node.WorkingEnterQty;
                        m_CurrentSellNodes.Add(node);
                    }
                    else if (node.IsEnterEnable && node.WorkingEnterQty > 0 && node.WorkingEnterPrice == minPrice)
                    {
                        sellQty += node.WorkingEnterQty;
                        m_CurrentSellNodes.Add(node);
                    }
                    sellLevel++;
                }
            }
            else
            {
                while (buyLevel >= 0)
                {
                    node = m_WorkingBuyNodes[buyLevel];
                    if (node.WorkingExitQty > 0 && node.WorkingStopProfitPrice < minPrice)
                    {
                        m_CurrentSellNodes.Clear();
                        minPrice = node.WorkingStopProfitPrice;
                        sellQty = node.WorkingExitQty;
                        m_CurrentSellNodes.Add(node);
                    }
                    else if (node.WorkingExitQty > 0 && node.WorkingStopProfitPrice == minPrice)
                    {
                        sellQty += node.WorkingExitQty;
                        m_CurrentSellNodes.Add(node);
                    }
                    buyLevel--;
                }
                while (sellLevel < m_WorkingSellNodes.Count)
                {
                    node = m_WorkingSellNodes[sellLevel];
                    if (node.IsEnterEnable && node.WorkingEnterQty > 0 && node.WorkingEnterPrice < minPrice)
                    {
                        m_CurrentSellNodes.Clear();
                        minPrice = node.WorkingEnterPrice;
                        sellQty = node.WorkingEnterQty;
                        m_CurrentSellNodes.Add(node);
                    }
                    else if (node.IsEnterEnable && node.WorkingEnterQty > 0 && node.WorkingEnterPrice == minPrice)
                    {
                        sellQty += node.WorkingEnterQty;
                        m_CurrentSellNodes.Add(node);
                    }
                    sellLevel++;
                }
            }
            return (!double.IsNaN(minPrice) && sellQty > 0);
        }
        #endregion


        #region Trading Node Setup
        public bool TrySetWorkingEnterFlag(TradeSide tradeSide, int level, bool workingEnterFlag)
        {
            bool isSuccess = false;
            switch (tradeSide)
            {
                case TradeSide.Unknown:
                    break;
                case TradeSide.Buy:
                    if (m_WorkingBuyNodes.ContainsKey(level))
                    {
                        m_WorkingBuyNodes[level].IsEnterEnable = workingEnterFlag;
                        isSuccess = true;
                    }
                    break;
                case TradeSide.Sell:
                    if (m_WorkingSellNodes.ContainsKey(level))
                    {
                        m_WorkingSellNodes[level].IsEnterEnable = workingEnterFlag;
                        isSuccess = true;
                    }
                    break;
            }
            return isSuccess;
        }

        public bool TrySetWorkingEnterPrice(TradeSide tradeSide, int level, double workingEntryPrice)
        {
            bool isSuccess = false;
            switch (tradeSide)
            {
                case TradeSide.Unknown:
                    break;
                case TradeSide.Buy:
                    if (m_WorkingBuyNodes.ContainsKey(level))
                    {
                        m_WorkingBuyNodes[level].WorkingEnterPrice = workingEntryPrice;
                        isSuccess = true;
                    }
                    break;
                case TradeSide.Sell:
                    if (m_WorkingSellNodes.ContainsKey(level))
                    {
                        m_WorkingSellNodes[level].WorkingEnterPrice = workingEntryPrice;
                        isSuccess = true;
                    }
                    break;
            }
            return isSuccess;
        }

        public bool TrySetWorkingEnterQty(TradeSide tradeSide, int level, int workingQty)
        {
            bool isSuccess = false;
            switch (tradeSide)
            {
                case TradeSide.Unknown:
                    break;
                case TradeSide.Buy:
                    if (m_WorkingBuyNodes.ContainsKey(level))
                    {
                        m_WorkingBuyNodes[level].WorkingEnterQty = workingQty;
                        isSuccess = true;
                    }
                    break;
                case TradeSide.Sell:
                    if (m_WorkingSellNodes.ContainsKey(level))
                    {
                        m_WorkingSellNodes[level].WorkingEnterQty = workingQty;
                        isSuccess = true;
                    }
                    break;
            }
            return isSuccess;
        }

        public bool TrySetWorkingStopLossPrice(TradeSide tradeSide, int level, double workingStopLossPrice)
        {
            bool isSuccess = false;
            switch (tradeSide)
            {
                case TradeSide.Unknown:
                    break;
                case TradeSide.Buy:
                    if (m_WorkingBuyNodes.ContainsKey(level))
                    {
                        m_WorkingBuyNodes[level].WorkingStopLossPrice = workingStopLossPrice;
                        isSuccess = true;
                    }
                    break;
                case TradeSide.Sell:
                    if (m_WorkingSellNodes.ContainsKey(level))
                    {
                        m_WorkingSellNodes[level].WorkingStopLossPrice = workingStopLossPrice;
                        isSuccess = true;
                    }
                    break;
            }
            return isSuccess;
        }

        public bool TrySetWorkingStopProfitPrice(TradeSide tradeSide, int level, double workingStopProfitPrice)
        {
            bool isSuccess = false;
            switch (tradeSide)
            {
                case TradeSide.Unknown:
                    break;
                case TradeSide.Buy:
                    if (m_WorkingBuyNodes.ContainsKey(level))
                    {
                        m_WorkingBuyNodes[level].WorkingStopProfitPrice = workingStopProfitPrice;
                        isSuccess = true;
                    }
                    break;
                case TradeSide.Sell:
                    if (m_WorkingSellNodes.ContainsKey(level))
                    {
                        m_WorkingSellNodes[level].WorkingStopProfitPrice = workingStopProfitPrice;
                        isSuccess = true;
                    }
                    break;
            }
            return isSuccess;
        }

        public bool TrySetWorkingExitQty(TradeSide tradeSide, int level, int workingQty)
        {
            bool isSuccess = false;
            switch (tradeSide)
            {
                case TradeSide.Unknown:
                    break;
                case TradeSide.Buy:
                    if (m_WorkingBuyNodes.ContainsKey(level))
                    {
                        m_WorkingBuyNodes[level].WorkingExitQty = workingQty;
                        isSuccess = true;
                    }
                    break;
                case TradeSide.Sell:
                    if (m_WorkingSellNodes.ContainsKey(level))
                    {
                        m_WorkingSellNodes[level].WorkingExitQty = workingQty;
                        isSuccess = true;
                    }
                    break;
            }
            return isSuccess;
        }

        public bool TrySetWorkingTrailingStopPrice(TradeSide tradeSide, int level, double workingTrailingStopPrice)
        {
            bool isSuccess = false;
            switch (tradeSide)
            {
                case TradeSide.Unknown:
                    break;
                case TradeSide.Buy:
                    if (m_WorkingBuyNodes.ContainsKey(level))
                    {
                        m_WorkingBuyNodes[level].WorkingTrailingStopPrice = workingTrailingStopPrice;
                        isSuccess = true;
                    }
                    break;
                case TradeSide.Sell:
                    if (m_WorkingSellNodes.ContainsKey(level))
                    {
                        m_WorkingSellNodes[level].WorkingTrailingStopPrice = workingTrailingStopPrice;
                        isSuccess = true;
                    }
                    break;
            }
            return isSuccess;
        }
        #endregion


        #region IStringifiable
        public virtual string GetAttributes()
        {
            return string.Format("Name={0} ShowLog={1}", m_Name, m_IsLogViewable);
        }
        public virtual List<IStringifiable> GetElements()
        {
            List<IStringifiable> iStringfiableList = new List<IStringifiable>();
            foreach (TradingNode tradingNode in m_WorkingBuyNodes.Values)
            {
                iStringfiableList.Add(tradingNode);
            }
            foreach (TradingNode tradingNode in m_WorkingSellNodes.Values)
            {
                iStringfiableList.Add(tradingNode);
            }
            return iStringfiableList;
        }
        public virtual void SetAttributes(Dictionary<string, string> attributes)
        {
            foreach (KeyValuePair<string, string> att in attributes)
            {
                if (att.Key.Equals("Name"))
                    m_Name = att.Value;
                else if (att.Key.Equals("ShowLog") && att.Value.Equals("True"))
                    m_IsLogViewable = true;
            }
            if (Log == null)
                Log = new LogHub(m_Name, UV.Lib.Application.AppInfo.GetInstance().LogPath, m_IsLogViewable, LogLevel.Minor);
        }
        public virtual void AddSubElement(IStringifiable subElement)
        {
            Type elementType = subElement.GetType();
            if (elementType == typeof(TradingNode))
            {
                TradingNode tradingNode = (TradingNode)subElement;
                this.TryAddTradingNode(tradingNode);
            }
        }
        #endregion


        #region Event Trigger
        public event EventHandler StopOrderTriggered;

        protected void OnStopOrderTriggered(StopOrderEventArgs e)
        {
            if (StopOrderTriggered != null)
                StopOrderTriggered(this, e);
        }
        #endregion

    }
}
