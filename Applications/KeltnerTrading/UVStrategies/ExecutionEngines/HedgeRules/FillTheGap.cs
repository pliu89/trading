using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Strategies.ExecutionEngines.HedgeRules
{
    using UV.Lib.Hubs;
    using UV.Lib.Engines;
    using UV.Lib.IO.Xml;

    public class FillTheGap : HedgeRule
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        private double[][] m_Prices;                // current mkt depth prices: m_Prices[mktSide][level]
        private int[][] m_Qtys;                     // current mkt depth Qtys: m_Qtys[mktside][level]
        private double m_TickSize;                  // bid/ask are this far apart when there is no gap
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        public FillTheGap() { }
        //
        //
        protected override void SetupInitialize(IEngineHub myEngineHub, IEngineContainer engineContainer, int engineID, bool setupGui)
        {
            this.m_EngineName = string.Format("HedgeRule:FillTheGap:{0}", m_HedgeRuleManager.m_QuoterLeg.m_PriceLeg.InstrumentName);
            base.SetupInitialize(myEngineHub, engineContainer, engineID, setupGui);
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
        public override void InitializeHedgeRule()
        {
            m_Prices = m_HedgeRuleManager.m_QuoterLeg.m_Market.Price;
            m_Qtys = m_HedgeRuleManager.m_QuoterLeg.m_Market.Qty;
            m_TickSize = m_HedgeRuleManager.m_QuoterLeg.InstrumentDetails.TickSize;
        }
        //
        //
        public override bool ApplyHedgeRule(double price, int mktSide, out double newprice)
        {
            bool isRuleTriggered = false;                                                               // flag for rule being tripped.
            
            int sign = UV.Lib.Utilities.QTMath.MktSideToMktSign(mktSide);
            double directionalTick = sign * m_TickSize;
            newprice = m_Prices[mktSide][0] + directionalTick; // directionalTick will be -Tick if this is Ask

            if(m_Prices[1][0] - m_TickSize > m_Prices[0][0] && !UV.Lib.Utilities.QTMath.IsPriceEqual(price, newprice, m_TickSize))
            { // if there is a gap on our side and our price doesn't already equal the gap price.
                m_Log.NewEntry(LogLevel.Minor, "ApplyHedgeRule: FillTheGap triggered, filing {0} OnTriggerContinue={1}", newprice, m_isOnTriggerContinue);
                isRuleTriggered = true;
            }
            else
                newprice = price;                                           // assign newprice to old price.
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
