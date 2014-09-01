using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;

namespace UV.TTServices.OrderBookHubs
{
    using UV.Lib.Application;
    using UV.Lib.Hubs;
    using UV.Lib.Products;
    using UV.Lib.OrderBookHubs;
    using UV.Lib.IO.Xml;
    using UV.Lib.Utilities;

    using TTKey = TradingTechnologies.TTAPI.InstrumentKey;
    using UVOrder = UV.Lib.OrderBookHubs.Order;

    using UV.TTServices.Markets;

    /// <summary>
    /// Implementation of OrderBookHubs.OrderBookHub.
    /// </summary>
    public class OrderBookHubTT : OrderBookHub, IService, IStringifiable
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // Internal state information.
        //

        //      
        // External services
        private AppServices m_Services = null;
        private TTApiService m_TTService = null;
        private MarketTTAPI m_Market = null;

        private OrderListener m_Listener = null;

        // Internal workspaces
        private ConcurrentDictionary<InstrumentName, TTKey> m_InstrumentNameToTTKey = new ConcurrentDictionary<InstrumentName, TTKey>();



        #endregion// members

        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public OrderBookHubTT()
            : base("OrderHubTT")
        {
            m_Services = AppServices.GetInstance();
        }
        //
        //       
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
        //
        // **********************************************
        // ****         CreateOrderBook()            ****
        // **********************************************
        /// <summary>
        /// External caller request for an order book for a specific instrument.
        /// Once he has the order book, he can subscribe to its events for updates
        /// and 
        /// See base class explanation.
        /// </summary>
        /// <param name="instrumentName"></param>
        /// <returns></returns>
        public override OrderBook CreateOrderBook(InstrumentName instrumentName)
        {
            OrderBookTT orderBook = new OrderBookTT(instrumentName, m_OrderRecycleFactory);
            this.HubEventEnqueue(m_Requests.Get(RequestCode.CreateBook, orderBook));
            return orderBook;
        }//CreateOrderBook()
        //
        //
        // *****************************************
        // ****       TryChangeOrderPrice()     ****
        // *****************************************
        /// <summary>
        /// Called by a strategy or user to change the pending price of a given order.
        /// </summary>
        /// <param name="orderToModify"></param>
        /// <param name="newIPrice"></param>
        /// <returns></returns>
        public override bool TryChangeOrderPrice(Order orderToModify, int newIPrice)
        {
            orderToModify.IPricePending = newIPrice;
            if (base.TryChangeOrderPrice(orderToModify, newIPrice))
            { // internal order was succesfully modified.
                m_Listener.ModifyOrder(orderToModify);  // send to TT to modify
                return true;
            }
            return false;
        }
        //
        //
        //
        // *****************************************
        // ****       TryChangeOrderQty()       ****
        // *****************************************
        /// <summary>
        /// Called by a strategy or user to change the pending qty of a given order.
        /// </summary>
        /// <param name="orderToModify"></param>
        /// <param name="newQty">SIGNED QTY</param>
        /// <returns>false if sign is incorrect</returns>
        public override bool TryChangeOrderQty(Order orderToModify, int newQty)
        {
            if (base.TryChangeOrderQty(orderToModify, newQty))
            {
                m_Listener.ModifyOrder(orderToModify);
                return true;
            }
            return false;
        }
        //
        //
        //
        // *****************************************
        // ****    TryChangeOrderPriceAndQty()  ****
        // *****************************************
        public override bool TryChangeOrderPriceAndQty(UVOrder orderToModify, int newQty, int newIPrice)
        {
            if (base.TryChangeOrderPriceAndQty(orderToModify, newQty, newIPrice))
            {
                m_Listener.ModifyOrder(orderToModify);
                return true;
            }
            return false;
        }
        //
        //
        //
        // *********************************************
        // ****         Try Submit Order()          ****
        // *********************************************
        public override bool TrySubmitOrder(int orderBookID, Order orderToSubmit)
        {
            if (base.TrySubmitOrder(orderBookID, orderToSubmit))
            {
                TTKey ttKey;
                if (m_InstrumentNameToTTKey.TryGetValue(orderToSubmit.Instrument, out ttKey))
                {
                    m_Listener.SubmitOrder(ttKey, orderToSubmit);
                    return true;
                }
            }
            return false;
        }// TrySubmitOrder()
        //
        //
        // *********************************************
        // ****            TryDeleteOrder           ****
        // *********************************************
        public override bool TryDeleteOrder(UVOrder order)
        {
            if (base.TryDeleteOrder(order))
            {
                m_Listener.DeleteOrder(order); // this will set state flags
                return true;
            }
            return false;
        }
        //
        #endregion//Public Methods

