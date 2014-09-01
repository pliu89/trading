using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.BookHubs
{
    using UV.Lib.Utilities;
    using UV.Lib.Products;

    /// <summary>
    /// This 
    /// </summary>
    public class MarketUpdateEventArgs : EventArgs
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        public InstrumentName Name;


        public int Side = QTMath.BidSide;
        public double Price;
        public int Qty;
        public int TotalVolume;    // total volume on day
        
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        //       
        #endregion//Constructors


 

    }
}
