using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;

namespace BRE.Models.Manuals
{
    using UV.Lib.Products;
    using UV.Lib.Hubs;
    using UV.Lib.Engines;
    using UV.Lib.FrontEnds.Graphs;
    using UV.Lib.MarketHubs;
    using UV.Lib.IO.Xml;
    using UV.Lib.Utilities;
    using UV.Lib.OrderBooks;
    using UV.Lib.DatabaseReaderWriters.Queries;
    using UV.Lib.Fills;

    using UV.Strategies.StrategyHubs;
    using UV.Strategies.StrategyEngines;


    /// <summary>
    /// This model serves as a pass through to allow users to "click" trade and still interact with the spreader 
    /// and underling UV System. It has some built in charting, and lots of properties to show intenals to 
    /// manual users.
    /// </summary>
    public class ManualSpreader : PricingEngine, IStringifiable, ITimerSubscriber
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // 
        //
        // external services
        //
        private IEngineHub m_IEngineHub;
        //
        // Execution Variables
        //
        private int m_DripQty;
        private int[] m_TotalQty = new int[2];          // total desired qty by side
        private int[] m_ExecutedQty = new int[2];       // total filled qty by side

        private double[] m_Price = new double[2];       // price by side.
        private bool m_IsTradingEnabled;                // state flag to stop trading.
        private bool m_IsIgnoreEconomicEvents;          // state flag to ignore event blocking.
        private double m_QuoteTickSize = .001;         // smallest quoting increment
        //
        // State and Internal Variables
        //
        private int m_CurrentPos = 0;
        private int m_GraphID = 0;
        private bool[] m_IsPriceInitialized = new bool[2];  // flag to show user has entered initial prices so we can start plotting.
        private double m_NHoursHistoricData = 5;        // deafult number of hours of data to display

        #endregion// members

