using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.OrderHubs
{

    using UV.Lib.Products;
    using M = UV.Lib.Utilities.QTMath;

    public class Order
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // Ordering Identifier
        public string HubName = string.Empty;       // unique hub name for owner of this order.
        public InstrumentName Instrument;           // 
        public string Tag;                          // unique id that remains unique across trading sessions, forever.

        // Data:
        public int Qty = 0;                         // This is the signed Qty.
        public int IPrice = 0;
        public int Side = -1;                       // will throw error when added to book if not set!

        //
        //
        //
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public Order(string hubName, InstrumentName instrumentName, string uniqueTag)
        {
            this.HubName = hubName;
            this.Instrument = instrumentName;
            this.Tag = uniqueTag;
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


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        public override string ToString()
        {
            return string.Format("{0} {1}@{2} {3}", M.MktSideToString(this.Side), this.Qty, this.IPrice, this.Instrument);
        }
        //
        //
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
