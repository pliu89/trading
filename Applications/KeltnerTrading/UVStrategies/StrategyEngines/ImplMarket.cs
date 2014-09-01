using System;
using System.Collections.Generic;
using System.Text;


namespace UV.Strategies.StrategyEngines
{
    using UV.Lib.MarketHubs;

    using UV.Lib.Utilities;
    using UV.Lib.BookHubs;
    using UV.Lib.Products;

    public class ImplMarket : Market
    {

        #region no Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        private ImplMarket()
            : base()
        {
            InitializeVariables(NMaxDepth);
        }
        //
        public static ImplMarket Create(string name)
        {
            ImplMarket instr = new ImplMarket();

            // Name this implied instrument.
            Product prod = new Product(string.Empty,name,ProductTypes.Synthetic);
            InstrumentName instrumentName = new InstrumentName(prod, name);
            instr.Name = instrumentName;

            //Exit
            return instr;
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


        #region no Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        /// <summary>
        /// This overloading creates an true implied market involving multiple instruments.
        /// </summary>
        /// 
        public void SetMarket(Book marketBook, List<PriceLeg> legs)
        {

            //
            // Inner market
            //
            for (int spreadSide = 0; spreadSide < 2; ++spreadSide)    // spread side, bid/ask
            {   // bid = where we can sell the spread, ask = where we can buy it
                int spreadSign = QTMath.MktSideToMktSign(spreadSide);     // bidside = +1, here!
                //
                // *** Inner market ****
                //
                int spreadLevel = 0;        // inner market for spread
                int leg = 0;                // First leg
                Market instr = marketBook.Instruments[legs[leg].MarketID];
                int legSide = QTMath.MktSignToMktSide(legs[leg].Weight * spreadSign);  // pos --> bidSide, neg --> askSide
                double spreadPrice = legs[leg].PriceMultiplier * instr.Price[legSide][spreadLevel];
                int spreadQty = Convert.ToInt32(Math.Floor(instr.Qty[legSide][spreadLevel] / Math.Abs(legs[leg].Weight)));
                leg++;
                // Remaining legs > 0                
                while (leg < legs.Count)
                {
                    instr = marketBook.Instruments[legs[leg].MarketID];
                    legSide = QTMath.MktSignToMktSide(legs[leg].Weight * spreadSign);
                    spreadPrice += legs[leg].PriceMultiplier * instr.Price[legSide][spreadLevel];
                    spreadQty = Math.Min(spreadQty, Convert.ToInt32(Math.Floor(instr.Qty[legSide][spreadLevel] / Math.Abs(legs[leg].Weight))));
                    //spreadQtyImp = Math.Min(spreadQtyImp, Convert.ToInt32(Math.Floor(instr.QtyImp[legSide][spreadLevel] / Math.Abs(legs[leg].Weight))));
                    leg++;
                }//next leg
                // Set inner-market prices.
                this.Price[spreadSide][spreadLevel] = spreadPrice;
                this.Qty[spreadSide][spreadLevel] = Math.Max(1, Math.Abs(spreadQty)) * Math.Sign(spreadQty);  // todo: this could be zero.
                //this.QtyImp[spreadSide][spreadLevel] = spreadQtyImp;
                //this.QtyImp[spreadSide][spreadLevel] = Math.Max(1, Math.Abs(spreadQtyImp)) * Math.Sign(spreadQtyImp);
                //this.QtyImp[spreadSide][spreadLevel] = Math.Max(1, Math.Abs(spreadQtyImp));// *spreadSign;                
                //
                // ****     Outer market    ****
                //
                if (legs.Count == 1)
                {
                    spreadLevel++;
                    leg = 0;                                // First leg
                    instr = marketBook.Instruments[legs[leg].MarketID];
                    while (spreadLevel < instr.DeepestLevelKnown)
                    {
                       
                        this.Price[spreadSide][spreadLevel] = instr.Price[spreadSide][spreadLevel] * legs[leg].PriceMultiplier;
                        this.Qty[spreadSide][spreadLevel] = instr.Qty[spreadSide][spreadLevel];  // todo: fix.
                        spreadLevel++;
                    }//next spreadLevel
                }
                if (spreadLevel > this.DeepestLevelKnown) { DeepestLevelKnown = spreadLevel; }
            }//next side
        }//SetMarket().
        
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
