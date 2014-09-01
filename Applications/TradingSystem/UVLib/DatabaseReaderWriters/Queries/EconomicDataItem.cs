using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

namespace UV.Lib.DatabaseReaderWriters.Queries
{

    using UV.Lib.Utilities;

    /// <summary>
    /// </summary>
    public class EconomicDataItem : IEquatable<EconomicDataItem>
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        // Data
        //
        public UInt32 UnixTime;
        public string EventName;
        public string ShortName;
        public int TickerId;

        //
        // Secondary data
        //
        protected DateTime m_TimeStamp = DateTime.MinValue;
        #endregion// members

        #region Properties
        /// <summary>
        /// DateTime in LOCAL TIME!
        /// </summary>
        public DateTime TimeStamp
        {
            get
            {
                if (m_TimeStamp == DateTime.MinValue)               // craete it now, if it doesn't exist.
                    m_TimeStamp = UV.Lib.Utilities.QTMath.EpochToDateTime(UnixTime).ToLocalTime();
                return m_TimeStamp;
            }
        }
        #endregion//Properties

        bool IEquatable<EconomicDataItem>.Equals(EconomicDataItem other)
        {
            if (other.UnixTime == this.UnixTime && other.TickerId == this.TickerId && this.EventName == other.EventName)
                return true;
            return false;
        }
    }//end class
}
