using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.BookHubs
{

    using UV.Lib.Utilities;
    using UV.Lib.Products;

    public class MarketBase : EventArgs
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        public InstrumentName Name;                     // instrument being updated

        //
        // Market data
        //
        public double[][] Price = null;
        public int[][] Qty = null;
        public int[] Volume = null;                     // Total Volume[buySide,sellSide,both,unknown]
        public List<int>[] ChangedIndices = null;       // list of all indeces that have changed for a give market.
        public int DeepestLevelKnown = 5;               // default value
        public bool IsIncludesTimeAndSales = false;     // flag for addional volume data 
        //
        // Constants
        //
        public const int BidSide = QTMath.BidSide;
        public const int AskSide = QTMath.AskSide;
        public const int LastSide = QTMath.LastSide;
        public const int UnknownSide = QTMath.UnknownSide;
        public static int MaxDepth = 5;                 // Universal max depth
        public const int NSides = 3;                    // number of "sides" to market.
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                   Constructors                          ****
        // *****************************************************************
        //
        public MarketBase()
        {
            this.ChangedIndices = new List<int>[NSides];
            this.Price = new double[NSides][];
            this.Qty = new int[NSides][];
            for (int side = 0; side < NSides; ++side)
            {
                this.Price[side] = new double[MaxDepth];
                this.Qty[side] = new int[MaxDepth];
                this.ChangedIndices[side] = new List<int>();
            }//next side

            // Volumne
            Volume = new int[4];
            for (int i = 0; i < this.Volume.Length; ++i)
            {
                Volume[i] = -1;
            }

        }//Constructor
        //
        //
        #endregion//Constructors

        #region Public Methods
        // *****************************************************************
        // ****                   Public Methods                        ****
        // *****************************************************************
        //
        // *******************************************************
        // ****              GetChangedDepth                  ****
        // *******************************************************
        /// <summary>
        /// Finds first changed depth by side handing caller back array indexed 
        /// by market side
        /// </summary>
        /// <returns></returns>
        public int[] GetChangedDepth()
        {
            int[] depthChanged = new int[2];
            for (int i = 0; i < 2; i++)
            {
                if (ChangedIndices[i].Count > 0)
                    depthChanged[i] = ChangedIndices[i][0];     // the first change is always the closes to the market"
                else
                    depthChanged[i] = -1;                       // not changed on this side.
            }
            return depthChanged;

        }
        //
        //
        // *******************************************************
        // ****                   Clear                       ****
        // *******************************************************
        /// <summary>
        /// Called to clean out list of changed indices prior to recycling or before use.
        /// This is essential to ensuring corrected price updates.
        /// 
        /// If this is done properly none of the other arrays need to be cleared out, since
        /// they will be overwritten during the update process correctly.
        /// </summary>
        public void Clear()
        {
            for (int side = 0; side < NSides; ++side)
            {
                this.ChangedIndices[side].Clear();
            }//next side
            IsIncludesTimeAndSales = false;
        }
        //
        // *******************************************************
        // ****                  ClearVolume                  ****
        // *******************************************************
        /// <summary>
        /// Called if the market bases volume by side needs to be updated, this allows for volume to be aggregated
        /// correctly since often many trades come in at the same time. Since we reset to zero, we can them
        /// simply sum all the volume for a block of trades by side.
        /// </summary>
        public void ClearVolume()
        {
            Clear();
            for (int side = 0; side < Volume.Length; ++side)
            {
                this.Volume[side] = -1;
            }//next side
        }
        #endregion // Public methods


    }
}
