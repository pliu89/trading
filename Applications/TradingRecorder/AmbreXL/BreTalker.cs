using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;

namespace Ambre.XL
{
    using System.Runtime.InteropServices;           // needed for com arguments.
    using Microsoft.Office.Interop.Excel;           // needed for RTD interface and objects.

    using Misty.Lib.Sockets;
    using Ambre.Lib.ExcelRTD;

    /// <summary>
    /// EXCEL add-in that provides communications between Excel RTD links and the outside world via a socket.
    /// There are two places from which messages originate:
    ///     1) IRtdServer.OnRtdConnectData(TopicBase) 
    ///         Called from the base class by the excel thread
    /// </summary>
    /// <remarks>
    /// Feb 13 2013.
    /// Must set build for x86, and use %systemroot%\Microsoft.NET\Framework\v4.0.30319\Regasm.exe "pathname.dll" /codebase 
    /// to register on target machine.
    /// Opened Excel.  Code works:  =RTD("bre.talker","","heyt")
    /// Debugging: 
    /// Configuration Properties -> Debugging -> Start Action, click Start External Program, enter the path to Microsoft Excel
    /// (for example, C:\Program Files\Microsoft Office\Office10\EXCEL.EXE).
    /// </remarks>
    [ComVisible(true),ProgId("BRE.Talker"),Guid("7db258f8-9278-3a59-8011-eebcfcda892f")]
    public class BreTalker : RTDServerBase
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // Services.
        private SocketManager m_Socket = null;
        private DateTime m_ConnectionTime = DateTime.MaxValue;                              // time client connected.
        private object m_ConnectionTimeLock = new object();

        // Topic lists 
        // The base.Topics dictionay is broken down into two lists in this class; UserTopics and ServerTopics.
        // ServerTopics control this object, and are not broadcast to the socket end-user.
        protected ConcurrentDictionary<int, TopicBase> m_UserTopics = new ConcurrentDictionary<int, TopicBase>();
        protected ConcurrentDictionary<string, ConcurrentDictionary<int,TopicBase>> m_ServerTopics = new ConcurrentDictionary<string, ConcurrentDictionary<int,TopicBase>>();   // key="topic key"


        // Constants
        public const string TopicKey_Base = "bret_";                                        // base keyname for server topics- use LOWER case only!
        public const string TopicKey_Status = TopicKey_Base + "status";                       
        public const string TopicKey_ConnectionTime = TopicKey_Base + "connectiontime";     // show connection time
        public const string TopicKey_RawMessage = TopicKey_Base + "rawmessage";             // display each raw message.
        public const string TopicKey_UpdateCounter = TopicKey_Base + "counter";             // Counter for updates.
        public const string TopicKey_LastWarning = TopicKey_Base + "lastwarning";           // display last warning
        public const string TopicKey_LastWarningTime = TopicKey_Base + "lastwarningtime";   // display last warning time


        public char[] DelimiterKeyValues = new char[] { '=' };                              // parameters to control this object delimited by this.
        
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public BreTalker() : base()
        {

        }
        //
        //       
        #endregion//Constructors



