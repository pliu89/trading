using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.DatabaseReaderWriters.Queries
{
    /// <summary>
    /// Query for reading the Strategies table.
    /// Notes:
    ///     1) in future, we may want to allow different sorts of queries, by groupId or dates for example.
    ///         These can be implemented using different constructors.
    /// </summary>
    public class StrategiesQuery : QueryBase
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        // Query Variables
        //
        private List<int> m_GroupId = null;
        private List<int> m_StrategyIds = null;
        


        //
        // Results
        //
        public List<StrategyQueryItem> Results = null;


        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        private StrategiesQuery() { }                   // this hides the default (no parameter) constructor from user.
        public StrategiesQuery(int groupId)
        {
            m_GroupId = new List<int>();
            m_GroupId.Add(groupId);
        }
        public StrategiesQuery(List<int> groupIds)
        {
            m_GroupId = new List<int>();
            m_GroupId.AddRange(groupIds);
        }
        public StrategiesQuery(List<int> groupIds, List<int> strategyIds)
        {
            if (groupIds!=null && groupIds.Count > 0)
            {
                m_GroupId = new List<int>();
                m_GroupId.AddRange(groupIds);
            }
            if (strategyIds!=null && strategyIds.Count > 0)
            {
                m_StrategyIds = new List<int>();
                m_StrategyIds.AddRange(strategyIds);
            }
        }

        //
        //
        //       
        #endregion//Constructors


        #region Public Methods
        // *************************************
        // ****            GetQuery         **** 
        // *************************************
        public override string GetQuery(DatabaseInfo dataBase)
        {
            TableInfo.UVStrategiesTableInfo strategyTable = dataBase.UVStrategies;
            // Start of query
            StringBuilder query = new StringBuilder();
            query.AppendFormat("SELECT * FROM {0}", strategyTable.TableNameFull);

            // Conditions
            StringBuilder cond = new StringBuilder();

            //
            // GroupId and StrategyId conditions           
            //
            bool groupIdCondition = (m_GroupId != null && m_GroupId.Count>0);                   // user has groupIds
            bool strategyIdCondition = (m_StrategyIds != null && m_StrategyIds.Count>0);        // user has strategyIds
            if ( groupIdCondition || strategyIdCondition)                                       
            {   // These are handled together since they will be "OR" conditions.   
                if (cond.Length > 0)
                    cond.Append(" AND");
                cond.Append("(");                                                               // put parens around this condition.
                // GroupId condition:
                if (groupIdCondition)
                {
                    if (m_GroupId.Count == 1)
                        cond.AppendFormat(" {0}={1}", strategyTable.GroupID, m_GroupId[0]);
                    else if ( m_GroupId.Count > 1)
                    {
                        cond.AppendFormat(" {0} in ({1}", strategyTable.GroupID,m_GroupId[0]);
                        for (int n=1; n<m_GroupId.Count; ++n)
                            cond.AppendFormat(",{0}", m_GroupId[n]);
                        cond.Append(")");
                    }
                    if (strategyIdCondition)                                                    // BOTH groupIds and stratIds were provided!
                        cond.Append(" OR");
                }
                if (strategyIdCondition)
                {
                    if (m_StrategyIds.Count == 1)
                        cond.AppendFormat(" {0}={1}", strategyTable.StrategyId, m_StrategyIds[0]);
                    else if (m_StrategyIds.Count > 1)
                    {
                        cond.AppendFormat(" {0} in ({1}", strategyTable.StrategyId, m_StrategyIds[0]);
                        for (int n = 1; n < m_StrategyIds.Count; ++n)
                            cond.AppendFormat(",{0}", m_StrategyIds[n]);
                        cond.Append(")");
                    }
                }
                cond.Append(")");                                                               // put parens around this condition.
            }

            // Now append them together.
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
        /// <returns></returns>
        public override QueryStatus AcceptData(DatabaseInfo dbInfo, List<object> values, List<string> fieldNames)
        {
            TableInfo.UVStrategiesTableInfo strategyTable = dbInfo.UVStrategies;
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
                    else if (fieldName.Equals(strategyTable.Attributes) )
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
            return string.Format("{0} {1} ", base.ToString(), this.m_GroupId);
        }// ToString()
        //
        //
        //
        //
        #endregion//Public Methods


    }//end class



    #region Strategy Query Item
    //
    //
    public class StrategyQueryItem
    {
        public int StrategyId = -1;
        public int GroupId = 0;
        public string Name = string.Empty;
        public string RunType = string.Empty;       // TODO: Create a run type enum!!

        // Start/end times.
        public DateTime StartDate = DateTime.MinValue;
        public TimeSpan StartTime = new TimeSpan(0, 0, 0);
        public DateTime EndDate = DateTime.MaxValue;
        public TimeSpan EndTime = new TimeSpan(0, 0, 0);

        // 
        public string AttributeString = string.Empty;

    }//end class
    //
    #endregion // StrategyQueryItem



}
