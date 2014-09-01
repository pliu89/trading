using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

namespace UV.Strategies.Engines
{
    using UV.Lib.IO.Xml;
    using UV.Lib.Products;
    using UV.Lib.Hubs;
    using UV.Lib.OrderBookHubs;
    using UV.Lib.Engines;
    using UV.Lib.Fills;
    using UV.Lib.Utilities;


    using UV.Lib.Application;

    /// <summary>
    /// This OrderEngine is the class necessary to send orders to the market for 
    /// a single leg strategy.
    /// </summary>
    public class OrderEngine : Engine, IOrderEngine, IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // External services
        protected LogHub Log = null;
        protected StrategyHub m_StrategyHub = null;
        protected Strategy m_Strategy = null;
        protected OrderBookHub m_OrderHub = null;
        protected Risk.RiskManager m_RiskManager = null;

        // Instrument 
        public InstrumentName m_Instrument;
        public InstrumentDetails m_InstrumentDetails;

        // Order and Fill Books
        public OrderBook m_OrderBook;
        public FillBook m_FillBook;
        public int[] m_StrategyPosition = new int[2];

        // Internal fields
        protected bool m_IsMarketReady = false;
        protected InstrumentName m_PendingFillBookToCreate;                     // fill book we want to create once we get a intitialized market

        // engine variables
        internal double[] m_StrategyWorkingPrice = new double[2];               // desired prices to work [side]
        internal int[] m_TotalDesiredQty = new int[2];                          // desired qty on each side of the market 
        internal int m_DripQty = 1;

        private bool[] m_IsQuotePriceOffMarket = new bool[2];                   // array for each side for state of quote order.

        private bool m_UseGTC;                                                  // should we submit GTC orders
        public bool m_IsRiskCheckPassed;
        private double m_QuoteTickSize = double.NaN;                            // Nan to start with 

        List<Order> m_OrderWorkSpace = new List<Order>();                       // clear before each use!
        #endregion// members

        #region Constructors & Setup Methods
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        public OrderEngine()
        {
        }
        //
        // *************************************************************
        // ****                 Setup Initialize()                  ****
        // *************************************************************
        /// <summary>
        /// Since I depend critically on an OrderBookHub, I will look for them now.
        /// </summary>
        /// <param name="myEngineHub"></param>
        /// <param name="engineContainer"></param>
        /// <param name="engineID"></param>
        /// <param name="setupGui"></param>
        protected override void SetupInitialize(IEngineHub myEngineHub, IEngineContainer engineContainer, int engineID, bool setupGui)
        {
            base.SetupInitialize(myEngineHub, engineContainer, engineID, setupGui);

            // Collect services that I need.
            m_StrategyHub = (StrategyHub)myEngineHub;
            this.Log = m_StrategyHub.Log;
            Strategy strategy = (Strategy)engineContainer;
            m_Strategy = strategy;

            // Locate an order hub.
            List<IService> services = AppServices.GetInstance().GetServices(typeof(OrderBookHub));
            if (services.Count < 1)
                m_StrategyHub.Log.NewEntry(LogLevel.Warning, "OrderEngine: {0} failed to located OrderHub.", strategy.Name);
            else
                m_OrderHub = (OrderBookHub)services[0];

        }// SetupInitialize()
        //
        // *************************************************************
        // ****                    Setup Begin()                   ****
        // *************************************************************
        public override void SetupBegin(IEngineHub myEngineHub, IEngineContainer engineContainer)
        {
            base.SetupBegin(myEngineHub, engineContainer);
            foreach (IEngine iEng in engineContainer.GetEngines())
                if (iEng is Risk.RiskManager)
                    m_RiskManager = (Risk.RiskManager)iEng;
            if (m_RiskManager == null)
                throw new NotImplementedException("All Strategies Must Have a Risk Manager, Please Add One To Your User Config");
        }//SetupBegin().
        //    
        //
        //       
        #endregion//Constructors

        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        public int TotalDesiredBuyQty
        {
            get { return m_TotalDesiredQty[Order.BuySide]; }
            set
            {
                if (m_RiskManager.ValidatePrices(m_StrategyWorkingPrice[Order.BuySide],
                    Order.BuySide, m_Instrument))
                {
                    m_TotalDesiredQty[Order.BuySide] = value;
                    Quote();
                }
            }
        }
        public int TotalDesiredSellQty
        {
            get { return m_TotalDesiredQty[Order.SellSide]; }
            set
            {
                if (m_RiskManager.ValidatePrices(m_StrategyWorkingPrice[Order.SellSide],
                   Order.SellSide, m_Instrument))
                {
                    m_TotalDesiredQty[Order.SellSide] = value;
                    Quote();
                }
            }
        }
        public int DripQty
        {
            get { return m_DripQty; }
            set
            {
                m_DripQty = value;
                Quote();
            }
        }
        public double WorkPriceBuy
        {
            get { return m_StrategyWorkingPrice[Order.BuySide]; }
            set
            {
                if (m_RiskManager.ValidatePrices(value, Order.BuySide, m_Instrument))
                {// validate our buy price before setting
                    m_StrategyWorkingPrice[Order.BuySide] = value;
                    Quote();
                }
            }
        }
        public double WorkPriceSell
        {
            get { return m_StrategyWorkingPrice[Order.SellSide]; }
            set
            {
                if (m_RiskManager.ValidatePrices(value, Order.SellSide, m_Instrument))
                {// validate our sell price before setting
                    m_StrategyWorkingPrice[Order.SellSide] = value;
                    Quote();
                }
            }
        }
        public bool UseGTC
        {
            get { return m_UseGTC; }
            set { m_UseGTC = value; }
        }
        /// <summary>
        /// For a single leg order engine this is simply the tick size of the leg.
        /// </summary>
        public double QuoteTickSize
        {
            get { return m_QuoteTickSize; }
            set { m_QuoteTickSize = value; }
        }
        //
        #endregion//Properties

