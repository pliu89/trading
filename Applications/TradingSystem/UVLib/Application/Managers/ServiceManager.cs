using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.Application.Managers
{
    using System.Net;
    using System.IO;

    using UV.Lib.Application;
    using UV.Lib.IO.Xml;
    using UV.Lib.Sockets;
    using UV.Lib.Hubs;
    using UV.Lib.Engines;
    using UV.Lib.Utilities;

    /// <summary>
    /// This object mananges communication between local services 
    /// registered in the AppServices object, and those of another
    /// instance located on a foreign location.
    /// To acheive this it connects to other ServiceManagers via a
    /// socket.
    /// </summary>
    public class ServiceManager : Hub, IService, IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        // Local services
        //
        private AppServices m_AppServices = null;                           // my local app service
        //private string m_ServiceName = "ServiceManager";                    // my local service name.
        private ServiceStates m_ServiceState = ServiceStates.Unstarted;     // starting service state
        private SocketManager m_Socket = null;
        private string m_ListenToPort = string.Empty;                       // if empty, don't accept incoming connections.
        private string m_ListenToIp = string.Empty;

        //
        // Local IServices or IEngineHubs we monitor.
        //
        private Dictionary<string, IEngineHub> m_EngineHubSubcriptions = new Dictionary<string, IEngineHub>();  // local hubs, I am subscribed to.
        
        //
        // Foreign connections and services
        //
        private List<ForeignConnection> m_ForeignConnections = new List<ForeignConnection>();// connection info for each foreign server.
        private List<ForeignServer> m_ServersDisconnected = new List<ForeignServer>();      // represents a single foreign ServiceManager we are connected to.
        private Dictionary<int, ForeignServer> m_ServersConnected = new Dictionary<int, ForeignServer>(); //key by their conversation.

        //
        // My internal work spaces
        //
        private RequestFactory<RequestCode> m_Requests = new RequestFactory<RequestCode>(); // private Requests recycled here.
        private RecycleFactory<Message> m_Messages = new RecycleFactory<Message>();        
        //private EventWaitQueueLite m_PendingRequests = null;



        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public ServiceManager() : base("ServiceManager",AppServices.GetInstance().Info.LogPath,false,LogLevel.ShowAllMessages)
        {
            m_AppServices = AppServices.GetInstance();
            base.m_WaitListenUpdatePeriod = 2000;                        
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
        // *************************************************
        // ****             SendMessage()               ****
        // *************************************************
        /// <summary>
        /// Simple method to send a Message to a specific ServiceManager
        /// on a specific socket conversation.  
        /// </summary>
        /// <param name="conversationId">Conversation to send message on, -1 means all conversations.</param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool SendMessage(int conversationId, Message msg)
        {
            string s = string.Format("{0}\n",Stringifiable.Stringify(msg,null,false));
            m_Messages.Recycle(msg);
            if (conversationId >= 0)
                return m_Socket.Send(s, conversationId);
            else
                return m_Socket.Send(s);
        }
        //
        // *****************************************
        // ****         Send Services()         ****
        // *****************************************
        /// <summary>
        /// Sends Messages specific to the addition/removal of 
        /// local services.  These messages are broadcast to all
        /// currently connected servers.
        /// TODO: Perhaps we need only broadcast to those connections
        /// that initiated the connection?
        /// </summary>
        /// <param name="conversationId"></param>
        /// <param name="msgType"></param>
        /// <param name="iServiceList"></param>
        private void SendServices(int conversationId, MessageType msgType, List<IService> iServiceList)
        {
            Message msg = GetMessage(MessageType.ServiceAdded, MessageState.Confirmed);
            foreach (IService iService in iServiceList)
            {
                if (iService is ServiceManager)
                    continue;                           // skip services we don't want to list on remote server.
                else if (iService is IEngineHub)
                {   // EngineHubs are special kinds of Services.
                    ForeignEngineHub service = new ForeignEngineHub(iService);
                    msg.Data.Add(service);
                }
                else
                {   // All other services.
                    ForeignService service = new ForeignService(iService);
                    msg.Data.Add(service);
                }
            }
            SendMessage(conversationId, msg);
        }// SendServices()
        //
        //
        #endregion//Public Methods


        #region Hub Event Processing 
        // *****************************************************************
        // ****                 Hub Event Handler                       ****
        // *****************************************************************
        protected override void HubEventHandler(EventArgs[] eventArgs)
        {
            foreach (EventArgs eventArg in eventArgs)
            {
                if (eventArg is RequestEventArg<RequestCode>)
                {
                    RequestEventArg<RequestCode> requestEventArg = (RequestEventArg<RequestCode>)eventArg;
                    switch (requestEventArg.RequestType)
                    {
                        case RequestCode.ServiceStateChange:
                            ProcessRequestServiceState(requestEventArg);    // request to change state of this service
                            break;
                        case RequestCode.ForeignServiceConnect:
                            ProcessRequestServerConnect(requestEventArg);   // request to connect to foreign server
                            break;
                        default:
                            Log.NewEntry(LogLevel.Major, "HubEventHandler: Unknown request {0}.", requestEventArg);
                            m_Requests.Recycle(requestEventArg);
                            break;
                    }//code switch        
                }
                else if (eventArg is SocketEventArgs)                       // Event from Socket
                    ProcessSocketEvent((SocketEventArgs)eventArg);
                else if (eventArg is AppServiceEventArg)                    // Event from AppService
                    ProcessAppServiceEvent((AppServiceEventArg)eventArg);
                else if (eventArg is EngineEventArgs)                       // Event from local IEngineHub
                    ProcessEngineEvent((EngineEventArgs)eventArg);
                else
                    Log.NewEntry(LogLevel.Minor, "HubEventHandler: Unknown event {0}", eventArg);
            }//next eventArg
        }//HubEventHandler()
        //
        //
        //
        // *************************************************
        // ****     Process Request Service State       ****
        // *************************************************
        /// <summary>
        /// Internal or external requests for this object to change its
        /// own state, states like Started, Running, and Stopped.
        /// Some states need to make sure certain features are running before
        /// they allow us to proceed to another state.
        /// Intialization is triggered here.
        /// </summary>
        /// <param name="requestEventArg"></param>
        private void ProcessRequestServiceState(RequestEventArg<RequestCode> requestEventArg)
        {
            ServiceStates currentState = m_ServiceState;
            ServiceStates requestedState = (ServiceStates) requestEventArg.Data[0];
            ServiceStates newState = ServiceStates.None;
            switch(currentState)                                // process based on current state.
            {
                case ServiceStates.Unstarted:                               // unstarted only while there is no thread.
                    
                    //
                    // Start socket
                    //
                    if (m_Socket == null)
                    {
                        m_Socket = new SocketManager();
                        m_Socket.Connected += new EventHandler(HubEventEnqueue);
                        m_Socket.Disconnected += new EventHandler(HubEventEnqueue);
                        m_Socket.InternalMessage += new EventHandler(HubEventEnqueue);
                        m_Socket.MessageReceived += new EventHandler(HubEventEnqueue);
                        int port = 0;
                        if (string.IsNullOrEmpty(m_ListenToPort))
                            Log.NewEntry(LogLevel.Major, "Process: Service manager socket server remaining off. ");
                        else if (int.TryParse(m_ListenToPort, out port))
                        {
                            Log.NewEntry(LogLevel.Major, "Process: Service manager socket server listening to port: {0} ", port);
                            if (string.IsNullOrEmpty(m_ListenToIp))
                                m_Socket.StartServer(port);
                            else
                                m_Socket.StartServer(port,m_ListenToIp);
                        }
                        else
                            Log.NewEntry(LogLevel.Major, "Process: Service manager socket server remaining off. Invalid port: {0}", m_ListenToPort);
                    }
                    //
                    // Subscribe to all Engines found locally.
                    //
                    m_AppServices.ServiceAdded += new EventHandler(this.HubEventEnqueue);       // subscribe to hear about new services.
                    m_AppServices.ServiceStopped += new EventHandler(this.HubEventEnqueue);     
                    List<IService> engineHubs = m_AppServices.GetServices(typeof(IEngineHub));
                    foreach (IService service in engineHubs)
                    {
                        if (m_EngineHubSubcriptions.ContainsKey(service.ServiceName) )
                            Log.NewEntry(LogLevel.Minor, "Process: Already found local engine hub {0}. Skipping.", service.ServiceName);
                        else
                        {   // Subscribe to engine events of this discovered engine hub.
                            // These events I will re-broadcast to my counterparts on other foreign servers.
                            Log.NewEntry(LogLevel.Minor, "Process: Found local engine hub {0}",service.ServiceName);                            
                            m_EngineHubSubcriptions.Add(service.ServiceName, (IEngineHub) service);
                            ((IEngineHub)service).EngineChanged += new EventHandler(HubEventEnqueue);
                        }
                    }

                    // Advance to next state
                    newState = ServiceStates.Started;                       // We are started 
                    break;
                case ServiceStates.Started:                                 // Stop in this state until we allow to be in run state.
                    if (requestedState == ServiceStates.Stopped || requestedState == ServiceStates.Stopping)
                    {   // User wants to shutdown now.
                        newState = ServiceStates.Stopping;
                        HubEventEnqueue(m_Requests.Get(RequestCode.ServiceStateChange, ServiceStates.Stopped));// resubmit our request to stop.                        
                    }
                    else if (requestedState >= ServiceStates.Running)
                    {   // User wants to Run now.
                        bool isAllowedToRun = true;                         
                        // Request connection to our list of connection objects.
                        foreach (ForeignConnection conn in m_ForeignConnections)
                            if (conn.Server == null)                        // These are new connections
                                ProcessRequestServerConnect(m_Requests.Get(RequestCode.ForeignServiceConnect, conn));
                        if (isAllowedToRun)
                        {   // We have passed all startup requirements.  Move to running state.
                            newState = ServiceStates.Running;
                        }
                    }
                    break;
                case ServiceStates.Running:
                    // Running state is where all normal operations occur. 
                    // From here, the user can ask to stop.
                    if (requestedState == ServiceStates.Stopped || requestedState == ServiceStates.Stopping)
                    {
                        newState = ServiceStates.Stopping;
                        HubEventEnqueue(m_Requests.Get(RequestCode.ServiceStateChange, ServiceStates.Stopped));
                    }
                    break;
                case ServiceStates.Stopping:
                    // We stay in this state until all subsystems are carefully shutdown.
                    // Then we move to the final Stopped state, when our thread will be released.
                    bool isAllowedToStop = true;
                    // Shutdown socket
                    if (m_Socket != null)
                    {
                        m_Socket.StopConversation();
                        m_Socket.StopServer();
                        m_Socket.Connected -= new EventHandler(HubEventEnqueue);
                        m_Socket.Disconnected -= new EventHandler(HubEventEnqueue);
                        m_Socket.InternalMessage -= new EventHandler(HubEventEnqueue);
                        m_Socket.MessageReceived -= new EventHandler(HubEventEnqueue);
                        m_Socket = null;
                    }
                    // Disconnect events
                    m_AppServices.ServiceAdded -= new EventHandler(HubEventEnqueue);
                    m_AppServices.ServiceStopped -= new EventHandler(HubEventEnqueue);

                    if (isAllowedToStop)
                    {
                        newState = ServiceStates.Stopped;
                        HubEventEnqueue(m_Requests.Get(RequestCode.ServiceStateChange, ServiceStates.Stopped));
                    }
                    break;
                case ServiceStates.Stopped:
                    Log.NewEntry(LogLevel.Major,"ProcessServiceState: Stopped successfully.");
                    base.Stop();
                    break;
            }//switch(currentState)

            // Exit
            m_Requests.Recycle( requestEventArg );            
            if (newState != ServiceStates.None)                     // Fire state change event.
            {
                ServiceStates prevState = m_ServiceState;
                m_ServiceState = newState;
                OnServiceStateChanged(m_ServiceState, prevState);   // inform subscribers of our state change.
            }
            
        }//ProcessRequestServiceState()
        //
        //
        // *****************************************************
        // ****             UpdatePeriodic()                ****
        // *****************************************************
        protected override void UpdatePeriodic()
        {
            if (this.m_ServiceState == ServiceStates.Stopping)      // if we are trying to stop, pulse stop request again.
                HubEventEnqueue(m_Requests.Get(RequestCode.ServiceStateChange, ServiceStates.Stopped));


        }//UpdatePeriodic()
        //
        //
        #endregion// Hub Event Processing


        #region Foreign Servers Connection & Disconnection
        // *********************************************************
        // ****         Process Request Server Connect          ****
        // *********************************************************
        /// <summary>
        /// This processes the local request to connect to a foreign server via a socket.
        /// Requirements:
        ///     Request.Data = {ForeignConnection,
        /// </summary>
        /// <param name="requestEventArg"></param>
        private void ProcessRequestServerConnect(RequestEventArg<RequestCode> requestEventArg)
        {
            // Validate request
            if (requestEventArg.Data == null || requestEventArg.Data.Count < 1)
            {
                Log.NewEntry(LogLevel.Warning, "ProcessServerConnect: Missing data.");
                return;
            }
            if ((requestEventArg.Data[0] is ForeignConnection) == false)
            {
                Log.NewEntry(LogLevel.Warning, "ProcessServerConnect: Incorrect data.");
                return;
            }
            // Extract the ip address & create a new foreign server object for this connection.
            ForeignConnection conn = (ForeignConnection)requestEventArg.Data[0];
            System.Net.IPAddress ipAddress;
            if (string.IsNullOrEmpty(conn.IpAddress) || IPAddress.TryParse(conn.IpAddress, out ipAddress) == false)
                ipAddress = System.Net.IPAddress.Loopback;              // Connect locally if no address provided
            int portid;
            if (!int.TryParse(conn.Port, out portid))
            {
                Log.NewEntry(LogLevel.Warning, "ProcessServerConnect: Failed to extract port for connection. Failed to connection {0}.", conn);
                return;
            }
            // On first connection attempt, we make a foreign server.
            if (conn.Server == null)
            {
                conn.Server = new ForeignServer(this);
                string baseTag = string.Empty;
                if (m_AppServices.User != null)
                    baseTag = m_AppServices.User.ToString();
                conn.Server.UniqueTag = ForeignServer.CreateUniqueTag(baseTag, DateTime.Now);   // Upon first connection, we must create a unique tag.
            }
            // Connect foreign server
            int conversationID;
            if (m_Socket.TryConnect(ipAddress, portid, out conversationID))
            {
                ForeignServerConnect(conn.Server, conversationID);
                // Send credentials.
                SendMessage(conn.Server.ConversationId, GetMessage(MessageType.Credentials, MessageState.Request, conn.Server));
                // Optionally load & send config files to foreign server.
                if (string.IsNullOrEmpty(conn.m_ConfigFilename) == false)
                {
                    string fn = string.Format("{0}{1}", m_AppServices.Info.UserConfigPath, conn.m_ConfigFilename);
                    string stringifiedObjects = null;
                    if (File.Exists(fn))
                    {
                        try
                        {
                            using (StreamReader sr = new StreamReader(fn))
                            {
                                 stringifiedObjects = sr.ReadToEnd();
                            }
                        }
                        catch (Exception e)
                        {
                            Log.NewEntry(LogLevel.Warning, "Config file {0} failed to load. Exception {1}",fn,e.Message);
                        }
                        List<IStringifiable> nodes = null;
                        if (Stringifiable.TryCreate(stringifiedObjects, out nodes, true))
                            SendMessage(conn.Server.ConversationId, GetMessage(MessageType.CreateServices,MessageState.Request,nodes));                            
                    }
                    else
                        Log.NewEntry(LogLevel.Warning, "Config file cannot be found: {0}", fn);
                }

            }
            else
            {   // Failed to connect.
                Log.NewEntry(LogLevel.Warning, "ProcessForeignServiceConnect: Failed to connect to {0}", conn.Server);
                if (!m_ServersDisconnected.Contains(conn.Server))
                    m_ServersDisconnected.Add(conn.Server);
            }
            // Exit
            m_Requests.Recycle(requestEventArg); ;
        }//ProcessForeignServiceConnect()
        //
        // *************************************************
        // ****         ForeignServerConnect()          ****
        // *************************************************
        /// <summary>
        /// Called when a ForeignServer is connected (or re-connected) to its remote
        /// via a new Conversation.
        /// </summary>
        /// <param name="fs"></param>
        /// <param name="newConversationId"></param>
        protected void ForeignServerConnect(ForeignServer fs, int newConversationId = -1)
        {
            if (newConversationId >= 0)
                fs.ConversationId = newConversationId;              // otherwise assume caller already set id.
            if (m_ServersDisconnected.Contains(fs))                 // remove from disconnected list.
                m_ServersDisconnected.Remove(fs);
            if (m_ServersConnected.ContainsKey(fs.ConversationId))  // add to connected list
            {
                if (m_ServersConnected[fs.ConversationId] != fs)
                {
                    Log.NewEntry(LogLevel.Error, "ConnectForeignServer: Error!  Collision between servers {1} {0}", fs, m_ServersConnected[fs.ConversationId]);
                    Log.NewEntry(LogLevel.Error, "ConnectForeignServer: Overwriting with new server.");
                    m_ServersConnected[fs.ConversationId] = fs;
                }
            }
            else
                m_ServersConnected.Add(fs.ConversationId, fs);       // this is the normal situation
            fs.m_Manager = this;                                    // make sure it knows its manager.
            Log.NewEntry(LogLevel.Major, "ConnectForeignServer: Connected {0}", fs);
        }//ForeignServerConnect()
        //
        //
        // ****************************************************
        // ****         ForeignServerDisconnect()          ****
        // ****************************************************
        /// <summary>
        /// When a conversation ends, the associated ForeignServer is moved
        /// to the disconnected list.  Its conversation ID is set to -1.
        /// </summary>
        /// <param name="fs"></param>
        protected void ForeignServerDisconnect(ForeignServer fs)
        {
            int id = fs.ConversationId;
            if (m_ServersConnected.ContainsKey(id))
                m_ServersConnected.Remove(id);
            fs.ConversationId = -1;
            if (!m_ServersDisconnected.Contains(fs))
                m_ServersDisconnected.Add(fs);
            Log.NewEntry(LogLevel.Major, "DisconnectForeignServer: Disconnected {0}", fs);
        }//ForeignServerDisconnect()
        //
        //
        #endregion // Foreign Servers Connection & Disconnection


        #region Process Incoming Socket Events
        // *********************************************************
        // ****             Process Socket Event()              ****
        // *********************************************************
        /// <summary>
        /// First stop in processing incoming socket messages.  
        /// Here, we split them by "event type" and those of type "MessageReceived" 
        /// are decoded, processed by ProcessRequestMessage() or ProcessConfirmMessage().
        /// </summary>
        /// <param name="eventArg"></param>
        private void ProcessSocketEvent(SocketEventArgs eventArg)
        {
            //Log.NewEntry(LogLevel.Minor, "ProcessSocketEvent: {0}", eventArg);
            int id;
            ForeignServer fs;
            switch (eventArg.EventType)
            {
                //
                // Message Recieved
                //
                case SocketEventType.MessageReceived:
                    List<IStringifiable> iStringList = null;                        // We expect msgs to be a list of Message objects.
                    if ( Stringifiable.TryCreate(eventArg.Message,out iStringList) )
                    {   
                        foreach (IStringifiable istr in iStringList)
                        {
                            if (istr is Message)
                            {
                                Message msg = (Message)istr;
                                if (msg.State == MessageState.Request)
                                    ProcessMessageRequests(eventArg, msg);          // requests from other servers
                                else
                                    ProcessMessageResponses(eventArg, msg);         // responses to our requests
                            }
                            else
                                Log.NewEntry(LogLevel.Warning, "ProcessSocketEvent: Unknown data {0}.", istr);
                        }                       
                    }
                    else
                        Log.NewEntry(LogLevel.Warning, "ProcessSocketEvent: Failed to extract Message from #{0}: {1} ", eventArg.ConversationId, eventArg.Message);
                    break;
                case SocketEventType.Connected:
                    id = eventArg.ConversationId;
                    fs = null;
                    if (m_ServersConnected.TryGetValue(id, out fs))
                    {   // We initiated this connection since we've already marked server as connected.
                        Log.NewEntry(LogLevel.Major, "ProcessSocketEvent: Server {1} connected to #{0}", eventArg.ConversationId,fs);
                    }
                    else 
                    {   // Initiated by foreigner, we must wait for a Credentials request to respond.
                        Log.NewEntry(LogLevel.Warning, "ProcessSocketEvent: Foreign server connecting #{0}. Waiting for his credentials.", id);
                    }                                        
                    break;
                case SocketEventType.Disconnected:
                    id = eventArg.ConversationId;
                    fs = null;
                    if (m_ServersConnected.TryGetValue(id, out fs))
                    {
                        Log.NewEntry(LogLevel.Warning, "ProcessSocketEvent: Foreign server {0} disconnecting from #{1}.",fs,id);
                        ForeignServerDisconnect(fs);
                    }
                    else
                        Log.NewEntry(LogLevel.Warning, "ProcessSocketEvent: Unknown foreign server disconnecting from #{0}.", id);
                    break;
                case SocketEventType.InternalMessage:
                    id = eventArg.ConversationId;
                    Log.NewEntry(LogLevel.Warning, "ProcessSocketEvent: Internal conversation #{0} msg: {1}.",id,eventArg.Message);
                    break;
                default:
                    Log.NewEntry(LogLevel.Major, "ProcessSocketEvent: Unknown event type {0}. Ignoring.", eventArg.EventType);
                    break;
            }
        }//ProcessSocketEvent
        //
        //
        // *************************************************************
        // ****             ProcessRequestMessage()                 ****
        // *************************************************************
        /// <summary>
        /// These are Messages that originated from foreign servers.  
        /// </summary>
        /// <param name="eventArg"></param>
        /// <param name="msg"></param>
        private void ProcessMessageRequests(SocketEventArgs eventArg, Message msg)
        {
            switch(msg.MessageType)
            {
                case MessageType.Credentials:
                    // A foreign server that just connected is required to send 
                    // credentials before we tell him about our services.
                    bool isCredentialed = true;
                    // TODO: Test credentials 
                    if (isCredentialed)
                    {   // Connection credentials succeeded.  Accept foreign connection.
                        ForeignServer server = (ForeignServer)msg.Data[0];
                        ForeignServerConnect(server, eventArg.ConversationId);
                        // Send him my services.
                        List<IService> iServiceList = m_AppServices.GetServices();
                        SendServices(server.ConversationId,MessageType.ServiceAdded, iServiceList); // send server our services
                    }
                    else
                        SendMessage(eventArg.ConversationId, GetMessage(MessageType.Credentials,MessageState.Failed));
                    break;
                case MessageType.CreateServices:

                    break;
                case MessageType.ServiceAdded:

                    break;
                case MessageType.ServiceRemoved:
                    foreach (IStringifiable istring in msg.Data)
                    {

                    }
                    break;
                case MessageType.EngineEvent:

                    break;

            }//switch

        }// ProcessRequestMessage()
        //
        //
        //
        //
        // *************************************************************
        // ****             ProcessConfirmMessage()                 ****
        // *************************************************************
        private void ProcessMessageResponses(SocketEventArgs eventArg, Message msg)
        {
            // Get server responding
            ForeignServer foreignServer = null;
            if (! m_ServersConnected.TryGetValue(eventArg.ConversationId,out foreignServer) )
            {
                Log.NewEntry(LogLevel.Warning,"ProcessConfirmMessage: Unknown server {0} with message: {1}",eventArg,msg);
                return;
            }

            switch (msg.MessageType)
            {
                case MessageType.Credentials:

                    if (msg.State == MessageState.Confirmed)
                    {   // Our credentials were accepted.
                        Log.NewEntry(LogLevel.Minor, "ProcessConfirmMessage: Credentials accepted by {0}", foreignServer);

                    }
                    else
                    {
                        Log.NewEntry(LogLevel.Minor, "ProcessConfirmMessage: Credentials rejected by {0}. Disconnecting.", foreignServer);
                        m_Socket.StopConversation(foreignServer.ConversationId);
                        ForeignServerDisconnect(foreignServer);
                    }                    
                    break;
                case MessageType.CreateServices:

                    break;
                case MessageType.ServiceAdded:
                    // ForeignServer has added a new service.  
                    // Here, I want to make a ghost-service that represents each service provided by the foreign server.
                    Log.BeginEntry(LogLevel.Minor,"ProcessConfirmMessage: Services discovered on {0}:",foreignServer);
                    foreach (IStringifiable iStr in msg.Data)
                        if (iStr is ForeignService)
                        {
                            ForeignService foreignService = (ForeignService)iStr;
                            if (foreignServer.TryAddService(foreignService))
                                Log.AppendEntry(" {0}", foreignService);                                
                            else
                                Log.AppendEntry(" {0}[Failed to add]", foreignService);
                        }
                    Log.EndEntry();


                    break;
                case MessageType.ServiceRemoved:
                    foreach (IStringifiable istring in msg.Data)
                    {

                    }
                    break;
                case MessageType.EngineEvent:
                    // Engine events from foreign IEngineHubs.
                    foreach (IStringifiable iString in msg.Data)
                    {
                        if (iString is EngineEventArgs)
                        {
                            EngineEventArgs e = (EngineEventArgs)iString;
                            if (e.Status == EngineEventArgs.EventStatus.Request)
                            {   // Send this to a single hub
                                IService iService = null;
                                if (m_AppServices.TryGetService(e.EngineHubName, out iService) && iService is IEngineHub)
                                    ((IEngineHub)iService).HubEventEnqueue(e);
                            }
                            else
                            {   // Broadcast to all subscribed to this hub.
                                ForeignService fs;
                                if (foreignServer.TryGetService(e.EngineHubName,out fs) && (fs is IEngineHub))
                                {
                                    e.EngineHubName = fs.ServiceName;
                                    ((IEngineHub)fs).OnEngineChanged(e);
                                }
                            }
                        }
                    }
                    break;

            }//switch

        }//ProcessConfirmMessage()
        //
        //
        #endregion// Process Socket Events



        #region Process Incoming Local Events 
        // *****************************************************************
        // ****                 ProcessEngineEvent                      ****
        // *****************************************************************
        /// <summary>
        /// These come from engine hub confirmations.
        /// </summary>
        /// <param name="eventArg"></param>
        private void ProcessEngineEvent(EngineEventArgs eventArg)
        {
            EngineEventArgs.EventStatus eventStatus = eventArg.Status;
            EngineEventArgs.EventType eventType = eventArg.MsgType;
            string hubName = eventArg.EngineHubName; 

            // Process requests
            if (eventStatus == EngineEventArgs.EventStatus.Request)
            {   // Requests are targeting a specific engine hub.
                foreach (KeyValuePair<int, ForeignServer> server in m_ServersConnected)
                {
                    if (server.Value.Services.ContainsKey(hubName))
                    {
                        Message msg = GetMessage(MessageType.EngineEvent, MessageState.Confirmed, eventArg);
                        SendMessage(server.Key, msg);
                    }
                }
            }
            else
            {   // These are broadcasted
                foreach (KeyValuePair<int, ForeignServer> server in m_ServersConnected)
                {
                    // if the hubName is in hubs that this server is subscribed to, send along message.
                    Message msg = GetMessage(MessageType.EngineEvent, MessageState.Confirmed, eventArg);
                    SendMessage(server.Key, msg);                    
                }
            }

        }//ProcessEngineEvent()
        //
        //
        // *****************************************************************
        // ****             ProcessAppServiceEvent                      ****
        // *****************************************************************
        private void ProcessAppServiceEvent(AppServiceEventArg eventArg)
        {
            IEngineHub hub = null;
            switch(eventArg.EventType)
            { 
                case AppServiceEventType.ServiceAdded:                    
                    if (m_EngineHubSubcriptions.TryGetValue(eventArg.ServiceName, out hub) == false)
                    {                        
                        IService iService;
                        if (m_AppServices.TryGetService(eventArg.ServiceName, out iService) && (iService is ForeignService)==false)
                        {   // This is a new service that is NOT a foreign reflection.
                            if (iService is IEngineHub)
                            {
                                hub.EngineChanged += new EventHandler(this.HubEventEnqueue);
                                m_EngineHubSubcriptions.Add(eventArg.ServiceName, hub);
                                Log.NewEntry(LogLevel.Warning, "ProcessAppServiceEvent: Subscribing to engine hub {0}.", eventArg.ServiceName);
                                // Broadcast our service changes
                                List<IService> services = new List<IService>();
                                services.Add((IService)hub);
                                SendServices(-1, MessageType.ServiceAdded, services);
                            }
                        }
                    }
                    break;
                case AppServiceEventType.ServiceRemoved:
                    if (m_EngineHubSubcriptions.TryGetValue(eventArg.ServiceName, out hub) == true)
                    {   // Found subscriptio entry, remove it.
                        hub.EngineChanged -= new EventHandler(this.HubEventEnqueue);
                        m_EngineHubSubcriptions.Remove(eventArg.ServiceName);
                        Log.NewEntry(LogLevel.Warning, "ProcessAppServiceEvent: Removing engine hub {0}.", eventArg.ServiceName);
                        // Broadcast our service changes
                        List<IService> services = new List<IService>();
                        services.Add( (IService) hub );
                        SendServices(-1, MessageType.ServiceRemoved, services);

                    }
                    break;
                default:
                    Log.NewEntry(LogLevel.Warning, "ProcessAppServiceEvent: Unhandled event {0}. Ignoring.", eventArg);
                    break;
            }
        }
        //
        //
        //
        #endregion//Private Methods


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        //
        //
        // ****             GetMessage()            ****
        //
        protected Message GetMessage(MessageType type, MessageState state, params object[] args)
        {
            Message response = m_Messages.Get();
            response.MessageType = type;
            response.State = state;
            if (args != null)
            {
                foreach (object o in args)
                    if (o is IStringifiable)
                        response.Data.Add((IStringifiable)o);
                    else if (o is List<IStringifiable>)
                        response.Data.AddRange((List<IStringifiable>)o);
            }
            return response;
        }//GetMessage()
        //
        #endregion// Private Methods




        #region Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        //
        //
        //
        #endregion//Event Handlers


        #region IStringifiable 
        //
        //
        string IStringifiable.GetAttributes()
        {
            StringBuilder s = new StringBuilder();

            return s.ToString();
        }
        List<IStringifiable> IStringifiable.GetElements()
        {
            return null;
        }
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {            
            bool b;
            foreach (KeyValuePair<string,string> a in attributes)
            {
                if (a.Key.Equals("Port", StringComparison.CurrentCultureIgnoreCase))
                    m_ListenToPort = a.Value;
                else if (a.Key.Equals("IpAddress",StringComparison.CurrentCultureIgnoreCase))
                    m_ListenToIp = a.Value;
                else if (a.Key.Equals("ShowLog", StringComparison.CurrentCultureIgnoreCase) && bool.TryParse(a.Value, out b))
                    Log.IsViewActive = b;
            }
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {
            if (subElement is ForeignConnection)
                m_ForeignConnections.Add((ForeignConnection)subElement);
        }
        #endregion // IStringifiable


        #region IService 
        public string ServiceName
        {
            get { return m_HubName; }
        }
        public override void Start()
        {
            this.HubEventEnqueue(m_Requests.Get(RequestCode.ServiceStateChange, ServiceStates.Started));
            if (base.ListenState == WaitListenState.ReadyToStart)
                base.Start();               // actually start the thread.
        }
        public void Connect()
        {
            this.HubEventEnqueue(m_Requests.Get(RequestCode.ServiceStateChange, ServiceStates.Running));
        }
        public override void RequestStop()
        {
            this.HubEventEnqueue(m_Requests.Get(RequestCode.ServiceStateChange, ServiceStates.Stopped));
        }
        //
        //
        // *************************************************
        // ****         Service State Changed           ****
        // *************************************************
        //
        public event EventHandler ServiceStateChanged;
        //
        private void OnServiceStateChanged(ServiceStates currentState, ServiceStates prevState)
        {
            if (ServiceStateChanged != null)
            {
                ServiceStateEventArgs eventArg = new ServiceStateEventArgs(this,currentState,prevState);
                ServiceStateChanged(this, eventArg);
            }
        }//OnServiceStateChanged()
        //
        //
        //
        #endregion//IService



    }//end class
}
