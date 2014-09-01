using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.DatabaseReaderWriters.Queries
{
    using UV.Lib.Products;
    //using MySql.Data.MySqlClient;

    public class Instruments : QueryBase
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        // Query arguments
        public InstrumentName InstrumentName; 

        //
        // Data returned
        //
        public int InstrumentID = -1;
        public Dictionary<string, string> RawTable = null;

        // UV definitions
        public double unit;
        public double tick;

        // TT definitions
        public double unit_TT;
        public double tick_TT;

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

            // Create a instrument expiry code.
            string expiryCode;
            if (! UV.Lib.Utilities.QTMath.TryConvertMonthYearToCodeY(InstrumentName.SeriesName, out expiryCode))
            {
                return string.Empty;
            }

            // TODO: Create the fields we want
            string desiredFields = "*";//string.Format("{0}", instrumentTable.InstrumentID );   

            // Create the query.
            StringBuilder query = new StringBuilder();
            query.AppendFormat("SELECT {0} FROM {1} ", desiredFields, dataBase.Instruments.TableNameFull);
            query.AppendFormat("WHERE {0} in (select {1} from {2} where {3} =\'{4}\') and {5} =\'{6}\' and {7} =\'{8}\'",
                                instrumentTable.ExchangeID,     // 0
                                instrumentTable.ExchangeID,     // 1
                                exchangeTable.TableNameFull,    // 2
                                exchangeTable.ExchangeNameTT,     // 3
                                InstrumentName.Product.Exchange,    // 4
                                instrumentTable.Product,        // 5
                                InstrumentName.Product.ProductName, // 6
                                instrumentTable.ExpirySymbol,   // 7
                                expiryCode);                    // 8   
            query.Append(";");
            return query.ToString();
        }// GetQuery();
        //
        //
        //
        /// <summary>
        /// 
        /// </summary>
        /// <param name="values"></param>
        /// <param name="fieldNames"></param>
        public override QueryStatus AcceptData(DatabaseInfo database, List<object> values, List<string> fieldNames)
        {
            // First locate the instrument ID.
            int ptr = fieldNames.IndexOf( database.Instruments.InstrumentID);
            if (ptr >= 0)
                this.InstrumentID = Convert.ToInt16(values[ptr]);
            else
                return QueryStatus.Failed;

            // Store all inforamation.
            RawTable = new Dictionary<string, string>();
            for (int i = 0; i < fieldNames.Count; ++i)
            {
                if ( values[i] != null)
                    RawTable.Add(fieldNames[i], values[i].ToString());
            }
           
            //
            // Create Instrument Details objects 
            //
            ptr = fieldNames.IndexOf(database.Instruments.tickTT);
            if (ptr >= 0)
                this.tick_TT = Convert.ToDouble(values[ptr]);
            ptr = fieldNames.IndexOf(database.Instruments.unitTT);
            if (ptr >= 0)
                this.unit_TT = Convert.ToDouble(values[ptr]);


            ptr = fieldNames.IndexOf(database.Instruments.tick);
            if (ptr >= 0)
                this.tick = Convert.ToDouble(values[ptr]);
            ptr = fieldNames.IndexOf(database.Instruments.unit);
            if (ptr >= 0)
                this.unit = Convert.ToDouble(values[ptr]);


                
            return QueryStatus.Completed;            
        }//
        //
        //
        // ****         ToString()          ****
        //
        public override string ToString()
        {
            return string.Format("{0} {1} Result={2}",base.ToString(),this.InstrumentName,this.InstrumentID);
        }
        //
        #endregion // public methods


        #region no Private Methods
        // *****************************************************************
        // ****                    Private Methods                      ****
        // *****************************************************************
        //
        //
        //
        #endregion//Private Methods

    }
}
