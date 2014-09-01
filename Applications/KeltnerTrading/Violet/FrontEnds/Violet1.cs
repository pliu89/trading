using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Violet.FrontEnds
{
    using Microsoft.Win32;
    using UV.Lib.FrontEnds.Utilities;

    using UV.Lib.Application;
    using UV.Lib.Hubs;
    using UV.Lib.FrontEnds;

    public partial class Violet1 : Form
    {
        
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // Initialization
        private string m_ConfigFileName = "VioletConfig.txt";
        private FrontEndServer m_FrontEndServer = null;                     // Server to log to.

        // Shutdown controls
        private bool m_IsShuttingDown = false;                              // 
        private int m_ShutdownCountdown = 10;                                // Max time to wait for shutdown.
        private FormWindowState m_LastWindowState = FormWindowState.Normal;
        private Timer m_ShutdownTimer = null;
        private int m_UpdateTimeInterval = 1000;                            // msecs


        #endregion// members


        #region Constructors & Startup
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public Violet1(string[] args)
        {
            System.Threading.Thread.CurrentThread.Name = "Violet UI";       // Name this thread for convenience.
            InitializeComponent();
            this.Icon = UV.Violet.Properties.Resources.Violet11;
            AppServices.GetInstance().AppIcon = this.Icon;                  // share icon to app service.
            InitializeNotifyIcon();
            // Or create one from bit map
            //this.Icon = Icon.FromHandle(UV.Violet.Properties.Resources.Violet1.GetHicon());
            //System.IO.FileStream fileStream = new System.IO.FileStream("Violet1.ico", System.IO.FileMode.OpenOrCreate);
            //this.Icon.Save(fileStream);
            //fileStream.Flush();
            //fileStream.Close();


            InitializeTabs();
            
            StartServices();
        }
        //
        //
        // *********************************************
        // ****         InitializeTabs()            ****
        // *********************************************
        /// <summary>
        /// Creates the controls and places them on tabs.
        /// </summary>
        private void InitializeTabs()
        {
            this.tabControl.SuspendLayout();
            this.SuspendLayout();

            // Delete old page
            this.tabControl.Controls.Clear();

            // Create the tab page.
            TabPage tabPage = new System.Windows.Forms.TabPage();
            tabPage.Location = new System.Drawing.Point(0, 0);
            tabPage.Name = "tabPageServices";
            tabPage.Padding = new System.Windows.Forms.Padding(0);
            //tabPage.Size = new System.Drawing.Size(453, 346);
            tabPage.TabIndex = 0;
            tabPage.Text = "Services";
            tabPage.UseVisualStyleBackColor = true;
            
            UV.Violet.Panels.ServiceViewer control = new UV.Violet.Panels.ServiceViewer();            
            control.Location = new System.Drawing.Point(2, 2);
            control.Visible = true;
            control.Size = new System.Drawing.Size(tabPage.ClientSize.Width, tabPage.ClientSize.Height);
            control.Anchor = (AnchorStyles)(AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
            tabPage.Controls.Add(control);
            this.tabControl.TabPages.Add(tabPage);


            // Exit.
            this.tabControl.ResumeLayout(false);
            this.ResumeLayout(false);
        }//InitializeTabs()
        //
        //
        // *********************************************
        // ****         StartServices()             ****
        // *********************************************
        private void StartServices()
        {
            //
            // Set Application services.
            //
            UV.Lib.Application.AppServices appServices = UV.Lib.Application.AppServices.GetInstance();
            appServices.Info.RequestShutdownAddHandler(new EventHandler(Service_RequestShutdown));
            appServices.ServiceStopped += new EventHandler(Service_ServiceStopped);
            appServices.ServiceAdded += new EventHandler(Service_ServiceAdded);

            // Listen to Win32 Sessions
            // TODO: Can these be handled within our AppServices class, and pass along these to Service_RequestShutdown
            Microsoft.Win32.SystemEvents.SessionEnding += SystemEvents_SessionEnding;
            Microsoft.Win32.SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;

            // Load services from config file.
            appServices.TryLoadServicesFromFile(m_ConfigFileName);
            appServices.Start();                                        // Tells sevices to start threads, all initialization.
            List<IService> serviceList = appServices.GetServices(typeof(FrontEndServer));
            if (serviceList.Count > 0)                
                m_FrontEndServer = (FrontEndServer) serviceList[0];        // use first front end as my log.
            appServices.Connect();

            // Update Violet form
            this.Text = string.Format("{0}", appServices.User);

        }// StartServices

        //
        //       
        #endregion//Constructors


        #region Shutdown
        // *****************************************************
        // ****             Request Shutdown()              ****
        // *****************************************************
        /// <summary>
        /// External thread request an application shutdown.
        /// </summary>
        private void Service_RequestShutdown(object sender, EventArgs eventArgs)
        {
            if (m_FrontEndServer != null)
                m_FrontEndServer.Log.NewEntry(LogLevel.Major, "Violet.Service_RequestShutdown:  ");
            if (m_IsShuttingDown)
                return;
            if (this.InvokeRequired)
                this.Invoke(new EventHandler(Service_RequestShutdown), new object[] { sender, eventArgs });
            else
            {
                BeginShutDown();
                this.Close();
            }
        }// Service_RequestShutdown()
        //
        //
        //
        // *****************************************************************
        // ****                     Shutdown                            ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// Called by GUI thread before the form is closed. Releases resources nicely.
        /// </summary>
        private void BeginShutDown()
        {
            if (m_FrontEndServer != null)
                m_FrontEndServer.Log.NewEntry(LogLevel.Major, "Violet.Shutdown()");
            if ( ! m_IsShuttingDown)
            {   // Begin shutdown sequence.
                m_IsShuttingDown = true;
                this.notifyIcon.Visible = false;
                //ControlTools.SetBalloonTip(this, this.notifyIcon, "Shutting down.");
                if (m_ShutdownTimer == null)
                {
                    m_ShutdownTimer = new Timer();
                }
                m_ShutdownTimer.Tick += new EventHandler(Timer_Tick);
                m_ShutdownTimer.Interval = m_UpdateTimeInterval;
                m_ShutdownTimer.Enabled = true;                    // provides countdown to shutdown.

                Microsoft.Win32.SystemEvents.SessionEnding -= SystemEvents_SessionEnding;
                Microsoft.Win32.SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
                UV.Lib.Application.AppServices.GetInstance().Shutdown();
                
            }
        }//Shutdown().
        //
        //
        //
        // *********************************************************
        // ****                 Timer_Tick()                    **** 
        // *********************************************************
        private void Timer_Tick(object sender, EventArgs eventArgs)
        {
            // Shutdown procedure
            if (m_IsShuttingDown)
            {
                m_ShutdownCountdown--;
                int aliveServices = UV.Lib.Application.AppServices.GetInstance().GetServices().Count;
                if (m_FrontEndServer != null)
                    m_FrontEndServer.Log.NewEntry(LogLevel.Major, "Violet.Timer_Tick:  Is shutting down. Services alive {0}.", aliveServices);
                if (aliveServices < 1 || m_ShutdownCountdown <= 0)
                {
                    this.Visible = false;
                    if (m_ShutdownTimer != null)
                    {
                        m_ShutdownTimer.Tick -= new EventHandler(Timer_Tick);
                        m_ShutdownTimer.Stop();
                        m_ShutdownTimer = null;
                    }
                    UV.Lib.Application.AppServices.GetInstance().ServiceStopped -= new EventHandler(Service_ServiceStopped);
                    UV.Lib.Application.AppServices.GetInstance().ServiceAdded -= new EventHandler(Service_ServiceAdded);
                    this.Close();
                }
                return;
            }// if shutting down
        }// Timer_Tick()
        //
        //
        //
        //
        #endregion//Shutdown


        #region no Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //
        //
        //
        //
        #endregion//Public Methods


        #region no Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        //
        //
        //
        //
        //
        //
        //
        //
        #endregion//Private Methods


        #region Service Event Handlers
        // *********************************************************************
        // ****             Service_ServiceStateChanged                     ****
        // *********************************************************************
        /// <summary>
        /// 
        /// </summary>
        private void Service_ServiceStateChanged(object sender, EventArgs eventArgs)
        {
            if (this.InvokeRequired)
            {
                //this.BeginInvoke(new EventHandler(Service_ServiceStateChanged), new object[] { sender, eventArgs });
                return;
            }
            if (m_FrontEndServer != null)
                m_FrontEndServer.Log.NewEntry(LogLevel.Major, "Violet.Service_ServiceStateChanged: {0} ", eventArgs);
            
        }// Service_ServiceStateChanged()
        //
        //
        // *********************************************************************
        // ****             Service_ServiceStopped()                        ****
        // *********************************************************************
        private void Service_ServiceStopped(object sender, EventArgs eventArgs)
        {            
            if (this.InvokeRequired)
            {
                //this.BeginInvoke(new EventHandler(Service_ServiceStopped), new object[] { sender, eventArgs });
                return;
            }
            if (m_FrontEndServer != null)
                m_FrontEndServer.Log.NewEntry(LogLevel.Major, "Violet.Service_ServiceStopped: {0} ", eventArgs);
        }// Service_ServiceStopped()
        //
        //
        //
        // *********************************************************************
        // ****             Service_ServiceAdded()                          ****
        // *********************************************************************
        private void Service_ServiceAdded(object sender, EventArgs eventArgs)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new EventHandler(Service_ServiceAdded), new object[] { sender, eventArgs });
                return;
            }
            if (m_FrontEndServer != null)
                m_FrontEndServer.Log.NewEntry(LogLevel.Major, "Violet.Service_ServiceAdded: {0} ", eventArgs);


        }//Service_ServiceAdded()
        //
        //
        #endregion//Event Handlers


        #region Win32 SystemEvent handlers
        //
        //
        //
        // ****             SystemEvents_SessionEnding()                ****
        private void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
        {
            if (m_FrontEndServer != null)
                m_FrontEndServer.Log.NewEntry(LogLevel.Major, "Violet.SystemEvents_SessionEnding: {0} ", e);
            e.Cancel = true;
            BeginShutDown();
        }
        //
        //
        /// <summary>
        /// This is triggered when the screen locks from inactivity.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (m_FrontEndServer != null)
                m_FrontEndServer.Log.NewEntry(LogLevel.Major, "Violet.SystemEvents_SessionSwitch: {0} ", e);
        }
        //
        //
        #endregion//system events

        #region Notify Icon Handlers
        //
        //
        // *********************************************************
        // ****             Initialize Context Menu()           ****
        // *********************************************************
        private void InitializeNotifyIcon()
        {
            // Start notify icon
            this.notifyIcon.Visible = false;
            this.notifyIcon.Text = "Violet";
            this.notifyIcon.Icon = this.Icon;
            this.notifyIcon.DoubleClick += new EventHandler(NotifyIcon_DoubleClick);
            this.notifyIcon.BalloonTipTitle = "Violet";
            //this.notifyIcon.BalloonTipText

            // Create its context menu.
            ContextMenu contextMenu;
            contextMenu = new ContextMenu();
            contextMenu.Name = "NotifyContextMenu";

            this.notifyIcon.ContextMenu = contextMenu;

            //
            // Add menu items.
            //
            MenuItem contextMenuItem;
            contextMenuItem = new MenuItem();
            //contextMenuItem.Index = 0;
            contextMenuItem.Name = "Hide/Restore";
            contextMenuItem.Text = "Hide";  // Hide/Restore
            contextMenuItem.Click += ContextMenuItem_Click;
            contextMenu.MenuItems.Add(contextMenuItem);

            //contextMenuItem = new MenuItem();
            //contextMenuItem.Text = "Status";
            //contextMenuItem.Click += ContextMenuItem_Click;
            //contextMenu.MenuItems.Add(contextMenuItem);

            contextMenuItem = new MenuItem();
            contextMenuItem.Text = "Services";
            contextMenuItem.Click += ContextMenuItem_Click;
            contextMenu.MenuItems.Add(contextMenuItem);


            contextMenuItem = new MenuItem();
            contextMenuItem.Text = "Exit";
            contextMenuItem.Click += ContextMenuItem_Click;
            contextMenu.MenuItems.Add(contextMenuItem);


            // Show start up message.
            //this.notifyIcon.BalloonTipText = "Starting...";
            //this.notifyIcon.ShowBalloonTip(1000);
            this.notifyIcon.Visible = true;

        }
        //
        //
        // *****************************************************************
        // ****                Update Context Menu                      ****
        // *****************************************************************
        /// <summary>
        /// Changes notify based on the visibility (etc) of the main form.
        /// </summary>
        private void UpdateContextMenu()
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.ShowInTaskbar = false;
                // TODO: Minimize all Log viewers too.
                //foreach (IService iService in AppServices.GetInstance().Services.Values)
                //if (iService is Hub)
                //    if (iService.GetType().IsSubclassOf(typeof(Hub)))
                //        ((Hub)iService).Log.IsViewActive = false;
                foreach (IService iService in AppServices.GetInstance().GetServices(typeof(Hub)))
                    ((Hub)iService).Log.IsViewActive = false;


                //foreach (Misty.Lib.MarketHubs.MarketHub iService in AppServices.GetInstance().ServiceMarkets.Values)
                //    ((Misty.Lib.MarketHubs.MarketHub)iService).Log.IsViewActive = false;
                //foreach (Misty.Lib.OrderHubs.OrderHub iService in AppServices.GetInstance().ServiceOrders.Values)
                //    ((Misty.Lib.OrderHubs.OrderHub)iService).Log.IsViewActive = false;
                // Update the context menu.
                foreach (MenuItem menu in this.notifyIcon.ContextMenu.MenuItems)
                    if (menu.Name.Equals("Hide/Restore") && menu.Text.Equals("Hide"))
                    {
                        menu.Text = "Restore";
                        break;
                    }
            }
            else if (this.WindowState == FormWindowState.Normal || this.WindowState == FormWindowState.Maximized)
            {
                this.ShowInTaskbar = true;
                foreach (MenuItem menu in this.notifyIcon.ContextMenu.MenuItems)
                    if (menu.Name.Equals("Hide/Restore") && menu.Text.Equals("Restore"))
                    {
                        menu.Text = "Hide";
                        break;
                    }
            }
        }// UpdateContextMenu()
        //
        //
        // *****************************************************************
        // ****                Notify Event Handlers                     ****
        // *****************************************************************
        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
                this.WindowState = FormWindowState.Normal;
            else if (this.WindowState == FormWindowState.Normal || this.WindowState == FormWindowState.Maximized)
                this.WindowState = FormWindowState.Minimized;
        }
        //
        //
        // ****             ContextMenuItem_Click()             ****
        //
        private void ContextMenuItem_Click(object sender, EventArgs e)
        {
            Type eventType = e.GetType();
            Type senderType = sender.GetType();
            if (senderType == typeof(MenuItem))
            {
                MenuItem menuItem = (MenuItem)sender;
                string selectedMenu = menuItem.Text;
                switch (selectedMenu)
                {
                    case "Exit":
                        BeginShutDown();
                        break;
                    case "Hide":
                        this.WindowState = FormWindowState.Minimized;
                        break;
                    case "Restore":
                        this.WindowState = FormWindowState.Normal;
                        break;
                    case "Status":
                        StringBuilder s = new StringBuilder();                  // Create status report
                        int nServices = 0;
                        foreach (IService iService in AppServices.GetInstance().GetServices())
                        {
                            /*
                            if (iService is TTServices.TTApiService)
                            {
                                TTServices.TTApiService ttApi = (TTServices.TTApiService)iService;
                                if (ttApi.IsRunning)
                                {
                                    s.AppendFormat("TT Api: connected.\r\n");
                                    s.AppendFormat("TT Login: {0}\r\n", ttApi.LoginUserName);
                                }
                                else
                                    s.Append("TT Api: disconnected.\r\n");
                            }
                            */ 
                            nServices++;
                        }
                        s.AppendFormat("Services running: {0}.", nServices); // note the lack of \r\n (keep this message as last in summary.)
                        ControlTools.SetBalloonTip(this, this.notifyIcon, s.ToString());
                        break;
                    case "Services":
                        StringBuilder s1 = new StringBuilder();                  // Create status report
                        nServices = 0;
                        foreach (IService iService in AppServices.GetInstance().GetServices())
                        {
                            /*
                            if (iService is TTServices.TTApiService)
                            {
                                TTServices.TTApiService ttApi = (TTServices.TTApiService)iService;
                                if (ttApi.IsRunning)
                                {
                                    s1.AppendFormat("TT Api: connected.\r\n");
                                    s1.AppendFormat("TT Login: {0}\r\n", ttApi.LoginUserName);
                                }
                                else
                                    s1.Append("TT Api: disconnected.\r\n");
                            }
                            else
                            {
                                s1.AppendFormat("Service: {0}\r\n", iService.ServiceName);
                            }
                            */
                            nServices++;
                        }
                        s1.AppendFormat("Services running: {0}.", nServices); // note the lack of \r\n (keep this message as last in summary.)
                        ControlTools.SetBalloonTip(this, this.notifyIcon, s1.ToString());
                        break;
                    default:
                        break;
                }
            }
        }//ContextMenuItem_Click()
        //
        //
        //        
        #endregion // notify icon

        #region Form Event Handlers
        // *****************************************************************
        // ****                 Form Event Handlers                     ****
        // *****************************************************************
        //
        //
        //
        //
        //
        //
        // *******************************************
        // ****         FormClosing()             ****
        // *******************************************
        private void Form_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!m_IsShuttingDown)
            {
                e.Cancel = true;                // First time called, cancel and try to shutdown slowly.
                BeginShutDown();
            }
        }
        //
        //
        // *****************************************
        // ****         Form_Resize()           ****
        // *****************************************
        private void Form_Resize(object sender, EventArgs e)
        {
            if (m_LastWindowState != this.WindowState)
            {   // Do these only when window is minimized/maximized etc.
                UpdateContextMenu();
            }
            m_LastWindowState = this.WindowState;
        }
        //
        //
        // *****************************************
        // ****         Button_Click()          ****
        // *****************************************
        private void Button_Click(object sender, EventArgs e)
        {
            AppServices services = AppServices.GetInstance();
            if (m_FrontEndServer != null )
                m_FrontEndServer.Log.NewEntry(LogLevel.Major, "Violet.Button_Click: {0} {1}", ((Control)sender).Name, e);            
        }//
        //
        //
        //
        #endregion//Form Event Handlers
    }
}
