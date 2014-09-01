using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace UV.Strategies.ExecutionEngines.Hedgers.TermStrutures
{
    using UV.Lib.Products;
    using UV.Lib.MarketHubs;
    using UV.Lib.OrderBooks;
    using UV.Lib.Utilities;

    using UV.Strategies.ExecutionEngines.OrderEngines.TermStructures;

    /// <summary>
    /// Class designed to help with hedging fills from a Curve Trader. 
    /// Will contain functions to help find the best hedge / layoff option
    /// as well as deal with ratio's to assis in properly hedging fills
    /// </summary>
    public class CurveHedger
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        public CurveTrader m_CurvetTrader;                  // curve trader for whom I manage hedging
        private List<CurveLeg> m_CurveLegs;
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
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
        // *************************************************************
        // ****               TryFindLegIDWithBestQtyRatio          ****
        // *************************************************************
        /// <summary>
        /// Caller would like to find the leg with the current best qty ratio for a given
        /// side of the market.  If side == Order.BidSide then we are looking for the leg 
        /// with the highest BidQty / AskQty Ratio (lowest AskQty/BidQty Ratio).  If we are
        /// looking at side == Order.AskSide then we are looking for the leg with the highest
        /// AskQty / BidQty ratio (lower BidQty / AskQty) ratio.
        /// </summary>
        /// <param name="legIDs"></param>
        /// <param name="side"></param>
        /// <param name="bestLegID"></param>
        /// <returns>False if a "best" leg id is not found.  This has to be a problem with our markets</returns>
        public bool TryFindLegIDWithBestQtyRatio(List<int> legIDs, int side, out int bestLegID)
        {
            bestLegID = -1;
            int bestQtyRatio = -1;
            int qtyRatio;
            int oppSide = QTMath.MktSideToOtherSide(side);

            for(int leg = 0; leg < legIDs.Count; leg++)
            {
                qtyRatio = m_CurveLegs[leg].m_Market.Qty[side][0] / m_CurveLegs[leg].m_Market.Qty[oppSide][0];
                if(qtyRatio > bestQtyRatio)
                {   // this leg is our current best qty ratio!
                    bestQtyRatio = qtyRatio;
                    bestLegID = leg;
                }
            }
            return (bestLegID != -1);
        }
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
