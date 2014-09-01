using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

namespace UV.Lib.DatabaseReaderWriters.Queries
{

    using UV.Lib.Utilities;

    /// <summary>
    /// </summary>
    public class MarketDataItem : IComparable<MarketDataItem>
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        // Data
        //
        public UInt32 UnixTime;
        public double[] Price = new double[3];
        public int[] Qty = new int[3];
        
        public int SessionVolume = 0;       // volume on the session as reported by TT
        public int LongVolume = 0;          // volume that happened initiated by a buyer (summed from time and sales)
        public int ShortVolume = 0;         // volume that happened initiated by a seller (summed from time and sales)
        public int TotalVolume = 0;         // total volume that happened (summed from time and sales)
        
        public int SessionCode = 0;         // code for session.  1 == trading 0 == not trading

        //
        // Secondary data
        //

        protected DateTime m_TimeStamp = DateTime.MinValue;

        public const int AskSide = QTMath.AskSide;
        public const int BidSide = QTMath.BidSide;
        public const int LastSide = QTMath.LastSide;
        #endregion// members

        #region Properties
        public DateTime TimeStamp
        {
            get
            {
                if (m_TimeStamp == DateTime.MinValue)               // craete it now, if it doesn't exist.
                    m_TimeStamp = UV.Lib.Utilities.QTMath.EpochToDateTime(UnixTime);
                return m_TimeStamp;
            }
        }
        #endregion//Properties


        #region IComparable
        //
        public int CompareTo(MarketDataItem other)
        {
            return this.UnixTime.CompareTo(other.UnixTime);
            //return this.TimeStamp.CompareTo(other.TimeStamp);
        }
        //
        #endregion// IComparable<>


    }//end class
}
