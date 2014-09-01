using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.Windows.Threading;
using System.Windows.Forms;

namespace UV.Lib.Application
{
    using UV.Lib.IO.Xml;
    using UV.Lib.Hubs;
    using UV.Lib.MarketHubs;
    //using UV.Lib.OrderHubs;

    using UV.Lib.FrontEnds.Utilities;           // GuiCreator
    
    /// <summary>
    /// The first caller to GetInstance() in this class must be the UI thread.
    /// If AppServices are to be used, then this will create an AppInfo object.
    /// Example Usage:
    ///     typeof(Ambre.TTServices.Markets.MarketTTAPI).ToString();            // force needed assemblies to load.
    ///     AppServices appServices = AppServices.GetInstance("appName", true); // Set application information - do this before hubs are instantiated.
    ///     appServices.Info.RequestShutdownAddHandler(new EventHandler(this.RequestShutdownHandler)); // let main form know about shutdown requests
    ///     appServices.LoadServicesFromFile("ConfigFile.txt");                 // Stringifications for all services.
    ///
    ///     appServices.Start();                                                // Start thread hubs.
    ///     appServices.Connect();                                              // Start connection to external world, like APIs etc.
    /// </summary>
    public class AppServices
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // Static objects
        //
        private static readonly AppServices m_AppServicesInstance = new AppServices();  // create this unique object.
        public readonly AppInfo Info = null;                                            // pointer to the unique AppInfo object.

        public readonly Dispatcher UiDispatcher;                        // the Ui dispatcher for this application.
        public readonly GuiCreator GuiCreator = null;                   // a single reference to GuiCreator

        //
        // Services
        //        
        private object m_ServiceLock = new object();
        private List<string> m_ServiceNames = new List<string>();
        private List<IService> m_Services = new List<IService>();

        //
        // Run-time user information
        //
        public UserInfo User = new UserInfo();
        public System.Drawing.Icon AppIcon = null;


        //
        // Internal controls
        //
        private bool m_IsShuttingDown = false;                          // ensures we don't try to release resources more than once.


        #endregion// members


        #region Constructors & Creators 
        // *****************************************************************
        // ****               Constructors & Creators                   ****
        // *****************************************************************
        /// <summary>
        /// The constructor must be called by the UI thread. Therefore, the UI thread
        /// must call GetInstance() creator the first time.  Afterwards, any thread can
        /// can call GetInstance().
        /// Alternative: use PresentationFramework Application.Current.Dispatcher?
        /// </summary>
        private AppServices()
        {   // Creates the singleton (default) objects right now, albeit trivial, default objects.
            this.Info = AppInfo.GetInstance();          // Creating the sole instance of an default AppInfoInstance.
            this.Info.RequestShutdownAddHandler(new EventHandler(this.RequestShutdownHandler));   

            UiDispatcher = Dispatcher.CurrentDispatcher;
            this.GuiCreator = GuiCreator.Create();
        }
        //
        //
        // *************************************************
        // ****				GetInstance()				****
        // *************************************************
        /// <summary>
        /// If AppServices is to be used, it must be called before AppInfo.
        /// </summary>
        /// <param name="applicationName"></param>
        /// <param name="isExactDirName"></param>
        /// <returns></returns>
        public static AppServices GetInstance(string applicationName, bool isExactDirName = true)
        {
            AppInfo.GetInstance(applicationName, isExactDirName);       // This initializes AppInfo the first time we call it.
            AppServices appServices = m_AppServicesInstance;            // singleton of this object.
            // TODO: Test if appServices is initialized, if not initialize it here.
            return appServices;
        }
        // *************************************************
        // ****             GetInstance()               ****
        // *************************************************
        /// <summary>
        /// This is the usual overloading that is called once the application
        /// has started.
        /// </summary>
        /// <returns></returns>
        public static AppServices GetInstance()
        {
            return m_AppServicesInstance;
        }
        //
        // *************************************************
        // ****             TryGetInstance()            ****
        // *************************************************
        /// <summary>
        /// New approach creates AppServices instance, and confirms that only ONE instance
        /// of this application is running.
        /// </summary>
        /// <returns></returns>
        public static bool TryCreateSoloInstance()
        {
            string name = System.Windows.Forms.Application.ProductName;
            // Confirm that there is only one process running.
            System.Diagnostics.Process[] procs = null;
            try
            {
                procs = System.Diagnostics.Process.GetProcessesByName(name);
            }
            catch (Exception)
            {
                procs = null;
            }
            System.Windows.Forms.DialogResult result = DialogResult.OK;
            if (procs != null && procs.Length > 1)
            {
                result = System.Windows.Forms.MessageBox.Show("Detecting another instance. Continue?", name, MessageBoxButtons.OKCancel);
            }
            // Exit
            if (result == DialogResult.OK)
            {
                AppServices appServices = AppServices.GetInstance(name);
                return true;
            }
            else
                return false;
        }// TryGetInstance()
        //
        //
        //       
        #endregion//Constructors & creators 


