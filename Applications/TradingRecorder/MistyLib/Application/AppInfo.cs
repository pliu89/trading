using System;
using System.Collections.Generic;
using System.Text;

namespace Misty.Lib.Application
{
    /// <summary>
    /// This singleton model object contains only the most basic information necessary
    /// for the application to run.
    /// </summary>
    public sealed class AppInfo
    {
        private static readonly AppInfo m_AppInfoInstance = new AppInfo(); // the single global instance of this object.
        
        //
        // Important application pathnames
        //
        // Herein, I used keywords Path and Dir where Path is the full directory name from the device name to the 
        // deepest directory, and Dir refers to just the name of the deepest directory.
        public string BasePath = string.Empty;                  // Base path of the application.
        public string UserDirectory = "user\\";				    // convention: directories have trailing slashes.
        // Subdirectories of the UserDirectory
        public string UserConfigDirectory = "Config\\";		    // directories inside "user" directory
        //public string UserSoundsDirectory = "Sounds\\";
        public string LogDirectory = "Logs\\";
        public string DropDirectory = "Drops\\";

        //
        // Application information
        //
        public string ApplicationName = string.Empty;			// Name of the application.
        private event EventHandler m_RequestShutdown;           // Application owner can register his shutdown request method here.


        //public RunNameType RunName = RunNameType.ProdA;		// identifying name for this particular run:
		

        #region Properties
		// *****************************************************************
		// ****                     Properties                          ****
		// *****************************************************************		
		public string UserPath
		{
            get { return string.Format("{0}{1}",BasePath, UserDirectory); }
		}
		public string LogPath
		{
			get { return string.Format("{0}{1}{2}", BasePath, UserDirectory, LogDirectory); }
		}
        public string DropPath
        {
            get { return string.Format("{0}{1}{2}", BasePath, UserDirectory, DropDirectory); }
        }
        public string UserConfigPath
        {
            get { return string.Format("{0}{1}{2}", BasePath, UserDirectory, UserConfigDirectory); }
        }

		//
		//
        /*
		/// <summary>
		/// Returns true, if current RunName is one of the production choices?
		/// </summary>
		public bool IsCurrentRunProduction
		{
			get
			{
				bool isProductionRun = RunName.ToString().StartsWith("Prod", StringComparison.CurrentCultureIgnoreCase);
				return isProductionRun;	// if run name starts with "prod" then its a production run type.
			}
		}
		public bool IsCurrentRunATest
		{
			get
			{
				bool isTestRun = RunName.ToString().StartsWith("Test", StringComparison.CurrentCultureIgnoreCase);
				return isTestRun;	// if run name starts with "prod" then its a production run type.
			}
		}
        */ 
        //
        //
        //
        //
		#endregion//Properties


        #region Constructor
        //
        //                      Constructor
        //
        private AppInfo()
        {
           
        }
        #endregion


        #region Static Methods
        // *****************************************************************
        // ****                     Static Methods                      ****
        // *****************************************************************
        //
        //
        // ****				GetInstance()				****
        //
        public static AppInfo GetInstance()
        {            
            return m_AppInfoInstance;
        }
        public static AppInfo GetInstance(string applicationName)
        {
            return GetInstance(applicationName, true);
        }
        public static AppInfo GetInstance(string applicationName, bool isExactDirName)
        {
            AppInfo info = m_AppInfoInstance;            
            if (string.IsNullOrEmpty(m_AppInfoInstance.ApplicationName))        // This gets set only on the first call.
            {
                m_AppInfoInstance.ApplicationName = applicationName;
                string currPath = System.IO.Directory.GetCurrentDirectory();
                string basePath = Misty.Lib.Utilities.FilesIO.GetPathToDirName(currPath, applicationName, isExactDirName);
                info.BasePath = basePath;
                info.LogDirectory = string.Format("{0}{1}", info.LogDirectory, Misty.Lib.Utilities.FilesIO.GetTodaysLogDirAndClean(info.LogPath));
                info.CreateDirectories();
            }
            return info;
        }
        //
        //
        //
        //
        //
        #endregion//Static Methods



        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        public void CreateDirectories()
        {
            if (!System.IO.Directory.Exists(this.UserPath))
                System.IO.Directory.CreateDirectory(this.UserPath);
            if (!System.IO.Directory.Exists(this.LogPath))
                System.IO.Directory.CreateDirectory(this.LogPath);
            if (!System.IO.Directory.Exists(this.UserConfigPath))
                System.IO.Directory.CreateDirectory(this.UserConfigPath);


        }
        //
        //
        // ****                 RequestApplicationShutdown()                ****
        /// <summary>
        /// To use this, the main application object (usually the main form) must
        /// add itself to the private m_RequestApplicationShutdown delegate using
        /// the method below.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArg"></param>
        public void RequestShutdown(object sender, EventArgs eventArg)
        {
            if (this.m_RequestShutdown != null)
                this.m_RequestShutdown(sender, eventArg);
        }
        //
        //
        // Usage: appInfo.RequestShutdownAddHandler(new EventHandler(this.RequestShutdown));
        //
        public void RequestShutdownAddHandler(EventHandler shutdownEventHandler)
        {
            this.m_RequestShutdown += shutdownEventHandler;
        }
        public void RequestShutdownSubtractHandler(EventHandler shutdownEventHandler)
        {
            this.m_RequestShutdown -= shutdownEventHandler;
        }
        //
        //
        //
        #endregion//Public Methods

        

    }
}
