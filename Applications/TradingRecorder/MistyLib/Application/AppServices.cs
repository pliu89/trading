using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.Windows.Threading;

namespace Misty.Lib.Application
{
    using Misty.Lib.IO.Xml;
    using Misty.Lib.Hubs;
    using Misty.Lib.MarketHubs;
    using Misty.Lib.OrderHubs;
    
    /// <summary>
    /// The first caller to GetInstance() in this class must be the UI thread.
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
        
        //
        // Services
        //
        public readonly Dispatcher UiDispatcher;                        // the Ui dispatcher for this application.
        //private ConcurrentDictionary<string, IService> m_Services = new ConcurrentDictionary<string, IService>();
        //private ConcurrentDictionary<string, IService> m_ServicesRemoved = new ConcurrentDictionary<string, IService>();

        private object m_ServiceLock = new object();
        private List<string> m_ServiceNames = new List<string>();
        private List<IService> m_Services = new List<IService>();

        // Global Ambre user name for each user on each machine.
        public UserInformation m_AmbreUserName;
        
        //
        // Internal controls
        //
        private bool m_IsShuttingDown = false;                          // ensures we don't try to release resources more than once.


        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        /// <summary>
        /// This must be called by the UI thread. (Alternatively, use PresentationFramework Application.Current.Dispatcher.)
        /// </summary>
        private AppServices()
        {   // Creates the singleton (default) objects right now, albeit trivial, default objects.
            this.Info = AppInfo.GetInstance();          // Creating the sole instance of an default AppInfoInstance.
            this.Info.RequestShutdownAddHandler(new EventHandler(this.RequestShutdownHandler));   

            UiDispatcher = Dispatcher.CurrentDispatcher;
            FrontEnds.GuiCreator.Create();
        }
        //
        //       
        #endregion//Constructors


        #region no Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        #endregion//Properties


        #region Static Methods
        // *****************************************************************
        // ****                     Static Methods                      ****
        // *****************************************************************
        //
        //
        // ****				GetInstance()				****
        //
        /// <summary>
        /// If AppServices is to be used, it must be called before AppInfo.
        /// </summary>
        /// <param name="applicationName"></param>
        /// <param name="isExactDirName"></param>
        /// <returns></returns>
        public static AppServices GetInstance(string applicationName, bool isExactDirName=true)
        {
            AppInfo.GetInstance(applicationName, isExactDirName);                   // This will initialize AppInfo the first time we call it.
            AppServices appServices = m_AppServicesInstance;                        // singleton of this object.
            // TODO: Test if appServices is initialized, if not initialize it here.
            return appServices;
        }
        //
        public static AppServices GetInstance()
        {
            return m_AppServicesInstance;
        }
        //
        //
        #endregion//Static Methods


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
            bool addedService = false;
            if (serviceObject is IService)                                 // These are other non-Misty services, like external APIs.
            {   
                IService service = (IService)serviceObject;
                lock (m_ServiceLock)
                {
                    if (m_ServiceNames.Contains(service.ServiceName))
                    {
                        addedService = false;
                        // Alternative is to create new name for the service.
                        //throw new Exception(string.Format("Service name {0} is not unique.", aHub.ServiceName));
                    }
                    else
                    {
                        m_ServiceNames.Add(service.ServiceName);
                        m_Services.Add(service);
                        addedService = true;
                    }
                }//lock
                return addedService;
            }
            else if (serviceObject is IStringifiable)
            {
                m_AmbreUserName = (UserInformation)serviceObject;
            }
            // Exit
            return addedService;
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
        /// We hold onto its pointer until we get the "Stopping" event callback.
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
                        m_ServiceNames.RemoveAt(ptr);
                        m_Services.RemoveAt(ptr);
                        
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
        public void LoadServicesFromFile(string configFileName)
        {
            try
            {
                string filePath = string.Format("{0}{1}", this.Info.UserConfigPath, configFileName);
                List<Misty.Lib.IO.Xml.IStringifiable> iStringObjects;
                using (Misty.Lib.IO.Xml.StringifiableReader reader = new IO.Xml.StringifiableReader(filePath))
                {
                    iStringObjects = reader.ReadToEnd();
                }                
                foreach (Misty.Lib.IO.Xml.IStringifiable iStrObj in iStringObjects)
                    TryAddService(iStrObj);
            }
            catch (Exception e)
            {
                StringBuilder msg = new StringBuilder();
                msg.AppendFormat("Exception: {0}\r\n", e.Message);
                System.Windows.Forms.MessageBox.Show(msg.ToString(), "AppServices.LoadSerivesFromFile");
            }
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
                    Misty.Lib.IO.Xml.IStringifiable iStringObj = service as Misty.Lib.IO.Xml.IStringifiable;
                    if (iStringObj != null)
                        s.AppendFormat("{0}\r\n", Stringifiable.Stringify(iStringObj));
                    if (service.ServiceName.Equals("TTApi"))
                    {
                        Dictionary<Type, string[]> stringifyOverrideTable = new Dictionary<Type, string[]>();
                        s.AppendFormat("{0}\r\n", Stringifiable.Stringify(m_AmbreUserName, stringifyOverrideTable));
                    }
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
        public List<IService> GetServices(Type t)
        {
            List<IService> services = new List<IService>();
            lock (m_ServiceLock)
            {
                foreach (IService iservice in this.m_Services)
                {
                    Type iServiceType = iservice.GetType();
                    if (iServiceType.Equals(t))
                        services.Add(iservice);
                    else if (iServiceType.IsSubclassOf(t))
                        services.Add(iservice);
                }
            }
            return services;
        }
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
                        if (iService is IDisposable)
                            ((IDisposable)iService).Dispose();
                        OnServiceStopped(iService);                 // report to subscribers that a service was removed.
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
                this.ServiceStopped(serviceRemoved, EventArgs.Empty);
            }
        }
        //
        //
        //
        #endregion//Events


    }
}
