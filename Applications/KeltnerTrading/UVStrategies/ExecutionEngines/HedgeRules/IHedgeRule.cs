using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Strategies.ExecutionEngines.HedgeRules
{
    using UV.Lib.Engines;
    /// <summary>
    /// This interface will be implemented by all of the hedge rules. 
    /// It will accept inputs of price and side and output a new price and a bool
    /// as to wether to cotinue down the decision tree or simply execute the 
    /// rule.
    /// </summary>
    interface IHedgeRule : IEngine
    {
        bool ApplyHedgeRule(double price, int side, out double newprice);

        /// <summary>
        /// This is called once all the insturment details are available.  Allowing a hedge rule
        /// to find all the pointers it needs.
        /// </summary>
        void InitializeHedgeRule();

        /// <summary>
        /// unique execution number for each leg, determines the order in which the logic should be executed.
        /// </summary>
        int RuleNumber
        {
            get;
        }
        /// <summary>
        /// Manager holding this hedge rule
        /// </summary>
        HedgeRuleManager HedgeRuleManager
        {
            get;
            set;
        }
    }
}
