using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Strategies.ExecutionEngines.OrderEngines.TermStructures
{
    using UV.Strategies.StrategyEngines;
    using UV.Lib.MarketHubs;
    using UV.Lib.Products;
    using UV.Lib.IO.Xml;
    using UV.Lib.Engines;
    using UV.Lib.Fills;
    using UV.Lib.Utilities;
    using UV.Strategies.ExecutionHubs;
    using UV.Strategies.ExecutionEngines.Scratchers;

    using UV.Strategies.ExecutionHubs.ExecutionContainers;
    //
    /// <summary>
    /// All parameters to define a leg of the curve (including its "scratch leg") and its market
    /// </summary>
    public class CurveLeg : Engine, IStringifiable, ITimerSubscriber
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        public PriceLeg m_PriceLeg;
        public UV.Lib.BookHubs.Market m_Market;
        public Scratchers.Scratcher m_Scratcher;
        private MultiThreadContainer m_MultiThreadContainer;
        private CurveTrader m_CurveTrader;
        //
        // Instrument Details
        //
        public InstrumentDetails InstrumentDetails;                 // store all details about this instrument.
        public string DefaultAccount;

        //
        // Lean Params - for quotes leaning on this instrument
        //
        private int m_BaseVolumeLean = 0;
        private double m_OffsetVolumeMultiplier = 1.0;
        private int m_NeededLeanQty;

        //
        // Quote Params - for quote orders in this instrument
        //
        private bool m_UserDefinedQuotingEnabled;
        public bool[] m_QuotingEnabled = new bool[2];               //  quoting bool for this leg and each side

        private int m_ThresholdJoin = 10;
        private int m_ThresholdPull = 8;
        private int m_ThresholdSqueeze = 2;
        private int m_RejoinDelayPostPullSeconds = 1;               // after an order is pulled this is the number of seconds we have to wait to consider rejoining.
        private int[] m_RejoinDelayCountBySide = new int[2];        // count that is incremented upon us pulling an order
        // NOTE: Currently m_RejoinDelayPostPullSeconds is a very rough delay.  It is not needed to be very precise or accurate, so for this reason
        // the current design of using itimersubscriber is sufficient.  We will certainly get varied delay times if we use 1 sec delay, these delays
        // could range from near immediate to the full second depending on the calls from Itimer.
        //
        // State Flags
        //
        public bool IsMarketGood;
        public bool[] m_IsMarketLeanable;                       // does sufficient qty exist on either side of the market to lean on            
        public bool[] m_IsInsideMarketAboveThresholdJoin;       // does the inside market on either side have great enough qty to Join with new orders
        public bool[] m_IsInsideMarketAboveThresholdPull;       // does the inside market on either side have great enough qty to not pull our orders
        public bool[] m_IsInsideMarketAboveThresholdSqueeze;    // does the inside market on either side have great enough qty to Squeeze

        public bool[] m_LeanablePriceChanged = new bool[2];     // on the last update our leanable price changed, by side[]
        public bool[] m_ThresholdJoinCrossed = new bool[2];     // on the last update our join threshold was crossed, by side[]
        public bool[] m_ThresholdPullCrossed = new bool[2];     // on the last update our pull threshold was crossed, by side[]
        public bool[] m_ThresholdSqueezeCrossed = new bool[2];  // on the last update our sqeeze threshold was crossed by side[]

        //
        // Market variables
        //
        public double[] m_LeanablePrices;                       // current mkt price with enough leanable qty: m_LeanablePrices[mktSide]
        private int[] m_LeanableDepth;                          // current depth with enough leanable qty  

        #endregion// members

        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// Create an instance of a Spreader Leg which contains details on a leg of synthetic spread
        /// and variables regarding execution.
        /// </summary>
        public CurveLeg()
        {
            // Initialize our internal market variables.
            m_LeanablePrices = new double[2];                                   // bidPrice, askPrice after cleaning
            m_LeanableDepth = new int[2];
            m_IsMarketLeanable = new bool[2];

            m_IsInsideMarketAboveThresholdJoin = new bool[2];
            m_IsInsideMarketAboveThresholdPull = new bool[2];
            m_IsInsideMarketAboveThresholdSqueeze = new bool[2];

            for (int mktSide = 0; mktSide < 2; ++mktSide)
            {
                m_LeanablePrices[mktSide] = double.NaN;
                m_LeanableDepth[mktSide] = 10; // means we care about all price updates to start

                // initialize all Inside Market States to state which will result in no action
                m_IsInsideMarketAboveThresholdJoin[mktSide] = false;
                m_IsInsideMarketAboveThresholdPull[mktSide] = true;
                m_IsInsideMarketAboveThresholdSqueeze[mktSide] = true;
            }//mktSide
        }
        //
        //
        protected override void SetupInitialize(IEngineHub myEngineHub, IEngineContainer engineContainer, int engineID, bool setupGui)
        {
            if (typeof(UV.Strategies.ExecutionHubs.ExecutionContainers.MultiThreadContainer).IsAssignableFrom(engineContainer.GetType()))
            {   // this is the "first" set up call from the manager container.
                base.SetupInitialize(myEngineHub, engineContainer, engineID, setupGui);
                m_MultiThreadContainer = (MultiThreadContainer)engineContainer;
                m_MultiThreadContainer.TryAddEngine(m_Scratcher);       // we need to add our sub engines to the container, just to allow set up and messaging to correctly function
            }
            else
            {   // this is the second set up call from the correct container, add correct sub engine mappings 
                ThreadContainer execContainer = (ThreadContainer)engineContainer;
                execContainer.TryAddEngine(m_Scratcher);

                if (execContainer.IOrderEngine is CurveTrader)
                    m_CurveTrader = (CurveTrader)execContainer.IOrderEngine;

                m_MultiThreadContainer.TryAddEngineIdToManagingContainer(execContainer, m_Scratcher.EngineID);
            }
        }
        //
        //
        public override void SetupBegin(IEngineHub myEngineHub, IEngineContainer engineContainer)
        {
            base.SetupBegin(myEngineHub, engineContainer);

            if (myEngineHub is ExecutionHub)
            { // set up ITimer to work
                ThreadContainer execContainer = (ThreadContainer)engineContainer;
                ((ExecutionHub)myEngineHub).SubscribeToTimer((ITimerSubscriber)execContainer.m_ExecutionListener);
                execContainer.m_ExecutionListener.SubscribeToTimer(this); // subscribe to updates
            }
        }
        //
        //
        #endregion//Constructors

        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// qty on a price that must be present before we consider leaning.
        /// </summary>
        public int BaseVolumeLean
        {
            get { return m_BaseVolumeLean; }
            set
            {
                if (value != m_BaseVolumeLean)
                {
                    m_BaseVolumeLean = value;
                    OnParameterChanged();
                }
            }
        }
        /// <summary>
        /// This value times our leg weight, plus the BaseVolumeLean must be present for us to lean a price
        /// </summary>
        public double OffsetVolumeMultiplier
        {
            get { return m_OffsetVolumeMultiplier; }
            set
            {
                if (m_OffsetVolumeMultiplier != value)
                {
                    m_OffsetVolumeMultiplier = value;
                    OnParameterChanged();
                }
            }
        }
        /// <summary>
        /// Threshold over which we will join a level
        /// </summary>
        public int JoinThreshold
        {
            get { return m_ThresholdJoin; }
            set
            {
                int oldValue = m_ThresholdJoin;
                m_ThresholdJoin = value;
                if (m_CurveTrader.m_RiskManager.IsLegThresholdsValid(this))
                {   // our new param is valid, let people know about the change
                    OnParameterChanged();
                }
                else
                {   // this change would put us in a bad state, set it back to old value!
                    m_ThresholdJoin = oldValue;
                }
            }
        }
        /// <summary>
        /// Threshold under which we will pull existing quote orders
        /// </summary>
        public int PullThreshold
        {
            get { return m_ThresholdPull; }
            set
            {
                int oldValue = m_ThresholdPull;
                m_ThresholdPull = value;
                if (m_CurveTrader.m_RiskManager.IsLegThresholdsValid(this))
                {   // our new param is valid, let people know about the change
                    OnParameterChanged();
                }
                else
                {   // this change would put us in a bad state, set it back to old value!
                    m_ThresholdPull = oldValue;
                }
            }
        }
        /// <summary>
        /// Threshold over which we will attempt to squeeze levels (opposite side)
        /// </summary>
        public int SqueezeThreshold
        {
            get { return m_ThresholdSqueeze; }
            set
            {
                int oldValue = m_ThresholdSqueeze;
                m_ThresholdSqueeze = value;
                if (m_CurveTrader.m_RiskManager.IsLegThresholdsValid(this))
                {   // our new param is valid, let people know about the change
                    OnParameterChanged();
                }
                else
                {   // this change would put us in a bad state, set it back to old value!
                    m_ThresholdSqueeze = oldValue;
                }
            }
        }
        /// <summary>
        /// Should we actively quote this spread
        /// </summary>
        public bool UserDefinedQuotingEnabled
        {
            get { return m_UserDefinedQuotingEnabled; }
            set
            {
                if (m_UserDefinedQuotingEnabled != value)
                {
                    m_UserDefinedQuotingEnabled = value;
                    OnParameterChanged();
                }
            }
        }
        //
        //
        #endregion//Properties

        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        // *********************************************************
        // ****             TrySetInstrumentDetails()           ****
        // *********************************************************
        //
        /// <summary>
        /// Called to set the instrument details for the first time. This also
        /// will set up the variables which rely upon this information.
        /// This is a method and not a property only to hide it from the GUI.
        /// </summary>
        /// <param name="instrDetails"></param>
        /// <returns></returns>
        public bool TrySetInstrumentDetails(InstrumentDetails instrDetails)
        {
            if (InstrumentDetails != null)
                return false;
            InstrumentDetails = instrDetails;
            return true;
        }
        //
        //
        // *********************************************************
        // ****                 Market_MarketChanged            ****
        // *********************************************************
        public void Market_MarketChanged(object sender, EventArgs eventArgs)
        {
            bool wasMarketGood = IsMarketGood;
            IsMarketGood = m_Market.IsMarketGood;
            if (!m_Market.IsMarketGood)
                return;
            if (IsMarketGood != wasMarketGood)
            {   // Market state has changed!  

                //
                // Update all of our internal states
                //
                UpdateLeanablePrices();
                UpdateThresholdStates();

                //
                // Force updates to our subscribers - not sure if all of these events need to be called currently
                //
                OnSqueezeStateChanged();
                OnLeanablePriceChanged();                                   // trigger events for our subscribers
                OnPullJoinStateChanged();

                OnPriceChanged();
                OnMarketStateChanged();
            }
            else if (IsMarketGood)
            {   // Market state hasn't changed - and is good!

                bool isLeanableStatusChange;
                bool isLeanablePriceChanged = UpdateLeanablePrices(out isLeanableStatusChange);

                bool isJoinStatusChanged;
                bool isPullStatusChanged;
                bool isSqueezeStatusChanged;
                UpdateThresholdStates(out isJoinStatusChanged, out isPullStatusChanged, out isSqueezeStatusChanged);

                //
                // Call events based on changes, order matters! Pull -> Scratch -> Lean -> Update best leans -> new quotes and joining
                //

                if (isLeanableStatusChange)                                     // this has to be called first to set the bools correctly for quoting.
                    OnMarketStateChanged();
                if (isJoinStatusChanged | m_Market.IsLastTickBestPriceChange | isPullStatusChanged)
                    OnPullJoinStateChanged();
                m_Scratcher.Market_MarketChanged(sender, eventArgs);    // scratchers doesn't have direct subscription to control sequencing
                if (isSqueezeStatusChanged)
                    OnSqueezeStateChanged();
                if (isLeanablePriceChanged)                                     // if LeanablePrices changed, then 
                    OnLeanablePriceChanged();                                   // trigger events for our subscribers
            }
        }
        //
        //
        // *********************************************************
        // ****                 ToString()                     *****
        // ********************************************************* 
        public override string ToString()
        {
            System.Text.StringBuilder s = new System.Text.StringBuilder(64);
            s.AppendFormat("{0}", this.m_PriceLeg.InstrumentName);
            return s.ToString();
        }
        //
        //
        //
        /*
        /// <summary>
        /// Called by the a Quoter to let the instrument know the needed lean qty of his leg.
        /// </summary>
        /// <param name="dripQty"></param>
        public void UpdateDripQty(int dripQty)
        {
            m_DripQty = dripQty;
            m_NeededLeanQty = (int)((m_OffsetVolumeMultiplier * Math.Abs(m_PriceLeg.Weight) * dripQty) + m_BaseVolumeLean);
            if (m_Market != null)
                Market_MarketChanged(this, EventArgs.Empty); // trigger fake price change to force updates since our leanable prices probably changed
        }
        */
        #endregion // Public Methods

        #region Private methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        // *********************************************************
        // ****             Update Leanable Prices()            ****
        // *********************************************************
        /// <summary>
        /// Computes m_LeanablePrices[].
        /// Called whenerver m_Prices[][], m_Qtys[][] have changed, or when leg parameters
        /// have been changed by user.
        /// </summary>
        /// <returns>true if either m_LeanablePrices[] values have changed and wherther a leg
        /// has chagned it's leanable state</returns>
        private bool UpdateLeanablePrices(out bool isLeanableStatusChange)
        {
            isLeanableStatusChange = false;
            if (!IsMarketGood)
            {   // market is in a bad state, assign everything to NaN 
                m_LeanablePrices[UV.Lib.Utilities.QTMath.BidSide] = double.NaN;                 // is this overkill?  Need to decide on protocol to handle bad mkts.
                m_LeanablePrices[UV.Lib.Utilities.QTMath.AskSide] = double.NaN;
                m_LeanablePriceChanged[UV.Lib.Utilities.QTMath.BidSide] = true;
                m_LeanablePriceChanged[UV.Lib.Utilities.QTMath.AskSide] = true;
                isLeanableStatusChange = true;
                return true;// leanable prices have changed!
            }

            bool isLeanablePriceChanged = false;
            double tick = this.InstrumentDetails.TickSize;
            // Update Leanable prices
            for (int side = 0; side < 2; ++side)
            {
                if (m_Market.BestDepthUpdated[side] > m_LeanableDepth[side])
                {// the last update on this side didn't affect our pricing
                    m_LeanablePriceChanged[side] = false;
                    continue;
                }

                int depth = 0;
                double qty = 0;
                double price = double.NaN;
                bool isMarketPreviouslyLeanable = m_IsMarketLeanable[side];
                m_IsMarketLeanable[side] = false;
                while (depth < m_Market.Price[side].Length)
                {
                    qty += m_Market.Qty[side][depth];
                    if (qty >= (m_NeededLeanQty))
                    {
                        price = m_Market.Price[side][depth];
                        m_IsMarketLeanable[side] = true;  // we can lean on this side
                        break;
                    }
                    else
                        depth++;
                }//level

                if (!UV.Lib.Utilities.QTMath.IsPriceEqual(m_LeanablePrices[side], price, tick))
                {// Determine whether leanable price changed or we have a state where we can't lean on the market.
                    isLeanablePriceChanged = true;
                    m_LeanablePriceChanged[side] = true;
                }
                else
                    m_LeanablePriceChanged[side] = false;

                if (isMarketPreviouslyLeanable != m_IsMarketLeanable[side])     // if we have changed states report it
                    isLeanableStatusChange = true;

                m_LeanablePrices[side] = price;
                m_LeanableDepth[side] = depth;

            }//side
            return isLeanablePriceChanged;
        }//UpdateLeanablePrices()
        //
        // *********************************************************
        // ****             Update Leanable Prices()            ****
        // *********************************************************
        /// <summary>
        /// This overload allows the user to not pass in a boolean
        /// </summary>
        /// <returns></returns>
        private bool UpdateLeanablePrices()
        {
            bool notused;
            return UpdateLeanablePrices(out notused);
        }
        //
        //
        // *********************************************************
        // ****             Update Threshold States             ****
        // *********************************************************
        /// <summary>
        /// Called to update our states for all different qty's.  This needs to be optimized once we are sure all the logic is correct.
        /// Much could be done based on the fact that the qty's for the threshold are restrained by each other.  So if you are over one threshold
        /// you are over the others or visa versa
        /// 
        /// </summary>
        private void UpdateThresholdStates(out bool isJoinStatusChanged, out bool isPullStatusChanged,
                                           out bool isSqueezeStatusChanged)
        {

            isJoinStatusChanged = false;
            isPullStatusChanged = false;
            isSqueezeStatusChanged = false;

            if (!IsMarketGood)
            {   // nothing to do, leave our states as is until we have a good state
                return;
            }

            for (int side = 0; side < 2; ++side)
            {   // foreach side, update thresholds 

                if (m_Market.BestDepthUpdated[side] != 0)
                {   // the last update on this side wasn't top of book, so we don't care about it for our threshold states
                    m_ThresholdJoinCrossed[side] = false;
                    m_ThresholdPullCrossed[side] = false;
                    m_ThresholdSqueezeCrossed[side] = false;
                    continue;
                }

                int qty = m_Market.Qty[side][0];

                //
                // Join Qty Thresholds
                //

                isJoinStatusChanged = UpdateJoinThresholdStates(qty, side);

                //
                // Pull Qty Thresholds
                //

                isPullStatusChanged = UpdatePullThresholdStates(qty, side);

                //
                // Squeeze Qty Thresholds
                //

                isSqueezeStatusChanged = UpdateSqueezeThresholdStates(qty, side);

            }
        }
        //
        /// <summary>
        /// This overload allows for updated to occur without regard for firing events.
        /// Typically only called when our market state changes.
        /// </summary>
        private void UpdateThresholdStates()
        {
            bool unused;
            this.UpdateThresholdStates(out unused, out unused, out unused);
        }
        //
        //
        //
        //
        // ********************************************************
        // ****            SetJoinThresholdStates              ****
        // ********************************************************
        /// <summary>
        /// Called on demand or during a market update.  This will set
        /// all internal flags for Join Thresholds
        /// </summary>
        /// <param name="qty"></param>
        /// <param name="side"></param>
        /// <returns>true if our join state has changed</returns>
        private bool UpdateJoinThresholdStates(int qty, int side)
        {
            bool isJoinStatusChanged = false;
            if (qty > m_ThresholdJoin & m_RejoinDelayCountBySide[side] == 0)
            {   // sufficient qty to join and we aren't in a "waiting to rejoin" state
                if (!m_IsInsideMarketAboveThresholdJoin[side])
                {   // we just changed into this state, set flags, and trigger cross
                    m_IsInsideMarketAboveThresholdJoin[side] = true;        //  we are now over join threshold
                    m_ThresholdJoinCrossed[side] = true;                    //  flag our new state to subscribers
                    isJoinStatusChanged = true;
                }
                else
                    m_ThresholdJoinCrossed[side] = false;
            }
            else
            {   // we aren't able to join, just make sure our states are correctly set
                m_IsInsideMarketAboveThresholdJoin[side] = false;
                m_ThresholdJoinCrossed[side] = false;
            }
            return isJoinStatusChanged;
        }
        //
        //
        // ********************************************************
        // ****            UpdatePullThresholdStates           ****
        // ********************************************************
        /// <summary>
        /// Called on demand or during a market update.  This will set
        /// all internal flags for Pull Thresholds
        /// </summary>
        /// <param name="qty"></param>
        /// <param name="side"></param>
        /// <returns>true if our pull state has changed</returns>
        private bool UpdatePullThresholdStates(int qty, int side)
        {
            bool isPullStatusChanged = false;
            if (qty < m_ThresholdPull)
            {   // qty is lower than pull! 
                if (m_IsInsideMarketAboveThresholdPull[side])
                {   // this is the first time we see this, set up our pull states
                    m_IsInsideMarketAboveThresholdPull[side] = false;                       // set our flags 
                    m_ThresholdPullCrossed[side] = true;
                    isPullStatusChanged = true;
                    m_RejoinDelayCountBySide[side] = m_RejoinDelayPostPullSeconds;          // increment our counter on this side
                }
                else
                    m_ThresholdPullCrossed[side] = false;

            }
            else
            {   // we are above our pull threshold, make sure flag is true;
                m_IsInsideMarketAboveThresholdPull[side] = true;
                m_ThresholdPullCrossed[side] = false;
            }
            return isPullStatusChanged;
        }
        //
        //
        // ********************************************************
        // ****            UpdateSqueezeThresholdStates        ****
        // ********************************************************
        /// <summary>
        /// Called on demand or during a market update.  This will set
        /// all internal flags for squeeze Thresholds
        /// </summary>
        /// <param name="qty"></param>
        /// <param name="side"></param>
        /// <returns>true if our squeeze state has changed</returns>
        private bool UpdateSqueezeThresholdStates(int qty, int side)
        {
            bool isSqueezeStatusChanged = false;
            if (qty < m_ThresholdSqueeze)
            {   // qty is less than our squeeze threshold 
                if (m_IsInsideMarketAboveThresholdSqueeze[side])
                {   // first time we are below our squeeze.
                    m_IsInsideMarketAboveThresholdSqueeze[side] = false;
                    m_ThresholdSqueezeCrossed[side] = true;
                    isSqueezeStatusChanged = true;
                }
                else
                    m_ThresholdSqueezeCrossed[side] = false;
            }
            else
            {
                m_IsInsideMarketAboveThresholdSqueeze[side] = true;
                m_ThresholdSqueezeCrossed[side] = false;
            }
            return isSqueezeStatusChanged;
        }
        #endregion//Private Methods

        #region ItimerSubscriber Implementation
        // *****************************************************************
        // ****                     ItimerSubscriber                    ****
        // *****************************************************************
        //
        /// <summary>
        /// Currently called once a second. This will check our delays for rejoning and make sure 
        /// our states are correctly set up.
        /// </summary>
        public void TimerSubscriberUpdate()
        {
            bool isJoinStateChanged = false;
            for (int side = 0; side < 2; side++)
            {   // check each side to see if we are waiting to rejoin
                if (m_RejoinDelayCountBySide[side] > 0)
                {   // we are waiting to rejoin on this side
                    m_RejoinDelayCountBySide[side]--;               // decrement our rejoin count
                    if (m_RejoinDelayCountBySide[side] == 0)
                    {   // we are now able to consider rejoin, check qty to see if we want to rejoin and if we do trigger event
                        isJoinStateChanged = UpdateJoinThresholdStates(m_Market.Qty[side][0], side);
                    }
                }
            }

            if (isJoinStateChanged)
                OnPullJoinStateChanged();
        }
        #endregion //Event Handlers

        #region Our Events and Triggers
        // *****************************************************************
        // ****                     Events                              ****
        // *****************************************************************
        //
        //
        // ****                 LeanablePriceChanged                ****
        /// <summary>
        /// After a mkt update, the LeanablePrices are computed.  If their values
        /// changed, then this method is called to inform subscribers.
        /// </summary>
        public event EventHandler LeanablePriceChanged;
        //
        private void OnLeanablePriceChanged()
        {
            if (this.LeanablePriceChanged != null)
                this.LeanablePriceChanged(this, EventArgs.Empty);
        }
        //
        //
        //
        //
        // ****                 InsidePriceChanged                   ****
        /// <summary>
        /// Event is triggered each time the price has changed on the inside market.
        /// </summary>
        public event EventHandler PriceChanged;
        //
        private void OnPriceChanged()
        {
            if (this.PriceChanged != null)
                this.PriceChanged(this, EventArgs.Empty);
        }
        //
        //
        //
        //
        // ****                 MarketStateChanged                   ****
        /// <summary>
        /// Even is triggered each time the market status of an instrument has changed
        /// </summary>
        public event EventHandler MarketStateChanged;
        //
        private void OnMarketStateChanged()
        {
            if (this.MarketStateChanged != null)
                this.MarketStateChanged(this, EventArgs.Empty);
        }
        //
        //
        //
        //
        //
        // ****                 SqueezeStateChanged                ****
        /// <summary>
        /// Called whenever a leg crosses a threshold based on inside market qty to squeeze open positions (or stop squeezing)
        /// </summary>
        public event EventHandler SqueezeStateChanged;
        //
        private void OnSqueezeStateChanged()
        {
            if (this.SqueezeStateChanged != null)
                this.SqueezeStateChanged(this, EventArgs.Empty);
        }
        //
        //
        //
        // ****                 PullJoinStateChanged                ****
        /// <summary>
        /// Called whenever a leg crosses a threshold based on inside market qty to join a level
        /// </summary>
        public event EventHandler PullJoinStateChanged;
        //
        private void OnPullJoinStateChanged()
        {
            if (this.PullJoinStateChanged != null)
                this.PullJoinStateChanged(this, EventArgs.Empty);
        }
        //
        // ****                 ParameterChanged                    ****
        /// <summary>
        /// Event is triggered each time the parameters for this leg change an the quoter needs to react.
        /// </summary>
        public event EventHandler ParameterChanged;
        //
        private void OnParameterChanged()
        {
            UpdateLeanablePrices();
            UpdateThresholdStates();
            if (this.ParameterChanged != null)
                this.ParameterChanged(this, EventArgs.Empty);
        }
        //
        #endregion// Our Events and Triggers

        #region IStringifiable Implementation
        // *****************************************************************
        // ****                     IStringifiable                      ****
        // *****************************************************************
        //
        //
        string IStringifiable.GetAttributes()
        {
            StringBuilder s = new StringBuilder(base.GetAttributes());
            s.AppendFormat(" BaseVolumeLean={0}", this.BaseVolumeLean);
            s.AppendFormat(" OffsetVolumeMultiplier={0}", this.OffsetVolumeMultiplier);
            s.AppendFormat(" QuotingEnabled={0}", this.m_UserDefinedQuotingEnabled);
            return s.ToString();
        }
        List<IStringifiable> IStringifiable.GetElements()
        {
            return null;
        }
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
            base.SetAttributes(attributes);
            int i;
            bool isTrue;
            foreach (KeyValuePair<string, string> attr in attributes)
            {
                if (attr.Key == "BaseVolumeLean" && int.TryParse(attr.Value, out i))
                    this.m_BaseVolumeLean = i;
                else if (attr.Key == "OffsetVolumeMultiplier" && int.TryParse(attr.Value, out i))
                    this.OffsetVolumeMultiplier = i;
                else if (attr.Key.Equals("JoinThreshold", StringComparison.OrdinalIgnoreCase) && int.TryParse(attr.Value, out i))
                    this.m_ThresholdJoin = i;
                else if (attr.Key.Equals("PullThreshold", StringComparison.OrdinalIgnoreCase) && int.TryParse(attr.Value, out i))
                    this.m_ThresholdPull = i;
                else if (attr.Key.Equals("SqueezeThreshold", StringComparison.OrdinalIgnoreCase) && int.TryParse(attr.Value, out i))
                    this.m_ThresholdSqueeze = i;
                else if (attr.Key.Equals("RejoinDelayPostPullSeconds", StringComparison.OrdinalIgnoreCase) && int.TryParse(attr.Value, out i))
                    this.m_RejoinDelayPostPullSeconds = i;
                else if (attr.Key == "QuotingEnabled" && bool.TryParse(attr.Value, out isTrue))
                    this.m_UserDefinedQuotingEnabled = isTrue;
                else if (attr.Key == "DefaultAccount")
                {
                    if (attr.Value.Length > 15) // 15 char limit!
                        this.DefaultAccount = attr.Value.Substring(0, 15);
                    else
                        this.DefaultAccount = attr.Value;
                }
            }

        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {
            base.AddSubElement(subElement);
            Type type = subElement.GetType();
            if (type == typeof(PriceLeg))
            {
                m_PriceLeg = (PriceLeg)subElement;
                this.m_EngineName = string.Format("CurveLeg:{0}", m_PriceLeg.InstrumentName.ToString());       // set our name.
            }
            else if (type == typeof(Scratcher))
            {
                m_Scratcher = (Scratcher)subElement;
                m_Scratcher.m_PriceLeg = this.m_PriceLeg;
            }
        }
        #endregion end IStringifiable

    }
}
