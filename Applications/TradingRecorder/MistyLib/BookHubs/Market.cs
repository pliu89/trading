using System;
using System.Collections.Generic;
using System.Text;

using Misty.Lib.Utilities;
using Misty.Lib.Products;

namespace Misty.Lib.BookHubs
{
    /// <summary>
    /// This object represents a single market for a particular Instrument.
    /// </summary>
    public class Market 
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        public int ID = -1;                     // index of this object in MarketHub.Instruments[] list.
        public InstrumentName Name;


        // Market
        public int DeepestLevelKnown = -1;      // Max depth so far observed for entire run! This is positive monotonic with time!
        public double[][] Price = null;
        public int[][] Qty = null;
        public int[][] QtyImp = null;
        public bool IsMarketGood = true;		// set by markethub to indicate whether all is well.

        // Trades
        public int[] Volume = new int[4];		// Volume[side] sides defined below. Volume[BidSide] is volume on bid side, initiated by a seller!


        public const int NSides = 3;            // number of sides in arrays:  BidSide, AskSide, LastSide, (UnknownSide)...
        public const int NMaxDepth = 5;

        
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************        
        public static Market Create(InstrumentName instrumentName) 
        {
            // Try to create the instrument.
            Market instr = new Market();
            instr.Name = instrumentName;

            instr.InitializeVariables(NMaxDepth);                   

            // Exit
            return instr;
        }//Create()
        //
        protected Market()
        {
        }
        //
        //
        // ****             Initialize Variables            ****
        //
        private void InitializeVariables(int depthToInitialize)
        {
            Price = new double[NSides][];
            Qty = new int[NSides][];
            QtyImp = new int[NSides][];
            for (int side = 0; side < NSides; ++side)
            {
                Price[side] = new double[depthToInitialize];
                Qty[side] = new int[depthToInitialize];
                QtyImp[side] = new int[depthToInitialize];
            }
        }//end InitializeVariables()
        //
        //
        //
        // ****         Create Copy()               ****
        //
        public static Market CreateCopy(Market instrToCopy)
        {
            Market newInstr = Market.Create(instrToCopy.Name);
            instrToCopy.CopyTo(newInstr);
            return newInstr;
        }//CreateCopy().
        //
        // 
        //
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        public double LastPrice
        {
            get { return Price[QTMath.LastSide][0]; }
        }
        //
        #endregion//Properties


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        // *****************************************************
        // ****                 SetMarket()                 ****
        // *****************************************************
        /// <summary>
        /// Called to update the maket depth for one side of the market, for one level.
        /// Currently, volume and trade information is updated by hand, there is no 
        /// method call for volume/trades.
        /// </summary>
        /// <param name="mktSide"></param>
        /// <param name="level"></param>
        /// <param name="price"></param>
        /// <param name="qty"></param>
        /// <param name="impliedQty"></param>
        public void SetMarket(int mktSide, int level, double price, int qty, int impliedQty)
        {
            if (level >= this.Price[mktSide].Length) return;                    // level 0 == "top of book"
            if (level > DeepestLevelKnown) { DeepestLevelKnown = level; }
            this.Price[mktSide][level] = price;
            this.Qty[mktSide][level] = Math.Max(1, qty);
            this.QtyImp[mktSide][level] = Math.Max(1, impliedQty);
        }//end SetMarket().
        //
        //
        //
        // ****             SetMarket               ****
        //
        /// <summary>
        /// This is the "copy" over-loading of SetMarket()
        /// </summary>
        /// <param name="instr"></param>
        private void SetMarket(Market instr)
        {
            // market - copy entire market
            for (int side = 0; side < this.Price.Length; ++side)
            {
                for (int level = 0; level < this.Price[side].Length; ++level)
                {
                    this.Price[side][level] = instr.Price[side][level];
                    this.Qty[side][level] = instr.Qty[side][level];
                    this.QtyImp[side][level] = instr.QtyImp[side][level];
                }
            }
            // Trades
            this.DeepestLevelKnown = instr.DeepestLevelKnown;
            for (int side = 0; side < this.Volume.Length; ++side)
                this.Volume[side] = instr.Volume[side];
            this.IsMarketGood = instr.IsMarketGood;
        }//end SetMarket()
        //
        //
        // ****             CopyTo()                ****
        //
        public virtual void CopyTo(Market aInstr)
        {            
            aInstr.SetMarket(this);
        }//end CopyTo()
        //
        //
        #endregion//Public Methods


        #region Public Output Methods
        // *****************************************************************
        // ****              Public Output Methods                      ****
        // *****************************************************************
        //
        //
        // ****                 ToString()              ****
        //
        public override string ToString()
        {
            string s = string.Format("{0}[{1}({2}) {3}({4})]", Name, this.Price[QTMath.BidSide][0].ToString(),
                this.Qty[QTMath.BidSide][0].ToString(), this.Price[QTMath.AskSide][0].ToString(), this.Qty[QTMath.AskSide][0].ToString());
            return s;
        }//ToString().
        //
        //
        //
        //
        #endregion//Public Methods


    }
}
