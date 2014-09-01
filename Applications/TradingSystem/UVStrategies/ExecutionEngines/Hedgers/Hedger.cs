using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace UV.Strategies.ExecutionEngines.Hedgers
{
    using UV.Lib.OrderBooks;
    using UV.Strategies.StrategyEngines;
    using UV.Strategies;
    using UV.Lib.Hubs;
    using UV.Lib.Engines;
    using UV.Lib.Fills;
    using UV.Lib.IO.Xml;

    using UV.Strategies.ExecutionHubs;
    /// <summary>
    /// This is a class for hedging.  Each leg will have one that is 
    /// "owned" by the hedge manager. 
    /// </summary>
    public class Hedger : Engine , IStringifiable
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        // External services:
        public ExecutionEngines.OrderEngines.SpreaderLeg m_SpreaderLeg = null;
        public ExecutionEngines.OrderEngines.Spreader m_Spreader = null;
        private ExecutionEngines.HedgeRules.HedgeRuleManager m_HedgeRuleManager = null;
        private LogHub m_Log;

        public double m_UnhedgedPartialCount;                       // Partial count for this instrument.
        public OrderBook m_OrderBook;                               // Order Book for this instrument.
        private int m_PayUpTicks;                                   // pay up ticks (positive or negative)
        private OrderTIF m_defaultTIF;                              // variable for our default TIF for all hedge orders.

        //State Flags
        public bool[] m_IsLegHung = new bool[2];                    // state flag for this leg being hung on either side of the STRATEGY
        private int m_InternalLegId;                                // id of leg in our quoter.
        private int m_NextOrderId;                                  // a way of tagging each unqiue hedge order 

        private Dictionary<int, Order> m_PendingHedgeOrders = new Dictionary<int, Order>();     // hedge orders waiting for updates indexed by a unique user tag 
        private List<Order> m_OrderWorkSpace = new List<Order>();                               // workspace for temp order storage-clear before each use!
        #endregion// members

        #region Constructors and Initialization
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        public Hedger() : base()
        {
            m_UnhedgedPartialCount = 0;                                                     // start with no partials
            m_NextOrderId = 0;                                                              // and no hedge orders yet.
            m_PayUpTicks = new int();                                                       // the HedgerManager should set on instantiation.
        }
        //
        //
        protected override void SetupInitialize(IEngineHub myEngineHub, IEngineContainer engineContainer, int engineID, bool setupGui)
        {
            m_EngineName = string.Format("Hedger:{0}", m_SpreaderLeg.m_PriceLeg.InstrumentName);  // called before base to change gui names!
            base.SetupInitialize(myEngineHub, engineContainer, engineID, setupGui);
        }
        //
        public override void SetupBegin(IEngineHub myEngineHub, IEngineContainer engineContainer)
        {
            base.SetupBegin(myEngineHub, engineContainer);
            m_Log = ((ExecutionHub)myEngineHub).Log;
            foreach (IEngine iEng in engineContainer.GetEngines())
            {// find our needed engine pointers
                if (iEng is ExecutionEngines.OrderEngines.Spreader)
                    m_Spreader = (ExecutionEngines.OrderEngines.Spreader)iEng;
            }

            if (m_Spreader == null)
                throw new Exception("Hedger Couldn't Find Quoter");

            m_OrderBook = m_Spreader.m_ExecutionListener.CreateOrderBook(m_SpreaderLeg.m_PriceLeg.InstrumentName, m_SpreaderLeg.DefaultAccount);      // create new order book for this leg's hedge fulls   
            m_InternalLegId = m_Spreader.m_SpreaderLegs.IndexOf(m_SpreaderLeg);                                         // set our internal id.
            m_Spreader.MarketsReadied += new EventHandler(Quoter_MarketsReadied);                                       // we will Finish our set up once we get this call.
            m_HedgeRuleManager = m_SpreaderLeg.m_HedgeRuleManager;
        }
        //
        //
        //       
        #endregion//Constructors

        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// Should this legs send GTC orders for hedgle legs
        /// </summary>
        public bool UseGTCHedge
        {
            get { return (m_defaultTIF == OrderTIF.GTC); }
            set
            {
                if (value)
                    m_defaultTIF = OrderTIF.GTC;                // set our default to GTC
                else
                    m_defaultTIF = OrderTIF.GTD;                // set our default to GTD
            }
        }
        /// <summary>
        /// Number of ticks to send hedge orders from our original lean price. Positive for worse price
        /// negative for better price.
        /// </summary>
        public int PayUpTicks
        {
            get { return m_PayUpTicks; }
            set { m_PayUpTicks = value; }
        }
        #endregion//Properties

        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        // *****************************************************
        // ****              SubmitToHedger                 ****
        // *****************************************************
        /// <summary>
        /// Hedge Manager will call this method asking the hedger to take care of hedging. 
        /// </summary>
        /// <param name="instr"></param>
        /// <param name="qty">signed qty for hedge order</param>
        /// <param name="price"></param>
        public void SubmitToHedger(ExecutionEngines.OrderEngines.SpreaderLeg instr, int qty, double price)
        {
            if (!instr.Equals(m_SpreaderLeg))
            { // check that we are submitting to the correct instrument
                m_Log.NewEntry(LogLevel.Error, "Hedger.SubmitToHedger: Wrong Instrument Has Been Submitted To Hedger");
                return;
            }
            int stratSign = Math.Sign(qty * m_Spreader.m_LegRatios[m_InternalLegId]);
            int stratSide = UV.Lib.Utilities.QTMath.MktSignToMktSide(stratSign);

            if (m_SpreaderLeg.IsMarketGood)
            { // make sure the market is okay before we send an order.
                Order hedgeOrder;
                int orderSign = Math.Sign(qty);
                int orderSide = UV.Lib.Utilities.QTMath.MktSignToMktSide(orderSign);
                price += orderSign * m_PayUpTicks * m_SpreaderLeg.InstrumentDetails.TickSize;                    // if we are using negative pay up ticks we should hedge a better price
                int iPrice = orderSign * (int)System.Math.Floor(orderSign * price / m_SpreaderLeg.InstrumentDetails.TickSize);    // integerized price, using "safe" rounding
                m_Spreader.m_ExecutionListener.TryCreateOrder(instr.m_PriceLeg.InstrumentName, orderSide,
                                         (int)(price / m_SpreaderLeg.InstrumentDetails.TickSize), qty, out hedgeOrder);
                hedgeOrder.OrderTIF = m_defaultTIF;
                hedgeOrder.UserDefinedTag = m_NextOrderId;                                                              // we are tagging each order so when we modify it we can identify the original order it stemmed from.
                hedgeOrder.OrderReason = OrderReason.Hedge;
                m_Spreader.m_ExecutionListener.TrySubmitOrder(m_OrderBook.BookID, hedgeOrder);
                m_HedgeRuleManager.ManageHedgeOrders();
                m_Spreader.m_RiskManager.m_TotalNumberOfQuotes++;                                                         // increment to count all quotes.
            }
            m_IsLegHung[stratSide] = true;                                          // we have submitted a new hedge order and until we get the fill we can assume we are in a hung state
            m_NextOrderId++;                                                        // increment our uniquie hedge count
        } // SubmitToHedger()
        //
        //
        // *****************************************************
        // ****            UpdateHedgerOrderPrice           ****
        // *****************************************************
        /// <summary>
        /// Caller would like to change the price of a hedge order, most likely called by HedgeRuleManager.
        /// Method will atttempt to use cancel replace is possible.  If not it will attempt to delete the old order
        /// set the new order up for submission when the old order is confirmed to be cancelled.
        /// </summary>
        /// <param name="order"></param>
        /// <param name="price"></param>
        public void UpdateHedgerOrderPrice(Order order, double price)
        {
            m_PendingHedgeOrders.Remove(order.UserDefinedTag);                                                                   // suceeding in updated, make sure we clear out any pending orders
            if (!m_Spreader.m_ExecutionListener.TryChangeOrderPrice(order, (int)(price / m_SpreaderLeg.InstrumentDetails.TickSize), order.OriginalQtyPending))
            { // Error. Failed to submit a cancel replace.  Use delete instead.
                //m_PendingHedgeOrders.Add(desiredOrder.UserDefinedTag, desiredOrder);                                          // After quoteOrder delete is confirmed, this order will be sent.
                m_Log.NewEntry(LogLevel.Warning, "UpdateHedgerOrderPrice: Failed to moidfy order {0} - This Needs To Be Worked On!", order);
            }
        } // UpdateHedgerOrderPrice
        //
        //
        #endregion//Public Methods

        #region no Private Methods

        #endregion//Private Methods

        #region Event Triggers
        // *****************************************************************
        // ****                     Event Triggers                      ****
        // *****************************************************************
        //
        //
        // ****              CompletelyFilled               ****
        //
        public event EventHandler CompletelyFilled;
        //
        /// <summary>
        /// After a fill we check to see if we have no more outstanding hedge orders for a given leg 
        /// this event is fired when this is true.
        /// </summary>
        public void OnCompletelyFilled(EventArgs orderStatusEventArgs)
        {
            if (this.CompletelyFilled != null)
            {
                this.CompletelyFilled(this, orderStatusEventArgs);
            }
        }
        //
        //
        //
        #endregion //Event Triggers

        #region Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        // *******************************************************
        // ****             Quoter_MarketsReadied()           ****
        // *******************************************************
        /// <summary>
        /// Called when the quoters get the call that his markets are all good and we can subscribe
        /// to the events we need.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void Quoter_MarketsReadied(object sender, EventArgs eventArgs)
        {
            m_OrderBook.OrderFilled += new EventHandler(OrderBook_OrderFilled);
            m_OrderBook.OrderStateChanged += new EventHandler(OrderBook_OrderStateChanged);
            m_HedgeRuleManager.MarketInstrumentInitialized();
        }
        //
        //
        //
        //
        // *******************************************************
        // ****             OrderBook_OrderFilled()           ****
        // *******************************************************
        /// <summary>
        /// Called when an order in the hedge order book has been filled.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void OrderBook_OrderFilled(object sender, EventArgs eventArgs)
        {
            FillEventArgs fillEventArgs = (FillEventArgs)eventArgs;
            Fill fill = fillEventArgs.Fill;
            int internalLegId = m_Spreader.m_InstrumentToInternalId[fillEventArgs.InstrumentName];

            int stratSign = Math.Sign(fill.Qty * m_Spreader.m_LegRatios[internalLegId]);
            int stratSide = UV.Lib.Utilities.QTMath.MktSignToMktSide(stratSign);

            // Update my IsLegHung status
            bool prevHungState = m_IsLegHung[stratSide];        // Keep prev state so we know it has changed.
            m_IsLegHung[stratSide] = false;
            m_Spreader.AddHedgeFill(stratSide, internalLegId, fill);      // this needs to get updated prior to us trigger OnCompletelyFilled
            int legSide = UV.Lib.Utilities.QTMath.MktSignToMktSide(m_Spreader.m_LegRatios[m_InternalLegId] * stratSign);
            if (m_OrderBook.Count(legSide) != 0)
                m_IsLegHung[stratSide] = true;                  // we have no outstanding hedge orders
            else
                OnCompletelyFilled(fillEventArgs);
        }
        //
        //
        //
        // *******************************************************
        // ****           OrderBook_OrderStateChanged()       ****
        // *******************************************************
        private void OrderBook_OrderStateChanged(object sender, EventArgs eventArgs)
        {
            OrderEventArgs orderEventArg = (OrderEventArgs)eventArgs;
            Order updatedOrder = orderEventArg.Order;               // Get information about instrument and order.
            int side = orderEventArg.Order.Side;
            Order newOrder;                                         // Check to see whether we have a pending update to push to the market and we aren't working an order with our tag
            if (m_PendingHedgeOrders.TryGetValue(updatedOrder.UserDefinedTag, out newOrder))
            {// we have an order pending to deal with
                if (updatedOrder.OrderStateConfirmed == OrderState.Dead)
                { // the order that got updated is dead
                    m_OrderWorkSpace.Clear();
                    m_OrderBook.GetOrdersByUserDefinedTag(side, updatedOrder.UserDefinedTag, ref m_OrderWorkSpace);         // see if we have another order we are working instead
                    if (m_OrderWorkSpace.Count == 0)
                    {// we aren't working a hedge order with this tag
                        bool isCompletelyFilled = updatedOrder.WorkingQtyPending == 0 && orderEventArg.Fill != null;        // if the order qty is zero and there is a fill attached our order was completely filled
                        if (!isCompletelyFilled)
                        { // partialled
                            if (orderEventArg.Fill != null)                                                                 // there was a fill that was not complete ie partialled
                                newOrder.OrderStatePending = updatedOrder.OrderStatePending;                                // make sure our qty's are okay since the order WAS partialled, and this could of been created prior to that
                            m_Spreader.m_ExecutionListener.TrySubmitOrder(m_OrderBook.BookID, newOrder);
                        }
                        m_PendingHedgeOrders.Remove(updatedOrder.UserDefinedTag);                                           // remove the pending order
                    }
                }
            }
        }
        #endregion//Event Handlers

        #region Istringifiable implementation
        // *****************************************************************
        // ****                     Istringifiable                      ****
        // *****************************************************************
        string IStringifiable.GetAttributes()
        {
            StringBuilder s = new StringBuilder(base.GetAttributes());
            s.AppendFormat(" UseGTCHedge={0}", this.UseGTCHedge);
            s.AppendFormat(" PayUpTicks={0}", this.PayUpTicks);
            return s.ToString();
        }
        //
        List<IStringifiable> IStringifiable.GetElements()
        {
            return base.GetElements();
        }
        //
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
            base.SetAttributes(attributes);
            int i;
            bool isTrue;
            foreach (KeyValuePair<string, string> attr in attributes)
            {
                if (attr.Key == "UseGTCHedge" && bool.TryParse(attr.Value, out isTrue))
                    this.UseGTCHedge = isTrue;
                if (attr.Key == "PayUpTicks" && int.TryParse(attr.Value, out i))
                    this.PayUpTicks = i;
            }
        }
        //
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {
            base.AddSubElement(subElement);
        }
        #endregion // Istringifiable

    }
}
