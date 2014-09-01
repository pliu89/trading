using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.DatabaseReaderWriters.Queries
{
    using UV.Lib.Products;
    using UV.Lib.Utilities;

    public class MarketDataQuery : QueryBase
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
        public List<MarketDataItem> Result = null;


        //
        // Instrument information query
        //
        //private Instruments m_InstrumentQuery = null;
        private InstrumentInfoQuery m_InstrumentQuery = null;

        //
        // Flags for order
        //

        private bool m_IsReverseOrder = false;
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
                query = GetQueryForInstrument(dataBase);
            else
                query = GetQueryForData(dataBase);
            return query.ToString();
        }
        //
        protected string GetQueryForInstrument(DatabaseInfo dataBase)
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
        protected string GetQueryForData(DatabaseInfo dataBase)
        {
            TableInfo.InstrumentsTableInfo instrumentTable = dataBase.Instruments;
            TableInfo.ExchangesTableInfo exchangeTable = dataBase.Exchanges;
            TableInfo.BarsTableInfo barsTable = dataBase.Bars;


            // Create a instrument expiry code.
            string expiryCode;
            if (!UV.Lib.Utilities.QTMath.TryConvertMonthYearToCodeY(InstrumentName.SeriesName, out expiryCode))
            {
                return string.Empty;
            }
            // Create sub selection string to get instr ID from InstrumentName.
            StringBuilder subQuery = new StringBuilder();
            int instrSqlId = m_InstrumentQuery.Results[0].InstrumentId;
            subQuery.AppendFormat("{0}", instrSqlId);
            // Create the final query.
            string desiredFields = "*";
            StringBuilder query = new StringBuilder();
            query.AppendFormat("SELECT {0} FROM {1}", desiredFields, barsTable.TableNameFull);
            query.AppendFormat(" WHERE {0} = ({1})", barsTable.InstrumentID, subQuery);
            if (this.StartDate > DateTime.MinValue)
            {
                int timestamp = (int)QTMath.DateTimeToEpoch(this.StartDate);
                string s = timestamp.ToString();
                query.AppendFormat(" AND {0} >= {1}", barsTable.TimeStamp, s);
            }
            if (this.EndDate < DateTime.MaxValue)
            {
                int timestamp = (int) QTMath.DateTimeToEpoch(this.EndDate);
                string s = timestamp.ToString();
                query.AppendFormat(" AND {0} <= {1}", barsTable.TimeStamp, s);
            }
            query.AppendFormat(" AND {0} = 1", barsTable.SessionCode);  // currently we only want sessionCode = 1 for when products are trading
            query.AppendFormat(" ORDER BY {0}", barsTable.TimeStamp);
            
            if(this.StartDate == DateTime.MinValue)
            { // since our date time is unnasigned, lets reverse the order of our timestamps for this query so we get the proper number of rows
                query.AppendFormat(" DESC");
                m_IsReverseOrder = true;
            }

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
            
            if (m_InstrumentQuery.Results == null || m_InstrumentQuery.Results.Count == 0 )
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
                // Hack.
                // Convert this data to the TT scale.
                // This is a hack in that I know that we are connected to TT, so its appropriate.
                // In future, the inverse of this multiplier should be added to the TTMarketHub instead.
                double multiplier = 1.0;// m_InstrumentQuery.unit / m_InstrumentQuery.unit_TT;


                // Setup
                const int askSide = MarketDataItem.AskSide;
                const int bidSide = MarketDataItem.BidSide;
                const int lastSide = MarketDataItem.LastSide;

                if (this.Result == null)
                {
                    this.Result = new List<MarketDataItem>();
                }

                // TODO: We should load "all" of the data in the table here.
                int ptr = 0;
                while (ptr < values.Count)
                {
                    MarketDataItem item = new MarketDataItem();
                    // Skip id# 
                    ptr++;
                    // Extract time stamp!
                    double unixTime = Convert.ToDouble(values[ptr]); ptr++;
                    item.UnixTime = (UInt32)unixTime;
                    //DateTime dt = UV.Lib.Utilities.QTMath.EpochToDateTime(unixTime);
                    //item.TimeStamp = dt;

                    bool isGood = true;

                    // extract all fields
                    item.Price[bidSide] = multiplier * Convert.ToDouble(values[ptr]); ptr++;
                    item.Qty[bidSide] = Convert.ToInt32(values[ptr]); ptr++;
                    if (item.Qty[bidSide] < 1)
                        isGood = false;
                    item.Price[askSide] = multiplier * Convert.ToDouble(values[ptr]); ptr++;
                    item.Qty[askSide] = Convert.ToInt32(values[ptr]); ptr++;
                    if (item.Qty[askSide] < 1)
                        isGood = false;

                    item.Price[lastSide] = multiplier * Convert.ToDouble(values[ptr]); ptr++;
                    
                    //
                    // Volume and Session Codes
                    //
                    item.SessionVolume = Convert.ToInt32(values[ptr]); ptr++;              
                    item.LongVolume = Convert.ToInt32(values[ptr]); ptr++;
                    item.ShortVolume = Convert.ToInt32(values[ptr]); ptr++;
                    item.TotalVolume = Convert.ToInt32(values[ptr]); ptr++;
                    item.SessionCode = Convert.ToInt32(values[ptr]); ptr++;

                    // Store it
                    if (isGood)
                        this.Result.Add(item);
                }//wend
                if(m_IsReverseOrder)        // we requested a descending time order
                    this.Result.Reverse();  // we want an ascending time order.

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