        #region Private Methods
        // *****************************************************************
        // ****                    Private Methods                      ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// This is called whenever a new topic arrives from the RtdServerBase that's 
        /// determined to be a "server topic".  Its then passed to this method by the excel thread.
        /// This is only called once (upon new topic creation), so is useful for initialization.
        /// Threading:  Called by excel thread.
        /// </summary>
        /// <returns>immediate string response</returns>
        private string ProcessNewServerTopic(string topicName, ref TopicBase newTopic)
        {
            string responseString = string.Empty;
            switch (topicName)
            {
                case TopicKey_Status:
                    // Parameters are passed in key/value pairs:  RTD("bre.talker","","bret_connect"
                    string ipAddress = string.Empty;
                    int portID = 6012;
                    int throttleTime = base.ThrottleTime;           // msec default throttle time.
                    for (int i = 1; i < newTopic.Arguments.Length; ++i)
                    {
                        string[] parts = newTopic.Arguments[i].Split(DelimiterKeyValues);   // keeping empty args here.
                        if (parts.Length == 2)
                        {
                            switch (parts[0].ToLower())
                            {
                                case "port":
                                    int.TryParse(parts[1], out portID);
                                    break;
                                case "throttle":
                                    int.TryParse(parts[1], out throttleTime);
                                    throttleTime = Math.Max(200, throttleTime);
                                    if (Convert.ToInt32(Math.Round(m_Timer.Interval)) != throttleTime)
                                    {
                                        base.ThrottleTime = throttleTime;
                                        m_Timer.Stop();
                                        m_Timer.Interval = base.ThrottleTime;
                                        m_Timer.Start();
                                    }
                                    break;
                                case "ipaddress":
                                    ipAddress = parts[1];
                                    break;
                                default:
                                    break;
                            }//switch
                        }
                    }
                    if (m_Socket.IsServing)
                    {                                               // After stopping, we can restart server to listen on another port, etc.
                        m_Socket.StopServer();                      // Disconnect any conversations.  Stop listener, and restart it on another port.
                    }
                    try
                    {
                        if (!string.IsNullOrEmpty(ipAddress))       // Start server
                            m_Socket.StartServer(portID, ipAddress);
                        else
                            m_Socket.StartServer(portID);
                        if (m_Socket.IsServing)
                            responseString = "Listening";           // report that server is successfully listening now...
                        else
                        {
                            responseString = "Not listening";       // report that server failed to start listener.
                            ReportWarning("ProcessNewServerTopic code=1: Failed to start listener.");
                        }
                    }
                    catch (Exception)
                    {
                        responseString = "Listen Exception";
                    }
                    
                    break;
                case TopicKey_ConnectionTime:
                    responseString = "0";
                    break;
                case TopicKey_UpdateCounter:
                    responseString = "0";
                    break;
                case TopicKey_LastWarning:
                    responseString = "None";
                    break;
                case TopicKey_LastWarningTime:
                    responseString = DateTime.Now.ToShortTimeString();
                    break;


                default:
                    break;
            }//switch
            return responseString;
        }// ProcessNewServerTopic()
        //
        //
        //
        //
        //
        //
        //
        //
        private void ProcessSocketMessage(string msg)
        {            
            MessageType type = MessageType.None;
            int topicID = -1;            
            TopicBase topic;
            string[] parts = msg.Split(new char[] { ',' }, StringSplitOptions.None);    // don't remove empties here!!
            if (parts.Length >= 2 && Enum.TryParse<MessageType>(parts[0], out type) && Int32.TryParse(parts[1], out topicID) &&
                m_UserTopics.TryGetValue(topicID, out topic))
            {
                switch (type)
                {
                    case MessageType.RequestCurrent:
                        m_Socket.Send(topic.SerializeCurrent(MessageType.Current));
                        break;
                    case MessageType.RequestChange:
                        if (parts.Length > 2)
                            topic.SetValue(parts[2]);
                        break;
                    case MessageType.RequestTopicArgs:
                        m_Socket.Send(topic.Serialize());
                        break;
                    default:
                        break;
                }
            }
            else
            {
                ReportWarning( string.Format("ProcessSocketMsg code=1: {0}.",msg));
            }
        }// ProcessSocketMessage().
        //
        //
        /// <summary>
        /// Any part of this object can generate warnings to show the user.
        /// </summary>
        /// <param name="warning"></param>
        private void ReportWarning(string warning)
        {
            // If there are subscriptions to warning, update them.
            ConcurrentDictionary<int, TopicBase> topicList;
            string time = DateTime.Now.ToShortTimeString();
            if (m_ServerTopics.TryGetValue(TopicKey_LastWarning, out topicList))
                foreach (TopicBase topic in topicList.Values)
                {
                    topic.SetValue(warning);
                }//next topic.
            if (m_ServerTopics.TryGetValue(TopicKey_LastWarningTime, out topicList))
                foreach (TopicBase topic in topicList.Values)
                {
                    topic.SetValue(time);
                }//next topic.
        }// ReportWarning()
        //
        //
        //
        #endregion//Private Methods


