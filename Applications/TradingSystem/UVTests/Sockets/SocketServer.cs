using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace UV.Tests.Sockets
{
    using UV.Lib.Sockets;
    using UV.Lib.Hubs;

    public partial class SocketServer : Form
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //    
        private LogHub m_Log = null;
        private SocketManager m_Socket = null;
        private List<SocketClient> m_Clients = new List<SocketClient>();		// place to hold clients GUIs.

        #endregion// members


		#region Constructors
		// *****************************************************************
		// ****                     Constructors                        ****
		// *****************************************************************
        public SocketServer()
		{
			InitializeComponent();
			string dirPath = System.IO.Directory.GetCurrentDirectory();
			m_Log = new LogHub("SocketTest", dirPath, true, LogLevel.ShowAllMessages);
			
		}
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

            // Auto echo repsonse.
            if (eventArgs.EventType == SocketEventType.MessageReceived)
            {
                //m_Log.NewEntry(LogLevel.Major, " - ");
                //m_Log.NewEntry(LogLevel.Major, "[{0}] {1}", eventArgs.EventType.ToString(), eventArgs.Message);
                m_Log.NewEntry(LogLevel.Major, "{0}", eventArgs);
                m_Log.NewEntry(LogLevel.Major, "Server will echo to conversation #{0}.",eventArgs.ConversationId);
                m_Socket.Send(string.Format("Server received message: {0}\n", eventArgs.Message), eventArgs.ConversationId);
            }
            else
            {
                m_Log.NewEntry(LogLevel.Major, "{0}", eventArgs);
                //m_Log.NewEntry(LogLevel.Major, "[{0}] {1}", eventArgs.EventType.ToString(), eventArgs.Message);
            }


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
            // Shut down clients
            foreach (SocketClient client in m_Clients)
                client.Close();

            m_Log.RequestStop();
        }
        //
        #endregion//Private Methods

        #region Form Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        private void button_Click(object sender, EventArgs e)
        {
            if (!(sender is Control)) return;			// only accept controls
            string senderName = ((Control)sender).Name;
            m_Log.NewEntry(LogLevel.Major, "Button Click: Button = {0}", senderName);

            switch (senderName)
            {
                case "buttonStartListener":
                    Button b = (Button)sender;
                    if (b.Text.Equals("stopped", StringComparison.CurrentCultureIgnoreCase))
                    {
                        b.Text = "started";
                        b.BackColor = System.Drawing.Color.Green;
                        b.ForeColor = System.Drawing.Color.Yellow;
                        m_Socket.StartServer(Convert.ToInt16(textBoxListenerPort.Text));
                    }
                    else
                    {
                        b.Text = "stopped";
                        b.BackColor = System.Drawing.Color.Red;
                        b.ForeColor = System.Drawing.Color.Yellow;
                        m_Socket.StopServer();
                    }
                    break;
                case "buttonSpawnClient":
                    //string ip = "";
                    SocketClient newClient = new SocketClient(System.Net.IPAddress.Loopback.ToString(), Convert.ToInt16(textBoxListenerPort.Text));
                    newClient.Show();
                    m_Clients.Add(newClient);

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
        private void textBoxListenerPort_TextChanged(object sender, EventArgs e)
        {

        }



        //
        #endregion//Event Handlers

    }
}
