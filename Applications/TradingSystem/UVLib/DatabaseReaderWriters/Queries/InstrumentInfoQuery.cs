using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.DatabaseReaderWriters.Queries
{
    using UV.Lib.Products;

    /// <summary>
    /// Notes:
    ///     1) Currently this only pulls rows base on instrument names that are TT-like.
    ///         To improve this, simpy have a flag that allows us to use Reuters names, TT names or whatever.
    ///         Alternatively, this object can call multiple InstrumentDetails, one for each TT, Reuters, etc.
    /// </summary>
    public class InstrumentInfoQuery : QueryBase
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        // Query arguments
        public InstrumentName InstrumentName ;                   // 
        public bool IsUseInstrumentFromTT = true;

        //
        // Data returned
        //
        public List<InstrumentInfoItem> Results = new List<InstrumentInfoItem>();


        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        //       
        #endregion//Constructors


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        public override string GetQuery(DatabaseInfo dataBase)
        {
            TableInfo.InstrumentsTableInfo instrumentTable = dataBase.Instruments;
            TableInfo.ExchangesTableInfo exchangeTable = dataBase.Exchanges;


            // TODO: Create the fields we want
            string desiredFields = "*";//string.Format("{0}", instrumentTable.InstrumentID );   

            // Create the query.
            StringBuilder query = new StringBuilder();
            query.AppendFormat("SELECT {0} FROM {1} ", desiredFields, dataBase.Instruments.TableNameFull);
            if (string.IsNullOrEmpty(this.InstrumentName.SeriesName) == false)
            {   // User has supplied a specific InstrumentName
                // Create a instrument expiry code.
                string expiryCode;
                if (!UV.Lib.Utilities.QTMath.TryConvertMonthYearToCodeY(this.InstrumentName.SeriesName, out expiryCode))
                {
                    return string.Empty;
                }
                // NOTE : Currently this has to be future product type! //
                query.AppendFormat("WHERE {0} in (select {1} from {2} where {3} =\'{4}\') and {5} =\'{6}\' and {7} =\'{8}\' and {9} = \'{10}\'",
                                    instrumentTable.ExchangeID,     // 0
                                    instrumentTable.ExchangeID,     // 1
                                    exchangeTable.TableNameFull,    // 2
                                    exchangeTable.ExchangeNameTT,     // 3
                                    InstrumentName.Product.Exchange,    // 4
                                    instrumentTable.Product,        // 5
                                    InstrumentName.Product.ProductName, // 6
                                    instrumentTable.ExpirySymbol,   // 7
                                    expiryCode,                    // 8   
                                    instrumentTable.ProdType,       //9
                                    InstrumentName.Product.Type.ToString().ToLower());       // 10
                                    
            }
            else
            {   // Sometimes, the instrument name does not contain series name. This means the user wants to get all the instruments for that product.
                query.AppendFormat("WHERE {0} in (select {1} from {2} where {3} =\'{4}\') and {5} =\'{6}\'",
                                    instrumentTable.ExchangeID,                              // 0
                                    instrumentTable.ExchangeID,                              // 1
                                    exchangeTable.TableNameFull,                             // 2
                                    exchangeTable.ExchangeNameTT,                            // 3
                                    InstrumentName.Product.Exchange,                         // 4
                                    instrumentTable.Product,                                 // 5
                                    InstrumentName.Product.ProductName);                     // 6
            }
            query.Append(";");
            return query.ToString();
        }// GetQuery();
        //
        //
        //
        //
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dbInfo"></param>
        /// <param name="values"></param>
        /// <param name="fieldNames"></param>
        public override QueryStatus AcceptData(DatabaseInfo dbInfo, List<object> values, List<string> fieldNames)
        {
            TableInfo.InstrumentsTableInfo table = dbInfo.Instruments;

            // dummy vars
            //int n = 0;
            ProductTypes type;
            InstrumentInfoItem item = null;
            //List<InstrumentInfoItem> itemList = new List<InstrumentInfoItem>();
            DateTime lastTradeDate = DateTime.MaxValue;

            //
            // Extract data
            //
            int ptr = 0;
            while (ptr < values.Count)
            {
                int fieldPtr = ptr % fieldNames.Count;
                if (fieldPtr == 0)
                {   // We are starting to load a new object.
                    item = new InstrumentInfoItem();                        // now create the next item to write to.                    
                    this.Results.Add(item);                                     // save it into our list.
                }
                //
                // Fill elements of item.
                //
                if (values[ptr] != null)
                {
                    string key = fieldNames[fieldPtr];

                    // Identifiers
                    if (key == table.InstrumentID)
                        item.InstrumentId = (int)values[ptr];
                    else if (key == table.ExchangeID)
                        item.ExchangeId = (int)values[ptr];
                    else if (key == table.ProdFamilyID)
                        item.ProdFamilyId = (int)values[ptr];
                    else if (key == table.ExpirySymbol)
                        item.Expiry = values[ptr].ToString();
                    else if (key == table.Product)
                        item.ProductName = values[ptr].ToString();
                    else if (key == table.ProdType)
                    {
                        if (Enum.TryParse<ProductTypes>(values[ptr].ToString(), true, out type))
                            item.Type = type;
                        else if (values[ptr].ToString().ToUpper().Contains("SPREAD"))
                            item.Type = ProductTypes.Spread;
                    }
                    else if (key == table.SpreadComposition)
                        item.SpreadComposition = values[ptr].ToString();
                    // specs
                    else if (key == table.LastTradeDate && DateTime.TryParse(values[ptr].ToString(), out lastTradeDate))
                        item.LastTradeDate = lastTradeDate;
                    else if (key == table.Currency)
                        item.Currency = values[ptr].ToString();
                    else if (key == table.unitTT)
                        item.UnitValue = (double)values[ptr];
                    else if (key == table.tickTT)
                        item.TickValue = (double)values[ptr];
                    else if (key == table.calendarTickTT)
                        item.CalendarTickValue = (double)values[ptr];
                    else if (key == table.HedgeOptions)
                        item.HedgeOptions = values[ptr].ToString();
                    else if (key == table.InstrumentName)
                        item.InstrumentNameDatabase = values[ptr].ToString();
                    else if (key == table.InstrumentNameTT)
                        item.InstrumentNameTT = values[ptr].ToString();
                }

                ptr++;
            }//wend ptr            
            // Exit;            
            return QueryStatus.Completed;
        }//
        //
        //
        // *************************************
        // ****         ToString()          ****
        // *************************************
        public override string ToString()
        {
            return string.Format("{0} Count={1}", base.ToString(), this.Results.Count);
        }
        //
        //
        //
        //
        #endregion // public methods

    }//InstrumentInfoQuery class


    #region Instrument Info Item
    // *****************************************************************
    // ****                Instrument Info Item                     ****
    // *****************************************************************
    //
    public class InstrumentInfoItem
    {
        //
        // Product instrument identifiers
        //
        public int InstrumentId = -1;        
        public int ProductId = -1;
        public int ExchangeId = -1;
        public int ProdFamilyId = -1;
        public string Expiry = string.Empty;         // Two digit code H3, for example
        public string ProductName = string.Empty;
        public string SpreadComposition = string.Empty;     // if this is a spread, this will contain information about the leg
        public string InstrumentNameDatabase;
        public string InstrumentNameTT;
        // 
        //public string Description = string.Empty;
        public ProductTypes Type = ProductTypes.Unknown;
        public string Currency = "USD";
        public DateTime LastTradeDate = DateTime.MaxValue;
        //public string TimeZone = "America/Chicago";

        // Tick size info
        public double UnitValue = 1.0;
        public double TickValue = 1.0;
        public double CalendarTickValue = 1.0;

        // Sessions
        public TimeSpan SessionElectronicBeginTime;
        public TimeSpan SessionElectronicEndTime;
        public TimeSpan SessionCashBeginTime;
        public TimeSpan SessionCashEndTime;
        public TimeSpan SessionFloorBeginTime;
        public TimeSpan SessionFloorEndTime;

        public string HedgeOptions;
        

        #region GetInstrumentDetails
        //
        //
        //
        public bool TryGetInstrumentDetails(InstrumentName instrumentName, out InstrumentDetails details)
        {
            double tickSize = this.TickValue / this.UnitValue;
            double calTickSize = this.CalendarTickValue / this.UnitValue;
            details = new InstrumentDetails(instrumentName, this.Currency, tickSize, calTickSize, this.UnitValue, this.LastTradeDate, this.Type);
            return true;
        }//TryGetInstrumentDetails()
        //
        //
        //
        #endregion//GetInstrumentDetails



    }//end class
    //
    //
    #endregion // Instrument Info Item




}