        #region Constructors and Initialization
        // *****************************************************************
        // ****                Constructors and Intitialization         ****
        // *****************************************************************
        //
        //
        public ManualSpreader()
            : base()
        {
        }
        //       
        //
        //
        //
        protected override void SetupInitialize(IEngineHub myEngineHub, IEngineContainer engineContainer, int engineID, bool setupGui)
        {
            base.SetupInitialize(myEngineHub, engineContainer, engineID, setupGui);
            // Request historic data for plots.
            DateTime end = ParentStrategy.StrategyHub.GetLocalTime();
            DateTime start = end.AddHours(-m_NHoursHistoricData);            // TODO: allow this to passed in as istringifiable param?
            base.RequestHistoricData(start, end);
            m_IEngineHub = myEngineHub;
        }
        //
        public override void SetupBegin(UV.Lib.Engines.IEngineHub myEngineHub, UV.Lib.Engines.IEngineContainer engineContainer)
        {
            base.SetupBegin(myEngineHub, engineContainer);
            this.ParentStrategy = (Strategy)engineContainer;
            this.Log = ((Hub)myEngineHub).Log;                      // Pricing engines tend to log a lot.

            ParentStrategy.StrategyHub.SubscribeToTimer(ParentStrategy, this);
            SetupGraph();
        }
        //
        public override void SetupComplete()
        {
            base.SetupComplete();
            ParentStrategy.m_OrderEngine.QuoteTickSize = m_QuoteTickSize;
            if (!m_IOrderEngineRemote.TrySetParameter("DripQty", m_DripQty))
                Log.NewEntry(LogLevel.Error, "{0} failed to set DripQty in IOrderEngine", this.EngineName);
        }
        //
        //
        private void SetupGraph()
        {
            #region Initialize Graph
            if (m_GraphEngine != null)
            {
                string[] strArray = this.m_EngineName.Split(':');
                string graphName = strArray[strArray.Length - 1];
                //
                // Plot #1
                //
                // Can we ask graphEngine for a new graphId and give it the name etc, then?
                int graphID = 0;
                foreach (CurveDefinition c in m_GraphEngine.CurveDefinitions.CurveDefinitions)
                    if (graphID <= c.GraphID) { graphID = c.GraphID + 1; }	// use next available ID#.  

                m_GraphID = graphID;

                CurveDefinition cdef = new CurveDefinition();
                cdef.CurveName = "Price";
                cdef.GraphID = graphID;
                cdef.CurveColor = Color.Black;
                cdef.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
                cdef.Symbol = ZedGraph.SymbolType.None;
                m_GraphEngine.AddDefinition(cdef);
                cdef.GraphName = string.Format("{0} #{1}", graphName, this.m_EngineID.ToString());

                cdef = new CurveDefinition();
                cdef.CurveName = UV.Lib.Utilities.QTMath.MktSideToLongString(0);
                cdef.GraphID = graphID;
                cdef.CurveColor = Color.Blue;
                cdef.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
                cdef.Symbol = ZedGraph.SymbolType.None;
                m_GraphEngine.AddDefinition(cdef);

                cdef = new CurveDefinition();
                cdef.CurveName = UV.Lib.Utilities.QTMath.MktSideToLongString(1);
                cdef.GraphID = graphID;
                cdef.CurveColor = Color.Red;
                cdef.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
                cdef.Symbol = ZedGraph.SymbolType.None;
                m_GraphEngine.AddDefinition(cdef);

                //
                // Position indicators
                //
                cdef = new CurveDefinition();
                cdef.CurveName = "Long Entry";
                cdef.GraphID = graphID;
                cdef.CurveColor = Color.Blue;
                cdef.IsLineVisible = false;
                cdef.Symbol = ZedGraph.SymbolType.Triangle;
                cdef.SymbolFillColor = Color.Blue;
                m_GraphEngine.AddDefinition(cdef);

                cdef = new CurveDefinition();
                cdef.CurveName = "Long Exit";
                cdef.GraphID = graphID;
                cdef.CurveColor = Color.Blue;
                cdef.IsLineVisible = false;
                cdef.Symbol = ZedGraph.SymbolType.TriangleDown;
                cdef.SymbolFillColor = Color.White;
                m_GraphEngine.AddDefinition(cdef);

                cdef = new CurveDefinition();
                cdef.CurveName = "Short Entry";
                cdef.GraphID = graphID;
                cdef.CurveColor = Color.Red;
                cdef.IsLineVisible = false;
                cdef.Symbol = ZedGraph.SymbolType.TriangleDown;
                cdef.SymbolFillColor = Color.Red;
                m_GraphEngine.AddDefinition(cdef);

                cdef = new CurveDefinition();
                cdef.CurveName = "Short Exit";
                cdef.GraphID = graphID;
                cdef.CurveColor = Color.Red;
                cdef.IsLineVisible = false;
                cdef.Symbol = ZedGraph.SymbolType.Triangle;
                cdef.SymbolFillColor = Color.White;
                m_GraphEngine.AddDefinition(cdef);

            }
            #endregion // graph initialization.
        }// SetupGraph
        #endregion//Constructors

        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// Total desired buy side qty.
        /// </summary>
        public int TotalBuyQty
        {
            get { return m_TotalQty[Order.BuySide]; }
            set
            {
                m_TotalQty[Order.BuySide] = value;
                UpdateModelOrders(Order.BuySide);
            }
        }
        /// <summary>
        /// total desired sell side qty
        /// </summary>
        public int TotalSellQty
        {
            get { return m_TotalQty[Order.SellSide]; }
            set
            {
                m_TotalQty[Order.SellSide] = System.Math.Abs(value);
                UpdateModelOrders(Order.SellSide);
            }
        }
        //
        public double WorkPriceBuy
        {
            get { return m_Price[Order.BuySide]; }
            set
            {
                m_IsPriceInitialized[Order.BuySide] = true;
                m_Price[Order.BuySide] = value;
                UpdateModelOrders(Order.BuySide);
            }
        }
        public double WorkPriceSell
        {
            get { return m_Price[UV.Lib.Utilities.QTMath.AskSide]; }
            set
            {
                m_IsPriceInitialized[Order.SellSide] = true;
                m_Price[Order.SellSide] = value;
                UpdateModelOrders(Order.SellSide);
            }
        }
        /// <summary>
        /// Disclosed qty
        /// </summary>
        public int DripQty
        {
            get { return m_DripQty; }
            set
            {
                if (m_IOrderEngineRemote.TrySetParameter("DripQty", value))
                {
                    m_DripQty = value;
                }

            }
        }
        /// <summary>
        /// User state flag for allowing orders to be sent
        /// </summary>
        public bool IsTradingEnabled
        {
            get { return m_IsTradingEnabled; }
            set
            {
                m_IOrderEngineRemote.TrySetParameter("IsUserTradingEnabled", value);
                m_IsTradingEnabled = value;
                UpdateModelOrders();
            }
        }
        //
        public bool IgnoreEconomicEvent
        {
            get { return m_IsIgnoreEconomicEvents; }
            set 
            { 
                m_IsIgnoreEconomicEvents = value;
                UpdateModelOrders();
            }
        }
        //
        public bool EconomicEvent
        {
            get { return base.IsBlockedForEconomicEvent; }
        }
        //
        public int NetPos
        {
            get { return m_CurrentPos; }
        }
        //
        public int LongQtyFilled
        {
            get { return m_ExecutedQty[Order.BuySide]; }
        }
        //
        public int ShortQtyFilled
        {
            get { return m_ExecutedQty[Order.SellSide]; }
        }
        #endregion//Properties

        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        public override void MarketInstrumentInitialized(UV.Lib.BookHubs.Book marketBook)
        {
            base.MarketInstrumentInitialized(marketBook);
            if (m_IsTradingEnabled)
                if (!m_IOrderEngineRemote.TrySetParameter("IsUserTradingEnabled", true)) // once we are all set allow orders to be sent out.
                    Log.NewEntry(LogLevel.Error, "{0} failed to set flag for IsUserTradingEnabled in IOrderEngine, no order will go out");

            //
            // Plot historical values
            //
            List<DateTime> historicTimeStamp = null;
            List<double> historicStrategyMid = null;
            List<MarketDataItem[]> historicLegMarkets = null;
            if (base.TryGetTimeSeries(out historicTimeStamp, out historicStrategyMid, out historicLegMarkets))
                PlotHistoricData(m_GraphID, "Price", historicTimeStamp, historicStrategyMid);
        }
        // *************************************************************
        // ****                 Market Change()                     ****
        // *************************************************************
        /// <summary>
        /// Called whenever the market changes.
        /// </summary>
        /// <param name="marketBook"></param>
        /// <returns></returns>
        public override bool MarketInstrumentChanged(UV.Lib.BookHubs.Book marketBook, UV.Lib.BookHubs.InstrumentChangeArgs eventArgs)
        {
            base.MarketInstrumentChanged(marketBook, eventArgs);
            return false;
        }
        //
        //
        public void UpdateModelOrders(int mktSide)
        {
            if (IsTradingEnabled && (!IsBlockedForEconomicEvent || m_IsIgnoreEconomicEvents))
            { // we are enable to trade AND (there is no event blocking OR we don't care that there is)
                int desiredQty = m_TotalQty[mktSide] * UV.Lib.Utilities.QTMath.MktSideToMktSign(mktSide);
                int allowedQty = desiredQty - m_ExecutedQty[mktSide];
                ParentStrategy.Quote(this, mktSide, m_Price[mktSide], allowedQty);
            }
            else
                ParentStrategy.Quote(this,mktSide, m_Price[mktSide], 0);
        }
        //
        public void UpdateModelOrders()
        {
            for (int side = 0; side < 2; ++side)
                UpdateModelOrders(side);
        }
        //
        #endregion//Public Methods

        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        // *****************************************************
        // ****             PlotHistoricData()              ****
        // *****************************************************
        /// <summary>
        /// TODO: 
        ///     1) Can this also be placed inside the GraphEngine?
        /// </summary>
        /// <param name="graphID"></param>
        /// <param name="curveName"></param>
        /// <param name="timeSeriesDateTime"></param>
        /// <param name="timeSeriesData"></param>
        private void PlotHistoricData(int graphID, string curveName, List<DateTime> timeSeriesDateTime, List<double> timeSeriesData)
        {
            if (graphID < 0)
                return;

            //DateTime offSet = Log.GetTime();
            DateTime offSet = ParentStrategy.StrategyHub.GetLocalTime();
            if (offSet.TimeOfDay.TotalHours > 16)
                offSet = offSet.AddDays(1.0);				// after 4pm call this tomorrow.
            offSet = offSet.Subtract(offSet.TimeOfDay);

            // Update plot as we would in live system.
            //double graphUpdateCounter = 1;
            //int m_UpdateGraphCounterMax = 1;
            for (int t = 1; t < timeSeriesDateTime.Count; ++t)
            {
                // If the graphs are only update once every N seconds, we can duplicate this behavior
                // in the following few lines.
                //TimeSpan ts = m_TimeSeriesDateTime[t].Subtract(m_TimeSeriesDateTime[t - 1]);
                //graphUpdateCounter += ts.TotalSeconds;					// elapsed time since last update.
                //if (graphUpdateCounter >= m_UpdateGraphCounterMax)		// n times fewer points than normal
                //{
                //    graphUpdateCounter = 0;
                //double hour = m_TimeSeriesDateTime[t].TimeOfDay.TotalHours + HourOffset;
                double hour = timeSeriesDateTime[t].Subtract(offSet).TotalHours;
                if (t < timeSeriesData.Count)
                { //PJD added
                    m_GraphEngine.AddPoint(graphID, curveName, hour, timeSeriesData[t], true);
                }
                //}
            }//next time tick t
        }
        //
        //
        // *****************************************************
        // ****                 Filled()                    ****
        // *****************************************************
        /// <summary>
        /// Called by the strategy hub after an order engine has processed a fill
        /// and determined that the other engines would like to know about it
        /// </summary>
        /// <param name="fill"></param>
        //public override void Filled(Lib.Fills.Fill fill)
        public override void Filled(Fill fill)
        {

            int previousPos = m_CurrentPos;
            int fillSide = UV.Lib.Utilities.QTMath.MktSignToMktSide(fill.Qty);
            m_ExecutedQty[fillSide] += fill.Qty;    // increment our filled qty on this side.
            m_CurrentPos += fill.Qty;               // keep track of total pos...not sure why I am doing this seperately.

            #region Graph New Fill
            int dPos = m_CurrentPos - previousPos;
            if (m_GraphEngine != null && dPos != 0)
            {
                bool isLonger = dPos > 0;
                bool isExit = dPos * previousPos < 0;
                if (isLonger)
                {
                    if (isExit) m_GraphEngine.AddPoint(m_GraphID, "Short Exit", fill.Price);
                    else m_GraphEngine.AddPoint(m_GraphID, "Long Entry", fill.Price);
                    BroadcastParameter(m_IEngineHub, ParentStrategy, "LongQtyFilled");
                }
                else
                {
                    if (isExit) m_GraphEngine.AddPoint(m_GraphID, "Long Exit", fill.Price);
                    else m_GraphEngine.AddPoint(m_GraphID, "Short Entry", fill.Price);
                    BroadcastParameter(m_IEngineHub, ParentStrategy, "ShortQtyFilled");
                }
            }
            #endregion

            BroadcastParameter(m_IEngineHub, ParentStrategy, "NetPos");
        }//Filled()

