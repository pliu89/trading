using System;
using System.Collections.Generic;
using System.Text;

using UV.Lib.Utilities;
using UV.Lib.Products;

namespace UV.Lib.BookHubs
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
        // Important notes on volume array:
        //      Currently, "last" side volume is the session volume as reported by the exchange/TT.  BidSide,AskSide,Unknown are all aggregated from the TimeAndSales data
        //      subscription.  This is filtered to remove block trades, so the sum of these three sides will be less than or equal to the sessions volume.  If you would like
        //      total volume, sans block trades, you can simply sum these three filtered volumes. Keep in mind that these are only aggregated from the time the system, and the
        //      time and sales subscription is created.  Therefore they may not always look correct.  
        public int[] Volume = null;		        // Volume[side] sides defined below. Volume[BidSide] is volume on bid side, initiated by a seller!

        public const int NSides = 3;            // number of sides in arrays:  BidSide, AskSide, LastSide, (UnknownSide)...
        public const int NMaxDepth = 5;

        // Update Info
        public int[] BestDepthUpdated = null;  // for each update this is the best (closest to inside market) update that occured on that side.      
        public bool IsLastTickBestPriceChange = false;  // flag to alert subscribers that this was a top of book price change
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
        protected void InitializeVariables(int depthToInitialize)
        {
            Price = new double[NSides][];
            Qty = new int[NSides][];
            QtyImp = new int[NSides][];
            BestDepthUpdated = new int[NSides];
            for (int side = 0; side < NSides; ++side)
            {
                Price[side] = new double[depthToInitialize];
                Qty[side] = new int[depthToInitialize];
                QtyImp[side] = new int[depthToInitialize];

            }

            // Volume             
            Volume = new int[4];
            for (int i = 0; i < this.Volume.Length; ++i)
            {
                Volume[i] = 0;
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
        /// </summary>
        /// <param name="marketBase"></param>
        public void SetMarket(MarketBase marketBase)
        {
            // Basic price/quantity
            for (int side = 0; side < MarketBase.NSides; ++side)
            {
                DeepestLevelKnown = marketBase.DeepestLevelKnown;
                foreach (int level in marketBase.ChangedIndices[side])
                {
                    this.Price[side][level] = marketBase.Price[side][level];
                    this.Qty[side][level] = marketBase.Qty[side][level];
                    //// Update trade volumes, if changed.
                    //if (marketBase.Volume[side] > -1)
                      //  this.Volume[side] = marketBase.Volume[side];            // negative volumes are ignored
                }
            }


            if (!marketBase.IsIncludesTimeAndSales)
            { // this is just a normal update, and possibly has info on session volume
                if (marketBase.ChangedIndices[QTMath.LastSide].Count > 0)
                {
                    if (marketBase.Volume[QTMath.LastSide] > -1)
                        this.Volume[QTMath.LastSide] = marketBase.Volume[QTMath.LastSide];
                }
            }
            else 
            { // this update contains more detail volume info, 
                for (int side = 0; side < marketBase.Volume.Length; side++)
                { // for all sides, if we have an update, add our volume
                    this.Volume[side] = marketBase.Volume[side];
                }
            }

        }//end SetMarket().
        //
        //
        //
        /// <summary>
        /// Called to update the maket depth for one side of the market, for one level.
        /// Currently, volume and trade information is updated by hand, there is no 
        /// method call for volume/trades.
        /// </summary>
        /// <param name="mktSide"></param>
        /// <param name="level"></param>
        /// <param name="price"></param>
        /// <param name="qty"></param>
        /// <param name="TotalVolume"></param>
        /// <param name="impliedQty"></param>
        public void SetMarket(int mktSide, int level, double price, int qty, int TotalVolume, int impliedQty)
        {
            if (level >= this.Price[mktSide].Length) return;                    // level 0 == "top of book"
            if (level > DeepestLevelKnown) { DeepestLevelKnown = level; }
            this.Price[mktSide][level] = price;
            this.Qty[mktSide][level] = Math.Max(1, qty);
            this.QtyImp[mktSide][level] = Math.Max(1, impliedQty);
            if (TotalVolume != 0)                                            //
                this.Volume[QTMath.LastSide] = TotalVolume;
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
            aInstr.ID = this.ID;
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

        #region Events
        // *****************************************************************
        // ****                        Events                           ****
        // *****************************************************************
        //
        /// <summary>
        /// Event called anytime a tick occurs within the depth we currently care 
        /// about.
        /// </summary>
        public EventHandler MarketChanged;
        //
        public void OnMarketChanged()
        {
            if (MarketChanged != null)
            {
                MarketChanged(this, EventArgs.Empty);
            }
        }
        //
        //
        /// <summary>
        /// Event called anytime a top of book price change occurs
        /// about.
        /// </summary>
        public EventHandler MarketBestPriceChanged;
        //
        public void OnMarketBestPriceChanged()
        {
            if (MarketBestPriceChanged != null)
            {
                MarketBestPriceChanged(this, EventArgs.Empty);
            }
        }
        #endregion // Events
    }
}
