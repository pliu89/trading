using System;
using System.Collections.Generic;
//using System.ComponentModel;
//using System.Data;
using System.Drawing;
//using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.Windows.Forms;

namespace MistyTests.Sockets
{
    using Misty.Lib.Hubs;
    using Misty.Lib.Sockets;

    public partial class SocketClient : Form
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //    
        private LogHub m_Log = null;
        private SocketManager m_Socket = null;

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
            {
                //m_Log.NewEntry(LogLevel.Major, " - ");
                m_Log.NewEntry(LogLevel.Major, " -> {0}",eventArgs.Message);
            }
            else if (eventArgs.EventType == SocketEventType.Disconnected)
            {
                if (checkBoxAutoReconnect.Checked)
                {
                    m_IsAutoConnectingCount = 0;
                }

            }
            m_Log.NewEntry(LogLevel.Major, "[{0}] {1}", eventArgs.EventType.ToString(), eventArgs.Message);

        }
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
            m_Socket.StopServer();
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
            m_Log.NewEntry(LogLevel.Major, " - ");
            m_Log.NewEntry(LogLevel.Major, "Button Click: Button = {0}", senderName);

            switch (senderName)
            {
                case "buttonClientConnect":
                    System.Net.IPAddress ipAddress;
                    if (!System.Net.IPAddress.TryParse(textBoxServerAddress.Text, out ipAddress))
                        ipAddress = System.Net.IPAddress.Loopback;
                    m_Socket.ConnectToServer(ipAddress, Convert.ToInt16(textBoxServerPort.Text));

                    break;
                case "buttonClientDisconnect":
                    checkBoxAutoReconnect.Checked = false;
                    m_Socket.StopServer();
                    break;
                case "buttonSend":
                    m_Socket.Send(string.Format("{0}\n", textBoxSendText.Text));
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
                    bool isSuccess = m_Socket.ConnectToServer(ipAddress, Convert.ToInt16(textBoxServerPort.Text));
                    if (isSuccess)
                        m_IsAutoConnectingCount = -1;			// turn off autoconnect counter/switch
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
