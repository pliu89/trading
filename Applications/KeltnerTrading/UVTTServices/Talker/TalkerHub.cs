using System;
using System.Collections.Generic;
using System.Text;

namespace UV.TTServices.Talker
{
    using UV.Lib.Application;
    using UV.Lib.Products;
    using UV.Lib.Hubs;
    using UV.Lib.Sockets;
    using UV.Lib.OrderHubs;
    using UV.Lib.IO.Xml;
    using UV.Lib.Excel.RTD;
    using UV.Lib.BookHubs;


    using UV.TTServices.Markets;
    using UV.TTServices.Fills;

    /// <summary>
    /// This is a stand-alone hub that manages a socket, allowing clients to request information
    /// from TTMarketHub, FillHub, etc.  
    /// 1) It only connects using Ambre.ExcelRTD protocols that are consistent with Ambre.XL socket server/RTD server.
    /// 2) TODO: extended this class to run a socket server, and allow other (possibly multiple) clients to connect to.
    /// 3) TODO: unrealized pnl depends on market -- not implemented yet (to decrease amount of updates).
    /// 4) TODO: Add market information.
    /// </summary>
    public class TalkerHub : Hub , IService , IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // External services
        //
        private MarketTTAPI m_MarketHub = null;
        private Dictionary<string, FillHub> m_FillHubs = new Dictionary<string, FillHub>();  // fill hubs by name.
        private SocketManager m_SocketBreTalker;

        // AmbreXL controls
        private int m_AmbreXLPort = 6012;
        private string m_AmbreXLIPAdress = string.Empty;
        private bool m_IsConnectedToClient = false;
        public int AmbreXLReconnectCounterMax = 5 * MINUTE;
        public int AmbreXLReconnectCounterMin = 5 * SECOND;
        private int m_AmbreXLReconnectCounter = 0;
        private int m_AmbreXLReconnectFailureCount = 0;                     // number of times we failed to connec



        // Subscription management
        private Dictionary<int,Topic> m_Topics = new Dictionary<int,Topic>();
        private Dictionary<string,Dictionary<InstrumentName, Dictionary<SubscriptionType, List<Topic>>>> m_SubscriptionsByHubName = new Dictionary<string,Dictionary<InstrumentName, Dictionary<SubscriptionType, List<Topic>>>>();

        // Workspace - used by hub thread only.
        private StringBuilder m_MessageWorkspace = new StringBuilder();     // place to construct socket message strings.
        private const string DefaultHubName = "****";
        private const int SECOND = 1000;                                    // number of msecs in second.
        private const int MINUTE = 60000;                                    // number of msecs in second.

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public TalkerHub() : this(false,6012)                               // needed by IStringifiable
        {

        }
        public TalkerHub(bool isLogViewerVisible, int ambreXLPort=6012)
            : base("Talker", UV.Lib.Application.AppInfo.GetInstance().LogPath, isLogViewerVisible, LogLevel.ShowAllMessages)
        {
            #if (!DEBUG)
                Log.IsViewActive = false;
            #endif

            m_AmbreXLPort = ambreXLPort;
            base.m_WaitListenUpdatePeriod = 1000;

        }//constructor
        //
        //       
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        public bool IsConnectedToClient
        {
            get { return m_IsConnectedToClient; }
        }
        public int Port
        {
            get { return this.m_AmbreXLPort; }
        }
        public string ServiceName
        {
            get { return string.Format("TalkerHub{0}", this.m_AmbreXLPort); }
        }
        //
        #endregion//Properties


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        /// <summary>
        /// External user can request for TalkerHub to talk various actions.
        /// </summary>
        public bool Request(TalkerHubRequest request)
        {
            return HubEventEnqueue(new TalkerHubEventArg(request));
        }
        public override void Start()
        {
            //UV.Lib.Application.AppServices sevices = UV.Lib.Application.AppServices.GetInstance();
            //foreach (IService service in sevices.Services.Values)
            //{
            //    if (service is FillHub)
            //        this.RequestAddHub((FillHub) service);
            //    else if (service is MarketTTAPI)
            //        this.RequestAddHub((MarketTTAPI)service);
            //}
            foreach (IService service in AppServices.GetInstance().GetServices(typeof(FillHub)))
                this.RequestAddHub((FillHub)service);
            foreach (IService service in AppServices.GetInstance().GetServices(typeof(MarketTTAPI)))
                this.RequestAddHub((MarketTTAPI)service);
            base.Start();
        }
        public void Connect()
        {
            //throw new NotImplementedException();
        }
        //
        //
        /*
        public bool RequestAddHub(FillHub fillHub)
        {
            string name = fillHub.Name;
            if (string.IsNullOrEmpty(name))
                name = DefaultHubName; 
            // Submit request to hub thread.
            TalkerHubEventArg request = new TalkerHubEventArg(TalkerHubRequest.RequestAddHub);
            request.Data = new List<object>();
            request.Data.Add(fillHub);
            return HubEventEnqueue(request);
        }// ConnectHub()
        //
        */ 
        public bool RequestAddHub(Hub hub)
        {
            string name = hub.HubName;
            if (string.IsNullOrEmpty(name))
                name = DefaultHubName;
            // Submit request to hub thread.
            TalkerHubEventArg request = new TalkerHubEventArg(TalkerHubRequest.RequestAddHub);
            request.Data = new List<object>();
            request.Data.Add(hub);
            return HubEventEnqueue(request);
        }// ConnectHub()
        //
        //
        public override void RequestStop()
        {
            if ( m_SocketBreTalker != null)
                m_SocketBreTalker.Close();
            base.Stop();
        }
        #endregion//Public Methods



