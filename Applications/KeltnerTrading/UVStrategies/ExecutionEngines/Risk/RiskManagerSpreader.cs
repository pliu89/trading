using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace UV.Strategies.ExecutionEngines.Risk
{
    using UV.Strategies.ExecutionEngines.OrderEngines;
    using UV.Strategies.ExecutionEngines.Hedgers;

    using UV.Lib.Engines;
    using UV.Lib.Hubs;
    using UV.Lib.Fills;
    using UV.Lib.IO.Xml;
    using UV.Lib.OrderBooks;
    
    /// <summary>
    /// Risk mananger that extends base functionality to handle extra
    /// risk associated with spreading.
    /// </summary>
    public class RiskManagerSpreader :  RiskManager, IStringifiable
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //   
        // External services:
        public HedgeManager m_HedgeManager;
        public ExecutionEngines.OrderEngines.Spreader m_Spreader;
        #endregion// members
        
        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// This is the deafult Istringifiable constructor. If used, RiskManager.Initialize must be called 
        /// prior to  RiskManager.MarketInstrumentInitialized to allow for proper setup up.
        /// </summary>
        public RiskManagerSpreader()
            : base()
        {
        }
        //
        //
        //       
        public override void SetupBegin(Lib.Engines.IEngineHub myEngineHub, Lib.Engines.IEngineContainer engineContainer)
        {
            base.SetupBegin(myEngineHub, engineContainer);
            foreach (IEngine iEng in engineContainer.GetEngines())
            { // Find order Engine
                if (iEng is Spreader)
                    this.m_Spreader = (Spreader)iEng;                                             // find our order engine
                if(iEng is HedgeManager)
                    this.m_HedgeManager = (HedgeManager)iEng;
            }
        }
        //
        //
        public override void SetupComplete()
        {
            base.SetupComplete();
            foreach (ExecutionEngines.OrderEngines.SpreaderLeg quoterLeg in m_Spreader.m_SpreaderLegs)
                base.m_MaxPossibleWorkingQuoteQtyPerLot += (int)(Math.Abs(quoterLeg.m_PriceLeg.Weight) * 2); // sum all leg ratios * 2 (each side)
        }
        #endregion//Constructors

        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// Number of ticks from opposite side we are able to send orders through.
        /// </summary>
        public override int FatFingerTicks
        {
            get { return m_FatFingerTicks; }
            set
            {
                if (RiskIsReady)
                { //we have a new value
                    m_FatFingerTicks = value; // set it
                    m_FatFingerPriceTolerance = m_Spreader.QuoteTickSize * value;
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
        //
        // *************************************************************
        // ****             MarketInstrumentInitialized()           ****
        // *************************************************************
        /// <summary>
        /// called once the market for all instruments is subscribed to and we have instrument
        /// details
        /// </summary>
        public override void Start()
        {
            RiskIsReady = true;                                         // set our state to ready
            FatFingerTicks = m_FatFingerTicks;                          // set our tolerances since we now have tick size
            m_Spreader.IsRiskCheckPassed = true;                          // let our order engine begin to send orders.
        }
        //
        #endregion//Public Methods

        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        //***********************************************************
        // ****             UpdateAndCheckSpreaderPnL()          ****
        //***********************************************************
        /// 
        /// <summary>
        /// Aggregate total PnL across all legs of the spread, and ensure we aren't below
        /// our allowed loss 
        /// </summary>
        /// <returns>true if we are below our maximum allowed loss</returns>
        protected override bool PnLCheck()
        {
            m_PnL = 0; // reset PnL to 0
            if (m_Spreader.m_IsLegSetupCompleted)
            {
                for (int i = 0; i < m_Spreader.m_LegFillBooks.Length; i++)
                {

                    double midPrice = (m_Spreader.m_SpreaderLegs[i].m_Market.Price[UV.Lib.Utilities.QTMath.BidSide][0] +
                        m_Spreader.m_SpreaderLegs[i].m_Market.Price[UV.Lib.Utilities.QTMath.AskSide][0]) / 2;

                    m_PnL += (m_Spreader.m_LegFillBooks[i].m_RealizedGain +              // (realized + unrealized) = PnL
                        m_Spreader.m_LegFillBooks[i].UnrealizedDollarGains(midPrice));
                }
                if (m_PnL < m_MaxLossPnL)
                {
                    m_Log.NewEntry(LogLevel.Error, "Max Loss PnL Has Been Exceeded: Current PnL = {0} MaxLossPnL = {1}", m_PnL, m_MaxLossPnL);
                    return true;
                }
            }
            return false;
        }
        //
        //
        //
        //
        //**************************************************************
        // ****             CheckLegPositions()                     ****
        //**************************************************************
        //
        /// <summary>
        /// Caller would like to check that our leg's positions are not in line with the parameters of the spreader.
        /// </summary>
        /// <returns>true if position in any given leg is larger than expected</returns>
        public bool CheckLegPositions()
        {
            // this is a collection of position sanity checks. They may be somewhat redundant and we may remove some once we are confident in logic elsewhere.
            bool isPositionLogical = true;
            for (int i = 0; i < m_Spreader.m_LegFillBooks.Length; i++)
            {// iterate through all fill books and make sure no leg is grossly out of whack
                long MaxSensicalLongQtyForLeg = (long)((Math.Abs(m_Spreader.m_LegRatios[i] * m_Spreader.m_TotalDesiredQty[UV.Lib.Utilities.QTMath.MktSignToMktSide(m_Spreader.m_LegRatios[i])])) +  // Qty * Ratio + Max Possibly Overfilled On
                    (m_Spreader.m_DripQty * (m_Spreader.m_LegRatios.Length - 1) * Math.Abs(m_Spreader.m_LegRatios[i]))); // if we are quoting all the legs and we get filled across everything this could happen

                long MaxSensicalShortQtyForLeg = (long)(((Math.Abs(m_Spreader.m_LegRatios[i] * m_Spreader.m_TotalDesiredQty[UV.Lib.Utilities.QTMath.MktSignToMktSide(m_Spreader.m_LegRatios[i] * -1)])) +  // Qty * Ratio + Max Possibly Overfilled On
                        (m_Spreader.m_DripQty * (m_Spreader.m_LegRatios.Length - 1) * Math.Abs(m_Spreader.m_LegRatios[i]))) * -1); // if we are quoting all the legs and we get filled across everything this could happen

                long ExpectedQtyForLeg = (long)(Math.Abs((m_Spreader.GetFillBook().m_NetPosition * m_Spreader.m_LegRatios[i])) +        // Our Spread Positon * LegRatio +
                    Math.Abs(m_Spreader.m_DripQty * m_Spreader.m_LegRatios[i] * m_Spreader.m_MaxAllowableHangs * 2) - 1);                                 // DripQty * LegRatio * # of PossibleHangs + quanity if we are partialled 

                if (m_Spreader.m_LegFillBooks[i].m_NetPosition > MaxSensicalLongQtyForLeg || m_Spreader.m_LegFillBooks[i].m_NetPosition < MaxSensicalShortQtyForLeg)
                { // our position exceeds the possible max 
                    m_Log.NewEntry(LogLevel.Error, "Our Position in {0} = {1} Which is outside our Max expected Long Qty of {2} or Short Qty of {3}",
                        m_Spreader.m_SpreaderLegs[i].m_PriceLeg.InstrumentName, m_Spreader.m_LegFillBooks[i].m_NetPosition, MaxSensicalLongQtyForLeg, MaxSensicalShortQtyForLeg);
                    isPositionLogical = false;  // set our bool to true stating risk should be triggered
                    break;
                }
                else if (Math.Abs(m_Spreader.m_LegFillBooks[i].m_NetPosition) > (ExpectedQtyForLeg))
                { // our position exceeds what we would logically expect.
                    m_Log.NewEntry(LogLevel.Error, "Our Position in {0} = {1} Which is greater than our expected Qty of {2} based on our sytnthetic position of {3}",
                       m_Spreader.m_SpreaderLegs[i].m_PriceLeg.InstrumentName, m_Spreader.m_LegFillBooks[i].m_NetPosition, ExpectedQtyForLeg, m_Spreader.GetFillBook().m_NetPosition);
                    isPositionLogical = false;  // set our bool to true stating risk should be triggered
                    break;
                }
            }
            return !isPositionLogical;  // the method wants to know if the postion doesn't make sense. so return the opposite of isPositionLogical.
        }
        //
        //
        //**************************************************************
        // ****           CheckWorkingOrderCounts()                 ****
        //**************************************************************
        //
        //
        /// <summary>
        /// Override specifically meant to deal with a spreader's orders.
        /// </summary>
        /// <returns></returns>
        protected override bool CheckWorkingOrderCounts()
        {
            long totalQuoteCount = 0;  // create variable to count our quote qty.
            long maxTotalWorkQuoteQty = m_MaxPossibleWorkingQuoteQtyPerLot * m_Spreader.m_DripQty; // this is assuming we are quoting both sides and all legs
            bool isTooManyWorkingOrders = false;

            for (int i = 0; i < m_Spreader.m_OrderBooks.Count; i++)
                totalQuoteCount += m_Spreader.m_OrderBooks[i].m_TotalWorkingOrderQty;     // sum all working quote orders in market for book 

            if (totalQuoteCount > maxTotalWorkQuoteQty)
            { // we are somehow quoting more than we should be. 
                m_Log.NewEntry(LogLevel.Error, "Total working qty in quote legs = {0} which is more than the expected max of = {1}",
                    totalQuoteCount, maxTotalWorkQuoteQty);
                isTooManyWorkingOrders = true;
            }

            long totalHedgeCount = 0;  // create variable to count our hedge qty.
            for (int i = 0; i < m_HedgeManager.m_Hedgers.Count; i++)
                totalHedgeCount += m_HedgeManager.m_Hedgers[i].m_OrderBook.m_TotalWorkingOrderQty;     // sum all working hedge orders in market for book 

            if (totalHedgeCount > maxTotalWorkQuoteQty)
            { // we have more outstanding hedge orders than we would should
                m_Log.NewEntry(LogLevel.Error, "Total working qty in hedge legs = {0} which is more than the expected max of = {1}",
                    totalHedgeCount, maxTotalWorkQuoteQty);
                isTooManyWorkingOrders = true;
            }
            return isTooManyWorkingOrders;
        }
        //        
        //
        //
        //**************************************************************
        // ****                FlagRiskEvents()                     ****
        //**************************************************************
        /// <summary>
        /// Caller would like to turn off all quoting, and turn the m_IsRiskTriggered to true.  This will allow for the next second for
        /// the strategy to completely stop.  Hopefully this allows for some hedging to occcur prior to the hard stop of all functionality.
        /// </summary>
        protected override void FlagRiskEvents()
        {
            m_IOrderEngine.IsRiskCheckPassed = false;
            ((Engine)m_IOrderEngine).BroadcastParameter((IEngineHub)m_Spreader.m_Hub, m_Spreader.m_ExecutionContainer, "IsRiskCheckPassed");
            m_IOrderEngine.CancelAllOrders();
            m_IsRiskTriggered = true;
            m_Spreader.OnQuoterStateChange();                 // push to quoter and then to legs.
            UV.Lib.FrontEnds.Utilities.GuiCreator.ShowMessageBox(string.Format("{0} : Risk Event Triggered! Trading is turning off.", m_Spreader.m_SyntheticSpreadFillBook.Instrument), "Risk Triggered");
        }

        #endregion//Private Methods

        #region Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                      ****
        // *****************************************************************
        //
        // ***********************************************************
        // ****               OrderBook_OrderFilled()             ****
        // ***********************************************************
        //
        public override void OrderBook_OrderFilled(object sender, EventArgs eventArgs)
        {
            FillEventArgs fillEventArgs = (FillEventArgs)eventArgs;
            Fill fill = fillEventArgs.Fill;
            if (UpdateAndCheckTotalFillCounts(fill) | PnLCheck() | CheckLegPositions())
            { // Take the fill and update our counts and PnL, checking both.
                FlagRiskEvents();
            }
        }
        //
        //
        #endregion//Event Handlers
        
        #region ITimerSubscriber Implementation
        public override void TimerSubscriberUpdate()
        {
            if (PnLCheck() | CheckQuoteLimits())    // max loss pnl tripped or max number of quotes per second exceeded
                FlagRiskEvents();
            m_SecondCount++;                    // increment our second counter
            if (m_IsRiskTriggered)
            {
                m_RiskCount++;
                if (m_RiskCount == 2)
                {
                    // TODO : add ability for user to reset
                    m_Log.NewEntry(LogLevel.Error, "Risk Event Triggered! Pausing Strategy");
                }
            }
            m_TotalNumberOfQuotes += m_NumberOfQuotesThisSecond; // Add to total's count.
            m_NumberOfQuotesThisSecond = 0; // reset count each second.
            base.BroadcastParameter((IEngineHub)m_Spreader.m_Hub, m_Spreader.m_ExecutionContainer, 4);
            base.BroadcastParameter((IEngineHub)m_Spreader.m_Hub, m_Spreader.m_ExecutionContainer, 5);
        }
        #endregion
    }
}
