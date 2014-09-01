using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Timers;
using System.Threading.Tasks;

namespace VioletAPI.KeltnerSpreader
{

    #region Usings
    using UV.Lib.Products;
    using UV.Lib.Hubs;
    using UV.Lib.BookHubs;
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

    using Lib;
    using Lib.PopUp;
    using Lib.Plotting;
    using Lib.TradingHelper;
    using Lib.StrategyRiskControl;
    #endregion


    class KeltnerTrader : PricingEngine, IStringifiable, ITimerSubscriber
    {

        #region Members
        // *****************************************************************
        // ****                        Members                          ****
        // *****************************************************************
        private System.Windows.Forms.Form m_MainForm = null;                                                    // Keep a pointer to the main form for closing implementation.
        private IEngineHub m_IEngineHub;                                                                        // Engine hub to set trading variables.
        private double m_QuoteTickSize = .001;                                                                  // Minimum quote tick size for the spread.
        private double m_RejectDataPercentThreshold = 0.9;                                                      // Threshold to reject bad data in the database.
        private double m_NHoursHistoricData = 24;                                                               // Historical database data request length.
        private bool m_IsTradingEnabled = false;                                                                // Trading flag enable button.
        private bool m_IsIgnoreEconomicEvents;                                                                  // Flag for user to ignore economic event.
        private bool m_InitialPositionValidated;                                                                // Flag of initial position validated.
        private bool m_IsFirstBehaviorCheck = false;                                                            // Flag of initial trading behavior check logic when the program launched.
        private bool m_IsFirstBehaviorCheckConfirmed = false;                                                   // Flag of initial trading behavior check confirmed.
        private bool m_IsProcessingFills = false;                                                               // Flag of processing fills.
        private object m_ProcessingFillLockObject = new object();                                               // Lock for fills.
        private bool[] m_IsNeededUpdate;                                                                        // Flag of needed update.
        private double[] m_MarketData = new double[3];                                                          // Market data global variables.
        private bool[] m_WorkingFlags = new bool[2];                                                            // Working spread buy/sell flags.
        private double[] m_WorkingPrices = new double[2];                                                       // Working spread buy/sell prices.
        private int[] m_WorkingTotalQtys = new int[2];                                                          // Working spread total quantities.
        private int m_DripQty;                                                                                  // Drip quantity specified by the user.
        private bool m_IsSettingOrders = false;                                                                 // Flag of setting orders.
        private object m_SetOrdersLockObject = new object();                                                    // Lock for setting orders.
        private int m_CurrentPosition = 0;                                                                      // Current spread position.
        private int[] m_ExecutedQty = new int[2];                                                               // Current executed quantities.
        private int m_HistoricalDataPlottingInterval = 60;                                                      // Make it quicker to plot in less interval.
        private double m_HistoricalLookingBackHour = 2;                                                         // Historical plotting needed.
        private double m_LastValidatedDataFeed = double.NaN;                                                    // Last validated data.
        private int m_GraphID = 0;                                                                              // Current graph ID.
        private DateTime m_NextPlotDateTime = DateTime.MinValue;                                                // Next update datetime.
        private List<string> m_EconomicBlockingEventList = new List<string>();                                  // Economic event storing list.
        private KeltnerIndicatorManager m_KeltnerIndicatorManager = null;                                       // Keltner trading indicator manager.
        private KeltnerTraderLogicManager m_KeltnerTraderLogicManager = null;                                   // Keltner trading logic manager.
        private KeltnerStrategyRiskManager m_KeltnerStrategyRiskManager = null;                                 // Keltner trading risk manager.
        private PopUpTool m_PopUpTool = null;                                                                   // Pop up tool control.
        private KeltnerTradingVariables m_KeltnerTradingVariables = null;                                       // Keltner trading variables.
        private bool m_IsMarketActive = false;                                                                  // Spread market active flag.
        private List<DateTime> m_MarketOpen = null;                                                             // Exchange open times.
        private List<DateTime> m_MarketClose = null;                                                            // Exchange close times.
        private bool m_IsSaved = false;                                                                         // Save button.
        private static bool m_SavedExitFlag = false;                                                            // Save exit flag unique for all.
        #endregion


        #region Constructors and Initialization
        // *****************************************************************
        // ****            Constructors and Intitialization             ****
        // *****************************************************************
        public KeltnerTrader()
            : base()
        {
            m_MainForm = Global.TradingStrategyMainForm;                                                        // Keep a pointer to main form to finish savings when user exits the program.
            m_MainForm.FormClosing += new System.Windows.Forms.FormClosingEventHandler(MainForm_Closing);       // Subscribe to the form closing event handler.

            m_IsNeededUpdate = new bool[2];                                                                     // This is the needed update quoting flags.
            if (Global.TraderLog == null)
                Global.TraderLog = new LogHub("Trade Log", UV.Lib.Application.AppInfo.GetInstance().LogPath, true, LogLevel.Major);
        }
        //
        //
        /// <summary>
        /// Save trading variables when the form is closing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_Closing(object sender, System.Windows.Forms.FormClosingEventArgs e)
        {
            m_MainForm.FormClosing -= new System.Windows.Forms.FormClosingEventHandler(MainForm_Closing);       // Only subscribe to form closing once.
            Log.NewEntry(LogLevel.Minor, "The main form is closing, so shut down all the services and save trading variables for {0}.", this.ParentStrategy.Name);
            m_KeltnerTraderLogicManager.StopOrderTriggered -= new EventHandler(TradingLogicManagerBase_StopOrderTriggered);
            if (m_KeltnerTraderLogicManager.Log != null)
                m_KeltnerTraderLogicManager.Log.RequestStop();                                                  // Have to shut down the log hub thread to clear all working threads.
            m_KeltnerStrategyRiskManager.Dispose();                                                             // Close all timers for strategy risk manager.

            if (Global.TraderLog != null)
            {
                Global.TraderLog.RequestStop();
                Global.TraderLog = null;
            }

            // Save all the trading variables when the application exits.
            string keltnerTradingLogicManagerFilePath = string.Format("{0}{1}_Variables.txt", UV.Lib.Application.AppInfo.GetInstance().UserConfigPath, this.ParentStrategy.Name);
            using (System.IO.StreamWriter streamWriter = new System.IO.StreamWriter(keltnerTradingLogicManagerFilePath, false))
            {
                streamWriter.WriteLine(Stringifiable.Stringify(m_KeltnerTraderLogicManager));
                streamWriter.Close();
            }

            if (!m_SavedExitFlag)
            {
                string strategyConfigFilePath = string.Format("{0}KeltnerTraderConfig.txt", UV.Lib.Application.AppInfo.GetInstance().UserConfigPath);
                StringBuilder stringBuilder = new StringBuilder();
                foreach (Strategy strategy in Global.Strategies)
                {
                    stringBuilder.AppendLine(Stringifiable.Stringify(strategy));
                }
                using (System.IO.StreamWriter streamWriter = new System.IO.StreamWriter(strategyConfigFilePath, false))
                {
                    streamWriter.WriteLine(stringBuilder.ToString());
                    streamWriter.Close();
                }
                m_SavedExitFlag = true;
            }
        }
        //
        //
        protected override void SetupInitialize(IEngineHub myEngineHub, IEngineContainer engineContainer, int engineID, bool setupGui)
        {
            base.SetupInitialize(myEngineHub, engineContainer, engineID, setupGui);
            DateTime end = ParentStrategy.StrategyHub.GetLocalTime();
            DateTime start = end.AddHours(-m_NHoursHistoricData);
            Log.NewEntry(LogLevel.Minor, "Request historical data in database from {0} to {1}, which has {2} hours.", start, end, m_NHoursHistoricData);
            base.RequestHistoricData(start, end);
            m_IEngineHub = myEngineHub;
        }
        //
        //
        public override void SetupBegin(IEngineHub myEngineHub, IEngineContainer engineContainer)
        {
            base.SetupBegin(myEngineHub, engineContainer);
            this.ParentStrategy = (Strategy)engineContainer;
            this.Log = ((Hub)myEngineHub).Log;
            ParentStrategy.StrategyHub.SubscribeToTimer(ParentStrategy, this);
            SetupGraph();
        }
        //
        //
        public override void SetupComplete()
        {
            base.SetupComplete();
            ParentStrategy.m_OrderEngine.QuoteTickSize = m_QuoteTickSize;
            Log.NewEntry(LogLevel.Minor, "The quote tick size is {0}.", ParentStrategy.m_OrderEngine.QuoteTickSize);
        }
        //
        //
        /// <summary>
        /// Setup graph using plotting tool.
        /// </summary>
        private void SetupGraph()
        {
            if (m_GraphEngine != null)
            {
                string[] strArray = this.m_EngineName.Split(':');
                string graphName = strArray[strArray.Length - 1];
                graphName = string.Format("{0} #{1}", graphName, this.m_EngineID.ToString());
                int graphID = 0;
                foreach (CurveDefinition c in m_GraphEngine.CurveDefinitions.CurveDefinitions)
                    if (graphID <= c.GraphID) { graphID = c.GraphID + 1; }
                m_GraphID = graphID;
                PlottingTool.AddCurveToGraphEngine(m_GraphEngine, graphName, "Price", m_GraphID, Color.Black, System.Drawing.Drawing2D.DashStyle.Solid, ZedGraph.SymbolType.None);
                PlottingTool.AddCurveToGraphEngine(m_GraphEngine, graphName, "EMA", m_GraphID, Color.Gold, System.Drawing.Drawing2D.DashStyle.Solid, ZedGraph.SymbolType.None);
                PlottingTool.AddCurveToGraphEngine(m_GraphEngine, graphName, "Long Enter", m_GraphID, Color.Green, System.Drawing.Drawing2D.DashStyle.Solid, ZedGraph.SymbolType.None);
                PlottingTool.AddCurveToGraphEngine(m_GraphEngine, graphName, "Long Fade", m_GraphID, Color.Purple, System.Drawing.Drawing2D.DashStyle.Solid, ZedGraph.SymbolType.None);
                PlottingTool.AddCurveToGraphEngine(m_GraphEngine, graphName, "Long Puke", m_GraphID, Color.DarkRed, System.Drawing.Drawing2D.DashStyle.Solid, ZedGraph.SymbolType.None);
                PlottingTool.AddCurveToGraphEngine(m_GraphEngine, graphName, "Short Enter", m_GraphID, Color.Green, System.Drawing.Drawing2D.DashStyle.Solid, ZedGraph.SymbolType.None);
                PlottingTool.AddCurveToGraphEngine(m_GraphEngine, graphName, "Short Fade", m_GraphID, Color.Purple, System.Drawing.Drawing2D.DashStyle.Solid, ZedGraph.SymbolType.None);
                PlottingTool.AddCurveToGraphEngine(m_GraphEngine, graphName, "Short Puke", m_GraphID, Color.DarkRed, System.Drawing.Drawing2D.DashStyle.Solid, ZedGraph.SymbolType.None);
                PlottingTool.AddCurveToGraphEngine(m_GraphEngine, graphName, "Long Entry", m_GraphID, Color.Blue, false, ZedGraph.SymbolType.Triangle, Color.Blue);
                PlottingTool.AddCurveToGraphEngine(m_GraphEngine, graphName, "Short Entry", m_GraphID, Color.Red, false, ZedGraph.SymbolType.TriangleDown, Color.Red);
                PlottingTool.AddCurveToGraphEngine(m_GraphEngine, graphName, "Long Exit", m_GraphID, Color.Blue, false, ZedGraph.SymbolType.TriangleDown, Color.White);
                PlottingTool.AddCurveToGraphEngine(m_GraphEngine, graphName, "Short Exit", m_GraphID, Color.Red, false, ZedGraph.SymbolType.Triangle, Color.White);
            }
        }
        #endregion


