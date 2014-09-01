using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Strategies.ExecutionEngines.HedgeRules
{
    using UV.Lib.Hubs;
    using UV.Lib.Engines;
    using UV.Lib.IO.Xml;
    /// <summary>
    /// Rule to cross opposite side of markey when opposite qty is less than a certain qty.
    /// </summary>
    class CrossUnderThreshold : HedgeRule
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        private double[][] m_Prices;                // current mkt depth prices: m_Prices[mktSide][level]
        private int[][] m_Qtys;                     // current mkt depth Qtys: m_Qtys[mktside][level]
        private int m_AggressThreshold;              // threshold for which to trigger rule.
        private double m_TickSize;                  // used for comparing prices.
        #endregion// members

        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        public CrossUnderThreshold()
        {
        }
        //
        //
        protected override void SetupInitialize(IEngineHub myEngineHub, IEngineContainer engineContainer, int engineID, bool setupGui)
        {
            this.m_EngineName = string.Format("HedgeRule:CrossUnderThreshold:{0}", m_HedgeRuleManager.m_QuoterLeg.m_PriceLeg.InstrumentName);
            base.SetupInitialize(myEngineHub, engineContainer, engineID, setupGui);
        }
        //
        #endregion//Constructors

        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        public int AggressThreshold
        {
            get { return m_AggressThreshold; }
            set { m_AggressThreshold = value; }
        }
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
        /// <summary>
        /// Interface method to apply hedge logic.
        /// </summary>
        /// <param name="price"></param>
        /// <param name="mktSide"></param>
        /// <param name="newprice"></param>
        /// <returns></returns>
        public override bool ApplyHedgeRule(double price, int mktSide, out double newprice)
        {
            bool isRuleTriggered = false;                                                               // flag for rule being tripped.
            int oppSide = UV.Lib.Utilities.QTMath.MktSignToMktSide(UV.Lib.Utilities.QTMath.MktSideToMktSign(mktSide) * -1); //opposite side of market
            newprice = m_Prices[oppSide][0];
            if (m_Qtys[oppSide][0] < m_AggressThreshold && !UV.Lib.Utilities.QTMath.IsPriceEqual(price, newprice, m_TickSize))
            { // if we are bid, and the new ask qty is less than threshold change our buy price to current best ask
                m_Log.NewEntry(LogLevel.Minor, "ApplyHedgeRule: CrossUnderThreshold triggered, crossing {0} OnTriggerContinue={1}", newprice, m_isOnTriggerContinue);
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
        // ****                     Event Handlers                      ****
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
            s.AppendFormat(" AggressThreshold={0}", this.m_AggressThreshold);
            return s.ToString();
        }

        public override  List<IStringifiable> GetElements()
        {
            return base.GetElements();
        }

        public override void SetAttributes(Dictionary<string, string> attributes)
        {
            base.SetAttributes(attributes);
            int i;
            foreach (KeyValuePair<string, string> attr in attributes)
            {
                if (attr.Key == "AggressThreshold" && int.TryParse(attr.Value, out i))
                    this.m_AggressThreshold = i;
            }
        }
        public override void AddSubElement(IStringifiable subElement)
        {
            base.AddSubElement(subElement);
        }
        #endregion // IStringifiable
    }
}
