//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace UV.Strategies.ExecutionEngines.SpreaderFills
//{
//    using UV.Lib.Utilities;
//    using UV.Lib.OrderBooks;
//    using UV.Lib.Fills;
//    /// <summary>
//    /// inherited class to allow for Ratio Spread Pricing.
//    /// </summary>
//    public class SpreaderRatioFillGenerator : SpreaderFillGenerator
//    {
//        #region no Members
//        // *****************************************************************
//        // ****                     Members                             ****
//        // *****************************************************************
//        //
//        #endregion// members

//        #region Constructors
//        // *****************************************************************
//        // ****                     Constructors                        ****
//        // *****************************************************************
//        public SpreaderRatioFillGenerator(string syntheticSpreadName, double[] legRatios, double[] legMultipliers, FillBook[] fillBooks)
//            : base(syntheticSpreadName, legRatios, legMultipliers, fillBooks)
//        {

//        }
//        //
//        //       
//        #endregion//Constructors

//        #region no Properties
//        // *****************************************************************
//        // ****                     Properties                          ****
//        // *****************************************************************
//        //
//        //
//        #endregion//Properties

//        #region no Public Methods
//        // *****************************************************************
//        // ****                     Public Methods                      ****
//        // *****************************************************************
//        //
//        //
//        public override SyntheticFill GenerateSyntheticFill(int stratSide)
//        {
//            SyntheticFill newSyntheticFill = new SyntheticFill();
//            newSyntheticFill.SyntheticSpreadName = m_SyntheticSpreadName;
//            newSyntheticFill.Qty = (int)(base.m_pendingFillQtys[base.m_SmallestLegIndex] /            // just a starting point
//                m_SmallestLegRatio);
//            // First lets try and find the fill qty. Since we have our array of pendingFills we should be 
//            // able to divide all legs by the smallest leg, and find the smallest whole number from that
//            // which will be our fillqty.
//            for (int i = 0; i < m_pendingFillQtys.Length; ++i)
//            {
//                int possibleSyntheticQty = (int)(m_pendingFillQtys[i] / (long)m_LegRatios[i]);
//                // we are looking to make sure we are going to take the largest number of COMPLETE fills
//                if (Math.Abs(possibleSyntheticQty) < Math.Abs(newSyntheticFill.Qty))
//                    newSyntheticFill.Qty = possibleSyntheticQty;
//            }
//            // we should theoretically now have them correct fill qty.
//            // we now need to work on price, while we do this we can remove fills from our list to ensure they
//            // aren't used more than once.
//            double[] legAvgPrices = new double[2];                                                                      // find the average pricing for the fills we are removing legAvgPrices[legID]
//            for (int leg = 0; leg < m_LegRatios.Length; ++leg)
//            {
//                int legSide = QTMath.MktSignToMktSide(QTMath.MktSideToMktSign(stratSide) * m_LegRatios[leg]);           // Find which side
//                long qtyToRemove = (long)(m_LegRatios[leg] * newSyntheticFill.Qty);                                     // find the qty we need to remove from the fill
//                double legAvgPrice = m_FillBooks[leg].m_FillPages[legSide].GetAveragePricing(qtyToRemove);              // find the average pricing for those fills we are removing
//                // TODO FIX THIS ISSUE with new methods!
//                //if (!m_FillBooks[leg].m_FillPages[legSide].DequeueQty(qtyToRemove))                                   // remove them and if false we probably have an issue so lets not report the fill.  
//                //    break;
//                legAvgPrices[leg] = Math.Abs(m_LegPriceMultipliers[leg]) * legAvgPrice;                                 // we need to multiply the average price times the leg multiplier 
//            }
//            newSyntheticFill.Price = legAvgPrices[0] / legAvgPrices[1];
//            newSyntheticFill.LocalTime = DateTime.Now;                                                                  // okay we have a synthetic fill with everything but a timestamp, lets take the current timestamp
//            OrderEventArgs e = new OrderEventArgs();                                                                    // trigger the event of synthetic fills.
//            e.Fill = newSyntheticFill;
//            base.OnSyntheticSpreadFilled(e);
//            return newSyntheticFill;
//        }
//        //
//        #endregion//Public Methods

//        #region no Private Methods
//        // *****************************************************************
//        // ****                     Private Methods                     ****
//        // *****************************************************************
//        //
//        //
//        #endregion//Private Methods

//        #region no Event Handlers
//        // *****************************************************************
//        // ****                     Event Handlers                     ****
//        // *****************************************************************
//        //
//        #endregion//Event Handlers
//    }
//}
