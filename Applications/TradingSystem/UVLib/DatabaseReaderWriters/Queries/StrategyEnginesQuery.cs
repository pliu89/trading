using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.DatabaseReaderWriters.Queries
{
    /// <summary>
    /// Query for reading the Strategy Engines table.
    /// Notes:
    /// </summary>
    public class StrategyEnginesQuery : QueryBase
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        // Query Variables
        //
        private int m_StrategyId = -1;
        private List<int> m_StrategyIdList = null;


        //
        // Results
        //
        public List<StrategyEnginesQueryItem> Results = null;


        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        private StrategyEnginesQuery() { }                   // this hides the default (no parameter) constructor from user.
        /// <summary>
        /// Creates query to get all engines associated with SINGLE strategyId.
        /// </summary>
        /// <param name="strategyId"></param>
        public StrategyEnginesQuery(int strategyId)
        {
            this.m_StrategyId = strategyId;
        }
        /// <summary>
        /// Creates query to get all engines associated with LIST of strategy Id numbers.
        /// </summary>
        /// <param name="strategyIdList">List of strategy Ids.</param>
        public StrategyEnginesQuery(List<int> strategyIdList)
        {
            this.m_StrategyIdList = strategyIdList;
        }
        //
        //       
        #endregion//Constructors


        #region Public Methods
        // *************************************
        // ****            GetQuery         **** 
        // *************************************
        public override string GetQuery(DatabaseInfo dataBase)
        {
            TableInfo.UVStrategyEnginesTableInfo table = dataBase.UVStrategyEngines;
            
            // Start of query
            StringBuilder query = new StringBuilder();
            query.AppendFormat("SELECT * FROM {0}", table.TableNameFull);

            //
            // Conditions
            //
            StringBuilder cond = new StringBuilder();
            if (this.m_StrategyId >= 0)
            {
                if (cond.Length > 0)                                // Note: if this first condition, this will not be triggered.   
                    cond.Append(" AND");                            
                cond.AppendFormat(" {0}={1}", table.StrategyId, m_StrategyId);
            }
            if (this.m_StrategyIdList != null && m_StrategyIdList.Count > 0)
            {
                if (cond.Length > 0)                                // This is not first condition, so include an AND statement.
                    cond.Append(" AND");
                cond.AppendFormat(" {0} in (", table.StrategyId);   
                cond.AppendFormat("{0}", m_StrategyIdList[0]);      // add first strategyId
                for (int i = 1; i < m_StrategyIdList.Count; ++i)
                    cond.AppendFormat(", {0}", m_StrategyIdList[i]);// add , next strategyId
                cond.Append(")");
            }

            // Now append query and conditions together.
            if (cond.Length > 0)
                query.AppendFormat(" WHERE{0}", cond.ToString());
            query.Append(";");
            return query.ToString();
        }// GetQuery()
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
            TableInfo.UVStrategyEnginesTableInfo table = dbInfo.UVStrategyEngines;
            if (this.Results == null)
                this.Results = new List<StrategyEnginesQueryItem>();
            else
                this.Results.Clear();

            // Extract  values.
            int ptr = 0;                                // ptr to current data value
            int fptr = 0;                               // ptr to associated field
            int n;                                      // dummy variables..
            //DateTime dt;
            //TimeSpan ts;
            StrategyEnginesQueryItem item = new StrategyEnginesQueryItem();   // place to store first strategy row
            while (ptr < values.Count)
            {
                object o = values[ptr];                 // load the value
                if (o != null)                          // null entries are left as the default value.
                {
                    // Extract data
                    string s = o.ToString();
                    string fieldName = fieldNames[fptr];
                    // Identifiers
                    if (fieldName.Equals(table.StrategyId) && int.TryParse(s, out n))
                        item.StrategyId = n;
                    else if (fieldName.Equals(table.EngineId) && int.TryParse(s, out n))
                        item.EngineId = n;
                    else if (fieldName.Equals(table.ParentEngineId) && int.TryParse(s, out n))
                        item.ParentEngineId = n;
                    //else if (fieldName.Equals(table.RowId) && int.TryParse(s, out n))
                    //    item.Id = n;  // ignore this value.
                    // data
                    else if (fieldName.Equals(table.EngineType))
                        item.EngineType = s;
                    else if (fieldName.Equals(table.AttributeString))
                        item.AttributeString = s;

                }
                // Increment pointers
                ptr++;
                fptr = (fptr + 1) % fieldNames.Count;
                if (fptr == 0)                          // We have read all fields.. we have completed this row.
                {
                    this.Results.Add(item);             // save this current row, 
                    item = new StrategyEnginesQueryItem();       // create a new object to hold the next row.
                }
            }//next value

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
            return string.Format("{0} {1} ", base.ToString(), this.m_StrategyId);
        }// ToString()
        //
        //
        //
        //
        #endregion//Public Methods


    }//end class



    #region Strategy Engine Query Item
    //
    //
    public class StrategyEnginesQueryItem
    {
        // Identifiers
        //public int Id = 0;                      // not used by us - unique id number in table.
        public int StrategyId = -1;             // negative -1 means empty.
        public int EngineId = -1;
        public int ParentEngineId = -1;

        public string EngineType = string.Empty;
        public string AttributeString = string.Empty;


    }//end class
    //
    #endregion // StrategyEnginesQueryItem



}
