using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace UV.Strategies.ExecutionEngines.Hedgers
{
    using UV.Lib.OrderBooks;
    using UV.Lib.Products;
    using UV.Lib.Hubs;
    using UV.Lib.Engines;
    using UV.Lib.Fills;
    using UV.Lib.IO.Xml;
    
    using UV.Strategies;
    
    using UV.Strategies.ExecutionHubs;
    /// <summary>
    /// This class will be the main communication point for all hedging activity. It will known about all the hedgers(for each leg) and dictate their behavior. 
    /// </summary>
    public class HedgeManager : Engine, IStringifiable
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        // External services:
        private Dictionary<InstrumentName, int> m_InstrumentToId = new Dictionary<InstrumentName, int>();   // map: Instr --> legId
        protected ExecutionEngines.OrderEngines.Spreader m_Quoter = null;
        protected List<ExecutionEngines.OrderEngines.SpreaderLeg> m_QuoterLegs = null;
        private LogHub m_Log;
        public List<Hedger> m_Hedgers = null;                   // List of hedgers for each leg of the spread.

        // accounting for each hedger
        public double[] m_UnhedgedPartialCount;                 //partial bank for each instrument.
        public bool[] m_IsSratHung = new bool[2];               // is the stratetgy hung on either side of the market.
        public int[] m_PayUpTicks;                              // an array indexed by leg for pay up ticks to use while hedging.       

        // Hedging user defined variables
        public bool m_UseGTCHedge;                              // should our hedge orders be GTCs?
        public OrderTIF m_defaultTIF;                           // variable for our default TIF for all hedge orders.
        private double m_UserDefinedHedgeThreshold = .7;
        // temp debug logging
        private bool m_DebugLoggingOn = false;

        #endregion// members

        #region Constructors and Initialization
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        public HedgeManager()
        {
        }
        //
        public override void SetupBegin(IEngineHub myEngineHub, IEngineContainer engineContainer)
        { 
            base.SetupBegin(myEngineHub, engineContainer);
            m_Log = ((ExecutionHub)myEngineHub).Log;
            foreach (IEngine iEng in engineContainer.GetEngines())
            {// find our needed engine pointers
                if (iEng is ExecutionEngines.OrderEngines.Spreader)
                    m_Quoter = (ExecutionEngines.OrderEngines.Spreader)iEng;
            }
        }
        //
        /// <summary>
        /// At this point we know all the linkages between engines should be complete.  Therefore we can find our hedgers for each leg.
        /// </summary>
        public override void SetupComplete()
        {
            base.SetupComplete();
            if (m_Quoter != null)
            {
                m_UnhedgedPartialCount = new double[m_Quoter.m_SpreaderLegs.Count]; // partial count for each instrument.
                m_PayUpTicks = new int[m_Quoter.m_SpreaderLegs.Count];

                m_UseGTCHedge = m_Quoter.m_UseGTCHedge;         // allow user to send GTC hedges if desired.
                if (m_UseGTCHedge)
                    m_defaultTIF = OrderTIF.GTC;                // set our default to GTC
                else
                    m_defaultTIF = OrderTIF.GTD;                // set our default to GTD

                m_QuoterLegs = new List<ExecutionEngines.OrderEngines.SpreaderLeg>();
                m_Hedgers = new List<Hedger>();

                for (int leg = 0; leg < m_Quoter.m_SpreaderLegs.Count; ++leg)
                {// add a hedger for each instrument.
                    ExecutionEngines.OrderEngines.SpreaderLeg quoteLeg = m_Quoter.m_SpreaderLegs[leg];
                    m_Hedgers.Add(quoteLeg.m_Hedger);                                                      // add our new hedger to the list of hedgers
                    m_InstrumentToId.Add(quoteLeg.m_PriceLeg.InstrumentName, m_QuoterLegs.Count);
                    m_QuoterLegs.Add(quoteLeg);
                    quoteLeg.m_Hedger.UseGTCHedge = m_UseGTCHedge;                                         // assign default TIF to our hedger for this leg.
                    quoteLeg.m_Hedger.CompletelyFilled += new EventHandler(Hedger_CompletelyFilled);       // susbsribe to events for a hedger being completely filled.
                }
                UseGTCHedge = m_UseGTCHedge;
            }
            else
                throw new Exception("HedgeManager Couldn't Find Quoter");
        }
        //
        //       
        #endregion//Constructors

        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        public bool UseGTCHedge
        {
            get { return m_UseGTCHedge; }
            set
            {
                m_UseGTCHedge = value;
                if (m_UseGTCHedge)
                    m_defaultTIF = OrderTIF.GTC;                // set our default to GTC
                else
                    m_defaultTIF = OrderTIF.GTD;                // set our default to GTD
                foreach (Hedger hedger in m_Hedgers)            // send to our hedgers.
                {
                    hedger.UseGTCHedge = value;
                }
            }
        }
        //
        /// <summary>
        /// Flag for strategy being hung on either side of the market. Mainyl for GUI display.
        /// </summary>
        public bool IsHung
        {
            get { return (m_IsSratHung[Order.BuySide] || m_IsSratHung[Order.SellSide]); }
        }
        //
        /// <summary>
        /// The fraction of a contract we need to have exposure to in order to send a hedge order.
        /// </summary>
        public double HedgeThreshold
        {
            get { return m_UserDefinedHedgeThreshold; }
            set { m_UserDefinedHedgeThreshold = value; }
        }

        #endregion//Properties

        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        public virtual void HedgeFill(int quoteInstrId, Fill aFill)
        {
            // Get information about quote leg
            ExecutionEngines.OrderEngines.SpreaderLeg quoteLeg = m_QuoterLegs[quoteInstrId];
            int quoteFillQty = aFill.Qty;
            double quoteFillPrice = aFill.Price;
            double currentAggressiblePrices = 0;
            bool isAggressible = false;

            int[] neededHedgeQty = CalculateNeededHegeQty(quoteInstrId, quoteFillQty);                                  // get our hedge QTY array 
            int stratSide = UV.Lib.Utilities.QTMath.MktSignToMktSide(Math.Sign(quoteFillQty * quoteLeg.m_PriceLeg.Weight));  // find what side of the spread we got filled on 
            if (m_IsSratHung[stratSide] == false)                                                                       // fire event for our hang state changing.
            {
                m_IsSratHung[stratSide] = true;                                                                         // We are not hedged on this side.
                OnHangStateChanged();
            }
            double stratPrice = m_Quoter.m_StrategyWorkingPrice[stratSide];                                             // find what price we are working in the spread
            if (m_QuoterLegs.Count > 2)                                                                                // if we are working than 2 or more legs
            { //we need to check the pricing to see if our prices are still at market.
                for (int i = 0; i < m_QuoterLegs.Count; i++)
                { // iterate through other legs to see what pricing we can pick up if we agress
                    if (i == quoteInstrId)                                                                              //we already are filled here so this price is already known
                        continue;
                    currentAggressiblePrices += m_QuoterLegs[i].m_PriceLeg.PriceMultiplier *
                        m_QuoterLegs[i].m_LeanablePrices[UV.Lib.Utilities.QTMath.MktSignToMktSide(stratSide * m_QuoterLegs[i].m_PriceLeg.Weight)];
                }
                // so now we have the price WITHOUT the "quote" leg that we are filled on.  So we add
                // that in and now we can compare the price we can get now with the price we wanted.
                currentAggressiblePrices += (quoteFillPrice * quoteLeg.m_PriceLeg.PriceMultiplier);
                if (stratSide == UV.Lib.Utilities.QTMath.BidSide)
                    isAggressible = currentAggressiblePrices <= stratPrice;
                else
                    isAggressible = currentAggressiblePrices >= stratPrice;
            }
            // Find Hedge Prices and Send Hedge Orders 
            for (int hedgeLegId = 0; hedgeLegId < m_QuoterLegs.Count; hedgeLegId++)
            {
                //iterate through each instrument 
                if (hedgeLegId == quoteInstrId)                                                                   // no need to hedge our quote instrument
                    continue;
                int hedgeQty = neededHedgeQty[hedgeLegId];                                                        // find the qty we need to sell
                if (hedgeQty != 0)
                {
                    int orderSide = UV.Lib.Utilities.QTMath.MktSignToMktSide(hedgeQty);
                    int leanSide = UV.Lib.Utilities.QTMath.MktSignToMktSide(hedgeQty * -1);
                    ExecutionEngines.OrderEngines.SpreaderLeg hedgeLeg = m_QuoterLegs[hedgeLegId];
                    double hedgePrice = 0;
                    if (m_QuoterLegs.Count == 2)
                    { // if our spread is 2 legged we can be very explicit with the pricing
                        hedgePrice = (stratPrice - (quoteFillPrice * quoteLeg.m_PriceLeg.PriceMultiplier)) / hedgeLeg.m_PriceLeg.PriceMultiplier; // without pay up ticks
                    }
                    else
                    { // we have to do something a bit more creative since we have more than 2 legs.
                        // calculate for more than 2 legs
                        // three possbile cases.
                        // 1. the price we want we can get. send hedge orders immediately
                        // 2. we can get a price BETTER than what we want. send hedge orders immediately
                        // 3. we can get only a worse price than the one we want so we need adjust our hedges 
                        if (isAggressible)
                        { // situation 1 or 2
                            hedgePrice = m_QuoterLegs[hedgeLegId].m_LeanablePrices[leanSide];
                        }
                        else
                        {// TODO: create logic in regards to dealing with three legged hangs.
                            hedgePrice = m_QuoterLegs[hedgeLegId].m_LeanablePrices[leanSide];
                        }
                    }
                    m_Hedgers[hedgeLegId].SubmitToHedger(hedgeLeg, hedgeQty, hedgePrice);                       // submit our hedge orders.
                }

            }
        }
        //
        #endregion//Public Methods

        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// Returns an array index by instrIds containing the needed hedge qty 
        /// for each instrument based upon the fill qty.
        /// </summary>
        /// <param name="instrId"></param>
        /// <param name="filledQty"></param>
        /// <returns></returns>
        protected int[] CalculateNeededHegeQty(int instrId, int filledQty)
        {
            double quoteLegRatio = m_QuoterLegs[instrId].m_PriceLeg.Weight;
            int[] NeededHedgeQty = new int[m_QuoterLegs.Count];

            double unroundedNeededHedgeQty = 0;
            int roundedNeededHedgeQty = 0;

            double partialQtyFromFill = 0;  // incomplete contracts needing to be hedged from filil 
            double totalPartialSum = 0;     // sum of unhedge partials from past fills and  from this fill 

            double stratQty = (double)filledQty / (double)quoteLegRatio;

            // Logic for finding needed hedge QTY:  If we are buying the spread,
            // and our ratio is 7:-2:-3 in a three legged spread.  if we got filled 
            // selling five in the first leg our filledQTY = -5, we weould first calculate the ratio
            // for that leg from the fill leg
            // (-2/7) * -5 =  1.42 contract need to be hedged long in the second leg.
            // lets say our partial bank has .3 long contracts to be hedged in it 
            // so now we have .72 of a contract remaining which is over our user defined threshold of .7
            // therefore we need to hedge that contract, and our partial bank should now be -.18 (totalPartialSum - hedgedamount)
            //
            // another example would be the opposite with which our remainder is .72 from our fill for instance,
            // our partial bank is -.22 so our long contracts needing hedged is .55 so no action taken 
            // and our partial bank should now be .55 (totalPartialSum - hedgeamount)
            //
            // The logic here has one flaw that I can see so far in which each treats each quote leg individually,
            // and wouldn't count a quote partial in one leg as offestting what is needing to be hedge in another.
            // maybe there is a smart way to do this.

            for (int i = 0; i < m_QuoterLegs.Count; i++)
            {
                if (i == instrId)
                { // if the instr is the same as the fill isntr, there is nothing to be done
                    NeededHedgeQty[i] = 0; // no qty to be hedged
                }
                else
                {
                    unroundedNeededHedgeQty = stratQty * (double)m_QuoterLegs[i].m_PriceLeg.Weight;                       // Find the needed number of contracts
                    if (m_DebugLoggingOn)
                    {
                        m_Log.NewEntry(LogLevel.Minor, "startQty = " + stratQty);
                        m_Log.NewEntry(LogLevel.Minor, "m_Instruments[i].m_LegRatio = " + m_QuoterLegs[i].m_PriceLeg.Weight);
                        m_Log.NewEntry(LogLevel.Minor, "unroundedNeededHedgQTY = " + unroundedNeededHedgeQty);
                    }
                    roundedNeededHedgeQty = (int)unroundedNeededHedgeQty;           // round to the smallest whole contract
                    if (m_DebugLoggingOn)
                        m_Log.NewEntry(LogLevel.Minor, "roundedNeededHedgeQty = " + roundedNeededHedgeQty);
                    partialQtyFromFill = unroundedNeededHedgeQty - roundedNeededHedgeQty;                           // find remaining partial from fill 
                    if (m_DebugLoggingOn)
                        m_Log.NewEntry(LogLevel.Minor, "partialQtyFromFill = " + partialQtyFromFill);
                    totalPartialSum = partialQtyFromFill + m_UnhedgedPartialCount[i];                               // add partial contracts from bank to see if we have enough to hedge them
                    if (m_DebugLoggingOn)
                        m_Log.NewEntry(LogLevel.Minor, "totalPartialSum = " + totalPartialSum);
                    if (Math.Abs(totalPartialSum) >= m_UserDefinedHedgeThreshold)
                    { // if we can hedge extra QTY, add the full contract to the roundedNeededHedgeQty, then adjust partial bank 
                        roundedNeededHedgeQty = roundedNeededHedgeQty + Math.Sign(unroundedNeededHedgeQty);
                        if (m_DebugLoggingOn)
                            m_Log.NewEntry(LogLevel.Minor, "Adding Partial To Hedge QTY = " + Math.Sign(unroundedNeededHedgeQty));
                        m_UnhedgedPartialCount[i] = totalPartialSum - Math.Sign(unroundedNeededHedgeQty);
                    }
                    else
                    { // there isn't enough qty to hedge a full contract 
                        m_UnhedgedPartialCount[i] += partialQtyFromFill;                                            // we still need to do some accounting with the partials, adding the partial qty from the fill
                    }
                    NeededHedgeQty[i] = roundedNeededHedgeQty;                                                      // assign to our array
                    if (m_DebugLoggingOn)
                        m_Log.NewEntry(LogLevel.Minor, "NeededHedgeQty = " + roundedNeededHedgeQty);
                }
            }
            return NeededHedgeQty;
        }

        //
        //
        //
        //
        #endregion//Private Methods

        #region Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        // *******************************************************
        // ****         Hedger_OrdersCompletelyFilled()       ****
        // *******************************************************
        private void Hedger_CompletelyFilled(object sender, EventArgs eventArgs)
        {
            FillEventArgs fillEventArgs = (FillEventArgs)eventArgs;
            // Get information about who was filled.
            int legId;
            if (!m_InstrumentToId.TryGetValue(fillEventArgs.InstrumentName, out legId))
                return;
            Fill fill = fillEventArgs.Fill;
            int stratSign = Math.Sign(fill.Qty * m_Quoter.m_LegRatios[legId]);
            int stratSide = UV.Lib.Utilities.QTMath.MktSignToMktSide(stratSign);

            // Update my IsHung status
            bool prevHungState = m_IsSratHung[stratSide];
            m_IsSratHung[stratSide] = false;
            for (int leg = 0; leg < m_Hedgers.Count; ++leg)
            {
                if (m_Hedgers[leg].m_IsLegHung[stratSide])          // hang flags for each instrument are divided by side of the strategy not the instrumetn.
                {
                    m_IsSratHung[stratSide] = true;
                    break;
                }
            }
            if (prevHungState != m_IsSratHung[stratSide])           // if we have changed states
            {
                m_Quoter.OnQuoterStateChange();                     //  we need to update the quoter. 
                OnHangStateChanged();                               // and call our subscribers.
            }
        }

        #endregion//Event Handlers

        #region Events
        // *****************************************************************
        // ****                         Events                          ****
        // *****************************************************************
        //
        //
        public event EventHandler HungStateChanged;
        //
        //
        /// <summary>
        /// Whenever our hung state has changed call our subscribers.
        /// </summary>
        protected void OnHangStateChanged()
        {
            if (this.HungStateChanged != null)
            {
                this.HungStateChanged(this, EventArgs.Empty);
            }
        }
        #endregion //Events

        #region Istringifiable implementation
        // *****************************************************************
        // ****                     Istringifiable                      ****
        // *****************************************************************
        string IStringifiable.GetAttributes()
        {
            StringBuilder s = new StringBuilder(base.GetAttributes());
            s.AppendFormat(" UseGTCHedge={0}", this.m_UseGTCHedge);
            s.AppendFormat(" HedgeThreshold={0}", this.HedgeThreshold);
            return s.ToString();
        }
        List<IStringifiable> IStringifiable.GetElements()
        {
            return base.GetElements();
        }

        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
            base.SetAttributes(attributes);
            double x;
            bool isTrue;
            foreach (KeyValuePair<string, string> attr in attributes)
            {
                if (attr.Key == "UseGTCHedge" && bool.TryParse(attr.Value, out isTrue))
                    this.m_UseGTCHedge = isTrue;
                if (attr.Key == "HedgeThreshold" && double.TryParse(attr.Value, out x))
                    this.HedgeThreshold = x;
                
            }
        }

        void IStringifiable.AddSubElement(IStringifiable subElement)
        {
            base.AddSubElement(subElement);
        }
        #endregion // Istringifiable
    }
}
