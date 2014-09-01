using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;


namespace UV.Strategies.ExecutionEngines.HedgeRules
{
    using UV.Lib.OrderBooks;
    using UV.Lib.Utilities;
    using UV.Lib.Engines;
    using UV.Lib.IO.Xml;
    using UV.Lib.Hubs;
    using UV.Strategies.ExecutionHubs;
    using UV.Strategies.ExecutionEngines.Hedgers;
    /// <summary>
    /// This Hedge Rule Manager will aggregate hedge rules defined by the user and manage subscription 
    /// to  market events to decide on how to move hedge orders.
    /// </summary>
    public class HedgeRuleManager : Engine , IStringifiable
    {
        
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // external services
        public ExecutionEngines.OrderEngines.SpreaderLeg m_QuoterLeg;                             // instrument exec for this hedge leg.
        private OrderBook m_OrderBook;                                          // Order Book for this hedge instrument   
        public Hedger m_Hedger;                                                // Hedger of this leg.
        private LogHub m_Log;                                                   // our pointer to the strategy hub log.
                            
        // collections 
        private List<HedgeRule> m_HedgeRules = new List<HedgeRule>();                     // hedge rules to iterate through in order.
        private Dictionary<int, HedgeRule> m_PriorityToRule = new Dictionary<int, HedgeRule>(); // this collection is used at start up to order rules.
        private Dictionary<int, double> m_OrderIdToUserDefinedWorstPrice = new Dictionary<int, double>(); // memory of orders original prices
        private Dictionary<int, Order> m_ActiveOrders = new Dictionary<int, Order>();       // this is a temporary dictionary that will be overwritten with all active orders 

        
        // state flags
        private bool m_isManagerActive;                                         // is manager currently managing orders
        private bool m_IsReady;                                                 // is manager ready to start managing orders
        // temp objects
        private Order tmpOrder = new Order();                                   // this will constantly be overwritten.

        // user variables
        private int m_MaxPayUpTicks = 15;                                         // max pay up ticks to allow


