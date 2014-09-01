using System;
using System.Collections.Generic;
using System.Text;


namespace UV.Strategies.ExecutionEngines.OrderEngines
{
    using UV.Lib.OrderBooks;
    using UV.Lib.BookHubs;
    using UV.Lib.IO.Xml;
    using UV.Lib.Application;
    using UV.Lib.Engines;
    using UV.Lib.Fills;
    using UV.Lib.Products;
    using UV.Lib.Hubs;
    using UV.Lib.Utilities;

    using UV.Strategies;
    using UV.Strategies.StrategyEngines;

    using UV.Strategies.ExecutionEngines.Risk;
    using UV.Strategies.ExecutionHubs;
    using UV.Strategies.ExecutionEngines.Hedgers;
    /// <summary>
    /// The quoter is the most basic version of an fully implemented spreader.  
    /// It will create all the necessary objects and helper classes to leg into a spread.
    /// </summary>
    public class Quoter : Engine, IStringifiable
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // External services:
        public HedgeManager m_HedgeManager;
        public ExecutionEngines.SpreaderFills.SpreaderFillGenerator m_SpreadFillGenerator = null;
        internal RiskManagerQuoter m_RiskManager = null;
        public IExecutionListener m_IExecutionListener = null;
        public ExecutionContainer m_ExecutionContainer = null;
        public Hub m_Hub = null;
        public LogHub m_Log = null;

        // Strategy variables
        protected double[] m_StrategyMarketPrice = new double[2];
        internal double[] m_StrategyWorkingPrice = new double[2];
        internal int[] m_TotalDesiredQty = new int[2];                                          // desired qty on each side of the market (signed)
        internal int m_DripQty = 1;
        internal int m_MaxAllowableHangs = 1;                                                   // not yet implemented, but we want to be able to be hung on more than 1 
        public bool m_UseGTCHedge = false;

        // lookup tables and collections 
        public Dictionary<InstrumentName, int> m_InstrumentToInternalId = new Dictionary<InstrumentName, int>();    // map: InstrumentName --> legId of the quoter.
        public List<QuoterLeg> m_QuoterLegs = new List<QuoterLeg>();                            // list of legs in our spread.
        protected List<InstrumentName> m_Instruments = new List<InstrumentName>();              // map: internallegId --> Instrument
        internal Dictionary<int, OrderBook> m_OrderBooks = new Dictionary<int, OrderBook>();    // map: legId --> OrderBook

        protected Order[][] m_PendingQuoteOrders;                                               // order not yet submitted [legId][legSide]
        private bool[][] m_IsQuotingLeg;
        internal FillBook[] m_LegFillBooks;                                                     // array of fill books indexed by legs
        internal double[] m_LegRatios;
        protected double[] m_LegPriceMultipliers;
        protected int[][] m_QuoteFillCount;                                                     // array for each leg and sides fill count for quotes
        protected int[][] m_QuotePartialCount;                                                  // duplicate array with actual fractional partial counts.       
        protected double[][] m_QuotingLegPrices;                                                // array for each leg and side of the prices we want to be quoting at
        protected bool[][] m_IsLegLeanable;                                                     // array for each leg and side for leg being leanable (has sufficient qty in book)

        // Variables for the spread.
        protected int[] m_SpreaderPos = new int[2];                                             // array for position on both sides
        private bool m_IsAllLegsGood = new bool();                                              // when all the market states of all legs are good, this is true.
        private FillBook m_SyntheticSpreadFillBook;                                             // Fill book just for the spread fills
        private double m_QuoteTickSize = double.NaN;

        // state flags for Quoter.
        public bool[] m_IsPartialFilled = new bool[2];                                          // flag for strategy having partial quote fills
        public bool[] m_QuoteSpreadSide = new bool[2];                                          // flag for being able to conitnue quoting a strategy side based on state.   
        private bool[][] m_IsQuotingLegPriceOffMarket;                                          // array for each leg and side for state of quote order.
        public bool m_IsLegSetupCompleted = false;                                              // true once strategy knows about all details for legs
        private bool m_IsRiskCheckPassed;
        private bool m_IsUserTradingEnabled = true;                                             // temp set to true, needs to be set back prior to prod
        private int nInstrumentsFoundCount;                                                     // internal count of instruments found.

        // temporary work spaces
        private List<Order> m_OrderWorkSpace = new List<Order>();                               // workspace for temp order storage-recycle after each use
        #endregion// members

        #region Constructors
        // *****************************************************************
        // ****               Constructors And Intializtion             ****
        // *****************************************************************
        //
        public Quoter()
            : base()
        {
        }
        //
        //
        ///
        protected override void SetupInitialize(Lib.Engines.IEngineHub myEngineHub, Lib.Engines.IEngineContainer engineContainer, int engineID, bool setupGui)
        {
            base.SetupInitialize(myEngineHub, engineContainer, engineID, setupGui);
            m_Hub = (Hub)myEngineHub;
            m_ExecutionContainer = (ExecutionContainer)engineContainer;
            m_Log = m_Hub.Log;

            //
            // add sub engines to container
            //
            foreach (QuoterLeg quoterLeg in m_QuoterLegs)
            { // add legs to container
                m_ExecutionContainer.TryAddEngine(quoterLeg);
            }
            m_ExecutionContainer.TryAddEngine(m_HedgeManager);

            //
            // Set Up
            //
            m_PendingQuoteOrders = new Order[m_QuoterLegs.Count][];                 // user-set orders for each [leg] and [side], not yet submitted.
            m_IsQuotingLeg = new bool[m_QuoterLegs.Count][];                        // user-set flag for quoting legs.

            m_LegFillBooks = new FillBook[m_QuoterLegs.Count];                      // fill books for each leg
            m_LegRatios = new double[m_QuoterLegs.Count];                           // array for leg ratios
            m_LegPriceMultipliers = new double[m_QuoterLegs.Count];                 // array for leg Price Multipliers
            m_IsAllLegsGood = false;                                                // start assuming all legs are in a "bad" state.
            m_TotalDesiredQty[0] = 0;                                               // default desired Qty to 1 on each side
            m_TotalDesiredQty[1] = 0;

            m_QuoteFillCount = new int[m_QuoterLegs.Count][];                        // array for each leg and side of quote fills
            m_QuotePartialCount = new int[m_QuoterLegs.Count][];                     // duplicate array for partials fills
            m_IsQuotingLegPriceOffMarket = new bool[m_QuoterLegs.Count][];           // array of off market states.
            m_QuotingLegPrices = new double[m_QuoterLegs.Count][];
            m_IsLegLeanable = new bool[m_QuoterLegs.Count][];
        }
        //
        //
        public override void SetupBegin(Lib.Engines.IEngineHub myEngineHub, Lib.Engines.IEngineContainer engineContainer)
        {
            base.SetupBegin(myEngineHub, engineContainer);                      // call base class
            //
            // Subscribe to events
            //
            m_IExecutionListener.InstrumentFound += new EventHandler(ExecutionListener_InstrumentsFound);

            //
            // Find all needed pointers to engines
            //
            foreach (IEngine iEng in engineContainer.GetEngines())      // find the user created risk manager
                if (iEng is RiskManagerQuoter)
                    m_RiskManager = (RiskManagerQuoter)iEng;
                else if (iEng is HedgeManager)
                    m_HedgeManager = (HedgeManager)iEng;


            if (m_RiskManager == null)
                throw new NotImplementedException("All Strategies Must Have a Risk Manager, Please Add One To Your User Config - Must Be UV.Execution.Risk.RiskManagerQuoter type");
            if (m_HedgeManager == null)
                throw new NotImplementedException("All Quoters Must Have a Hedge Manager, Please Add One To Your User Config - Must Be UV.Execution.HedgeManager type");

            //
            // Set up all legs 
            //
            string spreadName = "";                                     // synthetic spread name for reporting spread fills
            for (int leg = 0; leg < m_QuoterLegs.Count; ++leg)
            {
                QuoterLeg spreadLeg = m_QuoterLegs[leg];
                spreadLeg.LeanablePriceChanged += new EventHandler(Instrument_LeanablePriceChanged);    // subscribe to pricing updates.
                spreadLeg.MarketStateChanged += new EventHandler(Instrument_MarketStateChanged);        // subscribe to market state changes.
                spreadLeg.ParameterChanged += new EventHandler(Instrument_ParameterChanged);            // param changes need to make the quoter update.

                spreadLeg.UpdateDripQty(1);
                m_Instruments.Add(spreadLeg.m_PriceLeg.InstrumentName);

                OrderBook orderBook = m_IExecutionListener.CreateOrderBook(spreadLeg.m_PriceLeg.InstrumentName);
                m_OrderBooks.Add(leg, orderBook);
                orderBook.OrderFilled += new EventHandler(OrderBook_OrderFilled);
                orderBook.OrderStateChanged += new EventHandler(OrderBook_OrderStateChanged);

                FillBook fillBook = new FillBook(spreadLeg.m_PriceLeg.InstrumentName.ToString(), 0);    // Multiplier will need to be set once we have instrument details!
                m_LegFillBooks[leg] = fillBook;                                                         // add book to array to pass to spread fill generator

                m_IsQuotingLeg[leg] = spreadLeg.m_QuotingEnabled;
                m_PendingQuoteOrders[leg] = new Order[2];                                               // 2-sides of the mkt
                m_QuotingLegPrices[leg] = new double[2];
                m_IsQuotingLegPriceOffMarket[leg] = new bool[2];
                m_IsLegLeanable[leg] = new bool[2];

                m_LegRatios[leg] = spreadLeg.m_PriceLeg.Weight;                                         // add ratio and multipliers to array to pass to spread fill generator
                m_LegPriceMultipliers[leg] = spreadLeg.m_PriceLeg.PriceMultiplier;

                m_QuoteFillCount[leg] = new int[2];                                                     // Quote Fill counts to start are 0 for bid side  
                m_QuotePartialCount[leg] = new int[2];                                                  // Quote partial counts to start are 0 for bid side  

                if (leg != (m_QuoterLegs.Count - 1))  //append spread name
                    spreadName += (spreadLeg.m_PriceLeg.InstrumentName.FullName + ".");
                else
                    spreadName += spreadLeg.m_PriceLeg.InstrumentName.FullName;
            }
            m_SyntheticSpreadFillBook = new FillBook(spreadName, 0);                                    // create synthetic fill book for our synthetics fills. no mult.
            m_SpreadFillGenerator = new ExecutionEngines.SpreaderFills.SpreaderFillGenerator(                            // instantiate the spread fill generator passing an array of legRatios, and an array of books to it.
                spreadName, m_LegRatios, m_LegPriceMultipliers, m_LegFillBooks);
        }
        //
        //
        //
        #endregion//Constructors