        #region Hub Event Handlers & Processing
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        protected override void HubEventHandler(EventArgs[] eventArgList)
        {
            foreach (EventArgs eventArg in eventArgList)
            {
                Type eventType = eventArg.GetType();
                if (eventType == typeof(FillHub.PositionBookChangedEventArgs))
                    ProcessFillHubEvent((FillHub.PositionBookChangedEventArgs) eventArg);   // event from FillHub.
                else if (eventType == typeof(TalkerHubEventArg))
                    ProcessRequest((TalkerHubEventArg)eventArg);                            // Request from local user.
                else if (eventType == typeof(SocketEventArgs))
                    ProcessSocketEvent((SocketEventArgs)eventArg);                          // Request from non-local socket client.
                else
                    Log.NewEntry(LogLevel.Major, "Unknown hub event {0}.", eventArg.ToString());
            }            
        }//HubEventHandler()
        //
        //
        // *********************************************************
        // ****             Process Hub Request                 ****
        // *********************************************************
        /// <summary>
        /// Direct requests from outsiders for managing this TalkerHub.
        /// </summary>
        private void ProcessRequest(TalkerHubEventArg eventArg)
        {
            switch (eventArg.Request)
            {
                case TalkerHubRequest.AmberXLConnect:                     // try to connect to Excel
                    if (m_SocketBreTalker == null)
                    {
                        m_SocketBreTalker = new SocketManager();
                        m_SocketBreTalker.Connected += new EventHandler(HubEventEnqueue);
                        m_SocketBreTalker.Disconnected += new EventHandler(HubEventEnqueue);
                        m_SocketBreTalker.MessageReceived += new EventHandler(HubEventEnqueue);
                    }
                    if (!TryToConnectToExcel())
                    {   // Failed to connect.  
                        // TODO: Put this request in our waiting queue and try again periodically. 
                        m_AmbreXLReconnectFailureCount ++;
                        m_AmbreXLReconnectCounter = Math.Min(AmbreXLReconnectCounterMin * m_AmbreXLReconnectFailureCount, AmbreXLReconnectCounterMax);
                        Log.NewEntry(LogLevel.Major, "ProcessRequest: Failed {0} times to connect to socket. Try again in {1:0.0} seconds.", m_AmbreXLReconnectFailureCount, (m_AmbreXLReconnectCounter / 1000.0));
                    }
                    else
                    {
                        Log.NewEntry(LogLevel.Major, "ProcessRequest: Connected to Socket.");                        
                    }
                    break;
                case TalkerHubRequest.AmbreXLDisconnect:
                    m_SocketBreTalker.StopConversation();
                    break;
                case TalkerHubRequest.StopService:
                    if (m_SocketBreTalker != null)
                    {
                        m_SocketBreTalker.StopConversation();
                        if (m_SocketBreTalker.IsServing)
                            m_SocketBreTalker.StopServer();
                    }
                    this.Stop();
                    break;
                case TalkerHubRequest.RequestAddHub:
                    if (eventArg.Data != null)
                    {
                        foreach (object newHub in eventArg.Data)
                        {
                            if (newHub is FillHub)
                            {
                                FillHub newFillHub = (FillHub)newHub;
                                FillHub oldFillHub;
                                if (m_FillHubs.TryGetValue(newFillHub.Name, out oldFillHub))
                                {   // User must want to replace fill hub with same name...
                                    m_FillHubs.Remove(oldFillHub.Name);
                                    oldFillHub.PositionBookCreated -= new EventHandler(HubEventEnqueue);    // unsubscribe to this old hub
                                    oldFillHub.PositionBookChanged -= new EventHandler(HubEventEnqueue);
                                    oldFillHub.PositionBookDeleted -= new EventHandler(HubEventEnqueue);
                                }
                                m_FillHubs.Add(newFillHub.Name, newFillHub);
                                newFillHub.PositionBookCreated += new EventHandler(HubEventEnqueue);        // subscribe to this new hub
                                newFillHub.PositionBookChanged += new EventHandler(HubEventEnqueue);
                                newFillHub.PositionBookDeleted += new EventHandler(HubEventEnqueue);
                            }// if new hub is FillHub type.
                            else if (newHub is MarketTTAPI)
                            {
                                //UV.Lib.MarketHubs.MarketHub marketHub = (UV.Lib.MarketHubs.MarketHub)newHub;
                                MarketTTAPI marketHub = (MarketTTAPI)newHub;
                                m_MarketHub = marketHub;

                            }
                        }//for each hub passed to us.
                    }// if request contains data.
                    break;
                default:
                    break;
            }
        }//ProcessRequest()
        //
        //
        // *********************************************************
        // ****             Process FillHub Event               ****
        // *********************************************************
        /// <summary>
        /// Processes events from a fill hub.  Events include PositionBook changed or created events.
        /// The fillhub ptr and InstrumentName (corresponding to the book) is passed to us.  We lookup any subscription 
        /// topics for these.
        /// </summary>
        /// <param name="eventArg"></param>
        private void ProcessFillHubEvent(FillHub.PositionBookChangedEventArgs eventArg)
        {
            FillHub callingHub = eventArg.Sender;                               // pointer to the particular FillHub from which event was created.            
            string name = callingHub.Name;
            if (string.IsNullOrEmpty(name))
                name = DefaultHubName;
            InstrumentName instrument = eventArg.Instrument;

            // Search for the FillHub in our list of subscriptions.
            Dictionary<InstrumentName, Dictionary<SubscriptionType, List<Topic>>> m_Subscriptions = null;
            if (!m_SubscriptionsByHubName.TryGetValue(name, out m_Subscriptions))
                return;                                                         

            Dictionary<SubscriptionType, List<Topic>> subscriptionList;
            if (m_Subscriptions.TryGetValue(instrument, out subscriptionList) && subscriptionList.Count > 0)
            {   // We have some active subscriptions for this instrument.                

                string newValue ;
                FillBookLifo book;
                if (callingHub.TryEnterReadBook(instrument, out book))
                {   // We found the book that changed its position.  Update all things that may have changed.

                    //
                    // Update everything about this book.
                    //
                    List<Topic> topicList;
                    if (subscriptionList.TryGetValue(SubscriptionType.Position, out topicList))
                    {
                        newValue = book.NetPosition.ToString();
                        foreach (Topic topic in topicList)
                            if (!topic.PeekAtValue().Equals(newValue))
                                topic.SetValue(newValue);
                    }
                    if (subscriptionList.TryGetValue(SubscriptionType.AvePositionCost, out topicList))
                    {
                        newValue = book.AveragePrice.ToString();
                        foreach (Topic topic in topicList)
                            if (!topic.PeekAtValue().Equals(newValue))
                                topic.SetValue(newValue);
                    }
                    if (subscriptionList.TryGetValue(SubscriptionType.RealPnL, out topicList))
                    {
                        newValue = book.RealizedDollarGains.ToString();
                        foreach (Topic topic in topicList)
                            if (!topic.PeekAtValue().Equals(newValue))
                                topic.SetValue(newValue);
                    }
                    if (subscriptionList.TryGetValue(SubscriptionType.StartingRealPnL, out topicList))
                    {
                        newValue = book.RealizedStartingDollarGains.ToString();
                        foreach (Topic topic in topicList)
                            if (!topic.PeekAtValue().Equals(newValue))
                                topic.SetValue(newValue);
                    }
                    if (subscriptionList.TryGetValue(SubscriptionType.UnRealPnL, out topicList))
                    {
                        newValue = book.UnrealizedDollarGains().ToString();
                        foreach (Topic topic in topicList)
                            if (!topic.PeekAtValue().Equals(newValue))
                                topic.SetValue(newValue);
                    }
                    if (subscriptionList.TryGetValue(SubscriptionType.Volume, out topicList))
                    {
                        newValue = book.Volume.ToString();
                        foreach (Topic topic in topicList)
                            if (!topic.PeekAtValue().Equals(newValue))
                                topic.SetValue(newValue);
                    }
                    if (subscriptionList.TryGetValue(SubscriptionType.StartingVolume, out topicList))
                    {
                        newValue = book.StartingVolume.ToString();
                        foreach (Topic topic in topicList)
                            if (!topic.PeekAtValue().Equals(newValue))
                                topic.SetValue(newValue);
                    }


                    callingHub.ExitReadBook(instrument);
                }
                else
                {   // We have a subscription request for this instrument, we obtained an event from it, 
                    // yet, when we inquire about this instrument, it is unknown!?!?
                    // This CAN happen whenever an specific fill book is deleted by the user.
                    List<Topic> topicList;
                    newValue = "0";
                    if (subscriptionList.TryGetValue(SubscriptionType.Position, out topicList))
                    {
                        foreach (Topic topic in topicList)
                            if (!topic.PeekAtValue().Equals(newValue))
                                topic.SetValue(newValue);
                    }
                    if (subscriptionList.TryGetValue(SubscriptionType.AvePositionCost, out topicList))
                    {                       
                        foreach (Topic topic in topicList)
                            if (!topic.PeekAtValue().Equals(newValue))
                                topic.SetValue(newValue);
                    }
                    if (subscriptionList.TryGetValue(SubscriptionType.RealPnL, out topicList))
                    {
                        foreach (Topic topic in topicList)
                            if (!topic.PeekAtValue().Equals(newValue))
                                topic.SetValue(newValue);
                    }
                    if (subscriptionList.TryGetValue(SubscriptionType.StartingRealPnL, out topicList))
                    {
                        foreach (Topic topic in topicList)
                            if (!topic.PeekAtValue().Equals(newValue))
                                topic.SetValue(newValue);
                    }
                    if (subscriptionList.TryGetValue(SubscriptionType.UnRealPnL, out topicList))
                    {
                        foreach (Topic topic in topicList)
                            if (!topic.PeekAtValue().Equals(newValue))
                                topic.SetValue(newValue);
                    }
                    if (subscriptionList.TryGetValue(SubscriptionType.Volume, out topicList))
                    {
                        foreach (Topic topic in topicList)
                            if (!topic.PeekAtValue().Equals(newValue))
                                topic.SetValue(newValue);
                    }
                    if (subscriptionList.TryGetValue(SubscriptionType.StartingVolume, out topicList))
                    {
                        foreach (Topic topic in topicList)
                            if (!topic.PeekAtValue().Equals(newValue))
                                topic.SetValue(newValue);
                    }
                }
            }
            //else
            //    Log.NewEntry(LogLevel.Minor, "ProcessFillHubEvent: No subscriptions for {0}.",instrument);   // user hasn't subscribed to this instrument.
        }//ProcessFillHubEvent().
        //
        //
        //
        //
        //
        // *********************************************************
        // ****             Process Socket Event                ****
        // *********************************************************
        /// <summary>
        /// This is an event received from the socket. (Its usually of type "MessageReceived" from socket, 
        /// which is from BreTalker (on the other end of the socket connection), which needs to be decoded.
        /// </summary>
        private void ProcessSocketEvent(SocketEventArgs eventArgs)
        {
            // TODO: it'd be convenient if the SocketEventArgs told us exactly which SocketManager/Conversation sent the event.
            // In cases there are multiple socket clients.
            Log.NewEntry(LogLevel.Major, "SocketEvent [{0}] {1}", eventArgs.EventType.ToString(), eventArgs.Message);
            if (eventArgs.EventType == SocketEventType.MessageReceived)
            {
                // Analyze message from 
                MessageType messageType;
                int topicID;
                string currentValue;
                string[] args;
                Topic topic;
                if (TopicBase.TryReadSerialString(eventArgs.Message, out messageType, out topicID, out currentValue, out args))
                {
                    switch (messageType)
                    {
                        case MessageType.TopicArgs:
                            if (m_Topics.TryGetValue(topicID, out topic))
                            {   // This is an resending of a topic we already know.
                                // TODO: update this? dunno.
                                Log.NewEntry(LogLevel.Major, "Received TopicArgs for an old topic {0}, Message = {1}.  Will remove, and replace with new topic.", topic.ToString(), eventArgs.Message);
                                if (TryRemoveTopic(topicID))
                                    Log.NewEntry(LogLevel.Minor, "Removed previous topicID = {0}.", topicID);
                                else
                                    Log.NewEntry(LogLevel.Minor, "Failed to remove topicID = {0}.", topicID);
                                //if (!currentValue.Equals(topic.PeekAtValue())) //topic.SetValue(currentValue);               // at least update its current value.
                                //    topic.IsChangedSinceLastRead = true;            // tell excel that this is the current value.
                            }
                            if (!m_Topics.TryGetValue(topicID, out topic))
                            {   // New topic received, try to create a new specific topic.
                                if (Topic.TryCreate(topicID, args, currentValue, out topic))
                                {
                                    m_Topics.Add(topicID, topic);                               // add new topic to our lists.
                                    CategorizeTopic(topic);
                                }
                                else
                                {   // Failed to create a new topic!
                                    Log.NewEntry(LogLevel.Major, "Failed to create new topic for {0}.", eventArgs.Message);
                                }
                            }
                            break;
                        case MessageType.Current:
                            if (m_Topics.TryGetValue(topicID, out topic))
                            {
                                Log.NewEntry(LogLevel.Minor, "Received Current value response for topic {0}, Msg = {1}.",topic.ToString(), eventArgs.Message);
                                topic.IsChangedSinceLastRead = true;        // tell the user that THIS is the correct value.
                                // topic.SetValue(currentValue);
                                // Inform subscribers that this value is now up to date.
                            }
                            else
                                Log.NewEntry(LogLevel.Minor, "Topic current message received.  Failed to find topicID = {0}.", topicID);
                            break;
                        case MessageType.TopicRemoved:
                            if (TryRemoveTopic(topicID) )
                                Log.NewEntry(LogLevel.Minor, "Topic removed msg received. TopicID = {0} removed.  Msg = {1}", topicID, eventArgs.Message);
                            else
                                Log.NewEntry(LogLevel.Minor, "Topic removed msg received. Failed to find topicID = {0}.  Msg = {1}", topicID, eventArgs.Message);
                            break;
                        default:
                            break;
                    }//switch
                }// if deserialized new topic.
                else
                    Log.NewEntry(LogLevel.Warning, "Failed to deserialize the NEW topic from message {0}", eventArgs.Message);
            }
            else if (eventArgs.EventType == SocketEventType.Disconnected)
            {
                m_IsConnectedToClient = false;
                OnStateChanged();
            }
            else if (eventArgs.EventType == SocketEventType.Connected)
            {
                m_IsConnectedToClient = true;
                m_AmbreXLReconnectFailureCount = 0;                     // this is the flag for we are NOT trying to reconnect.                
                OnStateChanged();
            }
            else if (eventArgs.EventType == SocketEventType.InternalMessage)
            {

            }
            else
                Log.NewEntry(LogLevel.Major, "[{0}] unhandled socket event {1}", eventArgs.EventType.ToString(), eventArgs.Message);
        }// ProcessSocketEvent()
        //
        //
        //
        //
        #endregion//Event Handlers


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        //
        // *********************************************************
        // ****                 RemoveTopic()                   ****
        // *********************************************************
        private bool TryRemoveTopic(int topicID)
        {
            Topic topic;
            if (m_Topics.TryGetValue(topicID, out topic))
            {

                string hubName = topic.HubName;
                if (string.IsNullOrEmpty(hubName))
                    hubName = DefaultHubName;
                // New
                Dictionary<InstrumentName, Dictionary<SubscriptionType, List<Topic>>> m_Subscriptions = null;
                if (m_SubscriptionsByHubName.TryGetValue(hubName, out m_Subscriptions))
                {
                    Dictionary<SubscriptionType, List<Topic>> subscriptionList;
                    List<Topic> topicList;
                    if (m_Subscriptions.TryGetValue(topic.Instrument, out subscriptionList) && subscriptionList.TryGetValue(topic.Type, out topicList) && topicList.Contains(topic))
                        topicList.Remove(topic);
                }
                m_Topics.Remove(topicID);
                return true;
                // Inform subscribers that this topic is no longer needed?
            }
            else
            {   // This can happen since Excel never reuses topic Ids.  So even if before we connected the Excel user
                // deleted an RTD link today, Excel will tell us its been removed.
                //Log.NewEntry(LogLevel.Minor, "Topic removed msg received. Failed to find topicID = {0}.  Msg = {1}", topicID, eventArgs.Message);
                return false;
            }
        }// RemoveTopic
        //
        //
        //
        //
        //
        // *********************************************************
        // ****             TryToConnectToExcel()               ****
        // *********************************************************
        private bool TryToConnectToExcel()
        {           
            System.Net.IPAddress ipAddress;
            if (!System.Net.IPAddress.TryParse(m_AmbreXLIPAdress, out ipAddress))
                ipAddress = System.Net.IPAddress.Loopback;
            bool isConnected = m_SocketBreTalker.ConnectToServer(ipAddress, m_AmbreXLPort);
            if (isConnected)
            {

            }
            else
            {
            }
            return isConnected;
        }// TryToConnectToExcel()
        //
        //
        //
        // *********************************************************
        // ****                 Update Periodic                 ****
        // *********************************************************
        /// <summary>
        /// Pushes to excel are throttled.  Each topic is queried here and 
        /// a message string is created and sent to the socket.
        /// </summary>
        protected override void UpdatePeriodic()
        {
            if ( m_MarketHub != null )
                UpdateMarketTopics();

            // Create message to send to BreTalker socket in Excel.
            m_MessageWorkspace.Clear();
            foreach (Topic topic in m_Topics.Values)
            {
                if (topic.IsChangedSinceLastRead)
                {
                    topic.IsChangedSinceLastRead = true;
                    m_MessageWorkspace.AppendFormat("{0}", topic.SerializeCurrent(MessageType.RequestChange));
                }
            }
            if (m_MessageWorkspace.Length > 0)
                m_SocketBreTalker.Send(m_MessageWorkspace.ToString());

            // Try reconnect
            if (m_AmbreXLReconnectFailureCount > 0 && (m_AmbreXLReconnectCounter -= base.m_WaitListenUpdatePeriod) < 0)
            {
                Log.NewEntry(LogLevel.Major, "UpdatePeriodic: Requesting a reconnectioning to socket.");
                this.Request(TalkerHubRequest.AmberXLConnect);
            }

        }// UpdatePeriodic().
        //
        //
        Dictionary<InstrumentName, double> m_MarketPrices = new Dictionary<InstrumentName, double>();
        //
        // *****************************************************************
        // ****                 Update Market Topics()                  ****
        // *****************************************************************
        /// <summary>
        /// In order to throttle market updates, this method is called periodically.
        /// It updates subscriptions for the market hub directly, and also searches 
        /// thru each FillHub topics that are market dependent and updates the topics.
        /// </summary>
        private void UpdateMarketTopics()
        {
            // Update all markets
            Book mktBook = null ;
            if (! m_MarketHub.TryEnterReadBook(out mktBook) )
                return;
            


            // Update MarketHub topics
            // TODO: Introduce a Subscription type for market requests, bid/ask etc.

            //
            // Get all market dependent Fill topics.
            //
            Dictionary<InstrumentName,Dictionary<SubscriptionType, List<Topic>>> subscriptions;
            foreach (string hubName in m_FillHubs.Keys)
            {
                if (m_SubscriptionsByHubName.TryGetValue(hubName, out subscriptions))   // handle any direct market requests.
                {
                    foreach (InstrumentName instrumentName in subscriptions.Keys)
                    {
                        // Get last price for this instrument.                        
                        Dictionary<SubscriptionType, List<Topic>> subscriptionList;
                        List<Topic> topicList;
                        FillBookLifo fillBook;
                        int instrumentID;
                        if (subscriptions.TryGetValue(instrumentName, out subscriptionList) 
                            && subscriptionList.TryGetValue(SubscriptionType.UnRealPnL, out topicList) 
                            && m_FillHubs[hubName].TryEnterReadBook(instrumentName,out fillBook) )
                        {
                            if (m_MarketHub.TryLookupInstrumentID(instrumentName, out instrumentID))
                            {   // We have found topics for UnRealPnL.
                                double lastPrice = mktBook.Instruments[instrumentID].LastPrice;
                                string newValue = fillBook.UnrealizedDollarGains(lastPrice).ToString();
                                foreach (Topic topic in topicList)
                                {
                                    if (!topic.PeekAtValue().Equals(newValue))
                                        topic.SetValue(newValue);
                                }
                            }
                            m_FillHubs[hubName].ExitReadBook(instrumentName);
                        }
                    }//next instrumentName
                }
            }// next fillhub hubName

            // Exit
            m_MarketHub.ExitReadBook(mktBook);
        }//UpdateMarketTopics.
        //
        //
        //
        //
        private void CategorizeTopic(Topic topic)
        {
            string hubName = topic.HubName;
            if (string.IsNullOrEmpty(hubName))
                hubName = DefaultHubName;

            // New
            Dictionary<InstrumentName, Dictionary<SubscriptionType, List<Topic>>> m_Subscriptions = null;
            if (! m_SubscriptionsByHubName.TryGetValue(hubName,out m_Subscriptions))   
            {   // Create a new subscription list for this hub.
                m_Subscriptions = new Dictionary<InstrumentName, Dictionary<SubscriptionType, List<Topic>>>();
                m_SubscriptionsByHubName.Add(hubName, m_Subscriptions); 
            }

            // Create subscription lists for any new Instruments found.
            if (topic.Type != SubscriptionType.Unknown && ! m_Subscriptions.ContainsKey(topic.Instrument) )
                m_Subscriptions.Add(topic.Instrument, new Dictionary<SubscriptionType, List<Topic>>());

            Dictionary<SubscriptionType, List<Topic>> subscriptionList;                      // A list of all subscriptions of the desired type!
            if (m_Subscriptions.TryGetValue(topic.Instrument, out subscriptionList))
            {
                if (!subscriptionList.ContainsKey(topic.Type))
                    subscriptionList.Add(topic.Type, new List<Topic>());
                List<Topic> topicList;
                if (subscriptionList.TryGetValue(topic.Type, out topicList))                // list of subscriptions for desired type and instrument.
                {
                    if (!topicList.Contains(topic))
                    {   // This is a new topic.  The user may ask for same subscription multiple times.
                        // That would be inefficient, but if he does then this list will contain multiple topics
                        // that all show the same information.
                        Log.NewEntry(LogLevel.Minor, "New subscription {0}.",topic);
                        topicList.Add(topic);
                    }
                    else
                    {   // We have already categorized this topic.  Do nothing.

                    }
                }
                else
                    Log.NewEntry(LogLevel.Warning, "CategorizeTopic problem 1.");
            }
            else
                Log.NewEntry(LogLevel.Warning, "CategorizeTopic problem 2.");
            //
            // Calculate initial value.
            //
            FillHub m_FillHub = null;
            if (m_FillHubs.TryGetValue(hubName, out m_FillHub))
            {
                switch (topic.Type)
                {
                    case SubscriptionType.Position:
                        FillBookLifo book;
                        if (m_FillHub.TryEnterReadBook(topic.Instrument, out book))
                        {
                            topic.SetValue(book.NetPosition.ToString());
                            m_FillHub.ExitReadBook(topic.Instrument);
                        }
                        else
                            topic.SetValue("0");
                        break;
                    case SubscriptionType.AvePositionCost:
                        if (m_FillHub.TryEnterReadBook(topic.Instrument, out book))
                        {
                            topic.SetValue(book.AveragePrice.ToString());
                            m_FillHub.ExitReadBook(topic.Instrument);
                        }
                        else
                            topic.SetValue("");
                        break;
                    case SubscriptionType.RealPnL:
                        if (m_FillHub.TryEnterReadBook(topic.Instrument, out book))
                        {
                            topic.SetValue(book.RealizedDollarGains.ToString());
                            m_FillHub.ExitReadBook(topic.Instrument);
                        }
                        else
                            topic.SetValue("");
                        break;
                    case SubscriptionType.StartingRealPnL:
                        if (m_FillHub.TryEnterReadBook(topic.Instrument, out book))
                        {
                            topic.SetValue(book.RealizedStartingDollarGains.ToString());
                            m_FillHub.ExitReadBook(topic.Instrument);
                        }
                        else
                            topic.SetValue("");
                        break;                        
                    case SubscriptionType.UnRealPnL:
                        if (m_FillHub.TryEnterReadBook(topic.Instrument, out book))
                        {

                            topic.SetValue(book.UnrealizedDollarGains().ToString());
                            m_FillHub.ExitReadBook(topic.Instrument);
                        }
                        else
                            topic.SetValue("");
                        break;
                    case SubscriptionType.Volume:
                        if (m_FillHub.TryEnterReadBook(topic.Instrument, out book))
                        {

                            topic.SetValue(book.Volume.ToString());
                            m_FillHub.ExitReadBook(topic.Instrument);
                        }
                        else
                            topic.SetValue("");
                        break;
                    case SubscriptionType.StartingVolume:
                        if (m_FillHub.TryEnterReadBook(topic.Instrument, out book))
                        {

                            topic.SetValue(book.StartingVolume.ToString());
                            m_FillHub.ExitReadBook(topic.Instrument);
                        }
                        else
                            topic.SetValue("");
                        break;

                    default:
                        break;
                }
            }
            else
                topic.SetValue("Unknown HubName");
        }//CategorizeTopic()
        //
        //
        //
        #endregion//Private Methods



        #region Events and Triggers
        // *****************************************************************
        // ****                Events and Triggers                     ****
        // *****************************************************************
        //
        public event EventHandler ServiceStateChanged;
        //
        private void OnStateChanged()
        {
            if (ServiceStateChanged != null)
                ServiceStateChanged(this, EventArgs.Empty);        // TODO: in future, consider telling subscribers what changed.
        }

        #endregion//Event & Triggers



        #region IStringifiable
        public string GetAttributes()
        {
            StringBuilder s = new StringBuilder();
            s.AppendFormat("Port={0}", this.m_AmbreXLPort);
            return s.ToString();
        }
        public List<IStringifiable> GetElements()
        {
            return null;
        }
        public void SetAttributes(Dictionary<string, string> attributes)
        {
            int n = 0;
            foreach (string key in attributes.Keys)
                if (key.Equals("Port") && int.TryParse(attributes[key], out n))
                    this.m_AmbreXLPort = n;                    

        }
        public void AddSubElement(IStringifiable subElement)
        {            
        }
        #endregion // IStringifiable



    }
}
