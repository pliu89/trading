using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UV.Lib.DatabaseReaderWriters
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

            public string ExchangeName = "exchangeName";
            public string ExchangeNameTT = "exchangeNameTT";
            public string SecondaryExchange = "secondaryExchangeName";
            public string ExchangeTimeZone = "exchangeTimezone";


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
            public string ProductID = "prodFamilyId";
            public string Product = "prodName";
            public string Exchange = "exchangeId";
            public string ProductType = "prodType";

            public string SessionOpen = "session1Open";
            public string SessionClose = "session1Close";

            public string Unit = "unit";
            public string TickValue = "tick";
            public string SmallTickValue = "smallTick";
            
            
            // TT Specific params
            public string ProductTT = "prodNameTT";
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

            public string InstrumentName = "instrumentName";
            public string InstrumentNameTT = "instrumentNameTT";

            public string ProdType = "prodType";
            public string ProdFamilyID = "prodFamilyId";
            public string ExpirySymbol = "prodExpiry";
            public string LastTradeDate = "lastTradeDt";
            public string InstrumentExpireDate = "prodExpiryDt";
            public string Currency = "currency";
            
            
            // UV specific params
            public string unit = "unit";
            public string tick = "tick";
            public string smallTick = "smallTick";
            public string calendarTick = "calendarTick";
            public string SpreadComposition = "spreadComposition";


            // TT Specific params
            public string unitTT = "unitTT";
            public string tickTT = "tickTT";
            public string smallTickTT = "smallTickTT";
            public string calendarTickTT = "calendarTickTT";

            // Sessions
            public string SessionElectronicBeginTime = "startSessionT";
            public string SessionElectronicEndTime = "endSessionT";

            public string SessionCashBeginTime = "startSessionCashT";
            public string SessionCashEndTime = "endSessionCashT";

            public string SessionFloorBeginTime = "startSessionOutcryT";
            public string SessionFloorEndTime = "endSessionOutcryT ";

            // Hedge options.
            public string HedgeOptions = "hedgeOptions";
           

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




        #region UV Strategies
        public class UVStrategiesTableInfo : TableInfo
        {
            // Strategy Identification.
            public string StrategyId = "strategyId";
            public string GroupID = "groupId";            
            public string StrategyName = "strategyName";
            public string RunType = "runType";
            public string UpdatedTimeStamp = "updatedTimeStamp";

            // Start/end dates
            public string StartDate = "startDate";
            public string StartTime = "startTime";
            public string EndDate = "endDate";
            public string EndTime = "endTime";

            // Parameters
            public string Attributes = "attributes";

            //
            // Constructor
            //
            public UVStrategiesTableInfo()
            {
                TableNameFull = "uv.strategies";
            }

        }
        #endregion // strategies table

        #region UV Strategy Engines
        public class UVStrategyEnginesTableInfo : TableInfo
        {
            // Strategy Identification.
            public string RowId = "Id";                     // this is the unique table id.  Which we tend to ignore.
            public string StrategyId = "strategyId";
            public string EngineId = "engineId";
            public string ParentEngineId = "parentEngineId";
            // object details
            public string EngineType = "engineType";
            public string AttributeString = "attributes";

            //
            // Constructor
            //
            public UVStrategyEnginesTableInfo()
            {
                TableNameFull = "uv.strategyEngines";
            }

        }
        #endregion // strategies table

        #region UV Signals
        public class UVSignalsTableInfo : TableInfo
        {
            // Strategy Identification.
            public string StrategyId = "strategyId";
            public string EngineId = "engineId";
            public string UserName = "userName";
            public string RunType = "runType";

            // signal data
            public string TimeStamp = "timeStamp";
            public string Qty = "qty";
            public string Price = "price";
            public string Side = "side";

            // Parameters
            public string AttributeString = "attributes";

            //
            // Constructor
            //
            public UVSignalsTableInfo()
            {
                TableNameFull = "uv.signals";
            }

        }
        #endregion // strategies table

        #region UV Fills
        public class UVFillsTableInfo : TableInfo
        {
            // Strategy Identification.
            public string StrategyId = "strategyId";
            public string InstrumentId = "instrumentId";
            public string UserName = "userName";
            public string RunType = "runType";
            public string PricingEngineName = "pricingEngineName";

            // signal data
            public string TimeStamp = "timeStamp";
            public string UnixTime = "unixT";
            public string UnixMicroSecs = "unixu";
            public string Qty = "qty";
            public string Price = "price";

            // Parameters
            public string AttributeString = "attributes";

            //
            // Constructor
            //
            public UVFillsTableInfo()
            {
                TableNameFull = "uv.fills";
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
        public class EconomicDataTableInfo : TableInfo
        {
            public string EventId = "id";
            public string TickerId = "tickerId";
            public string EventName = "eventName";
            public string ShortName = "shortName";
            public string Date = "dateStr";				// Event date format: "20120601"
            public string TimeStamp = "ts";		        // mysql date/time format: "2012-06-01 07:30:00"
            public string UnixTime = "unixT";

            public string SurveyMedian = "surveyMedian";
            public string SurveyLow = "surveyLow";
            public string SurveyHigh = "surveyHigh";
            public string SurveyLast = "surveyLastPrice";

            // Constructor
            public EconomicDataTableInfo()
            {
                TableNameFull = "bbg.economicData";
            }
        }
        #endregion// bloomberg table

        #region Bloomberg Product Tickers Events
        public class EconomicTickersTableInfo : TableInfo
        {
            public string ProdFamilyId = "prodFamilyId";
            public string TickerId = "tickerId";

            // Constructor
            public EconomicTickersTableInfo()
            {
                TableNameFull = "bbg.productTickers";
            }
        }
        #endregion// product tickers

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
