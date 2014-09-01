using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace UV.Lib.OrderBooks
{
    using QTMath = UV.Lib.Utilities.QTMath;
    using UV.Lib.Engines;
    using UV.Lib.Fills;

    /// <summary>
    /// </summary>
    public class SyntheticOrder
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // Internal identification
        //
        public int OrderId;

        //
        // Trade target details
        //
        public int Side;
        public int Qty;
        public double Price;

        //
        // Fills
        //
        public List<SyntheticFill> m_SyntheticFills = new List<SyntheticFill>();// list of synthetic fills associated with this Order, these contain their legs fills as well.
        public List<Fill> m_PartialFills = new List<Fill>();                    // list of fills that have not been assigned to a synthetic fill as of yet.

        //
        // Extra fields
        //
        public string TradeReason = string.Empty;
        #endregion// members


        #region Constructors & Creators
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        protected SyntheticOrder()
            : base()
        {
        }
        //
        // 

        //
        //
        // *****************************************
        // ****         RequestNewTrade         ****
        // *****************************************
        public static SyntheticOrder RequestNewOrder(int tradeId)
        {
            SyntheticOrder e = new SyntheticOrder();
            e.OrderId = tradeId;
            return e;
        }//end Request AllControls
        //
        //
        //
        //
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        /// <summary>
        /// Synthetic Fill Qty.
        /// </summary>
        public int ExecutedQty
        {
            get
            {
                int qtyToReturn = 0;
                foreach (SyntheticFill synthFill in m_SyntheticFills)
                    qtyToReturn += synthFill.Qty;
                return qtyToReturn;
            }
        }
        #endregion//Properties


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        public SyntheticOrder Copy()
        {
            SyntheticOrder newSynthOrder = new SyntheticOrder();
            this.CopyTo(newSynthOrder);
            return newSynthOrder;
        }
        //
        //
        //
        protected void CopyTo(SyntheticOrder newSyntheticOrder)
        {
            newSyntheticOrder.OrderId = this.OrderId;
            newSyntheticOrder.Side = this.Side;
            newSyntheticOrder.Price = this.Price;
            newSyntheticOrder.Qty = this.Qty;
            newSyntheticOrder.TradeReason = this.TradeReason;
            foreach (SyntheticFill synthFill in this.m_SyntheticFills)
                newSyntheticOrder.m_SyntheticFills.Add(synthFill);
        }// CopyTo()
        //
        //        
        //
        public override string ToString()
        {
            StringBuilder msg = new StringBuilder();
            msg.AppendFormat("#{0}: {1} {2} @ {3} ExecQty = {4}", this.OrderId, QTMath.MktSideToString(this.Side), this.Qty, this.Price, this.ExecutedQty);
            return msg.ToString();
        }
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

    }//end class
}