        #region Properties
        // *****************************************************************
        // ****                       Properties                        ****
        // *****************************************************************
        //
        //
        public int TotalDesiredBuyQty
        {
            get { return m_TotalDesiredQty[Order.BuySide]; }
            set
            {
                if (m_RiskManager.ValidatePrices(m_StrategyWorkingPrice[Order.BuySide],
                    Order.BuySide, m_StrategyMarketPrice))
                {
                    m_TotalDesiredQty[Order.BuySide] = Math.Abs(value);
                    OnQuoterStateChange();
                }
            }
        }
        public int TotalDesiredSellQty
        {
            get { return m_TotalDesiredQty[Order.SellSide]; }
            set
            {
                if (m_RiskManager.ValidatePrices(m_StrategyWorkingPrice[Order.SellSide],
                    Order.SellSide, m_StrategyMarketPrice))
                {
                    m_TotalDesiredQty[UV.Lib.Utilities.QTMath.AskSide] = Math.Abs(value) * -1;        /// sell qty must always be negative
                    OnQuoterStateChange();
                }
            }
        }
        public int DripQty
        {
            get { return m_DripQty; }
            set
            {
                m_DripQty = value;
                OnQuoterStateChange();
            }
        }
        public double WorkPriceBuy
        {
            get { return m_StrategyWorkingPrice[Order.BuySide]; }
            set
            {
                if (m_RiskManager.ValidatePrices(value, Order.BuySide, m_StrategyMarketPrice))
                {// validate our buy price before setting
                    m_StrategyWorkingPrice[Order.BuySide] = value;
                    OnQuoterStateChange();
                }
            }
        }
        public double WorkPriceSell
        {
            get { return m_StrategyWorkingPrice[UV.Lib.Utilities.QTMath.AskSide]; }
            set
            {
                if (m_RiskManager.ValidatePrices(value, Order.SellSide, m_StrategyMarketPrice))
                {// validate our sell price before setting
                    m_StrategyWorkingPrice[Order.SellSide] = value;
                    OnQuoterStateChange();
                }
            }
        }
        /// <summary>
        /// Minimum increment the price should be able to be changed by in our synthetic spread.
        /// </summary>
        public double QuoteTickSize
        {
            get { return m_QuoteTickSize; }
            set { m_QuoteTickSize = value; }
        }
        //
        public bool IsRiskCheckPassed
        {
            get { return m_IsRiskCheckPassed; }
            set { m_IsRiskCheckPassed = value; }
        }
        //
        public FillBook FillBook
        {
            get { return m_SyntheticSpreadFillBook; }
            set { m_SyntheticSpreadFillBook = value; }
        }
        //
        /// <summary>
        /// User defined flag for allow order submission
        /// </summary>
        public bool IsUserTradingEnabled
        {
            get { return m_IsUserTradingEnabled; }
            set
            {
                m_IsUserTradingEnabled = value;
                OnQuoterStateChange();
            }
        }
        #endregion //Properties

        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        public override void ProcessEvent(EventArgs e)
        {
            base.ProcessEvent(e);
            if (e is TradeEventArgs)
            {
                TradeEventArgs tradeEvent = (TradeEventArgs)e;
                Quote(tradeEvent.TradeSide, tradeEvent.TargetPrice, tradeEvent.TargetQty);
            }
            else if (e is RequestEventArg<UV.Strategies.ExecutionHubs.RequestCode>)
            { // we have a request to change states.
                RequestEventArg<UV.Strategies.ExecutionHubs.RequestCode> reqEvent = (RequestEventArg<UV.Strategies.ExecutionHubs.RequestCode>)e;
                ServiceStates requestedState = (ServiceStates)reqEvent.Data[0];// get state requested
                switch (requestedState)
                {
                    case ServiceStates.Running:
                        this.Start();
                        break;
                    case ServiceStates.Stopping:
                        this.Stop();
                        break;
                    default:
                        break;
                }
                //TODO: recycle?
            }
        }