        #region Public Methods
        // *****************************************************************
        // ****                 Public Methods                         ****
        // *****************************************************************
        //
        //
        // *************************************************************
        // ****             MarketInstrumentInitialized()           ****
        // *************************************************************
        //
        /// <summary>
        /// called once the market for all instruments is subscribed to and we have instrument
        /// details
        /// </summary>
        public override void MarketInstrumentInitialized(Lib.BookHubs.Book marketBook)
        {
            if (m_IsMarketReady)
                return;
            m_IsMarketReady = true;
            if (m_StrategyHub.m_Market.TryGetInstrumentDetails(m_Instrument, out m_InstrumentDetails))
            {
                if (m_FillBook == null)
                    m_FillBook = new FillBook(m_Instrument.ToString(), m_InstrumentDetails.Multiplier);
                if(double.IsNaN(m_QuoteTickSize))                       // if our user hasn't defined this yet
                    m_QuoteTickSize = m_InstrumentDetails.TickSize;     // set it to the default tick size here
            }
            else
                Log.NewEntry(LogLevel.Error,
                    "OrderEngine:MarketInstrumentInitialized failed to get instrument details and create order book for {0}", m_Instrument);
        }
        //
        // *************************************************************
        // ****                      AddBook()                      ****
        // *************************************************************
        //
        /// <summary>
        /// Strategy wants to get a book for a single instrument.  This method is usually called by the Strategy
        /// or pricing model during SetupComplete().
        /// </summary>
        /// <param name="instrumentName"></param>
        public void AddBook(InstrumentName instrumentName)
        {
            if (m_Instrument != null)
            {   // a new instrument we should get a book for.
                m_Instrument = instrumentName;
                m_OrderBook = m_OrderHub.CreateOrderBook(instrumentName);
                m_StrategyHub.SubscribeToFills(m_Strategy, m_OrderBook);                                  // tell StrategyHub what events i want, and who they belong to.
                m_StrategyHub.SubscribeToMajorOrderStatusEvents(m_Strategy, m_OrderBook);
                m_StrategyHub.SubscribeToOrderSubmitted(m_Strategy, m_OrderBook);
                if (m_IsMarketReady && m_StrategyHub.m_Market.TryGetInstrumentDetails(instrumentName, out m_InstrumentDetails))
                    m_FillBook = new FillBook(instrumentName.ToString(), m_InstrumentDetails.Multiplier);
            }
        }// AddBook()
        //
        //
        // *************************************************************
        // ****                      Quote()                        ****
        // *************************************************************
        //
        //
        /// <summary>
        /// This sets the inner market price and qty for the trade.
        /// For now it assumes the strategy has only one OrderInstrument, which is always
        /// the case when we have an "ExecutionStrategy" deployed; eg, an autospreader.
        /// 
        /// qty must be signed, negative for sell qty's
        /// 
        /// This will validate all prices prior to setting them.
        /// </summary>
        /// <param name="tradeSide"></param>
        /// <param name="price"></param>
        /// <param name="qty">Signed qty</param>
        /// <param name="aBook"></param>
        public void Quote(int tradeSide, double price, int qty, UV.Lib.BookHubs.Book aBook)
        {
            if (qty != 0 && tradeSide != QTMath.MktSignToMktSide(qty))
            { // mismatch qty and sides
                Log.NewEntry(LogLevel.Warning, "Quote: tradeSide and side implied by qty sign do not match, rejecting quote update");
                return;
            }
            if (!m_IsMarketReady)
            { // we cant't even validate prices yet.
                Log.NewEntry(LogLevel.Major, "Quote: Market has not been intialized yet. Order's will not be sent until market is intialized");
                return;
            }
            if (!QTMath.IsPriceEqual(m_StrategyWorkingPrice[tradeSide], price, m_InstrumentDetails.TickSize))
            { // price is different.
                if (m_RiskManager.ValidatePrices(price, tradeSide, m_Instrument, aBook))
                { // our prices are valid so we can save variables
                    m_StrategyWorkingPrice[tradeSide] = price;
                    m_TotalDesiredQty[tradeSide] = qty;
                }
            }
            else
            { // price hasn't changed so if it was already set
                m_TotalDesiredQty[tradeSide] = qty;
            }
            Quote();                                    // go ahead an update our orders
        }//Quote()
        //
        //
        #endregion//Public Methods

        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        // *************************************************************
        // ****                      Quote()                        ****
        // *************************************************************
        /// <summary>
        /// This private method can be called on any state change to update our orders 
        /// in the market once all our internal prices, flags, and states are set.
        /// </summary>
        private void Quote()
        {
            bool isAllowedToWorkOrder;
            int marketSign;
            int qtyToWork;
            for (int side = 0; side < 2; side++)
            {
                marketSign = QTMath.MktSideToMktSign(side);
                isAllowedToWorkOrder = (Math.Abs(m_StrategyPosition[side]) < Math.Abs(m_TotalDesiredQty[side])) && m_IsRiskCheckPassed && m_DripQty > 0;
                // find our working order.
                m_OrderWorkSpace.Clear();
                m_OrderBook.GetOrdersByRank(side, 0, ref m_OrderWorkSpace);
                Order quoteOrder = null;
                int nthOrder = 0;
                while (nthOrder < m_OrderWorkSpace.Count && quoteOrder == null)                                     // loop until we find a living quoteOrder
                {
                    if (m_OrderWorkSpace[nthOrder].OrderStateConfirmed != OrderState.Dead)
                        quoteOrder = m_OrderWorkSpace[nthOrder];
                    nthOrder++;
                }
                if (isAllowedToWorkOrder)
                {
                    qtyToWork = QTMath.CalculateDripQty(m_DripQty, m_TotalDesiredQty[side], m_StrategyPosition[side]);
                    int iPrice = (int)(m_StrategyWorkingPrice[side] / m_InstrumentDetails.TickSize);
                    if (quoteOrder == null && qtyToWork != 0)
                    { // we aren't working an order, but would like to be
                        Order order;
                        if (m_OrderHub.TryCreateOrder(m_Instrument, side, iPrice, qtyToWork, out order))
                        {
                            if (m_UseGTC)
                                order.OrderTIF = OrderTIF.GTC;
                            m_OrderHub.TrySubmitOrder(m_OrderBook.BookID, order);
                            m_RiskManager.m_NumberOfQuotesThisSecond++;
                        }
                    }
                    else if (!QTMath.IsPriceEqual(quoteOrder.PricePending, m_StrategyWorkingPrice[side], m_InstrumentDetails.TickSize)
                             || quoteOrder.WorkingQtyPending != qtyToWork)
                    { // we need to change price, qty, or both
                        if (m_OrderHub.TryChangeOrderPriceAndQty(quoteOrder, qtyToWork, iPrice))                        // this will handle everthing for us!
                            m_RiskManager.m_NumberOfQuotesThisSecond += 2;                                              // cancel and replace = 2?
                        else
                            Log.NewEntry(LogLevel.Warning, "Quote: Failed To Modify order {0}", quoteOrder);            // do we want to do anything more here?
                    }
                }
                else if (quoteOrder != null)
                { // we are working an order, but don't want to be!
                    if (!m_OrderHub.TryDeleteOrder(quoteOrder))
                        Log.NewEntry(LogLevel.Warning, "Quote: Failed to canel order {0}", quoteOrder);
                }
            }
        }//Quote()
        //
        //
        //
        //
        //
        #endregion//private Methods

