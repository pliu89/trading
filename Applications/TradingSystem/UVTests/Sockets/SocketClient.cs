using System;
using System.Collections.Generic;
//using System.ComponentModel;
//using System.Data;
using System.Drawing;
//using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.Windows.Forms;

namespace UV.Tests.Sockets
{
    using UV.Lib.Hubs;
    using UV.Lib.Sockets;

    public partial class SocketClient : Form
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //    
        private LogHub m_Log = null;
        private SocketManager m_Socket = null;
        private object m_ConversationIdLock = new object();
        private List<int> m_ConversationId = new List<int>();

        private int m_IsAutoConnectingCount = -1;			// -1 means we are not trying to auto reconnect.
        private int m_SecondsToNextConnectAttempt = 0;		// auto reconnection if disconnected.



        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public SocketClient(string ipAddress, int port)
        {
            InitializeComponent();

            textBoxServerAddress.Text = ipAddress;
            textBoxServerPort.Text = port.ToString();

            string dirPath = System.IO.Directory.GetCurrentDirectory();
            m_Log = new LogHub("SocketClient", dirPath, true, LogLevel.ShowAllMessages);

            timer1.Interval = 1000;		// tick time in milliseconds.
            timer1.Tick += new EventHandler(Timer_Tick);
            timer1.Enabled = true;
        }
        //
        //
        //
        //       
        #endregion//Constructors


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        private void Socket_EventHandler(object sender, EventArgs e)
        {
            SocketEventArgs eventArgs = (SocketEventArgs)e;
            if (eventArgs.EventType == SocketEventType.MessageReceived)
            {   // Process message here if you like.

            }
            else if (eventArgs.EventType == SocketEventType.Disconnected)
            {
                lock(m_ConversationIdLock)
                {
                    if (m_ConversationId.Contains(eventArgs.ConversationId))
                        m_ConversationId.Remove(eventArgs.ConversationId);
                }
                if (checkBoxAutoReconnect.Checked)
                {
                    m_IsAutoConnectingCount = 0;
                }
            }
            m_Log.NewEntry(LogLevel.Major, "{0}", eventArgs);
        }//Socket_EventHandler()
        //
        //
        private void Initialize()
        {
            // Initialize socket.
            m_Socket = new SocketManager();
            m_Socket.Connected += new EventHandler(Socket_EventHandler);
            m_Socket.Disconnected += new EventHandler(Socket_EventHandler);
            m_Socket.InternalMessage += new EventHandler(Socket_EventHandler);
            m_Socket.MessageReceived += new EventHandler(Socket_EventHandler);
        }
        private void Shutdown()
        {
            // Shutdown all.
            lock (m_ConversationIdLock)
            {
                foreach (int id in m_ConversationId)
                    m_Socket.StopConversation(id);
            }
            //m_Socket.StopServer();
            m_Socket.Connected -= new EventHandler(Socket_EventHandler);
            m_Socket.Disconnected -= new EventHandler(Socket_EventHandler);
            m_Socket.InternalMessage -= new EventHandler(Socket_EventHandler);
            m_Socket.MessageReceived -= new EventHandler(Socket_EventHandler);
            m_Log.RequestStop();
        }
        //
        #endregion//Private Methods


        #region Form Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        //
        private void button_Click(object sender, EventArgs e)
        {
            if (!(sender is Control)) return;			// only accept controls
            string senderName = ((Control)sender).Name;

            switch (senderName)
            {
                case "buttonClientConnect":
                    System.Net.IPAddress ipAddress;
                    if (!System.Net.IPAddress.TryParse(textBoxServerAddress.Text, out ipAddress))
                        ipAddress = System.Net.IPAddress.Loopback;
                    m_Log.NewEntry(LogLevel.Major, "Button Click: {0}", senderName);
                    int conversationID;
                    if ( m_Socket.TryConnect(ipAddress, Convert.ToInt16(textBoxServerPort.Text),out conversationID))
                    {   // Successful connection.
                        lock (m_ConversationIdLock)
                        {
                            if (!m_ConversationId.Contains(conversationID))
                                m_ConversationId.Add(conversationID);//m_ConversationId = conversationID;
                        }
                    }

                    break;
                case "buttonClientDisconnect":
                    checkBoxAutoReconnect.Checked = false;
                    m_Log.NewEntry(LogLevel.Major, "Button Click: {0}", senderName);
                    lock (m_ConversationIdLock)
                    {
                        if (m_ConversationId.Count > 0)
                        {
                            int id = m_ConversationId[0];
                            m_Socket.StopConversation(id);
                        }
                    }

                    break;
                case "buttonSend":
                    m_Log.NewEntry(LogLevel.Major, "Button Click: {0} ", senderName);
                    lock (m_ConversationIdLock)
                    {
                        foreach (int ID in m_ConversationId)
                        {
                            if (m_Socket.Send(string.Format("{0}\n", textBoxSendText.Text), ID))
                                m_Log.NewEntry(LogLevel.Major, " #{1} -> {0}", textBoxSendText.Text, ID);
                            else
                                m_Log.NewEntry(LogLevel.Major, " #{1} -> {0}", "failed", ID);
                        }
                    }
                    break;

                default:
                    m_Log.NewEntry(LogLevel.Warning, "button_Click: Unknown button {0}", senderName);
                    break;
            }//switch.

        }
        private void Sockets_Load(object sender, EventArgs e)
        {
            Initialize();
        }
        private void Sockets_FormClosing(object sender, FormClosingEventArgs e)
        {
            Shutdown();
        }
        //
        //
        void Timer_Tick(object sender, EventArgs e)
        {
            //
            // Auto reconnect.
            //
            if (m_IsAutoConnectingCount >= 0 && checkBoxAutoReconnect.Checked)
            {
                if ((m_SecondsToNextConnectAttempt--) <= 0)
                {
                    // Try to connect.
                    System.Net.IPAddress ipAddress;
                    if (!System.Net.IPAddress.TryParse(textBoxServerAddress.Text, out ipAddress))
                        ipAddress = System.Net.IPAddress.Loopback;
                    int conversationId;
                    bool isSuccess = m_Socket.TryConnect(ipAddress, Convert.ToInt16(textBoxServerPort.Text),out conversationId);
                    if (isSuccess)
                    {
                        lock (m_ConversationIdLock)
                        {
                            m_IsAutoConnectingCount = -1;			// turn off autoconnect counter/switch
                            if (!m_ConversationId.Contains(conversationId))
                                m_ConversationId.Add(conversationId);
                        }
                    }
                    else
                    {
                        m_IsAutoConnectingCount++;                        
                        m_SecondsToNextConnectAttempt = (int)(Math.Floor(Math.Min(60, Math.Pow(2.0, m_IsAutoConnectingCount))));	// seconds to wait.
                        m_Log.NewEntry(LogLevel.Major, "Failed again.  Will attempt to connect again in {0} seconds.", m_SecondsToNextConnectAttempt.ToString());
                    }
                }
                else
                    m_Log.NewEntry(LogLevel.Major, "Will attempt to connect again in {0} seconds.", m_SecondsToNextConnectAttempt.ToString());
            }
        }
        //
        #endregion//Event Handlers

    }
}
