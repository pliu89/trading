using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;


namespace UV.Strategies.ExecutionHubs
{
    using UV.Lib.Engines;
    using UV.Lib.Hubs;
    using UV.Lib.Products;
    using UV.Lib.OrderBooks;
    
    using UV.Strategies.ExecutionHubs.ExecutionContainers;

    /// <summary>
    /// This is the base class that all Execution Listeners should inherit from.
    /// There are several abstract methods needed to finish off the interfaces of ITimerSubscriber
    /// as well as to allow whatever threading model the API 
    /// </summary>
    public abstract class ExecutionListener : ITimerSubscriber
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // External Services
        //
        protected IEngineHub m_EngineHub;                             // My parent EngineHub.
        protected LogHub Log = null;                                  // log I can write to.
        protected string m_Name;
        protected UV.Strategies.ExecutionHubs.ExecutionContainers.ThreadContainer m_ExecContainer;
        //
        // Internal lookup tables
        //
        protected Dictionary<InstrumentName, InstrumentDetails> m_UVInstrumentDetails = new Dictionary<InstrumentName, InstrumentDetails>();

        //
        // pending collection 
        //
        protected Dictionary<InstrumentName, List<OrderBook>> m_PendingOrderBooks = new Dictionary<InstrumentName, List<OrderBook>>();  // place to store all order books we need instrument details to create

        //
        // Itimer subscribers
        //
        protected List<ITimerSubscriber> m_ITimerSubscribers = new List<ITimerSubscriber>();