        #region Hub Event Handler
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        //
        // *********************************************
        // ****         Hub Event Handler()         ****
        // *********************************************
        protected override void HubEventHandler(EventArgs[] eventArgList)
        {
            foreach (EventArgs eventArg in eventArgList)
            {
                if (eventArg is RequestEventArg<RequestCode>)
                {
                    RequestEventArg<RequestCode> request = (RequestEventArg<RequestCode>)eventArg;
                    switch (request.RequestType)
                    {
                        case RequestCode.ServiceStateChange:
                            ProcessServiceStateChange(request);
                            break;
                        case RequestCode.CreateBook:
                            ProcessCreateBook(request);
                            break;
                        default:
                            Log.NewEntry(LogLevel.Warning, "HubEventHandler: Request not implemented {0}.", request);
                            m_Requests.Recycle(request);
                            break;
                    }
                }
            }//next eventArg
        }//HubEventHandler()
        //
        //
        #endregion//Private Methods

        #region Processing Methods
        // *****************************************************************
        // ****                  Processing Methods                     ****
        // *****************************************************************
        //
        //
        //
        // *****************************************
        // ****         ProcessCreateBook()     ****
        // *****************************************
        /// <summary>
        /// Process to initialize a single order book.
        /// </summary>
        /// <param name="request"></param>
        private void ProcessCreateBook(RequestEventArg<RequestCode> request)
        {
            // Extract order book and validate request.
            if (request.Data.Count < 1)
            {
                Log.NewEntry(LogLevel.Warning, "ProcessCreateBook: Fail to find book in request.");
                return;
            }
            OrderBookTT orderBook = request.Data[0] as OrderBookTT;
            if (orderBook == null)
            {
                Log.NewEntry(LogLevel.Warning, "ProcessCreateBook: Fail to find valid order book in request.");
                return;
            }

            // Locate the OrderInstrument for this book.
            OrderInstrument orderInstrument = null;
            if (!m_OrderInstruments.TryGetValue(orderBook.Instrument, out orderInstrument))
            {   // We do not have an order instrument for this Instrument.
                InstrumentDetails instrumentDetails;
                if (!m_Market.TryGetInstrumentDetails(orderBook.Instrument, out instrumentDetails))
                {   // We don't know this instrument yet.
                    // Request information from market, and set this to pending.
                    Log.NewEntry(LogLevel.Minor, "ProcessCreateBook: Requesting instrument details for {0} OrderBookID {1}. Will try again later.",
                        orderBook.Instrument, orderBook.BookID);
                    m_Market.RequestInstruments(orderBook.Instrument);
                    m_PendingQueue.AddPending(request, 2);
                    return;
                }
                else
                {   // We have the instrument details, so create the OrderInstrument now
                    Log.NewEntry(LogLevel.Minor, "ProcessCreateBook: Attempting to Process OrderBookID {0}", orderBook.BookID);
                    orderInstrument = new OrderInstrument(orderBook.Instrument, instrumentDetails, m_OrderRecycleFactory);
                    TradingTechnologies.TTAPI.InstrumentDetails ttDetails;

                    if (m_Market.TryLookupInstrumentDetails(orderBook.Instrument, out ttDetails))
                    {
                        m_InstrumentNameToTTKey[orderBook.Instrument] = ttDetails.Key;
                    }
                    if (m_OrderInstruments.TryAdd(orderBook.Instrument, orderInstrument))
                    {
                        Log.NewEntry(LogLevel.Minor, "ProcessCreateBook: Created new OrderInstrument {0}.", orderBook.Instrument);
                    }
                    else
                    {   // This could only happen if the orderInstrument somehow just now showed up spontaneously.
                        // That could happen if we allow other threads to add new OrderInstruments to list.
                        Log.NewEntry(LogLevel.Minor, "ProcessCreateBook: Failed to add new OrderInstrument {0}. Will try again later.",
                            orderBook.Instrument);
                        m_PendingQueue.AddPending(request, 1);
                        return;
                    }
                }
                // If we get here, lets try to add our new book to the order instrument.
                TTKey ttKey;
                if (orderInstrument != null && orderInstrument.TryAddBook(orderBook) &&
                    m_InstrumentNameToTTKey.TryGetValue(orderBook.Instrument, out ttKey))
                {
                    Log.NewEntry(LogLevel.Minor, "ProcessCreateBook: Added book {0} into OrderInstrument {1}.", orderBook.BookID, orderInstrument);
                    m_Listener.SubscribeToInstrument(ttKey);
                }
            }
            else
            {
                Log.NewEntry(LogLevel.Minor, "ProcessCreateBook: Exisiting OrderInstrument {0} found. Attempting to add OrderBookID {1}",
                    orderInstrument.Instrument, orderBook.BookID);
                orderInstrument.TryAddBook(orderBook);
            }
        }// ProcessCreateBook()
        //
        //
        // *********************************************
        // ****     ProcessServiceStateChange()     ****
        // *********************************************
        /// <summary>
        /// Called when someone (us or external user) wants to 
        /// change our current ServiceState.
        /// Notes:
        ///     1) The states are processed in reverse order so that if/when other states are included
        ///     in the enum, we will consider ourselves in that state if its higher or equal to the ones implemented.
        /// </summary>
        /// <param name="request"></param>
        private void ProcessServiceStateChange(RequestEventArg<RequestCode> request)
        {
            ServiceStates nextState = m_ServiceState;                   // change this if you want to move to a new state.
            ServiceStates requestedState = (ServiceStates)request.Data[0];
            Log.NewEntry(LogLevel.Warning, "ProcessStateChange: [{1}] Processing {0}.", request, m_ServiceState);
            if (m_ServiceState >= ServiceStates.Stopped)
            {   // Ok.  We must shut down now.
                m_Requests.Recycle(request);
                base.Stop();
            }
            else if (m_ServiceState >= ServiceStates.Stopping)
            {   // We are trying to stop. But havent stopped yet.                
                // Check that we can stop now.
                bool isReadyToStop = true;                      // todo: do the needed checks here.
                if (isReadyToStop)
                {
                    m_Listener.Dispose();
                    nextState = ServiceStates.Stopped;          // Okay. We can stop - set my state.
                    this.HubEventEnqueue(request);              // pulse this request again immediately.
                }
                else
                    m_PendingQueue.AddPending(request, 1);           // wait, then try to stop again.
            }
            else if (m_ServiceState >= ServiceStates.Running)
            {   // Here we are in Running state or better.
                if (requestedState >= ServiceStates.Stopping)
                {   // user wants to stop now.
                    nextState = ServiceStates.Stopping;
                    this.HubEventEnqueue(m_Requests.Get(RequestCode.ServiceStateChange, ServiceStates.Stopped));
                }
                else
                {   // Right now this is the only active state.
                    // However, we could go onto other states.. like paused, if desired in future.
                    Log.NewEntry(LogLevel.Warning, "ProcessStateChange: Ignoring request {0}", request);
                    m_Requests.Recycle(request);
                }
            }
            else if (m_ServiceState >= ServiceStates.Started)
            {   // We are atleast started now.
                if (requestedState >= ServiceStates.Stopping)
                {   // User wants to stop.  Thats okay.
                    nextState = ServiceStates.Stopping;
                    this.HubEventEnqueue(m_Requests.Get(RequestCode.ServiceStateChange, ServiceStates.Stopped));
                }
                else if (requestedState >= ServiceStates.Running)
                {   // Try to connect all services we need to be running.
                    // Locate services now.
                    List<IService> iServices;
                    if (m_TTService == null)
                    {
                        iServices = m_Services.GetServices(typeof(TTApiService));
                        if (iServices.Count > 0)
                        {
                            Log.NewEntry(LogLevel.Major, "ProcessStateChange: Starting.  Found TTAPI service. ");
                            m_TTService = (TTApiService)iServices[0];

                        }
                        else
                            Log.NewEntry(LogLevel.Error, "ProcessStateChange: Failed to locate TTAPI Service.");
                    }
                    if (m_Market == null)
                    {
                        Log.NewEntry(LogLevel.Major, "ProcessStateChange: Starting.  Found TT Market. ");
                        iServices = m_Services.GetServices(typeof(MarketTTAPI));
                        if (iServices.Count > 0)
                        {
                            m_Market = (MarketTTAPI)iServices[0];
                            m_Market.FoundResource += new EventHandler(HubEventEnqueue);        // subscribe to found resources.
                        }
                        else
                            Log.NewEntry(LogLevel.Error, "ProcessStateChange: Failed to locate TTAPI Market hub.");
                    }
                    // TODO: Continue to do connections here.
                    if (m_Market != null && m_TTService != null)
                    {
                        if (m_Listener == null)
                        {
                            m_Listener = new OrderListener("Listener", this);
                            m_Listener.Start();
                        }
                    }

                    // Try to connect.
                    bool isReadyToRun = true;
                    isReadyToRun = (m_TTService != null && m_TTService.IsRunning) && isReadyToRun;      // non-Lazy
                    isReadyToRun = (m_Market != null) && isReadyToRun;

                    if (isReadyToRun)
                    {   // Transition to Running state!
                        nextState = ServiceStates.Running;
                        if (requestedState > ServiceStates.Running)
                            HubEventEnqueue(request);                   // resubmit request since its more than Running.
                        else
                            m_Requests.Recycle(request);
                    }
                    else
                    {
                        Log.NewEntry(LogLevel.Major, "ProcessStateChange: Cannot proceed to state {0}. Not connected. Wait and try again.", requestedState);
                        m_PendingQueue.AddPending(request, 2);
                    }
                }
                else
                {

                }

            }
            else if (m_ServiceState >= ServiceStates.Unstarted)
            {   // We are marked as unstarted, but since we are here, we must at least be started.
                nextState = ServiceStates.Started;
                if (requestedState > ServiceStates.Started)
                    this.HubEventEnqueue(request);             // resubmit this request since user requested more.
                else
                    m_Requests.Recycle(request);
            }
            else
                Log.NewEntry(LogLevel.Warning, "ProcessStateChange: Unknown service state {0}", m_ServiceState);// This should never happen

            // Exit - report service state change if any.
            if (m_ServiceState != nextState)
            {                                                       // Our service state has changed.                
                ServiceStates prevState = m_ServiceState;           // save previous state.
                m_ServiceState = nextState;                         // accept new state.
                OnServiceStateChanged(prevState, m_ServiceState);
            }
        }//ProcessServiceStateChange()
        //
        //

