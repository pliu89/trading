using System;
using System.Collections.Generic;
using System.Text;
using UV.Lib.Utilities;

namespace UV.Lib.Fills
{
    public class SyntheticFill : Fill
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        public List<Fill> LegFills = new List<Fill>();          // fills that compromise the synthetic.
        //
        //
        #endregion// members

        #region Public Methods
        // *****************************************************************
        // ****                   Public Methods                        ****
        // *****************************************************************
        /// <summary>
        /// Callers has a Fill that they would like to convert to a Synthetic Fill
        /// to be allow it to be passed to a strategy.
        /// </summary>
        /// <param name="fill"></param>
        /// <returns></returns>
        public static SyntheticFill CreateSyntheticFillFromFill(Fill fill)
        {
            SyntheticFill syntheticFill = new SyntheticFill();
            
            syntheticFill.InstrumentName = fill.InstrumentName;
            syntheticFill.Price = fill.Price;
            syntheticFill.Qty = fill.Qty;
            syntheticFill.LocalTime = fill.LocalTime;
            syntheticFill.ExchangeTime = fill.ExchangeTime;
            syntheticFill.LegFills.Add(fill);
            
            return syntheticFill;
        }
        #endregion // Public Methods
    }
}
