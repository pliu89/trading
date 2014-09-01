using System;
using System.Collections.Generic;
using System.Text;

using Misty.Lib.Utilities;
using Misty.Lib.Products;

namespace Misty.Lib.BookHubs
{
    public class MarketInstrument 
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        public int ID = -1;                 // index of this object in MarketHub.Instruments[] list.
        public string Name;


        // Ticks
        public double TickSize;             // 0.5       demicimal value of each tick.
        public double TickValue;            // $12.50    contract $ amt per quoted tick.

        // Market
        public int DeepestLevelKnown = -1;      // Max depth so far observed for entire run! This is positive monotonic with time!
        public double[][] Price = null;
        public int[][] Qty = null;
        public int[][] QtyImp = null;
        public bool IsMarketGood = true;		// set by markethub to indicate whether all is well.

        // Trades
        //public double LastPrice = 0;
        public int[] Volume = new int[4];		// Volume[side] sides defined below. Volume[BidSide] is volume on bid side, initiated by a seller!


        public const int NSides = 3;            // number of sides in arrays:  BidSide, AskSide, LastSide, (UnknownSide)...
        public const int NMaxDepth = 5;

        
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************        
        public static MarketInstrument Create(string fullName, double tickSize, double tickValue, string serverName = "") 
        {
            // Try to create the instrument.
            MarketInstrument instr = new MarketInstrument();
            instr.Name = fullName;

            // Tick information
            instr.TickSize = tickSize;
            instr.TickValue = tickValue;

            instr.InitializeVariables(NMaxDepth);                   

            // Exit
            return instr;
        }//Create()
        //
        protected MarketInstrument()
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
        public static MarketInstrument CreateCopy(MarketInstrument instrToCopy)
        {
            MarketInstrument newInstr = MarketInstrument.Create(instrToCopy.Name,instrToCopy.TickSize, instrToCopy.TickValue);
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
        private void SetMarket(MarketInstrument instr)
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
        public virtual void CopyTo(MarketInstrument aInstr)
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
        /*
        public string ToLongString()
        {
            string s = string.Format("{0}[{1}({2}) {3}({4}) Vol={5}({6}/{7})]",
                this.Name, this.BestBid.ToString(),
                this.BestBidQty.ToString(), this.BestAsk.ToString(), this.BestAskQty.ToString(),
                this.Volume[LastSide].ToString(), this.Volume[BidSide].ToString(), this.Volume[AskSide].ToString());
            return s;
        }//ToString().
        */
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




    }
}