        // *************************************************************
        // ****                 AddExecutionListener                ****
        // *************************************************************
        //
        /// <summary>
        /// Called once after SetUpIntialize and before start.
        /// </summary>
        public void AddExecutionListener(IExecutionListener execListener)
        {
            m_IExecutionListener = execListener;
            m_IExecutionListener.ExecutionContainer = m_ExecutionContainer;      // assign my execution container to the listener 
        }
        //
        //
        //
        //
        // *****************************************************************
        // ****                OnQuoterStateChange()                    ****
        // *****************************************************************
        /// <summary>
        /// Method will Update m_QuoteSpreadSide and then call UpdateQuotingBools()
        /// to pass on state's to each leg and side. 
        /// </summary>
        public void OnQuoterStateChange()
        {
            OnQuoterStateChange(UV.Lib.Utilities.QTMath.BidSide);
            OnQuoterStateChange(UV.Lib.Utilities.QTMath.AskSide);
        }
        //
        //
        // *****************************************************************
        // ****                 Add Hedge Fill()                        ****
        // *****************************************************************
        public virtual ExecutionEngines.SpreaderFills.SyntheticFill AddHedgeFill(int stratSide, int legId, Fill fill)
        {
            // Add to Fill and test for trade completion.
            m_LegFillBooks[legId].TryAdd(fill);
            ExecutionEngines.SpreaderFills.SyntheticFill newSyntheticFill;
            if (m_SpreadFillGenerator.TryGenerateSyntheticFill(stratSide, out newSyntheticFill))// find out if we now have sufficient qty to create a synthetic
            {
                m_SpreaderPos[UV.Lib.Utilities.QTMath.MktSignToMktSide(newSyntheticFill.Qty)] += newSyntheticFill.Qty;
                m_SyntheticSpreadFillBook.TryAdd(newSyntheticFill);         // add fill to list                
                m_Log.NewEntry(LogLevel.Major, string.Format("         {0} - {1}", m_SyntheticSpreadFillBook.Instrument, newSyntheticFill));
                OnQuoterStateChange(UV.Lib.Utilities.QTMath.MktSignToMktSide(newSyntheticFill.Qty));
                return newSyntheticFill;
            }
            return null;
        }//AddHedgeFill()
        //
        #endregion//Public Methods

        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        // *************************************************************
        // ****                        Start                        ****
        // *************************************************************
        //
        /// <summary>
        /// Start will be called by the execution hub thread.  At this point we
        /// will spin off our own thread that will be the only thread used from now on.
        /// </summary>
        private void Start()
        {
            m_IExecutionListener.Initialized += new EventHandler(ExecutionListener_Initialized);
            m_IExecutionListener.Stopping += new EventHandler(ExecutionListener_Stopping);
            System.Threading.Thread thread = new System.Threading.Thread(m_IExecutionListener.InitializeThread);
            thread.Name = this.GetType().Name.ToString();
            thread.Start();
        }
        //
        //
        /// <summary>
        /// Threadsafe call to being shutdown procedures.
        /// </summary>
        private void Stop()
        {
            m_IExecutionListener.StopThread();
        }
        //
        //
        // *****************************************************
        // ****        UpdateQuotingLegPrices()             ****
        // *****************************************************
        //
        /// <summary>
        /// Caller would like to update all the quote leg prices for instruments we are actively quoting.
        /// </summary>
        /// <param name="strategySide"></param>
        /// <param name="changedLegID"></param>
        protected virtual void UpdateQuotingLegPrices(int strategySide, int changedLegID)
        {
            int strategySign = UV.Lib.Utilities.QTMath.MktSideToMktSign(strategySide);              // side of the strategy we are looking at
            for (int quoteLegID = 0; quoteLegID < m_Instruments.Count; ++quoteLegID)                // iterate through all legs
            {
                if (quoteLegID != changedLegID)                                                     // if it isn't the quote leg we want to update the pricing
                {
                    QuoterLeg quoteLeg = m_QuoterLegs[quoteLegID];
                    int quoteLegSide = UV.Lib.Utilities.QTMath.MktSignToMktSide(strategySign * Math.Sign(quoteLeg.m_PriceLeg.PriceMultiplier));  // what side is the quote on in this instr.
                    double quoteLegPrice = m_StrategyWorkingPrice[strategySide];                    // start with the entire spread price and we will decrement from there 
                    if (m_QuoterLegs[quoteLegID].m_QuotingEnabled[quoteLegSide])                    // only need to do this for instruments that we are quoting 
                    {
                        for (int hedgeLegID = 0; hedgeLegID < m_Instruments.Count; ++hedgeLegID)    //so for every hedge leg 
                        {
                            if (quoteLegID != hedgeLegID)
                            {
                                QuoterLeg hedgeLeg = m_QuoterLegs[hedgeLegID];                      // get the hedge instrument
                                int hedgeLegSide = UV.Lib.Utilities.QTMath.MktSignToMktSide(-strategySign * Math.Sign(hedgeLeg.m_PriceLeg.PriceMultiplier));
                                if (!hedgeLeg.IsMarketGood || !hedgeLeg.m_IsMarketLeanable[hedgeLegSide])
                                {// make sure we have a reasonable market
                                    break;
                                }
                                else
                                {
                                    quoteLegPrice -= hedgeLeg.m_PriceLeg.PriceMultiplier * hedgeLeg.m_LeanablePrices[hedgeLegSide]; // and find the price we can get for that instrument
                                    if (double.IsNaN(quoteLegPrice))
                                        break;
                                }
                            }
                        }
                        quoteLegPrice = quoteLegPrice / quoteLeg.m_PriceLeg.PriceMultiplier;
                        quoteLegPrice = UV.Lib.Utilities.QTMath.RoundPriceSafely(quoteLegPrice, quoteLegSide,   // this will round up for ask down for bid
                                                                                 quoteLeg.InstrumentDetails.TickSize);
                        m_QuotingLegPrices[quoteLegID][quoteLegSide] = quoteLegPrice;                           // save to our array of quote prices.
                    }
                }
            }
        }
        //
        //
        // *************************************************************
        // ****              UpdateMarketPrice()                    ****
        // *************************************************************
        /// <summary>
        /// Computes the current Bid/Ask of the strategy.
        /// </summary>
        protected virtual void UpdateMarketPrice()
        { // Calculate the market for this strategy.
            double previousBidPrice = m_StrategyMarketPrice[UV.Lib.Utilities.QTMath.BidSide];       // save our previous bid and ask prices 
            double previousAskPrice = m_StrategyMarketPrice[UV.Lib.Utilities.QTMath.AskSide];
            m_StrategyMarketPrice[UV.Lib.Utilities.QTMath.BidSide] = 0;
            m_StrategyMarketPrice[UV.Lib.Utilities.QTMath.AskSide] = 0;
            for (int i = 0; i < m_Instruments.Count; i++)
            {
                QuoterLeg instr = m_QuoterLegs[i];
                for (int stratSide = 0; stratSide < 2; ++stratSide)
                {
                    int legSide = UV.Lib.Utilities.QTMath.MktSignToMktSide(instr.m_PriceLeg.PriceMultiplier * UV.Lib.Utilities.QTMath.MktSideToMktSign(stratSide));
                    m_StrategyMarketPrice[stratSide] += (instr.m_Market.Price[legSide][0] * instr.m_PriceLeg.PriceMultiplier);
                }
            }
            if ((m_StrategyMarketPrice[UV.Lib.Utilities.QTMath.BidSide] != previousBidPrice) ||
                (previousAskPrice != m_StrategyMarketPrice[UV.Lib.Utilities.QTMath.AskSide]))   // if our prices have changed we need to fire an event to our subscribers/
                OnSpreadPriceChanged();
        }//UpdateMarketPrice()
        //
        //
        // *****************************************************************
        // ****                UpdateQuotingLegBools()                  ****
        // *****************************************************************
        /// <summary>
        /// Method weill update each legs and side ability to quote. and then call UpdateAllWorkingLegs();
        /// to ensure we are quoting correctly. 
        /// </summary>
        private void UpdateQuotingLegBools()
        {
            for (int strategySide = 0; strategySide < 2; strategySide++)
            {
                int strategySign = QTMath.MktSideToMktSign(strategySide);
                int oppSign = strategySign * -1;
                int oppSide = QTMath.MktSignToMktSide(oppSign);
                for (int quoteLegId = 0; quoteLegId < m_QuoterLegs.Count; ++quoteLegId)
                {
                    // if this leg ratio is positive & we are working the long strat side, turn on bid side quoting
                    // if this leg ratio is negative & we are working the short strat side, turn on bid side quoting  
                    if (((m_QuoterLegs[quoteLegId].m_PriceLeg.Weight > 0 && m_QuoteSpreadSide[strategySide] && m_TotalDesiredQty[strategySide] != 0) ||
                          (m_QuoterLegs[quoteLegId].m_PriceLeg.Weight < 0 & m_QuoteSpreadSide[oppSide] && m_TotalDesiredQty[oppSide] != 0))
                          && m_QuoterLegs[quoteLegId].UserDefinedQuotingEnabled && m_IsRiskCheckPassed && m_IsUserTradingEnabled && m_IsLegSetupCompleted)
                    {
                        m_QuoterLegs[quoteLegId].m_QuotingEnabled[strategySide] = true;
                        // 
                        // Need to check all legs to make sure they are leanable before we can enable quoting on this side.
                        //
                        for (int hedgeLegID = 0; hedgeLegID < m_Instruments.Count; ++hedgeLegID)    //so for every hedge leg 
                        {
                            if (quoteLegId != hedgeLegID)
                            {
                                QuoterLeg hedgeLeg = m_QuoterLegs[hedgeLegID];                      // get the hedge instrument
                                int hedgeLegSide = UV.Lib.Utilities.QTMath.MktSignToMktSide(-strategySign * Math.Sign(hedgeLeg.m_PriceLeg.PriceMultiplier));
                                if (!hedgeLeg.m_IsMarketLeanable[hedgeLegSide])
                                {// there isn't sufficient qty in the book to lean on
                                    m_QuoterLegs[quoteLegId].m_QuotingEnabled[strategySide] = false;  // we cannot quote this leg on this side
                                    m_Log.NewEntry(LogLevel.Error, "{0} Quoting is being turned off on the {1} due to not being able to lean on {2}",
                                        m_QuoterLegs[quoteLegId].InstrumentDetails.InstrumentName,
                                        Order.BuySide,
                                        hedgeLeg.InstrumentDetails.InstrumentName);
                                    break;
                                }
                            }
                        }
                    } // end if 
                    else
                    { // we aren't quoting this instrument on this side.
                        m_QuoterLegs[quoteLegId].m_QuotingEnabled[strategySide] = false;
                    }
                } // end quote leg for loop
            } //end for stratside
            UpdateWorkingLegs();
        }
        //
        //
        // *****************************************************
        // ****        OnQuoterStateChange()                ****
        // *****************************************************
        //
        //  Decision Tree for Quoter based on states.
        //                       Hung               Hung
        //                       TRUE        |      FALSE
        // __________________________________|_____________________________                  
        // |                 |Keep Quoting   |        Keep Quoting  
        // |        TRUE     |Keep WorkingQTY|         Keep Working QTY       
        //_|____partialled___|_______________|______________________________
        // |                 |Dont Quote     |Reload based on Pos Logic
        // |       FALSE     |Pull Same Side |
        //_|____partialled___|_______________|__________________________
        //
        /// <summary>
        /// Method will Update m_QuoteSpreadSide and then call UpdateQuotingBools()
        /// to pass on state's to a side.
        /// </summary>
        /// <param name="stratSideChanged"></param>
        protected void OnQuoterStateChange(int stratSideChanged)
        {
            if (m_HedgeManager.m_IsSratHung[stratSideChanged])
            { // hedger is in a hung state
                if (m_IsPartialFilled[stratSideChanged])
                { // we are still partialled however so continue to quote
                    m_QuoteSpreadSide[stratSideChanged] = true;
                }
                else
                { // if we aren't partialled stop quoting 
                    m_QuoteSpreadSide[stratSideChanged] = false;
                }
            }
            else
            { //  we are not hung 
                if (m_IsPartialFilled[stratSideChanged])
                { // we are partialled continue to quote
                    m_QuoteSpreadSide[stratSideChanged] = true;
                }
                else
                { //  we are not hung and not partialled, so as long as position makes sense quote on.
                    if (Math.Abs(m_SpreaderPos[stratSideChanged]) < Math.Abs(m_TotalDesiredQty[stratSideChanged]))
                        m_QuoteSpreadSide[stratSideChanged] = true;
                    else
                        m_QuoteSpreadSide[stratSideChanged] = false;
                }
            }
            UpdateQuotingLegBools();        // our quoter state is now defined, and now we need to update the legs from this info.
        }//OnQuoterStateChange()
        //
        //
        // *************************************************************
        // ****              UpdateWorkingLegs()                    ****
        // *************************************************************
        /// <summary>
        /// Caller has changed the spread price and wants to update all legs now.
        /// </summary>
        /// <param name="strategySide"></param>
        /// <param name="strategyPrice"></param>
        private void UpdateWorkingLegs()
        {
            UpdateQuotingLegPrices(UV.Lib.Utilities.QTMath.BidSide, -1);        // update the prices on the bid side 
            UpdateQuotingLegOrders(UV.Lib.Utilities.QTMath.BidSide, -1);        // -1 means that we will update all legs.
            UpdateQuotingLegPrices(UV.Lib.Utilities.QTMath.AskSide, -1);        // update the prices on the ask side.
            UpdateQuotingLegOrders(UV.Lib.Utilities.QTMath.AskSide, -1);        // -1 means that we will update all legs.

        }// UpdateAllWorkingLegs()
        //
        //
        // *****************************************************
        // ****        CheckOffMarketQuote()                ****
        // *****************************************************
        //
        /// <summary>
        /// quick check to see if a give quote is in the on or off marekt state
        /// </summary>
        /// <param name="quoteLeg"></param>
        /// <param name="quoteLegSide"></param>
        /// <param name="quoteLegPrice"></param>
        /// <returns>true if off market or if any prices are NaN</returns>
        private bool CheckOffMarketQuote(int quoteLeg, int quoteLegSide, double quoteLegPrice)
        {
            if (double.IsNaN(m_QuoterLegs[quoteLeg].m_Market.Price[quoteLegSide][0]) || double.IsNaN(m_QuoterLegs[quoteLeg].m_OffMarketPriceDifference))
                return true;
            else
                return Math.Abs(m_QuoterLegs[quoteLeg].m_Market.Price[quoteLegSide][0] - quoteLegPrice) > m_QuoterLegs[quoteLeg].m_OffMarketPriceDifference;
        }
        //
        // *****************************************************
        // ****        UpdateQuotingLegOrders()             ****
        // *****************************************************
        /// <summary>
        /// Updates the quoting prices of all legs that are leaning on the Instrument
        /// with index "changedLeg" for a given strategy side.  This most likely shouldn't be called
        /// without updatin the quote leg prices first. 
        /// </summary>
        /// <seealso cref="UpdateQuotingLegPrices()"/>
        /// <param name="strategySide"></param>
        /// <param name="changedLeg">Index of the leg that changed price.</param>
        private void UpdateQuotingLegOrders(int strategySide, int changedLeg)
        {
            for (int quoteLeg = 0; quoteLeg < m_Instruments.Count; ++quoteLeg)
            { //iterate through each leg and update the quote order 
                if (quoteLeg != changedLeg)
                    UpdateQuoteOrder(quoteLeg, strategySide);
            }
        }
        //
        //
        //
        //
        // *****************************************************
        // ****              UpdateQuoteOrder()             ****
        // *****************************************************
        //
        /// <summary>
        /// Update the quote order for a single leg and side. Called for its specificty.
        /// This will also set the flags for wether or not our quote is on or off market.
        /// </summary>
        /// <param name="quoteLegId"></param>
        /// <param name="strategySide"></param>
        private void UpdateQuoteOrder(int quoteLegId, int strategySide)
        {   //
            // Iterate through each quoting leg, recomputing its quote price/qty
            //
            int strategySign = UV.Lib.Utilities.QTMath.MktSideToMktSign(strategySide);
            QuoterLeg quoteLeg = m_QuoterLegs[quoteLegId];
            OrderBook quoteOrderBook = m_OrderBooks[quoteLegId];
            int quoteLegSide = UV.Lib.Utilities.QTMath.MktSignToMktSide(strategySign * Math.Sign(quoteLeg.m_PriceLeg.PriceMultiplier)); // side of market the quote is on 
            double quoteLegPrice = m_QuotingLegPrices[quoteLegId][quoteLegSide];                                // new price we want to be quoting
            bool isQuoteLeg = m_QuoterLegs[quoteLegId].m_QuotingEnabled[quoteLegSide];                          // global flag to see if we should be quoting this leg on this side.
            //
            // Collect all living quote orders for this legToQuote.
            //
            m_OrderWorkSpace.Clear();
            m_OrderBooks[quoteLegId].GetOrdersByRank(quoteLegSide, 0, ref m_OrderWorkSpace);
            Order quoteOrder = null;
            int nthOrder = 0;
            while (nthOrder < m_OrderWorkSpace.Count && quoteOrder == null)                                     // loop until we find a living quoteOrder
            {
                if (m_OrderWorkSpace[nthOrder].OrderStateConfirmed != OrderState.Dead)
                    quoteOrder = m_OrderWorkSpace[nthOrder];
                nthOrder++;
            }

            bool isNeedToDeleteOrder = !isQuoteLeg;                                                             // create flag for need to delete our quote order 
            bool isNeedToUpdateOrder = true;                                                                    // create flag for need to update our quote order

            if (isQuoteLeg)
            { // if we are quoting this leg 
                //
                // determine off market behavior
                //
                if (quoteLeg.OffMarketQuoting != UV.Strategies.ExecutionEngines.OrderEngines.OffMarketQuoteBehavior.Quote)
                { // if we are doing something besides always quoting
                    // create flags for our past state and the state we are going into once we update here.
                    bool isOldQuoteOffMarket = (quoteOrder == null || CheckOffMarketQuote(quoteLegId, quoteLegSide, (double)quoteOrder.PriceConfirmed));
                    bool isNewQuoteOffMarket = CheckOffMarketQuote(quoteLegId, quoteLegSide, quoteLegPrice);

                    if (quoteLeg.OffMarketQuoting == UV.Strategies.ExecutionEngines.OrderEngines.OffMarketQuoteBehavior.NoQuote && isNewQuoteOffMarket)
                    {   // if we want to not quote at all off market and our new quote if off market we want to make sure to delete any outstanding quote orders, and not submit anything new
                        isNeedToDeleteOrder = true;                                                                     // flag for deletion
                        m_IsQuotingLegPriceOffMarket[quoteLegId][quoteLegSide] = isNewQuoteOffMarket;                   // flag our off market state
                    } // end dealing with No Quote behavior
                    else if (quoteLeg.OffMarketQuoting == UV.Strategies.ExecutionEngines.OrderEngines.OffMarketQuoteBehavior.NoUpdate)
                    { // // if we don't want to update our quote order if we are off market, 
                        if (isOldQuoteOffMarket == isNewQuoteOffMarket && isNewQuoteOffMarket)
                        {   // we were previously and currently in an off market state, we don't need to update our order.
                            if (quoteOrder != null)                                                                     // if we already have a quote order
                                isNeedToUpdateOrder = false;                                                            // no need to update
                            m_IsQuotingLegPriceOffMarket[quoteLegId][quoteLegSide] = isNewQuoteOffMarket;               // this is probably redundant but go ahead and reset the flag 
                        }
                        else if (!isOldQuoteOffMarket && isNewQuoteOffMarket)
                        {// our old quote wasn't off the market, but our new quote now is off the market 
                            isNeedToUpdateOrder = true; // we need to update the old quote to get it off market(this is redundant since it is already set as true, but just writing it out for readability now)
                            m_IsQuotingLegPriceOffMarket[quoteLegId][quoteLegSide] = isNewQuoteOffMarket;             // set our flag for our updated order
                        }
                    } // end of dealing with No Update behavior.
                } // end of all off market behvaior logic
                //
                // check quote order to make sure we are working correct price and qty
                //
                if (isNeedToUpdateOrder && !isNeedToDeleteOrder)
                { // we need to update or ourder and we don't want to simply delete it.
                    int iWorkingLegPrice = (int)(quoteLegPrice / quoteLeg.InstrumentDetails.TickSize);                  // integer price to quote.
                    int workingLegQty = (((int)quoteLeg.m_PriceLeg.Weight *                                             // signed qty to quote. Need to update quantity to account for partials, etc.
                        UV.Lib.Utilities.QTMath.CalculateDripQty(m_DripQty,
                        m_TotalDesiredQty[strategySide], m_SpreaderPos[strategySide])) -
                        m_QuotePartialCount[quoteLegId][quoteLegSide]);                                                 // we want to subtract any unhedged quotes we have outstanding
                    // Submit orders
                    if (quoteOrder == null)
                    {   // We are currently working nothing on this side. So submit a new order.                        
                        if (workingLegQty != 0)
                        { // we want to work a qty.
                            if (m_IExecutionListener.TryCreateOrder(quoteLeg.m_PriceLeg.InstrumentName, quoteLegSide,
                                                          iWorkingLegPrice, workingLegQty, out quoteOrder))
                            { // order succesfully created
                                if (quoteLeg.IsMarketGood)
                                { // the market in that leg is good
                                    if (m_IExecutionListener.TrySubmitOrder(quoteOrderBook.BookID, quoteOrder))
                                    { // our order was submitted successfuly.
                                        m_RiskManager.m_NumberOfQuotesThisSecond++;                                             // increment our number of quotes this second
                                    }
                                }
                            }
                        }
                    }
                    else if (iWorkingLegPrice != quoteOrder.IPricePending || workingLegQty != quoteOrder.WorkingQtyPending)  //TODO: CLEAN THIS UP!
                    {   // We already working a quote, but at different price or quantitys                        
                        if (m_PendingQuoteOrders[quoteLegId][quoteLegSide] != null)
                        { // we had a pending quote order to delete
                            m_PendingQuoteOrders[quoteLegId][quoteLegSide] = null;
                        }
                        if (workingLegQty == 0)
                        { // we want to delete not change the qty or price
                            if (!m_IExecutionListener.TryDeleteOrder(quoteOrder))
                                m_Log.NewEntry(LogLevel.Error, "Quoter: Failed to delete order{0}", quoteOrder);
                        }
                        if (iWorkingLegPrice != quoteOrder.IPricePending)
                        { // we should change the price first, even if qty is an issue
                            if (!m_IExecutionListener.TryChangeOrderPrice(quoteOrder, iWorkingLegPrice))
                            {   // Error. Failed to submit a cancel replace.  Use delete instead. Should we try to cancel here?
                                Order desiredOrder;
                                m_IExecutionListener.TryCreateOrder(quoteLeg.m_PriceLeg.InstrumentName, quoteLegSide,
                                                          iWorkingLegPrice, workingLegQty, out desiredOrder);
                                m_PendingQuoteOrders[quoteLegId][quoteLegSide] = desiredOrder;                                          // As soon as quoteOrder state is updated, we will submit this order.
                                m_Log.NewEntry(LogLevel.Warning, "{0} Failed to Modify, Placing new order {1} in Pending Queue.", quoteOrder, desiredOrder);
                            }
                        }
                        if (quoteOrder.WorkingQtyPending != workingLegQty)
                        { // we need to change qty
                            if (!m_IExecutionListener.TryChangeOrderQty(quoteOrder, workingLegQty))                     // do we need to add executed qty in here?
                            {   // Error. Failed to submit a cancel replace.  Use delete instead. Should we try to cancel here?
                                Order desiredOrder;
                                m_IExecutionListener.TryCreateOrder(quoteLeg.m_PriceLeg.InstrumentName, quoteLegSide,
                                                          iWorkingLegPrice, workingLegQty, out desiredOrder);
                                m_PendingQuoteOrders[quoteLegId][quoteLegSide] = desiredOrder;                                          // As soon as quoteOrder state is updated, we will submit this order.
                                m_Log.NewEntry(LogLevel.Warning, "{0} Failed to Modify, Placing new order {1} in Pending Queue.", quoteOrder, desiredOrder);
                            }
                        }
                        m_RiskManager.m_NumberOfQuotesThisSecond += 2;                                                              // increment our number of quotes this second by two (one cancel and one submit)
                    }
                }
            }
            if ((!isQuoteLeg || isNeedToDeleteOrder) && quoteOrder != null)
            {   // this isn't a "quote leg" and we have an order out there, or we have flagged that order as needing to be deleted.
                if (m_PendingQuoteOrders[quoteLegId][quoteLegSide] != null)
                { // we had a pending quote order
                    m_PendingQuoteOrders[quoteLegId][quoteLegSide] = null;
                }
                if (!m_IExecutionListener.TryDeleteOrder(quoteOrder))
                    m_Log.NewEntry(LogLevel.Warning, "{0} Failed to cancel", quoteOrder);
            }
        } // UpdateQuoteOrder()
        //
        //
        // *****************************************************
        // ****         CheckLegMarketStates()              ****
        // *****************************************************
        /// <summary>
        /// Iterates through all lesg to check for a "good" market state in all of the legs.
        /// </summary>
        /// <returns>true is all legs have a good market.</returns>
        private bool CheckLegMarketStates()
        {
            bool isAllLegGood = true;       // assume all legs are good
            for (int leg = 0; leg < m_Instruments.Count; leg++)
            { //iterate through each leg, if any markets are bad change our bool, break our loop, and return the bool
                if (!m_QuoterLegs[leg].IsMarketGood)
                {
                    isAllLegGood = false;
                    break;
                }
            }
            return isAllLegGood;
        } //CheckLegMarketStates()
        //
        //
        //
        //
        #endregion//Private Methods

