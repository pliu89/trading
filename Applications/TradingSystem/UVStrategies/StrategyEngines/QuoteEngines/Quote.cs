using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Strategies.StrategyEngines.QuoteEngines
{
    /// <summary>
    /// </summary>
    public class Quote
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //    
        //
        // Owner:
        public PricingEngine PricingEngine = null;

        // Quote details
        public int IPrice = 0;
        public int Qty = 0;
        public QuoteReason Reason = QuoteReason.None;
        public StringBuilder FillAttributeStr = new StringBuilder();

        // Extra QuoteEngine information.
        public int Side = 0;
        public bool IsPriceListed = false;
        public double RawPrice = 0.0;

        // 

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public Quote()
        {
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
        public string FillAttribution()
        {
            return string.Format("{0} TradeReason={1}",this.FillAttributeStr,this.Reason);
        }
        public override string ToString()
        {
            return string.Format("{0}: {1} @ {2} {3}", this.PricingEngine.EngineName, Qty, IPrice, this.Reason);
        }
        //
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
     

    }//end class




}
