using System;
using System.Collections.Generic;
using System.Text;

namespace Misty.Lib.BookHubs
{
    using Misty.Lib.Utilities;
    using Misty.Lib.Products;


    public class MarketUpdateEventArgs : EventArgs
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //public InstrumentBase Instrument = null;
        public InstrumentName Name;



        public int Side = QTMath.BidSide;
        public double Price;
        public int Qty;
        
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
