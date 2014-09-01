using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ambre.PositionViewer.FrontEnds
{
    using SKACERO;

    using Misty.Lib.Application;
    using Misty.Lib.Utilities;
    using Misty.Lib.Hubs;
    using Misty.Lib.FrontEnds;

    using InstrumentName = Misty.Lib.Products.InstrumentName;
    using Ambre.TTServices;
    using Ambre.TTServices.Markets;
    using Ambre.TTServices.Fills;
    using Ambre.TTServices.Talker;
    using Misty.Lib.Products;
    using Misty.Lib.IO.Xml;
    using TradingTechnologies.TTAPI.WinFormsHelpers;

    using Microsoft.Win32;

    public partial class AmbreViewer : Form
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        private Dictionary<int, TalkerHub> m_TalkerHubs = new Dictionary<int, TalkerHub>();
        public LogHub Log = null;

        // My active children
        private Ambre.TTServices.Fills.RejectedFills.FormRejectViewer m_RejectedFillViewer = null;

        // Internal application controls
        private bool m_IsShuttingDown = false;                              // 
        private int m_ShutdownCountdown = 5;                                //
        private FormWindowState m_LastWindowState = FormWindowState.Normal;
        //private bool m_ServiceConnectionRequested = false;                // makes sure we only call "connect" once.
        private Timer m_Timer = new Timer();
        private int m_UpdateTimeInterval = 2000;                            // msecs


        // Constants
        private Color Color_WarningBG_None = System.Drawing.Color.Gray;     //.SystemColors.Tran.Control;
        private Color Color_WarningBG_Off = System.Drawing.Color.DarkGreen; //.SystemColors.Tran.Control;
        private Color Color_WarningBG_On = Color.DarkRed;
        private Color Color_WarningBG_On2 = Color.Red;

        private string Base_Directory_Logs = "\\\\fileserver\\Users\\DV_Ambre\\AmbreUsers\\";
        private string TalkerHub_TextBase = "Link to Excel: ";
        private string TalkerHub_NameBase = "LinkToExcel";
        public string UserConfigFileName = "AmbreConfig.txt";

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public AmbreViewer()
        {
            InitializeComponent();
            this.Icon = Ambre.PositionViewer.Properties.Resources.user_female;
            InitializeNotifyIcon();

            // Disable the buttons on the menu until TT is connected.
            this.menuFile.Enabled = false;
            this.menuWindows.Enabled = false;
            this.menuConnections.Enabled = false;

            while (tabControl.TabPages.Count > 0)                       // remove all starting tab pages.
                tabControl.TabPages.RemoveAt(0);
            textExcelLinkWarning.BackColor = Color_WarningBG_None;      // default is to turn warning off all-together.
            System.Threading.Thread.CurrentThread.Name = "AmbreUI";     // Name this thread for convenience.

            // Create service
            typeof(Ambre.TTServices.Markets.MarketTTAPI).ToString();    // force needed assemblies to load.
            AppServices appServices = AppServices.GetInstance("Ambre");
            appServices.Info.RequestShutdownAddHandler(new EventHandler(this.RequestShutdown));
            appServices.ServiceStopped += new EventHandler(Service_ServiceStopped);

            // Test whether config file exists.
            string configPath = string.Format("{0}{1}", appServices.Info.UserConfigPath, UserConfigFileName);
            if (!System.IO.File.Exists(configPath))
            {
                DialogResult result = System.Windows.Forms.DialogResult.Abort;
                result = MessageBox.Show(string.Format("Config file does not exist! \r\nFile: {0}\r\nDir: {1}\r\nShould I create an empty one?", UserConfigFileName, appServices.Info.UserConfigPath), "Config file not found", MessageBoxButtons.YesNo);
                if (result == System.Windows.Forms.DialogResult.Yes)
                    using (System.IO.StreamWriter writer = new System.IO.StreamWriter(configPath, false))
                    {
                        writer.WriteLine(" ");
                        writer.Close();
                    }
            }

            UpdateGrandfatherConfigFiles();
            CleanupLocalDropFolders();
            appServices.LoadServicesFromFile(UserConfigFileName);
            appServices.Start();
            appServices.Connect();
            foreach (IService service in appServices.GetServices())
                this.AddService(service);                                // Create pages for any services.

            m_Timer.Tick += new EventHandler(Timer_Tick);
            m_Timer.Interval = m_UpdateTimeInterval;
            m_Timer.Start();

            // Start minimized!

            Microsoft.Win32.SystemEvents.SessionEnding += SystemEvents_SessionEnding;
            Microsoft.Win32.SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
            //Microsoft.Win32.SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
            //Microsoft.Win32.SystemEvents.SessionEnded += SystemEvents_SessionEnded;
        }



        //
        //  
        //
        // *************************************************
        // ****             AddService()                ****
        // *************************************************
        /// <summary>
        /// After a services is added to AppServices, then call this to add it to this form.
        /// Called by UI thread to update form after creating new service.
        /// </summary>
        public void AddService(IService service)
        {
            if (service is FillHub)
            {
                FillHub fillHub = (FillHub)service;
                if (Log == null)
                {
                    Log = fillHub.Log;           // borrow log from first FillHub for this gui's log.
                    Log.NewEntry(LogLevel.Major, "Viewer: Ambre viewer will write to this Log.");
                }
                FillHubPage newPage = new FillHubPage(null);
                this.tabControl.TabPages.Add(newPage);
                newPage.AddHub(fillHub);
            }
            else if (service is TalkerHub)
            {
                TalkerHub talker = (TalkerHub)service;
                talker.ServiceStateChanged += new EventHandler(TalkerHub_ServiceStateChanged);
                talker.Start();
                lock (m_TalkerHubs)
                {
                    m_TalkerHubs.Add(talker.Port, talker);
                }

                // Create menuitem - add to correct submenu
                this.SuspendLayout();
                System.Windows.Forms.ToolStripMenuItem newMenuItem = new System.Windows.Forms.ToolStripMenuItem();
                newMenuItem.Name = string.Format("{0}{1}", TalkerHub_NameBase, talker.Port);
                newMenuItem.Size = new System.Drawing.Size(169, 22);
                newMenuItem.Text = string.Format("{0}{1}", TalkerHub_TextBase, talker.Port);
                newMenuItem.Click += new System.EventHandler(this.Menu_Click);
                this.menuConnections.DropDownItems.Add(newMenuItem);
                if (textExcelLinkWarning.BackColor == Color_WarningBG_None) // when there are NO Talkers, leave warning box gray.
                    textExcelLinkWarning.BackColor = Color_WarningBG_On;    // otherwise set to red.
                this.ResumeLayout(false);
            }
            else if (service is TTServices.TTApiService)
            {
                ((TTServices.TTApiService)service).ServiceStateChanged += new EventHandler(Service_ServiceStateChanged);
            }
        }//AddServiceView()
        //
        //
        // *************************************************
        // ****             RemoveService()             ****
        // *************************************************
        public void RemoveService(IService service)
        {
            if (service is TalkerHub)
            {
                TalkerHub talker = (TalkerHub)service;
                talker.ServiceStateChanged -= new EventHandler(TalkerHub_ServiceStateChanged);
                //talker.Start();
                lock (m_TalkerHubs)
                {
                    m_TalkerHubs.Remove(talker.Port);
                }
                talker.RequestStop();

                // Create menuitem - add to correct submenu
                this.SuspendLayout();
                string menuItemKey = string.Format("{0}{1}", TalkerHub_NameBase, talker.Port);
                if (this.menuConnections.DropDownItems.ContainsKey(menuItemKey))
                {
                    System.Windows.Forms.ToolStripMenuItem newMenuItem = (System.Windows.Forms.ToolStripMenuItem)this.menuConnections.DropDownItems[menuItemKey];
                    this.menuConnections.DropDownItems.RemoveByKey(menuItemKey);
                    newMenuItem.Click += new System.EventHandler(this.Menu_Click);
                }
                this.ResumeLayout(false);
            }
        }//RemoveService()
        //
        //
        //
        // *****************************************************************
        // ****                 Create New FillHub()                    ****
        // *****************************************************************
        public void CreateNewFillHub(Misty.Lib.IO.Xml.Node aNode)
        {
            if (aNode.Name.Equals(typeof(FillHub).FullName))
            {
                // Create hub
                FillHub fillHub = null;
                string name;
                if (!aNode.Attributes.TryGetValue("Name", out name))        // this allows the fillhub to find its dropbook file for initialization.
                    name = string.Empty;
                fillHub = new FillHub(name, true);

                if (!AppServices.GetInstance().TryAddService(fillHub))
                {   // Failed - non-unique name.

                }
                fillHub.Start();            // initializes all its services.

                this.AddService(fillHub);
                foreach (TalkerHub talker in m_TalkerHubs.Values)
                    talker.RequestAddHub(fillHub);

                fillHub.Connect();// If we have already made connection requests for other hubs, just request connection this new one.
            }
        }// CreateFillHub()
        //
        //
        //
        // *****************************************************************
        // ****               Create New TalkerHub()                    ****
        // *****************************************************************
        public void CreateNewTalkerHub(Misty.Lib.IO.Xml.Node aNode)
        {
            if (aNode.Name.Equals(typeof(TalkerHub).FullName))
            {
                int portId;
                if (int.TryParse(aNode.Attributes["Port"], out portId))
                {
                    TalkerHub talker = new TalkerHub(false, portId);
                    talker.Start();
                    this.AddService(talker);
                    talker.Connect();

                }
            }
        }
        //
        //
        // *****************************************************************
        // ****             Update Grandfather ConfigFiles()            ****
        // *****************************************************************
        /// <summary>
        /// Originally, the config files only contained the FillHubs. 
        /// So, this routine is called to upgrade the users' config file.
        /// It is probably no longer needed, since all User have been using Ambre regularly
        /// and have already been updated presumably.
        /// </summary>
        private void UpdateGrandfatherConfigFiles()
        {
            AppServices appServices = AppServices.GetInstance();
            // Grand-father old config files.
            string commonBase = "Ambre.TTServices.";
            List<string> newServiceList = new List<string>();
            newServiceList.Add("Markets.MarketTTAPI");
            newServiceList.Add("TTApiService FollowXTrader=True");
            string newUserLine = "Misty.Lib.Application.UserInformation Name=";
            string userLine = string.Empty;
            bool inputNewUserNameFlag = false;
            List<string> serviceList = new List<string>();
            string configPath = string.Format("{0}{1}", appServices.Info.UserConfigPath, UserConfigFileName);

            try
            {
                // Load all lines.
                using (System.IO.StreamReader reader = new System.IO.StreamReader(configPath))
                {
                    string aLine = string.Empty;
                    while ((aLine = reader.ReadLine()) != null)
                    {
                        aLine = aLine.Trim();
                        if (aLine.Contains(newUserLine))
                            userLine = aLine;
                        else if (!string.IsNullOrEmpty(aLine))
                            serviceList.Add(aLine.Trim());
                    }
                    reader.Close();
                }

                // Search for newly required lines.
                foreach (string aService in serviceList)
                {
                    int n = 0;
                    while (n < newServiceList.Count)
                    {
                        if (aService.Contains(newServiceList[n]))
                            newServiceList.RemoveAt(n);
                        else
                            n++;
                    }
                }

                // Determine whether the user name is valid.
                string userName;
                if (string.IsNullOrEmpty(userLine))
                {
                    inputNewUserNameFlag = true;
                    userName = "";
                }
                else
                {
                    userName = userLine.Substring(userLine.LastIndexOf("=") + 1, userLine.Length - (userLine.LastIndexOf("=") + 1));
                    if (string.IsNullOrEmpty(userName))
                        inputNewUserNameFlag = true;
                    else
                        inputNewUserNameFlag = false;
                }

                // Let user input the user name.
                if (inputNewUserNameFlag)
                {
                    string message = "Please input the user name!";
                    string title = "User Information";
                    userName = Microsoft.VisualBasic.Interaction.InputBox(message, title, "", 500, 500);
                }

                // If there are still new services to include, add them now to the config file.
                if (newServiceList.Count > 0 || inputNewUserNameFlag)
                {
                    foreach (string aNewService in newServiceList)
                        serviceList.Add(string.Format("<{0}{1}/>", commonBase, aNewService));       // Add missing services.

                    // Save copy of old config file.                    
                    System.IO.File.Copy(configPath, string.Format("{0}{1}_{2:yyyyMMdd}.txt", appServices.Info.UserConfigPath,
                        UserConfigFileName.Substring(0, UserConfigFileName.LastIndexOf('.')), DateTime.Now));

                    // Now write the full service list back out.
                    using (System.IO.StreamWriter writer = new System.IO.StreamWriter(configPath, false))
                    {
                        foreach (string aService in serviceList)
                            writer.WriteLine(aService);
                        writer.WriteLine(string.Format("<{0}{1}/>", newUserLine, userName));
                        writer.Close();
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
        //
        //
        // *****************************************************************
        // ****              Update AmbreName ConfigFiles()             ****
        // *****************************************************************
        /// <summary>
        /// Originally, the config files' FillHubs do not contain AmbreName. 
        /// So, this routine is called to upgrade the users' config file.
        /// </summary>
        //private void UpdateFillHubConfigFile()
        //{
        //    AppServices appServices = AppServices.GetInstance();
        //    List<string> serviceList = new List<string>();
        //    string configPath = string.Format("{0}{1}", appServices.Info.UserConfigPath, UserConfigFileName);
        //    bool filechange = false;
        //    // Load all lines.
        //    List<IStringifiable> iStringObjects;
        //    using (StringifiableReader reader = new StringifiableReader(configPath))
        //        iStringObjects = reader.ReadToEnd();
        //    List<IStringifiable> newIStringObjects = new List<IStringifiable>();
        //    Dictionary<Type, string[]> m_StringifyOverrideTable = null;
        //    using (System.IO.StreamReader reader = new System.IO.StreamReader(configPath))
        //    {
        //        //string aLine = string.Empty;
        //        //while ((aLine = reader.ReadLine()) != null)
        //        foreach (IStringifiable obj in iStringObjects)
        //        {
        //            if (obj is FillHub)
        //            {
        //                FillHub newhub = (FillHub)obj;
        //                if (newhub.AmbreName == string.Empty)
        //                {
        //                    string AmbreName = string.Empty;
        //                    using (FormUpdateAmbreName AmbreNameGetter = new FormUpdateAmbreName(string.Format("{0}{1}", "Trading account ", newhub.Name)))
        //                    {
        //                        AmbreNameGetter.ShowDialog();
        //                        AmbreName = AmbreNameGetter.Tag.ToString();
        //                    }
        //                    newhub.AmbreName = AmbreName;
        //                    //newIStringObjects.Add((IStringifiable)newhub);
        //                    filechange = true;
        //                    serviceList.Add(Stringifiable.Stringify((IStringifiable)newhub, m_StringifyOverrideTable));
        //                }
        //                else
        //                    serviceList.Add(Stringifiable.Stringify(obj, m_StringifyOverrideTable));
        //            }
        //            else
        //                //newIStringObjects.Add(obj);
        //                serviceList.Add(Stringifiable.Stringify(obj, m_StringifyOverrideTable));
        //        }
        //    }
        //    if (filechange)
        //    {
        //        using (System.IO.StreamWriter writer = new System.IO.StreamWriter(configPath, false))
        //        {
        //            foreach (string aService in serviceList)
        //                writer.WriteLine(aService);
        //            writer.Close();
        //        }
        //    }
        //}
        #endregion//Constructors


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //
        // *********************************************************
        // ****             Show Rejected Fills()               ****
        // *********************************************************
        /// <summary>
        /// </summary>
        /// <param name="sender">The FillHub that rejected the fill.</param>
        /// <param name="eventArgs"></param>
        public void ShowRejectedFills(object sender, EventArgs eventArgs)
        {
            if (this.InvokeRequired)
                this.Invoke(new EventHandler(ShowRejectedFills), new object[] { sender, eventArgs });
            else
            {
                FillHub fillHub;
                if (sender is FillHubPage)      // originally we accepted FillHubPages as well.
                    fillHub = ((FillHubPage)sender).m_FillHub;
                else if (sender is FillHub)
                    fillHub = (FillHub)sender;
                else
                    return;

                if (m_RejectedFillViewer == null || m_RejectedFillViewer.IsDisposed)
                {
                    try
                    {
                        m_RejectedFillViewer = new TTServices.Fills.RejectedFills.FormRejectViewer(fillHub);
                        m_RejectedFillViewer.Show();
                        m_RejectedFillViewer.FormClosing += new FormClosingEventHandler(ChildForm_Closing);
                        m_RejectedFillViewer.UpdateNow(fillHub);
                    }
                    catch (Exception)
                    {
                        Log.NewEntry(LogLevel.Major, "Viewer: Failed to open Rejected fill viewer.");
                    }
                }
                else
                {
                    m_RejectedFillViewer.UpdateNow(fillHub);
                    m_RejectedFillViewer.Focus();
                }
            }
        }// ShowRejectedFills()
        //
        //
        //
        //
        #endregion//Public Methods


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        //
        //       
        //
        // ************************************************************
        // ****                   ShutDown()                       ****
        // ************************************************************
        /// <summary>
        /// Called by GUI thread when the form is closing. Releases resources nicely.
        /// </summary>
        private void ShutDown()
        {
            // Archieve books before any kinds of shut down methods.
            foreach (IService service in AppServices.GetInstance().GetServices(typeof(FillHub)))
                ((FillHub)service).Request(new Misty.Lib.OrderHubs.OrderHubRequest(Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestDropCopyNow));

            if (!m_IsShuttingDown)
            {
                m_IsShuttingDown = true;
                Microsoft.Win32.SystemEvents.SessionEnding -= SystemEvents_SessionEnding;
                Microsoft.Win32.SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
                //Microsoft.Win32.SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
                //Microsoft.Win32.SystemEvents.SessionEnded += SystemEvents_SessionEnded;

                AppServices.GetInstance().Shutdown();

                ControlTools.SetBalloonTip(this, this.notifyIcon, "Shutting down.");
                this.notifyIcon.Visible = false;

            }
        }//Shutdown().
        //
        //
        //
        //
        // *********************************************************
        // ****                 Timer_Tick()                    **** 
        // *********************************************************
        private void Timer_Tick(object sender, EventArgs eventArgs)
        {
            if (m_IsShuttingDown)
            {
                m_ShutdownCountdown--;
                int aliveServices = AppServices.GetInstance().GetServices().Count;
                if (aliveServices < 1 || m_ShutdownCountdown <= 0)
                {
                    this.Visible = false;
                    m_Timer.Stop();
                    m_Timer = null;
                    AppServices.GetInstance().ServiceStopped -= new EventHandler(Service_ServiceStopped);
                    this.Close();                   // This is how we can slow down the quiting process.
                }
                return;
            }

            //
            // Flash connection link if off - cycle thru warning colors.
            //
            ControlTools.TrySwapBGColor(textExcelLinkWarning, Color_WarningBG_On, Color_WarningBG_On2);
            ControlTools.TrySwapBGColor(textApiLoginName, Color_WarningBG_On, Color_WarningBG_On2);
            //Color currentColor = textExcelLinkWarning.BackColor;
            //if (currentColor == Color_WarningBG_On)
            //    textExcelLinkWarning.BackColor = Color_WarningBG_On2;
            //else if ((currentColor == Color_WarningBG_On2))
            //    textExcelLinkWarning.BackColor = Color_WarningBG_On;

            //
            // Update the instrument infos for multiple pages.
            //
            foreach (TabPage eachPage in this.tabControl.TabPages)
            {
                if (eachPage is FillHubPage)
                    ((FillHubPage)eachPage).UpdateInstrumentInfos();
            }

            //
            // Update the visible page.
            //
            FillHubPage page = (FillHubPage)tabControl.SelectedTab;
            if (page != null)
                page.UpdatePage();

        }// Timer_Tick()
        //
        //
        //
        // *********************************************************
        // ****         Cleanup Local Drop Folders()            ****
        // *********************************************************
        /// <summary>
        /// Deletes the local drop folder after two weeks.
        /// </summary>
        private void CleanupLocalDropFolders()
        {
            double daysToKeep = 14;
            DateTime now = DateTime.Now;
            // Collect all the LOCAL drop directories.
            string dropPath = AppServices.GetInstance().Info.DropPath;
            if (!System.IO.Directory.Exists(dropPath))
            {
                System.IO.Directory.CreateDirectory(dropPath);
            }
            List<string> dirList = new List<string>(System.IO.Directory.GetDirectories(dropPath));
            dirList.Sort();
            foreach (string dirPath in dirList)
            {   // Delete only those that are old, and have a date format: yyyyMMdd
                DateTime dt;
                string dir = dirPath.Substring(dirPath.LastIndexOf('\\') + 1);
                if (Strings.TryParseDate(dir, out dt, "yyyyMMdd"))
                {
                    if (now.CompareTo(dt.AddDays(daysToKeep)) > 0)
                    {
                        System.IO.Directory.Delete(dirPath, true);
                    }
                }
            }// next dirPath

        }//CleanUpLocalDropFolders()

        /// <summary>
        /// This function saved logs to centralized place on the file server to better debug.
        /// </summary>
        private void CentralizeLogsToServer()
        {
            if (!Base_Directory_Logs.EndsWith("\\"))
                Base_Directory_Logs += "\\";
            string UserBasePath = string.Format("{0}{1}\\", Base_Directory_Logs, AppServices.GetInstance().m_AmbreUserName.Name);
            foreach (IService service in AppServices.GetInstance().GetServices())
            {
                if (service is MarketTTAPI)
                {
                    MarketTTAPI marketTTAPI = (MarketTTAPI)service;
                    marketTTAPI.Log.ProcessCopyTo(UserBasePath);
                }
                else if (service is FillHub)
                {
                    FillHub fillHub = (FillHub)service;
                    fillHub.Log.ProcessCopyTo(UserBasePath);
                }
                else if (service is TTApiService)
                {
                    TTApiService ttAPIService = (TTApiService)service;
                    ttAPIService.Log.ProcessCopyTo(UserBasePath);
                }
            }
        }
        //
        //
        //
        #endregion//Private Methods


        #region Notify Icon Handlers
        //
        //
        // *********************************************************
        // ****             Initialize Context Menu()           ****
        // *********************************************************
        private void InitializeNotifyIcon()
        {
            // Start notify icon
            this.notifyIcon.Text = "Ambre";
            this.notifyIcon.Icon = this.Icon;
            this.notifyIcon.DoubleClick += new EventHandler(NotifyIcon_DoubleClick);
            this.notifyIcon.BalloonTipTitle = "Ambre";
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
            this.notifyIcon.BalloonTipText = "Starting...";
            this.notifyIcon.ShowBalloonTip(1000);
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
                        RequestShutdown(sender, EventArgs.Empty);
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


        #region Ambre Event Handlers
        // *****************************************************************
        // ****                Ambre Event Handlers                     ****
        // *****************************************************************
        //
        //
        // ****             Request Shutdown()              ****
        //
        /// <summary>
        /// External thread request an application shutdown.
        /// </summary>
        private void RequestShutdown(object sender, EventArgs eventArgs)
        {
            if (m_IsShuttingDown)
                return;
            if (this.InvokeRequired)
                this.Invoke(new EventHandler(RequestShutdown), new object[] { sender, eventArgs });
            else
            {
                ShutDown();
                this.Close();
            }
        }// RequestShutdown()
        //
        //
        //
        #endregion//External Event Handlers


        #region External Service Event Handlers
        // *****************************************************************
        // ****           External Service Event Handlers               ****
        // *****************************************************************
        //
        //
        //
        // *********************************************************************
        // ****             TalkerHub Server State Changed                  ****
        // *********************************************************************
        private void Service_ServiceStateChanged(object sender, EventArgs eventArgs)
        {
            if (sender is TTServices.TTApiService)
            {
                TTServices.TTApiService ttApi = (TTServices.TTApiService)sender;
                if (ttApi.IsRunning)
                {
                    string msg = string.Format("TTApi login: {0}", ttApi.LoginUserName);
                    ControlTools.SetBalloonTip(this, this.notifyIcon, msg);
                    ControlTools.SetBGColor(textApiLoginName, Color_WarningBG_Off);
                    ControlTools.SetText(textApiLoginName, ttApi.LoginUserName);

                    // Enable the buttons on the menu when the TT is connected.
                    ControlTools.SetButton(this, menuFile, true);
                    ControlTools.SetButton(this, menuWindows, true);
                    ControlTools.SetButton(this, menuConnections, true);
                }
                else
                {
                    ControlTools.SetBalloonTip(this, this.notifyIcon, "TTApi not connected.");
                    ControlTools.SetBGColor(textApiLoginName, Color_WarningBG_On);
                    ControlTools.SetText(textApiLoginName, "? ? ?");
                }
            }
        }// Service_ServiceStateChanged()
        //
        //
        // *********************************************************************
        // ****             Service_ServiceStopped()                        ****
        // *********************************************************************
        private void Service_ServiceStopped(object sender, EventArgs eventArgs)
        {
            //int n = AppServices.GetInstance().GetServices().Count;
            //string msg = string.Format("Service stopped.");
            //MessageBox.Show(string.Format("Service stopped.  {0} remain.",n.ToString()));
            //AmbreViewer.SetBalloonTip(this, this.notifyIcon, msg);
        }
        //
        //
        //
        // *********************************************************************
        // ****             Service_ServiceAdded()                          ****
        // *********************************************************************
        private void Service_ServiceAdded(object service, EventArgs eventArgs)
        {
            if (service is IService)
                this.AddService((IService)service);
        }
        //
        //
        //
        // *********************************************************************
        // ****             TalkerHub Server State Changed                  ****
        // *********************************************************************
        private void TalkerHub_ServiceStateChanged(object sender, EventArgs eventArgs)
        {
            if (m_IsShuttingDown) return;
            Log.NewEntry(LogLevel.Minor, "Viewer: Talker hub state change {0}", eventArgs);
            int numberConnected = 0;
            bool isAllConnected = true;
            lock (m_TalkerHubs)
            {
                foreach (TalkerHub talker in m_TalkerHubs.Values)
                    if (talker.IsConnectedToClient)
                        numberConnected++;
                isAllConnected = numberConnected == m_TalkerHubs.Count;
            }
            if (isAllConnected)
            {
                ControlTools.SetBalloonTip(this, this.notifyIcon, "All excel links connected.");
                ControlTools.SetBGColor(textExcelLinkWarning, Color_WarningBG_Off);
            }
            else
            {
                ControlTools.SetBalloonTip(this, this.notifyIcon, string.Format("{0} excel links connected.", numberConnected));
                ControlTools.SetBGColor(textExcelLinkWarning, Color_WarningBG_On);
            }

            // Check the specific talker in the menu.
            if (sender is TalkerHub)
            {
                TalkerHub talker = (TalkerHub)sender;
                string menuName = string.Format("{0}{1}", TalkerHub_NameBase, talker.Port);
                if (this.menuConnections.DropDownItems.ContainsKey(menuName))
                {
                    System.Windows.Forms.ToolStripMenuItem menuItem = (System.Windows.Forms.ToolStripMenuItem)this.menuConnections.DropDownItems[menuName];
                    ControlTools.SetCheck(this, menuItem, talker.IsConnectedToClient);
                }
            }
        }
        //
        //
        //
        //
        // *********************************************************************
        // ****                     Child Form Closing                      ****
        // *********************************************************************
        /// <summary>
        /// If the user closes one of the child forms owned by this object, we want to drop its pointer.
        /// All sub forms, opened by this form, need to fire this event when they are closing.  That is, 
        /// this object will subscribe to "Closing" event for every child form it opens.
        /// </summary>
        private void ChildForm_Closing(object sender, FormClosingEventArgs e)
        {
            if (m_IsShuttingDown) return;
            Type formType = sender.GetType();
            Log.NewEntry(LogLevel.Minor, "Viewer: Child form closing {0}", formType);
            if (formType == typeof(FormAddFills))
            {
                //m_FormAddFills.FormClosing -= new FormClosingEventHandler(ChildForm_Closing);
                //m_FormAddFills = null;                      // disconnect
            }
            //else if (formType == typeof(FormFillBookViewer))
            //{
            //m_FormFillBook.FormClosing -= new FormClosingEventHandler(ChildForm_Closing);
            //m_FormFillBook = null;
            //}
            else if (formType == typeof(Ambre.TTServices.Fills.RejectedFills.FormRejectViewer))
            {
                //m_RejectedFillViewer.FormClosing -= new FormClosingEventHandler(ChildForm_Closing);
                //m_RejectedFillViewer = null;
            }
        }//ExternalForm_Closing()
        //
        #endregion // External Service Event Handlers


        #region Win32 SystemEvent handlers
        //
        //
        //
        // ****             SystemEvents_SessionEnding()                ****
        private void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
        {
            if (this.Log != null)
            {
                Log.NewEntry(LogLevel.Major, "Viewer: SessionEnding event received. Reason = {0}. ", e.Reason.ToString());
                Log.NewEntry(LogLevel.Major, "Viewer: Triggering request for nice shutdown now.");
                e.Cancel = true;
                Log.Flush();
                ShutDown();
            }

        }
        void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (this.Log != null)
            {
                Log.NewEntry(LogLevel.Major, "Viewer: SessionEnding event received. Reason = {0}. ", e.Reason.ToString());
                Log.NewEntry(LogLevel.Major, "Viewer: Triggering request for nice shutdown now.");
                Log.Flush();
                ShutDown();
            }
        }
        //
        //
        #endregion//system events


        #region Form Event Handlers
        // *****************************************************************
        // ****                 Form Event Handlers                     ****
        // *****************************************************************
        //
        //
        // ****             AmbreViewer_FormClosing()           ****
        //
        private void AmbreViewer_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!m_IsShuttingDown)
            {
                // First time called, cancel and try to shutdown slowly.
                e.Cancel = true;
                if (Log != null)
                    Log.NewEntry(LogLevel.Minor, "Viewer: Form closing.");

                ShutDown();
            }
        }
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
        //
        //
        // ****             TabControl_Selected()               ****
        //
        private void TabControl_Selected(object sender, TabControlEventArgs e)
        {
            if (e.TabPage is FillHubPage)            // The user has selected a specific tab in the control, update it.
            {
                FillHubPage page = (FillHubPage)e.TabPage;
                //page.Update();
                page.UpdatePage();
            }
        }
        //
        //
        //
        // ****             buttonExcelLink_Click()             ****
        // 
        private void buttonExcelLink_Click(object sender, EventArgs e)
        {
            // New version - clicking on light, tries to connect to all.
            lock (m_TalkerHubs)
            {
                foreach (TalkerHub talker in m_TalkerHubs.Values)
                    if (!talker.IsConnectedToClient)
                        talker.Request(TalkerHubRequest.AmberXLConnect);
            }
        }
        //
        //
        //
        // ****                 Menu Click()                    ****
        //
        private void Menu_Click(object sender, EventArgs eventArgs)
        {
            if (sender is ToolStripItem)
            {   //
                // Determine current active fillHub; that is, which tab is selected.
                //
                FillHub selectedFillHub = null;
                FillHubPage selectedFillHubPage = null;
                if (tabControl.SelectedTab != null && tabControl.SelectedTab is FillHubPage)
                {
                    selectedFillHubPage = (FillHubPage)tabControl.SelectedTab;
                    selectedFillHub = selectedFillHubPage.m_FillHub;
                }
                //
                // Determine menu item that was selected:
                //
                ToolStripItem tool = (ToolStripItem)sender;
                if (Log != null)
                {
                    if (selectedFillHub != null)
                        Log.NewEntry(LogLevel.Minor, "Viewer: Menu Click {0}.  Current tab {1}.  ", tool.Name, selectedFillHub.Name);
                    else
                        Log.NewEntry(LogLevel.Minor, "Viewer: Menu Click {0}.  Current tab is null.  ", tool.Name);
                }
                // ****             Reset PnL Dialog            *****
                if (tool == menuOpenPnLManager)
                {
                    if (Log != null)
                        Log.NewEntry(LogLevel.Minor, "The user clicked menuOpenPnLManager");

                    // Collect all the current fill hubs
                    List<FillHub> fillHubs = new List<FillHub>();
                    foreach (IService service in AppServices.GetInstance().GetServices(typeof(FillHub)))
                        fillHubs.Add((FillHub)service);
                    /*
                    Dialogs.FormResetPnL newForm; 
                    newForm = new Dialogs.FormResetPnL(fillHubs);                        
                    newForm.Show();
                    */
                    Dialogs.FormPnLTransferTool newForm = new Dialogs.FormPnLTransferTool(fillHubs);
                    newForm.Show();
                }
                // ****             Reset Daily PNL             ****
                else if (tool == menuResetDailyPnL)
                {
                    if (Log != null)
                        Log.NewEntry(LogLevel.Minor, "The user clicked menuResetDailyPnL");

                    // Reset the daily PnL for this book.  Rolls PnL into long-term PnL.
                    if (selectedFillHub != null)
                    {
                        Misty.Lib.OrderHubs.OrderHubRequest request = new Misty.Lib.OrderHubs.OrderHubRequest(Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestRealPnLReset);
                        selectedFillHub.Request(request);
                    }
                }
                else if (tool == menuItemArchiveBooks)
                {
                    if (Log != null)
                        Log.NewEntry(LogLevel.Minor, "The user clicked menuItemArchiveBooks");

                    foreach (IService service in AppServices.GetInstance().GetServices(typeof(FillHub)))
                        ((FillHub)service).Request(new Misty.Lib.OrderHubs.OrderHubRequest(Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestDropCopyArchive));

                }
                else if (tool == menuItemSaveLogs)
                {
                    if (Log != null)
                        Log.NewEntry(LogLevel.Minor, "The user clicked menuItemSaveLogs");

                    // Launch the service log copy to functions.
                    CentralizeLogsToServer();
                }
                else if (tool == menuUpdateConfig)
                {
                    if (Log != null)
                        Log.NewEntry(LogLevel.Minor, "The user clicked menuUpdateConfig");

                    if (!AppServices.GetInstance().TrySaveServicesToFile(UserConfigFileName) && Log != null)
                        Log.NewEntry(LogLevel.Major, "AmbreViewer: Failed to write config file.");
                }
                else if (tool == menuItemExit)
                {
                    if (Log != null)
                        Log.NewEntry(LogLevel.Minor, "The user clicked menuItemExit");

                    ShutDown();
                    this.Close();
                }
                else if (tool.Name.Contains(TalkerHub_NameBase)) // == menuItemLinkToExcel)
                {
                    if (Log != null)
                        Log.NewEntry(LogLevel.Minor, "The user clicked menuItemLinkToExcel");

                    // The menu clicked is a TalkerHub
                    string[] elems = tool.Name.Split(new string[] { TalkerHub_NameBase }, StringSplitOptions.RemoveEmptyEntries);
                    string id = elems[elems.Length - 1].Trim();
                    int n;
                    TalkerHub talker;
                    if (int.TryParse(id, out n) && m_TalkerHubs.TryGetValue(n, out talker))
                    {
                        if (!talker.IsConnectedToClient)
                            talker.Request(TalkerHubRequest.AmberXLConnect);
                        else
                            talker.Request(TalkerHubRequest.AmbreXLDisconnect);
                    }
                }
                else if (tool == menuItemNewTalkerHub)
                {
                    if (Log != null)
                        Log.NewEntry(LogLevel.Minor, "The user clicked menuItemNewTalkerHub");

                    // User wants to create another TalkerHub.                    
                    int nextPort = 6012;
                    if (m_TalkerHubs.Count > 0)
                    {
                        while (m_TalkerHubs.ContainsKey(nextPort))
                            nextPort++;
                    }
                    TalkerHub talkerHub = new TalkerHub(false, nextPort);
                    AppServices.GetInstance().TryAddService(talkerHub);
                    this.AddService(talkerHub);
                    // Write new config file.
                    if (!AppServices.GetInstance().TrySaveServicesToFile(UserConfigFileName) && Log != null)
                        Log.NewEntry(LogLevel.Major, "AmbreViewer: Failed to write config file.");
                }
                //
                // Open Log windows
                //
                else if (tool == menuItemMarketLog)
                {
                    if (Log != null)
                        Log.NewEntry(LogLevel.Minor, "The user clicked menuItemMarketLog");

                    foreach (IService service in AppServices.GetInstance().GetServices(typeof(Misty.Lib.MarketHubs.MarketHub)))
                    {
                        Misty.Lib.MarketHubs.MarketHub hub = (Misty.Lib.MarketHubs.MarketHub)service;
                        hub.Log.IsViewActive = true;
                    }
                }
                else if (tool == menuItemFillHubLog && selectedFillHub != null)
                {
                    if (Log != null)
                        Log.NewEntry(LogLevel.Minor, "The user clicked menuItemFillHubLog");
                    selectedFillHub.Log.IsViewActive = true;
                }
                else if (tool == menuViewTalkerLogs && m_TalkerHubs != null)
                {
                    if (Log != null)
                        Log.NewEntry(LogLevel.Minor, "The user clicked menuViewTalkerLogs");

                    lock (m_TalkerHubs)
                    {
                        foreach (TalkerHub talker in m_TalkerHubs.Values)
                            talker.Log.IsViewActive = true;
                    }
                }
                else if (tool == menuItemRejectedFills)
                {
                    if (Log != null)
                        Log.NewEntry(LogLevel.Minor, "The user clicked menuItemRejectedFills");

                    FillHubPage selectedPage = null;
                    if (tabControl.SelectedTab != null && tabControl.SelectedTab is FillHubPage)
                        selectedPage = (FillHubPage)tabControl.SelectedTab;
                    if (m_RejectedFillViewer == null && selectedPage != null)
                    {
                        m_RejectedFillViewer = new TTServices.Fills.RejectedFills.FormRejectViewer(selectedPage.m_FillHub);
                        m_RejectedFillViewer.Show();
                        m_RejectedFillViewer.FormClosing += new FormClosingEventHandler(ChildForm_Closing);
                        m_RejectedFillViewer.UpdateNow(selectedPage.m_FillHub);
                    }
                    else
                        m_RejectedFillViewer.Focus();
                }
                else if (tool == menuItemCreateCashInstrument)
                {
                    if (Log != null)
                        Log.NewEntry(LogLevel.Minor, "The user clicked menuItemCreateCashInstrument");

                    FillHubPage selectedPage = null;
                    if (tabControl.SelectedTab != null && tabControl.SelectedTab is FillHubPage)
                    {
                        selectedPage = (FillHubPage)tabControl.SelectedTab;
                        FillHub fillHub = selectedPage.m_FillHub;

                        string inputCurrencyName = string.Empty;

                        while (string.IsNullOrEmpty(inputCurrencyName))
                        {
                            inputCurrencyName = Microsoft.VisualBasic.Interaction.InputBox("Please input the currency name for the new cash instrument. Input empty string to exit",
                                "Request new cash instrument name", "USD", 450, 450);

                            if (inputCurrencyName == string.Empty)
                                return;
                        }

                        DialogResult result = MessageBox.Show(string.Format("I am going to create a new cash instrument with currency code of {0}, Ok?",
                        inputCurrencyName), "Create new cash instrument confirmation", MessageBoxButtons.YesNo);

                        if (result == System.Windows.Forms.DialogResult.Yes)
                        {
                            Product product = new Product("Cash", inputCurrencyName, ProductTypes.Cash, "");
                            InstrumentName newCashInstr = new InstrumentName(product, "");

                            Misty.Lib.OrderHubs.OrderHubRequest cashBookCreateRequest = new Misty.Lib.OrderHubs.OrderHubRequest(Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestCreateUpdateCashBook);
                            cashBookCreateRequest.Data = new object[5] {
                            newCashInstr, 
                            inputCurrencyName,
                            1.0,
                            0.0, 
                            0.0 };
                            fillHub.Request(cashBookCreateRequest);
                        }
                    }
                }
                else if (tool == menuNewFillHub)
                {
                    if (Log != null)
                        Log.NewEntry(LogLevel.Minor, "The user clicked menuNewFillHub");

                    // User wants to create a new fill hub.
                    Dialogs.FormNewFillHub newForm = new Dialogs.FormNewFillHub(this);
                    newForm.Show();
                }
                else if (tool == menuDeleteFillManager)
                {
                    if (Log != null)
                        Log.NewEntry(LogLevel.Minor, "The user clicked menuDeleteFillManager");
                    //
                    // Destroy FillHub
                    //
                    //menuWindows = null;               // What was this doing here?!?!
                    if (selectedFillHubPage == null)    // user must have selected a tab to delete.
                        return;
                    DialogResult result = MessageBox.Show(string.Format("Destroy {0}?  Are you sure?", selectedFillHubPage.Text), "Destroy fill manager", MessageBoxButtons.OKCancel);
                    if (result == System.Windows.Forms.DialogResult.OK)
                    {   // Destroy it! Shutdown the service.
                        if (selectedFillHubPage == null)
                            return;
                        tabControl.TabPages.Remove(selectedFillHubPage);    // removes visible tab page from gui
                        selectedFillHubPage.DeleteHub();                    // sends position delete, final drop file requests.
                        // Update the our application services
                        AppServices appService = AppServices.GetInstance();
                        appService.TryShutdownService(selectedFillHub.ServiceName);
                        if (!appService.TrySaveServicesToFile(UserConfigFileName) && Log != null)
                            Log.NewEntry(LogLevel.Major, "AmbreViewer: Failed to write config file.");
                    }
                }
                else if (tool == menuDeleteBrettTalker)
                {
                    if (Log != null)
                        Log.NewEntry(LogLevel.Minor, "The user clicked menuDeleteBrettTalker");
                    //
                    // Delete Brett Talker
                    //
                    AppServices appService = AppServices.GetInstance();
                    string serviceNameToDelete = string.Empty;
                    foreach (IService service in appService.GetServices(typeof(TalkerHub)))
                    {
                        if (string.IsNullOrEmpty(serviceNameToDelete) || serviceNameToDelete.CompareTo(service.ServiceName) < 0)
                            serviceNameToDelete = service.ServiceName;
                    }
                    // Update the our application services
                    IService serviceToDelete = null;
                    if (string.IsNullOrEmpty(serviceNameToDelete) || !appService.TryGetService(serviceNameToDelete, out serviceToDelete))
                        return; // Can not remove this service.
                    if (appService.TryShutdownService(serviceNameToDelete))
                    {
                        if (!appService.TrySaveServicesToFile(UserConfigFileName) && Log != null)
                            Log.NewEntry(LogLevel.Major, "AmbreViewer: Failed to write config file.");
                        this.RemoveService(serviceToDelete);// Shutdown service.
                    }
                }
                else
                {
                    if (Log != null)
                        Log.NewEntry(LogLevel.Warning, "Viewer: Unknown menu clicked {0}.", tool.Name);
                }
            }// if ToolStripItem
        }
        //
        //
        //
        // ****                 Form1_DragDrop()                ****
        /// <summary>
        /// Allows user to drag a instrument from any TT window and drop it onto this form.
        /// We then submit a zero-qty fill into the FillHub, which in turn will collect all 
        /// the necessary information about the instrument, and ultimately inform this form 
        /// to add the contract.
        /// </summary>
        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.HasInstrumentKeys())
            {
                foreach (TradingTechnologies.TTAPI.InstrumentKey key in e.Data.GetInstrumentKeys())                       // Loop thru each instr dropped.
                {
                    TabPage currentPage = tabControl.SelectedTab;
                    if (currentPage != null && currentPage is FillHubPage)
                    {
                        FillHubPage fillHubPage = (FillHubPage)currentPage;
                        Log.NewEntry(Misty.Lib.Hubs.LogLevel.Major, "Viewer: Detected dropped instrument {0} onto tabPage {1}.", key.ToString(), currentPage.Name);
                        // Determine if this is a new (unknown) instrument. It is unknown if:
                        // 1) MarketHub doesn't recognize the key.
                        // 2) MarketHub knows the key, but we have no position book yet for this key.
                        InstrumentName instrumentName;
                        if (!fillHubPage.m_FillHub.MarketHub.TryLookupInstrument(key, out instrumentName)
                            //|| !fillHubPage.m_InstrumentNames.ContainsValue(instrumentName))
                            || !fillHubPage.m_InstrumentInfos.ContainsKey(instrumentName.FullName))
                        {   // To create a fill book for this instrument, pretend we were filled with a zero qty.
                            Misty.Lib.OrderHubs.Fill aFill = Misty.Lib.OrderHubs.Fill.Create();
                            aFill.Price = 0.0;
                            aFill.Qty = 0;
                            aFill.LocalTime = Log.GetTime();
                            FillEventArgs fillEventArgs = new FillEventArgs(key, FillType.UserAdjustment, aFill);
                            fillHubPage.m_FillHub.HubEventEnqueue(fillEventArgs);
                        }
                    }
                }//next instrumentKey
            }
        }
        //
        // ****                 Form1_DragOver()                ****
        /// <summary>
        /// Show dragover effects to let user know we will respond to his drop.
        /// </summary>
        private void Form1_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.HasInstrumentKeys())
                e.Effect = DragDropEffects.Copy;
        }

        private void AmbreViewer_FormClosed(object sender, FormClosedEventArgs e)
        {
        }
        //
        //
        //
        #endregion//Form Event Handlers

    }
}
