using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

namespace UV.Lib.Sockets
{
    /// <summary>
    /// Socket manager  
    /// </summary>
    public class SocketManager
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // Server:
        public string Name = "Socket";
        private TcpListener m_Listener = null;
		private bool m_IsServing = false;
		private object m_ServerLock = new object();     // lock server Starts and Stops. 
        private AddressFamily m_ServerPreferredAddressFamily = AddressFamily.InterNetwork;

		// Clients:
        private int m_ConnectTimeout = 5;				// timeout in seconds.        
		private Conversation m_Conversation = null;     // each client after connecting, will be associated with one of these.

        // Conversations:
        private ConcurrentDictionary<int, Conversation> m_Conversations = new ConcurrentDictionary<int, Conversation>();

        //
        // Message processing controls
        //
        public char[] m_MessageTerminators = new char[] { '\n', '\r', '\0' };
        public delegate void ProcessMessage(ref string newMessage, ref List<string> completeMessages, ref string unfinishedMessage);
        public ProcessMessage m_ProcessMessage;         // Msgs from buffer assembled into complete msgs by this function.

        // Events
        public event EventHandler MessageReceived;      // raw text of incoming message
        public event EventHandler InternalMessage;      // verbose internal state messages of socket mngr
        public event EventHandler Connected;            // triggered on connection to new client.
        public event EventHandler Disconnected;         // trigger at end of conversation with client.

        // Internal message event keys.
        public const string Msg_ServerWaiting = "Socket waiting...";
        public const string Msg_ServerConnected = "Socket client connected:";
        public const string Msg_ServerException = "Socket Exception:";
        public const string Msg_ClientDisconnected = "Client disconnected.";

        #endregion


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        /// <summary>
        /// Main constructor for SocketManager.  A ProcessMessage delagate must be 
        /// provided to decipher incoming byte-wise messages into complete messages.
        /// The function also must return any trailing fragments of an incomplete 
        /// message (that may be part of the next forth-coming message).
        /// </summary>
        /// <param name="msgProcessingFunction">Message processing delegate.</param>
        public SocketManager(ProcessMessage msgProcessingFunction )
        {
            m_ProcessMessage = msgProcessingFunction;       // store the user-defined processing function.
        }//
        //
        /// <summary>
        /// The basic constructor uses a default simple message processing delegate.
        /// The simple delegate splits messages into strings using the default MessageDelimiters.
        /// </summary>
        public SocketManager()
        {
            m_ProcessMessage = new ProcessMessage(SimpleMessageProcessor);
        }//
        //
        #endregion//Constructors


        #region Properties
        //
        //
        //
        //
        public bool IsServing
        {
            get { return m_IsServing; }
        }
        public bool IsConversing
        {
            get
            {
                bool isConversing = (m_Conversation != null) && (m_Conversation.IsContinuing);
                return isConversing;
            }
        }
        //
        //
        //
        #endregion // properties


