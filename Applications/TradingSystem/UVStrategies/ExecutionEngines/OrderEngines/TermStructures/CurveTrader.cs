using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UV.Strategies.ExecutionEngines.OrderEngines.TermStructures
{

    using UV.Lib.Engines;
    using UV.Lib.IO.Xml;
    using UV.Lib.Hubs;
    using UV.Lib.Fills;
    using UV.Lib.Products;
    using UV.Lib.OrderBooks;
    using UV.Lib.BookHubs;
    using UV.Lib.Utilities;
    using PositionBook = UV.Lib.Positions.PositionBook;

    using UV.Strategies.ExecutionHubs;
    using UV.Strategies.ExecutionHubs.ExecutionContainers;
    using UV.Strategies.ExecutionEngines;
    using UV.Strategies.ExecutionEngines.Scratchers;
    using UV.Strategies.ExecutionEngines.Risk;

    public class CurveTrader : Engine, IOrderEngine, IOrderEngineParameters, IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        // External Services and Engines
        public ExecutionListener m_ExecutionListener = null;
        public ThreadContainer m_ExecutionContainer = null;
        public CurveTradeManager m_CurveTradeManager = null;
        public Hub m_Hub = null;
        public LogHub m_Log = null;
        public ScratchManager m_ScratchManager = null;
        public RiskManagerCurveTrader m_RiskManager = null;

        // Mappings
        public List<CurveLeg> m_CurveLegs = new List<CurveLeg>();
        protected List<InstrumentName> m_Instruments = new List<InstrumentName>();                                  // map: internallegId --> Instrument
        public Dictionary<InstrumentName, int> m_InstrumentToInternalId = new Dictionary<InstrumentName, int>();    // map: InstrumentName --> legId 
        internal PositionBook[] m_PositionBooks;                            // array of positions books indexed by legs
        internal OrderBook[] m_OrderBooks;                                  // array internallegId --> OrderBook


        // Engine Variables
        private string m_DefaultAccount;                                    // default account to send orders
        private int m_DripQty = 1;                                          // default order size to show the market
        private double m_QuoteTickSize = 1;                                 // Minimum fluctuation for the Order Engine to consider a price change.
        private bool m_IsRiskCheckPassed;                                   // is engine currently in a okay to trade state based on risk checks
        private bool m_IsUserTradingEnabled;                                // is engine allowed to trade base on user preference
        private int m_LevelsToQuoteMax = 10;                                // maximum number of levels to leave orders without pulling
        private int m_LevelsToQuoteMin = 5;                                 // minimum number of leves to quote always.             

        // Books
        public FillBook m_SyntheticSpreadFillBook;                          // Fill book just for the spread fills

        // Internal variables  
        private int nInstrumentsFoundCount = 0;                             // count for all instruments requested                                      
        private bool m_IsAllLegsGood = new bool();                          // when all the market states of all legs are good, this is true.
        public bool m_IsLegSetupCompleted = false;                          // true once strategy knows about all details for legs

        // Collections 
        protected Order[][] m_PendingQuoteOrders;                           // order not yet submitted [legId][legSide]
        private bool[][] m_IsQuotingLeg;
        internal double[] m_LegRatios;
        protected double[] m_LegPriceMultipliers;
        protected int[][] m_QuoteFillCount;                                 // array for each leg and sides fill count for quotes
        protected int[][] m_QuotePartialCount;                              // duplicate array with actual fractional partial counts.       
        protected double[][] m_QuotingLegPrices;                            // array for each leg and side of the prices we want to be quoting at
        protected bool[][] m_IsLegLeanable;                                 // array for each leg and side for leg being leanable (has sufficient qty in book)
        protected Dictionary<int, int>[][] m_NeededHedgeQty;                // Needed Hedge Qty's m_NeededHedgeQty[LegId][Side][IPriceLevel] - not sure if i need side here?
        protected Dictionary<int, int>[] m_NeededScratchQty;                // m_NeededScratchQty[LegId][IPriceLevel] Needed scratch qty at a price level, this is only for oder accounting

        // temporary work spaces
        private List<Order> m_OrderWorkSpace = new List<Order>();                               // workspace for temp order storage-recycle after each use
        #endregion// members

        #region Constructors
        // *****************************************************************
        // ****               Constructors And Intializtion             ****
        // *****************************************************************
        //
        public CurveTrader()
            : base()
        { }
        //
        //       
        protected override void SetupInitialize(IEngineHub myEngineHub, IEngineContainer engineContainer, int engineID, bool setupGui)
        {
            if (typeof(UV.Strategies.ExecutionHubs.ExecutionContainers.MultiThreadContainer).IsAssignableFrom(engineContainer.GetType()))
            {   // this is the "first" set up call from the manager container.  All subengines that are remote MUST be added in this call to launch
                // properly.  Once this is done on the next call through, we can add them to the specific managing container and map their engine id
                // to the correct thread.
                base.SetupInitialize(myEngineHub, engineContainer, engineID, setupGui);
                m_Hub = (Hub)myEngineHub;
                m_Log = m_Hub.Log;
                ThreadContainer container = (ThreadContainer)engineContainer;
                foreach (CurveLeg curveLeg in m_CurveLegs)
                {   // add curve leg to our managing container
                    container.TryAddEngine(curveLeg);
                }
                container.TryAddEngine(m_ScratchManager);   // Add scratch manager to our multithread container. 
                container.TryAddEngine(m_RiskManager);      // add risk manager too.
            }
            else
            {   // this is the second call from the actual container, this is the container we want to manage all of our engines 
                m_ExecutionContainer = (ThreadContainer)engineContainer;
                MultiThreadContainer multiThreadContainer = (MultiThreadContainer)m_CurveTradeManager.m_ExecutionContainer;
                foreach (CurveLeg curveLeg in m_CurveLegs)
                {   // add curve leg to our container
                    m_ExecutionContainer.TryAddEngine(curveLeg);
                    multiThreadContainer.TryAddEngineIdToManagingContainer(m_ExecutionContainer, curveLeg.EngineID);
                }
                m_ExecutionContainer.TryAddEngine(m_ScratchManager);                                                        // add our scratch manager
                multiThreadContainer.TryAddEngineIdToManagingContainer(m_ExecutionContainer, m_ScratchManager.EngineID);    // map his id to the correct container

                m_ExecutionContainer.TryAddEngine(m_RiskManager);                                                           // add our risk  manager
                multiThreadContainer.TryAddEngineIdToManagingContainer(m_ExecutionContainer, m_RiskManager.EngineID);       // map his id to the correct container

                //
                // Set up all collections here.
                //
                m_PendingQuoteOrders = new Order[m_CurveLegs.Count][];                  // user-set orders for each [leg] and [side], not yet submitted.
                m_IsQuotingLeg = new bool[m_CurveLegs.Count][];                         // user-set flag for quoting legs.

                m_PositionBooks = new PositionBook[m_CurveLegs.Count];                  // position books for each leg
                m_OrderBooks = new OrderBook[m_CurveLegs.Count];                        // order books for each leg
                m_LegRatios = new double[m_CurveLegs.Count];                            // array for leg ratios
                m_LegPriceMultipliers = new double[m_CurveLegs.Count];                  // array for leg Price Multipliers
                m_IsAllLegsGood = false;                                                // start assuming all legs are in a "bad" state.

                m_QuoteFillCount = new int[m_CurveLegs.Count][];                        // array for each leg and side of quote fills
                m_QuotePartialCount = new int[m_CurveLegs.Count][];                     // duplicate array for partials fills
                m_QuotingLegPrices = new double[m_CurveLegs.Count][];
                m_IsLegLeanable = new bool[m_CurveLegs.Count][];
                m_NeededHedgeQty = new Dictionary<int, int>[m_CurveLegs.Count][];
                m_NeededScratchQty = new Dictionary<int, int>[m_CurveLegs.Count];
            }
        }
        //
        //
        public override void SetupBegin(IEngineHub myEngineHub, IEngineContainer engineContainer)
        {
            base.SetupBegin(myEngineHub, engineContainer);

            //
            // Find our risk manager, if we cant find one, throw an exception
            //
            foreach(IEngine ieng in engineContainer.GetEngines())
            {
                if (ieng is RiskManagerCurveTrader)
                    m_RiskManager = (RiskManagerCurveTrader)ieng;
            }


            if (m_RiskManager == null)
                throw new NotImplementedException("All Strategies Must Have a Risk Manager, Please Add One To Your User Config - Must Be UV.Execution.Risk.RiskManagerQuoter type");

            //
            // Finish setup
            //
            m_ExecutionListener.InstrumentFound += new EventHandler(ExecutionListener_InstrumentsFound);

            for (int leg = 0; leg < m_CurveLegs.Count; ++leg)
            {
                CurveLeg curveLeg = m_CurveLegs[leg];
                m_Instruments.Add(curveLeg.m_PriceLeg.InstrumentName);

                //
                // Complete all subscriptions
                //
                curveLeg.LeanablePriceChanged += new EventHandler(CurveLeg_LeanablePriceChanged);    // subscribe to pricing updates.
                curveLeg.MarketStateChanged += new EventHandler(CurveLeg_MarketStateChanged);        // subscribe to market state changes.
                curveLeg.ParameterChanged += new EventHandler(CurveLeg_ParameterChanged);            // param changes need to make the quoter update.
                curveLeg.SqueezeStateChanged += new EventHandler(CurveLeg_SqueezeStateChanged);      // subscribe to market events crossing squeeze thresholds

                //if (curveLeg.UserDefinedQuotingEnabled)
                //{   // if we are quoting this leg, we need all these subscriptions, TODO: deal with this later to make things more optimal
                curveLeg.PullJoinStateChanged += new EventHandler(CurveLeg_PullJoinStateChanged);           // changes for joining orders on this leg. 
                //}

                PositionBook fillBook = new PositionBook(curveLeg.m_PriceLeg.InstrumentName, 0);     // Multiplier will need to be set once we have instrument details!
                m_PositionBooks[leg] = fillBook;                                                         // add book to array to pass to spread fill generator

                m_IsQuotingLeg[leg] = curveLeg.m_QuotingEnabled;
                m_PendingQuoteOrders[leg] = new Order[2];                                               // 2-sides of the mkt
                m_QuotingLegPrices[leg] = new double[2];
                m_IsLegLeanable[leg] = new bool[2];

                m_LegRatios[leg] = curveLeg.m_PriceLeg.Weight;                                         // add ratio and multipliers to array to pass to spread fill generator
                m_LegPriceMultipliers[leg] = curveLeg.m_PriceLeg.PriceMultiplier;

                m_QuoteFillCount[leg] = new int[2];                                                     // Quote Fill counts to start are 0 for bid side  
                m_QuotePartialCount[leg] = new int[2];                                                  // Quote partial counts to start are 0 for bid side  
                m_NeededHedgeQty[leg] = new Dictionary<int, int>[2];                                    // hedge qty dictionary by side
                for (int side = 0; side < 2; ++side)
                {   // populate dictionaries for both sides
                    m_NeededHedgeQty[leg][side] = new Dictionary<int, int>();
                }
                m_NeededScratchQty[leg] = new Dictionary<int, int>();
            }
        }
        //
        //
        public override void SetupComplete()
        {
            base.SetupComplete();

            // if we create order books here we know risk is already listening for the events.
            for (int leg = 0; leg < m_CurveLegs.Count; ++leg)
            {
                CurveLeg curveLeg = m_CurveLegs[leg];
                OrderBook orderBook = m_ExecutionListener.CreateOrderBook(curveLeg.m_PriceLeg.InstrumentName, curveLeg.DefaultAccount);
                m_OrderBooks[leg] = orderBook;
                orderBook.OrderFilled += new EventHandler(OrderBook_OrderFilled);
                orderBook.OrderStateChanged += new EventHandler(OrderBook_OrderStateChanged);
                curveLeg.m_Scratcher.m_OrderEngineOrderBook = orderBook;    // give our scratcher a pointer to the order book we want him to submit orders to
            }

            m_ScratchManager.ScratchOrderSumbitted += new EventHandler(ScratchManager_ScratchOrderSubmitted);
        }
        #endregion//Constructors

        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// Default order size to show the market
        /// </summary>
        public int DripQty
        {
            get { return m_DripQty; }
            set { m_DripQty = value; }
        }
        //
        //
        /// <summary>
        /// Minimum fluctuation for the Order Engine to consider a price change.
        /// </summary>
        public double QuoteTickSize
        {
            get { return m_QuoteTickSize; }
            set { m_QuoteTickSize = value; }
        }
        //
        //
        /// <summary>
        /// Has risk verified that is order engine is okay to send orders.
        /// </summary>
        public bool IsRiskCheckPassed
        {
            get { return m_IsRiskCheckPassed; }
            set { m_IsRiskCheckPassed = value; }
        }
        //
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
                //OnQuoterStateChange();
                if (m_Log != null)
                    m_Log.NewEntry(LogLevel.Error, "CruveTrader: Turning On Trading is Not Implemented Yet!");
            }
        }
        //
        //
        /// <summary>
        /// If no specific account is specifed elsewhere all orders will be sent with this 
        /// </summary>
        public string DefaultAccount
        {
            get { return m_DefaultAccount; }
            set
            {
                if (value.Length > 15) // 15 char limit!
                    value = value.Substring(0, 15);
                m_DefaultAccount = value;
            }
        }
        //
        //
        /// <summary>
        /// Minimum number of levels to begin quoting.
        /// </summary>
        public int LevelsToQuoteMin
        {
            get { return m_LevelsToQuoteMin; }
            set 
            {
                if (value <= LevelsToQuoteMax)
                    m_LevelsToQuoteMin = value;
            }
        }
        //
        //
        /// <summary>
        /// Maximum levels to maintain quote orders at.  If more than this number of levels
        /// have orders, orders at the furthest levels from market will be pulled
        /// </summary>
        public int LevelsToQuoteMax
        {
            get { return m_LevelsToQuoteMax; }
            set 
            { 
                if(value >= m_LevelsToQuoteMin )
                    m_LevelsToQuoteMax = value; 
            }
        }
        //
        // Property like methods...hidden from the gui (our gui automatically displays all properties to the user, these do not need to be displayed.
        //
        public ExecutionListener GetExecutionListener()
        {
            return m_ExecutionListener;
        }
        //
        //
        public void SetExecutionListener(ExecutionListener executionListener)
        {
            m_ExecutionListener = executionListener;
            m_ExecutionListener.ExecutionContainer = m_ExecutionContainer;      // assign my execution container to the listener 
        }
        //
        //
        public FillBook GetFillBook()
        {
            return m_SyntheticSpreadFillBook;
        }
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

        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        // *****************************************************
        // ****          CurveTraderLaunched()              ****
        // *****************************************************
        /// <summary>
        /// Called by the internal thread to signal our launch both to the execution hub
        /// as well as our manager. 
        /// </summary>
        private void CurveTraderLaunched()
        {
            m_ExecutionContainer.ConfirmStrategyLaunched();                 // signal the hub.
            CurveTraderEventArgs eventArg = new CurveTraderEventArgs();     // create event args to be safely passed via dispatcher to master execution thread
            eventArg.MsgType = CurveTraderEventArgs.EventType.Launched;
            eventArg.Status = CurveTraderEventArgs.EventStatus.Confirm;
            eventArg.EngineId = this.EngineID;
            m_CurveTradeManager.m_ExecutionListener.ProcessEvent(eventArg);
        }
        //
        //
        // *****************************************************
        // ****          ShouldFoundOrderBeAcquired()       ****
        // *****************************************************
        /// <summary>
        /// When the systems find an order, the curve trader has the option
        /// of trying to take over management of the order.  This method
        /// will determine if the order should be taken from the default book
        /// and intergrated into the execution strategy.
        /// </summary>
        /// <param name="foundOrder"></param>
        /// <returns></returns>
        private bool ShouldFoundOrderBeAcquired(Order foundOrder)
        {
            // TOOD: add more logic - this could be based on user tags or
            // account or whatever....for now we can toggle this for debugging...for now dont pick up orders
            return false;
        }
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
                if (!m_CurveLegs[leg].IsMarketGood)
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
        // *****************************************************
        // ****            StackQuoteOrders()               ****
        // *****************************************************
        /// <summary>
        /// Called when it is desired to go through quotable price levels and ensure correct order qty exists.
        /// Typically this is done on startup but could also be used in other cases.
        /// </summary>
        private void StackQuoteOrders()
        {
            for (int quoteLegId = 0; quoteLegId < m_CurveLegs.Count; quoteLegId++)
            {
                for (int side = 0; side < 2; side++)
                {
                    this.StackQuoteOrders(quoteLegId, side);
                }
            }
        }
        //
        private void StackQuoteOrders(int legId, int side)
        {
            // TODO: this needs to be finished
            // 1 .  Find minimum number of levels we need to stack
            // 2 .  iterate through them, calling UpdateQuoteOrders for each level
            // 3.   Make sure to check "max" how to handle orders outside of the "max" that are already in the book.

            double bestMarketPrice;
            double tickSize;
            double priceToQuote;
            int sign = QTMath.MktSideToMktSign(side);

            CurveLeg curveLeg = m_CurveLegs[legId];
            if (curveLeg.UserDefinedQuotingEnabled)
            {
                bestMarketPrice = curveLeg.m_Market.Price[side][0];
                tickSize = curveLeg.InstrumentDetails.TickSize;
                for (int i = m_LevelsToQuoteMin - 1; i >= 0; i--)
                {
                    priceToQuote = bestMarketPrice - sign * (tickSize * i);
                    UpdateQuoteOrders(legId, side, priceToQuote);
                }
            }
        }
        //
        // *****************************************************
        // ****           UpdateQuoteOrders()               ****
        // *****************************************************
        //
        private void UpdateQuoteOrders(int quoteLegId, int quoteSide, double quotePrice)
        {
            CurveLeg curveLeg = m_CurveLegs[quoteLegId];
            OrderBook legOrderBook = m_OrderBooks[quoteLegId];                          // find our order book for this leg
            bool isQuoteLeg = curveLeg.m_QuotingEnabled[quoteSide];                     // find our flag for quoting this side of this leg.
            int quoteSign = QTMath.MktSideToMktSign(quoteSide);
            int iPrice = QTMath.RoundToSafeIPrice(quotePrice, quoteSide, curveLeg.InstrumentDetails.TickSize);  // find our order price

            m_OrderWorkSpace.Clear();                                                   // clear our order list
            legOrderBook.GetOrdersByIPrice(quoteSide, iPrice, ref m_OrderWorkSpace);    // find all of our orders at this price

            int aggregatedWorkingQty = 0;
            foreach (Order order in m_OrderWorkSpace)                                   // find the current total working qty
                aggregatedWorkingQty += order.WorkingQtyPending;

            int desiredHedgeQty = 0;
            m_NeededHedgeQty[quoteLegId][quoteSide].TryGetValue(iPrice, out desiredHedgeQty);  // we have qty we need to hedge at this level that needs to be taken into account

            int desiredScratchQty = 0;
            m_NeededScratchQty[quoteLegId].TryGetValue(iPrice, out desiredScratchQty);          // find out how much scratch qty we need at this price

            int desiredQuoteQty = 0;                                                            // this is the qty at a price that is truly only for quoting
            if (isQuoteLeg)
            { // we want to quote this leg!
                // TODO: take into consideration open position!
                int marketIPrice = QTMath.RoundToSafeIPrice(curveLeg.m_Market.Price[quoteSide][0], quoteSide, curveLeg.InstrumentDetails.TickSize);
                int oppSign = quoteSign * -1;
                if (marketIPrice == quotePrice)
                {   // price is on inside market, we need to check state (qty)
                    if (curveLeg.m_IsInsideMarketAboveThresholdJoin[quoteSide])
                    {   // we are allowed to join, make sure we have full desired qty
                        desiredQuoteQty = m_DripQty * quoteSign;                                         // current drip qty
                    }
                    else if (curveLeg.m_IsInsideMarketAboveThresholdPull[quoteSide])
                    {   // we are not allowed to add more, but we do not need to pull.
                        desiredQuoteQty = aggregatedWorkingQty - desiredHedgeQty - desiredScratchQty;    // we want to only quote what is already there that is not a hedge or scratch order
                    }
                    else if (!curveLeg.m_IsInsideMarketAboveThresholdPull[quoteSide])
                    {   // we need to pull all quote orders, need to leave only hedge and scratch orders
                        desiredQuoteQty = 0;
                    }
                    else
                    {   // not sure how we can end up in this state, catch it here just to figure that out.
                        m_Log.NewEntry(LogLevel.Error, "UpdateQuoteOrders: Uknown State Found - Please Examine");
                    }
                }
                else if (quotePrice * oppSign > marketIPrice * oppSign)
                {   // price is "off market", allow full quote qty.
                    desiredQuoteQty = m_DripQty * quoteSign;                                         // current drip qty
                }
                else
                {   // price is through market, we don't want to quote anymore!

                }
            }

            int desiredAggregatedWorkingQty = desiredHedgeQty + desiredQuoteQty + desiredScratchQty;                // this is the actual qty we would like at the price level.
            if (desiredAggregatedWorkingQty != aggregatedWorkingQty)
            {   // we need a qty change
                if (desiredAggregatedWorkingQty * quoteSign > aggregatedWorkingQty * quoteSign)
                {   // we need to add qty at this level
                    Order newOrder;                                                             // create new order to submit
                    m_ExecutionListener.TryCreateOrder(curveLeg.InstrumentDetails.InstrumentName, quoteSide, iPrice,
                                                       desiredAggregatedWorkingQty - aggregatedWorkingQty, out newOrder);
                    newOrder.OrderReason = OrderReason.Quote;                                    // tag our order with "reason"
                    if (m_ExecutionListener.TrySubmitOrder(legOrderBook.BookID, newOrder))
                    {   // new order submitted
                        m_RiskManager.m_NumberOfQuotesThisSecond++;  
                    }
                }
                else
                {   // we need to delete qty at this level, we should delete qty from the orders in the back of our list, these have the worse priority.
                    int qtyToDelete = desiredAggregatedWorkingQty - aggregatedWorkingQty;

                    for (int i = m_OrderWorkSpace.Count - 1; i >= 0; i--)
                    {   // iterate backwards through list, deleting orders in the back of the list first!
                        Order order = m_OrderWorkSpace[i];
                        if (order.WorkingQtyPending * quoteSign > qtyToDelete * quoteSign)
                        {   // this order has more working qty than we need to delete so just reduce this order 
                            int newQty = order.OriginalQtyPending + qtyToDelete;        // take the original qty, and reduce it by the correct amount
                            if(m_ExecutionListener.TryChangeOrderQty(order, newQty))
                                m_RiskManager.m_NumberOfQuotesThisSecond++;
                            break;
                        }
                        else
                        {   // this order needs to be completely deleted
                            qtyToDelete -= order.WorkingQtyPending;                     // find the qty we are deleting and aggregate it
                            if(m_ExecutionListener.TryDeleteOrder(order))
                                m_RiskManager.m_NumberOfQuotesThisSecond++;
                        }
                    }

                    if (qtyToDelete != 0)
                    {
                        m_Log.NewEntry(LogLevel.Error, "UpdateQuoteOrders: qtyToDelete is non zero after removing all orders.  This must be an error");
                    }
                }
            }
        }
        //
        //
        //
        //
        // *****************************************************************
        // ****                   UpdateCurveLegBools                   ****
        // *****************************************************************
        /// <summary>
        /// Called when any states or parameters are chagned to correctly set flags for each leg
        /// </summary>
        private void UpdateCurveLegBools()
        {
            for (int strategySide = 0; strategySide < 2; strategySide++)
            {
                for (int curveLegId = 0; curveLegId < m_CurveLegs.Count; ++curveLegId)
                {
                    if (m_CurveLegs[curveLegId].UserDefinedQuotingEnabled)
                        m_CurveLegs[curveLegId].m_QuotingEnabled[strategySide] = true;
                }
            }
        }
        //
        //
        private void ProcessCurveLegStateChange(CurveLeg curveLeg)
        {
            int curveLegId = m_CurveLegs.IndexOf(curveLeg);

            if (curveLeg.m_Market.IsLastTickBestPriceChange & curveLeg.m_Market.BestDepthUpdated[Order.BuySide] == 0)
            {   // last update was a top of book price change on the bid side
                StackQuoteOrders(curveLegId, Order.BuySide);
            }
            else if (curveLeg.m_ThresholdJoinCrossed[Order.BuySide] | curveLeg.m_ThresholdPullCrossed[Order.BuySide])
            {   // last update was a qty change on the bid side that triggered a cross of our threshold
                UpdateQuoteOrders(curveLegId, Order.BuySide, curveLeg.m_Market.Price[Order.BuySide][0]);
            }


            if (curveLeg.m_Market.IsLastTickBestPriceChange & curveLeg.m_Market.BestDepthUpdated[Order.SellSide] == 0)
            {   // last update was a top of book price change on the ask side
                StackQuoteOrders(curveLegId, Order.SellSide);
            }
            else if (curveLeg.m_ThresholdJoinCrossed[Order.SellSide] | curveLeg.m_ThresholdPullCrossed[Order.SellSide])
            {   // last update was a qty change on the ask side that triggered a cross of our threshold
                UpdateQuoteOrders(curveLegId, Order.SellSide, curveLeg.m_Market.Price[Order.SellSide][0]);
            }
        }
        #endregion//Private Methods

        #region Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                      ****
        // *****************************************************************
        //
        //

        #region Execution Listener Events
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
            m_Log.NewEntry(LogLevel.Major, "CurveTrader is shutting down, attemption to cancel all quoter orders");
            m_IsUserTradingEnabled = false; // stops new order from going out
            CancelAllOrders();              // just to be sure lets cancel all quote orders
        }
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
            CurveLeg curveLeg = m_CurveLegs[internalId];
            curveLeg.m_Market = m_ExecutionContainer.m_Markets[instrDetails.InstrumentName];                    // keep a pointer to the market
            curveLeg.InstrumentDetails = instrDetails;                                                          // save the instrument details
            m_InstrumentToInternalId.Add(instrDetails.InstrumentName, internalId);
            m_PositionBooks[internalId].m_TickSize = curveLeg.InstrumentDetails.TickSize;            // now we can also assign the correct multiplier for the fill book

            // subscribe to events.
            m_ExecutionContainer.m_Markets[instrDetails.InstrumentName].MarketChanged += new EventHandler(curveLeg.Market_MarketChanged);       // each curve leg is responsible for listening to each tick and filtering messages
            m_ExecutionContainer.m_Markets[instrDetails.InstrumentName].MarketBestPriceChanged += new EventHandler(Market_BestPriceChanged);    // if a best price change occurs, we want to know directly

            //
            // Calculate possible tick size for the spread
            //
            double newPossibleMinTickSize = Math.Abs(instrDetails.TickSize * m_CurveLegs[internalId].m_PriceLeg.PriceMultiplier);
            if (double.IsNaN(m_QuoteTickSize) || newPossibleMinTickSize < m_QuoteTickSize)
                m_QuoteTickSize = newPossibleMinTickSize;
            //
            // Find default books for each of these insstruments
            //
            OrderBook defaultBook = m_ExecutionContainer.m_OrderInstruments[curveLeg.m_PriceLeg.InstrumentName].DefaultBook;
            defaultBook.OrderFound += new EventHandler(DefaultOrderBook_OrderFound);
            Dictionary<int, Order> orderById = new Dictionary<int, Order>();
            for (int side = 0; side < 2; side++)
            {
                defaultBook.GetOrdersBySide(side, ref orderById);
                foreach (KeyValuePair<int, Order> idOrderPair in orderById)
                {
                    if (ShouldFoundOrderBeAcquired(idOrderPair.Value))
                    {   // this execution system wants to take control of this order.  
                        // Needs to be taken from the default book to our desired book here.
                        m_ExecutionListener.TryTransferOrderToNewBook(idOrderPair.Value, m_OrderBooks[internalId]);
                    }
                }
                orderById.Clear();                                          // clear our dictionary for reuse
            }


            nInstrumentsFoundCount++;
            if (nInstrumentsFoundCount == m_CurveLegs.Count)
            {   // we have found all the legs we are interested in
                CurveTraderLaunched();
                m_IsLegSetupCompleted = true;
                base.BroadcastAllParameters((IEngineHub)m_Hub, m_ExecutionContainer);                                           // update the gui
                UpdateCurveLegBools();
                //    OnMarketsReadied();
                //    m_RiskManager.Start();
                //    m_ExecutionContainer.ConfirmStrategyLaunched();
            }

            base.BroadcastAllParameters((IEngineHub)m_Hub, m_ExecutionContainer);                                           // update the gui
        }
        //
        #endregion execution listener events

        #region Curve Leg Events
        // *****************************************************************
        // ****             CurveLeg_LeanablePriceChanged()             ****
        // *****************************************************************
        /// <summary>
        /// Curve Leg has experienced a market change that has changed it's leanable
        /// price
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void CurveLeg_LeanablePriceChanged(object sender, EventArgs eventArgs)
        {
            CurveLeg senderInstr = (CurveLeg)sender;
            int instrId = m_CurveLegs.IndexOf(senderInstr);
            if (senderInstr.m_LeanablePriceChanged[Order.BuySide])
            {   // Bid side of this instrument has changed. Quoting legs leaning on this bid, must want to sell
                // this instrument.  Therefore, the following gets the appropriate strategySide.
                int strategySide = UV.Lib.Utilities.QTMath.MktSignToMktSide(-senderInstr.m_PriceLeg.PriceMultiplier);
                //UpdateQuotingLegPrices(strategySide, instrId);  // update our pricing 
                //UpdateQuotingLegOrders(strategySide, instrId);  //update our quotes
            }
            if (senderInstr.m_LeanablePriceChanged[Order.SellSide])
            {
                int strategySide = UV.Lib.Utilities.QTMath.MktSignToMktSide(senderInstr.m_PriceLeg.PriceMultiplier);
                //UpdateQuotingLegPrices(strategySide, instrId);  // update our pricing 
                //UpdateQuotingLegOrders(strategySide, instrId);  //update our quotes
            }

            //UpdateMarketPrice();
        } //Instrument_LeanablePriceChanged
        //
        //
        // *****************************************************************
        // ****             CurveLeg_SqueezeStateChanged()              ****
        // *****************************************************************
        //
        private void CurveLeg_SqueezeStateChanged(object sender, EventArgs eventArgs)
        {

        }
        //
        //
        // *****************************************************************
        // ****              CurveLeg_PullJoinStateChanged              ****
        // *****************************************************************
        /// <summary>
        /// A curve leg has crossed the threshold on market qty needed to join that side of the market (or pull)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void CurveLeg_PullJoinStateChanged(object sender, EventArgs eventArgs)
        {
            CurveLeg senderCurveLeg = (CurveLeg)sender;
            this.ProcessCurveLegStateChange(senderCurveLeg);
        }
        //
        //
        //
        // *****************************************************************
        // ****             CurveLeg_ParameterChanged()                 ****
        // *****************************************************************
        /// <summary>
        /// When a leg parameters change and we need to therefore deal checking all of the orders
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventargs"></param>
        private void CurveLeg_ParameterChanged(object sender, EventArgs eventargs)
        {
            int curveLegId;
            if(m_InstrumentToInternalId.TryGetValue(((CurveLeg)sender).InstrumentDetails.InstrumentName, out curveLegId))
            {
                StackQuoteOrders(curveLegId, Order.BuySide);    // we have to go through the entire stack in case they turned quoting on or off
                StackQuoteOrders(curveLegId, Order.SellSide);
            }
            else
            {   // some error must have hapened
                m_Log.NewEntry(LogLevel.Error, "CurveTrader{0} - Recvd Parameter Change Event for Unknown Curve Leg {1}",
                    EngineID, ((CurveLeg)sender).InstrumentDetails.InstrumentName);
            }
        }
        //
        // *****************************************************************
        // ****              CurveLeg_MarketStateChanged()              ****
        // *****************************************************************
        private void CurveLeg_MarketStateChanged(object sender, EventArgs eventargs)
        {
            bool wasAllLegsGood = m_IsAllLegsGood;
            m_IsAllLegsGood = CheckLegMarketStates();
            if (m_IsAllLegsGood && !wasAllLegsGood)
            { // we have changed to a good state and can now validate our entry prices.
                StackQuoteOrders();
                //if (!m_RiskManager.ValidatePrices(m_StrategyWorkingPrice[Order.BuySide],
                //                                 Order.BuySide, m_StrategyMarketPrice))
                //{ // our bid price is no good, turn our qty to 0
                //    m_TotalDesiredQty[Order.BuySide] = 0;
                //}
                //if (!m_RiskManager.ValidatePrices(m_StrategyWorkingPrice[Order.SellSide],
                //                                Order.SellSide, m_StrategyMarketPrice))
                //{ // our ask price is no good turn our qty to 0
                //    m_TotalDesiredQty[Order.SellSide] = 0;
                //}
            }
            //UpdateQuotingLegBools();
        } //CurveLeg_MarketStateChanged
        #endregion Curve Leg Events

        #region Market Events
        //
        // *********************************************************
        // ****               Market_BestPriceChanged           ****
        // *********************************************************
        private void Market_BestPriceChanged(object sender, EventArgs eventargs)
        {
            Market market = (Market)sender;
            int internalId = m_InstrumentToInternalId[market.Name];
            CurveLeg quoteLeg = m_CurveLegs[internalId];
            for (int side = 0; side < 2; ++side)
            {
                int sgn = UV.Lib.Utilities.QTMath.MktSideToMktSign(side);
                if (quoteLeg.m_QuotingEnabled[side])
                {   // if we are quoting this instrument.
                    //bool isQuotePriceNowOnMarket = CheckOffMarketQuote(internalId, side, m_QuotingLegPrices[internalId][side]);
                    //if (m_IsQuotingLegPriceOffMarket[internalId][side] != isQuotePriceNowOnMarket)
                    //{   // we have changed states.
                    //    UpdateQuoteOrder(internalId, UV.Lib.Utilities.QTMath.MktSignToMktSide(quoteLeg.m_PriceLeg.Weight * sgn));  // we need to update our quote orders since we have now changed states.
                    //    m_IsQuotingLegPriceOffMarket[internalId][side] = isQuotePriceNowOnMarket;  // and set our new state flag.
                    //}
                }
            }//side
        } //Market_BestPriceChanged()
        //
        //
        #endregion // Market Event

        #region Order Events
        //
        // *****************************************************************
        // ****               OrderBook_OrderFilled()                   ****
        // *****************************************************************
        private void OrderBook_OrderFilled(object sender, EventArgs eventArgs)
        {
            // Get information about who was filled.
            FillEventArgs fillEventArgs = (FillEventArgs)eventArgs;

            int internalLegId;
            if (!m_InstrumentToInternalId.TryGetValue(fillEventArgs.InstrumentName, out internalLegId))
            {
                m_Log.NewEntry(LogLevel.Error, "CurveTrader-{0} : Received Fill for Unknown Instrument {1}", this.EngineID, fillEventArgs.InstrumentName);
                return;
            }

            Fill fill = fillEventArgs.Fill;
            int fillSide = QTMath.MktSignToMktSide(fill.Qty);
            int iPrice = (int)(fill.Price / m_CurveLegs[internalLegId].InstrumentDetails.TickSize);
            int positionAtIPriceBeforeFill = m_PositionBooks[internalLegId].GetPositionAtIPrice(iPrice);
            int remainingPositionAtIPrice = m_PositionBooks[internalLegId].AddPosition(fill);            // this will return the remaining non cancelled position at the same price.

            // Below here need to be completely rewritten.  
            int oppSide = QTMath.MktSideToOtherSide(fillSide);
            if (positionAtIPriceBeforeFill != 0)
            {   // we started with a position at this iPrice
                if (Math.Sign(positionAtIPriceBeforeFill) == Math.Sign(fill.Qty))
                {   // we added to our position
                    m_ScratchManager.TryAddPositionToScratcher(internalLegId, iPrice, fill.Qty);
                    UpdateQuoteOrders(internalLegId, oppSide, fill.Price);                          // -Added
                }
                else
                {   // we scratched some of our position, need to see if we flipped it...how to call the scratcher here?
                    // 1. Check to see if the fill came from a scratch order
                    if (fillEventArgs.OrderReason == OrderReason.Scratch)
                    {   // direcly from a scratch order..need to confirm this is actually scratching

                    }
                    else
                    {   // this isn't a scratch order that was filled, we should mnake sure we can remove some qty.
                        m_ScratchManager.TryRemovePositionFromScratcher(internalLegId, iPrice, fill.Qty);
                    }
                }
            }
            else
            {   // we had no position at this IPrice, this is a new position
                // 1. Check if we can scalp another open position
                // 2. Check if this fill can hedge a current position
                // 3. find hedge and lay off for remaining qty and make sure to call scratcher just in case.

                m_ScratchManager.TryAddPositionToScratcher(internalLegId, iPrice, fill.Qty);
                UpdateQuoteOrders(internalLegId, oppSide, fill.Price);                          // -Added
            }

            //// Cheng Impelmentation: 
            // The reason why I added UpdateQuoteOrders(internalLegId, oppSide, fill.Price) is when I placed a 100 lots of sell order at price -30,
            // which has working lots of 21 buy orders. Sometimes it fails to submit quote order at price -30 after the market price sign at -30 changes to sell.
            // The reason for this bug is that orderbook fill event update is later than market change event update, although it seldom happens.
            // The below code is new code to implement scratch fill allocation. It is simple implementation and many other stuffs should also be added.

            int fillSign = Math.Sign(fill.Qty);
            int filledOrderId = fillEventArgs.OrderId;                                      // Use fill order ID to find working price when that order is alive.
            Order filledDeadOrder = null;
            if (m_OrderBooks[internalLegId].TryGet(filledOrderId, out filledDeadOrder, true))
            {   // Find working pending IPrice in the dead book for that order.
                int orderIPrice = filledDeadOrder.IPricePending;
                if (m_NeededScratchQty[internalLegId].ContainsKey(orderIPrice) && m_NeededScratchQty[internalLegId][orderIPrice] != 0)
                {   // Ensure the needed scratch quantity does not change sign. ---- Can we change needed scratch quantities to two side?
                    // Decrement the scratch quantity in a reasonable way.
                    if (m_NeededScratchQty[internalLegId][orderIPrice] * fillSign > fill.Qty * fillSign)
                        m_NeededScratchQty[internalLegId][orderIPrice] -= fill.Qty;
                    else
                        m_NeededScratchQty[internalLegId][orderIPrice] = 0;
                }
                else
                {
                    m_Log.NewEntry(LogLevel.Error, "CurveTrader-{0} : No scratch quantity found in the dictionary at price {1}.", this.EngineID, orderIPrice);
                }
            }
            else
            {
                m_Log.NewEntry(LogLevel.Error, "CurveTrader-{0} : Failed to find dead order for instrument {1} from order book", this.EngineID, fillEventArgs.InstrumentName);
            }
        }
        //
        //
        // *****************************************************************
        // ****               OrderBook_OrderStateChanged()             ****
        // *****************************************************************
        private void OrderBook_OrderStateChanged(object sender, EventArgs eventArgs)
        { // Get information about instrument and order.

            OrderEventArgs orderEventArg = (OrderEventArgs)eventArgs;
            Order updatedOrder = orderEventArg.Order;
            int internalLegId;
            if (!m_InstrumentToInternalId.TryGetValue(orderEventArg.Order.Instrument, out internalLegId))
                return;

            //TODO: Finish building this out!   
        }
        //
        //
        // *****************************************************************
        // ****               DefaultOrderBook_OrderFound()             ****
        // *****************************************************************
        /// <summary>
        /// This is called anytime a new order appears from outside the system, either
        /// on startup or from a user entering a manual order
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void DefaultOrderBook_OrderFound(object sender, EventArgs eventArgs)
        { // Get information about instrument and order.

            OrderEventArgs orderEventArg = (OrderEventArgs)eventArgs;
            Order foundOrder = orderEventArg.Order;
            int internalLegId;
            if (!m_InstrumentToInternalId.TryGetValue(orderEventArg.Order.Instrument, out internalLegId))
                return;

            if (ShouldFoundOrderBeAcquired(foundOrder))
            {   // this is an order we want to take control of, transfer it to our book
                if (m_ExecutionListener.TryTransferOrderToNewBook(foundOrder, m_OrderBooks[internalLegId]))
                {
                    m_Log.NewEntry(LogLevel.Major, "CurveTrader{0}: Capturing found order {1}", this.EngineID, foundOrder);
                }
            }

            //TODO: probably need to force an update here.  Just in case we have more qty than we expected or something. 
        }
        //
        //
        // *****************************************************************
        // ****           ScratchManager_ScratchOrderSubmitted()        ****
        // *****************************************************************
        /// <summary>
        /// Scratch Manager has submitted an order to deal scratch a position we have 
        /// asked him to manage for us.  It is now in our order book 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void ScratchManager_ScratchOrderSubmitted(object sender, EventArgs eventArgs)
        {
            OrderEventArgs orderEventArg = (OrderEventArgs)eventArgs;
            Order scratchOrder = orderEventArg.Order;
            int internalLegId;
            if (!m_InstrumentToInternalId.TryGetValue(scratchOrder.Instrument, out internalLegId))
            {   // something is wrong, we don't know about this instrument
                m_Log.NewEntry(LogLevel.Error, "CurveTrader{0}:ScratchManager_ScratchOrderSubmitted recvd event for uknown instrument {1}",
                    this.EngineName, scratchOrder.Instrument);
                return;
            }

            if (m_NeededScratchQty[internalLegId].ContainsKey(scratchOrder.IPricePending))
            {   // we already have qty waiting to scratch here
                m_NeededScratchQty[internalLegId][scratchOrder.IPricePending] += scratchOrder.OriginalQtyPending;
            }
            else
            {   // this is the first time we have a qty for this level
                m_NeededScratchQty[internalLegId][scratchOrder.IPricePending] = scratchOrder.OriginalQtyPending;
            }
        }

        #endregion //Order events



        #endregion//Event Handlers

        #region IOrderEngine Implementation
        // ***********************************************************************
        // ****                IOrderEngine Implementation                    ****
        // ***********************************************************************
        // *****************************************************************
        // ****                   CancelAllOrders()                     ****
        // *****************************************************************
        /// <summary>
        /// Called by Risk Manager or User who would like to cancel all exisiting orders.
        /// </summary>
        /// 
        public void CancelAllOrders()
        {
            if (m_Log != null)
                m_Log.NewEntry(LogLevel.Error, "CruveTrader: CancelAllOrders Not Implement Yet");
        }
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
        public void Start()
        {
            m_ExecutionListener.Initialized += new EventHandler(ExecutionListener_Initialized);
            m_ExecutionListener.Stopping += new EventHandler(ExecutionListener_Stopping);
            System.Threading.Thread thread = new System.Threading.Thread(m_ExecutionListener.InitializeThread);
            thread.Name = string.Format("{0}:{1}", this.GetType().Name.ToString(), m_ExecutionContainer.EngineContainerID);
            thread.Start();
        }
        //
        //
        // *************************************************************
        // ****                        Stop                         ****
        // *************************************************************
        /// <summary>
        /// Threadsafe call to being shutdown procedures.
        /// </summary>
        public void Stop()
        {
            m_ExecutionListener.StopThread();
        }
        #endregion

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
            if (elements == null)
                elements = new List<IStringifiable>();
            foreach (CurveLeg curveLeg in m_CurveLegs)
                elements.Add(curveLeg);
            return elements;
        }
        public override void SetAttributes(Dictionary<string, string> attributes)
        {
            base.SetAttributes(attributes);
            int i;
            foreach (KeyValuePair<string, string> attr in attributes)
            {
                if (attr.Key.Equals("MaxLevelsToQuote", StringComparison.OrdinalIgnoreCase) && int.TryParse(attr.Value, out i))
                    this.m_LevelsToQuoteMax = i;
                else if (attr.Key.Equals("MinLevelsToQuote", StringComparison.OrdinalIgnoreCase) && int.TryParse(attr.Value, out i))
                    this.m_LevelsToQuoteMin = i;
            }
        }
        public override void AddSubElement(IStringifiable subElement)
        {
            base.AddSubElement(subElement);
            if (subElement is CurveLeg)
            {   // find all curve traders
                CurveLeg curveLeg = (CurveLeg)subElement;
                m_CurveLegs.Add(curveLeg);
            }
            else if (subElement is ScratchManager)
            {
                m_ScratchManager = (ScratchManager)subElement;
                m_ScratchManager.m_CurveTrader = this;
            }
            else if(subElement is RiskManagerCurveTrader)
            {
                m_RiskManager = (RiskManagerCurveTrader)subElement;
            }
        }
        #endregion

    }//end class
}
