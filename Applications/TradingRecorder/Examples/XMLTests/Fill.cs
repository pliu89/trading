using System;
using System.Collections.Generic;
using System.Text;

namespace XMLTests
{
    public class Fill
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        public int Qty = 0;                                     // signed quantity.
        public double Price;
        public DateTime TransactionTime;                        // time fill was received by local application.
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public Fill() { }
        //       
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        #endregion//Properties


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        public override string ToString()
        {
            return string.Format("{0}@{1} {2:MM-dd-yy H:mm:ss.fff zzz}", this.Qty.ToString("+0;-0;0"), this.Price.ToString(), this.TransactionTime);
        }
        //
        //
        //
        //
        //
        #endregion//Public Methods


    }
}
