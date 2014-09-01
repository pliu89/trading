using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.DatabaseReaderWriters.Queries
{
    using UV.Lib.Application;

    /// <summary>
    /// Query for writing reading to the Signals table.
    /// </summary>
    public class SignalQuery : QueryBase
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        // Query Variables
        //
        // Writing multiple signals.  User adds to this list of items.
        private List<SignalQueryItem> m_ItemsToWrite = new List<SignalQueryItem>();


        //
        // Results
        //

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public SignalQuery() : base()
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
        public void AddItemToWrite(int strategyId, int engineId, DateTime timeStamp, UserInfo userInfo, string attributeString, int side=-1, int qty=0, double price=0)
        {
            SignalQueryItem newItem = new SignalQueryItem();
            newItem.StrategyId = strategyId;
            newItem.EngineId = engineId;

            newItem.RunType = userInfo.RunType.ToString().ToLower();
            newItem.UserName = userInfo.UserName;

            newItem.TimeStamp = timeStamp;

            newItem.Side = side;
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
        // *************************************************
        // ****             AcceptData()                ****
        // *************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dbInfo"></param>
        /// <param name="values"></param>
        /// <param name="fieldNames"></param>
        /// <returns></returns>
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
        private string GetWriteQuery(DatabaseInfo dataBase)
        {
            TableInfo.UVSignalsTableInfo table = dataBase.UVSignals;
            // Start of query
            StringBuilder query = new StringBuilder();
            query.AppendFormat("INSERT INTO {0}", table.TableNameFull);

            //
            // Fields to write
            //
            // Id
            query.AppendFormat(" ({0}",table.StrategyId);
            query.AppendFormat(",{0}", table.EngineId);
            query.AppendFormat(",{0}", table.UserName);
            query.AppendFormat(",{0}", table.RunType);
            // time stamp
            query.AppendFormat(",{0}", table.TimeStamp);
            // data fields
            query.AppendFormat(",{0}", table.Side);
            query.AppendFormat(",{0}", table.Qty);
            query.AppendFormat(",{0}", table.Price);
            query.AppendFormat(",{0}", table.AttributeString);

            query.Append(")");

            //
            // Values to write
            //
            query.AppendFormat(" VALUES ");            
            SignalQueryItem item = this.m_ItemsToWrite[0];
            WriteValues(item, ref query);
            for (int i=1; i<m_ItemsToWrite.Count; ++i)
            {
                query.Append(", ");
                item = this.m_ItemsToWrite[i];
                WriteValues(item, ref query);
            }
            query.Append(";");
            return query.ToString();
        }// GetWriteQuery()
        //
        //
        private void WriteValues(SignalQueryItem item, ref StringBuilder query)
        {
            query.AppendFormat("({0},{1}", item.StrategyId, item.EngineId);
            query.AppendFormat(",\'{0}\',\'{1}\'", item.UserName, item.RunType);
            // time stamp
            query.AppendFormat(",\'{0:yyyy-MM-dd HH:mm:ss}\'", item.TimeStamp);
            // data
            query.AppendFormat(",{0}", item.Side);
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



    #region Signal Query Item
    //
    //
    public class SignalQueryItem
    {
        // Singal ID
        public int StrategyId = -1;
        public int EngineId = 0;

        public string UserName = string.Empty;
        public string RunType = string.Empty;       // TODO: Create a run type enum!!

        // Start/end times.
        public DateTime TimeStamp = DateTime.MinValue;
        public int Qty = 0;
        public double Price = 0;
        public int Side = -1;                       // -1 means No side.

        // 
        public string AttributeString = string.Empty;

    }//end class
    //
    #endregion // StrategyQueryItem



}
