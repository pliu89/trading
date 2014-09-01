using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;



namespace Ambre.TTServices
{
    using Misty.Lib.Application;
    using Misty.Lib.Hubs;
    using Misty.Lib.IO.Xml;
    using TradingTechnologies.TTAPI;

    /// <summary>
    /// TT API wrapper service.
    /// TODO:
    /// 1. Need to explore/implement disconnection/reconnect from XTrader when users logout.
    ///     Difficulty is probably how Market/Order/Strategy hubs react...  Need to react appropriatedly
    ///     to a state change here of "NotConnected"
    /// </summary>
    public class TTApiService : IDisposable, IStringifiable, IService
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // Service objects
        //
        private readonly static TTApiService m_Instance = new TTApiService();   // thread-safe singleton model
        private LogHub m_Log = null;                                        // a log hub dedicated to this object
        private bool m_IsDisposed = false;                                  // flag indicates whether we are in shutdown process.
        private object m_DisposeLock = new object();

        //
        // TT Api connections
        //
        private UniversalLoginTTAPI m_UAPI = null;                          // universal api instance - public for debugging only        
        private XTraderModeTTAPI m_XAPI = null;                             // xtrader api.
        private bool m_UseXTraderLogin = true;                             // flag to follow the local XTrader login.        
        public WorkerDispatcher m_Dispatcher = null;                        // Dispatcher for API worker thread created here.        
        
        public Session session = null;                                      // place to store either type of session.

