using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.Fills
{
    //using UV.Lib.OrderBookHubs;
    using UV.Lib.Utilities;
    public class FillBook
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // Identification for this bookk
        public readonly string Instrument;
        // instrument params

        public double m_ContractMultiplier = 50;              
        
        // book will have 2 pages for each side of the market, each page has
        // a queue of orders waiting to be put into synthetics and also 
        // a list of all fills for reference.
        public FillPage[] m_FillPages = null;

        // Internally maintained active fills.
        private List<Fill> m_FillsOpen = new List<Fill>(16);            // store for open fills that comprise our CURRENT position.


        public int m_NetPosition = 0;                                  // Short cut to remember current net position.
        // Fill book PnL variables.
        public DateTime RealizedGainStartTime = DateTime.Now;           // Time from which PnL will be accumulated.
        public double m_RealizedGain = 0.0;                             // dollarized.
        public double m_UnRealizedGain = 0.0;                           // dollarized.
        private double m_RealizedStartingGain = 0.0;                    // Real gain at the last time we reset RealGain; CummulativeRealGain = RealGain + RealGainAtStart

        private long m_Volume = 0;                                       // Sum of total absolute quantity fill.
        private long m_StartingVolume = 0;

        private double m_AveCost = 0.0;                                 // Maintain the average fill price here.            
        private double m_LastMidPrice = 0;                              // Most recent price used for evaluating value of open position.



        #endregion// members

        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        /// <summary>
        /// Construct a new fillbook for a given instrument.
        /// </summary>
        /// <param name="instrument">name of security</param>
        /// <param name="contractMultiplier">contractMultiplier of instrument.  This should be the dollar value of an integerized price changed for the instrument</param>
        public FillBook(string instrument, double contractMultiplier)
        {
            this.Instrument = instrument;
            m_ContractMultiplier = contractMultiplier;  
            
            m_FillPages = new FillPage[2];           // 1 for long, 1 for short
            for (int side = 0; side < m_FillPages.Length; ++side)
            {
                m_FillPages[side] = new FillPage(side);
            }
        }
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
        // *************************************************
        // ****                 Try Add()               ****
        // *************************************************
        //
        /// <summary>
        /// Add new Fill to the fill book
        /// </summary>
        /// <param name="aFill"></param>
        /// <returns>false if add failed</returns>
        public bool TryAdd(Fill aFill)
        {
            int remainingQty = aFill.Qty;                                                  // variable to subtract fills from
            
            bool isAddOkay = m_FillPages[QTMath.MktSignToMktSide(aFill.Qty)].TryAdd(aFill);        // Store the Fill
            m_Volume += Math.Abs(aFill.Qty);                                                // updated total volume

            // Allocate Fills against our open position/
            if ( m_FillsOpen.Count == 0 || remainingQty * m_FillsOpen[0].Qty >= 0)
             {   // we have no opens fill OR the fill is on the same side so we adding to our position, or new position.
                Fill newFill = aFill.Clone();           // clone the fill so we can edit it
                m_FillsOpen.Add(newFill);               // add the fill to the "open" list
            }
            else
            {   // we have an opposing position so we need to cancel positions.
                double realPnL = 0.0;
                while (remainingQty!=0 && m_FillsOpen.Count > 0)
                {
                    Fill oldFill = m_FillsOpen[m_FillsOpen.Count - 1];
                    if ((oldFill.Qty + remainingQty) * remainingQty >= 0)
                    {   // Incoming qty kills old fill qty "Q0" entirely; so the transacting amount is Q0
                        realPnL += -oldFill.Qty * oldFill.Price + oldFill.Qty * aFill.Price;// -Q0*P0 - (-Q0)*P
                        remainingQty += m_FillsOpen[m_FillsOpen.Count - 1].Qty;                     // Q = Q + Q0
                        m_FillsOpen.RemoveAt(m_FillsOpen.Count - 1);                                // remove entire old fill [P0,Q0]
                    }
                    else
                    {   // Old fill completely absorbs new fill qty "Q"; transacting amount is Q
                        realPnL += remainingQty * oldFill.Price - remainingQty * aFill.Price;// -pnl = -(-Q)*P1 - Q*P
                        oldFill.Qty += remainingQty;                                        // reduce oldQty by amount transacted.
                        remainingQty = 0;                                                   // there is no quantity left.
                    }
                }
                if (remainingQty != 0)
                {   // After cancelling out all levels of old fills, we still have some left!
                    Fill newFill = aFill.Clone();
                    newFill.Qty = remainingQty;                                              // overwrite the quantity with remaining qty.
                    m_FillsOpen.Add(newFill);
                }
                m_RealizedGain += realPnL * m_ContractMultiplier;
            }

            UpdatePositionInfo();
            
            return isAddOkay;
        }
        //
        //
        // *********************************************************************
        // ****                         PnL Methods                         ****
        // *********************************************************************
        /// <summary>
        /// Allows user to reset the PnL.  Useful at the end of a trading session, 
        /// if we don't want to roll our PnL forward into the next session.
        /// </summary>
        public void ResetRealizedDollarGains()
        {
            m_RealizedStartingGain += m_RealizedGain;               // Roll the gain into the cummulative gain.
            m_RealizedGain = 0;
            m_StartingVolume += m_Volume;                           // Roll volume into the cummulative volume.
            m_Volume = 0;
        }
        /// <summary>
        /// Allows the user to set the current gains for this book.  If only one of them needs to be set, 
        /// then the user can pass a NaN for the other (which will be skipped).
        /// </summary>
        /// <param name="todaysRealPnL">PnL (in book's currency) realized since last reset time.</param>
        /// <param name="openingRealPnL">PnL (in book's currency) realized over lifetime.</param>
        public void ResetRealizedDollarGains(double todaysRealPnL, double openingRealPnL)
        {
            if (!double.IsNaN(todaysRealPnL))
                m_RealizedGain = todaysRealPnL;
            if (!double.IsNaN(openingRealPnL))
                m_RealizedStartingGain = openingRealPnL;
        }
        //
        public double UnrealizedDollarGains()
        {
            return UnrealizedDollarGains(m_LastMidPrice);
        }
        public double UnrealizedDollarGains(double midPrices)
        {
            m_LastMidPrice = midPrices;
            double gain = 0.0;
            foreach (Fill aFill in m_FillsOpen)
            {
                gain -= aFill.Qty * (aFill.Price - m_LastMidPrice);
            }
            m_UnRealizedGain = gain * m_ContractMultiplier;
            return (m_UnRealizedGain);
        }
        /// <summary>
        /// The user can demand that all internal values are recaulated.  Useful upon initialization.
        /// </summary>
        public void RecalculateAll()
        {
            UpdatePositionInfo();
            UnrealizedDollarGains(this.m_AveCost);
        }
        //
        //
        public override string ToString()
        {
            StringBuilder msg = new StringBuilder();
            msg.AppendFormat("{0} ", this.Instrument);
            msg.AppendFormat("Net={0} ", this.m_NetPosition);
            foreach (Fill fill in m_FillsOpen)
                msg.AppendFormat("[{0}]", fill.ToString());
            return msg.ToString();
        }//ToString()
        //
        //
        //
        #endregion//Public Methods

        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //***************************************************
        // ****        Update Position Info()           *****
        //***************************************************
        /// <summary>
        /// Update Net Positions and Average Costs
        /// </summary>
        private void UpdatePositionInfo()
        {
            if (m_FillsOpen.Count == 0)
            {
                m_NetPosition = 0;
                m_AveCost = 0.0;
                return;
            }
            int net = 0;
            double aveCost = 0.0;
            foreach (Fill aFill in m_FillsOpen)
            {
                net += aFill.Qty;
                aveCost += aFill.Qty * aFill.Price;
            }
            m_NetPosition = net;
            m_AveCost = aveCost / m_NetPosition;
        }// UpdatePositionInfo().
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
