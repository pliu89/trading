using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace UV.Strategies.ExecutionEngines.Scratchers
{
    using UV.Lib.Engines;
    using UV.Lib.IO.Xml;
    using UV.Lib.MarketHubs;
    using UV.Lib.Products;
    using UV.Lib.OrderBooks;
    using UV.Lib.Hubs;
    using UV.Lib.Utilities;

    using UV.Strategies.ExecutionHubs;
    using UV.Strategies.ExecutionEngines.OrderEngines;
    /// <summary>
    /// Class representing a scrtacher for a loeg that can have positions for the "ScratchManager" to manage.
    /// Currently it will montitor the market and then determine when it needs to scratch.  When this happens,
    /// an order is then created, and either submitted immediately, or if it will cross our own orders, it will 
    /// be queued and the other orders monitored so the Scratcher knows when it is able to submit the scratch order.
    /// This implementation could be optimized significantly to avoid so much looping.  For the time being, I am erring on 
    /// the safe side and leaving it with the easily implemented loopin logic.
    /// </summary>
    public class Scratcher : Engine, IStringifiable
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        public UV.Lib.BookHubs.Market m_Market;                     // pointer to market for leg
        public OrderBook m_OrderEngineOrderBook;                    // pointer to quote order book for this leg, used for checking we aren't trading with ourselves!
        public LogHub m_Log;
        public InstrumentDetails m_InstrumentDetails;               // store all details about this instrument.
        public PriceLeg m_PriceLeg;
        public ExecutionListener m_ExecutionListener;               // pointer to listener for order manipulation
        public ScratchManager m_ScratchManager;                     // object managing collection of scratchers above me

        // Engine Variables
        private int m_ScratchThreshold;                             // threshold qty to trigger scratch action
        private bool m_IsActive;                                    // whether scratcher should be active (agress) or passive (join)

        // Collections
        private bool[] m_WasInsideMarketQtyAboveThreshold = new bool[2];
        private Dictionary<int, int> m_IPriceToPosition = new Dictionary<int, int>();                               // map IPrice to current position to be scratched
        private List<Order> m_OrderWorkSpaceList = new List<Order>();                                               // workspace for order manipulations, clear before use!
        private List<int> m_IPriceWorkSpaceList = new List<int>();                                                  // list of prices needed to be scratche...clear before use!
        private Dictionary<int, Order> m_IPriceToPendingScratchOrder = new Dictionary<int, Order>();
        
        // Note: An "Opposite" order is an order who we need to confirm is no longer on the other side
        // of the scratch order we would like to submit. This is due to the fact that we do not want to 
        // cross with ourselves. The two collections are needed due to the possibility of an order having 
        // its price changed by the order engine without us knowing about it.  Because of this we need a
        // mapping of an order ID to the original IPrice we were waiting to scratch.
        private Dictionary<int, HashSet<int>> m_IPriceToOppositeOrderIDs = new Dictionary<int, HashSet<int>>(); // has set of orders ids we care about, hash set
        private Dictionary<int, int> m_OppositeOrderIDToIPrice = new Dictionary<int, int>();                    // map order ids to IPrice we want to scratch 

        #endregion// members

        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        protected override void SetupInitialize(IEngineHub myEngineHub, IEngineContainer engineContainer, int engineID, bool setupGui)
        {
            this.m_EngineName = string.Format("Scratcher:{0}", m_PriceLeg.InstrumentName);
            if (typeof(UV.Strategies.ExecutionHubs.ExecutionContainers.MultiThreadContainer).IsAssignableFrom(engineContainer.GetType()))
            {   // this is the "first" set up call from the manager container.
                base.SetupInitialize(myEngineHub, engineContainer, engineID, setupGui);
                m_Log = ((ExecutionHubs.ExecutionHub)myEngineHub).Log;                  // keep pointer to log
            }
            else
            {   // this is the second set up call from the correct container, add correct sub engine mappings 
            }
        }
        //
        public override void SetupBegin(IEngineHub myEngineHub, IEngineContainer engineContainer)
        {
            base.SetupBegin(myEngineHub, engineContainer);
            foreach (IEngine eng in engineContainer.GetEngines())
            {
                if (eng is IOrderEngine)
                {   // find our orer engine and our execution listner
                    m_ExecutionListener = ((IOrderEngine)eng).GetExecutionListener();
                    continue;
                }
            }
        }
        //
        //
        public override void SetupComplete()
        {
            base.SetupComplete();

            // Subscribe to order events so we know when we can scratch
            m_OrderEngineOrderBook.OrderStateChanged += new EventHandler(OrderBook_OrderStateChanged);  // this includes fills
            m_OrderEngineOrderBook.OrderUpdated += new EventHandler(OrderBook_OrderUpdated);

        }


        #endregion//Constructors

        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// Qty on inside market to trigger active or passive scratch
        /// </summary>
        public int ScratchThreshold
        {
            get { return m_ScratchThreshold; }
            set
            {
                // TODO: Validation!
                m_ScratchThreshold = value;
            }
        }
        //
        //
        /// <summary>
        /// Toggle for active scratch when true (aggressive) or passive scratch when false (join)
        /// </summary>
        public bool ActiveScratch
        {
            get { return m_IsActive; }
            set
            {
                // TODO: Validation!
                m_IsActive = value;
            }
        }

        #endregion//Properties

        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //
        //
        // *********************************************************
        // ****                 Market_MarketChanged            ****
        // *********************************************************
        /// <summary>
        /// Called on every tick from the market.  Attempt was made to optimize this
        /// by filtering out times when we don't have a position to manage.  Additionally
        /// all calls that are not top of book are ignored.  Checks are created to watch for when market
        /// quantity crosses thresholds as this is what triggers the scratch event. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        public void Market_MarketChanged(object sender, EventArgs eventArgs)
        {
            if (m_IPriceToPosition.Count == 0)
            {   // we are not actively managing a position, we dont care about market updates
                return;
            }

            if (m_Market.IsLastTickBestPriceChange)
            {   // this update was a best price change! we need to check everything to see if we need to scratch
                m_IPriceWorkSpaceList.Clear();  // use this list since TryScratchPosition removes keys from m_IPricePosition, we cannot iterate on it!

                foreach (KeyValuePair<int, int> kvPair in m_IPriceToPosition)
                {   // check each price we have a position at, if we need to scratch, then add it to our list of prices to scratch
                    if (IsScratchNeededAtIPrice(kvPair.Key))
                        m_IPriceWorkSpaceList.Add(kvPair.Key);
                }

                for (int i = 0; i < m_IPriceWorkSpaceList.Count; i++)
                {   // for every price we need to scratch 
                    TryScratchPosition(m_IPriceWorkSpaceList[i], true);
                }

                // update our state variables with new market quantities!
                for (int side = 0; side < 2; ++side)
                    m_WasInsideMarketQtyAboveThreshold[side] = m_Market.Qty[side][0] > m_ScratchThreshold;

            }
            else
            {   // this could be a top of book qty change, lets check
                bool isMarketQtyOverThreshold;
                for (int side = 0; side < 2; ++side)
                {
                    if (m_Market.BestDepthUpdated[side] == 0)
                    {   // the last update on this side was top of market!
                        isMarketQtyOverThreshold = m_Market.Qty[side][0] > m_ScratchThreshold;
                        if ((m_IsActive & !isMarketQtyOverThreshold & m_WasInsideMarketQtyAboveThreshold[side]) |
                            (!m_IsActive & isMarketQtyOverThreshold & !m_WasInsideMarketQtyAboveThreshold[side]))
                        {   // we are actively scratching, the market is now under our threshold and it was previously above! 
                            // OR we are passively scratching the market is now over our threshold and it was previously below!
                            int iPrice = QTMath.RoundToSafeIPrice(m_Market.Price[side][0], side, m_InstrumentDetails.TickSize);
                            if (IsScratchNeededAtIPrice(iPrice))
                            {   // this call will complete the checks and if returns true we actually need to scratch
                                TryScratchPosition(iPrice, true);
                            }
                        }
                        m_WasInsideMarketQtyAboveThreshold[side] = isMarketQtyOverThreshold;    // save new threshold state
                    }
                }
            }

        }
        //
        //
        //
        //
        // *********************************************************
        // ****                     AddPosition                 ****
        // *********************************************************
        /// <summary>
        /// Caller would like to add a position to be managed and scratched if need be!
        /// </summary>
        /// <param name="iPrice"></param>
        /// <param name="qty"></param>
        public void AddPosition(int iPrice, int qty)
        {
            int previousPos;
            if (m_IPriceToPosition.TryGetValue(iPrice, out previousPos))
            {   // we already have a position to scratch at this price
                m_Log.NewEntry(LogLevel.Major, "Scratcher:{0} Adding {1} to existing IPrice {2}", this.EngineName, qty, iPrice);
                if (Math.Sign(previousPos) == Math.Sign(qty))
                {   // we are just adding qty to the position we already are managing. 
                    // Since we check on every market update, we already know we don't
                    // need to scratch this price with our current market state, so just add the position
                    m_IPriceToPosition[iPrice] = qty + previousPos;

                    //// Cheng Implementation: Add to immediately scratch position if condition met.
                    // The reason is that order book event update is later than market update.
                    // If does not have this, market will not scratch!

                    if (IsScratchNeededAtIPrice(iPrice))
                    {   // we need to immediately scratch this new position!
                        m_Log.NewEntry(LogLevel.Major, "Scratcher:{0} New position needs to be scratched immediately! Scratching.", this.EngineName);
                        TryScratchPosition(iPrice, true);
                    }
                }
                else
                {   // this is probably an error so lets log about it
                    m_Log.NewEntry(LogLevel.Error, "Scratcher:{0} Recvd request to scratch position {1} opposite current position {2} at price {3}.",
                        this.EngineName, qty, previousPos, iPrice);
                }
            }
            else
            {   // no position at this price currently, we need to add it, and we need to check if we need to scratch it immediately!
                m_Log.NewEntry(LogLevel.Major, "Scratcher:{0} Adding {1} to new IPrice {2}", this.EngineName, qty, iPrice);
                m_IPriceToPosition.Add(iPrice, qty);

                if (IsScratchNeededAtIPrice(iPrice))
                {   // we need to immediately scratch this new position!
                    m_Log.NewEntry(LogLevel.Major, "Scratcher:{0} New position needs to be scratched immediately! Scratching.", this.EngineName);
                    TryScratchPosition(iPrice, true);
                }
            }
        }
        //
        //
        // *********************************************************
        // ****                     TryRemovePosition           ****
        // *********************************************************
        /// <summary>
        /// Caller would like to attempt to remove a position from being managed.
        /// If the qty request to be removed is greater than the current position 
        /// being managed, False will be returned and no action will be taken to
        /// removed the current position.  False can also be returned if the IPrice
        /// is not found to have any currently managed positions
        /// </summary>
        /// <param name="iPrice"></param>
        /// <param name="qtyToRemove"></param>
        /// <returns></returns>
        public bool TryRemovePosition(int iPrice, int qtyToRemove)
        {
            int currentPos;
            if (!m_IPriceToPosition.TryGetValue(iPrice, out currentPos))
            {   // something is wrong, we don't know about the position from this price.
                m_Log.NewEntry(LogLevel.Error, "Scratcher:{0} TryRemovePosition failed. No position found at IPrice {1}", this.EngineName, iPrice);
                return false;
            }
            else if (Math.Sign(currentPos) != Math.Sign(qtyToRemove))
            {   // seomthing is wrong, position sides dont make sense
                m_Log.NewEntry(LogLevel.Error, "Scratcher:{0} TryRemovePosition failed. Position found at IPrice {1} is opposite side of position {2} requested for removal",
                                this.EngineName, iPrice, qtyToRemove);
                return false;
            }
            else if (Math.Sign(currentPos) < Math.Abs(qtyToRemove))
            {
                m_Log.NewEntry(LogLevel.Error, "Scratcher:{0} TryRemovePosition failed. Position found at IPrice {1} is less than position {2} requested for removal",
                                this.EngineName, iPrice, qtyToRemove);
                return false;
            }


            m_IPriceToPosition[iPrice] = currentPos - qtyToRemove;
            if (currentPos == qtyToRemove)
            {   // all position here has been consumed, remove from dictionary!
                m_IPriceToPosition.Remove(iPrice);
                m_Log.NewEntry(LogLevel.Error, "Scratcher:{0} TryRemovePosition removed entire position at IPrice {1}", this.EngineName, iPrice);
            }
            else
            {   // some position still remains at this iPrice.
                m_Log.NewEntry(LogLevel.Error, "Scratcher:{0} TryRemovePosition removed {1} from position at IPrice {2}, {3} remain to be managed",
                    this.EngineName, qtyToRemove, iPrice, m_IPriceToPosition[iPrice]);
            }

            return true;
        }
        #endregion//Public Methods

        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        // *********************************************************
        // ****                IsScratchNeededAtIPrice          ****
        // *********************************************************
        /// <summary>
        /// Caller would like to check if a position from a  specific level needs to 
        /// be scratched based on the parameters and our current market state.
        /// Notes : This could be optimized a bit for speed up purposes!
        /// </summary>
        /// <param name="iPrice"></param>
        /// <returns></returns>
        private bool IsScratchNeededAtIPrice(int iPrice)
        {
            int currentPos;
            if (!m_IPriceToPosition.TryGetValue(iPrice, out currentPos))     // we have no position to manage here!
                return false;

            int posSide = QTMath.MktSignToMktSide(currentPos);  // find out which side our position is from 
            int marketIPrice;
            if (m_IsActive)
            {   // if we are actively scratching 
                // If i am long from the bid, i only care when the bid changes
                int posSign = QTMath.MktSideToMktSign(posSide);
                marketIPrice = (int)(m_Market.Price[posSide][0] / m_InstrumentDetails.TickSize);

                if (iPrice * posSign > marketIPrice * posSign | (marketIPrice == iPrice & m_Market.Qty[posSide][0] < m_ScratchThreshold))
                {   // market has gone through us, or quantity is now less than our threshold
                    return true;
                }
            }
            else
            {   // we are passively scratching 
                // if i am long from the bid, i only care when the ask price changes ( i am going to join when price goes sellers)
                int oppSide = QTMath.MktSideToOtherSide(posSide);
                int oppSign = QTMath.MktSideToMktSign(oppSide);
                marketIPrice = (int)(m_Market.Price[oppSide][0] / m_InstrumentDetails.TickSize);

                if (marketIPrice * oppSign > iPrice * oppSign | (marketIPrice == iPrice & m_Market.Qty[oppSide][0] > m_ScratchThreshold))
                {   // market has gone through us, or quantity is now less greater than our threshold
                    return true;
                }
            }
            return false;
        }
        //
        //
        // *********************************************************
        // ****                  TryScratchPosition             ****
        // *********************************************************
        /// <summary>
        /// Called when a market state change triggers a scratch criteria to be met, 
        /// Or after an order change triggers us to attempt and reprocess pending scratch orders
        /// </summary>
        /// <param name="iPrice"></param>
        /// <param name="isFirstAttempt">will be true if market state caused this attempt </param>
        /// <returns>true if a scratch order has been submitted and not queued</returns>
        private bool TryScratchPosition(int iPrice, bool isFirstAttempt)
        {
            Order scratchOrder;                                             // Our scratch order to be submit or queued
            int currentPos;

            if (isFirstAttempt)
            {
                if (!m_IPriceToPosition.TryGetValue(iPrice, out currentPos))
                {
                    m_Log.NewEntry(LogLevel.Error, "Scratcher:{0} TryScratchPosition failed. iPrice {1} not found in dictionary", this.EngineName, iPrice);
                    return false;
                }

                int qtyToScratch = currentPos * -1;             // opposit of qty of position give us scratch qty
                int scratchSide = QTMath.MktSignToMktSide(qtyToScratch);        // opposite side is the scratch side

                if (m_IPriceToPendingScratchOrder.TryGetValue(iPrice, out scratchOrder))
                {   // we already have a pending scratch order waiting for submisssion, just add our quantity to it
                    m_ExecutionListener.TryChangeOrderQty(scratchOrder, scratchOrder.OriginalQtyPending + qtyToScratch);
                }
                else if (!m_ExecutionListener.TryCreateOrder(m_InstrumentDetails.InstrumentName, scratchSide, iPrice, qtyToScratch, out scratchOrder))
                {   // order creation failed for some reason, this has to be a logic mistake!
                    m_Log.NewEntry(LogLevel.Error, "Scratcher:{0} TryScratchPosition failed. Order Creation failed", this.EngineName);
                    return false;
                }

                m_IPriceToPosition.Remove(iPrice);                              // Remove key from dictionary.
                scratchOrder.OrderReason = OrderReason.Scratch;                 // tag order with "reason"
            }
            else
            {   // this is a second call after we believe we are ready to scratch, check opposing orders again before we submit
                if (!m_IPriceToPendingScratchOrder.TryGetValue(iPrice, out scratchOrder))
                {  // something went wrong, log the error
                    m_Log.NewEntry(LogLevel.Error, "Scratcher:{0} TryScratchPosition failed. Unable to find pending scratch order for IPrice {1}", this.EngineName, iPrice);
                    return false;
                }
            }


            bool isOkayToSubmitScratch = true;

            if (m_IsActive)
            {   // if we are actively scratching, we could possibly cross ourselves in the market, need to be careful here 
                m_OrderWorkSpaceList.Clear();           // clear order workspace
                m_OrderEngineOrderBook.GetOrdersByIPrice(QTMath.MktSideToOtherSide(scratchOrder.Side), iPrice, ref m_OrderWorkSpaceList);
                if (m_OrderWorkSpaceList.Count > 0)
                {   // we have opposing orders that need to be pulled prior to us submitting, arm ourselves and wait for delete ack

                    HashSet<int> orderIdsWaitingDelete;
                    if (!m_IPriceToOppositeOrderIDs.TryGetValue(iPrice, out orderIdsWaitingDelete))
                    {   // we don't have a list yet for this iPrice, create one
                        orderIdsWaitingDelete = new HashSet<int>();
                    }
                    for (int i = 0; i < m_OrderWorkSpaceList.Count; i++)
                    {   // iterate through all pending orders and save their order ids
                        orderIdsWaitingDelete.Add(m_OrderWorkSpaceList[i].Id);   // since this is a hash set it will only add unique ids
                        m_OppositeOrderIDToIPrice[m_OrderWorkSpaceList[i].Id] = iPrice; // make sure we map every order to the iprice we care about
                    }
                    m_IPriceToOppositeOrderIDs[iPrice] = orderIdsWaitingDelete;   // save our new hash set by iPrice
                }
                else
                {   // there is no opposing order, immediately send our scratch order 
                    isOkayToSubmitScratch = true;
                    if (!isFirstAttempt)
                    {   // we need to cleanup our collection
                        m_IPriceToOppositeOrderIDs.Remove(iPrice);              // remove this collection
                    }
                }
            }

            if (isOkayToSubmitScratch)
            {   // we are either passive (there is no possible way we can cross ourselves with a scratch order) or we are active but okay to submit
                TrySubmitScratchOrder(scratchOrder);
                return true;
            }
            return false;
        }
        //
        //
        //
        // *********************************************************
        // ****                 TrySubmitScratchOrder           ****
        // *********************************************************
        /// <summary>
        /// Simple wrapper for submitting a scratch order to our order engines book.
        /// </summary>
        /// <param name="scratchOrder"></param>
        /// <returns></returns>
        private bool TrySubmitScratchOrder(Order scratchOrder)
        {
            if (!m_ExecutionListener.TrySubmitOrder(m_OrderEngineOrderBook.BookID, scratchOrder))
            {   // something failed with order submission
                m_Log.NewEntry(LogLevel.Error, "Scratcher:{0} TryScratchPosition - Passive failed. Order {1} Submission failed", this.EngineName, scratchOrder.Id);
                return false;
            }
            m_ScratchManager.OnScratchOrderSubmitted(scratchOrder, m_OrderEngineOrderBook.BookID);  // call manager to create event for new order
            return true;
        }//
        //
        //
        // *********************************************************
        // ****                 ProcessOrderEvent               ****
        // *********************************************************
        /// <summary>
        /// Called whenever the scratcher recieves an event associated with the order book we are subscribed to.
        /// </summary>
        /// <param name="orderEventArgs"></param>
        private void ProcessOrderEvent(OrderEventArgs orderEventArgs)
        {

            int iPrice;
            if (!m_OppositeOrderIDToIPrice.TryGetValue(orderEventArgs.Order.Id, out iPrice))
                return;     // this is an order we don't care about, just move on

            if (orderEventArgs.Order.OrderStateConfirmed == OrderState.Dead |
                (orderEventArgs.Order.IPricePending != iPrice & orderEventArgs.Order.IStopPriceConfirmed != iPrice))
            {   // order is in a dead state (fully filled or cancelleed) OR it is no longer at our price and isn't planned to be moved to our price
                m_OppositeOrderIDToIPrice.Remove(orderEventArgs.Order.Id);

                HashSet<int> oppositeOrderIDs;
                if (!m_IPriceToOppositeOrderIDs.TryGetValue(iPrice, out oppositeOrderIDs))
                {   // this can only happen if something is wrong with out logic, log the error and return
                    m_Log.NewEntry(LogLevel.Error, "Scratcher:{0} ProcessOrderEvent - IPrice {1} hash set not found!", this.EngineName, iPrice);
                    return;
                }
                oppositeOrderIDs.Remove(orderEventArgs.Order.Id);

                if (oppositeOrderIDs.Count == 0)
                {   // this was the last order we were waiting on to be cancelled at this iPrice, attempt to scratch
                    TryScratchPosition(iPrice, false); // this will take care of all cleanup of collections
                }
            }

        }
        #endregion//Private Methods

        #region Events
        // *****************************************************************
        // ****                         Events                          ****
        // *****************************************************************
        //
        //
        //
        //
        #endregion // events

        #region Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                      ****
        // *****************************************************************
        //
        private void OrderBook_OrderStateChanged(object sender, EventArgs e)
        {
            OrderEventArgs orderEventArgs = (OrderEventArgs)e;
            ProcessOrderEvent(orderEventArgs);
        }
        //
        //
        private void OrderBook_OrderUpdated(object sender, EventArgs e)
        {
            OrderEventArgs orderEventArgs = (OrderEventArgs)e;
            ProcessOrderEvent(orderEventArgs);
        }
        #endregion//Event Handlers

        #region Istringifiable implementation
        // *****************************************************************
        // ****                     Istringifiable                      ****
        // *****************************************************************
        string IStringifiable.GetAttributes()
        {
            StringBuilder s = new StringBuilder(base.GetAttributes());
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
            bool isTrue;
            foreach (KeyValuePair<string, string> attr in attributes)
            {
                if (attr.Key.Equals("ScratchThreshold", StringComparison.OrdinalIgnoreCase) && int.TryParse(attr.Value, out i))
                    this.m_ScratchThreshold = i;
                if (attr.Key.Equals("ActiveScratch", StringComparison.OrdinalIgnoreCase) && bool.TryParse(attr.Value, out isTrue))
                    this.m_IsActive = isTrue;
            }
        }

        void IStringifiable.AddSubElement(IStringifiable subElement)
        {
            base.AddSubElement(subElement);
        }
        #endregion // Istringifiable
    }
}
