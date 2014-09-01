using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace UV.Strategies.ExecutionEngines.Scratchers
{
    using UV.Lib.Engines;
    using UV.Lib.IO.Xml;
    using UV.Lib.Products;
    using UV.Lib.Hubs;
    using UV.Lib.OrderBooks;
    using UV.Strategies.ExecutionHubs.ExecutionContainers;

    using UV.Strategies.ExecutionEngines.OrderEngines.TermStructures;

    /// <summary>
    /// This engine manages positions for an execution strategy than possibly need to be 
    /// scratched.  Eventually this class could be inherited to write a "Scalp/Scratcher" since
    /// the orders than might be reused (cancel/replace/modify) vs net new order for scratch
    /// 
    /// TODO: This engine will need some way of confirming with the execution strategy that quote orders have been pulled
    /// at a given price prior to scratching that price.  
    ///     Possible solution:
    ///         1) Give pointer to order book so scratcher can check that there are no orders at a price level and side
    ///         2) if there is scratcher could keep track of the orders that we are waiting on the "ack" for.
    ///         3) scratcher would subscribe to events and recieve the ack that an order is confirmed deleted and then allow scratch to happen
    /// </summary>
    public class ScratchManager : Engine , IStringifiable
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        public ThreadContainer m_ExecutionContainer = null;
        public List<Scratcher> m_Scratchers = new List<Scratcher>();  // list of scratch legs I manage
        public CurveTrader m_CurveTrader = null;
        private LogHub m_Log;

        private OrderEventArgs m_OrderEventArg;                     // careful, but this is always recycled

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        protected override void SetupInitialize(IEngineHub myEngineHub, IEngineContainer engineContainer, int engineID, bool setupGui)
        {
            if (typeof(UV.Strategies.ExecutionHubs.ExecutionContainers.MultiThreadContainer).IsAssignableFrom(engineContainer.GetType()))
            {   // this is the "first" set up call from the manager container.
                base.SetupInitialize(myEngineHub, engineContainer, engineID, setupGui);
                m_Log = ((ExecutionHubs.ExecutionHub)myEngineHub).Log;
            }
            else
            {   // this is the second set up call from the correct container, add correct sub engine mappings 
                m_ExecutionContainer = (ThreadContainer)engineContainer;
            }
        }
        //
        public override void SetupComplete()
        {
            base.SetupComplete();
            if(m_CurveTrader != null)
            {   
                for (int leg = 0; leg < m_CurveTrader.m_CurveLegs.Count; ++leg)
                {   // add a scratcher for each instrument
                    CurveLeg curveLeg = m_CurveTrader.m_CurveLegs[leg];
                    m_Scratchers.Add(curveLeg.m_Scratcher);
                    curveLeg.m_Scratcher.m_ScratchManager = this;               // give pointer to ourself
                }

                m_CurveTrader.m_ExecutionListener.InstrumentFound += new EventHandler(ExecutionListener_InstrumentsFound);
            }
            m_OrderEventArg = new OrderEventArgs();     // create our event arg for recycling
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
        // *********************************************************
        // ****             TryAddPositionToScratcher           ****
        // *********************************************************
        /// <summary>
        /// Caller would like to add a position to be managed by the scratcher.
        /// Can only fail (return false) if the leg id is not found.  This call is always additive!
        /// To remove a position the safest way is to call TryRemovePositionFromScratcher
        /// </summary>
        /// <param name="scratchLegId"></param>
        /// <param name="iPrice"></param>
        /// <param name="qty"></param>
        /// <returns></returns>
        public bool TryAddPositionToScratcher(int scratchLegId, int iPrice, int qty)
        {
            if (scratchLegId >= m_Scratchers.Count)
            {   // this id must not exist!
                m_Log.NewEntry(LogLevel.Error, "ScratchManager:TryAddPositionToScratcher failed, {0} Uknown Leg ID", scratchLegId);
                return false;
            }
            m_Scratchers[scratchLegId].AddPosition(iPrice, qty);
            return true;
        }
        //
        // *********************************************************
        // ****           TryRemovePositionFromScratcher        ****
        // *********************************************************
        /// <summary>
        /// Caller would like to remove a position that was previously added to be managed by the 
        /// scratcher.  Can fail (return false) if the positiion is not found, or 
        /// we are asking to remove more qty than what exists
        /// </summary>
        /// <param name="scratchLegId"></param>
        /// <param name="iPrice"></param>
        /// <param name="qty"></param>
        /// <returns></returns>
        public bool TryRemovePositionFromScratcher(int scratchLegId, int iPrice, int qty)
        {
            if (scratchLegId > m_Scratchers.Count)
            {   // this id must not exist!
                m_Log.NewEntry(LogLevel.Error, "ScratchManager:TryRemovePositionFromScratcher failed, {0} Uknown Leg ID", scratchLegId);
                return false;
            }
            return m_Scratchers[scratchLegId].TryRemovePosition(iPrice, qty);
        }
        //
        #endregion//Public Methods

        #region no Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        #endregion//Private Methods

        #region Events
        // *****************************************************************
        // ****                          Event                          ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// Event handler for signalling a new scratch order has been submitted. At this point
        /// the order is still pending submitted from the exchange. This is different than the 
        /// OrderBook_OrderSubmittted event which is for a confirm by the exchange,
        /// Typically subscribed to by the order engine who wants to know about the order and 
        /// deal with the logic surrounding it
        /// WARNING : 
        ///     The event args that are passed with the event are OrderEventArgs and it is recylced.
        ///     Keeping a pointer to the event arg will result in bad behavior!
        /// </summary>
        public event EventHandler ScratchOrderSumbitted;
        //
        //
        /// <summary>
        /// Called whenever a scratcher submits a new scratch order.
        /// </summary>
        /// <param name="scratchOrder"></param>
        /// <param name="orderBookId"></param>
        public void OnScratchOrderSubmitted(Order scratchOrder, int orderBookId)
        {
            m_CurveTrader.m_RiskManager.m_NumberOfQuotesThisSecond++;   // increment for risk calculations
            
            if (this.ScratchOrderSumbitted != null)
            {
                m_OrderEventArg.Order = scratchOrder;
                m_OrderEventArg.OrderBookID = orderBookId;
                ScratchOrderSumbitted(this, m_OrderEventArg);
            }
        }
        #endregion//Events

        #region Event Handlers
        // *****************************************************************
        // ****                          Event Handler                 ****
        // *****************************************************************
        //
        //
        //
        // *****************************************************************
        // ****         ExecutionListener_InstrumentsFound()            ****
        // *****************************************************************
        /// <summary>
        /// Called when our execution listener has found a new instrument.  This means that it has also created
        /// a market for this instrument which we can now have a pointer to in the quoter leg, as well as subscribe 
        /// to the MarketChanged events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void ExecutionListener_InstrumentsFound(object sender, EventArgs eventArgs)
        {
            //
            // Gather and save pertinent information about the Instrument found.
            //
            InstrumentsFoundEventArgs instrEventArgs = (InstrumentsFoundEventArgs)eventArgs;
            InstrumentDetails instrDetails = instrEventArgs.InstrumentDetails;
            int internalId = m_CurveTrader.m_InstrumentToInternalId[instrDetails.InstrumentName];
            m_Scratchers[internalId].m_Market = m_ExecutionContainer.m_Markets[instrDetails.InstrumentName];                    // keep a pointer to the market
            m_Scratchers[internalId].m_InstrumentDetails = instrDetails;
        }
        #endregion EventHandlers
    }
}
