using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.DatabaseReaderWriters.Queries
{

    /// <summary>
    /// This class is a query object that will load the entire ExchangeInfo table
    /// into a list of ExchangeInfoItems (one for each row entry in table).
    /// </summary>
    public class ExchangeInfoQuery : QueryBase
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //  
        public List<ExchangeInfoItem> Results = null;


        #endregion// members


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //
        public override string GetQuery(DatabaseInfo databaseInfo)
        {
            TableInfo.ExchangesTableInfo exchangeTable = databaseInfo.Exchanges;            
            string desiredFields = "*";

            // Create the query.
            StringBuilder query = new StringBuilder();
            query.AppendFormat("SELECT {0} FROM {1} ", desiredFields, exchangeTable.TableNameFull);
            query.Append(";");
            return query.ToString();
        }
        //
        //
        //
        public override QueryStatus AcceptData(DatabaseInfo dbInfo, List<object> values, List<string> fieldNames)
        {            
            // Load all exchange rows.
            int nColumns = fieldNames.Count;
            if (Results == null)
                Results = new List<ExchangeInfoItem>();
            else
                Results.Clear();

            int ptr = 0;
            int fptr = 0;
            int n;
            ExchangeInfoItem item = new ExchangeInfoItem();
            while (ptr < values.Count)
            {
                string s = values[ptr].ToString();
                string fieldName = fieldNames[fptr];
                if (fieldName.Equals(dbInfo.Exchanges.ExchangeID) && int.TryParse(s, out n))
                    item.ExchID = n;
                else if (fieldName.Equals(dbInfo.Exchanges.ExchangeName))
                    item.ExchName = s;
                else if (fieldName.Equals(dbInfo.Exchanges.SecondaryExchange))
                    item.ExchName2 = s;
                else if (fieldName.Equals(dbInfo.Exchanges.ExchangeNameTT))
                    item.ExchNameTT = s;
                else if (fieldName.Equals(dbInfo.Exchanges.ExchangeTimeZone))
                {   // 
                    TimeZoneInfo tz = UV.Lib.Utilities.QTMath.OlsonTimeZoneToTimeZoneInfo(s );
                    item.TimeZoneInfo = tz;
                }

                // Increment pointers
                ptr++;
                fptr = (fptr + 1) % nColumns;
                if ( fptr == 0)
                {
                    Results.Add(item);
                    item = new ExchangeInfoItem();
                }
            }//next value

            // Exit
            return QueryStatus.Completed;            
        }// AcceptData()
        //
        //
        //
        //
        //
        #endregion//Public Methods


    }//end class


    #region Exchange Item Class
    // *****************************************************************
    // ****                     Exchange Item                       ****
    // *****************************************************************
    //
    /// <summary>
    /// Small object to hold a row from the ExhangeInfo
    /// </summary>
    public class ExchangeInfoItem
    {
        public int ExchID = -1;
        public string ExchName = string.Empty;
        public string ExchName2 = string.Empty;
        public string ExchNameTT = string.Empty;

        public TimeZoneInfo TimeZoneInfo = null;
    }
    //
    //
    #endregion//Event Handlers

}
