using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.BookHubs
{

    public class MarketStatusEventArgs : EventArgs
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************

        //public Misty.Lib.Products.InstrumentBase Instrument = null;
        public UV.Lib.Products.InstrumentName InstrumentName;
        public MarketStatus Status = MarketStatus.Unknown;
      


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