        #region no Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        #endregion//Properties


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //
        // *********************************************************
        // ****                 TryAddService()                 ****
        // *********************************************************
        /// <summary>
        /// Tries to add a service object to the appropriate service list.
        /// Will ensure the name is unique.
        /// </summary>
        /// <param name="serviceObject"></param>
        /// <returns></returns>
        public bool TryAddService(object serviceObject)
        {
            bool isServiceAdded = false;
            if (serviceObject is IService)
            {
                IService service = (IService)serviceObject;
                lock (m_ServiceLock)
                {
                    if (m_ServiceNames.Contains(service.ServiceName))
                        isServiceAdded = false;                 // Services must have unique name.
                    else
                    {
                        m_ServiceNames.Add(service.ServiceName);
                        m_Services.Add(service);
                        isServiceAdded = true;
                    }
                }//lock
                // Trigger the service added
                if (isServiceAdded)
                    OnServiceAdded(service);
            }
            else if (serviceObject is UserInfo)
                this.User = (UserInfo)serviceObject;                    // user info provided by user

            // Exit
            return isServiceAdded;
        }// TryAddService()
        //
        //
        //
        // *********************************************************
        // ****               TryShutdownService()              ****
        // *********************************************************
        /// <summary>
        /// Attempt to remove a specific service.  If found, its immediately removed from 
        /// the service list, and requested to stop. Its immediate removal guarantees that
        /// we can immediately save the config file for the Services.
        /// </summary>
        /// <param name="serviceNameToRemove"></param>
        /// <returns></returns>
        public bool TryShutdownService(string serviceNameToRemove)
        {            
            if (string.IsNullOrEmpty(serviceNameToRemove))
                return false;
            bool isSuccess = false;
            lock (m_ServiceLock)
            {
                if (m_ServiceNames.Contains(serviceNameToRemove) )
                {
                    int ptr = m_ServiceNames.IndexOf(serviceNameToRemove);
                    if (ptr > -1)
                    {
                        IService service = m_Services[ptr];
                        m_ServiceNames.RemoveAt(ptr);           // Remove from name list.
                        m_Services.RemoveAt(ptr);               // Remove from service list.
                        
                        service.Stopping += new EventHandler(Service_Stopping);
                        service.RequestStop();
                        isSuccess = true;
                    }
                }
            }
            return isSuccess;
        }// TryRemoveService.
        //
        //
        //
        // *************************************************************
        // ****                 LoadServicesFromFile()              ****
        // *************************************************************
        /// <summary>
        /// Creates all services according to XML in config file.
        /// </summary>
        /// <param name="configFileName"></param>
        public bool TryLoadServicesFromFile(string configFileName)
        {
            try
            {
                string filePath = string.Format("{0}{1}", this.Info.UserConfigPath, configFileName);
                List<UV.Lib.IO.Xml.IStringifiable> iStringObjects;
                using (UV.Lib.IO.Xml.StringifiableReader reader = new IO.Xml.StringifiableReader(filePath))
                {
                    iStringObjects = reader.ReadToEnd();
                }
                foreach (UV.Lib.IO.Xml.IStringifiable iStrObj in iStringObjects)
                    TryAddService(iStrObj);
            }
            catch (Exception e)
            {
                StringBuilder msg = new StringBuilder();
                msg.AppendFormat("Exception: {0}\r\nContinue?", e.Message);
                System.Windows.Forms.DialogResult result = System.Windows.Forms.MessageBox.Show(msg.ToString(), "AppServices.TryLoadSerivesFromFile",System.Windows.Forms.MessageBoxButtons.OKCancel);
                // TODO: Shutdown ourselves?
                return result == System.Windows.Forms.DialogResult.OK;
            }
            return true;
        }// LoadServicesFromFile()
        //
        //
        //
        //
        //
        // *************************************************************
        // ****          TrySaveServicesToFile()                    ****
        // *************************************************************
        //
        public bool TrySaveServicesToFile(string fileName, bool overWriteExisting=true)
        {
            string filePath = string.Format("{0}{1}", Info.UserConfigPath, fileName);
            if (System.IO.File.Exists(filePath))
            {
                if (overWriteExisting)
                    System.IO.File.Delete(filePath);
                else
                    return false;
            }

            // Create string xml.
            StringBuilder s = new StringBuilder();
            lock (m_ServiceLock)
            {
                foreach (IService service in m_Services)
                {
                    UV.Lib.IO.Xml.IStringifiable iStringObj = service as UV.Lib.IO.Xml.IStringifiable;
                    if (iStringObj != null)
                        s.AppendFormat("{0}\r\n", Stringifiable.Stringify(iStringObj));
                }
            }
            // Write out file.
            try
            {
                using (System.IO.StreamWriter writer = System.IO.File.CreateText(filePath))
                {
                    writer.Write(s.ToString());
                    writer.Close();
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }//TrySaveServicesToFile()
        //
        //
        //
        //
        // *************************************************************
        // ****                 Get Services()                      ****
        // *************************************************************
        /// <summary>
        /// Allow users to get all services of a certain type, or service name.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public List<IService> GetServices(Type t)
        {
            List<IService> services = new List<IService>();
            lock (m_ServiceLock)
            {
                foreach (IService iservice in this.m_Services)
                {
                    //Type iServiceType = iservice.GetType();
                    //if (iServiceType.Equals(t))
                    //    services.Add(iservice);
                    //else if (iServiceType.IsSubclassOf(t))
                    //    services.Add(iservice);
                    if (t.IsInstanceOfType(iservice)) 
                        services.Add(iservice);
                }
            }
            return services;
        }
        /// <summary>
        /// Returns all active services.
        /// </summary>
        /// <returns></returns>
        public List<IService> GetServices()
        {
            List<IService> services = new List<IService>();
            lock (m_ServiceLock)
            {
                foreach (IService iservice in this.m_Services)
                    services.Add(iservice);
            }
            return services;
        }
        /// <summary>
        /// Returns service with exact service name, or returns false.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        public bool TryGetService(string serviceName, out IService service)
        {
            service = null;
            bool isSuccess = false;
            lock (m_ServiceLock)
            {
                int ptr = -1;
                if (m_ServiceNames.Contains(serviceName))
                {
                    ptr = m_ServiceNames.IndexOf(serviceName);
                    service = m_Services[ptr];
                    isSuccess = true;
                }                
            }
            return isSuccess;
        }
        /// <summary>
        /// Returns all service names 
        /// </summary>
        /// <returns></returns>
        public List<string> GetServiceNames()
        {
            List<string> services = new List<string>();
            lock (m_ServiceLock)
            {                
                services.AddRange(m_ServiceNames);
            }
            return services;
        }
        //
        // *************************************************************
        // ****                     Start()                         ****
        // *************************************************************
        /// <summary>
        /// TODO: Services like the hubs, that are managed services should all have to 
        /// implement a single interface that implements these state requests:  Start(), 
        /// Connect(), RequestStop(), etc.
        /// </summary>
        public void Start()
        {
            List<IService> services = GetServices();    // The lock is in here, and this way, we release the lock 
            foreach (IService iservice in services)     // before we call out of this object.
                iservice.Start();
        }
        // *************************************************************
        // ****                   Connect()                         ****
        // *************************************************************
        public void Connect()
        {
            List<IService> services = GetServices();   
            foreach (IService iservice in services)
                iservice.Connect();
        }//Connect()
        //
        // *************************************************************
        // ****                     Shutdown()                      ****
        // *************************************************************
        public void Shutdown()
        {            
            if (!m_IsShuttingDown)
            {
                m_IsShuttingDown = true;
                List<string> serviceNames = GetServiceNames();
                foreach (string name in serviceNames)
                    this.TryShutdownService(name);
            }
        }//Shutdown()
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
        #endregion//Private Methods


        #region Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        /// <summary>
        /// Respond to someone requesting a shutdown of the application by shutting 
        /// down services nicely.
        /// TODO: I think this feature should be within this object not AppInfo.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArg"></param>
        private void RequestShutdownHandler(object sender, EventArgs eventArg)
        {
            Shutdown();
        }
        //
        //
        /// <summary>
        /// When I request a service to shutdown, I also attach myself to its Stopping event
        /// and expect to hear back from it.  Upon receiving, the service is removed from 
        /// the ServicesRemoved list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void Service_Stopping(object sender, EventArgs eventArgs)
        {
            IService iService = null;
            if (sender is IService)
            {
                string serviceName = ((IService)sender).ServiceName;
                bool isSuccess = false;
                lock (m_ServiceLock)
                {
                    if (!string.IsNullOrEmpty(serviceName) && m_ServiceNames.Contains(serviceName))
                    {
                        int ptr = m_ServiceNames.IndexOf(serviceName);
                        iService = m_Services[ptr];
                        m_Services.RemoveAt(ptr);
                        m_ServiceNames.RemoveAt(ptr);
                        isSuccess = true;
                    }
                }
                if (isSuccess)
                {
                    try
                    {
                        iService.Stopping -= new EventHandler(this.Service_Stopping);
                        OnServiceStopped(iService);                 // report to subscribers that a service was removed.
                        if (iService is IDisposable)
                            ((IDisposable)iService).Dispose();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }// Service_Stopping()
        //
        #endregion//Event Handlers


        #region Events
        // *****************************************************************
        // ****                         Events                          ****
        // *****************************************************************
        //
        //
        //
        // ****             Service Stopped             ****
        /// <summary>
        /// Triggered when a Service has stopped and it has been removed from our ServiceList.
        /// </summary>
        public event EventHandler ServiceStopped;
        private void OnServiceStopped(IService serviceRemoved)
        {
            if (this.ServiceStopped != null)
            {
                AppServiceEventArg eventArg = new AppServiceEventArg();
                eventArg.ServiceName = serviceRemoved.ServiceName;
                eventArg.EventType = AppServiceEventType.ServiceRemoved;
                this.ServiceStopped(serviceRemoved, eventArg);
                //this.ServiceStopped(serviceRemoved, EventArgs.Empty);

            }
        }
        //
        //
        // ****             Service Added             ****
        /// <summary>
        /// Triggered when a Service is added to our ServiceList.
        /// </summary>
        public event EventHandler ServiceAdded;
        private void OnServiceAdded(IService service)
        {
            if (this.ServiceAdded != null)
            {
                AppServiceEventArg eventArg = new AppServiceEventArg();
                eventArg.ServiceName = service.ServiceName;
                eventArg.EventType = AppServiceEventType.ServiceAdded;
                this.ServiceAdded(service, eventArg);
                //this.ServiceAdded(service, EventArgs.Empty);
            }
        }
        //
        //
        //
        #endregion//Events


    }
}