        #region Properties
        // *****************************************************************
        // ****                       Properties                        ****
        // *****************************************************************
        public bool IsTradingEnabled
        {
            get { return m_IsTradingEnabled; }
            set
            {
                if (value == false && !m_IOrderEngineRemote.TrySetParameter("IsUserTradingEnabled", value))                                                    // once we are all set allow orders to be sent out.
                    Log.NewEntry(LogLevel.Error, "{0} failed to set flag for IsUserTradingEnabled in IOrderEngine to {1}", this.ParentStrategy.Name, value);
                m_IsNeededUpdate[OrderSide.BuySide] = true;
                m_IsNeededUpdate[OrderSide.SellSide] = true;
                m_IsTradingEnabled = value;
                BroadcastParameter(m_IEngineHub, ParentStrategy, "IsTradingEnabled");
            }
        }
        public bool IgnoreEconomicEvent
        {
            get { return m_IsIgnoreEconomicEvents; }
            set
            {
                if (m_IsIgnoreEconomicEvents == false && value == true)
                {
                    m_IsNeededUpdate[OrderSide.BuySide] = true;
                    m_IsNeededUpdate[OrderSide.SellSide] = true;
                }
                m_IsIgnoreEconomicEvents = value;
                BroadcastParameter(m_IEngineHub, ParentStrategy, "IgnoreEconomicEvent");
            }
        }
        public new bool IsBlockedForEconomicEvent
        {
            get { return base.IsBlockedForEconomicEvent; }
        }
        public bool LongEntryAllowed
        {
            get { return m_KeltnerTraderLogicManager.GetEntryAllowFlag(TradeSide.Buy); }
            set
            {
                Log.NewEntry(LogLevel.Minor, "**The long entry allowed is set by user to {0}.**", value);
                m_KeltnerTraderLogicManager.SetEntryAllowFlag(TradeSide.Buy, value);
                m_IsNeededUpdate[OrderSide.BuySide] = true;
                BroadcastParameter(m_IEngineHub, ParentStrategy, "LongEntryAllowed");
            }
        }
        public double WorkingBuyPrice
        {
            get { return m_WorkingPrices[OrderSide.BuySide]; }
            set
            {
                if (value != m_WorkingPrices[OrderSide.BuySide])
                {
                    m_WorkingPrices[OrderSide.BuySide] = value;
                    BroadcastParameter(m_IEngineHub, ParentStrategy, "WorkingBuyPrice");
                }
            }
        }
        public int TotalWorkingBuyQty
        {
            get { return m_WorkingTotalQtys[OrderSide.BuySide]; }
            set
            {
                if (value != m_WorkingTotalQtys[OrderSide.BuySide])
                {
                    m_WorkingTotalQtys[OrderSide.BuySide] = value;
                    BroadcastParameter(m_IEngineHub, ParentStrategy, "TotalWorkingBuyQty");
                }
            }
        }
        public bool ShortEntryAllowed
        {
            get { return m_KeltnerTraderLogicManager.GetEntryAllowFlag(TradeSide.Sell); }
            set
            {
                Log.NewEntry(LogLevel.Minor, "**The short entry allowed is set by user to {0}.**", value);
                m_KeltnerTraderLogicManager.SetEntryAllowFlag(TradeSide.Sell, value);
                m_IsNeededUpdate[OrderSide.SellSide] = true;
                BroadcastParameter(m_IEngineHub, ParentStrategy, "ShortEntryAllowed");
            }
        }
        public double WorkingSellPrice
        {
            get { return m_WorkingPrices[UV.Lib.Utilities.QTMath.AskSide]; }
            set
            {
                if (value != m_WorkingPrices[UV.Lib.Utilities.QTMath.AskSide])
                {
                    m_WorkingPrices[OrderSide.SellSide] = value;
                    BroadcastParameter(m_IEngineHub, ParentStrategy, "WorkingSellPrice");
                }
            }
        }
        public int TotalWorkingSellQty
        {
            get { return m_WorkingTotalQtys[OrderSide.SellSide]; }
            set
            {
                if (value != m_WorkingTotalQtys[OrderSide.SellSide])
                {
                    m_WorkingTotalQtys[OrderSide.SellSide] = value;
                    BroadcastParameter(m_IEngineHub, ParentStrategy, "TotalWorkingSellQty");
                }
            }
        }
        public int EntryQty
        {
            get { return m_KeltnerTraderLogicManager.EntryQty; }
            set
            {
                Log.NewEntry(LogLevel.Minor, "The entry quantity is set by user to {0}.", value);
                m_KeltnerStrategyRiskManager.MaxNetPosition = value + m_KeltnerTraderLogicManager.FadeQty;
                m_KeltnerTradingVariables.MaxNetPosition = m_KeltnerStrategyRiskManager.MaxNetPosition;
                m_KeltnerStrategyRiskManager.MaxTotalFills += 6 * (value - m_KeltnerTraderLogicManager.EntryQty);
                m_KeltnerTradingVariables.MaxTotalFills = m_KeltnerStrategyRiskManager.MaxTotalFills;
                m_KeltnerTraderLogicManager.EntryQty = value;
                m_KeltnerTradingVariables.EntryQty = value;
                BroadcastParameter(m_IEngineHub, ParentStrategy, "EntryQty");
                m_IsNeededUpdate[OrderSide.BuySide] = true;
                m_IsNeededUpdate[OrderSide.SellSide] = true;
                m_PopUpTool.TryRecordTradingVariablesChange("EntryQty", value.ToString());
            }
        }
        public int FadeQty
        {
            get { return m_KeltnerTraderLogicManager.FadeQty; }
            set
            {
                Log.NewEntry(LogLevel.Minor, "The fade quantity is set by user to {0}.", value);
                m_KeltnerStrategyRiskManager.MaxNetPosition = value + m_KeltnerTraderLogicManager.EntryQty;
                m_KeltnerTradingVariables.MaxNetPosition = m_KeltnerStrategyRiskManager.MaxNetPosition;
                m_KeltnerStrategyRiskManager.MaxTotalFills += 6 * (value - m_KeltnerTraderLogicManager.FadeQty);
                m_KeltnerTradingVariables.MaxTotalFills = m_KeltnerStrategyRiskManager.MaxTotalFills;
                m_KeltnerTraderLogicManager.FadeQty = value;
                m_KeltnerTradingVariables.FadeQty = value;
                BroadcastParameter(m_IEngineHub, ParentStrategy, "FadeQty");
                m_IsNeededUpdate[OrderSide.BuySide] = true;
                m_IsNeededUpdate[OrderSide.SellSide] = true;
                m_PopUpTool.TryRecordTradingVariablesChange("FadeQty", value.ToString());
            }
        }
        public int DripQty
        {
            get { return m_DripQty; }
            set
            {
                m_DripQty = value;
                m_KeltnerTradingVariables.DripQty = m_DripQty;
                BroadcastParameter(m_IEngineHub, ParentStrategy, "DripQty");
                m_IsNeededUpdate[OrderSide.BuySide] = true;
                m_IsNeededUpdate[OrderSide.SellSide] = true;
                m_IOrderEngineRemote.TrySetParameter("DripQty", m_DripQty);
            }
        }
        public int EMALength
        {
            get { return m_KeltnerTradingVariables.EMALength; }
            set
            {
                m_KeltnerTradingVariables.EMALength = value;
                m_PopUpTool.TryRecordTradingVariablesChange("EMALength", value.ToString());
            }
        }
        public int ATRLength
        {
            get { return m_KeltnerTradingVariables.ATRLength; }
            set
            {
                m_KeltnerTradingVariables.ATRLength = value;
                m_PopUpTool.TryRecordTradingVariablesChange("ATRLength", value.ToString());
            }
        }
        public int MomentumLength
        {
            get { return m_KeltnerTradingVariables.MomentumLength; }
            set
            {
                m_KeltnerTradingVariables.MomentumLength = value;
                m_PopUpTool.TryRecordTradingVariablesChange("MomentumLength", value.ToString());
            }
        }
        public double EntryWidth
        {
            get { return m_KeltnerTradingVariables.EntryWidth; }
            set
            {
                m_KeltnerTradingVariables.EntryWidth = value;
                m_PopUpTool.TryRecordTradingVariablesChange("EntryWidth", value.ToString());
            }
        }
        public double FadeWidth
        {
            get { return m_KeltnerTradingVariables.FadeWidth; }
            set
            {
                m_KeltnerTradingVariables.FadeWidth = value;
                m_PopUpTool.TryRecordTradingVariablesChange("FadeWidth", value.ToString());
            }
        }
        public double PukeWidth
        {
            get { return m_KeltnerTradingVariables.PukeWidth; }
            set
            {
                m_KeltnerTradingVariables.PukeWidth = value;
                m_PopUpTool.TryRecordTradingVariablesChange("PukeWidth", value.ToString());
            }
        }
        public int BarIntervalInSeconds
        {
            get { return m_KeltnerTradingVariables.BarIntervalInSeconds; }
            set
            {
                m_KeltnerTradingVariables.BarIntervalInSeconds = value;
                m_PopUpTool.TryRecordTradingVariablesChange("BarIntervalInSeconds", value.ToString());
            }
        }
        public double MomentumEntryThreshold
        {
            get { return m_KeltnerTradingVariables.MomentumEntryValue; }
            set
            {
                m_KeltnerTradingVariables.MomentumEntryValue = value;
                m_KeltnerTraderLogicManager.MomentumEntry = value;
                m_PopUpTool.TryRecordTradingVariablesChange("MomentumEntryValue", value.ToString());
            }
        }
        public double MomentumPukeThreshold
        {
            get { return m_KeltnerTradingVariables.MomentumPukeValue; }
            set
            {
                m_KeltnerTradingVariables.MomentumPukeValue = value;
                m_PopUpTool.TryRecordTradingVariablesChange("MomentumPukeValue", value.ToString());
            }
        }
        public double LastEMA
        {
            get { return m_KeltnerIndicatorManager.LastEMA; }
        }
        public double LastATR
        {
            get { return m_KeltnerIndicatorManager.LastATR; }
        }
        public double LastMomentum
        {
            get { return m_KeltnerIndicatorManager.LastMomentum; }
        }
        public double BidPrice
        {
            get { return m_KeltnerTraderLogicManager.BidPrice; }
        }
        public double AskPrice
        {
            get { return m_KeltnerTraderLogicManager.AskPrice; }
        }
        public double MidPrice
        {
            get { return m_KeltnerTraderLogicManager.MidPrice; }
        }
        public double LongEnterBand
        {
            get { return m_KeltnerTraderLogicManager.LongEntryBand; }
        }
        public double LongFadeBand
        {
            get { return m_KeltnerTraderLogicManager.LongFadeBand; }
        }
        public double LongPukeBand
        {
            get { return m_KeltnerTraderLogicManager.LongPukeBand; }
        }
        public double ShortEnterBand
        {
            get { return m_KeltnerTraderLogicManager.ShortEntryBand; }
        }
        public double ShortFadeBand
        {
            get { return m_KeltnerTraderLogicManager.ShortFadeBand; }
        }
        public double ShortPukeBand
        {
            get { return m_KeltnerTraderLogicManager.ShortPukeBand; }
        }
        public double EconomicPaddingHour
        {
            get { return (double)(EventPaddingSeconds) / 3600; }
            set
            {
                EventPaddingSeconds = (int)(value * 3600);
                m_KeltnerTradingVariables.EconomicEventBlockingHour = value;
                m_PopUpTool.TryRecordTradingVariablesChange("EconomicPaddingHour", value.ToString());
            }
        }
        public int MaxNetSpreadPosition
        {
            get { return m_KeltnerStrategyRiskManager.MaxNetPosition; }
            set
            {
                m_KeltnerStrategyRiskManager.MaxNetPosition = value;
                m_KeltnerTradingVariables.MaxNetPosition = value;
                m_PopUpTool.TryRecordTradingVariablesChange("MaxNetSpreadPosition", value.ToString());
            }
        }
        public int MaxTotalSpreadFills
        {
            get { return m_KeltnerStrategyRiskManager.MaxTotalFills; }
            set
            {
                m_KeltnerStrategyRiskManager.MaxTotalFills = value;
                m_KeltnerTradingVariables.MaxTotalFills = value;
                m_PopUpTool.TryRecordTradingVariablesChange("MaxTotalSpreadFills", value.ToString());
            }
        }
        public int LongQtyFilled
        {
            get { return m_ExecutedQty[OrderSide.BuySide]; }
        }
        public int ShortQtyFilled
        {
            get { return m_ExecutedQty[OrderSide.SellSide]; }
        }
        public int NetPosition
        {
            get { return m_CurrentPosition; }
        }
        public bool SaveAlls
        {
            get { return m_IsSaved; }
            set
            {
                m_IsSaved = value;

                // Save strategies to config file.
                //bool isFoundStrategy = false;
                //Strategy strategy = null;
                //using (StringifiableReader streamReader = new StringifiableReader(strategyConfigFilePath))
                //{
                //    List<IStringifiable> iStringfiableObjs = streamReader.ReadToEnd(false);
                //    foreach (IStringifiable obj in iStringfiableObjs)
                //    {
                //        if (obj is Strategy)
                //        {
                //            strategy = (Strategy)obj;
                //            if (strategy.Name.Equals(this.ParentStrategy.Name))
                //            {

                //                isFoundStrategy = true;
                //                break;
                //            }
                //        }
                //    }
                //    streamReader.Close();
                //}

                //if (isFoundStrategy)
                //{
                //    List<IStringifiable> executionRemotes = ((IStringifiable)strategy).GetElements();
                //    foreach (IStringifiable engine in executionRemotes)
                //    {
                //        if (engine is ExecutionRemote)
                //        {
                //            ExecutionRemote e = (ExecutionRemote)engine;

                //        }
                //    }
                //}

                // Save all the trading variables when the application exits.
                string keltnerTradingLogicManagerFilePath = string.Format("{0}{1}_Variables.txt", UV.Lib.Application.AppInfo.GetInstance().UserConfigPath, this.ParentStrategy.Name);
                using (System.IO.StreamWriter streamWriter = new System.IO.StreamWriter(keltnerTradingLogicManagerFilePath, false))
                {
                    streamWriter.WriteLine(Stringifiable.Stringify(m_KeltnerTraderLogicManager));
                    streamWriter.Close();
                }

                string strategyConfigFilePath = string.Format("{0}KeltnerTraderConfig.txt", UV.Lib.Application.AppInfo.GetInstance().UserConfigPath);
                StringBuilder stringBuilder = new StringBuilder();
                foreach (Strategy strategy in Global.Strategies)
                {
                    stringBuilder.AppendLine(Stringifiable.Stringify(strategy));
                }
                using (System.IO.StreamWriter streamWriter = new System.IO.StreamWriter(strategyConfigFilePath, false))
                {
                    streamWriter.WriteLine(stringBuilder.ToString());
                    streamWriter.Close();
                }
            }
        }
        #endregion


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        /// <summary>
        /// The preparation for trading:
        /// 1.Load trading logic manager and trading variables.
        /// 2.Initialize strategy risk manager.
        /// 3.Load historical market data and plot them. 
        /// 4.Calculate the indicators by historical data and plot them.
        /// 5.Start trading pop up tool and request initial position validation.
        /// </summary>
        /// <param name="marketBook"></param>
        public override void MarketInstrumentInitialized(Book marketBook)
        {
            // Setup alarm and load implied market.
            Global.Strategies.Add(this.ParentStrategy);
            base.MarketInstrumentInitialized(marketBook);
            m_PopUpTool = new PopUpTool();
            m_PopUpTool.DialogUserComplete += new EventHandler(PopUpTool_DialogUserComplete);

            // Trading logic path is constructed by spread name and variables suffix. Load all trading variables from this path.
            string keltnerTradingLogicManagerFilePath = string.Format("{0}{1}_Variables.txt", UV.Lib.Application.AppInfo.GetInstance().UserConfigPath, this.ParentStrategy.Name);
            Log.NewEntry(LogLevel.Minor, "Prepare to load trading variables from file:{0}.", keltnerTradingLogicManagerFilePath);

            if (System.IO.File.Exists(keltnerTradingLogicManagerFilePath))
            {
                // If the path exists for the keltner trading logic manager, load it.
                List<IStringifiable> iStringObjects = null;
                using (StringifiableReader stringifiableReader = new StringifiableReader(keltnerTradingLogicManagerFilePath))
                {
                    iStringObjects = stringifiableReader.ReadToEnd();
                    Log.NewEntry(LogLevel.Minor, "Load trading variables from file {0} successfully for {1}.", keltnerTradingLogicManagerFilePath, this.ParentStrategy.Name);
                }

                // Only one node actually will be contained in the trading variables file.
                foreach (IStringifiable iStringObject in iStringObjects)
                {
                    if (iStringObject.GetType() == typeof(KeltnerTraderLogicManager))
                    {
                        m_KeltnerTraderLogicManager = (KeltnerTraderLogicManager)iStringObject;                                     // Loaded trading logic manager. 
                        m_KeltnerTradingVariables = m_KeltnerTraderLogicManager.KeltnerTradingVariables;                            // Loaded trading variables.
                        m_CurrentPosition = m_KeltnerTradingVariables.CurrentPos;                                                   // Loaded initial spread position for validation.
                    }
                }
            }
            else
            {
                // If there is no file exists for the path, create one using default parameters input.
                Log.NewEntry(LogLevel.Minor, "No file at {0} exists, create default trading variables for {1}.", keltnerTradingLogicManagerFilePath, this.ParentStrategy.Name);
                m_KeltnerTradingVariables = new KeltnerTradingVariables();                                                          // Created trading variables.                          
                m_KeltnerTraderLogicManager = new KeltnerTraderLogicManager(this.ParentStrategy.Name, m_KeltnerTradingVariables);   // Created trading logic manager.

                // Create trading variables file.
                using (System.IO.StreamWriter streamWriter = new System.IO.StreamWriter(keltnerTradingLogicManagerFilePath, false))
                {
                    streamWriter.WriteLine(Stringifiable.Stringify(m_KeltnerTraderLogicManager));
                    streamWriter.Close();
                }
            }

            // Create strategy risk manager and subscribe to the event of stop order triggered.
            m_KeltnerStrategyRiskManager = new KeltnerStrategyRiskManager(this.ParentStrategy.Name, m_KeltnerTradingVariables);
            m_KeltnerStrategyRiskManager.NetPosition = m_CurrentPosition;
            m_KeltnerTraderLogicManager.StopOrderTriggered += new EventHandler(TradingLogicManagerBase_StopOrderTriggered);

            // Load drip quantity, economic event padding seconds and exchange open close ranges.
            DripQty = m_KeltnerTradingVariables.DripQty;
            EventPaddingSeconds = (int)(m_KeltnerTradingVariables.EconomicEventBlockingHour * 3600);
            LoadMarketOpenCloseTime();

            // Complete more functionality of historical data plotting.
            List<DateTime> historicTimeStamp = null;
            List<double> historicStrategyMid = null;
            List<MarketDataItem[]> historicLegMarkets = null;
            if (base.TryGetTimeSeries(out historicTimeStamp, out historicStrategyMid, out historicLegMarkets))
                Log.NewEntry(LogLevel.Minor, "Successfully get historical data from database, and there is {0} rows data.", historicTimeStamp.Count);
            else
                Log.NewEntry(LogLevel.Major, "Failed to get historical data from database.");

            // Calculate the values for indicators using historical data and plot them on the graph.
            SetHistoricalMarketOpenClose(historicTimeStamp[0]);
            m_KeltnerIndicatorManager = new KeltnerIndicatorManager(historicTimeStamp[0], m_KeltnerTradingVariables, m_MarketOpen, m_MarketClose);
            Log.NewEntry(LogLevel.Minor, "The earliest time stamp in the database is {0}.", historicTimeStamp[0].ToString("yyyy/MM/dd HH:mm:ss"));
            DateTime localDateTime = DateTime.Now.AddHours(-m_HistoricalLookingBackHour);
            SortedList<DateTime, double> historicalEMA = new SortedList<DateTime, double>();
            SortedList<DateTime, double> historicalATR = new SortedList<DateTime, double>();
            SortedList<DateTime, double> historicalMid = new SortedList<DateTime, double>();
            double lastEMA = double.NaN;
            double lastATR = double.NaN;
            double lastMOM = double.NaN;
            for (int ptr = 0; ptr < historicTimeStamp.Count; ++ptr)
            {
                UpdateMarketActiveStatus(historicTimeStamp[ptr]);
                if (m_IsMarketActive)
                {
                    if (IsDataGood(historicStrategyMid[ptr]))
                    {
                        m_KeltnerIndicatorManager.FeedDataToAllIndicators(historicTimeStamp[ptr], historicStrategyMid[ptr]);
                        if (m_KeltnerIndicatorManager.TryGetIndicatorValues(out lastEMA, out lastATR, out lastMOM) && historicTimeStamp[ptr] >= localDateTime)
                        {
                            historicalMid.Add(historicTimeStamp[ptr], historicStrategyMid[ptr]);
                            historicalEMA.Add(historicTimeStamp[ptr], lastEMA);
                            historicalATR.Add(historicTimeStamp[ptr], lastATR);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Bad data: {0} -> {1}", historicTimeStamp[ptr], historicStrategyMid[ptr]);
                    }
                }
            }
            PlotHistoricData(m_GraphID, "Price", new List<DateTime>(historicalMid.Keys), new List<double>(historicalMid.Values));
            PlotHistoricalEMAATRBands(historicalEMA, historicalATR);

            // Request the user to validate the initial positions before trading.
            Global.TraderLog.NewEntry(LogLevel.Major, "Initialize trading strategy {0}.", this.ParentStrategy.Name);
            LoadMarketOpenCloseTime();
            InitializeMarketState();
            Log.NewEntry(LogLevel.Minor, "Request user to validate the initial position.");
            IsTradingEnabled = false;
            DialogType dialogType = DialogType.PositionValidationYesNo;
            string topic = "Initial Position Validation";
            string text = string.Format("The current position is {0} for {1}, correct?", m_CurrentPosition, this.ParentStrategy.Name);
            m_PopUpTool.ShowDialog(dialogType, DialogIDMap.InitialPositionYESNO, topic, text);
            BroadcastAllParameters(m_IEngineHub, ParentStrategy);
        }
        //
        //
        /// <summary>
        /// Called when market microstructure variables changed.
        /// </summary>
        /// <param name="marketBook"></param>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        public override bool MarketInstrumentChanged(UV.Lib.BookHubs.Book marketBook, UV.Lib.BookHubs.InstrumentChangeArgs eventArgs)
        {
            base.MarketInstrumentChanged(marketBook, eventArgs);
            UpdateSpreadMarketPrices();
            return true;
        }
        //
        //
        /// <summary>
        /// Update working orders and do logs.
        /// </summary>
        /// <param name="mktSide"></param>
        public void UpdateModelOrders(int mktSide)
        {
            if (IsTradingEnabled)
            {
                if (m_WorkingFlags[mktSide] && m_WorkingTotalQtys[mktSide] >= 0 && !double.IsNaN(m_WorkingPrices[mktSide]))
                {
                    string side = (mktSide == 0) ? "Buy" : "Sell";
                    m_KeltnerTraderLogicManager.Log.NewEntry(LogLevel.Minor, "***Update quoting: {0} at {1} @ {2}***", side, m_WorkingTotalQtys[mktSide], m_WorkingPrices[mktSide]);
                    ParentStrategy.Quote(this, mktSide, m_WorkingPrices[mktSide], m_WorkingTotalQtys[mktSide] * UV.Lib.Utilities.QTMath.MktSideToMktSign(mktSide));
                }
                else
                    ParentStrategy.Quote(this, mktSide, m_MarketData[mktSide], 0);
            }
            else
                ParentStrategy.Quote(this, mktSide, m_MarketData[mktSide], 0);
        }
        #endregion


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        /// <summary>
        /// Plot band values for the spread strategy by historical data.
        /// </summary>
        /// <param name="historicalEMA"></param>
        /// <param name="historicalATR"></param>
        private void PlotHistoricalEMAATRBands(SortedList<DateTime, double> historicalEMA, SortedList<DateTime, double> historicalATR)
        {
            List<DateTime> indicatorDataTime = new List<DateTime>(historicalEMA.Keys);
            List<double> bandValues = new List<double>();

            bandValues.Clear();
            foreach (DateTime dt in indicatorDataTime)
            {
                bandValues.Add(historicalEMA[dt]);
            }
            PlotHistoricData(m_GraphID, "EMA", indicatorDataTime, bandValues);

            bandValues.Clear();
            foreach (DateTime dt in indicatorDataTime)
            {
                bandValues.Add(historicalEMA[dt] - m_KeltnerTraderLogicManager.EntryWidth * historicalATR[dt]);
            }
            PlotHistoricData(m_GraphID, "Long Enter", indicatorDataTime, bandValues);

            bandValues.Clear();
            foreach (DateTime dt in indicatorDataTime)
            {
                bandValues.Add(historicalEMA[dt] - m_KeltnerTraderLogicManager.FadeWidth * historicalATR[dt]);
            }
            PlotHistoricData(m_GraphID, "Long Fade", indicatorDataTime, bandValues);

            bandValues.Clear();
            foreach (DateTime dt in indicatorDataTime)
            {
                bandValues.Add(historicalEMA[dt] - m_KeltnerTraderLogicManager.PukeWidth * historicalATR[dt]);
            }
            PlotHistoricData(m_GraphID, "Long Puke", indicatorDataTime, bandValues);

            bandValues.Clear();
            foreach (DateTime dt in indicatorDataTime)
            {
                bandValues.Add(historicalEMA[dt] + m_KeltnerTraderLogicManager.EntryWidth * historicalATR[dt]);
            }
            PlotHistoricData(m_GraphID, "Short Enter", indicatorDataTime, bandValues);

            bandValues.Clear();
            foreach (DateTime dt in indicatorDataTime)
            {
                bandValues.Add(historicalEMA[dt] + m_KeltnerTraderLogicManager.FadeWidth * historicalATR[dt]);
            }
            PlotHistoricData(m_GraphID, "Short Fade", indicatorDataTime, bandValues);

            bandValues.Clear();
            foreach (DateTime dt in indicatorDataTime)
            {
                bandValues.Add(historicalEMA[dt] + m_KeltnerTraderLogicManager.PukeWidth * historicalATR[dt]);
            }
            PlotHistoricData(m_GraphID, "Short Puke", indicatorDataTime, bandValues);

            Log.NewEntry(LogLevel.Minor, "Complete plot historical indicators for this trading strategy");
        }
        //
        //
        /// <summary>
        /// Plot one time series for strategy using historical data.
        /// </summary>
        /// <param name="graphID"></param>
        /// <param name="curveName"></param>
        /// <param name="timeSeriesDateTime"></param>
        /// <param name="timeSeriesData"></param>
        private void PlotHistoricData(int graphID, string curveName, List<DateTime> timeSeriesDateTime, List<double> timeSeriesData)
        {
            if (timeSeriesDateTime.Count == 0 || timeSeriesData.Count == 0)
            {
                Log.NewEntry(LogLevel.Major, "There is no historical data for the strategy {0}.", this.ParentStrategy.Name);
                return;
            }

            DateTime offSet = ParentStrategy.StrategyHub.GetLocalTime();
            if (offSet.TimeOfDay.TotalHours > 16)
                offSet = offSet.AddDays(1.0);
            offSet = offSet.Subtract(offSet.TimeOfDay);

            m_NextPlotDateTime = timeSeriesDateTime[0];
            for (int t = 1; t < timeSeriesDateTime.Count; ++t)
            {
                double hour = timeSeriesDateTime[t].Subtract(offSet).TotalHours;
                if (t < timeSeriesData.Count && timeSeriesDateTime[t] >= m_NextPlotDateTime)
                {
                    m_GraphEngine.AddPoint(graphID, curveName, hour, timeSeriesData[t], true);
                    m_NextPlotDateTime.AddSeconds(m_HistoricalDataPlottingInterval);
                }
            }
        }
        //
        //
        /// <summary>
        /// Called when getting fills.
        /// </summary>
        /// <param name="fill"></param>
        public override void Filled(Fill fill)
        {
            // Record fills and log them.
            int dPos = fill.Qty;                                                                                        // Net spread position change.
            if (dPos == 0)
            {
                Log.NewEntry(LogLevel.Major, "Detect 0 quantity fill.");
                return;
            }
            Log.NewEntry(LogLevel.Minor, "*****Filled:{0} @ {1} at {2}*****", dPos, fill.Price, fill.ExchangeTime);     // Log fills profile.
            int previousPos = m_CurrentPosition;                                                                        // Record previous position for plotting fills on the graph.
            m_ExecutedQty[UV.Lib.Utilities.QTMath.MktSignToMktSide(dPos)] += dPos;                                      // Record executed spread quantities.
            m_CurrentPosition += dPos;                                                                                  // Calculate current net position.
            m_KeltnerTradingVariables.CurrentPos = m_CurrentPosition;                                                   // Record net position into trading variables to save it when the program exits.
            Log.NewEntry(LogLevel.Minor, "Net position becomes {0}.", m_CurrentPosition);                               // Log the current position.
            BroadcastParameter(m_IEngineHub, ParentStrategy, "NetPosition");                                            // Show net position on the GUI.

            lock (m_ProcessingFillLockObject)
            {
                // Process fills in the trading logic manager if there is no risk event happened.
                m_IsProcessingFills = true;
                m_KeltnerTraderLogicManager.TryProcessFills(fill);
                m_IsNeededUpdate[OrderSide.BuySide] = true;
                m_IsNeededUpdate[OrderSide.SellSide] = true;
                m_IsProcessingFills = false;
                SetWorkingOrders();
            }

            // Show fills on the graph.
            if (m_GraphEngine != null)
            {
                bool isLonger = dPos > 0;                                                                               // Up triangle for long trade and down triangle for short trade.
                bool isExit = dPos * previousPos < 0;                                                                   // Plot exit flag using opposite triangle.
                if (isLonger)
                {
                    if (isExit)
                    {
                        Global.TraderLog.NewEntry(LogLevel.Major, "Spread {0} Long Exit {1} @ {2} --> Net Position:{3}.", this.ParentStrategy.Name, Math.Abs(dPos), fill.Price, m_CurrentPosition);
                        m_GraphEngine.AddPoint(m_GraphID, "Short Exit", fill.Price);
                    }
                    else
                    {
                        Global.TraderLog.NewEntry(LogLevel.Major, "Spread {0} Long Enter {1} @ {2} --> Net Position:{3}.", this.ParentStrategy.Name, Math.Abs(dPos), fill.Price, m_CurrentPosition);
                        m_GraphEngine.AddPoint(m_GraphID, "Long Entry", fill.Price);
                    }
                    BroadcastParameter(m_IEngineHub, ParentStrategy, "LongQtyFilled");
                }
                else
                {
                    if (isExit)
                    {
                        Global.TraderLog.NewEntry(LogLevel.Major, "Spread {0} Short Exit {1} @ {2} --> Net Position:{3}.", this.ParentStrategy.Name, Math.Abs(dPos), fill.Price, m_CurrentPosition);
                        m_GraphEngine.AddPoint(m_GraphID, "Long Exit", fill.Price);
                    }
                    else
                    {
                        Global.TraderLog.NewEntry(LogLevel.Major, "Spread {0} Short Enter {1} @ {2} --> Net Position:{3}.", this.ParentStrategy.Name, Math.Abs(dPos), fill.Price, m_CurrentPosition);
                        m_GraphEngine.AddPoint(m_GraphID, "Short Entry", fill.Price);
                    }
                    BroadcastParameter(m_IEngineHub, ParentStrategy, "ShortQtyFilled");
                }
            }

            // Validate fills using strategy risk manager.
            string errorInfo = null;                                                                                    // Try show/log error information if risk event is triggered.
            if (m_KeltnerStrategyRiskManager.DetectFillsRiskEvent(fill.Qty, out errorInfo))
            {
                // This block checks spread position limit breaking situations.
                IsTradingEnabled = false;
                LongEntryAllowed = false;
                ShortEntryAllowed = false;
                Log.NewEntry(LogLevel.Major, errorInfo);
                m_PopUpTool.ShowDialog(DialogType.OverFillsStopLoss, DialogIDMap.OverFillsStopLoss, "Over fills for stop trading", errorInfo);
            }
        }
        //
        //
        /// <summary>
        /// This block is used to request user to confirm initial trading behavior if special cases like puke,fade,entry levels are reached.
        /// </summary>
        /// <param name="marketData"></param>
        /// <param name="lastEMA"></param>
        /// <param name="lastATR"></param>
        /// <param name="lastMOM"></param>
        private void CheckInitialAbnormalStatus(double[] marketData, double lastEMA, double lastATR, double lastMOM)
        {
            double bidPrice = marketData[0];
            double askPrice = marketData[1];

            // First behavior check will be only used once and it is only for initial position checkings.
            if (!m_IsFirstBehaviorCheck && !double.IsNaN(lastEMA) && !double.IsNaN(lastATR) && !double.IsNaN(lastMOM))
            {
                DialogType dialogType;
                string topic;
                string text;
                if (askPrice < lastEMA - m_KeltnerTraderLogicManager.PukeWidth * lastATR)
                {
                    // Long puke when the ask price is lower than the corresponding threshold.
                    dialogType = DialogType.InitialPukeConfirm;
                    topic = "Long Puke Notice";
                    text = string.Format("The position is in long puke state, stop entering flags now for {0}. The current ask price {1} is lower than long puke level {2}.", this.ParentStrategy.Name
                        , askPrice, lastEMA - m_KeltnerTraderLogicManager.PukeWidth * lastATR);
                    LongEntryAllowed = false;
                    m_PopUpTool.ShowDialog(dialogType, DialogIDMap.InitialPukeConfirm, topic, text);
                    Log.NewEntry(LogLevel.Minor, "Initial trading behavior shows long puke confirm dialog.");
                }
                else if (bidPrice > lastEMA + m_KeltnerTraderLogicManager.PukeWidth * lastATR)
                {
                    // Short puke when the ask price is lower than the corresponding threshold.
                    dialogType = DialogType.InitialPukeConfirm;
                    topic = "Short Puke Notice";
                    text = string.Format("The position is in short puke state, stop entering flags now for {0}. The current bid price {1} is higher than short puke level {2}", this.ParentStrategy.Name
                        , bidPrice, lastEMA + m_KeltnerTraderLogicManager.PukeWidth * lastATR);
                    ShortEntryAllowed = false;
                    m_PopUpTool.ShowDialog(dialogType, DialogIDMap.InitialPukeConfirm, topic, text);
                    Log.NewEntry(LogLevel.Minor, "Initial trading behavior shows short puke confirm dialog.");
                }
                else if (askPrice < lastEMA - m_KeltnerTraderLogicManager.FadeWidth * lastATR && askPrice >= lastEMA - m_KeltnerTraderLogicManager.PukeWidth * lastATR)
                {
                    // Long fade when the ask price is lower than the corresponding threshold.
                    dialogType = DialogType.InitialFadeLevelReached;
                    topic = "Long Fade Notice";
                    text = string.Format("The current ask price {1} is lower than long fade level {2}. The position is in long fade state for {0}, do you still want to trade?", this.ParentStrategy.Name
                        , askPrice, lastEMA - m_KeltnerTraderLogicManager.FadeWidth * lastATR);
                    m_PopUpTool.ShowDialog(dialogType, DialogIDMap.InitialFadeLevelReached, topic, text);
                    Log.NewEntry(LogLevel.Minor, "Initial trading behavior shows long fade confirm dialog.");
                }
                else if (bidPrice > lastEMA + m_KeltnerTraderLogicManager.FadeWidth * lastATR && bidPrice <= lastEMA + m_KeltnerTraderLogicManager.PukeWidth * lastATR)
                {
                    // Short fade when the ask price is lower than the corresponding threshold.
                    dialogType = DialogType.InitialFadeLevelReached;
                    topic = "Short Fade Notice";
                    text = string.Format("The current bid price {1} is higher than short fade level {2}. The position is in short fade state for {0}, do you still want to trade?", this.ParentStrategy.Name
                        , bidPrice, lastEMA + m_KeltnerTraderLogicManager.FadeWidth * lastATR);
                    m_PopUpTool.ShowDialog(dialogType, DialogIDMap.InitialFadeLevelReached, topic, text);
                    Log.NewEntry(LogLevel.Minor, "Initial trading behavior shows short fade confirm dialog.");
                }
                else if (askPrice < lastEMA - m_KeltnerTraderLogicManager.EntryWidth * lastATR && askPrice >= lastEMA - m_KeltnerTraderLogicManager.FadeWidth * lastATR)
                {
                    // Long entry when the ask price is lower than the corresponding threshold.
                    dialogType = DialogType.InitialEntryLevelReached;
                    topic = "Long Entry Notice";
                    text = string.Format("The current ask price {1} is lower than long entry level {2}. The position is in long entry state for {0}, do you still want to trade?", this.ParentStrategy.Name
                        , askPrice, lastEMA - m_KeltnerTraderLogicManager.EntryWidth * lastATR);
                    m_PopUpTool.ShowDialog(dialogType, DialogIDMap.InitialEntryLevelReached, topic, text);
                    Log.NewEntry(LogLevel.Minor, "Initial trading behavior shows long entry confirm dialog.");
                }
                else if (bidPrice > lastEMA + m_KeltnerTraderLogicManager.EntryWidth * lastATR && bidPrice <= lastEMA + m_KeltnerTraderLogicManager.FadeWidth * lastATR)
                {
                    // Short entry when the ask price is lower than the corresponding threshold.
                    dialogType = DialogType.InitialEntryLevelReached;
                    topic = "Short Entry Notice";
                    text = string.Format("The current bid price {1} is higher than short entry level {2}. The position is in short entry state for {0}, do you still want to trade?", this.ParentStrategy.Name
                        , bidPrice, lastEMA + m_KeltnerTraderLogicManager.EntryWidth * lastATR);
                    m_PopUpTool.ShowDialog(dialogType, DialogIDMap.InitialEntryLevelReached, topic, text);
                    Log.NewEntry(LogLevel.Minor, "Initial trading behavior shows short entry confirm dialog.");
                }
                else
                {
                    // Normal status and pass first check.
                    m_IsFirstBehaviorCheckConfirmed = true;
                    if (m_InitialPositionValidated && m_IsMarketActive)
                        IsTradingEnabled = true;
                    Log.NewEntry(LogLevel.Minor, "Initial trading behavior passed directly and no entry or fade level reached at the beginning for {0}. There is no abnormal situation", this.ParentStrategy.Name);
                }
                m_IsFirstBehaviorCheck = true;                                                                                  // Flag the first behavior check true after it is first triggered.
            }
        }
        //
        //
        /// <summary>
        /// Update the economic event blocing status.
        /// </summary>
        private void UpdateEconomicBlockingStatus()
        {
            if (m_EconomicBlockingEventList.Count > 0)
            {
                // If any one event is still blocking the trade, do not allow entry.
                LongEntryAllowed = false;
                ShortEntryAllowed = false;
            }
            else
            {
                // If all event is not blocking the trade, allow entry.
                LongEntryAllowed = true;
                ShortEntryAllowed = true;
            }
        }
        //
        //
        /// <summary>
        /// Load ranges of market open/close times for trading.
        /// </summary>
        private void LoadMarketOpenCloseTime()
        {
            // Parse time range by '/'.
            string timeString = m_KeltnerTradingVariables.MarketRunningDateTime;
            char[] delimiter = { '/' };
            string[] timeRanges = timeString.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
            int rangeCount = timeRanges.Length;
            m_MarketOpen = new List<DateTime>();
            m_MarketClose = new List<DateTime>();

            // Parse open and close time by '-'. Sort them in order.
            delimiter[0] = '-';
            int index = 0;
            DateTime tempDateTime;
            foreach (string timeRange in timeRanges)
            {
                string[] openCloseTime = timeRange.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
                if (!DateTime.TryParseExact(openCloseTime[0], "HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out tempDateTime))
                    Log.NewEntry(LogLevel.Major, "Parse market open date time failed for {0}.", openCloseTime[0]);
                else
                    m_MarketOpen.Add(tempDateTime);

                if (!DateTime.TryParseExact(openCloseTime[1], "HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out tempDateTime))
                    Log.NewEntry(LogLevel.Major, "Parse market open date time failed for {0}.", openCloseTime[1]);
                else
                    m_MarketClose.Add(tempDateTime);
                index++;
            }
            if (m_MarketOpen.Count != m_MarketClose.Count)
                Log.NewEntry(LogLevel.Major, "Load incorrect pair number for market open/close range");
            for (index = 0; index < rangeCount; ++index)
            {
                if (m_MarketOpen[index] > m_MarketClose[index])
                    m_MarketOpen[index] = m_MarketOpen[index].AddDays(-1);
            }

            // Set correct exchange open/close times.
            DateTime localDateTime = DateTime.Now;
            for (index = 0; index < rangeCount; ++index)
            {
                if (localDateTime > m_MarketClose[index])
                {
                    m_MarketClose[index] = m_MarketClose[index].AddDays(1);
                    m_MarketOpen[index] = m_MarketOpen[index].AddDays(1);
                }
            }
            m_MarketOpen.Sort();
            m_MarketClose.Sort();
        }
        //
        //
        /// <summary>
        /// Initialize market active flag.
        /// </summary>
        private void InitializeMarketState()
        {
            DateTime localDateTime = DateTime.Now;
            if (localDateTime > m_MarketOpen[0] && localDateTime < m_MarketClose[0])
                m_IsMarketActive = true;
            else
            {
                m_IsMarketActive = false;
                string topic = "Exchange Close Notice";
                string text = string.Format("The exchange is currently inactive. The market will be active at {0} to {1} specified by user as exchange open/close times.", m_MarketOpen[0], m_MarketClose[0]);
                m_PopUpTool.ShowDialog(DialogType.ExchangeOpenCloseNotice, DialogIDMap.ExchangeOpenCloseNotice, topic, text);
            }
        }
        //
        //
        /// <summary>
        /// Set datetime for the market open and close.
        /// </summary>
        /// <param name="localHistoricalDateTime"></param>
        private void SetHistoricalMarketOpenClose(DateTime localHistoricalDateTime)
        {
            List<DateTime> hisOpenDateTime = new List<DateTime>();
            List<DateTime> hisCloseDateTime = new List<DateTime>();
            foreach (DateTime dt in m_MarketOpen)
            {
                hisOpenDateTime.Add(localHistoricalDateTime.Date.Add(dt.TimeOfDay));
            }
            foreach (DateTime dt in m_MarketClose)
            {
                hisCloseDateTime.Add(localHistoricalDateTime.Date.Add(dt.TimeOfDay));
            }
            int rangeCount = hisOpenDateTime.Count;
            m_MarketOpen = hisOpenDateTime;
            m_MarketClose = hisCloseDateTime;
            for (int index = 0; index < rangeCount; ++index)
            {
                if (m_MarketOpen[index] > m_MarketClose[index])
                    m_MarketOpen[index] = m_MarketOpen[index].AddDays(-1);
            }
            for (int index = 0; index < rangeCount; ++index)
            {
                if (localHistoricalDateTime > hisCloseDateTime[index])
                {
                    hisOpenDateTime[index] = hisOpenDateTime[index].AddDays(1);
                    hisCloseDateTime[index] = hisCloseDateTime[index].AddDays(1);
                }
            }
            hisOpenDateTime.Sort();
            hisCloseDateTime.Sort();

            if (localHistoricalDateTime > m_MarketOpen[0] && localHistoricalDateTime < m_MarketClose[0])
            {
                m_IsMarketActive = true;
                IsTradingEnabled = true;
            }
            else
            {
                m_IsMarketActive = false;
                IsTradingEnabled = false;
            }
        }
        //
        //
        /// <summary>
        /// Update the market active flag.
        /// </summary>
        private void UpdateMarketActiveStatus(DateTime updateDateTime)
        {
            if (m_IsMarketActive)
            {
                // Check whether the time reached the exchange close time.
                if (updateDateTime > m_MarketClose[0])
                {
                    m_MarketClose[0] = m_MarketClose[0].AddDays(1);
                    m_MarketOpen[0] = m_MarketOpen[0].AddDays(1);
                    m_IsMarketActive = false;
                    IsTradingEnabled = false;
                    m_MarketOpen.Sort();
                    m_MarketClose.Sort();
                    if (DateTime.Now <= updateDateTime)
                    {
                        string topic = "Exchange Close Notice";
                        string text = string.Format("The exchange is inactive now. The market will be active at {0} to {1} specified by user as exchange open/close times.", m_MarketOpen[0], m_MarketClose[0]);
                        m_PopUpTool.ShowDialog(DialogType.ExchangeOpenCloseNotice, DialogIDMap.ExchangeOpenCloseNotice, topic, text);
                    }
                }
            }
            else
            {
                // Check whether the time reached the open.
                if (updateDateTime >= m_MarketOpen[0] && updateDateTime <= m_MarketClose[0])
                {
                    m_IsMarketActive = true;
                    IsTradingEnabled = true;
                }
            }
        }
        //
        //
        /// <summary>
        /// Edit the working orders.
        /// </summary>
        /// <param name="m_MarketData"></param>
        /// <returns></returns>
        private void SetWorkingOrders()
        {
            // Initial behavior checked.
            if (!m_IsFirstBehaviorCheckConfirmed)
                return;

            // Flag of processing fills.
            if (m_IsProcessingFills)
                return;

            // Flag of setting orders.
            if (m_IsSettingOrders)
                return;

            lock (m_SetOrdersLockObject)
            {
                m_IsSettingOrders = true;
                double[] workingPrices;
                int[] workingQtys;
                bool[] workingFlags;
                m_KeltnerTraderLogicManager.TryGetCurrentWorkingFlagQtyPrice(m_MarketData, out workingFlags, out workingQtys, out workingPrices);

                if (m_IsNeededUpdate[OrderSide.BuySide])
                {
                    m_WorkingFlags[OrderSide.BuySide] = workingFlags[OrderSide.BuySide];
                    WorkingBuyPrice = workingPrices[OrderSide.BuySide];
                    TotalWorkingBuyQty = workingQtys[OrderSide.BuySide];
                    UpdateModelOrders(OrderSide.BuySide);
                    m_IsNeededUpdate[OrderSide.BuySide] = false;
                }

                if (m_IsNeededUpdate[OrderSide.SellSide])
                {
                    m_WorkingFlags[OrderSide.SellSide] = workingFlags[OrderSide.SellSide];
                    WorkingSellPrice = workingPrices[OrderSide.SellSide];
                    TotalWorkingSellQty = workingQtys[OrderSide.SellSide];
                    UpdateModelOrders(OrderSide.SellSide);
                    m_IsNeededUpdate[OrderSide.SellSide] = false;
                }
                m_IsSettingOrders = false;
            }
        }
        //
        //
        /// <summary>
        /// Determine whether the data is good.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        private bool IsDataGood(double newValue)
        {
            if (double.IsNaN(m_LastValidatedDataFeed))
            {
                m_LastValidatedDataFeed = newValue;
                return true;
            }
            else
            {
                if (!double.IsNaN(newValue))
                {
                    if (Math.Abs(newValue) < Math.Abs(m_LastValidatedDataFeed) * (1 - m_RejectDataPercentThreshold) && Math.Abs(newValue) > Math.Abs(m_LastValidatedDataFeed) * (1 + m_RejectDataPercentThreshold))
                        return false;
                    else
                    {
                        m_LastValidatedDataFeed = newValue;
                        return true;
                    }
                }
                else
                    return false;
            }
        }
        //
        //
        /// <summary>
        /// Update spread market prices.
        /// </summary>
        private void UpdateSpreadMarketPrices()
        {
            double bidPrice = this.ImpliedMarket.Price[BidSide][0];
            double askPrice = this.ImpliedMarket.Price[AskSide][0];
            double midPrice = 0.5 * (bidPrice + askPrice);
            m_MarketData[0] = bidPrice;
            m_MarketData[1] = askPrice;
            m_MarketData[2] = midPrice;
        }
        #endregion


        #region Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        /// <summary>
        /// The trigger of alarm for economic event.
        /// </summary>
        /// <param name="engineEventArgs"></param>
        public override void AlarmTriggered(EngineEventArgs engineEventArgs)
        {
            if (engineEventArgs.MsgType == EngineEventArgs.EventType.AlarmTriggered)
            {
                if (engineEventArgs.Status == EngineEventArgs.EventStatus.EconomicEventEnd)
                {
                    Log.NewEntry(LogLevel.Major, "AlarmTriggered: Economic event alarm message received to resume trading");
                    foreach (object o in engineEventArgs.DataObjectList)
                    {
                        if (o.GetType() == typeof(EconomicDataItem))
                        {
                            EconomicDataItem economicData = (EconomicDataItem)o;
                            Log.NewEntry(LogLevel.Minor, "EconomicEventName: {0} @ {1} End", economicData.EventName, economicData.TimeStamp);
                            if (m_EconomicBlockingEventList.Contains(economicData.EventName))
                                m_EconomicBlockingEventList.Remove(economicData.EventName);
                        }
                    }
                    BroadcastAllParameters(m_IEngineHub, ParentStrategy);
                    UpdateEconomicBlockingStatus();
                }
                else if (engineEventArgs.Status == EngineEventArgs.EventStatus.EconomicEventStart)
                {
                    Log.NewEntry(LogLevel.Major, "AlarmTriggered: Economic event alarm message received to stop trading");
                    foreach (object o in engineEventArgs.DataObjectList)
                    {
                        if (o.GetType() == typeof(EconomicDataItem))
                        {
                            EconomicDataItem economicData = (EconomicDataItem)o;
                            Log.NewEntry(LogLevel.Minor, "EconomicEventName: {0} @ {1} Start", economicData.EventName, economicData.TimeStamp);
                            if (!m_EconomicBlockingEventList.Contains(economicData.EventName))
                                m_EconomicBlockingEventList.Add(economicData.EventName);
                        }
                    }
                    BroadcastAllParameters(m_IEngineHub, ParentStrategy);
                    UpdateEconomicBlockingStatus();
                }
                else
                {
                    Log.NewEntry(LogLevel.Major, "AlarmTriggered: Uknown Event Economic Event Type Received.");
                }
            }
        }
        //
        //
        /// <summary>
        /// Triggered when the stop orders are detected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TradingLogicManagerBase_StopOrderTriggered(object sender, EventArgs e)
        {
            StopOrderEventArgs stopOrderEventArgs = (StopOrderEventArgs)e;
            DialogType showWarningDialog = DialogType.StopOrderTriggered;
            switch (stopOrderEventArgs.TradeSide)
            {
                case TradeSide.Unknown:
                    break;
                case TradeSide.Buy:
                    m_IsNeededUpdate[OrderSide.SellSide] = true;
                    break;
                case TradeSide.Sell:
                    m_IsNeededUpdate[OrderSide.BuySide] = true;
                    break;
            }
            string topic = "Stop Order Notice";
            string text = string.Format("Stop order signal happened for {0}. The detail is {1} for {2} with {3} @ {4}.", this.ParentStrategy.Name, stopOrderEventArgs.StopOrderEventType, stopOrderEventArgs.TradeSide, stopOrderEventArgs.StopQty, stopOrderEventArgs.StopPrice);
            m_PopUpTool.ShowDialog(showWarningDialog, DialogIDMap.StopOrderTriggered, topic, text);
            m_KeltnerStrategyRiskManager.SetUpStopLossTimer(stopOrderEventArgs.TradeSide, m_KeltnerTradingVariables.StopLossTimeTrack);
        }
        //
        //
        /// <summary>
        /// Called when the dialog is responsed by the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PopUpTool_DialogUserComplete(object sender, EventArgs e)
        {
            PopUpEventArgs eventArgs = (PopUpEventArgs)e;
            DialogType dialogType = (DialogType)eventArgs.Data[0];
            bool answer;
            switch (dialogType)
            {
                case DialogType.PositionValidationYesNo:
                    answer = (bool)eventArgs.Data[1];
                    m_KeltnerTraderLogicManager.AdjustTradingNodes(m_CurrentPosition);
                    if (!answer)
                    {
                        DialogType inputDialog = DialogType.PositionValidationInput;
                        string topic = "Position Validation";
                        string text = string.Format("Please input the correct position for {0}", this.ParentStrategy.Name);
                        m_PopUpTool.ShowDialog(inputDialog, DialogIDMap.PositionValidationInput, topic, text);
                    }
                    else
                    {
                        m_InitialPositionValidated = true;
                        m_KeltnerTraderLogicManager.LogTradingNodesInfo();
                    }
                    Log.NewEntry(LogLevel.Minor, "User answer:{0} for position validation yes or no question for {1}.", answer, this.ParentStrategy.Name);
                    break;
                case DialogType.PositionValidationInput:
                    int correctPosition = (int)eventArgs.Data[1];
                    m_CurrentPosition = correctPosition;
                    BroadcastParameter(m_IEngineHub, ParentStrategy, "NetPosition");
                    m_KeltnerTradingVariables.CurrentPos = m_CurrentPosition;
                    m_KeltnerStrategyRiskManager.NetPosition = m_CurrentPosition;
                    m_KeltnerTraderLogicManager.AdjustTradingNodes(m_CurrentPosition);
                    m_InitialPositionValidated = true;
                    m_KeltnerTraderLogicManager.LogTradingNodesInfo();
                    Log.NewEntry(LogLevel.Minor, "User answer:{0} for position validation input correct position for {1}.", correctPosition, this.ParentStrategy.Name);
                    break;
                case DialogType.InitialPukeConfirm:
                    m_IsFirstBehaviorCheckConfirmed = true;
                    Log.NewEntry(LogLevel.Minor, "User saw the puke dialog for {0}.", this.ParentStrategy.Name);
                    break;
                case DialogType.InitialFadeLevelReached:
                    answer = (bool)eventArgs.Data[1];
                    if (!answer)
                    {
                        TradeSide tradeSide;
                        Enum.TryParse(eventArgs.Data[2].ToString(), out tradeSide);
                        switch (tradeSide)
                        {
                            case TradeSide.Unknown:
                                LongEntryAllowed = false;
                                ShortEntryAllowed = false;
                                break;
                            case TradeSide.Buy:
                                LongEntryAllowed = false;
                                break;
                            case TradeSide.Sell:
                                ShortEntryAllowed = false;
                                break;
                        }
                    }
                    m_IsFirstBehaviorCheckConfirmed = true;
                    if (m_InitialPositionValidated && m_IsMarketActive)
                        IsTradingEnabled = true;
                    Log.NewEntry(LogLevel.Minor, "User answer:{0} for fade initial entry yes or no question for {1}.", answer, this.ParentStrategy.Name);
                    break;
                case DialogType.InitialEntryLevelReached:
                    answer = (bool)eventArgs.Data[1];
                    if (!answer)
                    {
                        TradeSide tradeSide;
                        Enum.TryParse(eventArgs.Data[2].ToString(), out tradeSide);
                        switch (tradeSide)
                        {
                            case TradeSide.Unknown:
                                LongEntryAllowed = false;
                                ShortEntryAllowed = false;
                                break;
                            case TradeSide.Buy:
                                m_KeltnerTraderLogicManager.TrySetWorkingEnterFlag(tradeSide, 0, false);
                                break;
                            case TradeSide.Sell:
                                m_KeltnerTraderLogicManager.TrySetWorkingEnterFlag(tradeSide, 0, false);
                                break;
                        }
                    }
                    m_IsFirstBehaviorCheckConfirmed = true;
                    if (m_InitialPositionValidated && m_IsMarketActive)
                        IsTradingEnabled = true;
                    Log.NewEntry(LogLevel.Minor, "User answer:{0} for enter initial entry yes or no question for {1}.", answer, this.ParentStrategy.Name);
                    break;
                default:
                    break;
            }
        }
        #endregion


        #region ITimerSubscriber Implementation
        /// <summary>
        /// Called every second to update indicators and orders.
        /// </summary>
        /// <param name="aBook"></param>
        public void TimerSubscriberUpdate(Book aBook)
        {
            UpdateMarketActiveStatus(DateTime.Now);                                                                                   // Update the market active state.

            if (m_IsMarketActive)
            {
                UpdateSpreadMarketPrices();

                // Continue to update indicators in real time only when the spread market is active.
                DateTime localTimeNow = DateTime.Now;
                m_KeltnerIndicatorManager.FeedDataToAllIndicators(localTimeNow, m_MarketData[2]);

                // Get the calculated indicators values.
                double lastEMA = double.NaN;
                double lastATR = double.NaN;
                double lastMOM = double.NaN;
                m_KeltnerIndicatorManager.TryGetIndicatorValues(out lastEMA, out lastATR, out lastMOM);

                if (!m_InitialPositionValidated)
                    return;                                                                                                           // Check the initial positions validated information.

                CheckInitialAbnormalStatus(m_MarketData, lastEMA, lastATR, lastMOM);                                                  // Check the initial abnormal trading status.

                BroadcastParameter(m_IEngineHub, ParentStrategy, "LastEMA");
                BroadcastParameter(m_IEngineHub, ParentStrategy, "LastATR");
                BroadcastParameter(m_IEngineHub, ParentStrategy, "LastMomentum");
                BroadcastParameter(m_IEngineHub, ParentStrategy, "BidPrice");
                BroadcastParameter(m_IEngineHub, ParentStrategy, "AskPrice");
                BroadcastParameter(m_IEngineHub, ParentStrategy, "MidPrice");
                BroadcastParameter(m_IEngineHub, ParentStrategy, "LongEnterBand");
                BroadcastParameter(m_IEngineHub, ParentStrategy, "LongFadeBand");
                BroadcastParameter(m_IEngineHub, ParentStrategy, "LongPukeBand");
                BroadcastParameter(m_IEngineHub, ParentStrategy, "ShortEnterBand");
                BroadcastParameter(m_IEngineHub, ParentStrategy, "ShortFadeBand");
                BroadcastParameter(m_IEngineHub, ParentStrategy, "ShortPukeBand");

                // Update graph.
                if (localTimeNow >= m_NextPlotDateTime)
                {
                    m_GraphEngine.AddPoint(m_GraphID, "Price", m_MarketData[2]);
                    m_GraphEngine.AddPoint(m_GraphID, "EMA", lastEMA);
                    m_GraphEngine.AddPoint(m_GraphID, "Long Enter", lastEMA - m_KeltnerTraderLogicManager.EntryWidth * lastATR);
                    m_GraphEngine.AddPoint(m_GraphID, "Long Fade", lastEMA - m_KeltnerTraderLogicManager.FadeWidth * lastATR);
                    m_GraphEngine.AddPoint(m_GraphID, "Long Puke", lastEMA - m_KeltnerTraderLogicManager.PukeWidth * lastATR);
                    m_GraphEngine.AddPoint(m_GraphID, "Short Enter", lastEMA + m_KeltnerTraderLogicManager.EntryWidth * lastATR);
                    m_GraphEngine.AddPoint(m_GraphID, "Short Fade", lastEMA + m_KeltnerTraderLogicManager.FadeWidth * lastATR);
                    m_GraphEngine.AddPoint(m_GraphID, "Short Puke", lastEMA + m_KeltnerTraderLogicManager.PukeWidth * lastATR);
                    m_NextPlotDateTime.AddSeconds(m_HistoricalDataPlottingInterval);
                }

                // Send orders.
                m_KeltnerTraderLogicManager.UpdateTradingNodesPricesBools(m_MarketData[2], lastEMA, lastATR, lastMOM, ref m_IsNeededUpdate);    // Update the prices, quantities and flags for the working orders.
                SetWorkingOrders();                                                                                                             // Set working orders.
            }
        }
        #endregion


        #region IStringifiable implentation
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
        #endregion

    }
}