        #region RtdServerBase Overridden Methods
        // *****************************************************************
        // ****                  Overridden Methods                     ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// All "OnRtdServer..." methods are called by the Excel thread.
        /// </summary>
        protected override void OnRtdServerStart()
        {            
            if (m_Socket == null)                       // Initialize socket.
            {
                m_Socket = new SocketManager();
                m_Socket.Connected += new EventHandler(Socket_EventHandler);
                m_Socket.Disconnected += new EventHandler(Socket_EventHandler);
                m_Socket.InternalMessage += new EventHandler(Socket_EventHandler);
                m_Socket.MessageReceived += new EventHandler(Socket_EventHandler);                
            }
        }
        //
        protected override void OnRtdServerTerminate()
        {
            if (m_Socket != null )
            {
                m_Socket.StopServer();
                m_Socket.Connected -= new EventHandler(Socket_EventHandler);
                m_Socket.Disconnected -= new EventHandler(Socket_EventHandler);
                m_Socket.InternalMessage -= new EventHandler(Socket_EventHandler);
                m_Socket.MessageReceived -= new EventHandler(Socket_EventHandler);
                m_Socket = null;
            }
            
        } // OnRtdServerTerminate()
        //
        //
        /// <summary>
        /// When a new topic is created (in the base class), we separate these topics into
        /// "UserTopics" (intended for client on other end of the socket) and those we keep as private "ServerTopics."
        /// The private "ServerTopics" allow the Excel user to control the behavior of this object and its services.
        /// The base class created a new TopicBase object to hold info requested from excel.
        /// This new TopicBase is analyzed and stored here as a "UserTopic" or "ServerTopic".
        /// ServerTopics are forwarded to ProcessNewServerTopic(), UserTopics are serialized and sent to socket client.
        /// </summary>
        /// <returns>a value immediately displayed by excel.</returns>
        protected override string OnRtdConnectData(ref TopicBase newTopic)
        {
            string responseString = string.Empty;                           // default

            string topicName = newTopic.Arguments[0].Trim().ToLower();      // first element must always exist!  
            if (topicName.StartsWith(TopicKey_Base))
            {                                                               // this is a server topic.  Store it.
                if (!m_ServerTopics.ContainsKey(topicName))                 // if this topicName is the first, create place for them.
                    m_ServerTopics.TryAdd(topicName, new ConcurrentDictionary<int,TopicBase>());
                ConcurrentDictionary<int,TopicBase> topicList;
                if (m_ServerTopics.TryGetValue(topicName, out topicList))   // get all topics with this same name.
                    topicList.TryAdd(newTopic.TopicID,newTopic);
                responseString = ProcessNewServerTopic(topicName, ref newTopic);// initial processing of this server topic.
            }
            else
            {
                m_UserTopics.TryAdd(newTopic.TopicID, newTopic);            // This is a user topic.
                if ( m_Socket.IsConversing )
                    m_Socket.Send(newTopic.Serialize());                    // Send new topic to socket.
            }            
            // Exit
            return responseString;                                          // what appears in cell immediately.
        }// OnRtdConnectData()
        //
        protected override void OnRtdDisconnect(TopicBase topicToRemove)
        {
            // See if topic is a ServerTopic
            TopicBase topic = null;
            foreach (string topicName in m_ServerTopics.Keys)
            {
                ConcurrentDictionary<int,TopicBase> topicList;                
                if (m_ServerTopics.TryGetValue(topicName, out topicList) && topicList.ContainsKey(topicToRemove.TopicID))
                {
                    topicList.TryRemove(topicToRemove.TopicID, out topic);
                    break;
                }
            }
            if (topic == null)                      // if topic is not null, it was a server topic.
            {   // We did not find this topic in ServerTopics.  Check UserTopics.
                if (m_UserTopics.ContainsKey(topicToRemove.TopicID))
                {
                    m_UserTopics.TryRemove(topicToRemove.TopicID, out topic);
                    m_Socket.Send(topicToRemove.SerializeCurrent(MessageType.TopicRemoved));
                }
            }            
                    
        }
        //
        /// <summary>
        /// Threading: This is called by Timer threadpool.
        /// </summary>
        protected override void OnUpdateNotify()
        {
            ConcurrentDictionary<int,TopicBase> topicList;
            // ServerTopic: Connection Time
            if (m_ServerTopics.TryGetValue(TopicKey_ConnectionTime, out topicList))       
            {
                TimeSpan ts;
                lock (m_ConnectionTimeLock) // connection time updated by socket thread.
                {
                    ts = DateTime.Now.Subtract(m_ConnectionTime);         
                }
                foreach (TopicBase topic in topicList.Values)
                {
                    if (ts.TotalSeconds > 0)
                    {

                        if (ts.Days < 1)
                            topic.SetValue(string.Format(@"{0:hh\:mm\:ss}", ts));
                        else
                            topic.SetValue(string.Format(@"{0:dd} days, {0:hh\:mm\:ss}", ts));
                        //topic.SetValue( ts.TotalSeconds.ToString("0") );
                    }
                    else
                        topic.SetValue("00:00:00");
                }//next topic.
            }
            // ServerTopic: UpdateCounters.
            if (m_ServerTopics.TryGetValue(TopicKey_UpdateCounter, out topicList))  
                foreach (TopicBase topic in topicList.Values)
                {
                    int n;
                    if (Int32.TryParse(topic.PeekAtValue(), out n))
                        topic.SetValue( (n + 1).ToString() );
                    else
                        topic.SetValue( "0" );
                }//next topic.



        }// OnUpdateNotify()
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
        /// <summary>
        /// Threading: These are called by a socket conversation thread.
        /// </summary>
        private void Socket_EventHandler(object sender, EventArgs eventArgs)
        {
            SocketEventArgs e = (SocketEventArgs)eventArgs;
            ConcurrentDictionary<int,TopicBase> topicList;
            if (e.EventType == SocketEventType.MessageReceived)
            {   // Received message from socket client.                                
                // Write Raw message out.
                if ( m_ServerTopics.TryGetValue(TopicKey_RawMessage,out topicList) )
                    foreach (TopicBase t in topicList.Values)                    
                        t.SetValue( e.Message );
                else
                    ReportWarning(string.Format("Socket_Event code 1: {0}.",e));
                // Process Message.
                ProcessSocketMessage(e.Message);
            }
            else if (e.EventType == SocketEventType.Connected)
            {
                // Update Connection time ServerTopics.
                lock (m_ConnectionTimeLock)
                {
                    m_ConnectionTime = DateTime.Now;
                }
                if (m_ServerTopics.TryGetValue(TopicKey_Status, out topicList))
                    foreach (TopicBase t in topicList.Values)
                        t.SetValue("Connected");                                       // update "status" topics.

                // Send all topics to new connection.
                foreach (TopicBase topic in m_UserTopics.Values)
                    m_Socket.Send(topic.Serialize());
                    
            }
            else if (e.EventType == SocketEventType.Disconnected)
            {
                // Update Connection time ServerTopics.
                lock (m_ConnectionTimeLock)
                {
                    m_ConnectionTime = DateTime.MaxValue;                               // Reset items after client disconnects.
                }
                if (m_ServerTopics.TryGetValue(TopicKey_Status, out topicList))     // set status to disconnected.
                    foreach (TopicBase t in topicList.Values)
                        t.SetValue( "Disconnected" );
                if (m_ServerTopics.TryGetValue(TopicKey_ConnectionTime, out topicList)) // set connection time to zero.
                    foreach (TopicBase t in topicList.Values)
                        t.SetValue("0"); 
            }
            else if (e.EventType == SocketEventType.InternalMessage)
            {
                
            }


        }// Socket_EventHandler()
        //
        //
        #endregion//Event Handlers


    }
}