        // Internal controls
        private bool m_Stopping = false;
        private bool m_CheckXTraderProcessExists = true;                    // controls Xtrader search mode.
        private int m_WaitForAPISeconds = 30;                               // time to wait between checking API.
        //
        //
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        private TTApiService()                                              // External callers must use GetInstance() static method
        {
        }
        public static TTApiService GetInstance()
        {
            return m_Instance;
        }
        public LogHub Log
        {
            get { return m_Log; }
            set { m_Log = value; }
        }
        //
        //
        // ****                     Start()                     ****
        //
        /// <summary>
        /// Begins asynchronous call to connect to TT API.  
        /// AppInfo.LogPath MUST be properly set to the Log path before calling this method.
        /// This method can be called by any thread.
        /// </summary>
        public void Start(bool followXTraderLogin)
        {
            m_UseXTraderLogin = followXTraderLogin;
            this.Start();
        }
        /// <summary>
        /// This is called by an external thread.  It creates a Thread.WorkerThread and 
        /// calls StartInitAPI().  Here, a dispatcher for the worker is created and the 
        /// first Action (a call to InitAPI()) is queued and run.
        /// </summary>
        public void Start()
        {
            bool isLogViewerVisible = false;
            //#if DEBUG 
            //    isLogViewerVisible = true;
            //#endif

            if (m_Log == null)
                m_Log = new LogHub("TT API Services", Misty.Lib.Application.AppInfo.GetInstance().LogPath, isLogViewerVisible, LogLevel.ShowAllMessages);
            if (m_Instance == null)
            {
                if (m_Log != null)
                    m_Log.NewEntry(LogLevel.Error, "Start: No API instantiated.  Cannot Start(). ");
                return;
            }
            if (m_IsDisposed)
            {
                if (m_Log != null)
                    m_Log.NewEntry(LogLevel.Error, "Start: This TTService is already disposed.  Cannot Start(). ");
                return;
            }
            Thread workerThread = new Thread(m_Instance.StartInitAPI);  // Create a thread to run the API.
            workerThread.Name = "TT API Service";
            workerThread.Start();                                       // Start new thread and exit, releases the calling thread!
        }// Start()
        public void Connect()
        {
        }
        public void RequestStop()
        {
            if (m_Log != null)
                m_Log.RequestStop();
            m_Stopping = true;
            // TODO: How to disconnect from TTApi?
            this.Dispose();
        }
        /// <summary>
        /// This is called asynchronously by the TTService worker thread.  It then creates and
        /// attaches a Dispatcher for itself, and calls the InitAPI() method.  This creates a 
        /// queue for the TTService worker thread, which keeps it alive after processing requests.
        /// </summary>
        private void StartInitAPI()
        {
            if ( m_Dispatcher == null )
                m_Dispatcher = Dispatcher.AttachWorkerDispatcher();         
            m_Dispatcher.BeginInvoke(new Action(InitAPI));
            try
            {   // this is a blocking statement, the thread will return here whenever a call is returned.
                m_Dispatcher.Run();   // This keeps the thread alive at the end of a return.
            }
            catch (Exception e)
            {
                if (m_Log != null)
                    m_Log.NewEntry(LogLevel.Error, "StartInitAPI: Error. Failed to start TT API. {0}", e.Message);
                Thread.Sleep(2000);             // worker should sleep, then try to restart.
                StartInitAPI();                 // try again.
            }
        }// StartInitAPI().
        //
        /// <summary>
        /// Create UniversalLogin or XTrader API instance.
        /// Called by the TTService worker thread.
        /// </summary>
        private void InitAPI()
        {
            if (m_UseXTraderLogin)
            {   // Follow the local XTrader login.
                // Note: This will not fail even if no XTrader is running.
                // Rather, the call m_XAPI.ConnectToXTrader(), below, will get stuck forever if there is not XTrader running.
                //System.Windows.Forms.MessageBox.Show("test TT1");
                // Check for Xtrader process, postpone connecting if its NOT running.
                //System.Windows.Forms.MessageBox.Show("test5");
                System.Diagnostics.Process[] procs = null;
                try
                {
                    if (m_CheckXTraderProcessExists)
                        procs = System.Diagnostics.Process.GetProcessesByName("x_trader");
                }
                catch (Exception e)
                {
                    if (m_Log != null)
                        m_Log.NewEntry(LogLevel.Warning, "InitAPI: Exception {0}.  Turning off XTrader searching.  Will try to start API directly.",e.Message);
                    m_CheckXTraderProcessExists = false;                    // Ok, security issues may have caused this to fail.  Nothing to do be proceed.
                }
                if (m_CheckXTraderProcessExists && procs != null && procs.Length < 1)
                {   // There is NO Xtrader running.
                    if (m_Log != null)
                        m_Log.NewEntry(LogLevel.Warning, "InitAPI: Found NO XTrader process. Waiting {0} secs to look again.", m_WaitForAPISeconds);
                    Thread.Sleep(1000 * m_WaitForAPISeconds);
                    try
                    {
                        if (! m_Stopping && m_Dispatcher != null)
                            m_Dispatcher.BeginInvoke(new Action(InitAPI));
                    }
                    catch (Exception e)
                    {   // This exception can happen when we are shutting down, before we connect.
                        m_Log.NewEntry(LogLevel.Warning, "InitAPI: Exception when re-invoking. Exiting. Exception = {0}.", e.Message);
                    }
                }
                else
                {   // XTrader is running, connect to it.
                    if (m_Log != null)
                        m_Log.NewEntry(LogLevel.Warning, "InitAPI: Creating XTrader mode API.");
                    TTAPI.XTraderModeDelegate d = new TTAPI.XTraderModeDelegate(TT_XTraderInitComplete);
                    XTraderModeTTAPIOptions options = new XTraderModeTTAPIOptions();
                    options.XTServicesConnectionTimeout = new TimeSpan(0, 0, 20);
                    try
                    {
                        TTAPI.CreateXTraderModeTTAPI(m_Dispatcher, options, d);
                    }
                    catch (Exception e)
                    {
                        if (m_Log != null)
                            m_Log.NewEntry(LogLevel.Warning, "InitAPI: TT Exception {0}.", e.Message);
                    }
                }
            }
            else
            {   // Use "Universal Login" Login Mode
                TTAPI.UniversalLoginModeDelegate ulDelegate = new TTAPI.UniversalLoginModeDelegate(TT_UniversalInitComplete);   // Call back after API is created.
                try
                {
                    TTAPI.CreateUniversalLoginTTAPI(m_Dispatcher, ulDelegate);
                }
                catch (Exception e)
                {
                    if (m_Log != null)
                        m_Log.NewEntry(LogLevel.Warning, "InitAPI: TT Exception {0}.", e.Message);
                }
            }
            //System.Windows.Forms.MessageBox.Show("test TT2");
        }// InitAPI()
        //
        //
        #endregion//constructors


