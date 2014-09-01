using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Strategies.ExecutionEngines.HedgeRules
{
    using UV.Lib.Hubs;
    using UV.Lib.Engines;
    using UV.Lib.IO.Xml;

    public abstract class HedgeRule : Engine, IHedgeRule
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // external services
        protected LogHub m_Log;
        protected HedgeRuleManager m_HedgeRuleManager;
        
        //
        public bool m_isOnTriggerContinue;          //user defined flag.  If our hedge rule is triggerd, should we continue down decision tree or simply execute
        private int m_HedgeRuleExecutionOrderNumber;
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        protected override void SetupInitialize(IEngineHub myEngineHub, IEngineContainer engineContainer, int engineID, bool setupGui)
        {
            base.SetupInitialize(myEngineHub, engineContainer, engineID, setupGui);
            m_Log = ((Hub)myEngineHub).Log;
        }
        #endregion//Constructors

        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        public int RuleNumber
        {
            get { return m_HedgeRuleExecutionOrderNumber;  }
            set
            {
                m_HedgeRuleExecutionOrderNumber = value;
            }
        }
        //
        //
        public HedgeRuleManager HedgeRuleManager
        {
            get { return m_HedgeRuleManager; }
            set
            {
                m_HedgeRuleManager = value;
            }
        }
        #endregion//Properties

        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// This call signals the HedgeRule that is allowed to now grab all information
        /// from the instruments.  At this point all the details are guranteed to be there.
        /// </summary>
        public abstract void InitializeHedgeRule();
        //
        //
        public abstract bool ApplyHedgeRule(double price, int mktSide, out double newprice);
        //
        //
        //
        //
        #endregion//Public Methods

        #region IStringifiable Implementation
        // *****************************************************************
        // ****                     IStringifiable                      ****
        // *****************************************************************
        //
        public override string GetAttributes()
        {
            StringBuilder s = new StringBuilder(base.GetAttributes());
            s.AppendFormat(" OnTriggerContinue={0}", this.m_isOnTriggerContinue);
            s.AppendFormat(" RuleNumber={0}", this.m_HedgeRuleExecutionOrderNumber);
            return s.ToString();
        }

        public override List<IStringifiable> GetElements()
        {
            return base.GetElements();
        }

        public override void SetAttributes(Dictionary<string, string> attributes)
        {
            base.SetAttributes(attributes);
            bool isTrue;
            int i;
            foreach (KeyValuePair<string, string> attr in attributes)
            {
                if (attr.Key == "OnTriggerContinue" && bool.TryParse(attr.Value, out isTrue))
                    this.m_isOnTriggerContinue = isTrue;
                if (attr.Key == "RuleNumber" && int.TryParse(attr.Value, out i))
                    this.m_HedgeRuleExecutionOrderNumber = i;
            }
        }
        public override void AddSubElement(IStringifiable subElement)
        {
            base.AddSubElement(subElement);
        }
        #endregion // IStringifiable
    }
}
