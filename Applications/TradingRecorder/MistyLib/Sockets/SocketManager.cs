using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

namespace Misty.Lib.Sockets
{
    /// <summary>
    /// 
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
		//private IPEndPoint m_ServerIP = null;
        //private IPAddress m_ServerIPAddress = null;
        //private int m_ServerPort = 0;

		// Client:
        private TcpClient m_Client = null;              // Client        
		private int m_ConnectTimeout = 5;				// timeout in seconds.
		private Conversation m_Conversation = null;     // each client after connecting, will be associated with one of these.
        private bool m_IsClosing = false;

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


        #region Public & Private Server Methods
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
            lock (m_ServerLock)
            {
                if (m_IsServing)
                {   // we are currently running a listener. Tell it to stop.
                    m_IsServing = false;
                    m_Listener.Stop();                    
                }                
            }
			// Now close all active conversations.
			if (m_Conversation != null && !m_Conversation.IsStopping)
			{ 
				m_Conversation.RequestClose();
				m_Conversation = null;
			}
        }// Stop()
        //
        //
		//
		//
		// ****             Begin Start Server()              ****
		//
		/// <summary>
		/// This is an asynchronous call, that is handed to a unique thread, 
		/// and then thread.Start(), which enters a listener/server loop 
		/// as long as m_IsServing = true.
		/// </summary>
		private void BeginStartServer()
		{
			try
			{
				while (m_IsServing)
				{
                    OnInternalMessage(Msg_ServerWaiting);
                    if (!m_Listener.Pending())
                    {
                        Thread.Sleep(500);
                        continue;                               // no connection pending, then wait, check again.
                    }                    
					m_Client = m_Listener.AcceptTcpClient();   // blocking - wait for clients to connect.
					OnInternalMessage(string.Format("{3} h={0} local ip={1} remote ip={2}."
						, m_Client.Client.Handle.ToString(), m_Client.Client.RemoteEndPoint.ToString()
						, m_Client.Client.LocalEndPoint.ToString(),Msg_ServerConnected));
					if (m_Conversation != null && (!m_Conversation.IsStopping)) { return; }	// break out of server loop.					
					m_Conversation = new Conversation(m_Client,this.BeginConversation); 
					m_Conversation.Start();
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
        // ****             ConnectToServer()           ****
        //
        /// <summary>
        /// Called by an external thread, attempts to connect to a TCP server, and
		/// if successful, spawns a new Conversation to handle incoming messages.
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="portID"></param>
        /// <returns></returns>
        public virtual bool ConnectToServer(IPAddress addr, int portID)
        {
            bool isSuccessful = true;
            IPEndPoint ep = new IPEndPoint(addr, portID);

            if (m_Conversation != null && m_Conversation.IsContinuing)
			{
				OnInternalMessage(string.Format("Failed to connect.  There is already active conversation with {0}:{1} ", ep.Address.ToString(), ep.Port.ToString()));
				return false;
			}

			OnInternalMessage( string.Format("Trying to connect to {0}:{1} ...  ",ep.Address.ToString(),ep.Port.ToString()) );

			TcpClient tcpClient;
			if (TryConnectToClient(ep, out tcpClient))
			{
				m_Client = tcpClient;
				OnInternalMessage(string.Format("Connected to {0} h={1}.", m_Client.Client.LocalEndPoint.ToString(), m_Client.Client.Handle.ToString()));
				m_Conversation = new Conversation(m_Client,this.BeginConversation); //new Conversation(m_Client, ep, this.BeginConversation);
				m_Conversation.Start();
				isSuccessful = true;
			}
			else
			{
				if (m_Client != null) m_Client.Close();
				OnInternalMessage(string.Format("Failed to connect to {0}:{1}. Timeout exceeded.", ep.Address.ToString(), ep.Port.ToString()));
				isSuccessful = false;
			}
            // Exit.
            return isSuccessful;
        }//ConnectToServer()
        //     
		//
		//
        //
        //
		// ****				Try Connect To Client()					****
		//
		private bool TryConnectToClient(IPEndPoint ip, out TcpClient client)
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
				OnInternalMessage(string.Format("TryConnectToClient exception: {0}.",ex.Message) );
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
        /// Called asynchronously by a thread created just for the purpose of running
        /// this method.   When the conversation is complete, the thread falls out the bottom 
        /// of this method and dies.
        /// </summary>
        /// <param name="aNewConversation">Converstation holds info about who we are talking with.</param>
        private void BeginConversation(object aNewConversation)
        {
            Conversation conversation = (Conversation)aNewConversation;            
            OnConnectedToClient();
			int problemCounter = 0;
            byte[] buffer = new byte[1024];
            List<string> completeMessages = new List<string>();
            string unfinishedMessage = string.Empty;    // place to store partial messages.
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
                            {
                                OnMessageReceived(aMessage);                             // Report actual messages
                                //OnInternalMessage(String.Format("Recv: {0}", aMessage)); // Report to log  
                            }
                            completeMessages.Clear();                                   // Clear message list for next time.
							//conversation.IsStopping = true;	// for DEBUGGING!!!!!
                        }
                        else
                        {   // Read zero byte message - client disconnected!
							problemCounter++;
							OnInternalMessage(string.Format("Received {0} empty messages.", problemCounter.ToString()));
							if (problemCounter > 2)
							{
								OnInternalMessage(Msg_ClientDisconnected);
								conversation.IsContinuing = false;
							}
                        }
                    }
                    catch (Exception ex)
                    {   
                        if (m_IsClosing)
                        {
                            OnInternalMessage("Socket stream read exception caught. Must be requested from user. Flag is set to close manager.");
                            conversation.IsContinuing = false;
                        }
                        else
                        {
                            problemCounter++;
                            Console.WriteLine("Socket stream read exception caught. Suggests socket client disconnected.");
                            OnInternalMessage(string.Format("Socket read exception #{0}: {1}", problemCounter.ToString(), ex.Message));
                            if (problemCounter > 2)
                            {
                                OnInternalMessage("Suggests socket client disconnected. Discontinuing conversation.");
                                conversation.IsContinuing = false;
                            }
                        }
                    }
                }
				while (conversation.IsContinuing && ! conversation.IsStopping);
                OnInternalMessage(string.Format("Conversation finished. Continuing={0}. IsStopping={1}.",conversation.IsContinuing.ToString(),conversation.IsStopping.ToString()));
            }//using

			OnDisconnectedFromClient();
            conversation.Close();
            this.m_Conversation = null;                     // Feb 2013:  this is used as flag that we are NOT currently conversing.
        }//end conversation
        //
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
		/// <returns></returns>
		public virtual bool Send(string message)
		{
			bool isSuccessful = true;
			//
			if (m_Conversation == null) return false;
			NetworkStream outStream = m_Conversation.GetOutStream();
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
        public void StopConversation()
        {
            if (m_Conversation != null && !m_Conversation.IsStopping)
                m_Conversation.RequestClose();          // this will cause a read exception, which allows us to exit.            
        }
        //
        public void Close()
        {
            m_IsClosing = true;                         // flag that we got a close request.
            if (m_Conversation != null && ! m_Conversation.IsStopping)
                m_Conversation.RequestClose();          // this will cause a read exception, which allows us to exit.
            if (m_IsServing)
                StopServer();
        } // CloseConversations()
        //
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
        protected virtual void OnConnectedToClient()
        {
            if (this.Connected != null) Connected(this, new SocketEventArgs(SocketEventType.Connected,"Connected."));
        }
        //
        //
        // ****         OnDisconnectedFromClient()      ****
        //
        //
        protected virtual void OnDisconnectedFromClient()
        {
            if (this.Disconnected != null) Disconnected(this, new SocketEventArgs(SocketEventType.Disconnected,"Disconnected."));
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
        /// <param name="s"></param>
        protected virtual void OnMessageReceived(string s)
        {
            if (this.MessageReceived != null) MessageReceived(this, new SocketEventArgs(SocketEventType.MessageReceived,s));
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
            if (this.InternalMessage != null) InternalMessage(this, new SocketEventArgs(SocketEventType.InternalMessage,s));
        }// OnInternalMessage()
        //
        //
        #endregion//Event Args and Event Triggers


    }//end class
}
