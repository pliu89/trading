using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Threading;

namespace UV.Strategies.ExecutionEngines.Risk
{
    using UVFill = UV.Lib.Fills.Fill;
    using UV.Lib.Fills;
    using UV.Lib.Engines;
    using UV.Lib.MarketHubs;
    using UV.Lib.Hubs;
    using UV.Lib.Products;
    using UV.Lib.OrderBooks;
    using UV.Lib.IO.Xml;

    using UV.Strategies.StrategyEngines;
    using UV.Strategies.ExecutionHubs;
    using UV.Strategies.ExecutionEngines.OrderEngines;
    /// <summary>
    /// Collection of methods and functions to manage risk for a strategy.
    /// </summary>
    public class RiskManager : Engine, ITimerSubscriber, IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //   
        // External services:
        protected LogHub m_Log;
        protected Hub m_Hub;
        protected ExecutionContainer m_ExecutionContainer;
        protected IOrderEngine m_IOrderEngine;
        private SingleLegExecutor m_SingleLegExecutor;

        // User Defined Risk Variables
        public double m_MaxLossPnL = 0;                         // maximum acceptable loss of the strategy before firing risk trigger
        public int m_MaxFillQty = 0;                            // max number of fills expected by strategy before firing risk trigger
        protected int m_MaxQuotesPerSecond = 0;                 // max numer of quotes the quoter can send out per second before firing risk trigger.
        protected double m_FatFingerPriceTolerance;             // list of tolerances for pricing
        public int m_FatFingerTicks = 0;                        // max number of ticks that our price is allowed to go through the market on start or a param price change.

        // Risk accounting variables
        public int m_TotalFillQty = 0;                          // current absolute value of fills across all instruments 
        protected double m_PnL = 0;                             // current PnL across entire strat.
        public int m_NumberOfQuotesThisSecond;                  // variable for keeping count of quotes (only from quoter)that have occured this second. 
        public int m_TotalNumberOfQuotes;                       // variable for keeping count of total quotes for quoter AND Hedger.

        //dummy variables
        protected int m_SecondCount = 0;                          // each time we trip the risk flag this will increment
        protected int m_RiskCount = 0;
        protected bool m_IsRiskTriggered;

        // sanity check variables
        protected int m_MaxPossibleWorkingQuoteQtyPerLot;       // essentially a sum of the abs() of all the leg ratios 

        // data objects
        private PriceLeg m_Leg;                                 // leg we are responsible for
        private InstrumentDetails m_InstrDetails;

        // state flags
        protected bool RiskIsReady = false;                     // is risk manager all set up and able to check risk.
        #endregion// members

        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        public RiskManager()
            : base()
        {
        }

