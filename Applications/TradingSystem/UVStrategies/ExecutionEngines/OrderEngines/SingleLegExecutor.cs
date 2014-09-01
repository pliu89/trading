using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

namespace UV.Strategies.ExecutionEngines.OrderEngines
{
    using UV.Lib.IO.Xml;
    using UV.Lib.Products;
    using UV.Lib.Hubs;
    using UV.Lib.OrderBooks;
    using UV.Lib.Engines;
    using UV.Lib.Fills;
    using UV.Lib.Utilities;
    using UV.Lib.MarketHubs;
    using UV.Lib.BookHubs;
    using UV.Lib.Application;
    using UV.Lib.FrontEnds.GuiTemplates;

    using UV.Strategies.StrategyHubs;
    using UV.Strategies.StrategyEngines;
    using UV.Strategies.ExecutionEngines.Risk;
    using UV.Strategies.ExecutionHubs;
    using UV.Strategies.ExecutionHubs.ExecutionContainers;


    /// <summary>
    /// This SingleLegExecutor is the class necessary to send orders to the market for 
    /// a single leg strategy.
    /// 
    /// TODO: 
    ///     1. Implement functionality for weights of instruments
    /// </summary>
    public class SingleLegExecutor : Engine, IStringifiable, IOrderEngineParameters, IOrderEngine
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // External services
        protected LogHub Log = null;
        public Hub m_Hub = null;
        public ThreadContainer m_ExecutionContainer = null;
        protected UV.Strategies.ExecutionHubs.ExecutionListener m_ExecutionListener = null;
        protected ExecutionEngines.Risk.RiskManager m_RiskManager = null;


        // Instrument 
        public InstrumentDetails m_InstrumentDetails;
        public PriceLeg m_PriceLeg;

        // Order and Fill Books
        public OrderBook m_OrderBook;
        private FillBook m_FillBook;
        public int[] m_StrategyPosition = new int[2];

        // Internal fields
        protected InstrumentName m_PendingFillBookToCreate;                     // fill book we want to create once we get a intitialized market

        // engine variables
        internal double[] m_StrategyWorkingPrice = new double[2];               // desired prices to work [side]
        internal int[] m_TotalDesiredQty = new int[2];                          // desired qty on each side of the market 
        internal int m_DripQty = 1;
        private bool[] m_IsQuotePriceOffMarket = new bool[2];                   // array for each side for state of quote order.
        public bool m_IsRiskCheckPassed;
        public bool m_IsLegSetUpComplete = false;                               // state flag for set up.
        private bool m_IsUserTradingEnabled;
        private bool m_UseGTC;                                                  // should we submit GTC orders
        private double m_QuoteTickSize = double.NaN;                            // Nan to start with 
        private string m_DefaultAccount;                                        // default account to send orders


        List<Order> m_OrderWorkSpace = new List<Order>();                       // clear before each use!

        //Synthetic orders
        public SyntheticOrder[] m_OpenSyntheticOrders = new SyntheticOrder[2];  // each mkt side...currently only storing 2 orders.
        #endregion// members

        #region Constructors & Setup Methods
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        public SingleLegExecutor()
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
            base.SetupInitialize(myEngineHub, engineContainer, engineID, false);
            EngineGui engineGui = base.SetupGuiTemplates();
            engineGui.LowerHudFullName = typeof(OrderEngineHud).FullName;


