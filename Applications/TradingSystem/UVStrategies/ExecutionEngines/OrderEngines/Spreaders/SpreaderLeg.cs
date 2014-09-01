using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Strategies.ExecutionEngines.OrderEngines
{
    using UV.Strategies.StrategyEngines;
    using UV.Lib.MarketHubs;
    using UV.Lib.Products;
    using UV.Lib.IO.Xml;
    using UV.Lib.Engines;
    using UV.Lib.Fills;
    using UV.Lib.Utilities;
    using UV.Strategies.ExecutionHubs;
    using UV.Strategies.ExecutionEngines.HedgeRules;
    using UV.Strategies.ExecutionEngines.Hedgers;
    using UV.Strategies.ExecutionHubs.ExecutionContainers;
    //
    /// <summary>
    /// All parameters to define a leg of a spread.
    /// </summary>
    public class SpreaderLeg : Engine, IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        public PriceLeg m_PriceLeg;
        public Hedger m_Hedger;
        public HedgeRuleManager m_HedgeRuleManager;
        public UV.Lib.BookHubs.Market m_Market;
        //
        // Lean Params
        //
        private int m_BaseVolumeLean = 0;
        private double m_OffsetVolumeMultiplier = 1.0;
        private int m_NeededLeanQty;
        //
        // Quote Params
        //
        private int m_DripQty = 0;
        private bool m_UserDefinedQuotingEnabled;
        public bool[] m_QuotingEnabled = new bool[2];               //  quoting bool for this leg and each side
        private OffMarketQuoteBehavior m_OffMarketQuotingBehavior = OffMarketQuoteBehavior.NoQuote;  // by default we won't quote off market 
        private int m_OffMarketTicks;                                  // number of ticks away from same side best price that we are in the "off market" state.
        //
        // Instrument Details
        //
        public InstrumentDetails InstrumentDetails;                 // store all details about this instrument.
        public string DefaultAccount;
        //
        // State Flags
        //
        public bool IsMarketGood;
        public bool[] m_IsMarketLeanable;           // does sufficient qty exist on either side of the market to lean on            
        //
        // Market variables
        //
        public int m_OrderBookDepthMax = 5;         // max number of levels to consider.
        public double[] m_LeanablePrices;           // current mkt price with enough leanable qty: m_LeanablePrices[mktSide]
        private int[] m_LeanableDepth;              // current depth with enough leanable qty  
        public bool[] m_LeanablePriceChanged = new bool[2];
        //
        public double m_OffMarketPriceDifference = 0;
        
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
        public SpreaderLeg()
        {
            // Initialize our internal market variables.
            m_LeanablePrices = new double[2];                                   // bidPrice, askPrice after cleaning
            m_LeanableDepth = new int[2];
            m_IsMarketLeanable = new bool[2];
            for (int mktSide = 0; mktSide < 2; ++mktSide)
            {
                m_LeanablePrices[mktSide] = double.NaN;
                m_LeanableDepth[mktSide] = 10; // means we care about all price updates
            }//mktSide
        }
        //
        //
        protected override void SetupInitialize(IEngineHub myEngineHub, IEngineContainer engineContainer, int engineID, bool setupGui)
        {
            base.SetupInitialize(myEngineHub, engineContainer, engineID, setupGui);
            ThreadContainer execContainer = (ThreadContainer)engineContainer;
            execContainer.TryAddEngine(m_Hedger);   // add my sub engines to the container.
            execContainer.TryAddEngine(m_HedgeRuleManager);
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
        /// <summary>
        /// How should this leg handle quotes when they are off market defined by a number of ticks
        /// </summary>
        public OffMarketQuoteBehavior OffMarketQuoting
        {
            get { return m_OffMarketQuotingBehavior; }
            set
            {
                if (m_OffMarketQuotingBehavior != value)
                {
                    m_OffMarketQuotingBehavior = value;
                    OnParameterChanged();
                }
            }
        }
        //
        /// <summary>
        /// Number of ticks for a quote to be consider off market.
        /// </summary>
        public int OffMarketTicks
        {
            get { return m_OffMarketTicks; }
            set
            {
                if (m_OffMarketTicks != value)
                {
                    m_OffMarketTicks = value;
                    m_OffMarketPriceDifference = m_OffMarketTicks * InstrumentDetails.TickSize;
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
        // ****             TrySetInstrumentDetails()            ****
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
            m_OffMarketPriceDifference = m_OffMarketTicks * instrDetails.TickSize;
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
                UpdateLeanablePrices();
                OnLeanablePriceChanged();                                       //  trigger events for our subscribers  
                OnPriceChanged();
                OnMarketStateChanged();
            }
            else if (IsMarketGood)
            {   // Market state hasn't changed - and is good!
                bool isLeanableStatusChange;
                bool isLeanablePriceChanged = UpdateLeanablePrices(out isLeanableStatusChange);
                if (isLeanableStatusChange)                                     // this has to be called first to set the bools correctly for quoting.
                    OnMarketStateChanged();
                if (isLeanablePriceChanged)                                     // if LeanablePrices changed, then 
                    OnLeanablePriceChanged();                                   //  trigger events for our subscribers
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
        /// <summary>
        /// Called by the a Quoter to let the instrument know the needed lean qty of his leg.
        /// </summary>
        /// <param name="dripQty"></param>
        public void UpdateDripQty(int dripQty)
        {
            m_DripQty = dripQty;
            m_NeededLeanQty = (int)((m_OffsetVolumeMultiplier * Math.Abs(m_PriceLeg.Weight) * dripQty) + m_BaseVolumeLean);
            if(m_Market != null)
                Market_MarketChanged(this, EventArgs.Empty); // trigger fake price change to force updates since our leanable prices probably changed
        }
        #endregion // Public Methods

        #region Private methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        //
        //**********************************************************
        //****             Update Market Prices()               ****
        //**********************************************************
        /// <summary>
        /// Called whenever the mkt changes.  Updates m_Prices[][] and m_Qtys[][].
        /// returns True if inside market has changed prices
        /// </summary>
        /// TODO: THIS NEEDS TO BE FIXED
        private bool UpdateMarketPrices(UV.Lib.BookHubs.Market market, List<int>[] MarketDepthChangedList)
        {
            bool isChanged = false;
            for (int side = 0; side < 2; ++side)
            {
                if (MarketDepthChangedList[side].Count == 0 || MarketDepthChangedList[side][0] > m_LeanableDepth[side] || MarketDepthChangedList[side][0] > m_OrderBookDepthMax)
                { // if no change on this side || top change is past our leanable depth || top change is past the depth we care about
                    //nothing to do here!
                }
                else
                { // this is a price or qty change that we need to consider
                    if (MarketDepthChangedList[side][0] == 0 && !QTMath.IsPriceEqual(m_Market.Price[side][MarketDepthChangedList[side][0]], market.Price[side][0], InstrumentDetails.TickSize))
                    {// if this is a top of book price change!
                        isChanged = true;
                    }
                    foreach (int level in MarketDepthChangedList[side])
                    { // Set new internal market from the depth changed back
                        if (level < m_OrderBookDepthMax)
                        { // we care about updates at this level
                            m_Market.Price[side][level] = market.Price[side][level];
                            m_Market.Qty[side][level] = market.Qty[side][level];
                        }
                    }
                }
            }
            return isChanged;
        }
        //
        //
        // * Update Market Price should theoretically be able to leave stale quotes for market depth greater than what we need.
        //  if the UpdateLenablePrices and it both work correctly, the time savings of not clearing these levels will be considerable.
        //
        // ****             Update Leanable Prices()            ****
        //
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
            {
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
        //
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
        //
        //
        //
        #endregion//Private Methods

        #region Our Events and Triggers
        // *****************************************************************
        // ****                     Events                              ****
        // *****************************************************************
        //
        //
        //
        // ****                 LeanablePriceChanged                ****
        //
        public event EventHandler LeanablePriceChanged;
        /// <summary>
        /// After a mkt update, the LeanablePrices are computed.  If their values
        /// changed, then this method is called to inform subscribers.
        /// </summary>
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
        //
        public event EventHandler PriceChanged;
        //
        //
        /// <summary>
        /// Event is triggered each time the price has changed on the inside market.
        /// </summary>
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
        //
        private void OnMarketStateChanged()
        {
            if (this.MarketStateChanged != null)
                this.MarketStateChanged(this, EventArgs.Empty);
        }
        //
        //
        //
        // ****                 ParameterChanged                   ****
        //
        public event EventHandler ParameterChanged;
        //
        //
        /// <summary>
        /// Event is triggered each time the parameters for this leg change an the quoter needs to react.
        /// </summary>
        private void OnParameterChanged()
        {
            UpdateDripQty(m_DripQty);
            UpdateLeanablePrices();
            if (this.ParameterChanged != null)
                this.ParameterChanged(this, EventArgs.Empty);
        }
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
            s.AppendFormat(" OffMarketQuotingBehavior={0}", this.m_OffMarketQuotingBehavior);
            s.AppendFormat(" OffMarketTicks={0}", this.m_OffMarketTicks);
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
                else if (attr.Key == "QuotingEnabled" && bool.TryParse(attr.Value, out isTrue))
                    this.m_UserDefinedQuotingEnabled = isTrue;
                else if (attr.Key == "OffMarketQuotingBehavior" && int.TryParse(attr.Value, out i))
                    this.m_OffMarketQuotingBehavior = (OffMarketQuoteBehavior)i;
                else if (attr.Key == "OffMarketTicks" && int.TryParse(attr.Value, out i))
                    this.m_OffMarketTicks = i;
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
                this.m_EngineName = string.Format("QuoterLeg:{0}", m_PriceLeg.InstrumentName.ToString());       // set our name.
            }
            else if (type == typeof(Hedger))
            {
                m_Hedger = (Hedger)subElement;
                m_Hedger.m_SpreaderLeg = this;            // give pointer to ourself
            }
            else if (type == typeof(HedgeRuleManager))
            {
                m_HedgeRuleManager = (HedgeRuleManager)subElement;
                m_HedgeRuleManager.m_QuoterLeg = this;
            }
        }
        #endregion end IStringifiable

    }
}