        protected override void SetupInitialize(IEngineHub myEngineHub, IEngineContainer engineContainer, int engineID, bool setupGui)
        {
            base.SetupInitialize(myEngineHub, engineContainer, engineID, setupGui);
            m_Hub = (Hub)myEngineHub;
            this.m_Log = m_Hub.Log;                                              // set up our logging 
            this.m_ExecutionContainer = (ExecutionContainer)engineContainer;

        }
        //
        //
        public override void SetupBegin(IEngineHub myEngineHub, IEngineContainer engineContainer)
        {
            base.SetupBegin(myEngineHub, engineContainer);
            foreach (IEngine ieng in engineContainer.GetEngines())
            { // Find order Engine
                if (ieng is IOrderEngine)
                    this.m_IOrderEngine = (IOrderEngine)ieng;                                             // find our order engine
            }
            if (m_IOrderEngine is SingleLegExecutor)
            { // if this is a single leg order engine
                m_SingleLegExecutor = (SingleLegExecutor)m_IOrderEngine;
                this.m_Leg = m_SingleLegExecutor.m_PriceLeg;
                m_MaxPossibleWorkingQuoteQtyPerLot += (int)(Math.Abs(m_Leg.Weight) * 2);            // leg ratios * 2 (each side)
                
            }
            // If we subscribe to order book creation, we can be sure to get all the info for all fills and submissions
            // this only works if create order books after setup beging.
            m_IOrderEngine.GetExecutionListener().OrderBookCreated += new EventHandler(ExecutionListener_OrderBookCreated);

            if(m_Hub is ExecutionHub)
            { // set up ITimer to work
                ((ExecutionHub)m_Hub).SubscribeToTimer((ITimerSubscriber)m_ExecutionContainer.m_ExecutionListener);
                m_ExecutionContainer.m_ExecutionListener.SubscribeToTimer(this); // subscrie to updates
            }
        }
        //       
        #endregion//Constructors

        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// The total number of fills the strategy is allowed to get before pausing
        /// </summary>
        public int MaxFillQty
        {
            get { return m_MaxFillQty; }
            set { m_MaxFillQty = value; }
        }
        /// <summary>
        /// The maximum negative PnL the strategy is allowed to reach before pausing
        /// </summary>
        public double MaxLossPnL
        {
            get { return m_MaxLossPnL; }
            set { m_MaxLossPnL = value; }
        }
        /// <summary>
        /// Number of ticks from opposite side we are able to send orders through.
        /// </summary>
        public virtual int FatFingerTicks
        {
            get { return m_FatFingerTicks; }
            set
            {
                if (RiskIsReady)
                { //we have a new value
                    m_FatFingerTicks = value; // set it
                    m_FatFingerPriceTolerance = m_InstrDetails.TickSize * value;
                }
            }
        }
        /// <summary>
        /// Simple risk check to ensure we aren't spamming the market.
        /// </summary>
        public int MaxQuotesPerSecond
        {
            get { return m_MaxQuotesPerSecond; }
            set { m_MaxQuotesPerSecond = value; }
        }
        /// <summary>
        /// Current PnL of the strategy.
        /// </summary>
        public double StratPnL
        {
            get { return m_PnL; }
        }
        /// <summary>
        /// Allows a user to reset a risk tripped state after
        /// changing tolerances
        /// </summary>
        public bool ResetRiskControls
        {
            get { return m_IsRiskTriggered; }
            set
            {
                m_IsRiskTriggered = value;
                if (!m_IsRiskTriggered)
                {
                    m_IOrderEngine.IsRiskCheckPassed = true;
                    if (PnLCheck() | CheckQuoteLimits())
                        FlagRiskEvents();
                }
            }
        }
        #endregion//Properties

        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        // *************************************************************
        // ****                       Start()                       ****
        // *************************************************************
        /// <summary>
        /// called once the the order engine has found all the legs it is looking for.
        /// details
        /// </summary>
        public virtual void Start()
        {
            if (m_SingleLegExecutor != null && !TryIntitialize())
                m_Log.NewEntry(LogLevel.Error, "RiskManager : Failed To Initialize, Couldn't Find All Instrument Details");
            RiskIsReady = true;                                         // set our state to ready
            FatFingerTicks = m_FatFingerTicks;                          // set our tolerances since we now have tick size
            m_IOrderEngine.IsRiskCheckPassed = true;                    // let our order engine begin to send orders.
        }
        //
        //
        // *************************************************************
        // ****                    ValidatePrices()                 ****
        // *************************************************************
        /// <summary>
        /// Called prior to updating prices in a strategy to make sure our prices are valid.
        /// 
        /// This overload is called when the user does NOT have the book.
        /// </summary>
        /// <param name="desiredPrice"></param>
        /// <param name="mktSide"></param>
        /// <param name="instrName"></param>
        /// <returns>true if prices are valid.</returns>
        public bool ValidatePrices(double desiredPrice, int mktSide, InstrumentName instrName)
        {
            if (m_Leg.InstrumentName != instrName)
            {
                m_Log.NewEntry(LogLevel.Error, "{0} Submitted Price Check To Risk Manager That Does Not Manage Risk For That Instrument", m_ExecutionContainer);
                return false;
            }

            int mktSign = UV.Lib.Utilities.QTMath.MktSideToMktSign(mktSide);                            // sign of the market we are looking to execute on 
            int oppMktSign = mktSign * -1;                                                              // opposite sign 
            int oppMktSide = UV.Lib.Utilities.QTMath.MktSignToMktSide(oppMktSign);                      // opposite side
            double oppMktPrice = m_ExecutionContainer.m_Markets[instrName].Price[oppMktSide][0];             // best bid/offer (aggressible) price

            double allowedPrice = oppMktPrice + (m_FatFingerPriceTolerance * mktSign);                  // this is the max allowable price that we are allow to buy/sell 

            if ((desiredPrice * mktSign) >= (allowedPrice * mktSign))                                   // this should correctly avoid us executing any prices outside our tolerances.
            {
                m_Log.NewEntry(LogLevel.Error, "Risk Check has Rejected Your Price Update To {0}, Price must be better than {1}", desiredPrice, allowedPrice);
                return false;
            }
            else
                return true;
        }
        //
        //
        /// <summary>
        /// Called prior to updating prices in a strategy to make sure our prices are valid.
        /// This overload is used for passing in a synthetic market.  
        /// </summary>
        /// <param name="desiredPrice"></param>
        /// <param name="mktSide"></param>
        /// <param name="mktPrices"></param>
        /// <returns>true if prices are valid.</returns>
        public bool ValidatePrices(double desiredPrice, int mktSide, double[] mktPrices)
        {
            if (!RiskIsReady)
            {
                m_Log.NewEntry(LogLevel.Error, "ValidatePrices: RiskManager is not ready to check prices, rejecting update");
                return false;
            }

            int mktSign = UV.Lib.Utilities.QTMath.MktSideToMktSign(mktSide);                            // sign of the market we are looking to execute on 

            int oppMktSign = mktSign * -1;                                                              // opposite sign 
            int oppMktSide = UV.Lib.Utilities.QTMath.MktSignToMktSide(oppMktSign);                      // opposite side
            double allowedPrice = mktPrices[oppMktSide] + (m_FatFingerPriceTolerance * mktSign);        // this is the max allowable price that we are allow to buy/sell 

            if ((desiredPrice * mktSign) >= (allowedPrice * mktSign))                                   // this should correctly avoid us executing any prices outside our tolerances.
            {
                m_Log.NewEntry(LogLevel.Error, "Risk Check has Rejected Your Price Update To {0}, Price must be better than {1}", desiredPrice, allowedPrice);
                return false;
            }
            else
                return true;
        }
        //
        #endregion//Public Methods

        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        //**************************************************************
        // ****                     TryIntitialize()                ****
        //**************************************************************
        //
        /// <summary>
        /// Called once the markets are initialized to set up information 
        /// for faster processing later on.
        /// </summary>
        /// <returns></returns>
        private bool TryIntitialize()
        {
            if (!m_ExecutionContainer.m_InstrDetails.TryGetValue(m_Leg.InstrumentName, out m_InstrDetails))
                return false;
            return true;
        }
        //**************************************************************
        // ****             UpdateAndCheckTotalFillCounts()         ****
        //**************************************************************
        /// <summary>
        /// Aggregate total number of fills across entire strategy and check 
        /// to make sure we are under the number of allowed fills. If we are over return true.
        /// </summary>
        /// <param name="fill"></param>
        /// <returns>true if over max allowed fills</returns>
        protected bool UpdateAndCheckTotalFillCounts(UVFill fill)
        {
            if (fill == null)
                return false;
            int fillQty = Math.Abs(fill.Qty);
            // sum total fill count 
            m_TotalFillQty += fillQty;
            // check to see if we have tripped our risk trigger
            if (m_TotalFillQty > m_MaxFillQty)
            {
                m_Log.NewEntry(LogLevel.Error, "Max Fills Has Been Exceeded: FillQty = {0} MaxFillQty = {1}",
                  m_TotalFillQty, m_MaxFillQty);
                return true;
            }
            else
                return false;
        }
        //
        //
        //
        //***********************************************************
        // ****                    PnLCheck()                    ****
        //***********************************************************
        // 
        //
        /// <summary>
        /// Aggregate total PnL across all legs of the strategy, and ensure we aren't below
        /// our allowed loss 
        /// </summary>
        /// <returns>true if we are below our maximum allowed loss</returns>
        protected virtual bool PnLCheck()
        {
            if (!m_SingleLegExecutor.m_IsLegSetUpComplete)
                return false;

            m_PnL = 0; // reset PnL to 0
            double midPrice = (m_ExecutionContainer.m_Markets[m_SingleLegExecutor.m_InstrumentDetails.InstrumentName].Price[UV.Lib.Utilities.QTMath.BidSide][0] +
            m_ExecutionContainer.m_Markets[m_SingleLegExecutor.m_InstrumentDetails.InstrumentName].Price[UV.Lib.Utilities.QTMath.AskSide][0]) / 2;
            m_PnL = m_IOrderEngine.GetFillBook().m_RealizedGain + m_IOrderEngine.GetFillBook().UnrealizedDollarGains(midPrice);
            if (m_PnL < m_MaxLossPnL)
            {
                m_Log.NewEntry(LogLevel.Error, "Max Loss PnL Has Been Exceeded: Current PnL = {0} MaxLossPnL = {1}", m_PnL, m_MaxLossPnL);
                return true;
            }
            else
                return false;
        }
        //
        //
        //
        //
        /// <summary>
        /// Caller would like to ensure we aren't over the user defined quotes per minute limit
        /// </summary>
        /// <returns>true if we above the limit</returns>
        protected bool CheckQuoteLimits()
        {
            if (m_NumberOfQuotesThisSecond > m_MaxQuotesPerSecond)
            { //we have exceeded max number of quotes per second
                m_Log.NewEntry(LogLevel.Error, "Number of Quotes this second = {0} and exceeds the user defined max allowable of {1}.",
                    m_NumberOfQuotesThisSecond, m_MaxQuotesPerSecond);
                return true;
            }
            else
                return false;
        }
        /// <summary>
        /// Caller would like to turn off all order routing, and turn the m_IsRiskTriggered to true.  This will allow for the next second for
        /// the strategy to completely stop.  Hopefully this allows for some hedging to occcur prior to the hard stop of all functionality.
        /// </summary>
        protected virtual void FlagRiskEvents()
        {
            m_IOrderEngine.IsRiskCheckPassed = false;
            ((Engine)m_IOrderEngine).BroadcastParameter((IEngineHub)m_SingleLegExecutor.m_Hub, m_SingleLegExecutor.m_ExecutionContainer, "IsRiskCheckPassed");
            m_IOrderEngine.CancelAllOrders();
            m_IsRiskTriggered = true;
            UV.Lib.FrontEnds.Utilities.GuiCreator.ShowMessageBox(string.Format("{0} : Risk Event Triggered! Trading is turning off.", m_InstrDetails.InstrumentName), "Risk Triggered");
        }
        //
        //
        //
        //
        //**************************************************************
        // ****           CheckWorkingOrderCounts()                 ****
        //**************************************************************
        //
        //
        //
        protected virtual bool CheckWorkingOrderCounts()
        {
            long maxTotalWorkQuoteQty = m_MaxPossibleWorkingQuoteQtyPerLot * m_IOrderEngine.DripQty; // this is assuming we are quoting both sides and all legs
            if (m_SingleLegExecutor.m_OrderBook.m_TotalWorkingOrderQty > maxTotalWorkQuoteQty)
            { // we are somehow quoting more than we should be. 
                m_Log.NewEntry(LogLevel.Error, "Total working qty in quote legs = {0} which is more than the expected max of = {1}",
                    m_SingleLegExecutor.m_OrderBook.m_TotalWorkingOrderQty, maxTotalWorkQuoteQty);
                return true;
            }
            return false;
        }
        #endregion//Private Methods

