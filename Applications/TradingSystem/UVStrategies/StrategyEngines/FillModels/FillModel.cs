using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UV.Strategies.StrategyEngines.FillModels
{
    using UV.Lib.MarketHubs;
    using UV.Lib.Products;
    using UV.Lib.Fills;
    using UV.Strategies.StrategyEngines.QuoteEngines;
    using UV.Lib.Utilities;
    /// <summary>
    /// This is the most basic implementation of a fill model meant to create our own internal fills for
    /// backtesting or simulation purposes.  In the future this will become an interface with propietary models
    /// implemented else where.
    /// </summary>
    public static class FillModel
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
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
        //
        /// <summary>
        /// Caller would like to check if a quote has been filled.  This implementation simply looks to see if
        /// we are on the market or have bettered it. If so, we then create a fill.
        /// </summary>
        /// <param name="quote"></param>
        /// <param name="fill"></param>
        /// <param name="tickRounding"></param>
        /// <returns></returns>
        public static bool TryFill(Quote quote, out Fill fill, double tickRounding = 0.0001)
        {
            fill = null;
            int tradeSign = QTMath.MktSideToMktSign(quote.Side);
            double sameSidePrice = quote.PricingEngine.ImpliedMarket.Price[quote.Side][0]; 
            double otherSidePrice = quote.PricingEngine.ImpliedMarket.Price[QTMath.MktSideToOtherSide(quote.Side)][0];
            double orderPrice = tickRounding * Math.Round(quote.RawPrice / tickRounding);
            if ( tradeSign * (sameSidePrice - orderPrice) > 0 )
            {   // We are outside market
                return false;
            }
            else if (orderPrice >= otherSidePrice)
            {   // order has crossed market.  Fill it at market.
                fill = new Fill();
                fill.LocalTime = DateTime.Now;
                fill.ExchangeTime = DateTime.Now;
                fill.Price = otherSidePrice;
                fill.Qty = quote.Qty;
                return true;
            }
            else 
            {   // we are inside the market.
                fill = new Fill();
                fill.LocalTime = DateTime.Now;
                fill.ExchangeTime = DateTime.Now;
                fill.Price = orderPrice;
                fill.Qty = quote.Qty;
                return true;
            }
        }
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