        #endregion//Private Methods

        #region Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        //**************************************************
        //*****              Alarm Triggered            ****
        //**************************************************
        /// <summary>
        /// Called whenever our alarm is triggered on a specific time.  Be careful with threading! 
        /// As long as we are only setting bool flags here this should be threadsafe
        /// </summary>
        /// <param name="eventArgs"></param>
        public override void AlarmTriggered(EngineEventArgs engineEventArgs)
        {
            if (engineEventArgs.MsgType == EngineEventArgs.EventType.AlarmTriggered)
            {
                if (engineEventArgs.Status == EngineEventArgs.EventStatus.EconomicEventEnd)
                { // We are clear to trade 
                    Log.NewEntry(LogLevel.Major, "AlarmTriggered: Economic event alarm message received to resume trading");
                    UpdateModelOrders();
                    BroadcastAllParameters(m_IEngineHub, ParentStrategy);   // broadcast new state
                }
                else if (engineEventArgs.Status == EngineEventArgs.EventStatus.EconomicEventStart)
                { // We should stop trading for the number
                    Log.NewEntry(LogLevel.Major, "AlarmTriggered: Economic event alarm message received to stop trading");
                    foreach (object o in engineEventArgs.DataObjectList)
                    {
                        if (o.GetType() == typeof(EconomicDataItem))
                        {
                            EconomicDataItem economicData = (EconomicDataItem)o;
                            Log.NewEntry(LogLevel.Major, "EconomicEventName: {0} @ {1}", economicData.EventName, economicData.TimeStamp);
                        }
                    }
                    UpdateModelOrders();
                    BroadcastAllParameters(m_IEngineHub, ParentStrategy); // broadcast new state
                }
                else
                {
                    Log.NewEntry(LogLevel.Major, "AlarmTriggered: Uknown Event Economic Event Type Received.");
                }
            }
        }
        #endregion//Event Handlers