        #region Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                      ****
        // *****************************************************************
        //
        //
        // ***********************************************************
        // ****               OrderBook_OrderFilled()             ****
        // ***********************************************************
        //
        public virtual void OrderBook_OrderFilled(object sender, EventArgs eventArgs)
        {
            FillEventArgs fillEventArgs = (FillEventArgs)eventArgs;
            UVFill fill = fillEventArgs.Fill;
            if (UpdateAndCheckTotalFillCounts(fill) | PnLCheck())
            { // Take the fill and update our counts and PnL, checking both.
                FlagRiskEvents();
            }
        }
        //
        //
        //
        // ***********************************************************
        // ****                OrderBook_OrderSubmitted()         ****
        // ***********************************************************
        public void OrderBook_OrderSubmitted(object sender, EventArgs eventArgs)
        {
            OrderEventArgs orderEventArgs = (OrderEventArgs)eventArgs;
            if (orderEventArgs.Order.OrderStateConfirmed == UV.Lib.OrderBooks.OrderState.Submitted)      // every time we submit an order 
                if (CheckWorkingOrderCounts())                                                              // check our counts
                    FlagRiskEvents();
        }
        //
        //
        // ***********************************************************
        // ****      ExecutionListener_OrderBookCreated()         ****
        // ***********************************************************
        public void ExecutionListener_OrderBookCreated(object sender, EventArgs eventArgs)
        {
            OrderBook orderBook = (OrderBook)sender;
            orderBook.OrderSubmitted += new EventHandler(OrderBook_OrderSubmitted);
            orderBook.OrderFilled += new EventHandler(OrderBook_OrderFilled);
        }
        //
        #endregion//Event Handlers