        #endregion //Processing Methods

        #region External Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //

        //
        //
        // *****************************************************
        // ****     TTAPIService_ServiceStateChanged()      ****
        // *****************************************************
        /// <summary>
        /// We want to know the state of TT API Service.
        /// </summary>
        /* 
        private void TTAPIService_ServiceStateChanged(object sender, EventArgs eventArgs)
        {
            if ( eventArgs is ServiceStateEventArgs)
            {
                ServiceStateEventArgs e = (ServiceStateEventArgs)eventArgs;
                if (e.CurrentState >= ServiceStates.Running && m_ServiceState < ServiceStates.Running)
                {   // The first time we hear that TTAPI is running, we complete our startup.
                    this.HubEventEnqueue(m_Requests.Get(RequestCode.Run));
                }
            }
        }//TTAPIService_ServiceStateChanged()
        */
        //
        #endregion//Event Handlers

        #region IStringifiable
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        string IStringifiable.GetAttributes()
        {
            return string.Empty;
        }
        List<IStringifiable> IStringifiable.GetElements()
        {
            return null;
        }
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
            bool isTrue;
            foreach (KeyValuePair<string, string> keyVal in attributes)
            {
                if (keyVal.Key.Equals("ShowLog") && bool.TryParse(keyVal.Value, out isTrue))
                    Log.IsViewActive = isTrue;
            }
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {
        }
        //
        //
        #endregion//Event Handlers

    }//end class
}