        #region Destructors
        // *****************************************************************
        // ****                     Destructors                         ****
        // *****************************************************************
        //
        //
        //   
        // ****             Dispose()               ****
        //
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);            
        }//Dispose()
        protected virtual void Dispose(bool disposing)
        {
            lock (m_DisposeLock)
            {
                if (!m_IsDisposed)
                {
                    if (disposing)
                    {
                        if (m_Log != null)
                        {
                            m_Log.RequestStop();
                            m_Log = null;
                        }
                        if (m_Dispatcher != null)
                        {
                            m_Dispatcher.BeginInvokeShutdown();
                            m_Dispatcher = null;
                        }
                        if (m_UAPI != null)
                        {
                            m_UAPI.Shutdown();
                            m_UAPI = null;
                        }
                        if (m_XAPI != null)
                        {
                            m_XAPI.Shutdown();
                            m_XAPI = null;
                        }
                    }
                }
                m_IsDisposed = true;
                OnStopping();
            }
        }//Dispose()
        //
        //       
        #endregion//Destructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        public bool IsRunning
        {
            get
            {
                if (m_UAPI == null && m_XAPI == null)          // confirm existance of api
                {
                    if (m_Log!=null) 
                        m_Log.NewEntry(LogLevel.Major, "IsRunning: Called by {0}. No API instance found.",Thread.CurrentThread.Name);
                    return false;
                }
                if (session == null)
                {
                    if (m_Log!=null)
                        m_Log.NewEntry(LogLevel.Major, "IsRunning:  Called by {0}. No TT session instance found.", Thread.CurrentThread.Name);
                    return false;
                }
                if (string.IsNullOrEmpty(session.UserName))
                {
                    if (m_Log != null)
                        m_Log.NewEntry(LogLevel.Major, "IsRunning:  Called by {0}. No TT User login found.", Thread.CurrentThread.Name);
                    return false;
                }
                return true;                        // successful exit.
            }
        }//IsRunning
        //
        public string LoginUserName
        {
            get
            {
                if (session != null)
                    return session.UserName;
                else
                    return string.Empty;
            }
        }// LoginUserName
        //
        public string ServiceName
        {
            get { return "TTApi"; }
        }
        //
        #endregion//Properties


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


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        //
        //
        // *****************************************************************
        // ****               XTraderLoginTimerCallback()               ****
        // *****************************************************************
        /// <summary>
        /// If this application is started before XTrader, and we have specified
        /// a follow XTrader login mode, then we may have to wait until he 
        /// completes his login.
        /// </summary>
        private void XTraderLoginTimerCallback(object sender, System.Timers.ElapsedEventArgs e)
        {
            m_XTraderLoginTimer.Enabled = false;
            if (string.IsNullOrEmpty(m_XAPI.Session.UserName))
            {   // No user. Wait more.
                if (m_Log != null )
                    m_Log.NewEntry(LogLevel.Major, "XTraderLoginTimerCallback: Still waiting for user login.");
                m_XTraderLoginTimer.Enabled = true;
            }
            else if (m_XTraderLoginWaitMore)
            {
                double secondsToWaitMore = 5.0;
                if (m_Log != null)
                    m_Log.NewEntry(LogLevel.Major, "XTraderLoginTimerCallback: Detected User: {0}.  Will wait another {1:0.0} seconds to login.", m_XAPI.Session.UserName, secondsToWaitMore);
                m_XTraderLoginWaitMore = false;
                m_XTraderLoginTimer.Interval = secondsToWaitMore * 1000.0;
                m_XTraderLoginTimer.Enabled = true;                
            }
            else
            {
                if (m_Log != null)
                    m_Log.NewEntry(LogLevel.Major, "XTraderLoginTimerCallback: Will invoke ConnectToXTrader.");
                m_XTraderLoginTimer.Elapsed -= XTraderLoginTimerCallback;
                m_XTraderLoginTimer = null;
                m_Dispatcher.BeginInvoke(new Action(m_XAPI.ConnectToXTrader));
            }
        }//XTraderLoginTimerCallback()
        private System.Timers.Timer m_XTraderLoginTimer = null;
        private bool m_XTraderLoginWaitMore = true;         // makes us wait extra to login to XTrader.
        //
        //
        //
        #endregion//Private Methods


        #region API and Session Callbacks
        // *********************************************************************
        // ****             TT Universal Init Complete()                    ****
        // *********************************************************************
        /// <summary>
        /// Callback once Universal TTAPI instance is created, it is passed back to us here.
        /// </summary>
        private void TT_UniversalInitComplete(UniversalLoginTTAPI api, Exception ex)
        {
            if (ex == null)
            {
                m_UAPI = api;                                // API instance we created asynchronously.
                m_Log.NewEntry(LogLevel.Major,"UniversalInitComplete: API universal initialization is successful.");
                m_UAPI.AuthenticationStatusUpdate += new EventHandler<AuthenticationStatusUpdateEventArgs>(TT_AuthenticationStatusUpdate);
                m_UAPI.LicenseIssue += new EventHandler<LicenseIssueEventArgs>(TT_LicenseIssueEventHandler);

                // User login - can move this in future.
                if (m_Log != null)
                    m_Log.NewEntry(LogLevel.Major, "UniversalInitComplete: Requesting login authentication. ");
                m_UAPI.Authenticate("DVSIM8", "ABC123");                
            }
            else
                m_Log.NewEntry(LogLevel.Error, "UniversalInitComplete: API universal initialization failed: {0}", ex.Message);
        }// TT_UniversalInitComplete()
        //
        // *********************************************************************
        // ****             TT Authentication Status Update                 ****
        // *********************************************************************
        /// <summary>
        /// Callback for user login authentication for the Universal login.
        /// </summary>
        private void TT_AuthenticationStatusUpdate(object sender, AuthenticationStatusUpdateEventArgs eventArg)
        {
            if (eventArg.Status.IsSuccess)
            {
                this.session = m_UAPI.Session;              // store session ptr.
                if (m_Log != null)
                    m_Log.NewEntry(LogLevel.Major, "AuthenticationStatusUpdate: Login authentication is successful. ");                
                this.session.AdminMessage += new EventHandler<AdminMessageEventArgs>(Session_AdminMessage);               
            }
            else if (m_Log != null)
                m_Log.NewEntry(LogLevel.Error, "AuthenticationStatusUpdate: Login authentication failed: " + eventArg.Status.StatusMessage);
            OnServiceStatusChanged(new ServiceStatusChangeEventArgs(eventArg.Status.IsSuccess));
        }// TT_AuthenticationStatusUpdate()
        //
        // *********************************************************************
        // ****                 TT XTrader Init Complete                    ****
        // *********************************************************************
        private void TT_XTraderInitComplete(XTraderModeTTAPI api, Exception ex)
        {
            if (ex == null)
            {
                m_XAPI = api;
                if (m_Log != null)
                    m_Log.NewEntry(LogLevel.Major, "XTraderInitComplete: API Xtrader initialization is successful.");
                m_XAPI.ConnectionStatusUpdate += new EventHandler<ConnectionStatusUpdateEventArgs>(TT_ConnectionStatusUpdate);
                m_XAPI.LicenseIssue += new EventHandler<LicenseIssueEventArgs>(TT_LicenseIssueEventHandler);
                m_XAPI.XTraderStatusChanged += new EventHandler<TradingTechnologies.TTAPI.XTInteraction.XTraderStatusChangedEventArgs>(TT_XTraderStatusChanged);

                if (string.IsNullOrEmpty(m_XAPI.Session.UserName))
                {   // There is no user logged into the x-trader session.
                    // We will need to wait until one shows up.
                    if (m_XTraderLoginTimer == null)
                    {
                        double waitTimeMinutes = 0.20;
                        if (m_Log != null)
                            m_Log.NewEntry(LogLevel.Major, "XTraderInitComplete: No user logged into XTrader yet. Will check again in {0:0.0} minutes.",waitTimeMinutes);
                        m_XTraderLoginTimer = new System.Timers.Timer();
                        m_XTraderLoginTimer.AutoReset = false;
                        m_XTraderLoginTimer.Elapsed += XTraderLoginTimerCallback;
                        m_XTraderLoginTimer.Interval = waitTimeMinutes * (60000.0);    // minutes * (msecs/min)
                        m_XTraderLoginTimer.Enabled = true;
                        return;
                    }
                }
                else
                    m_XAPI.ConnectToXTrader();
            }
            else
            {
                if (m_Log != null)
                    m_Log.NewEntry(LogLevel.Error, "XTraderInitComplete: Xtrader initialization failed. Exception {0}. Will try again.", ex.Message);
                Thread.Sleep(10000);
                m_Dispatcher.BeginInvoke(new Action(m_XAPI.ConnectToXTrader));
            }
        }//TT_XTraderInitComplete()
        //
        // *********************************************************************
        // ****                 TT_ConnectionStatusUpdate                   ****
        // *********************************************************************
        private void TT_ConnectionStatusUpdate(object sender, ConnectionStatusUpdateEventArgs eventArg)
        {
            if (eventArg.Status.IsSuccess)
            {
                this.session = m_XAPI.Session;
                this.session.AdminMessage += new EventHandler<AdminMessageEventArgs>(Session_AdminMessage);
                if (m_Log != null)
                    m_Log.NewEntry(LogLevel.Major, "ConnectionStatusUpdate: Login connection successful.");
                OnServiceStatusChanged(new ServiceStatusChangeEventArgs(eventArg.Status.IsSuccess));
            }
            else
            {
                if (m_Log != null)
                    m_Log.NewEntry(LogLevel.Error, "ConnectionStatusUpdate: Login connection for XTrader failed: " + eventArg.Status.StatusMessage);
            }
        }//TT_ConnectionStatusUpdate()
        //
        // *********************************************************************
        // ****                 TT_XTraderStatusChanged                     ****
        // *********************************************************************
        private void TT_XTraderStatusChanged(object sender, TradingTechnologies.TTAPI.XTInteraction.XTraderStatusChangedEventArgs eventArgs)
        {
            // TODO: Consider how to recover from an XTrader shutdown.
        }// TT_XTraderStatusChanged()
        //
        // *********************************************************************
        // ****                 TT License Issue EventHandler               ****
        // *********************************************************************
        /// <summary>
        /// API-wide level callback from TT with there is a licensing issue detected.
        /// </summary>
        private void TT_LicenseIssueEventHandler(object sender, LicenseIssueEventArgs eventArg)
        {
            if (m_Log != null)
                m_Log.NewEntry(LogLevel.Error, "LicenseIssueEventHandler: TT API license issue detected from gateway {1}. {0}.", eventArg.Message, eventArg.GatewayKey.Name);
            //OnServiceStatusChanged(new ServiceStatusChangeEventArgs());
        }//TT_LicenseIssueEventHandler()
        //
        // *********************************************************************
        // ****                 Session_AdminMessage                        ****
        // *********************************************************************
        private void Session_AdminMessage(object sender, AdminMessageEventArgs eventArg)
        {
            if (m_Log != null)
                m_Log.NewEntry(LogLevel.Major, "AdminMessage: {0}", eventArg.Message);
            //OnServiceStatusChanged(new ServiceStatusChangeEventArgs());
        }//Session_AdminMessage()
        //
        #endregion// Event Handlers Methods


        #region Public Events and Event Args
        // *****************************************************************
        // ****                     Public Event                        ****
        // *****************************************************************
        //
        public event EventHandler ServiceStateChanged;                 // fired for highlevel events: Login, admin events, session events, etc.
        //
        private void OnServiceStatusChanged(ServiceStatusChangeEventArgs e)
        {
            if (this.ServiceStateChanged != null)
                ServiceStateChanged(this, e);
        }
        public event EventHandler Stopping;
        //
        protected void OnStopping()                                     // fired when stopping.
        {
            if (this.Stopping != null)
                Stopping(this, EventArgs.Empty);
        }
        //
        //
        public class ServiceStatusChangeEventArgs : EventArgs
        {
            // Variables
            public bool IsConnected = true;                                // if connected to either XTrader or Universal login.

            public ServiceStatusChangeEventArgs(bool isConnected) { this.IsConnected = isConnected; }
            public ServiceStatusChangeEventArgs() {}
            
        }
        //
        //
        #endregion//Public Events


        #region IStringifiable interface
        public string GetAttributes()
        {
            StringBuilder msg = new StringBuilder();
            if (this.m_UseXTraderLogin)
                msg.AppendFormat("FollowXTrader={0}", this.m_UseXTraderLogin);

            // Exit
            return msg.ToString();            
        }
        public List<IStringifiable> GetElements()
        {
            return null;
        }
        public void SetAttributes(Dictionary<string, string> attributes)
        {
            bool isTrue;
            foreach (string key in attributes.Keys)
            {
                if (key.Equals("FollowXTrader") && bool.TryParse(attributes[key], out isTrue))
                    m_UseXTraderLogin = isTrue;
            }         
        }
        public void AddSubElement(IStringifiable subElement)
        {
        }
        #endregion // IStringifiable interface
    }
}
