using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Strategies.Engines
{
    using UV.Lib.Hubs;
    using UV.Lib.Utilities;
    using UV.Lib.Engines;
    using UV.Lib.IO.Xml;

    using UV.Strategies.StrategyHubs;

    public class StopBookEngine : Engine, IStringifiable
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        private string m_Name;
        private Strategy m_Parent = null;
        private LogHub Log = null;
        public bool IsLogWrite = true;				// allows us to turn off logging, good for simulated books.

        // Parameters - user set controls
        private double m_StopBase = 100.0;			// Base stop scale, always positive, in units of $$.
        private double m_Multiplier1 = 1.0;			// Externally controlled multiplier for stop.
        private double m_MultiplierUnused = 1.0;	// for future use?


        // Stop tables
        private List<double> m_StopPriceList = new List<double>();
        private Dictionary<double, int> m_StopPriceQty = new Dictionary<double, int>();
        private bool m_IsStopTriggered = false;
        private int m_StopSign = 0;                    // zero means no activated stops
        private int m_LastPosition = 0;

        // Trailing controls
        private bool m_IsTrailing = false;				// indicates whether stop prices are updated after created.		

        // Blocking
        private bool m_IsBlockingTimeOut = true;		// whether we allow unblocking because of a timeout.
        private int m_BlockingTimeOut = 120;			// 120 periods of MA updates (usually 1 second each) for blocking to be in effect.
        private int m_BlockingTimeOutRemaining = 0;
        private bool m_IsBlockingResetOnCrossing = true;	// unblocking caused by FairValue cross over.


        // non saved work space.
        private List<double> m_PriceToDelete = new List<double>();	// reused workspace!
        private List<double> m_PriceToAdd = new List<double>();	// reused workspace!

        //
        #endregion// members


        #region Constructors and Setup
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public StopBookEngine() : base()
        {

        }
        //   
        //
        // ****             Setup Begin()               ****
        //
        public override void SetupBegin(IEngineHub myEngineHub, IEngineContainer engineContainer)
        {
            base.SetupBegin(myEngineHub,engineContainer);
            m_Name = base.EngineName;
            m_Parent = (Strategy)engineContainer;
            this.Log = ((StrategyHub)myEngineHub).Log;

        }//SetupBegin()
        //
        //
        //
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        public bool IsStopped { get { return m_IsStopTriggered; } }
        /// <summary>
        /// Postive value of the stop level.
        /// </summary>
        public double StopBase
        {
            get { return m_StopBase; }
            set { m_StopBase = value; }
        }
        public double Multiplier1
        {
            get { return m_Multiplier1; }
            set
            {
                m_Multiplier1 = value;
                this.BroadcastParameters();	// this can be changed internally as well.
            }
        }
        //	
        public int Position
        {
            get { return m_LastPosition; }
        }
        //
        // Trailing controls
        //
        public bool IsTrailing
        {
            get { return m_IsTrailing; }
            set { m_IsTrailing = value; }
        }
        /// <summary>
        /// Gets the price most likely to stop or returns a NaN if no current position.
        /// </summary>
        public double NearestStopPrice
        {
            get
            {
                if (m_LastPosition == 0) return double.NaN;
                int level = (m_StopPriceList.Count - 1) * (Math.Sign(m_LastPosition) + 1) / 2;
                double stopPrice = m_StopPriceList[level];		// closest stop price.
                return stopPrice;
            }
        }
        //
        //
        //
        public int NearestStopQty
        {
            get
            {
                if (m_LastPosition == 0) return 0;
                int level = (m_StopPriceList.Count - 1) * (Math.Sign(m_LastPosition) + 1) / 2;
                double stopPrice = m_StopPriceList[level];		// closest stop price.
                return m_StopPriceQty[stopPrice];
            }
        }
        //
        // ***			Blocking			***
        //
        public bool IsBlockingTimeOutAllowed
        {
            get { return m_IsBlockingTimeOut; }
            set { m_IsBlockingTimeOut = value; }
        }
        public int BlockingTimeOut
        {
            get { return m_BlockingTimeOut; }
            set { m_BlockingTimeOut = value; }
        }
        public bool IsBlockingResetOnCrossing
        {
            get { return m_IsBlockingResetOnCrossing; }
            set { m_IsBlockingResetOnCrossing = value; }

        }
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
        // ****					Is Entry Block()				****
        /// <summary>
        /// Detects whether new trades (of type tradeSign=buy/sell) are blocked.
        /// A trade can be blocked when an earlier trade (on the same side of the market)
        /// has triggered a stop rule.  The blocking feature will clear itself under
        /// certain conditions, and normal trading can commense.
        /// </summary>
        /// <param name="tradeSign">Sign of trade we want to do: buy/sell = +1/-1.</param>
        /// <returns>True if such a trade is currently blocked.</returns>
        public bool IsEntrySignBlocked(int tradeSign)
        {
            return (tradeSign * m_StopSign > 0);	// m_StopSign = sign of side that had recent stop.
        }
        public bool IsEntrySideBlocked(int tradeSide)
        {
            int tradeSign = QTMath.MktSideToMktSign(tradeSide);
            return (tradeSign * m_StopSign > 0);	// m_StopSign = sign of side that had recent stop.
        }
        //
        //
        //
        // ****					Check New Stop Violation 				****
        //
        /// <summary>
        /// New version: July 2012.
        /// </summary>
        /// <param name="mktPrice">Price used to test whether stop is triggered.</param>
        /// <returns>true if a NEW stop level has been violated just now.</returns>		
        public bool CheckNewStopViolation(double mktPrice)
        {
            if (m_LastPosition == 0) { return false; }			// exit immediately
            if (m_IsStopTriggered) { return false; }			// we are already working to exit a stop condition.
            int posSign = Math.Sign(m_LastPosition);

            // Update trailing stops
            if (m_IsTrailing)
                UpdateTrailingStops(mktPrice);

            // Get riskiest stop price and qty.
            // The StopPriceList[] is sorted each time there is a new fill.  So, the fills with 
            // smallest price are listed first (these would be the most risky for short fills).
            // Riskiest short-> level=0, riskiest long-> level=top of list.
            int level = (m_StopPriceList.Count - 1) * (posSign + 1) / 2;
            if (level < 0)	// level is the "most at risk stop level" this should never happen.		
            {
                if (IsLogWrite) Log.NewEntry(LogLevel.Error, "StopBookEngine.CheckStopViolation: Error level is less than zero!");
                return false;
            }
            double stopPrice = m_StopPriceList[level];		// closest stop price.

            // Test stopping condition now.
            if ((stopPrice - mktPrice) * m_LastPosition > 0)
            {   // Stop just now penetrated!
                m_IsStopTriggered = true;
                m_StopSign = Math.Sign(m_LastPosition);	// If the short-side stops out, then StopSign < 0.

                if (m_IsBlockingTimeOut)
                    m_BlockingTimeOutRemaining = m_BlockingTimeOut; // set blocking timeout.

                if (IsLogWrite)
                {
                    Log.NewEntry(LogLevel.Major,
                   "StopBookEngine: {0} stop of [{1} @ {2}] penetrated at price={3} pos={4}."
                   , m_Name, m_StopPriceQty[stopPrice].ToString(), stopPrice.ToString()
                   , mktPrice.ToString(), m_LastPosition.ToString()
                   );
                }

                this.IsUpdateRequired = true;
                BroadcastParameters();
                return true;
            }
            // Exit - unstopped.
            return false;
        }//IsStopTriggered()
        //
        // ****					Is Stop Triggered()				****
        //
        /// <summary>
        /// Note: The one thing that is strange about this method, and perhaps could be
        /// improved is that only ONE level is allowed to be stopped out at any given time.
        /// </summary>
        /// <param name="mktPrice">Price used to test whether stop is triggered.</param>
        /// <param name="tradeSign">trade we would want to do: +1 buying, -1 selling.  Stops
        ///		can be triggered only when tradeSign is *opposite side* of the current position.</param>
        /// <param name="stopQty">signed quantity to dump (positive means we want to exit a long position, etc).</param>
        /// <returns></returns>		
        public bool IsStopTriggered(double mktPrice, int tradeSign, out int stopQty)
        {
            stopQty = 0;						// The stopped-Qty to be exited immediately.
            // Validate tradeSign.
            if (m_LastPosition * tradeSign >= 0) { return false; }	// can only be stopped for positions on this side.
            int posSign = Math.Sign(m_LastPosition);

            // Update trailing stops
            if (m_IsTrailing)
                UpdateTrailingStops(mktPrice);

            // Get riskiest stop price and qty.
            // The StopPriceList[] is sorted each time there is a new fill.  So, the fills with 
            // smallest price are listed first (these would be the most risky for short fills).
            // Riskiest short-> level=0, riskiest long-> level=top of list.
            int level = (m_StopPriceList.Count - 1) * (posSign + 1) / 2;
            if (level < 0)// level is the "most at risk stop level" this should never happen.		
            {
                if (IsLogWrite) Log.NewEntry(LogLevel.Error, "IsStopTriggered. Error level is less than zero!");
                return false;
            }
            double stopPrice = m_StopPriceList[level];		// closest stop price.
            stopQty = m_StopPriceQty[stopPrice];			// associated stop qty.
            if (m_IsStopTriggered)							// Check whether we are already stopped!	
                return true;	// already stopped out!				


            // Test stopping condition now.
            if ((stopPrice - mktPrice) * m_LastPosition > 0)
            {   // Stop just now penetrated!
                m_IsStopTriggered = true;
                m_StopSign = Math.Sign(m_LastPosition);	// If the short-side stops out, then StopSign < 0.

                if (m_IsBlockingTimeOut)
                    m_BlockingTimeOutRemaining = m_BlockingTimeOut; // set blocking timeout.

                if (IsLogWrite) Log.NewEntry(LogLevel.Major,
                   "StopRule: {0} stop of [{2} @ {3}] penetrated at price={4} pos={5}."
                   , m_Name, QTMath.MktSignToString(tradeSign), stopQty.ToString()
                   , stopPrice.ToString(), mktPrice.ToString(), m_LastPosition.ToString()
                   );

                this.IsUpdateRequired = true;
                BroadcastParameters();
                return true;
            }
            // Exit - unstopped.
            return false;
        }//IsStopTriggered()
        //
        //
        //
        /// <summary>
        /// This must be called periodically to update things like volatility, blocking timeouts, 
        /// etc.
        /// </summary>
        /// <param name="excessValue">The strategy excess value: (P - FairValue)</param>
        public void Update(double excessValue)
        {
            if (m_StopSign != 0)
            {	// We are stopped.				
                //
                // Check blocking timeout.
                //
                if (m_IsBlockingTimeOut)
                {
                    m_BlockingTimeOutRemaining -= 1;		// decrease countdown to timeout.
                    if (m_BlockingTimeOutRemaining <= 0)
                    {
                        if (IsLogWrite) Log.NewEntry(LogLevel.Major, "StopRule: Blocking timed out. StopLoss for {1} returning to normal trading on {0} side."
                                                , QTMath.MktSignToString(m_StopSign), m_Name);
                        m_StopSign = 0;    // return to normal trading.	
                        this.IsUpdateRequired = true;
                        BroadcastParameters();
                    }
                }
                //
                // Crossing fair value can also reset blocking.
                //
                // Example: Consider the price trending is upward and that we stopped out on a short
                // position, so m_StopSign = -1.  As the we continue to trend, the expectedReturn < 0.
                if (m_IsBlockingResetOnCrossing)
                {
                    if (excessValue * m_StopSign > 0)	// occurs when we cross to other side of fair value.
                    {
                        if (IsLogWrite) Log.NewEntry(LogLevel.Major, "StopRule: Blocking halted because fair value crosssed. StopLoss for {1} returning to normal trading on {0} side."
                        , QTMath.MktSignToString(m_StopSign), m_Name);
                        m_StopSign = 0;    // return to normal trading.	
                        this.IsUpdateRequired = true;
                        BroadcastParameters();
                    }
                }


            }// if stopped.

        }// Update().
        //
        //
        //
        //
        //
        // ****                 Update Stop Levels()                ****
        //
        public void NewFill(UV.Lib.Fills.Fill aFill)
        {
            UpdateNewFill(aFill.Qty, aFill.Price, m_LastPosition);
            m_LastPosition += aFill.Qty;
            this.IsUpdateRequired = true;
            BroadcastParameters();
        }//UpdateStopLevels
        //
        //
        //
        public override string ToString()
        {
            StringBuilder msg = new StringBuilder();
            if (IsStopped) msg.AppendFormat("Stopped ");
            if (m_StopSign > 0)
                msg.Append("Buy-Block ");
            else if (m_StopSign < 0)
                msg.Append("Sell-Block ");
            if (m_StopSign != 0 && m_IsBlockingTimeOut)
            {
                int mins = (int)Math.Floor(m_BlockingTimeOutRemaining / 60.0);
                int secs = m_BlockingTimeOutRemaining - 60 * mins;
                msg.AppendFormat("timeout in {0} mins and {1} secs.", mins.ToString(), secs.ToString());
            }
            for (int i = 0; i < m_StopPriceList.Count; ++i)
            {
                double p = m_StopPriceList[i];
                msg.AppendFormat("[{1} @ {0}]", p.ToString("0.0##"), m_StopPriceQty[p].ToString());
            }
            return msg.ToString();
        }
        //
        #endregion//Public Methods



        #region Private Methods
        // *****************************************************************
        // ****						Private Methods						****
        // *****************************************************************
        //
        //
        //
        //
        // ****                 Update Stop Levels()                ****
        //
        /// <summary>
        /// This updates the list of stop-levels each time there is a fill.
        /// </summary>
        /// <param name="id">id of worker that is filled.</param>
        /// <param name="signedQty"></param>
        /// <param name="price"></param>
        private void UpdateNewFill(int signedQty, double fillPrice, int preFillPosition)
        {
            m_IsStopTriggered = false;          // reset this
            if (preFillPosition * signedQty >= 0)
            {   // New fill is adding to our current position (or no previous position).
                // Calculate new stopping level associated with this fill.
                double pStop = fillPrice - Math.Sign(signedQty) * m_StopBase * m_Multiplier1 * m_MultiplierUnused;
                if (m_StopPriceList.Contains(pStop))// stop book already contains this price level.
                    m_StopPriceQty[pStop] += signedQty;
                else
                {
                    m_StopPriceQty.Add(pStop, signedQty);
                    m_StopPriceList.Add(pStop);
                }
                m_StopPriceList.Sort();             // keep this list sorted.
            }
            else
            {   // New fill is decreasing our position (or we've completely flipped to other side).
                // In this case, we know that signedQty and quantities in stop list have opposite signs.
                // Remove stops starting at the closest:
                // That means, for long position, we are interested in LARGEST stop price levels, etc.
                int qtyToRemove = -signedQty;          // if we sold -5, then qtyToRemove is +5, same sign as position!
                int level = (m_StopPriceList.Count - 1) * (Math.Sign(qtyToRemove) + 1) / 2;
                m_PriceToDelete.Clear();
                while (qtyToRemove != 0 && level >= 0 && level < m_StopPriceList.Count)
                {
                    double price = m_StopPriceList[level];
                    int qty = m_StopPriceQty[price];
                    int qtyMin = Math.Sign(qty) * Math.Min(Math.Abs(qty), Math.Abs(qtyToRemove));
                    qty -= qtyMin;                      // remove qty from list
                    qtyToRemove -= qtyMin;              // remove for outstanding qty to remove.

                    if (qty == 0)
                        m_PriceToDelete.Add(price);   // remove zero qty entries, remove from StopPriceList outside loop!
                    else
                        m_StopPriceQty[price] = qty;
                    // change the counter
                    level += Math.Sign(signedQty);      // pos long --> decrease level counter, etc.
                }//while
                // Clean out any zero-qty levels.
                foreach (double price in m_PriceToDelete)
                {
                    m_StopPriceQty.Remove(price);
                    m_StopPriceList.Remove(price);
                }

                // Check whether there is still qty.  If so, we've flipped sides.
                if (qtyToRemove != 0)
                {
                    if (IsLogWrite) Log.NewEntry(LogLevel.Major, "StopRule.UpdateStopLevels: {2} has fill qty {0} @ {1} remaining, flipped our position.",
                        qtyToRemove.ToString(), fillPrice.ToString(), m_Name);
                    UpdateNewFill(-qtyToRemove, fillPrice, 0);   // Create new stop on other side of market.
                }
            }//if block
            //
            // Log
            if (IsLogWrite && Log.BeginEntry(LogLevel.Major))
            {
                Log.AppendEntry("StopRule.UpdateStopLevels: {1} {0} ", m_Name, m_Parent.Name);
                Log.AppendEntry("prev pos = {1}, new fills={2} @ {0} "
                    , fillPrice.ToString(), preFillPosition.ToString(), signedQty.ToString());
                Log.AppendEntry("StopInit={0} StopMulti={1} Stops: ", m_StopBase.ToString(), m_Multiplier1.ToString());
                if (m_StopPriceList.Count > 0)
                {
                    for (int i = 0; i < m_StopPriceList.Count; ++i)
                    {
                        double p = m_StopPriceList[i];
                        Log.AppendEntry("[{1} @ {0}] ", p.ToString(), m_StopPriceQty[p].ToString());
                    }
                }
                else
                    Log.AppendEntry("[]. ");
                Log.EndEntry();
            }
        }//UpdateNewFill()
        //
        //
        //
        //
        private void UpdateTrailingStops(double mktPrice)
        {
            int posSign = Math.Sign(m_LastPosition);
            m_PriceToDelete.Clear();									// place to store old stop price.
            m_PriceToAdd.Clear();										// place to store new stop price.
            // Check state of each stop price.
            for (int i = 0; i < m_StopPriceList.Count; ++i)				// loop thru each stop-price in list.
            {
                // Compute the new stop price for this level.
                double S = (m_StopBase * m_Multiplier1 * m_MultiplierUnused);
                double newStopPrice = mktPrice - posSign * S;			// new stop price for this level.
                if ((newStopPrice - m_StopPriceList[i]) * posSign > 0)	// Test whether stop price needs updating.
                {
                    m_PriceToDelete.Add(m_StopPriceList[i]);			// if so, mark it for updating.
                    m_PriceToAdd.Add(newStopPrice);
                }
            }//next stop level i
            // Update our stop lists.
            for (int i = 0; i < m_PriceToAdd.Count; ++i)
            {
                double newStopPrice = m_PriceToAdd[i];
                if (!m_StopPriceList.Contains(newStopPrice))
                {
                    m_StopPriceList.Add(newStopPrice);
                    m_StopPriceQty.Add(newStopPrice, 0);			// put a zero qty space holder.
                }
                double oldStopPrice = m_PriceToDelete[i];			// original price to remove now.
                m_StopPriceList.Remove(oldStopPrice);
                m_StopPriceQty[newStopPrice] += m_StopPriceQty[oldStopPrice];
                m_StopPriceQty.Remove(oldStopPrice);
            }
            //this.IsUpdateRequired = true;					
        }// UpdateTrailingStops()
        //
        //
        //
        //
        //
        private void BroadcastParameters()
        {
            if (this.IsUpdateRequired)
            {
                base.BroadcastAllParameters(m_Parent.StrategyHub, m_Parent);
                /*
                EngineEventArgs eArgs = new EngineEventArgs();
                eArgs.MsgType = EngineEventArgs.EventType.ParameterValue;
                eArgs.Status = EngineEventArgs.EventStatus.Confirm;
                eArgs.EngineHubResponding = m_Parent.m_StrategyHub;
                eArgs.EngineContainerID = m_Parent.EngineContainerID;
                eArgs.EngineID = this.EngineID;
                if (GetParameterValue(ref eArgs.DataIntA, ref eArgs.DataObjectList))
                {
                    base.m_EventQueue.Add(eArgs);
                }
                */
            }
        }// UpdateParameters()
        // 		
        //
        //
        //
        //
        //
        #endregion//private methods


        #region IStringify 
        string IStringifiable.GetAttributes()
        {
            return string.Empty;
        }
        List<IStringifiable> IStringifiable.GetElements()
        {
            return null;
        }
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
            foreach (KeyValuePair<string,string> keyValPair in attributes)
            {
                string valStr = keyValPair.Value;
                double x;
                switch(keyValPair.Key)
                {
                    case "StopBase":
                        x = Convert.ToDouble(valStr);
                        if (x > 0)
                            m_StopBase = x;
                        //else
                        //    m_StopBase = m_Cu
                        break;
                    case "Multiplier1":
                        this.Multiplier1 = Convert.ToDouble(valStr);
                        break;
                    case "BlockingTimeout":
                        this.BlockingTimeOut = Convert.ToInt32(valStr);
                        this.IsBlockingTimeOutAllowed = true;
                        break;
                    case "IsBlockingResetOnCrossing":
                        this.IsBlockingResetOnCrossing = Convert.ToBoolean(valStr);
                        break;
                    default:
                        break;
                }

            }
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {
        }
        #endregion // IStringify

    }
}
