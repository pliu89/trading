using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Strategies.ExecutionEngines.Hedgers
{
    //using UV.Lib.OrderBookHubs;
    using UV.Lib.Hubs;
    public class RatioHedgeManager : HedgeManager
    {
        #region no  Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        #endregion// members

        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        ////
        public RatioHedgeManager(ExecutionEngines.OrderEngines.Spreader quoter, LogHub logHub)
            : base()
        {
            // instantiate base class
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
        //
        //
        //
        //public override void HedgeFill(int quoteInstrId, Fill aFill)
        //{
        //    BreInstrumentExecutor quoteInstr = base.m_Instruments[quoteInstrId];                                // Get information about quote leg
        //    long quoteFillQty = aFill.Qty;
        //    double quoteFillPrice = aFill.Price;
        //    int stratSide = UV.Lib.Utilities.QTMath.MktSignToMktSide(Math.Sign(quoteFillQty * quoteInstr.Weight));    // find what side of the spread we got filled on 
        //    double stratPrice = base.m_Quoter.m_StrategyWorkingPrice[stratSide];                                // find what price we are working in the spread
        //    long[] neededHedgeQty = CalculateNeededHegeQty(quoteInstrId, quoteFillQty);                         // get our hedge QTY array 
        //    if (m_IsSratHung[stratSide] == false)                                                               // fire event for our hang state changing.
        //    {
        //        m_IsSratHung[stratSide] = true;                                                                 // We are not hedged on this side.
        //        OnHangStateChanged();
        //    }
        //    int hedgeLegId;                                                                                     // Find Hedge Prices and Send Hedge Orders 
        //    if (quoteInstrId == 0)                                                                              // filled on LegA, hedge in LegB
        //        hedgeLegId = 1;
        //    else                                                                                                // filled on LegB hedge in Leg A
        //        hedgeLegId = 0;
        //    long hedgeQty = neededHedgeQty[hedgeLegId];                                                         // find the qty we need to hedge
        //    if (hedgeQty != 0)
        //    { // we need to send a hedge order!
        //        BreInstrumentExecutor hedgeInstr = m_Instruments[hedgeLegId];
        //        double hedgePrice;
        //        if(hedgeLegId == 0)                                                                             // hedge in legA
        //            hedgePrice = stratPrice * quoteFillPrice *                                                  // LegAPrice = (LegB*abs(LegBMult)) * SpreadPrice
        //                Math.Abs(base.m_Instruments[quoteInstrId].PriceMultiplier);
        //        else                                                                                            // hedge in Leg B
        //            hedgePrice = ((base.m_Instruments[quoteInstrId].PriceMultiplier *                      // LegB = ((LegA * LegAMult) / SpreadPrice) / abs(legBMult)
        //                            quoteFillPrice) / stratPrice) / Math.Abs(base.m_Instruments[hedgeLegId].PriceMultiplier);
        //        m_Hedgers[hedgeLegId].SubmitToHedger(hedgeInstr, hedgeQty, hedgePrice);                         // submit our hedge orders.
        //    }
        //}
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

        #region no Events
        // *****************************************************************
        // ****                         Events                          ****
        // *****************************************************************
        //
        //
        #endregion //Events
    }
}