        #region Event Handlers
        // ***********************************************************************
        // ****                     Event Handlers                            ****
        // ***********************************************************************
        //
        //
        // *****************************************************************
        // ****             Instrument_LeanablePriceChanged()           ****
        // *****************************************************************
        private void Instrument_LeanablePriceChanged(object sender, EventArgs eventArgs)
        {
            QuoterLeg senderInstr = (QuoterLeg)sender;
            int instrId = m_QuoterLegs.IndexOf(senderInstr);
            if (senderInstr.m_LeanablePriceChanged[0])
            {   // Bid side of this instrument has changed. Quoting legs leaning on this bid, must want to sell
                // this instrument.  Therefore, the following gets the appropriate strategySide.
                int strategySide = UV.Lib.Utilities.QTMath.MktSignToMktSide(-senderInstr.m_PriceLeg.PriceMultiplier);
                UpdateQuotingLegPrices(strategySide, instrId);  // update our pricing 
                UpdateQuotingLegOrders(strategySide, instrId);  //update our quotes
            }
            if (senderInstr.m_LeanablePriceChanged[1])
            {
                int strategySide = UV.Lib.Utilities.QTMath.MktSignToMktSide(senderInstr.m_PriceLeg.PriceMultiplier);
                UpdateQuotingLegPrices(strategySide, instrId);  // update our pricing 
                UpdateQuotingLegOrders(strategySide, instrId);  //update our quotes
            }

            UpdateMarketPrice();
        } //Instrument_LeanablePriceChanged
        //
        //
        //
        // *****************************************************************
        // ****             Instrument_ParameterChanged()               ****
        // *****************************************************************
        /// <summary>
        /// When a leg parameters change and we need to therefore deal with a quoter change or order 
        /// chaange we must update.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventargs"></param>
        private void Instrument_ParameterChanged(object sender, EventArgs eventargs)
        {
            OnQuoterStateChange();
        }
        //
        // *********************************************************
        // ****               Market_BestPriceChanged           ****
        // *********************************************************
        private void Market_BestPriceChanged(object sender, EventArgs eventargs)
        {
            Market market = (Market)sender;
            int internalId = m_InstrumentToInternalId[market.Name];
            QuoterLeg quoteLeg = m_QuoterLegs[internalId];
            for (int side = 0; side < 2; ++side)
            {
                int sgn = UV.Lib.Utilities.QTMath.MktSideToMktSign(side);
                if (quoteLeg.m_QuotingEnabled[side])
                {   // if we are quoting this instrument.
                    bool isQuotePriceNowOnMarket = CheckOffMarketQuote(internalId, side, m_QuotingLegPrices[internalId][side]);
                    if (m_IsQuotingLegPriceOffMarket[internalId][side] != isQuotePriceNowOnMarket)
                    {   // we have changed states.
                        UpdateQuoteOrder(internalId, UV.Lib.Utilities.QTMath.MktSignToMktSide(quoteLeg.m_PriceLeg.Weight * sgn));  // we need to update our quote orders since we have now changed states.
                        m_IsQuotingLegPriceOffMarket[internalId][side] = isQuotePriceNowOnMarket;  // and set our new state flag.
                    }
                }
            }//side
        } //Instrument_PriceChanged()
        //
        //
        // *****************************************************************
        // ****             Instrument_MarketStateChanged()             ****
        // *****************************************************************
        private void Instrument_MarketStateChanged(object sender, EventArgs eventargs)
        {
            bool wasAllLegsGood = m_IsAllLegsGood;
            m_IsAllLegsGood = CheckLegMarketStates();
            if (m_IsAllLegsGood && !wasAllLegsGood)
            { // we have changed to a good state and can now validate our entry prices.
                if (!m_RiskManager.ValidatePrices(m_StrategyWorkingPrice[Order.BuySide],
                                                 Order.BuySide, m_StrategyMarketPrice))
                { // our bid price is no good, turn our qty to 0
                    m_TotalDesiredQty[Order.BuySide] = 0;
                }
                if (!m_RiskManager.ValidatePrices(m_StrategyWorkingPrice[Order.SellSide],
                                                Order.SellSide, m_StrategyMarketPrice))
                { // our ask price is no good turn our qty to 0
                    m_TotalDesiredQty[Order.SellSide] = 0;
                }
            }
            UpdateQuotingLegBools();
        } //Instrument_MarketStateChanged()
        //
        //
        //
        // *****************************************************************
        // ****               OrderBook_OrderFilled()                   ****
        // *****************************************************************
        private void OrderBook_OrderFilled(object sender, EventArgs eventArgs)
        {
            // Get information about who was filled.
            FillEventArgs fillEventArgs = (FillEventArgs)eventArgs;
            if (!m_Instruments.Contains(fillEventArgs.InstrumentName))
            {
                m_Log.NewEntry(LogLevel.Error, "Quoter : Recieved Fill for Unknown Instrument {0}", fillEventArgs.InstrumentName);
                return;
            }
            int internalLegId = m_Instruments.IndexOf(fillEventArgs.InstrumentName);    // maybe this is to slow and we should set up look up table?
            Fill fill = fillEventArgs.Fill;
            if (fill == null)
                return;

            ExecutionEngines.SpreaderFills.SyntheticFill newSyntheticFill = null;   // check to see if we can create a compled synthetic fill now.
            if (m_OrderBooks[internalLegId].BookID != fillEventArgs.OrderBookID)
            {// this fill didn't come from our book, must be from the hedger!
                newSyntheticFill = m_HedgeManager.m_Hedgers[internalLegId].Filled(fillEventArgs, internalLegId);
            }
            else
            { // the fill is a quote fill 
                int legSide = UV.Lib.Utilities.QTMath.MktSignToMktSide(fill.Qty);
                int stratSide = UV.Lib.Utilities.QTMath.MktSignToMktSide(fill.Qty * m_LegRatios[internalLegId]);

                Order remainingOrder;
                if (m_OrderBooks[internalLegId].TryGet(fillEventArgs.OrderId, out remainingOrder))
                { // if this order has remaining working qty we are partially filled on our quote.
                    m_IsPartialFilled[stratSide] = remainingOrder.WorkingQtyPending != 0;
                    if (m_IsPartialFilled[stratSide] && m_PendingQuoteOrders[internalLegId][legSide] != null)
                    { // we are partialled and have a pending order that qty now needs to be updated in case we end up submitting it
                        m_PendingQuoteOrders[internalLegId][legSide].OriginalQtyPending = remainingOrder.WorkingQtyPending;
                    }
                }
                else
                { // we can't find the order because it has been deleted since it was filled completely.
                    m_IsPartialFilled[stratSide] = false;
                }

                m_LegFillBooks[internalLegId].TryAdd(fill);                                              // Add to Fill Queue and List record
                m_HedgeManager.HedgeFill(internalLegId, fill);                                           // Call Hedger - this will set Hedger.IsHung state.


                if (m_SpreadFillGenerator.TryGenerateSyntheticFill(stratSide, out newSyntheticFill))
                {
                    m_SpreaderPos[UV.Lib.Utilities.QTMath.MktSignToMktSide(newSyntheticFill.Qty)] += newSyntheticFill.Qty;
                    m_SyntheticSpreadFillBook.TryAdd(newSyntheticFill);         // add fill to list  
                    m_Log.NewEntry(LogLevel.Major, string.Format("         {0} - {1}", m_SyntheticSpreadFillBook.Instrument, newSyntheticFill));
                }
                m_QuoteFillCount[internalLegId][legSide] += fill.Qty;  // add fill qty to unhedged quote count
                m_QuotePartialCount[internalLegId][legSide] = (m_QuoteFillCount[internalLegId][legSide] % (int)m_LegRatios[internalLegId]);
                //Math.DivRem(m_QuoteFillCount[legId][legSide], m_LegRatios[legId], out m_QuotePartialCount[legId][legSide]); // update running partial counts
                OnQuoterStateChange(stratSide);
            }

            if (newSyntheticFill != null)
            {
                //fire event for fill?
            }
        }
        //
        //
        // *****************************************************************
        // ****               OrderBook_OrderStateChanged()             ****
        // *****************************************************************
        private void OrderBook_OrderStateChanged(object sender, EventArgs eventArgs)
        { // this needs to cleaned up!
            // Get information about instrument and order.
            OrderEventArgs orderEventArg = (OrderEventArgs)eventArgs;
            Order updatedOrder = orderEventArg.Order;
            if (!m_Instruments.Contains(orderEventArg.Order.Instrument))
            {
                m_Log.NewEntry(LogLevel.Error, "Quoter : Recieved Order Update for Unknown Instrument {0}", orderEventArg.Order.Instrument);
                return;
            }
            int internalLegId = m_Instruments.IndexOf(orderEventArg.Order.Instrument);    // maybe this is to slow and we should set up look up table?

            if (m_OrderBooks[internalLegId].BookID != orderEventArg.OrderBookID)
            {// this order didn't come from our book, must be from the hedger
                m_HedgeManager.m_Hedgers[internalLegId].OrderStateChanged(orderEventArg, internalLegId);
            }
            int side = orderEventArg.Order.Side;
            int strategySide = UV.Lib.Utilities.QTMath.MktSignToMktSide(UV.Lib.Utilities.QTMath.MktSideToMktSign(updatedOrder.Side) * m_LegRatios[internalLegId]);

            // Check to see whether we are quoting an order still, and what the state of our spreader is.
            m_OrderWorkSpace.Clear();
            m_OrderBooks[internalLegId].GetOrdersByRank(side, 0, ref  m_OrderWorkSpace);
            if (m_OrderWorkSpace.Count == 0)
            {
                if (m_PendingQuoteOrders[internalLegId][side] != null &&                      // pending order exists
                     m_QuoterLegs[internalLegId].m_QuotingEnabled[side])
                {
                    Order newOrder = m_PendingQuoteOrders[internalLegId][side];
                    m_PendingQuoteOrders[internalLegId][side] = null;
                    if (m_QuoterLegs[internalLegId].IsMarketGood)
                    {
                        m_IExecutionListener.TrySubmitOrder(m_OrderBooks[internalLegId].BookID, newOrder);
                    }
                }
            }
        }
        //
        //
        //
        // *****************************************************************
        // ****         ExecutionListener_InstrumentsFound()            ****
        // *****************************************************************
        /// <summary>
        /// Called when our execution listener has found a new instrument.  This means that it has also created
        /// a market for this instrument which we can now have a pointer to in the quoter leg, as well as subscribe 
        /// to the MarketChanged events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void ExecutionListener_InstrumentsFound(object sender, EventArgs eventArgs)
        {
            //
            // Gather and save pertinent information about the Instrument found.
            //
            InstrumentsFoundEventArgs instrEventArgs = (InstrumentsFoundEventArgs)eventArgs;
            InstrumentDetails instrDetails = instrEventArgs.InstrumentDetails;
            int internalId = m_Instruments.IndexOf(instrDetails.InstrumentName);
            QuoterLeg quoteLeg = m_QuoterLegs[internalId];
            quoteLeg.m_Market = m_ExecutionContainer.m_Markets[instrDetails.InstrumentName];                    // keep a pointer to the market
            quoteLeg.InstrumentDetails = instrDetails;                                                          // save the instrument details
            m_InstrumentToInternalId.Add(instrDetails.InstrumentName, internalId);
            m_LegFillBooks[internalId].m_ContractMultiplier = quoteLeg.InstrumentDetails.Multiplier;            // now we can also assign the correct multiplier for the fill book

            m_ExecutionContainer.m_Markets[instrDetails.InstrumentName].MarketChanged += new EventHandler(quoteLeg.Market_MarketChanged); // subscribe to events.
            if (quoteLeg.OffMarketQuoting != OffMarketQuoteBehavior.Quote && quoteLeg.UserDefinedQuotingEnabled)// if we are not always quoting this leg, we need to subscribe to updates.
                m_ExecutionContainer.m_Markets[instrDetails.InstrumentName].MarketBestPriceChanged += new EventHandler(Market_BestPriceChanged);
            //
            // Calculate possible tick size for the spread
            //
            double newPossibleMinTickSize = Math.Abs(instrDetails.TickSize * m_QuoterLegs[internalId].m_PriceLeg.PriceMultiplier);
            if (double.IsNaN(m_QuoteTickSize) || newPossibleMinTickSize < m_QuoteTickSize)
                m_QuoteTickSize = newPossibleMinTickSize;

            nInstrumentsFoundCount++;
            if (nInstrumentsFoundCount == m_QuoterLegs.Count)
            { // we have found all the legs we are interested in/
                m_IsLegSetupCompleted = true;
                base.BroadcastAllParameters((IEngineHub)m_Hub, m_ExecutionContainer);                                           // update the gui
                UpdateQuotingLegBools();
                OnMarketsReadied();
                m_RiskManager.Start();
                m_ExecutionContainer.ConfirmStrategyLaunched();
            }
        }
        //
        //
        // *****************************************************************
        // ****           ExecutionListener_Initialized()               ****
        // *****************************************************************
        /// <summary>
        /// Called once dispatcher is attached to thread allowing us to finish our setup.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void ExecutionListener_Initialized(object sender, EventArgs eventArgs)
        {
            IEngineHub iEngHub = (IEngineHub)m_Hub;     // cast our generic hub to an Ieng hub

            foreach (Engine eng in m_ExecutionContainer.EngineList.Values)  // call all engine's begin, now we are on the right thread!
                eng.SetupBegin(iEngHub, m_ExecutionContainer);
            foreach (Engine eng in m_ExecutionContainer.EngineList.Values) // call all engine's complete
                eng.SetupComplete();
        }
        //
        //
        // *****************************************************************
        // ****             ExecutionListener_Stopping()                ****
        // *****************************************************************
        /// <summary>
        /// Called by the execution listener thread when it has gotten the signal to try and 
        /// shutdown nicely.  As soon as we give back the thread in this function we can asusme 
        /// that we will not get any more callbacks.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void ExecutionListener_Stopping(object sender, EventArgs eventArgs)
        {
            m_Log.NewEntry(LogLevel.Major, "Quoter is shutting down, attemption to cancel all quoter orders");
            m_IsUserTradingEnabled = false; // stops new order from going out
            UpdateQuotingLegBools();        // sets bools and should remove any quoter orders
            CancelAllOrders();              // just to be sure lets cancel all quote orders
            // for now leaving any outstanding hedge orders in the market.
        }
        #endregion//Event Handlers