        #region ITimerSubscriber Implementation
        //
        // *****************************************************************
        // ****                  TimerSubscriberUpdate                  ****
        // *****************************************************************
        //
        public virtual void TimerSubscriberUpdate()
        {
            if (PnLCheck() | CheckQuoteLimits())    // max loss pnl tripped or max number of quotes per second exceeded
                FlagRiskEvents();
            m_SecondCount++;                            // increment our second counter
            if (m_IsRiskTriggered)
            {
                m_RiskCount++;
                if (m_RiskCount == 2)
                {
                    m_Log.NewEntry(LogLevel.Error, "Risk Event Triggered! Pausing Strategy");
                }
            }
            m_TotalNumberOfQuotes += m_NumberOfQuotesThisSecond; // Add to total's count.
            m_NumberOfQuotesThisSecond = 0; // reset count each second.
            base.BroadcastParameter((IEngineHub)m_SingleLegExecutor.m_Hub, m_SingleLegExecutor.m_ExecutionContainer, 4);
            base.BroadcastParameter((IEngineHub)m_SingleLegExecutor.m_Hub, m_SingleLegExecutor.m_ExecutionContainer, 5);
        }
        #endregion

        #region Istringifiable Implementation
        //
        // *****************************************************************
        // ****              Istringifiable Implementation              ****
        // *****************************************************************
        //
        string IStringifiable.GetAttributes()
        {
            StringBuilder s = new StringBuilder();
            s.AppendFormat("MaxQuotesPerSecond={0}", this.MaxQuotesPerSecond);
            s.AppendFormat("FatFingerTicks={0}", this.m_FatFingerTicks);
            s.AppendFormat("MaxLossPnL={0}", this.MaxLossPnL);
            s.AppendFormat("MaxFillQty={0}", this.MaxFillQty);
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
            double x;
            foreach (KeyValuePair<string, string> attr in attributes)
            {
                if (attr.Key == "MaxQuotesPerSecond" && int.TryParse(attr.Value, out i))
                    this.MaxQuotesPerSecond = i;
                else if (attr.Key == "FatFingerTicks" && int.TryParse(attr.Value, out i))
                    this.m_FatFingerTicks = i;
                else if (attr.Key == "MaxLossPnL" && double.TryParse(attr.Value, out x))
                    this.MaxLossPnL = x;
                else if (attr.Key == "MaxFillQty" && int.TryParse(attr.Value, out i))
                    this.MaxFillQty = i;
                else if (attr.Key == "EngineId" && int.TryParse(attr.Value,out i))
                    this.m_EngineID = i;
            }
        }

        void IStringifiable.AddSubElement(IStringifiable subElement)
        {
            base.AddSubElement(subElement);
        }
        #endregion //Istringifiable
    }
}