        #region Server Methods
        // *****************************************************************
		// ****                     Server Methods                      ****
        // *****************************************************************
        //
        //
        //
        //
		// ****                 StartServer()                 ****
        //
        /// <summary>
        /// Start a new socket-listener SERVER in this object.
        /// Called by external thread which is immediately released, but only 
        /// after a new thread is spawned to listen to incoming connection requests.
        /// TODO: 
        ///     1) Need to test re-starting server.
        ///     2) What is the correct choice for host.address to use?
        /// </summary>
        public virtual void StartServer(int portID, string hostIPAddress="localhost")
        {
            IPHostEntry host = Dns.GetHostEntry(hostIPAddress); 
            IPAddress[] addrList = host.AddressList;
            IPAddress ipAddress = null;
            if (m_ServerPreferredAddressFamily != AddressFamily.Unknown && m_ServerPreferredAddressFamily != AddressFamily.Unspecified)
            {   // user has a preferred network type to connect to. 
                foreach (IPAddress addr in addrList)    // Connect to first that matches.
                {
                    if (addr.AddressFamily == m_ServerPreferredAddressFamily)
                    {
                        ipAddress = addr;
                        break;
                    }
                }
            }
            if (ipAddress==null)
                ipAddress = addrList[addrList.Length - 1];
            //ipAddress = addrList[0];
            // Create the TCP listener / server on its own thread.
			Thread aThread = null;
			lock (m_ServerLock)
			{
				if ( ! m_IsServing )
				{
					m_IsServing = true;
					IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, portID);
					m_Listener = new TcpListener(ipEndPoint);

					m_Listener.Start();
					aThread = new Thread(new ThreadStart(this.BeginStartServer));
					aThread.Name = string.Format("{0} TCPServer", this.Name);
				}
			}
            if ( aThread != null ) aThread.Start();		// this thread will forever wait for new clients.

        }// Start()
        //
        //
        //
		// ****                 StopServer()                      ****
        //
		/// <summary>
		/// Stops the server, if its currently running, and closes all current conversations.
		/// </summary>
        public virtual void StopServer()
        {
            OnInternalMessage("Server stop requested.");
            lock (m_ServerLock)
            {
                if (m_IsServing)
                {   // We are currently running a listener. Tell it to stop.
                    m_IsServing = false;
                    m_Listener.Stop();                    
                }                
            }
            // Close and remove conversations.
            foreach (KeyValuePair<int,Conversation> c in m_Conversations)
            {
                OnInternalMessage(string.Format("Server closing {0} conversations.",m_Conversations.Count));
                Conversation conversation = null;
                if (m_Conversations.TryRemove(c.Key, out conversation))
                    conversation.RequestClose();                    
            }
        }// StopServer()
        //
        //
		//
		//
		// ****             Begin Start Server()              ****
		//
		/// <summary>
		/// This is an asynchronous call, that is handed to the unique thread, 
		/// called the TcpListener thread.  The only task for this thread
        /// is to listen for incoming socket connections.
        /// 
        /// It will do wait as long as the TcpListener is open and m_IsServer is true.
        /// To shut down the server, call StopServer().
        /// 
        /// Each time we detect a new connection on the TcpListener, we collects the
        /// TcpClient and put it into a new Conversation object, and call Conversation.Start().
        /// This spawns a Conversation thread to talk to the client. The conversation 
        /// is processed within the method SocketManager.BeginConversation() (in this object)!
		/// </summary>
		private void BeginStartServer()
		{
			try
			{
				while (m_IsServing)
				{
                    OnInternalMessage(Msg_ServerWaiting);
                    //if (!m_Listener.Pending())
                    //    Thread.Sleep(500);
                    // Wait for connection.
                    TcpClient tcpClient = m_Listener.AcceptTcpClient();   // blocking - wait for clients to connect.
					OnInternalMessage(string.Format("{3} h={0} local ip={1} remote ip={2}."
                        , tcpClient.Client.Handle.ToString(), tcpClient.Client.RemoteEndPoint.ToString()
                        , tcpClient.Client.LocalEndPoint.ToString(), Msg_ServerConnected));
                    Conversation conversation = new Conversation(tcpClient, this.BeginConversation);
                    AddConversation(conversation);                      // store conversation in list
                    conversation.Start();                               // launch conversation
				}
			}
			catch (SocketException ex)
			{
				OnInternalMessage(string.Format("{0} {1}",Msg_ServerException,ex.Message));
			}
			finally
			{
				StopServer();
			}
		}//StartServer()
		//
		//
		//
		//
		//
        //
        //
        #endregion//Server Methods


		#region Client Methods
		// *****************************************************************
		// ****						Client Methods						****
		// *****************************************************************
		//
		//
		//
        // ****             TryConnectToServer()           ****
        //
        /// <summary>
        /// Called by an external thread, attempts to connect to a TCP server, and
		/// if successful, spawns a new Conversation to handle incoming messages.
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="portID"></param>
        /// <param name="conversationID"></param>
        /// <returns></returns>
        public virtual bool TryConnect(IPAddress addr, int portID, out int conversationID)
        {
            bool isSuccessful = false;
            conversationID = - 1;
            IPEndPoint ep = new IPEndPoint(addr, portID);
			OnInternalMessage( string.Format("Trying to connect to {0}:{1} ...  ",ep.Address.ToString(),ep.Port.ToString()) );
            
			TcpClient tcpClient;
            int attemptsRemaining = 5;
            while (attemptsRemaining > 0 && isSuccessful==false)
            {
                if (TryConnectClient(ep, out tcpClient))
                {
                    Conversation conversation = new Conversation(tcpClient, this.BeginConversation); //new Conversation(m_Client, ep, this.BeginConversation);
                    OnInternalMessage(string.Format("Connecting to server {0} h={1}.  Starting conversation #{2}.", tcpClient.Client.LocalEndPoint.ToString(), tcpClient.Client.Handle.ToString(), conversation.ID));
                    AddConversation(conversation);
                    conversationID = conversation.ID;
                    conversation.Start();
                    isSuccessful = true;
                    
                }
                else
                {
                    if (tcpClient != null)
                        tcpClient.Close();
                    OnInternalMessage(string.Format("Failed to connect to {0}:{1}. Timeout exceeded.", ep.Address.ToString(), ep.Port.ToString()));
                    conversationID = -1;
                    isSuccessful = false;
                }
                attemptsRemaining--;
            }
            // Exit.
            return isSuccessful;
        }//ConnectToServer()
        //     
		//
		//
        //
        //
        // ****				TryConnectClient()					****
		//
		private bool TryConnectClient(IPEndPoint ip, out TcpClient client)
		{
			client = null;
			bool isSuccess = false;
			TcpClient tcpClient = null;	
			System.Threading.WaitHandle waitHandle = null;
			try
			{
				tcpClient = new TcpClient();
				IAsyncResult async = tcpClient.BeginConnect(ip.Address, ip.Port, null, null);
				waitHandle = async.AsyncWaitHandle;			
				if (!async.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(m_ConnectTimeout), false))
				{
					tcpClient.Close();
					isSuccess = false;
				}
				else
				{
					tcpClient.EndConnect(async);
					client = tcpClient;
					isSuccess = true;
				}
			}
			catch (Exception ex)
			{
				if (tcpClient != null) tcpClient.Close();
                OnInternalMessage(string.Format("TryConnectClient exception: {0}.", ex.Message));
				isSuccess = false;
			}
			finally
			{
				if ( waitHandle!= null) waitHandle.Close();
			}
			// Exit.
			return isSuccess;
		}//TryConnectToClient()
		//
        //
        #endregion//Public Methods


        #region Conversation Methods
        // *****************************************************************
		// ****             Conversation Methods                        ****
        // *****************************************************************
		//
        //
        //
        //
        //
        // ****             BeginConversation()          ****
        //
        //
        /// <summary>
        /// This is called by the Conversation thread created when we become connected
        /// to a socket.   This triggers the Connected event.
        /// When the conversation is complete, the Disconnected event is triggered, and 
        /// the thread falls out the bottom of the method and dies.  
        /// Each time a complete message is read from the buffer, this thread call OnMessageReceived event.
        /// </summary>
        /// <param name="aNewConversation">Converstation holds info about who we are talking with.</param>
        private void BeginConversation(object aNewConversation)
        {
            Conversation conversation = (Conversation)aNewConversation;
            OnConnectedToClient(conversation.ID);                    // Trigger connection event
			int problemCounter = 0;
            byte[] buffer = new byte[1024];                         // message buffer
            List<string> completeMessages = new List<string>();
            string unfinishedMessage = string.Empty;                // place to store partial messages.
            using (NetworkStream stream = conversation.Client.GetStream())
            {
                int bytesRead = 0;                 
                do
                {
                    try
                    {                            
                        bytesRead = stream.Read(buffer, 0, buffer.Length);  // blocking - wait to read msg.
						
                        if (bytesRead > 0)
                        {   //
                            // Recast the incoming message into a string.
                            //
							problemCounter = 0;						// reset empty message counter.
                            string s = string.Empty;
                            if (String.IsNullOrEmpty(unfinishedMessage))
                            {   // there is NO previous unfinished message.
                                s = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                            }
                            else
                            {   // there is a previous unfinished message, add new msg to old one.
                                s = String.Format("{0}{1}", unfinishedMessage, Encoding.ASCII.GetString(buffer, 0, bytesRead));
                                unfinishedMessage = String.Empty;   // empty the buffer.
                            }
                            // ProcessMessage depends on the outside exchange message definition.
							// This is why its a delegate supplied by the main application.
                            m_ProcessMessage(ref s, ref completeMessages, ref unfinishedMessage);
                            foreach (string aMessage in completeMessages)
                                OnMessageReceived(conversation.ID, aMessage);               // Report actual messages
                            completeMessages.Clear();                                       // Clear message list for next time.
                        }
                        else
                        {   // Read zero byte message - client disconnected!
							problemCounter++;
							OnInternalMessage(string.Format("#{1} received {0} empty messages.", problemCounter.ToString(),conversation.ID));
							if (problemCounter > 2)
							{
								OnInternalMessage(Msg_ClientDisconnected);
								conversation.IsContinuing = false;
							}
                        }
                    }
                    catch (Exception ex)
                    {
                        OnInternalMessage(string.Format("Conversation #{2}: Socket read exception #{0}: {1}", problemCounter.ToString(), ex.Message,conversation.ID));
                        if ( conversation.IsContinuing == false)
                        {
                            OnInternalMessage( string.Format("Conversation #{0} already flagged to stop, suggests local disconnect request.",conversation.ID));
                            conversation.IsContinuing = false;
                        }
                        else
                        {
                            problemCounter++;
                            OnInternalMessage(string.Format("Conversation #{0} not flagged to stop, suggests remote disconnection.", conversation.ID));
                            
                            if (problemCounter > 2)
                            {
                                OnInternalMessage(string.Format("Discontinuing conversation #{0}.",conversation.ID));
                                conversation.IsContinuing = false;
                            }
                        }
                    }
                }
				while (conversation.IsContinuing && ! conversation.IsStopping);
                OnInternalMessage(string.Format("Conversation #{2} continuing={0}. IsStopping={1}.",conversation.IsContinuing.ToString(),conversation.IsStopping,conversation.ID));
            }//using
            // Exit.			
            //conversation.Close();
            RemoveConversation(conversation.ID);
            OnDisconnectedFromClient(conversation.ID);
        }//end conversation
        //
        //
        //
        // ****         RemoveConversation()           ****
        //
        /// <summary>
        /// Conversations attempt to remove themselves from the conversation
        /// lookup tables after they close.
        /// </summary>
        /// <param name="id"></param>
        private void RemoveConversation(int id)
        {
            Conversation conversation;
            if (m_Conversations.TryRemove(id, out conversation))
            {
                conversation.RequestClose();
                OnInternalMessage(string.Format("Removed conversation #{0}. Conversations active: {1}.", conversation.ID, m_Conversations.Count));
            }
            else
                OnInternalMessage(string.Format("Failed to remove conversation #{0}. Conversations active: {1}.", id, m_Conversations.Count));
        }
        //
        //
        // ****         Add Conversation()          ****
        // 
        private void AddConversation( Conversation conversation )
        {
            m_Conversations.TryAdd(conversation.ID, conversation);
            OnInternalMessage(string.Format("New conversation #{0}. Conversations active: {1}.", conversation.ID, m_Conversations.Count));
        }
        //
        //
        //
        // ****         Get ConversationIds         ****
        //
        public bool TryGetConversationIds(out List<int> conversationIdList)
        {
            conversationIdList = null;
            if (m_Conversations.Count == 0)
                return false;
            else
            {
                conversationIdList = new List<int>(m_Conversations.Keys);
                return true;
            }
        }
        //
		//
		//
		// ****             Send()                  ****
		//
		/// <summary>
		/// Send a string message to already connected client.
		/// This can be called by external threads.
		/// </summary>
		/// <param name="message"></param>
        /// <param name="conversationId"></param>
		/// <returns></returns>
		public virtual bool Send(string message, int conversationId=-1)
		{
			// Try to find desired conversation
            Conversation conversation = null;
            int id;
            if (conversationId < 0)
            {   // Default behavior is to broadcast to all connections.
                List<int> keys = new List<int>(m_Conversations.Keys);
                foreach (int i in keys)
                    Send(message, i);
                return true;
            }
            else
                id = conversationId;
            if (!m_Conversations.TryGetValue(id, out conversation))
                return false;       // failed to find desired conversation            

            // Send message
            bool isSuccessful = true;
            NetworkStream outStream = conversation.GetOutStream();
			if (outStream == null)
			{
				OnInternalMessage("Failed to obtained out stream. Send failed.");
				return false;
			}

			// Create buffer for outgoing message.  
			byte[] buffer;
			if (string.IsNullOrEmpty(message))
				buffer = new byte[0];
			else
				buffer = Encoding.ASCII.GetBytes(message);

			// Send the message
			//StringBuilder internalMessage = new StringBuilder(message);
			//lock (outStream)  // i am worried about multiple callers of this function producing mixed msgs.
			{
				try
				{
					outStream.Write(buffer, 0, buffer.Length);
					outStream.Flush();
					//internalMessage.Insert(0, "Sent: ");
				}
				catch (Exception)
				{
					//internalMessage.Insert(0, "Failed to send. ");
					OnInternalMessage(string.Format("Failed to send: {0}",message));
				}
			}
			// Report results to subscribers
			//OnInternalMessage(internalMessage.ToString());
			// Exit
			return isSuccessful;
		}//Send().
		//        
        //
        //
        public void StopConversation(int conversationId = -1)
        {
            Conversation conversation;
            if (conversationId < 0)
            {   // User wants to end all conversations.
                List<Conversation> list = new List<Conversation>(m_Conversations.Values);
                foreach (Conversation conver in list)
                    conver.RequestClose();
            } 
            else if ( m_Conversations.TryGetValue(conversationId, out conversation))
                conversation.RequestClose();
        }
        //
        //
        //
        // ****         Simple Message Processor    ****
        /// <summary>
        /// This is the default, simple message processor that assume all messages are terminated 
        /// by one of the special characters given in the array MessageTerminators.
        /// </summary>
        /// <param name="newMessage"></param>
        /// <param name="completeMessages"></param>
        /// <param name="unfinishedMessage"></param>
        public void SimpleMessageProcessor(ref string newMessage, ref List<string> completeMessages, ref string unfinishedMessage)
        {
            // Splitting messages:  If the msgs end with a proper terminator, then 
            // the final element of msgs[] will be empty; that is, never store the final 
            // piece of the message.  Below, if we find that this final part was not empty, then
            // the final part of message was NOT complete, and we store it in unfinishedMessage.
            string[] msgs = newMessage.Split(m_MessageTerminators);
            for (int i=0; i < msgs.Length - 1; ++i){ completeMessages.Add(msgs[i]); }  
            
            // Now, examine final message.  If string was properly terminated,
            // this final element should have zero length, else its a msg fragment.
            if (msgs[msgs.Length - 1].Length > 0)
            {   // final message was not yet terminated, return the unfinished part, which
                // informs the caller he needs to wait for more.
                unfinishedMessage = msgs[msgs.Length - 1];                
            }
        }// SimpleMessageProcessor
        //
        //
        //
        //
        //
        #endregion//Private Methods


        #region Event Triggers
        // *********************************************************************
        // ****                         Event Triggers                      ****
        // *********************************************************************
        //
        //
        //
        // ****         OnConnectedToClient()           ****
        //
        protected virtual void OnConnectedToClient(int id)
        {
            if (this.Connected != null) 
                Connected(this, new SocketEventArgs(id,SocketEventType.Connected,"Connected."));
        }
        //
        //
        // ****         OnDisconnectedFromClient()      ****
        //
        //
        protected virtual void OnDisconnectedFromClient(int id)
        {
            if (this.Disconnected != null) 
                Disconnected(this, new SocketEventArgs(id, SocketEventType.Disconnected,"Disconnected."));
        }
        //
        //
        //
        //
        // ****         OnMessageReceived()             ****
        //
        /// <summary>
        /// This method is called each time a new incoming message is received
        /// thru the socket.  This is called by an internal thread.
        /// It is virtual and so can be over-riden by an inheriting class in order to 
        /// implement processing without events.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="s"></param>
        protected virtual void OnMessageReceived(int id, string s)
        {
            if (this.MessageReceived != null) 
                MessageReceived(this, new SocketEventArgs(id, SocketEventType.MessageReceived,s));
        }// On MessageReceived()
        //
        //
        //
        // ****         OnInternalMessage()             ****
        //
        /// <summary>
        /// This method is called each time a new incoming message is received
        /// thru the socket.  This is called by an internal thread.
        /// It is virtual and so can be over-riden by an inheriting class in order to 
        /// implement processing without events.
        /// </summary>
        /// <param name="s"></param>
        protected virtual void OnInternalMessage(string s)
        {
            if (this.InternalMessage != null) 
                InternalMessage(this, new SocketEventArgs(-1, SocketEventType.InternalMessage,s));
        }// OnInternalMessage()
        //
        //
        #endregion//Event Args and Event Triggers


    }//end class
}
