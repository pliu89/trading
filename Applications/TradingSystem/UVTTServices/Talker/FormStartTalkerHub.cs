using System;
using System.Collections.Generic;
//using System.ComponentModel;
//using System.Data;
using System.Drawing;
//using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.Windows.Forms;

namespace UV.TTServices.Talker
{
    using UV.Lib.Application;
    using UV.Lib.Hubs;

    using UV.TTServices.Markets;
    using UV.TTServices.Fills;


    /// <summary>
    /// Stand-alone appl for the TalkerHub.   It instantiates a TalkerHub, and various Market/Order Hubs, allowing
    /// TalkerHub to run on a machine runing TradingTechnologies XTrader and answering querries from a client
    /// on the other side of a socket.
    /// </summary>
    public partial class FormStartTalkerHub : Form
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // Services
        private LogHub Log = null;
        private TTApiService m_TTServices;
        private MarketTTAPI m_MarketHub = null;
        private FillHub m_FillHub = null;
        private TalkerHub m_TalkerHub = null;
        private bool m_IsAppShuttingDown = false;

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public FormStartTalkerHub()
        {
            InitializeComponent();
            InitializeServices();
        }//constructor
        //
        //
        private void InitializeServices()
        {
            AppInfo info = AppInfo.GetInstance("Bret");

            bool isLogViewerVisible = false;
            #if (DEBUG)
                isLogViewerVisible = true;
            #endif

            Log = new LogHub("TalkerForm", info.LogPath, isLogViewerVisible, LogLevel.ShowAllMessages);

            // Instantiate TT API.
            m_TTServices = TTServices.TTApiService.GetInstance();
            m_TTServices.ServiceStateChanged += new EventHandler(TTServices_ServiceStateChanged);
            m_TTServices.Start(true);

            // Instantiate hubs
            m_MarketHub = new MarketTTAPI();
            m_MarketHub.Log.IsViewActive = isLogViewerVisible;
            //m_MarketHub.InstrumentChanged += new EventHandler(MarketHub_InstrumentChanged);
            m_MarketHub.Start();
            
            m_FillHub = new FillHub(string.Empty, isLogViewerVisible);
            m_FillHub.MarketHub = m_MarketHub;
            //m_FillHub.PositionBookCreated += new EventHandler(FillHub_PositionBookCreated);
            //m_FillHub.PositionBookChanged += new EventHandler(FillHub_PositionBookChanged);
            //m_FillHub.PositionBookPnLChanged += new EventHandler(FillHub_PositionBookPnLChanged);
            m_FillHub.Start();
            

            m_TalkerHub = new TalkerHub(isLogViewerVisible);
            m_TalkerHub.ServiceStateChanged += new EventHandler(TalkerHub_ServiceStateChanged);
            m_TalkerHub.RequestAddHub(m_FillHub);
            m_TalkerHub.Start();
            
            
           
        }
        #endregion//Constructors



        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        //
        //
        //
        // ****                   ShutDown()                       ****
        //
        /// <summary>
        /// Called when the form is about to close to release resources nicely.
        /// </summary>
        private void ShutDown()
        {
            if (!m_IsAppShuttingDown)
            {
                m_IsAppShuttingDown = true;

                if (Log != null)
                {
                    Log.RequestStop();
                    Log = null;
                }

                if (m_FillHub != null)
                {
                    //m_FillHub.PositionBookCreated -= new EventHandler(FillHub_PositionBookCreated);
                    //m_FillHub.PositionBookChanged -= new EventHandler(FillHub_PositionBookChanged);
                    //m_FillHub.PositionBookPnLChanged -= new EventHandler(FillHub_PositionBookPnLChanged);
                    m_FillHub.Request(new UV.Lib.OrderHubs.OrderHubRequest(UV.Lib.OrderHubs.OrderHubRequest.RequestType.RequestShutdown));
                    m_FillHub = null;
                }
                if (m_MarketHub != null)
                {
                    m_MarketHub.RequestStop();
                    m_MarketHub = null;
                }
                if (m_TTServices != null)
                {
                    m_TTServices.ServiceStateChanged -= new EventHandler(TTServices_ServiceStateChanged);
                    m_TTServices.Dispose();
                    m_TTServices = null;
                }
                if (m_TalkerHub != null)
                {
                    m_TalkerHub.ServiceStateChanged -= new EventHandler(TalkerHub_ServiceStateChanged);
                    m_TalkerHub.Request(TalkerHubRequest.StopService);
                    //m_TalkerHub = null;
                }
            }

        }//Shutdown().
        //
        //
        private void SetText(Control control, string text)
        {
            if (control.InvokeRequired)
                this.Invoke(new Action<Control>((c) => c.Text = text),control);   
            else
                control.Text = text;
        }//SetText()
        //        
        //
        #endregion//Private Methods


        #region External Service Event Handlers
        // *****************************************************************
        // ****                Form Event Handlers                     ****
        // *****************************************************************
        //
        //
        //
        // ****         TTServices_ServiceStateChanged()            ****
        //
        private void TTServices_ServiceStateChanged(object sender, EventArgs eventArgs)
        {
            if (m_IsAppShuttingDown) return;
            if (eventArgs is TTServices.TTApiService.ServiceStatusChangeEventArgs)
            {
                TTServices.TTApiService.ServiceStatusChangeEventArgs e = (TTServices.TTApiService.ServiceStatusChangeEventArgs)eventArgs;
                if (e.IsConnected)
                {
                    Log.NewEntry(UV.Lib.Hubs.LogLevel.Major, "TTServices connected.");
                    SetText(txtAPIConnected, "connected");       
                    if ( m_MarketHub!=null)
                        m_MarketHub.Connect();
                    if ( m_FillHub != null )
                        m_FillHub.Connect();
                }
                else
                {
                    Log.NewEntry(UV.Lib.Hubs.LogLevel.Major, "TTServices disconnected.");
                    //SetText(txtAPIConnected, "disconnected");
                    if (m_TTServices != null)
                    {
                        m_TTServices.Dispose();
                        m_TTServices = null;
                    }
                }
            }
        }// TTServices_ServiceStateChanged()
        //
        //
        //
        // ****         TalkerHub_StateChanged()            ****
        //
        private void TalkerHub_ServiceStateChanged(object sender, EventArgs eventArgs)
        {
            if (m_TalkerHub.IsConnectedToClient)
                SetText(txtBreTalkerConnectionStatus, "connected");
            else
                SetText(txtBreTalkerConnectionStatus, "disconnect");


        }//TalkerHub_ServiceStateChanged()
        //
        //
        //
        //
        #endregion // External Services Event Handlers


        #region Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        private void FormStartTalkerHub_FormClosing(object sender, FormClosingEventArgs e)
        {
            
            ShutDown();
        }
        private void buttonConnectToExcel_Click(object sender, EventArgs e)
        {
            if ( ! m_TalkerHub.IsConnectedToClient )
                m_TalkerHub.Request(TalkerHubRequest.AmberXLConnect);
            else
                m_TalkerHub.Request(TalkerHubRequest.AmbreXLDisconnect);
        }
        //
        //
        //
        //
        #endregion//Event Handlers

    }
}
