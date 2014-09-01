using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.DatabaseReaderWriters.Queries
{

    using UV.Lib.Utilities;

    /// <summary>
    /// Query for writing reading to the Fills table.
    /// </summary>
    public class FillsQuery : QueryBase
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        // Query Variables
        //
        // Writing multiple signals.  User adds to this list of items.
        private List<FillsQueryItem> m_ItemsToWrite = new List<FillsQueryItem>();


        //
        // Results
        //

        #endregion// members

        #region Public Properties
        // *************************************
        // ****             Count           **** 
        // *************************************
        public int Count
        {
            get { return m_ItemsToWrite.Count;  }
        }
           
        #endregion// Properties


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public FillsQuery()
            : base()
        {
            base.IsRead = false;                // default behavior is that this is a write
        }
        //
        //
        //
        //       
        #endregion//Constructors


        #region Public Methods
        // *************************************
        // ****     AddItemToWrite()        **** 
        // *************************************
        /// <summary>
        /// User calls this function to add a new signal item for later writing.
        /// TODO: 
        ///     1) this is written like this so in future, we can recycle signal items
        ///         without the user having access to them.
        /// </summary>
        public void AddItemToWrite(int strategyId, int instrId, DateTime localTime, UV.Lib.Application.UserInfo user, string pricingEngineName, string attributeString, int qty = 0, double price = 0)
        {
            FillsQueryItem newItem = new FillsQueryItem();
            newItem.StrategyId = strategyId;
            newItem.InstrumentId = instrId;
            newItem.RunType = user.RunType.ToString().ToLower();
            newItem.UserName = user.UserName;
            newItem.PricingEngineName = pricingEngineName;

            newItem.TimeStamp = localTime;
            double utc = QTMath.DateTimeToEpoch(localTime.ToUniversalTime());
            newItem.UnixUTC = (int)Math.Floor(utc);
            newItem.UnixMicroSec = (int)((utc - newItem.UnixUTC) * 1000000);

            newItem.Qty = qty;
            newItem.Price = price;

            newItem.AttributeString = attributeString;

            this.m_ItemsToWrite.Add(newItem);
        }//AddItemToWrite()
        //
        //
        // *************************************
        // ****            GetQuery         **** 
        // *************************************
        public override string GetQuery(DatabaseInfo dataBase)
        {
            if (base.IsRead)
                return GetReadQuery(dataBase);
            else
                return GetWriteQuery(dataBase);
        }
        //
        //
        // *************************************
        // ****       GetWriteQueryPrefix   **** 
        // *************************************
        public override string GetWriteQueryPrefix(DatabaseInfo dataBase)
        {
            TableInfo.UVFillsTableInfo table = dataBase.UVFills;
            // Start of query
            StringBuilder query = new StringBuilder();
            query.AppendFormat("INSERT INTO {0}", table.TableNameFull);

            //
            // Fields to write
            //
            // Id
            query.AppendFormat(" ({0}", table.StrategyId);
            query.AppendFormat(",{0}", table.InstrumentId);
            query.AppendFormat(",{0}", table.UserName);
            query.AppendFormat(",{0}", table.RunType);
            query.AppendFormat(",{0}", table.PricingEngineName);
            // time stamp
            query.AppendFormat(",{0}", table.TimeStamp);
            query.AppendFormat(",{0}", table.UnixTime);
            query.AppendFormat(",{0}", table.UnixMicroSecs);
            // data fields            
            query.AppendFormat(",{0}", table.Qty);
            query.AppendFormat(",{0}", table.Price);
            query.AppendFormat(",{0}", table.AttributeString);

            query.Append(") VALUES");
            return query.ToString();
    
        }
        //
        //
        // *************************************
        // ****       GetWriteQuerySuffix   **** 
        // *************************************
        public override string GetWriteQuerySuffix(DatabaseInfo dataBase)
        {
            TableInfo.UVFillsTableInfo table = dataBase.UVFills;
            // Start of query
            StringBuilder query = new StringBuilder();
            //
            // Values to write
            //
            if (m_ItemsToWrite.Count == 0)
                return string.Empty;
            FillsQueryItem item = this.m_ItemsToWrite[0];
            WriteValues(item, ref query);
            for (int i = 1; i < m_ItemsToWrite.Count; ++i)
            {
                query.Append(", ");
                item = this.m_ItemsToWrite[i];
                WriteValues(item, ref query);
            }
            return query.ToString();
        }
        //
        //
        //
        // *************************************************
        // ****             AcceptData()                ****
        // *************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dbInfo"></param>
        /// <param name="values"></param>
        /// <param name="fieldNames"></param>
        public override QueryStatus AcceptData(DatabaseInfo dbInfo, List<object> values, List<string> fieldNames)
        {
            TableInfo.UVStrategiesTableInfo strategyTable = dbInfo.UVStrategies;
            /*
            if (this.Results == null)
                this.Results = new List<StrategyQueryItem>();
            else
                this.Results.Clear();

            // Extract  values.
            int ptr = 0;                                // ptr to current data value
            int fptr = 0;                               // ptr to associated field
            int n;                                      // dummy variables..
            DateTime dt;
            TimeSpan ts;
            StrategyQueryItem item = new StrategyQueryItem();   // place to store first strategy row
            while (ptr < values.Count)
            {
                object o = values[ptr];                 // load the value
                if (o != null)
                {
                    // Extract data
                    string s = o.ToString();
                    string fieldName = fieldNames[fptr];
                    if (fieldName.Equals(strategyTable.StrategyId) && int.TryParse(s, out n))
                        item.StrategyId = n;
                    else if (fieldName.Equals(strategyTable.GroupID) && int.TryParse(s, out n))
                        item.GroupId = n;
                    else if (fieldName.Equals(strategyTable.StrategyName))
                        item.Name = s;
                    else if (fieldName.Equals(strategyTable.RunType))
                        item.RunType = s;
                    //
                    // start/end times
                    //
                    else if (fieldName.Equals(strategyTable.StartDate) && DateTime.TryParse(s, out dt))
                        item.StartDate = dt;
                    else if (fieldName.Equals(strategyTable.StartTime) && TimeSpan.TryParse(s, out ts))
                        item.StartTime = ts;
                    else if (fieldName.Equals(strategyTable.EndDate) && DateTime.TryParse(s, out dt))
                        item.EndDate = dt;
                    else if (fieldName.Equals(strategyTable.EndTime) && TimeSpan.TryParse(s, out ts))
                        item.EndTime = ts;
                    //
                    // Parameters
                    //
                    else if (fieldName.Equals(strategyTable.Attributes))
                        item.AttributeString = s;

                }
                // Increment pointers
                ptr++;
                fptr = (fptr + 1) % fieldNames.Count;
                if (fptr == 0)                          // We have read all fields.. we have completed this row.
                {
                    this.Results.Add(item);             // save this current row, 
                    item = new StrategyQueryItem();       // create a new object to hold the next row.
                }
            }//next value
            */
            // Exit;            
            return QueryStatus.Completed;
        }//
        //
        //
        // *************************************
        // ****         ToString()          ****
        // *************************************
        //
        //
        //
        //
        #endregion//Public Methods


        #region Private Methods
        // *************************************
        // ****        GetWriteQuery        **** 
        // *************************************
        /// <summary>
        /// Caller would like to get a fully formed query to insert into database.
        /// </summary>
        /// <param name="dataBase"></param>
        /// <returns></returns>
        private string GetWriteQuery(DatabaseInfo dataBase)
        {
            TableInfo.UVFillsTableInfo table = dataBase.UVFills;
            // Start of query
            StringBuilder query = new StringBuilder();
            query.Append(GetWriteQueryPrefix(dataBase));
            query.AppendFormat("{0} {1};", GetWriteQueryPrefix(dataBase), GetWriteQuerySuffix(dataBase));
            return query.ToString();
        }// GetWriteQuery()
        //
        //
        //
        //
        //
        
        //
        //
        private void WriteValues(FillsQueryItem item, ref StringBuilder query)
        {
            query.AppendFormat("({0},{1}", item.StrategyId, item.InstrumentId);
            query.AppendFormat(",\'{0}\',\'{1}\'", item.UserName, item.RunType);
            query.AppendFormat(",\'{0}\'", item.PricingEngineName);
            // time stamp
            query.AppendFormat(",\'{0:yyyy-MM-dd HH:mm:ss}\'", item.TimeStamp);
            query.AppendFormat(",{0}", item.UnixUTC);
            query.AppendFormat(",{0}", item.UnixMicroSec);
            // data
            query.AppendFormat(",{0},{1}", item.Qty, item.Price);
            query.AppendFormat(",\'{0}\'", item.AttributeString);
            query.Append(")");
        }
        //
        //
        // *************************************
        // ****         GetReadQuery        **** 
        // *************************************
        private string GetReadQuery(DatabaseInfo dataBase)
        {
            TableInfo.UVStrategiesTableInfo strategyTable = dataBase.UVStrategies;
            // Start of query
            StringBuilder query = new StringBuilder();
            /*
            query.AppendFormat("SELECT * FROM {0}", strategyTable.TableNameFull);

            // Conditions
            StringBuilder cond = new StringBuilder();
            if (m_GroupId != null)
            {
                if (m_GroupId.Count == 1)
                {
                    if (cond.Length > 0)
                        cond.Append(" AND");
                    cond.AppendFormat(" {0}={1}", strategyTable.GroupID, m_GroupId[0]);
                }
                else if (m_GroupId.Count > 1)
                {
                    if (cond.Length > 0)
                        cond.Append(" AND");
                    cond.AppendFormat(" {0} in ({1}", strategyTable.GroupID, m_GroupId[0]);
                    for (int n = 1; n < m_GroupId.Count; ++n)
                        cond.AppendFormat(",{0}", m_GroupId[n]);
                    cond.Append(")");
                }
            }

            // Now append them together.
            if (cond.Length > 0)
                query.AppendFormat(" WHERE{0}", cond.ToString());
            query.Append(";");
            */
            return query.ToString();
        }// GetReadQuery()
        //
        //
        //
        #endregion // private methods


    }//end class



    #region Fills Query Item
    //
    //
    public class FillsQueryItem
    {
        // Singal ID
        public int StrategyId = -1;
        public int InstrumentId = -1;               // default -1 means (not a leg fill, but rather) a synthetic fill.

        public string UserName = string.Empty;
        public string PricingEngineName = string.Empty;
        public string RunType = string.Empty;       // TODO: Create a run type enum!!

        // Start/end times.
        public DateTime TimeStamp = DateTime.MinValue;
        public int UnixUTC = 0;
        public int UnixMicroSec = 0;
        public int Qty = 0;
        public double Price = 0;

        // 
        public string AttributeString = string.Empty;

    }//end class
    //
    #endregion // StrategyQueryItem



}
