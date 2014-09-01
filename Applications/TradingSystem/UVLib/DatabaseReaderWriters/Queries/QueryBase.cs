using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.DatabaseReaderWriters.Queries
{
    public abstract class QueryBase : EventArgs
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // Unique Identifier
        //
        public readonly int QueryID;
        private static int m_NextQueryID = 0;
        
        //
        // Internal controls
        //
        public Queries.QueryStatus Status = QueryStatus.New;
        public bool IsRead = true;


        public string ErrorMessage = string.Empty;          // place where Writer puts reason for failure.

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public QueryBase()
        {
            this.QueryID = System.Threading.Interlocked.Increment(ref m_NextQueryID);
        }
        //
        //       
        #endregion//Constructors


        #region no Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        #endregion//Properties


        #region Abstract Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        /// <summary>
        /// Returns the query string that should be executed to the mysql command.
        /// </summary>
        /// <returns></returns>
        public abstract string GetQuery(DatabaseInfo databaseInfo);
        //
        //
        //
        /// <summary>
        /// Returns the string that would be the "prefix" of the mysql command. Everything from "INSERT" and to "VALUES"
        /// Useful for query aggregation
        /// </summary>
        /// <param name="databaseInfo"></param>
        /// <returns></returns>
        public virtual string GetWriteQueryPrefix(DatabaseInfo databaseInfo) 
        {
            return string.Empty;
        }
        //
        //
        //
        /// <summary>
        /// Returns the string that would be the "suffix" of the mysql command. Everything after "VALUES" and before ";"
        /// Useful for query aggregation
        /// </summary>
        /// <param name="databaseInfo"></param>
        /// <returns></returns>
        public virtual string GetWriteQuerySuffix(DatabaseInfo databaseInfo)
        {
            return string.Empty;
        }
        //
        //
        //
        /// <summary>
        /// This is the function that accepts the data from the DatabaseReaderWriter object, 
        /// and casts it into
        /// </summary>
        /// <param name="databaseInfo"></param>
        /// <param name="values"></param>
        /// <param name="fieldNames"></param>
        public abstract QueryStatus AcceptData(DatabaseInfo databaseInfo, List<object> values, List<string> fieldNames);
        //
        //
        //
        //
        //
        #endregion// Abstract Methods


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        // ****         ToString()              ***
        //
        public override string ToString()
        {
            if ( string.IsNullOrEmpty(this.ErrorMessage) )
                return string.Format("#{0} {1}", this.QueryID, this.Status);            
            else
                return string.Format("#{0} {1} Error:{2}", this.QueryID, this.Status, this.ErrorMessage);
    
        }// ToString()
        //
        //
        //
        #endregion//Public Methods



        #region no Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        #endregion//Private Methods


        #region no Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        #endregion//Event Handlers

    }
}
