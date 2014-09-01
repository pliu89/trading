using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UV.Lib.Database
{
    public class TableInfo
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        //
        public string TableNameFull = string.Empty;				// Compound name of "databaseName.tableName"
        #endregion// members

        #region ExchangeTableInfo
        public class ExchangesTableInfo : TableInfo
        {
            // Column Names
            public string ExchangeID = "exchangeId";
            public string ExchangeName = "exchangeNameTT";
            public string SecondaryExchange = "secondaryExchangeName";
            public string ExchangeTZ = "exchangeTimezone";


            // Constructor
            public ExchangesTableInfo()
            {
                TableNameFull = "uv.exchangeInfo";
            }
        }
        #endregion

        #region ProductsTableInfo
        public class ProductsTableInfo : TableInfo
        {
            // Column Names
            public string ProductID = "prodSetId";
            public string Product = "prodSet";
            public string Exchange = "exchange";
            public string ProductType = "prodType";

            public string SessionOpen = "session1Open";
            public string SessionClose = "session1Close";

            public string Dollarization = "dollarization";
            public string TickSize = "tickOutright";
            
            // TT Specific params
            public string unitTT = "unitTT";
            public string tickTT = "tickTT";
            public string smallTickTT = "smallTickTT";
            public string calendarTickTT = "calendarTickTT";
            
            // Constructor
            public ProductsTableInfo()
            {
                TableNameFull = "uv.productInfo";
            }
        }
        #endregion

        #region InstrumentsTableInfo
        public class InstrumentsTableInfo : TableInfo
        {
            public string InstrumentID = "instrId";
            public string Product = "prodNameTT";
            public string ExchangeID = "exchangeId";

            public string ExpirySymbol = "prodExpiry";
            public string ExpirationDate = "lastTradeDt";
            public string ProductType = "prodType";
            public string SpreadComposition = "spreadComposition";
            
            // TT Specific params
            public string unitTT = "unitTT";
            public string tickTT = "tickTT";
            public string smallTickTT = "smallTickTT";
            public string calendarTickTT = "calendarTickTT";

            // Constructor
            public InstrumentsTableInfo()
            {
                TableNameFull = "uv.instrumentInfo";
            }


        }
        #endregion

        #region BarsTableInfo
        public class BarsTableInfo : TableInfo
        {
            //
            // Column names
            //
            public string InstrumentID = "instrumentID";
            public string TimeStamp = "unixTime";

            public string BidPrice = "bidPrice";
            public string BidQty = "bidQty";
            public string AskPrice = "askPrice";
            public string AskQty = "askQty";
            public string LastPrice = "lastTradePrice";

            public string SessionVolume = "sessionVolume";          // as reported by TT
            public string LongVolume = "longVolume";                // summed from Time and Sales
            public string ShortVolume = "shortVolume";              // summed from Time and Sales
            public string TotalVolume = "totalVolume";              // summed from Time and Sales
            public string SessionCode = "sessionCode";              // code 1 == Trading 0 == Not Trading
            //
            // Information about tables
            //
            public DateTime EarliestDateTime = new DateTime(2011, 07, 26).ToLocalTime();

            // Constructor
            public BarsTableInfo()
            {
                TableNameFull = "md.marketData";
            }
        }
        #endregion // bar table






        #region Tramp Strategies
        public class TrampStrategiesTableInfo : TableInfo
        {
            public string ID = "strategyId";
            public string GroupID = "groupId";
            public string TimeStamp = "updateTime";
            public string StrategyName = "strategyName";

            public string StrategyEngine = "strategyEngine";
            public string PricingEngine = "pricingEngine";
            public string ModelEngine = "modelEngine";
            public string OrderEngine = "orderEngine";
            public string FillEngine = "fillEngine";
            public string GraphEngine = "graphEngine";
            public string MessageEngine = "messageEngine";

            // Model extras
            public string BinEdges = "binEdges";
            public string Signals = "signals";
            public string Factors = "factors";

            // not used
            public string PNLDaily = "pnlDaily";

            //
            // Constructor
            //
            public TrampStrategiesTableInfo()
            {
                TableNameFull = "tramp.strategies";
            }

        }
        #endregion // strategies table

        #region Tramp Model Engines
        public class TrampModelEnginesTableInfo : TableInfo
        {
            public string ID = "Id";				// int: auto-incremented model engine mysql id.
            public string GroupID = "packId";			// int: indentifier that collects multiple engines together.
            public string Engine = "modelEngine";	// string - class name

            public string EngineParameters = "trampParameters";	// parameters for defining engine
            public string BackTestParameters = "backtestParameters";// parameters for backtester
            //
            // Constructor
            //
            public TrampModelEnginesTableInfo()
            {
                TableNameFull = "tramp.modelengines";
            }

        }
        #endregion // model engines table

        #region Tramp Orders
        public class TrampOrdersTableInfo : TableInfo
        {
            public string Auto_ID = "Id";		// AUTO INCREMENTED

            public string StrategyID = "strategyId";
            public string StrategyName = "strategyName";

            public string RunType = "runType";
            public string Qty = "quantity";
            public string IsSell = "isSell";
            public string Price = "price";
            public string TimeStamp = "time";

            // Constructor
            public TrampOrdersTableInfo()
            {
                TableNameFull = "tramp.orders";
            }
        }
        #endregion // strategies table

        #region Tramp Fills
        public class TrampFillsTableInfo : TableInfo
        {
            public string Auto_ID = "Id";				// auto incremented

            public string StrategyID = "strategyId";		// null or int
            public string EngineID = "engineId";		// null or Int
            public string StrategyName = "strategyName";

            public string RunType = "runType";
            public string Agent = "agent";
            public string Account = "account";
            public string Price = "price";
            public string Qty = "quantity";
            public string TimeStamp = "time";			// TimeStamp created by MySql

            public string TangoTimeStamp = "tangoFillTime";
            public string TangoTradable = "tangoTradable";
            public string Info = "eventInfo";		// string for model-dependent info

            // Constructor
            public TrampFillsTableInfo()
            {
                TableNameFull = "tramp.fills";
            }
        }
        #endregion // strategies table

        #region Tramp model signals
        public class TrampModelSignalsTableInfo : TableInfo
        {
            public string StrategyID = "strategyId";
            public string EngineID = "engineId";
            public string EngineName = "engineName";

            public string RunType = "runType";
            public string TimeStamp = "time";
            public string Qty = "tradeQty";
            public string IsExit = "isExit";
            public string TradeReason = "tradeReason";
            public string EventType = "eventType";
            public string EventInfo = "eventInfo";

            public string InitialTimeStamp = "initialTime";
            public string InitialInfo = "initialInfo";

            // Constructor
            public TrampModelSignalsTableInfo()
            {
                TableNameFull = "tramp.modelsignals";
            }
        }
        #endregion // strategies table

        #region Tramp Positions
        public class TrampModelPositionsTableInfo : TableInfo
        {
            public string StrategyID = "id";
            public string TimeStamp = "time";

            public string PositionBook = "positionBook";
            public string MarketPrice = "marketPrice";
            public string Position = "position";
            public string EntryPrice = "entryPrice";
            public string FairValue = "fairValue";

            // Constructor
            public TrampModelPositionsTableInfo()
            {
                TableNameFull = "tramp.positions";
            }
        }
        #endregion // strategies table





        #region Bloomberg Economic Events
        public class BloombergEconomicEvents : TableInfo
        {
            public string EventID = "eventIdx";
            public string EventType = "eventType";
            public string Date = "date";				// Event date format: "20120601"
            public string TimeStamp = "eventDate";		// mysql date/time format: "2012-06-01 07:30:00"
            public string UnixTime = "unixtime";

            public string SurveyMedian = "surveyMedian";
            public string SurveyLow = "surveyLow";
            public string SurveyHigh = "surveyHigh";
            public string ActualValue = "actualVal";

            public string RevisedValue = "revisedVal";
            public string RevisionDateFirst = "firstRevisionDate"; // Format "20120601"


            // Constructor
            public BloombergEconomicEvents()
            {
                TableNameFull = "bbg.economicevents12";
            }
        }
        #endregion// bloomberg table

        #region Bloomberg Economic Event Types
        public class BloombergEconomicEventTypes : TableInfo
        {
            public string EventType = "eventType";

            public string Ticker = "eventTicker";
            public string Name = "eventName";
            public string ReleaseTime = "releaseTime";  // format "00:00:00.000"

            public string Importance = "importance";
            public string Country = "country";


            // Constructor
            public BloombergEconomicEventTypes()
            {
                TableNameFull = "bbg.eventtype12";
            }
        }
        #endregion

        #region Bloomberg Important Economic Events
        public class BloombergImportantEconomicEvents : TableInfo
        {
            public string ProductType = "prodType";
            public string EventType = "eventType";


            // Constructor
            public BloombergImportantEconomicEvents()
            {
                TableNameFull = "bbg.importantevents";
            }
        }
        #endregion

    }
}