        #region IOrderEngine Implementation
        //
        //
        // *************************************************************
        // ****                      Filled()                       ****
        // *************************************************************
        //
        /// <summary>
        /// Called by the StrategyHub when it received a fillEventArg from an OrderBook
        /// that this engine subscribed to.
        /// </summary>
        /// <param name="fillEventArgs"></param>
        /// <returns>null is no fill is generated.</returns>
        public Fill Filled(FillEventArgs fillEventArgs)
        {
            m_FillBook.TryAdd(fillEventArgs.Fill);
            int fillSide = QTMath.MktSignToMktSide(fillEventArgs.Fill.Qty);
            m_StrategyPosition[fillSide] += fillEventArgs.Fill.Qty;
            m_RiskManager.Filled(fillEventArgs);                                    // make sure our risk manager knows
            return fillEventArgs.Fill;                                              // pass fill through to other engines.
        }//Filled()
        //
        //
        // *************************************************************
        // ****                 OrderStateChanged()                 ****
        // *************************************************************
        /// <summary>
        /// Called by the strategy hub when it recieves and order state change event from an orderbook
        /// </summary>
        /// <param name="orderEventArgs"></param>
        public void OrderStateChanged(OrderEventArgs orderEventArgs)
        {
            Log.NewEntry(LogLevel.Major, "OrderStateChanged: Order State Updates : Not implemented yet");
        }
        //
        // *************************************************************
        // ****                OrderSubmitted()                     ****
        // *************************************************************
        /// <summary>
        /// Called by the strategy hub when it recieves confirmation of an order submission
        /// </summary>
        /// <param name="orderEventArgs"></param>
        public void OrderSubmitted(OrderEventArgs orderEventArgs)
        {
            m_RiskManager.OrderBook(orderEventArgs);
        }
        //
        //
        //
        // *************************************************************
        // ****                CancelAllOrders()                    ****
        // *************************************************************
        /// <summary>
        /// Caller would like to cancell all outstanding orders
        /// </summary>
        public void CancelAllOrders()
        {
            m_OrderWorkSpace.Clear();
            for (int side = 0; side < 2; side++)
            {
                m_OrderBook.GetOrdersByRank(side, 0, ref m_OrderWorkSpace);
                foreach (Order order in m_OrderWorkSpace)
                    m_OrderHub.TryDeleteOrder(order);
            }
        }
        //
        #endregion //IOrderEngine Implementation

        #region no Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        #endregion//Event Handlers

        #region IStringifiable
        // *************************************************
        // ****             IStringifiable              ****
        // *************************************************
        string IStringifiable.GetAttributes()
        {
            StringBuilder s = new StringBuilder();
            return s.ToString();
        }
        List<IStringifiable> IStringifiable.GetElements()
        {
            //List<IStringifiable> elements = new List<IStringifiable>();
            //foreach (InstrumentName leg in this.m_DesiredInstruments)
            //    elements.Add(leg);
            // Exit
            //return elements;
            return null;
        }
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
            base.SetAttributes(attributes);
            double x;
            foreach (KeyValuePair<string, string> attr in attributes)
            {
                if (attr.Key == "QuoteTickSize" && double.TryParse(attr.Value, out x))
                    this.QuoteTickSize = x;
            }
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {
            //if (subElement is PriceLeg)
            //    m_Legs.Add((PriceLeg)subElement);
        }
        #endregion// IStringifiable


    }//end class
}
