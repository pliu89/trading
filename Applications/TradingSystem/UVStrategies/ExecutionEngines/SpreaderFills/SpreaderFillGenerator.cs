using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Strategies.ExecutionEngines.SpreaderFills
{
    using UV.Lib.OrderBooks;
    using UV.Lib.Fills;
    using UV.Lib.Products;
    public class SpreaderFillGenerator
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        public InstrumentName m_SyntheticInstrumentName;    // information about our sythnetic.

        protected double[] m_LegRatios;
        protected double[] m_LegPriceMultipliers;
        protected FillBook[] m_FillBooks;

        protected int m_SmallestLegIndex;                   // the id of the leg with the smallest ratio
        protected double m_SmallestLegRatio;                // the ratio of the smallest leg.

        protected long[] m_pendingFillQtys;                 // when we check the Qty's we should keep them.
        #endregion// members

        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public SpreaderFillGenerator(InstrumentName syntheticInstrumentName, double[] legRatios, double[] legMultipliers, FillBook[] fillBooks)
        {
            this.m_SyntheticInstrumentName = syntheticInstrumentName;
            this.m_LegRatios = legRatios;
            this.m_LegPriceMultipliers = legMultipliers;
            this.m_FillBooks = fillBooks;

            // find the smallest ratio 
            m_SmallestLegRatio = m_LegRatios[0];                // we can assume it is the first to start

            m_pendingFillQtys = new long[m_LegRatios.Length];   // create place to store pending Qtys

            for (int leg = 1; leg < m_LegRatios.Length; ++leg)
            {
                if (Math.Abs(m_LegRatios[leg]) < Math.Abs(m_SmallestLegRatio))
                    m_SmallestLegRatio = m_LegRatios[leg];      // this is NOT an abs value, but the actual ratio rounded to an correct qty that isn't a decimal
            }
            // if 2 legs have the same (and smallest ratio) It won't matter for our calculations
            // we just want the smallest first.  I hope this will save computational time 
            // during runtime.  
            m_SmallestLegIndex = Array.IndexOf(legRatios, m_SmallestLegRatio);
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
        // *************************************************
        // ****         CheckFillQueues()               ****
        // *************************************************
        /// <summary>
        /// Will check all temporary fill lists on a side of strat to see if there is 
        /// sufficient qty to generate a Synthetic Fill from a an array containing
        /// the fill books of all the legs of a given strategy.
        /// </summary>
        /// <param name="stratSide"></param>
        /// <param name="legFillBooks"></param>
        /// <returns>true if sufficient Qty's for synthetic fill</returns>
        public bool CheckFillQueues(int stratSide, FillBook[] legFillBooks)
        {
            // Check if we have a fill QTY in the leg with the smallest ratio.
            // if we are buying our strat, and our smallest leg is negative, than we want to check
            // the short fills in the fill book to see if we have any, if we don't we can move on.
            // if we do we need to deal with the queue and removing our fill qty's for each instrument.
            int smallestLegSide = UV.Lib.Utilities.QTMath.MktSignToMktSide(UV.Lib.Utilities.QTMath.MktSideToMktSign(stratSide) * m_SmallestLegRatio);
            long smallestLegPendingQty = legFillBooks[m_SmallestLegIndex].m_FillPages[smallestLegSide].GetPendingQty();
            // Check to see if want to try and start checking all the queueus, if not return false.
            if (Math.Abs(smallestLegPendingQty) < (long)Math.Abs(m_SmallestLegRatio))
                return false;

            int syntheticFillFlagCount = 0;  // if we have sufficient pending qty in ALL legs this will == our number of legs
            for (int leg = 0; leg < m_LegRatios.Length; ++leg)
            {

                double legRatio = m_LegRatios[leg];
                int legSide = UV.Lib.Utilities.QTMath.MktSignToMktSide(UV.Lib.Utilities.QTMath.MktSideToMktSign(stratSide) * legRatio);
                FillPage legFillPage = legFillBooks[leg].m_FillPages[legSide];
                long legPendingQty = legFillPage.GetPendingQty();
                if (Math.Abs(legPendingQty) < (long)Math.Abs(legRatio)) // we have insufficient qty
                    break;
                else
                    m_pendingFillQtys[leg] = legPendingQty; // save this QTY so later we can decide the correct qty for our fills.
                syntheticFillFlagCount++;
            }

            return syntheticFillFlagCount == m_LegRatios.Length;  // all legs have sufficient pending qty
        }
        //
        public bool TryGenerateSyntheticFill(int stratSide, out SyntheticFill newSyntheticFill)
        {
            if (CheckFillQueues(stratSide, m_FillBooks))
            {
                newSyntheticFill = GenerateSyntheticFill(stratSide);
                return true;
            }
            else
            {
                newSyntheticFill = null;
                return false;
            }
        }
        //
        //
        public virtual SyntheticFill GenerateSyntheticFill(int stratSide)
        {
            SyntheticFill newSyntheticFill = new SyntheticFill();
            newSyntheticFill.InstrumentName = m_SyntheticInstrumentName;
            newSyntheticFill.Qty = (int)(m_pendingFillQtys[m_SmallestLegIndex] / m_SmallestLegRatio); // just a starting point
            // First lets try and find the fill qty. Since we have our array of pendingFills we should be 
            // able to divide all legs by the smallest leg, and find the smallest whole number from that
            // which will be our fillqty.
            for (int i = 0; i < m_pendingFillQtys.Length; ++i)
            {
                int possibleSyntheticQty = (int)(m_pendingFillQtys[i] / (long)m_LegRatios[i]);
                // we are looking to make sure we are going to take the largest number of COMPLETE fills
                if (Math.Abs(possibleSyntheticQty) < Math.Abs(newSyntheticFill.Qty))
                    newSyntheticFill.Qty = possibleSyntheticQty;
            }
            // we should theoretically now have them correct fill qty.
            // we now need to work on price, while we do this we can remove fills from our list to ensure they
            // aren't used more than once.
            for (int leg = 0; leg < m_LegRatios.Length; ++leg)
            {
                // Find which side
                int legSide = UV.Lib.Utilities.QTMath.MktSignToMktSide(UV.Lib.Utilities.QTMath.MktSideToMktSign(stratSide) * m_LegRatios[leg]);
                // find the qty we need to remove from the fill
                int qtyToRemove = (int)(m_LegRatios[leg] * newSyntheticFill.Qty);
                // find the average pricing for those fills we are removing
                double legAvgPrice = m_FillBooks[leg].m_FillPages[legSide].GetAveragePricing(qtyToRemove);
                // remove them and if false we probably have an issue so lets not report the fill.  
                if (!m_FillBooks[leg].m_FillPages[legSide].DequeueQty(qtyToRemove, ref newSyntheticFill.LegFills))
                    break;
                // we need to multiply the average price times the leg multiplier 
                legAvgPrice = m_LegPriceMultipliers[leg] * legAvgPrice;
                // assign this legs price to our spread price we will report.
                newSyntheticFill.Price += legAvgPrice;
            }
            // okay we have a synthetic fill with everything but a timestamp, lets take the current timestamp
            newSyntheticFill.LocalTime = newSyntheticFill.ExchangeTime = DateTime.Now;
            
            // trigger the event of synthetic fills.
            FillEventArgs e = new FillEventArgs();
            e.Fill = newSyntheticFill;
            OnSyntheticSpreadFilled(e);

            return newSyntheticFill;
        }
        //
        #endregion//Public Methods

        #region no Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        #endregion//Private Methods

        #region  Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        public event EventHandler SyntheticSpreadFilled;
        //
        //
        //
        protected void OnSyntheticSpreadFilled(FillEventArgs fillEventArgs)
        {
            if (this.SyntheticSpreadFilled != null)
                this.SyntheticSpreadFilled(this, fillEventArgs);
        }
        #endregion//Event Handlers

    }
}
