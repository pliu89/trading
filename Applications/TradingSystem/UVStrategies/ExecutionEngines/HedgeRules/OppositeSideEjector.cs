using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UV.Strategies.ExecutionEngines.HedgeRules
{
    /// <summary>
    /// This is compleltely untested, written by oustide person
    /// </summary>
    public class OppositeSideEjector// : //IHedgeRule
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        private double[][] m_Prices;                // current mkt depth prices: m_Prices[mktSide][level]
        private double[][] m_Qtys;                  // current mkt depth Qtys: m_Qtys[mktside][level]
        public bool m_isOnTriggerContinue;          //user defined flag.  If our hedge rule is triggerd, should we continue down decision tree or simply execute
        private int m_ApplyRuleQtyThreshold;              // threshold for which to trigger rule.
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        //
        /// <summary>
        /// Insantiates an instance of the class CrossUnderThreshold which will continually 
        /// join the best price of the market on the side of the order.
        /// </summary>
        /// <param name="prices"></param>
        /// <param name="qtys"></param>
        /// <param name="OnTriggerContinue"></param>
        /// <param name="applyRuleQtyThreshold"></param>
        public OppositeSideEjector(double[][] prices, double[][] qtys, int applyRuleQtyThreshold, bool OnTriggerContinue)
        {
            m_Prices = prices;  // our pointer to the market prices.
            m_Qtys = qtys;      // our pointer to the market prices.
            m_ApplyRuleQtyThreshold = applyRuleQtyThreshold; //user defined threshold 
            m_isOnTriggerContinue = OnTriggerContinue;
        }
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
        public bool ApplyHedgeRule(double price, int mktSide, out double newprice)
        {
            bool isRuleTriggered = false;
            newprice = price;// flag for rule being tripped.

            int mktSign = UV.Lib.Utilities.QTMath.MktSideToMktSign(mktSide);
            mktSign *= -1;
            int oppSide = UV.Lib.Utilities.QTMath.MktSignToMktSide(mktSign); //opposite side of market
            if (m_Qtys[oppSide][0] < m_ApplyRuleQtyThreshold)
                isRuleTriggered = true;
            // if our rule didn't triger return true, 
            // if it did trigger and the user wants to execute, return false
            return (!isRuleTriggered) || (isRuleTriggered && !m_isOnTriggerContinue); 
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
