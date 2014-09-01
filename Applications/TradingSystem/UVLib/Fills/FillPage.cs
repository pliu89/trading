using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.Fills
{
    //using UV.Lib.OrderBookHubs;
    using UV.Lib.Utilities;
    public class FillPage
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // Each fill queue will contain fills for an instrument on a side 
        // of the market.
        // Each instrument will have a book with 2 pages, 1 for each side of the
        // strategy
        // Fill Lookup tables
        private readonly int FillSide;     
        private readonly int FillSign;

        private List<Fill> m_TempFillList = new List<Fill>();  // we will always pull from the top and add to the back.
        private List<Fill> m_FillList = new List<Fill>();      // this will simply be a total history of fills
        

        #endregion// members

        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sideOfOrders">Each FillPage contains Long or Short Fills</param>
        public FillPage(int sideOfOrders)
        {
            this.FillSide = sideOfOrders;
            this.FillSign = QTMath.MktSideToMktSign(this.FillSide);
        }
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
        // *************************************************
        // ****                 Try Add()               ****
        // *************************************************
        /// <summary>
        /// TryAdd will attempt to add a fill to both the synthetic Queue of 
        /// fills as well as the historic list of fills return a bool for
        /// the status of acceptance into these collections.
        /// </summary>
        /// <param name="newFill"></param>
        /// <returns></returns>
        public bool TryAdd(Fill newFill)
        {
            // make sure it is the correct side
            if (this.FillSign != Math.Sign(newFill.Qty))
                return false;
            // We want to add the complete fill to the list of fills now.  This is a 
            // copy we aren't ever going to change.
            m_FillList.Add(newFill);
            // We want to take this fill and create a new copy of it because we want
            // to be able to modify it taking "part" of the fill away as needed for 
            // assigning synthetic fills before we pop it off the queue when we are done.
            Fill newFillTempCopy = newFill.Clone();
            m_TempFillList.Add(newFillTempCopy); // add to the back 
            return true;
        }
        //
        // *************************************************
        // ****           GetPendingQty()               ****
        // *************************************************
        /// <summary>
        /// Finds total pending or unassigned(to a synthetic) fill Qty. NOTE: this is NOT and abs value.
        /// </summary>
        /// <returns>long fill qty that is currently unnassgined/pending</returns>
        public long GetPendingQty()
        {
            long pendingFillQty = 0;
            //iterate through list to find the pending fill qty
            foreach (Fill fill in m_TempFillList)
            {
                pendingFillQty += fill.Qty;
            }
            return pendingFillQty;
        }
        //
        // *************************************************
        // ****           DequeueQty()                  ****
        // *************************************************
        /// <summary>
        /// Decrements a list of fills based on Qty needed, always pulling 
        /// unnassigned fill Qty's from the top.  If the user wants the specific
        /// fills that are dequeued they will be outputed in referenced fillsDequeued.
        ///     Note : These fills will have qty manipulated if that have been split!
        /// </summary>
        /// <param name="qtyToRemove"></param>
        /// <param name="fillsDequeued"></param>
        /// <returns>false if remove failed</returns>
        public bool DequeueQty(int qtyToRemove, ref List<Fill> fillsDequeued)
        {
            int removedQty = 0;
            // check to make sure we have sufficient qty.
            if(Math.Abs(GetPendingQty()) < Math.Abs(qtyToRemove))
                return false;
            // check we are looking at right side of the market
            if(this.FillSign != Math.Sign(qtyToRemove))
                return false;
            // start decrementing list from the top.
            while(Math.Abs(removedQty) < Math.Abs(qtyToRemove))
            {
                int neededQty = qtyToRemove - removedQty;
                // this is a safety check to make sure we don't get stuck in here 
                if(m_TempFillList.Count == 0)
                    break;
                // if the fill on the top is the same size or less than the qty we need to remove
                // we can simply pull it off the top.
                if(Math.Abs(m_TempFillList[0].Qty) <= Math.Abs(neededQty))
                {
                    removedQty += m_TempFillList[0].Qty;
                    fillsDequeued.Add(m_TempFillList[0]);
                    m_TempFillList.Remove(m_TempFillList[0]);
                }
                else
                {   //we need to keep the fill(not removing from the list), but change its qty.
                    int availableQty = m_TempFillList[0].Qty;
                    int correctedFillQty = availableQty - neededQty;
                    removedQty += neededQty;
                    
                    //
                    // Create new modified fill to return to the list with the corrected qty.
                    // 
                    m_TempFillList[0].Qty = correctedFillQty;
                    Fill fillToDequeue = m_TempFillList[0].Clone();
                    fillToDequeue.Qty = neededQty;
                    fillsDequeued.Add(fillToDequeue);
                    // by definition we should now have had to removed the complete qty
                }
            }
            return (Math.Abs(removedQty) == Math.Abs(qtyToRemove));
        }
        //
        // *************************************************
        // ****           GetAveragePricing()           ****
        // *************************************************
        /// <summary>
        /// Find the average price of the given qty of fills from the top of the list of 
        /// pending fills
        /// </summary>
        /// <param name="qty"></param>
        /// <returns>double of average price</returns>
        public double GetAveragePricing(long qty)
        {
            long accountedForQty = 0;
            double averagelegPrice = 0;
            // check to make sure we have sufficient qty.
            if (Math.Abs(GetPendingQty()) < Math.Abs(qty))
                return double.NaN;
            // check we are looking at right side of the market
            if (this.FillSign != Math.Sign(qty))
                return double.NaN;
            // start decrementing list from the top.
            while (Math.Abs(accountedForQty) < Math.Abs(qty))
            {
                long neededQty = qty - accountedForQty;
                // this is a safety check to make sure we don't get stuck in here 
                if (m_TempFillList.Count == 0)
                    break;
                // if the fill on the top is the same size or less than the qty we need to remove
                // we can just want to take the average price and conitnue
                if (Math.Abs(m_TempFillList[0].Qty) <= Math.Abs(neededQty))
                {
                    accountedForQty += m_TempFillList[0].Qty;
                    averagelegPrice += (double)Math.Abs(m_TempFillList[0].Qty) * m_TempFillList[0].Price;
                }
                else
                {
                    accountedForQty += neededQty;
                    averagelegPrice += (double)Math.Abs(neededQty) * m_TempFillList[0].Price;
                }
            }
            return averagelegPrice = averagelegPrice / (double)Math.Abs(qty);
        }
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