        #region Events
        // *****************************************************************
        // ****                         Events                          ****
        // *****************************************************************
        //
        //
        public event EventHandler SpreadPriceChanged;
        //
        //
        /// <summary>
        /// Whenever our synthetic spread price has changed call our subscribers.
        /// </summary>
        protected void OnSpreadPriceChanged()
        {
            if (this.SpreadPriceChanged != null)
            {
                this.SpreadPriceChanged(this, EventArgs.Empty);
            }
        }
        //
        //
        public event EventHandler MarketsReadied;
        //
        //
        //
        /// <summary>
        /// Called when all of our markets are intitialized.
        /// </summary>
        public void OnMarketsReadied()
        {
            if (this.MarketsReadied != null)
                this.MarketsReadied(this, EventArgs.Empty);
        }
        #endregion //Events

        #region IStringifiable Implementation
        // *****************************************************************
        // ****               IStringifiable Implementation             ****
        // *****************************************************************
        //
        //
        public override string GetAttributes()
        {
            StringBuilder s = new StringBuilder(base.GetAttributes());
            return s.ToString();
        }
        public override List<IStringifiable> GetElements()
        {
            List<IStringifiable> elements = base.GetElements();
            if (elements == null)                                   // check this in case base class has elements in future.
                elements = new List<IStringifiable>();
            foreach (QuoterLeg quoterLeg in m_QuoterLegs)
                elements.Add(quoterLeg);
            // Exit
            return elements;
        }
        public override void SetAttributes(Dictionary<string, string> attributes)
        {
            base.SetAttributes(attributes);
            double x;
            int n;
            foreach (KeyValuePair<string, string> attr in attributes)
            {
                if (attr.Key == "QuoteTickSize" && double.TryParse(attr.Value, out x))
                    this.QuoteTickSize = x;
                else if (attr.Key == "EngineId" && int.TryParse(attr.Value, out n))
                    this.m_EngineID = n;
            }
        }
        public override void AddSubElement(IStringifiable subElement)
        {
            if (subElement is QuoterLeg)                    // find all quoter legs we are trying to use to create our spreader
                m_QuoterLegs.Add((QuoterLeg)subElement);
            else if (subElement is HedgeManager)
                m_HedgeManager = (HedgeManager)subElement;
        }
        #endregion