        #region ITimerSubscriber Implementation
        // *****************************************************
        // ****         TimerSubscriberUpdate()             ****
        // *****************************************************
        public void TimerSubscriberUpdate(UV.Lib.BookHubs.Book aBook)
        {
            double midPrice = 0.5 * (this.ImpliedMarket.Price[BidSide][0] + this.ImpliedMarket.Price[AskSide][0]);

            for (int tradeSide = 0; tradeSide < 2; tradeSide++)
            {
                if (m_IsPriceInitialized[tradeSide] && System.Math.Abs(m_TotalQty[tradeSide]) > System.Math.Abs(m_ExecutedQty[tradeSide]) && m_IsTradingEnabled)
                    m_GraphEngine.AddPoint(m_GraphID, UV.Lib.Utilities.QTMath.MktSideToLongString(tradeSide), m_Price[tradeSide]);
            }

            m_GraphEngine.AddPoint(m_GraphID, "Price", midPrice);
        }
        #endregion // ITimer Subscriber

        #region IStringifiable implentation
        // *****************************************************
        // ****                IStringifiable()             ****
        // *****************************************************
        public override void SetAttributes(Dictionary<string, string> attributes)
        {
            base.SetAttributes(attributes);
            double x;
            foreach (KeyValuePair<string, string> att in attributes)
            {
                if (att.Key.Equals("QuoteTickSize") && double.TryParse(att.Value, out x))
                    m_QuoteTickSize = x;
                else if (att.Key.Equals("HistoricHours") && double.TryParse(att.Value, out x))
                    m_NHoursHistoricData = x;
            }
        }
        #endregion // IString
    }
}
