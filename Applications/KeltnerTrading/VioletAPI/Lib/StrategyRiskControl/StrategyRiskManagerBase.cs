using System;
using System.Timers;

namespace VioletAPI.Lib.StrategyRiskControl
{
    using Lib.TradingHelper;

    public class StrategyRiskManagerBase
    {

        #region Members
        protected string m_Name;

        protected int m_NetPosition = 0;
        protected int m_MaxNetPosition = 0;

        protected int m_TotalFills = 0;
        protected int m_MaxTotalFills = 0;

        protected Timer[] m_StopLossTimers;
        protected int m_StopLossTrackingTime;
        protected bool[] m_StopLossTriggered;
        protected int[] m_TotalFillStopLoss;
        #endregion


        #region Properties
        public int NetPosition
        {
            get { return m_NetPosition; }
            set
            {
                m_NetPosition = value;
            }
        }
        public int MaxNetPosition
        {
            get { return m_MaxNetPosition; }
            set
            {
                m_MaxNetPosition = value;
            }
        }
        public int MaxTotalFills
        {
            get { return m_MaxTotalFills; }
            set
            {
                m_MaxTotalFills = value;
            }
        }
        public int StopLossTrackingTime
        {
            get { return m_StopLossTrackingTime; }
            set
            {
                m_StopLossTrackingTime = value;
            }
        }
        #endregion


        #region Constructor
        public StrategyRiskManagerBase(string name)
        {
            m_Name = name;
            m_StopLossTimers = new Timer[2];
            m_StopLossTriggered = new bool[2];
            m_TotalFillStopLoss = new int[2];

            m_StopLossTimers[OrderSide.BuySide] = null;
            m_StopLossTimers[OrderSide.SellSide] = null;
            m_StopLossTriggered[OrderSide.BuySide] = false;
            m_StopLossTriggered[OrderSide.SellSide] = false;
            m_TotalFillStopLoss[OrderSide.BuySide] = 0;
            m_TotalFillStopLoss[OrderSide.SellSide] = 0;
        }
        #endregion


        #region Public Methods
        /// <summary>
        /// Detect fills risk event.
        /// </summary>
        /// <param name="qty"></param>
        /// <param name="errorInfo"></param>
        /// <returns></returns>
        public virtual bool DetectFillsRiskEvent(int qty, out string errorInfo)
        {
            bool isTriggered = false;
            errorInfo = null;
            m_NetPosition += qty;
            m_TotalFills += Math.Abs(qty);

            // Check net position limit.
            if (Math.Abs(m_NetPosition) > m_MaxNetPosition)
            {
                errorInfo = string.Format("Net position {0} breaks max net position {1} for {2}.", m_NetPosition, m_MaxNetPosition, m_Name);
                isTriggered = true;
            }

            // Check total fills limit.
            if (m_TotalFills > m_MaxTotalFills)
            {
                errorInfo = string.Format("Total position {0} breaks max total position {1} for {2}.", m_TotalFills, m_MaxTotalFills, m_Name);
                isTriggered = true;
            }

            // Check for total fills when stop loss is triggered.
            if (m_StopLossTriggered[OrderSide.BuySide] && qty < 0)
            {
                m_TotalFillStopLoss[OrderSide.BuySide] += Math.Abs(qty);
                if (m_TotalFillStopLoss[OrderSide.BuySide] > m_MaxNetPosition)
                {
                    errorInfo = string.Format("Stop loss for buy side total fills {0} breaks max limit {1} for {2}.", m_TotalFillStopLoss[OrderSide.BuySide], m_MaxNetPosition, m_Name);
                    isTriggered = true;
                }
            }
            if (m_StopLossTriggered[OrderSide.SellSide] && qty > 0)
            {
                m_TotalFillStopLoss[OrderSide.SellSide] += qty;
                if (m_TotalFillStopLoss[OrderSide.SellSide] > m_MaxNetPosition)
                {
                    errorInfo = string.Format("Stop loss for sell side total fills {0} breaks max limit {1} for {2}.", m_TotalFillStopLoss[OrderSide.SellSide], m_MaxNetPosition, m_Name);
                    isTriggered = true;
                }
            }

            return isTriggered;
        }
        //
        //
        /// <summary>
        /// Setup stop loss timers if stop orders signals are triggered.
        /// </summary>
        /// <param name="side"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public bool SetUpStopLossTimer(TradeSide side, int time)
        {
            switch (side)
            {
                case TradeSide.Unknown:
                    break;
                case TradeSide.Buy:
                    if (m_StopLossTimers[OrderSide.BuySide] == null)
                    {
                        m_StopLossTimers[OrderSide.BuySide] = new Timer(time);
                        m_StopLossTimers[OrderSide.BuySide].Elapsed += new ElapsedEventHandler(StopLossTimer_OnTick);
                    }
                    if (m_StopLossTriggered[OrderSide.BuySide] == false)
                    {
                        m_StopLossTriggered[OrderSide.BuySide] = true;
                        m_StopLossTimers[OrderSide.BuySide].Start();
                    }
                    break;
                case TradeSide.Sell:
                    if (m_StopLossTimers[OrderSide.SellSide] == null)
                    {
                        m_StopLossTimers[OrderSide.SellSide] = new Timer(time);
                        m_StopLossTimers[OrderSide.SellSide].Elapsed += new ElapsedEventHandler(StopLossTimer_OnTick);
                    }
                    if (m_StopLossTriggered[OrderSide.SellSide] == false)
                    {
                        m_StopLossTriggered[OrderSide.SellSide] = true;
                        m_StopLossTimers[OrderSide.SellSide].Start();
                    }
                    break;
            }
            return true;
        }
        //
        //
        /// <summary>
        /// Dispose the timers.
        /// </summary>
        public void Dispose()
        {
            if (m_StopLossTimers[OrderSide.BuySide] != null)
            {
                m_StopLossTimers[OrderSide.BuySide].Stop();
                m_StopLossTimers[OrderSide.BuySide].Close();
            }

            if (m_StopLossTimers[OrderSide.SellSide] != null)
            {
                m_StopLossTimers[OrderSide.SellSide].Stop();
                m_StopLossTimers[OrderSide.SellSide].Close();
            }
        }
        #endregion


        #region Private Methods
        /// <summary>
        /// Ontick if the stop loss timers are triggered.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopLossTimer_OnTick(object sender, ElapsedEventArgs e)
        {
            if (sender == m_StopLossTimers[OrderSide.BuySide])
            {
                m_TotalFillStopLoss[OrderSide.BuySide] = 0;
                m_StopLossTimers[OrderSide.BuySide].Stop();
                m_StopLossTriggered[OrderSide.BuySide] = false;
            }

            if (sender == m_StopLossTimers[OrderSide.SellSide])
            {
                m_TotalFillStopLoss[OrderSide.SellSide] = 0;
                m_StopLossTimers[OrderSide.SellSide].Stop();
                m_StopLossTriggered[OrderSide.SellSide] = false;
            }
        }
        #endregion

    }
}
