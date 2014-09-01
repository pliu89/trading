using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UV.Lib.Database
{
    public class DatabaseWriterEventArgs : EventArgs
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        public DatabaseWriterRequests Request;
        public StringBuilder QueryBase = new StringBuilder();
        public StringBuilder QueryValues = new StringBuilder();

        // Internal usage.
        public int NFails = 0;
        #endregion members

        #region Properties
        // *****************************************************************
        // ****                     Properties                         ****
        // *****************************************************************
        //
        public string Query
        {
            get { return string.Format("{0} {1}", QueryBase.ToString(), QueryValues.ToString()); }
        }
        #endregion //Properties

        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        public void Clean()
        {
            this.Request = DatabaseWriterRequests.None;
            this.QueryBase.Remove(0, QueryBase.Length);
            this.QueryValues.Remove(0, QueryValues.Length);
            this.NFails = 0;
        }
        public override string ToString()
        {
            return string.Format("{0} {1} {2}", Request.ToString(), QueryBase.ToString(), QueryValues.ToString());
        }
        public string ToStringShort()
        {
            return string.Format("{0} {1}", Request.ToString(), QueryBase.ToString());
        }
        #endregion // Public Methods

    }//class EventArgs

    public enum DatabaseWriterRequests
    {
        Write,
        SendEmail,
        Stop,
        None
    }
}
