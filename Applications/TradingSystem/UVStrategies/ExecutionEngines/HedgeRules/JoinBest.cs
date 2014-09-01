using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Strategies.ExecutionEngines.HedgeRules
{
    using UV.Lib.Hubs;
    using UV.Lib.Engines;
    using UV.Lib.IO.Xml;

    /// <summary>
    /// Insantiates an instance of the class JoinBest which will continually 
    /// join the best price of the market on the side of the order.
    /// </summary>
    public class JoinBest : HedgeRule
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        private double[][] m_Prices;                // current mkt depth prices: m_Prices[mktSide][level]
        private double m_TickSize;                  // used for comparing prices.
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        public JoinBest() { }
        //
        //
        protected override void SetupInitialize(IEngineHub myEngineHub, IEngineContainer engineContainer, int engineID, bool setupGui)
        {
            this.m_EngineName = string.Format("HedgeRule:JoinBest:{0}", m_HedgeRuleManager.m_QuoterLeg.m_PriceLeg.InstrumentName);
            base.SetupInitialize(myEngineHub, engineContainer, engineID, setupGui);
        }
        #endregion//Constructors


        #region Properties
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
        public override void InitializeHedgeRule()
        {
            m_Prices = m_HedgeRuleManager.m_QuoterLeg.m_Market.Price;
            m_TickSize = m_HedgeRuleManager.m_QuoterLeg.InstrumentDetails.TickSize;
        }
        //
        //
        //
        public override bool ApplyHedgeRule(double price, int mktSide, out double newprice)
        {
            bool isRuleTriggered = false;                                   // flag for rule being tripped.
            int mktSign = UV.Lib.Utilities.QTMath.MktSideToMktSign(mktSide);
            newprice = m_Prices[mktSide][0];
            if ((mktSign * m_Prices[mktSide][0]) > (price * mktSign) && !UV.Lib.Utilities.QTMath.IsPriceEqual(price, newprice, m_TickSize))  // by multiplying by side, whatever side of the market we are on this should work.
            { // if we are bid, and the new bid price is greater than our price join best.  If we are offer, and the new offer price is less than join best.
                m_Log.NewEntry(LogLevel.Minor, "ApplyHedgeRule: JoinBest triggered, joining {0} OnTriggerContinue={1}", newprice, m_isOnTriggerContinue);
                isRuleTriggered = true;
            }
            else                                                            // rule wasn't triggered
                newprice = price;                                           // assign newprice to old price.
            return (!isRuleTriggered) || (isRuleTriggered && !m_isOnTriggerContinue); // if our rule didn't triger return true, if it was and the user wants to execute return false
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

        #region IStringifiable Implementation
        // *****************************************************************
        // ****                     IStringifiable                      ****
        // *****************************************************************
        //
        public override string GetAttributes()
        {
            StringBuilder s = new StringBuilder(base.GetAttributes());
            return s.ToString();
        }
        public override List<IStringifiable> GetElements()
        {
            return base.GetElements();
        }
        public override void SetAttributes(Dictionary<string, string> attributes)
        {
            base.SetAttributes(attributes);
        }
        public override void AddSubElement(IStringifiable subElement)
        {
            base.AddSubElement(subElement);
        }
        #endregion // IStringifiable
    }
}