            // Collect services that I need.
            m_Hub = (Hub)myEngineHub;
            this.Log = m_Hub.Log;
            m_ExecutionContainer = (ThreadContainer)engineContainer;
        }// SetupInitialize()
        //
        // *************************************************************
        // ****                    Setup Begin()                    ****
        // *************************************************************
        public override void SetupBegin(IEngineHub myEngineHub, IEngineContainer engineContainer)
        {
            base.SetupBegin(myEngineHub, engineContainer);

            //
            // Subscribe to events
            //
            m_ExecutionListener.InstrumentFound += new EventHandler(ExecutionListener_InstrumentsFound);

            foreach (IEngine iEng in engineContainer.GetEngines())
                if (iEng is ExecutionEngines.Risk.RiskManager)
                    m_RiskManager = (ExecutionEngines.Risk.RiskManager)iEng;
            if (m_RiskManager == null)
                throw new NotImplementedException("All Strategies Must Have a Risk Manager, Please Add One To Your User Config - Must be UV.Execution.Risk.RiskManager type");
        }//SetupBegin().
        //
        // *************************************************************
        // ****                    SetupComplete()                  ****
        // *************************************************************
        public override void SetupComplete()
        {
            base.SetupComplete();
            // if we create order books here we know risk is already listening for the events.
            if (!QTMath.IsNearEqual(m_PriceLeg.Weight, 1, .01))
                Log.NewEntry(LogLevel.Error, "SingleLegExecutor: Does not have functionality for legs with weight greater than 1 implemented yet");
            m_OrderBook = m_ExecutionListener.CreateOrderBook(m_PriceLeg.InstrumentName, m_DefaultAccount);
            m_OrderBook.OrderFilled += new EventHandler(OrderBook_OrderFilled);
            m_OrderBook.OrderStateChanged += new EventHandler(OrderBook_OrderStateChanged);
        }
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
                    Order.BuySide, m_PriceLeg.InstrumentName))
                {
                    m_TotalDesiredQty[Order.BuySide] = value;
                    if (m_IsLegSetUpComplete)
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
                   Order.SellSide, m_PriceLeg.InstrumentName))
                {
                    m_TotalDesiredQty[Order.SellSide] = value;
                    if (m_IsLegSetUpComplete)
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
                if (m_IsLegSetUpComplete)
                    Quote();
            }
        }
        public double WorkPriceBuy
        {
            get { return m_StrategyWorkingPrice[Order.BuySide]; }
            set
            {
                if (m_RiskManager.ValidatePrices(value, Order.BuySide, m_PriceLeg.InstrumentName))
                {// validate our buy price before setting
                    m_StrategyWorkingPrice[Order.BuySide] = value;
                    if (m_IsLegSetUpComplete)
                        Quote();
                }
            }
        }
        public double WorkPriceSell
        {
            get { return m_StrategyWorkingPrice[Order.SellSide]; }
            set
            {
                if (m_RiskManager.ValidatePrices(value, Order.SellSide, m_PriceLeg.InstrumentName))
                {// validate our sell price before setting
                    m_StrategyWorkingPrice[Order.SellSide] = value;
                    if (m_IsLegSetUpComplete)
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
        /// <summary>
        /// Is this engine okay to submit orders
        /// </summary>
        public bool IsRiskCheckPassed
        {
            get { return m_IsRiskCheckPassed; }
            set { m_IsRiskCheckPassed = value; }
        }
        //

        //
        //
        /// <summary>
        /// User defined flag for allow order submission. Defaults to false and must
        /// be set to true by user.
        /// </summary>
        public bool IsUserTradingEnabled
        {
            get { return m_IsUserTradingEnabled; }
            set
            {
                m_IsUserTradingEnabled = value;
                if (m_IsLegSetUpComplete)
                    Quote();
            }
        }
        //
        //
        public string DefaultAccount
        {
            get { return m_DefaultAccount; }
        }

        #region Property like methods to hide from gui!
        //
        public ExecutionListener GetExecutionListener()
        {
            return m_ExecutionListener;
        }
        public void SetExecutionListener(ExecutionListener executionListener)
        {
            m_ExecutionListener = executionListener;
            m_ExecutionListener.ExecutionContainer = m_ExecutionContainer;      // assign my execution container to the listener 
        }
        //
        //
        /// <summary>
        /// Fill book for this single leg.
        /// </summary>
        public FillBook GetFillBook()
        {
            return m_FillBook;
        }
        #endregion

        #endregion//Properties

        #region Public Methods
        // *****************************************************************
        // ****                 Public Methods                          ****
        // *****************************************************************
        //
        //
        public override void ProcessEvent(EventArgs e)
        {
            base.ProcessEvent(e);
            if (e is EngineEventArgs)
            {
                EngineEventArgs engineEventArg = (EngineEventArgs)e;
                if (engineEventArg.MsgType == EngineEventArgs.EventType.SyntheticOrder)
                {
                    SyntheticOrder syntheticOrder = (SyntheticOrder)engineEventArg.DataObjectList[0];
                    if (m_OpenSyntheticOrders[syntheticOrder.Side] == null)
                    { // this is the first time we have seen this order
                        m_OpenSyntheticOrders[syntheticOrder.Side] = syntheticOrder;
                        Quote(syntheticOrder.Side, syntheticOrder.Price, syntheticOrder.Qty);
                    }
                    else
                    { // we have seen this order before, check what do do...currently not implementing much
                        // todo: how to save, check what user is requesting to change, etc
                        m_OpenSyntheticOrders[syntheticOrder.Side].Qty = syntheticOrder.Qty;
                        m_OpenSyntheticOrders[syntheticOrder.Side].Price = syntheticOrder.Price;
                        m_OpenSyntheticOrders[syntheticOrder.Side].TradeReason = syntheticOrder.TradeReason;
                        Quote(syntheticOrder.Side, syntheticOrder.Price, syntheticOrder.Qty);
                    }
                }
            }
        }
        //
        // *************************************************************
        // ****                      Quote()                       *****
        // *************************************************************
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
        public void Quote(int tradeSide, double price, int qty)
        {
            if (qty != 0 && tradeSide != QTMath.MktSignToMktSide(qty))
            { // mismatch qty and sides
                Log.NewEntry(LogLevel.Warning, "Quote: tradeSide and side implied by qty sign do not match, rejecting quote update");
                return;
            }
            if (!m_IsLegSetUpComplete)
            { // we cant't even validate prices yet.
                Log.NewEntry(LogLevel.Major, "Quote: Market has not been intialized yet. Order's will not be sent until market is intialized");
                return;
            }

            price = price / m_PriceLeg.PriceMultiplier;             // convert from strat price to instrument price
            qty = (int)(qty * Math.Abs(m_PriceLeg.Weight));                   // convert from strat qty to instrument qty

            if (!QTMath.IsPriceEqual(m_StrategyWorkingPrice[tradeSide], price, m_InstrumentDetails.TickSize) || m_TotalDesiredQty[tradeSide] != qty)
            { // price is different.
                if (m_RiskManager.ValidatePrices(price, tradeSide, m_PriceLeg.InstrumentName) || qty == 0)
                { // our prices are valid so we can save variables
                    m_StrategyWorkingPrice[tradeSide] = price;
                    m_TotalDesiredQty[tradeSide] = qty;
                    Log.NewEntry(LogLevel.Major, "Quote:{4} Working {1} {0} @ {2} in {3}",                  // while this may be silly to log first, it is here for readability
                            m_OpenSyntheticOrders[tradeSide].TradeReason,  // 0       
                            qty, // 1
                            price, // 2
                            m_InstrumentDetails.InstrumentName,  //3
                            m_ExecutionContainer.EngineContainerID);//4
                    Quote();                                    // go ahead an update our orders
                }
            }

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
            int orderSign;
            int qtyToWork;
            for (int side = 0; side < 2; side++)
            {
                orderSign = QTMath.MktSideToMktSign(side);
                isAllowedToWorkOrder = (Math.Abs(m_StrategyPosition[side]) < Math.Abs(m_TotalDesiredQty[side])) && m_IsRiskCheckPassed && m_DripQty > 0 && m_IsUserTradingEnabled;
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
                    int iPrice = orderSign * (int)System.Math.Floor(orderSign * m_StrategyWorkingPrice[side] / m_InstrumentDetails.TickSize);    // integer price to quote- safely rounded away.
                    if (quoteOrder == null && qtyToWork != 0)
                    { // we aren't working an order, but would like to be
                        Order order;
                        if (m_ExecutionListener.TryCreateOrder(m_PriceLeg.InstrumentName, side, iPrice, qtyToWork, out order))
                        {
                            if (m_OpenSyntheticOrders[side] != null)
                            {
                                order.OrderReason = OrderReason.Quote;
                                Log.NewEntry(LogLevel.Major, "SingleLegExecutor: {0} {1} being submitted for {2}", m_ExecutionContainer.EngineContainerID, order, m_OpenSyntheticOrders[side].TradeReason);
                            }
                            if (m_UseGTC)
                                order.OrderTIF = OrderTIF.GTC;
                            m_ExecutionListener.TrySubmitOrder(m_OrderBook.BookID, order);
                            m_RiskManager.m_NumberOfQuotesThisSecond++;
                        }
                    }
                    else
                    {
                        if (qtyToWork == 0)
                        { // we need to delete this order
                            m_ExecutionListener.TryDeleteOrder(quoteOrder);
                            m_RiskManager.m_NumberOfQuotesThisSecond += 1;
                        }
                        else
                        { // we need to edit the order.
                            if (quoteOrder.PricePending != m_StrategyWorkingPrice[side])
                            { // we need to change prices.
                                Log.NewEntry(LogLevel.Major, "SingleLegExecutor: {0} {1} changing order to {2} @ {3}", m_ExecutionContainer.EngineContainerID, quoteOrder, qtyToWork, m_StrategyWorkingPrice[side]);
                                m_ExecutionListener.TryChangeOrderPrice(quoteOrder, iPrice, qtyToWork);
                            }
                            if (quoteOrder.WorkingQtyPending != qtyToWork)
                            { // we need to change qty
                                Log.NewEntry(LogLevel.Major, "SingleLegExecutor: {0} {1} changing qty to {2}", m_ExecutionContainer.EngineContainerID, quoteOrder, qtyToWork);
                                m_ExecutionListener.TryChangeOrderQty(quoteOrder, qtyToWork);
                            }
                            m_RiskManager.m_NumberOfQuotesThisSecond += 2;                                              // cancel and replace = 2?
                        }
                    }
                }
                else if (quoteOrder != null)
                { // we are working an order, but don't want to be!
                    Log.NewEntry(LogLevel.Major, "SingleLegExecutor: {0} deleting {1} ", m_ExecutionContainer.EngineContainerID, quoteOrder);
                    if (!m_ExecutionListener.TryDeleteOrder(quoteOrder))
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
        // *************************************************************
        // ****           IOrderEngine Implementation()             ****
        // *************************************************************
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
            thread.Name = this.GetType().Name.ToString();
            thread.Start();
        }
        //
        //
        // *************************************************************
        // ****                 AddExecutionListener                ****
        // *************************************************************
        //
        /// <summary>
        /// Called once after SetUpIntialize and before start.
        /// </summary>
        public void AddExecutionListener(ExecutionListener execListener)
        {
            m_ExecutionListener = execListener;
            m_ExecutionListener.ExecutionContainer = m_ExecutionContainer;      // assign my execution container to the listener 
        }
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
        //
        //
        //
        // *******************************************************
        // ****             CancelAllOrders()                 ****
        // *******************************************************
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
                    m_ExecutionListener.TryDeleteOrder(order);
            }
        }
        //
        #endregion //IOrderEngine Implementation

        #region Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        //
        // *****************************************************************
        // ****                 OrderBook_OrderFilled()                 ****
        // *****************************************************************
        private void OrderBook_OrderFilled(object sender, EventArgs eventArgs)
        {
            FillEventArgs fillEventArgs = (FillEventArgs)eventArgs;
            m_FillBook.TryAdd(fillEventArgs.Fill);
            int fillSide = QTMath.MktSignToMktSide(fillEventArgs.Fill.Qty);
            m_StrategyPosition[fillSide] += fillEventArgs.Fill.Qty;
            fillEventArgs.Fill.Price = fillEventArgs.Fill.Price * m_PriceLeg.PriceMultiplier;
            Quote();    // this will update our orders now that we got filled, and resubmit if we are dripping etc..
            //
            // Create a synthetic fill to pass back to the strategy.
            //
            if (m_OpenSyntheticOrders[fillSide] != null)
            {
                SyntheticFill newSyntheticFill = SyntheticFill.CreateSyntheticFillFromFill(fillEventArgs.Fill);
                m_OpenSyntheticOrders[fillSide].m_SyntheticFills.Add(newSyntheticFill);
                m_ExecutionContainer.SendSyntheticOrderToRemote(m_OpenSyntheticOrders[fillSide]);
            }

        }
        //
        //
        // *****************************************************************
        // ****             OrderBook_OrderStateChanged()              ****
        // *****************************************************************
        private void OrderBook_OrderStateChanged(object sender, EventArgs eventArgs)
        { }
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
            m_InstrumentDetails = instrEventArgs.InstrumentDetails;
            m_FillBook = new FillBook(m_PriceLeg.InstrumentName.ToString(), m_InstrumentDetails.Multiplier);
            if (double.IsNaN(m_QuoteTickSize))                          // if our user hasn't defined this yet
                m_QuoteTickSize = m_InstrumentDetails.TickSize;         // set it to the default tick size here
            m_IsLegSetUpComplete = true;
            m_RiskManager.Start();
            m_ExecutionContainer.ConfirmStrategyLaunched();             // confirm we are "launched"
        }
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
            Log.NewEntry(LogLevel.Major, "SingleLegExcutor is shutting down, attemption to cancel all quoter orders");
            m_IsUserTradingEnabled = false; // stops new order from going out
            if (m_OrderBook != null)        // useful check when we shutdown before mkt is completely initialized.
                CancelAllOrders();          // just to be sure lets cancel all quote orders
        }
        #endregion//Event Handlers

        #region IStringifiable
        // *************************************************
        // ****             IStringifiable              ****
        // *************************************************
        string IStringifiable.GetAttributes()
        {
            StringBuilder s = new StringBuilder(base.GetAttributes());
            return s.ToString();
        }
        List<IStringifiable> IStringifiable.GetElements()
        {
            List<IStringifiable> elements = base.GetElements();
            if (elements == null)                                   // check this in case base class has elements in future.
                elements = new List<IStringifiable>();
            elements.Add(m_PriceLeg);
            return elements;
        }
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
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
                else if (attr.Key == "DripQty" && int.TryParse(attr.Value, out n))
                    this.m_DripQty = n;
                else if (attr.Key == "DefaultAccount")
                {
                    if (attr.Value.Length > 15) // 15 char limit!
                        this.m_DefaultAccount = attr.Value.Substring(0, 15);
                    else
                        this.m_DefaultAccount = attr.Value;
                }
                    
            }
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {
            if (subElement is PriceLeg)
                if (m_PriceLeg == null)
                    m_PriceLeg = ((PriceLeg)subElement);
                else
                    throw new Exception("Implemented Order Engine can only handle 1 leg, however more than 1 has been assigned");
        }
        #endregion// IStringifiable


    }//end class
}