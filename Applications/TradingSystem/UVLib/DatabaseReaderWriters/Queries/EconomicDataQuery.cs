using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.DatabaseReaderWriters.Queries
{
    using UV.Lib.Products;
    using UV.Lib.Utilities;

    public class EconomicDataQuery : QueryBase
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        // Query arguments:
        public InstrumentName InstrumentName;           // desired instrument.
        public DateTime StartDate = DateTime.MinValue;
        public DateTime EndDate = DateTime.MaxValue;

        public int MaxRows = -1;                        // -1 = "do not limit row count".

        //
        // Results:
        //
        public List<EconomicDataItem> Result = null;


        //
        // Instrument information query
        //
        //private Instruments m_InstrumentQuery = null;
        private InstrumentInfoQuery m_InstrumentQuery = null;

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
        //
        //
        //
        // *********************************************
        // ****             Get Query()             ****
        // *********************************************
        public override string GetQuery(DatabaseInfo dataBase)
        {
            string query;
            if (m_InstrumentQuery == null)
                query = GetQueryForInstrumentTickers(dataBase);
            else
                query = GetQueryForEconomicTickers(dataBase);
            return query.ToString();
        }
        //
        protected string GetQueryForInstrumentTickers(DatabaseInfo dataBase)
        {
            // we make this query object so that the instrument details
            // query we want to make is already written in this object, and
            // there is no need to write it twice.
            m_InstrumentQuery = new InstrumentInfoQuery();
            m_InstrumentQuery.InstrumentName = InstrumentName;
            m_InstrumentQuery.IsRead = true;
            m_InstrumentQuery.Status = QueryStatus.New;
            return m_InstrumentQuery.GetQuery(dataBase);
        }
        //
        protected string GetQueryForEconomicTickers(DatabaseInfo dataBase)
        {
            TableInfo.EconomicDataTableInfo economicTable = dataBase.EconomicEvents;
            TableInfo.EconomicTickersTableInfo economicTickers = dataBase.EconomicTickers;
            // Create a instrument expiry code.
            string expiryCode;
            if (!UV.Lib.Utilities.QTMath.TryConvertMonthYearToCodeY(InstrumentName.SeriesName, out expiryCode))
            {
                return string.Empty;
            }
            // Create sub selection string to get instr ID from InstrumentName.
            StringBuilder subQuery = new StringBuilder();
            int prodFamilyId = m_InstrumentQuery.Results[0].ProdFamilyId;
            subQuery.AppendFormat("{0}", prodFamilyId);
            // Create the final query.
            //select * from bbg.economicData where unixT>=%d and unixT<=%d and tickerId in (select tickerId from bbg.productTickers where prodFamilyId in (-1,33)) order by unixT asc
            string desiredFields = "*";
            StringBuilder query = new StringBuilder();
            query.AppendFormat("SELECT {0} FROM {1}", desiredFields, economicTable.TableNameFull);
            if (this.StartDate > DateTime.MinValue)
            {
                int timestamp = (int)QTMath.DateTimeToEpoch(this.StartDate);
                string s = timestamp.ToString();
                query.AppendFormat(" WHERE {0} >= {1}", economicTable.UnixTime, s);
            }
            if (this.EndDate < DateTime.MaxValue)
            {
                int timestamp = (int) QTMath.DateTimeToEpoch(this.EndDate);
                string s = timestamp.ToString();
                query.AppendFormat(" AND {0} <= {1}", economicTable.UnixTime, s);
            }

            query.AppendFormat(" AND {0} IN", economicTable.TickerId);
            query.AppendFormat(" (SELECT {0} FROM {1} where {2}", economicTable.TickerId, economicTickers.TableNameFull, economicTickers.ProdFamilyId);
            query.AppendFormat(" IN (-1,{0})) ORDER BY {1} ASC", prodFamilyId, economicTable.UnixTime);

            if ( this.MaxRows > 0)
                query.AppendFormat(" LIMIT {0}",this.MaxRows);
            query.Append(";");
            // Exit
            return query.ToString();
        }// GetQuery();
        //
        //
        // *************************************************
        // ****             AcceptData()                ****
        // *************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="database"></param>
        /// <param name="values"></param>
        /// <param name="fieldNames"></param>
        public override QueryStatus AcceptData(DatabaseInfo database, List<object> values, List<string> fieldNames)
        {
            if (m_InstrumentQuery.Results == null || m_InstrumentQuery.Results.Count == 0)
            {   // First, we need to collect market instrument details to proceed.
                // This is probably the callback from that query.
                QueryStatus status;
                status = m_InstrumentQuery.AcceptData(database, values, fieldNames);
                if (status == QueryStatus.Failed)
                    return QueryStatus.Failed;
                else
                    return QueryStatus.New;
            }
            else
            {
                TableInfo.EconomicDataTableInfo table = database.EconomicEvents;
                EconomicDataItem item = null;
                if (this.Result == null)
                {
                    this.Result = new List<EconomicDataItem>();
                }
                //
                // Extract data
                //
                int ptr = 0;
                while (ptr < values.Count)
                {
                    int fieldPtr = ptr % fieldNames.Count;
                    if (fieldPtr == 0)
                    {   // We are starting to load a new object.
                        item = new EconomicDataItem();                        // now create the next item to write to.                    
                        this.Result.Add(item);                                     // save it into our list.
                    }
                    //
                    // Fill elements of item.
                    //
                    if (values[ptr] != null)
                    {
                        string key = fieldNames[fieldPtr];

                        // Identifiers
                        if (key == table.EventName)
                            item.EventName = values[ptr].ToString();
                        else if (key == table.TickerId)
                            item.TickerId = Convert.ToInt32(values[ptr]);
                        else if (key == table.UnixTime)
                        {
                            double unixTime = Convert.ToDouble(values[ptr]);
                            item.UnixTime = (UInt32)unixTime;
                        }
                        else if (key == table.ShortName)
                            item.ShortName = values[ptr].ToString();

                    }
                    ptr++;
                }//wend ptr            
                // Exit;            
                return QueryStatus.Completed;
            }
        }//AcceptData()
        //
        //
        // *************************************
        // ****         ToString()          ****
        // *************************************
        public override string ToString()
        {
            if ( this.Result != null )
            {
                int count = this.Result.Count;
                return string.Format("{0} {1} records. {2} to {3}.",base.ToString(),count,Result[0].TimeStamp,Result[count-1].TimeStamp);
            }
            else
                return string.Format("{0} empty.",base.ToString());
        }//ToString()
        //
        //
        //
        #endregion // public methods

    }
}