        //
        // Event Queues
        //
        protected Queue<EventArgs> m_WorkQueue = new Queue<EventArgs>();                      // Completely private, owned by Listener thread.
        protected ConcurrentQueue<EventArgs> m_InQueue = new ConcurrentQueue<EventArgs>();    // threadsafe queue to push events to processed onto.
        #endregion //members

        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public ExecutionListener(string name, IEngineHub engineHub)
        {
            m_EngineHub = engineHub;
            if (engineHub is Hub)
                this.Log = ((Hub)engineHub).Log;
            m_Name = name;
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
        // *****************************************
        // ****              Name()             ****
        // *****************************************
        protected string Name
        {
            get { return System.Threading.Thread.CurrentThread.Name; }
        }
        //
        //
        //
        // *****************************************
        // ****       ExecutionContainer()      ****
        // *****************************************
        /// <summary>
        /// Get and Set the ExecutionContainer for the listener.  If setting the instrument details
        /// dictionary contained in this objet will be assigned and shared by ExecutionContainer 
        /// </summary>
        public ThreadContainer ExecutionContainer
        {
            get { return m_ExecContainer; }
            set
            {
                m_ExecContainer = value;
                m_ExecContainer.m_InstrDetails = m_UVInstrumentDetails;
            }
        }
        #endregion//Properties

        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        // *****************************************
        // ****         InitializeThread()      ****
        // *****************************************
        /// <summary>
        /// Called to allow dispatcher to be attached to thread.  
        /// </summary>
        public abstract void InitializeThread();
        //
        // *****************************************
        // ****           StopThread()          ****
        // *****************************************
        /// <summary>
        /// threadsafe call to signal nice shutdown
        /// </summary>
        public abstract void StopThread();
        //
        //
        // *****************************************************
        // ****             SubscribeToTimer()              ****
        // *****************************************************
        /// <summary>
        /// Caller would like to get periodic updates from the hub.
        /// </summary>
        /// <param name="subscriber"></param>
        public void SubscribeToTimer(ITimerSubscriber subscriber)
        {
            Log.NewEntry(LogLevel.Warning, "SubscribeToTimer: Strategy {0} subscribing.", subscriber.GetType().Name);
            m_ITimerSubscribers.Add(subscriber);
        }
        //
        //
        //
        // *****************************************
        // ****       CreateOrderBook()         ****
        // *****************************************
        /// <summary>
        /// Caller would like to created an Order Book. This will handle all neccesarry subscriptions.
        /// The user can directly subscribe to events from ths book.
        /// </summary>
        /// <param name="instrumentName"></param>
        /// <param name="account"></param>
        /// <returns></returns>
        public OrderBook CreateOrderBook(InstrumentName instrumentName, string account)
        {
            OrderBook orderBook = new OrderBook(instrumentName, account);
            ProcessCreateBook(orderBook);
            OnOrderBookCreated(orderBook);          // trigger events for any subscriber
            return orderBook;
        }
        //
        //
        //
        // *****************************************
        // ****       GetAllOrdersBooks()       ****
        // *****************************************
        /// <summary>
        /// Caller would like to add all books managed by all order instruments
        /// to the referenced list.
        /// </summary>
        /// <param name="orderBookList"></param>
        public void GetAllOrderBooks(ref List<OrderBook> orderBookList)
        {
            foreach (OrderInstrument orderInst in m_ExecContainer.m_OrderInstruments.Values)
            {
                orderInst.GetAllOrderBooks(ref orderBookList);
            }
        }
        //
        // *****************************************
        // ****       GetAllDefaultBooks        ****
        // *****************************************
        /// <summary>
        /// Caller would like to add all books managed by all order instruments
        /// to the referenced list.
        /// </summary>
        /// <param name="orderBookList"></param>
        public void GetAllDefaultBooks(ref List<OrderBook> orderBookList)
        {
            foreach (OrderInstrument orderInst in m_ExecContainer.m_OrderInstruments.Values)
            {
                orderBookList.Add(orderInst.DefaultBook);
            }
        }
        //
        //
        // *****************************************
        // ****         TryCreateOrder          ****
        // *****************************************
        /// <summary>
        /// The caller wants to create an order.  After creation, the order must be submitted 
        /// to the specific OrderBook to be managed using TrySubmiteOrder
        /// </summary>
        /// <param name="instrumentName"></param>
        /// <param name="tradeSide"></param>
        /// <param name="iPrice"></param>
        /// <param name="qty"></param>
        /// <param name="newOrder"></param>
        /// <returns></returns>
        public virtual bool TryCreateOrder(InstrumentName instrumentName, int tradeSide, int iPrice, int qty, out Order newOrder)
        {
            newOrder = null;
            if ((UV.Lib.Utilities.QTMath.MktSideToMktSign(tradeSide) * qty) < 0)
            {   // this means our signs are incorrect!
                Log.NewEntry(LogLevel.Error, "Attempt to Create Order For Instrument {0} Failed, Mismatched Sides and Qtys", instrumentName);
                return false;
            }
            OrderInstrument orderInstrument;
            if (m_ExecContainer.m_OrderInstruments.TryGetValue(instrumentName, out orderInstrument))
            {
                newOrder = new Order();
                newOrder.Instrument = instrumentName;
                newOrder.Id = Order.GetNextId();
                newOrder.Side = tradeSide;
                newOrder.OriginalQtyPending = qty;
                newOrder.IPricePending = iPrice;
                newOrder.TickSize = orderInstrument.Details.TickSize;
                newOrder.OrderType = OrderType.LimitOrder;
            }
            // Exit.
            return (newOrder != null);
        }//CreateOrderBook()
        //
        //
        // *****************************************
        // ****         Try Submit Order()      ****
        // *****************************************
        /// <summary>
        /// This method will take care of all internal bookkeeping needed to submit an order,
        /// the class inhertiting ExecutionListener should then submit to the correct API
        /// 
        /// Note: Call this method before submitting to API! This will tag order with correct account
        /// 
        /// </summary>
        /// <param name="orderBookID"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        public virtual bool TrySubmitOrder(int orderBookID, Order order)
        {
            OrderInstrument orderInstrument;
            if (!m_ExecContainer.m_OrderInstruments.TryGetValue(order.Instrument, out orderInstrument))
                return false;
            return orderInstrument.TryAddOrder(orderBookID, order);
        }
        //
        //
        // *****************************************
        // ****         Try Delete Order()      ****
        // *****************************************
        /// <summary>
        /// called by user to delete an order, this method handles all internal bookkeeping.
        /// class inhertiting ExecutionListener should then call to the correct API to cancel the order
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public virtual bool TryDeleteOrder(Order order)
        {
            if (order.OrderStatePending == OrderState.Dead)
            {
                Log.NewEntry(LogLevel.Warning, "TryDeleteOrder: {0} - Was already processed to be cancelled", order);
                return false;
            }
            OrderInstrument orderInstrument;
            if (m_ExecContainer.m_OrderInstruments.TryGetValue(order.Instrument, out orderInstrument))
            {
                order.OrderStatePending = OrderState.Dead;  // set pending to dead, we will set the rest when we here back from TT
                order.OriginalQtyPending = 0;
                return true;
            }
            return false;
        }
        //
        //
        // *****************************************
        // ****       TryChangeOrderPrice()     ****
        // *****************************************
        /// <summary>
        /// called by user to change an orders price, this method handles all internal bookkeeping.
        /// class inhertiting ExecutionListener should then call to the correct API to change the order
        /// </summary>
        /// <param name="order"></param>
        /// <param name="newIPrice"></param>
        /// <param name="newQty"></param>
        /// <returns></returns>
        public virtual bool TryChangeOrderPrice(Order order, int newIPrice, int newQty)
        {
            OrderInstrument orderInstrument;
            if (m_ExecContainer.m_OrderInstruments.TryGetValue(order.Instrument, out orderInstrument))
            {
                if (newQty != order.OriginalQtyPending)
                {// we also need to change the qty        
                    if ((UV.Lib.Utilities.QTMath.MktSideToMktSign(order.Side) * newQty) < 0)
                    { // order signs dont make sense!
                        Log.NewEntry(LogLevel.Error, "Attempt to change Order {0} qty to the opposite sign, rejecting attempted change", order);
                        return false;
                    }
                    else 
                    { // if the qty is valid and has been changed, proceed to allowing price change
                        order.OriginalQtyPending = newQty;
                        return orderInstrument.TryChangeOrderPrice(order, newIPrice);
                    }
                }
                else
                { // only a price change here, no qty difference.
                    return orderInstrument.TryChangeOrderPrice(order, newIPrice);
                }
            }
            return false;
        }
        //
        //
        // *****************************************
        // ****       TryChangeOrderQty()       ****
        // *****************************************
        /// <summary>
        /// called by user to change an orders Qty, this method handles all internal bookkeeping.
        /// class inhertiting ExecutionListener should then call to the correct API to change the order
        /// </summary>
        /// <param name="order"></param>
        /// <param name="newQty">SIGNED QTY</param>
        /// <returns>false if sign is incorrect</returns>
        public virtual bool TryChangeOrderQty(Order order, int newQty)
        {
            if ((UV.Lib.Utilities.QTMath.MktSideToMktSign(order.Side) * newQty) < 0)
            {   //this means our signs are incorrect!
                Log.NewEntry(LogLevel.Error, "Attempt to change Order {0} qty to the opposite sign, rejecting attempted change", order);
                return false;
            }
            order.OriginalQtyPending = newQty;
            return true;
        }
        //
        //
        // *****************************************
        // ****     TryTransferOrderToNewBook   ****
        // *****************************************
        /// <summary>
        /// Caller would like transfer and order from one book to another.
        /// This is usually only done from the default book to anohter book
        /// when the system has found an order that it would like to take control of.
        /// </summary>
        /// <returns></returns>
        public virtual bool TryTransferOrderToNewBook(Order order, OrderBook newOrderBook)
        {
            OrderInstrument orderInstrument;
            if (m_ExecContainer.m_OrderInstruments.TryGetValue(order.Instrument, out orderInstrument))
            {
                if (orderInstrument.TryTransferOrderToNewBook(order, newOrderBook))
                {
                    return true;
                }
                else
                {
                    Log.NewEntry(LogLevel.Error, "TryTransferOrderToNewBook: Order is possibly in bad state! - This could be a serious error!");
                }
            }
            return false;
        }
        #endregion //public methods.

        #region Private Methods
        // *****************************************************************************
        // ****                           Private  Methods                          ****
        // *****************************************************************************
        //
        // *****************************************************************
        // ****             Process Instruments Found()                 ****
        // *****************************************************************
        //
        /// <summary>
        /// When a new instrument details is found this method appropiate events to sbuscribers
        /// who would like to know about the insturment and details
        /// </summary>
        /// <param name="instrumentDetails"></param>
        protected void ProcessInstrumentsFound(InstrumentDetails instrumentDetails)
        {
            m_UVInstrumentDetails[instrumentDetails.InstrumentName] = instrumentDetails;
            if (m_PendingOrderBooks.ContainsKey(instrumentDetails.InstrumentName))
            {
                while (m_PendingOrderBooks[instrumentDetails.InstrumentName].Count > 0)
                { // remove from list and send to be processed.
                    OrderBook orderBookToProcess = m_PendingOrderBooks[instrumentDetails.InstrumentName][0];
                    m_PendingOrderBooks[instrumentDetails.InstrumentName].Remove(orderBookToProcess);
                    ProcessCreateBook(orderBookToProcess);
                }
            }
            OnInstrumentsFound(instrumentDetails);
        }
        //
        //
        protected abstract void ProcessCreateBook(OrderBook orderBook);    // figure out how to make this in the base!
        #endregion //Private Methods

        #region Events
        // *****************************************************************
        // ****                         Events                          ****
        // *****************************************************************
        // *************************************************************
        // ****                 Instrument Found                   ****
        // *************************************************************
        /// <summary>
        /// Event for subscribers who are looking for instruments.  When this is fired,
        /// the instrument details have been discovered as well as a market object created.
        /// </summary>
        public event EventHandler InstrumentFound;
        //
        protected void OnInstrumentsFound(InstrumentDetails instrDetails)
        {
            if (this.InstrumentFound != null)
            {
                UV.Lib.Products.InstrumentsFoundEventArgs e = new UV.Lib.Products.InstrumentsFoundEventArgs();
                e.InstrumentDetails = instrDetails;
                this.InstrumentFound(this, e);
            }
        }
        //
        // *************************************************************
        // ****                 OrderBookCreated                    ****
        // *************************************************************
        /// <summary>
        /// Event for subscribers who would like to know each and every time an order book is created.
        /// Usually useful fo risk managers whou would like to be able to be handed an order book 
        /// so they can subscribe to appropiate events.
        /// </summary>
        public event EventHandler OrderBookCreated;
        //
        protected void OnOrderBookCreated(OrderBook orderBook)
        {
            if (this.OrderBookCreated != null)
            {
                this.OrderBookCreated(orderBook, EventArgs.Empty);
            }
        }
        //
        // *************************************************************
        // ****                     Initialized                     ****
        // *************************************************************
        /// <summary>
        /// Called to allow SingleLegExecutor to finish set up prior to calling Dispatch.Run
        /// </summary>
        public event EventHandler Initialized;
        //
        protected void OnInitialized()
        {
            if (this.Initialized != null)
            {
                this.Initialized(this, EventArgs.Empty);
            }
        }
        //
        //
        // *************************************************************
        // ****                     Stopping                        ****
        // *************************************************************
        /// <summary>
        /// Called to allow SingleLegExecutor to finish and clean up prior to thread teardown
        /// </summary>
        public event EventHandler Stopping;
        //
        protected void OnStopping()
        {
            if (this.Stopping != null)
            {
                this.Stopping(this, EventArgs.Empty);
            }
        }
        #endregion //events

        #region Event Processing
        // *****************************************************************
        // ****                    Events Processing                    ****
        // *****************************************************************
        //
        public abstract void ProcessEvent(EventArgs eventArg);
        #endregion // Event Processing.

        #region ITimer Implementation
        // *****************************************************
        // ****             TimerSubscriberUpdate()         ****
        // *****************************************************
        /// <summary>
        /// Called by the hub thread to update us since we are a iTimerSubscriber.
        /// We will push it onto our thread and then call our own subscribers with the correct thread.
        /// </summary>
        public abstract void TimerSubscriberUpdate();
        //
        public void CallITimerSubscribers()
        {
            int i = 0;
            while (i < m_ITimerSubscribers.Count)
            {
                try
                {
                    m_ITimerSubscribers[i].TimerSubscriberUpdate();
                }
                catch (Exception e)
                {
                    Log.NewEntry(LogLevel.Error, "CallITimerSubscribers: Failed to update TimeSubscibers: {0}", e.Message);
                }
                i++;
            }
        }
        #endregion // ITimerImplementation
    }
}