        #endregion// members

        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        public HedgeRuleManager()
        {
        }
        //
        //
        protected override void SetupInitialize(IEngineHub myEngineHub, IEngineContainer engineContainer, int engineID, bool setupGui)
        {
            this.m_EngineName = string.Format("HedgeRuleManager:{0}", m_QuoterLeg.m_PriceLeg.InstrumentName);
            base.SetupInitialize(myEngineHub, engineContainer, engineID, setupGui);
            m_Hedger = m_QuoterLeg.m_Hedger;
            m_Log = ((ExecutionHub)myEngineHub).Log;
            ExecutionContainer execContainer = (ExecutionContainer)engineContainer;
            
            m_HedgeRules.OrderBy(x => x.RuleNumber); // sort our list by the rule number, so they get called in order!
               
            foreach (HedgeRule rule in m_HedgeRules)    // check that they are all unique, and add them to our container
            {
                if (!m_PriorityToRule.ContainsKey(rule.RuleNumber))
                {
                    m_PriorityToRule.Add(rule.RuleNumber, rule);
                    execContainer.TryAddEngine((Engine)rule);   // add my sub engines to the container.
                }
                else
                {
                    m_Log.NewEntry(LogLevel.Error, "HedgeRuleManager: Failed to find unique hedge rules");
                    throw new Exception("HedgeRuleManager: Failed to find unique hedge rules");
                }
            }
        }
        //
        //
        public override void SetupComplete()
        {
            base.SetupComplete();
            m_OrderBook = m_Hedger.m_OrderBook; // at this point our hedger has created his book
        }
        #endregion//Constructors

        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// From the original price a hedge order was submitted, this is the number
        /// of ticks total we are allowed to move the order
        /// </summary>
        public int MaxPayUpTicks
        {
            get { return m_MaxPayUpTicks; }
            set { m_MaxPayUpTicks = value; }
        }
        #endregion//Properties

        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        // ************************************************************
        // ****                RunHedgeLogic()                     ****
        // ************************************************************
        /// <summary>
        /// Called when all legs are ready and hedge rules can be set up, since some rely
        /// on insturment details to function properly.
        /// </summary>
        public void MarketInstrumentInitialized()
        {
            foreach (IHedgeRule rule in m_HedgeRules)
                rule.InitializeHedgeRule();
            m_IsReady = true;
        }
        //
        //
        // ************************************************************
        // ****                ManageHedgeOrders()                 ****
        // ************************************************************
        /// <summary>
        /// Caller would like the HedgeRuleManager to activate managing for orders on a side of the 
        /// market.
        /// </summary>
        public void ManageHedgeOrders()
        {
            if (!m_IsReady)     // hedger is unready to deal with orders!
                return;
            if (m_isManagerActive) // if we are active just force us to rerun our logic 
                RunHedgeLogic();
            else
            { // we need to run our logic and subscribe!
                m_isManagerActive = true;
                RunHedgeLogic(); // immediately run our hedge logic
                m_QuoterLeg.m_Market.MarketChanged += new EventHandler(Market_MarketChanged);   // subscribe to orders state changes for this leg.
            }
        }
        //
        //
        //
        //
        #endregion//Public Methods

        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        // *****************************************************************
        // ****                     RunHedgeLogic()                     ****
        // *****************************************************************
        /// <summary>
        /// Called after Instrument_TickChanged to update hedge prices of 
        /// orders under active management by the HedgeRuleManager.
        /// </summary>
        private void RunHedgeLogic()
        {
            double endingPrice;                                                                     // price we end up with after our rules are applied.
            double userDefinedWorstPrice;                                                           // price the user has defined as the worst possible price
            if (m_OrderBook.Count() == 0)
            {// we have no orders to manage
                m_isManagerActive = false;
                m_QuoterLeg.m_Market.MarketChanged -= new EventHandler(Market_MarketChanged);                // subscribe to orders state changes for this leg.
                m_Log.NewEntry(LogLevel.Minor, "HedgeRuleManager : {0} has no orders to manage, Unsunbscribing from market", m_QuoterLeg.InstrumentDetails.InstrumentName);
                return;
            }
            else // we have orders and need to manage them
            {
                for (int mktSide = 0; mktSide < 2; ++mktSide)
                { // each side of market
                    if (m_OrderBook.Count(mktSide) != 0)
                    {//we have orders on this side of the market
                        m_ActiveOrders.Clear();                                                     // these can not be recycled since the hedger could still be holding an order.
                        m_OrderBook.GetOrdersBySide(mktSide, ref m_ActiveOrders);                   // populate all orders for this side of the market
                        foreach (int id in m_ActiveOrders.Keys)
                        {
                            if (m_ActiveOrders.TryGetValue(id, out tmpOrder))
                            { // we can find the order
                                
                                int orderSign = QTMath.MktSideToMktSign(tmpOrder.Side);
                                if (!m_OrderIdToUserDefinedWorstPrice.ContainsKey(id))
                                {
                                    userDefinedWorstPrice = tmpOrder.PricePending + (orderSign * tmpOrder.TickSize * m_MaxPayUpTicks);
                                    m_OrderIdToUserDefinedWorstPrice.Add(id, userDefinedWorstPrice);
                                }
                                else
                                    userDefinedWorstPrice = m_OrderIdToUserDefinedWorstPrice[tmpOrder.Id];
                                
                                endingPrice = tmpOrder.PricePending;                                // assume we have no change to start.
                                foreach (IHedgeRule rule in m_HedgeRules)
                                { // apply our hedge rules
                                    bool isContinue = rule.ApplyHedgeRule(endingPrice, tmpOrder.Side, out endingPrice);  // hand the function the ending price and let it update it 
                                    if (!isContinue)                                                // we want to execute our rule immediately
                                        break;
                                }

                                if ((endingPrice * orderSign) > userDefinedWorstPrice * orderSign)  // if our ending price is worse than worse price, reassing it.
                                    endingPrice = userDefinedWorstPrice;

                                if (!QTMath.IsPriceEqual(endingPrice, tmpOrder.PricePending, tmpOrder.TickSize))
                                { // our price has been changed
                                    m_Hedger.UpdateHedgerOrderPrice(tmpOrder, endingPrice);         // call the hedger to change the order 
                                }
                            }
                        } // end foreach
                    } // end if 
                } // end mktside
            } // end else
        } // RunHedgeLogic()
        #endregion//Private Methods

        #region Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        // *****************************************************************
        // ****              Market_MarketChanged()                     ****
        // *****************************************************************
        private void Market_MarketChanged(object sender, EventArgs eventargs)
        {
            RunHedgeLogic();                    // run our hedge logic
        } //Instrument_TickChanged()
        #endregion//Event Handlers

        #region IStringifiable Implementation
        // *****************************************************************
        // ****                     IStringifiable                      ****
        // *****************************************************************
        //
        string IStringifiable.GetAttributes()
        {
            StringBuilder s = new StringBuilder(base.GetAttributes());
            s.AppendFormat(" MaxPayUpTicks={0}", this.m_MaxPayUpTicks);
            return s.ToString();
        }

        List<IStringifiable> IStringifiable.GetElements()
        {
            return base.GetElements();
        }

        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
            base.SetAttributes(attributes);
            int i;
            foreach (KeyValuePair<string, string> attr in attributes)
            {
                if (attr.Key == "MaxPayUpTicks" && int.TryParse(attr.Value, out i))
                    this.m_MaxPayUpTicks = i;
            }
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {
            base.AddSubElement(subElement);
            if (subElement is HedgeRule)
            {
                HedgeRule HedgeRule = (HedgeRule)subElement;
                HedgeRule.HedgeRuleManager = this;
                m_HedgeRules.Add(HedgeRule); // add now we will sort later!
            }
        }
        #endregion

    }
}