        #region IOrderEngine Implementation
        // ***********************************************************************
        // ****                IOrderEngine Implementation                    ****
        // ***********************************************************************
        //
        // *****************************************************************
        // ****                   CancelAllOrders()                     ****
        // *****************************************************************
        /// <summary>
        /// Called by Risk Manager or User who would like to cancel all exisiting orders.
        /// </summary>
        /// 
        public void CancelAllOrders()
        {
            foreach (OrderBook orderBook in m_OrderBooks.Values)
            {
                for (int side = 0; side < 2; side++)
                {
                    m_OrderWorkSpace.Clear();
                    orderBook.GetOrdersByRank(side, 0, ref m_OrderWorkSpace);
                    foreach (Order order in m_OrderWorkSpace)
                        m_IExecutionListener.TryDeleteOrder(order);
                }
            }
        }
        //
        //
        // *****************************************************************
        // ****                         Quote()                         ****
        // *****************************************************************
        /// <summary>
        /// Called by a model to changed the prices and qty of the quoter.
        /// If a user desires to turn of quoting on a side calling this with a
        /// 0 qty will accomplish that.
        /// 
        /// qty should be signed appropiately. - qty for sell side.
        /// 
        /// Prices will also be validated prior to be set
        /// </summary>
        /// <param name="tradeSide"></param>
        /// <param name="price"></param>
        /// <param name="qty"></param>
        public void Quote(int tradeSide, double price, int qty)
        {
            if (qty != 0 && tradeSide != UV.Lib.Utilities.QTMath.MktSignToMktSide(qty))
            { // mismatch qty and sides
                m_Log.NewEntry(LogLevel.Warning, "Quote: tradeSide and side implied by qty sign do not match, rejecting quote update");
                return;
            }
            bool isNeedToUpdate = false;
            if (m_StrategyWorkingPrice[tradeSide] != price)
            {  // price has changed
                if (m_RiskManager.ValidatePrices(price, tradeSide, m_StrategyMarketPrice) || !m_IsAllLegsGood)
                { // our prices are valid so we can save variables, or we don't have a good market yet,
                  // so lets save them and then we can check the variables once our market is good.
                    m_StrategyWorkingPrice[tradeSide] = price;
                    m_TotalDesiredQty[tradeSide] = qty;
                    isNeedToUpdate = true;
                }
            }
            else if (m_TotalDesiredQty[tradeSide] != qty)
            { // qty change only 
                m_TotalDesiredQty[tradeSide] = qty;
                isNeedToUpdate = true;
            }
            if (isNeedToUpdate)
            {// reprice this side
                OnQuoterStateChange(tradeSide);
                m_Log.NewEntry(LogLevel.Minor, "Quote: Working {0} @ {1} in {2}", qty, price, m_SyntheticSpreadFillBook.Instrument);
            }
        }
        //
        //
        #endregion // end IOrderEngine
    }
}
