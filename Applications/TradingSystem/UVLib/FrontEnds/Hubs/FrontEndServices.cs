using System;

using UV.Lib.FrontEnds;
//using BGTLib.Instruments.MP;			
//using UV.Lib.Database.Chimera;

namespace UV.Lib.FrontEnds
{
	/// <summary>
    /// This is DEFUNCT.  Replaced by AppInfo class.
	/// Receptical for important constants, directory locations etc.
	/// </summary>
	public class FrontEndServices
	{


		#region Members
		// *****************************************************************
		// ****                     Members                             ****
		// *****************************************************************
		//
		// Static pointer for myself
		//
		private static FrontEndServices m_FrontEndServicesInstance = null;

		// Service holders					
		public FrontEndServer FrontEndServer = null;

		//
		// Important pathnames
		//
		public string BaseDirectory = string.Empty;

		public string UserDirectory = "user\\";				// convention: directories have trailing slashes.
		public string UserConfigDirectory = "Config\\";		// directories inside "user" directory
		public string UserSoundsDirectory = "Sounds\\";
		public string LogDirectory = "Logs\\";

		//
		// Application information
		//
		public string AppName = "Tramp";					// Name of the application.
		public RunNameType RunName = RunNameType.ProdA;		// identifying name for this particular run:
		
		// Email messaging
		public string AppEmailAddress = "uvtrading1@gmail.com";
		public string[] EmailRecipients = null;					// list of email addresses


		//
		// Database connection
		//
		//public DatabaseInfo.DatabaseLocation DefaultDBTramp = DatabaseInfo.DatabaseLocation.Cermak;
		//public DatabaseInfo.DatabaseLocation DefaultDBMarketInfo = DatabaseInfo.DatabaseLocation.Cermak;

		//
		//
		#endregion// members


		#region  Private Constructor		
		protected FrontEndServices()
		{
			m_FrontEndServicesInstance = (FrontEndServices) this;
		}
		#endregion//constructor


		#region Properties
		// *****************************************************************
		// ****                     Properties                          ****
		// *****************************************************************
		//
		public string UserConfigPath
		{
			get { return string.Format("{0}{1}{2}", BaseDirectory, UserDirectory, UserConfigDirectory); }
		}
		public string UserPath
		{
			get { return string.Format("{0}{1}", BaseDirectory, UserDirectory ); }
		}
		public string LogPath
		{
			get { return string.Format("{0}{1}{2}", BaseDirectory, UserDirectory, LogDirectory); }
		}
		//
		//
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
		#endregion//Properties


		#region Static Methods
		// *****************************************************************
		// ****                     Public Methods                      ****
		// *****************************************************************
		//
		//
		// ****				GetInstance()				****
		public static FrontEndServices GetInstance()
		{
			if (m_FrontEndServicesInstance == null) { m_FrontEndServicesInstance = new FrontEndServices(); } // create new singleton instance.
			return m_FrontEndServicesInstance;
		}
		//
		//
		//
		//
		//
		#endregion//Public Methods


		#region Public Enums
		// *****************************************************************
		// ****                     Public Enums                        ****
		// *****************************************************************
		//
		//
		/// <summary>
		/// Note: 
		/// 1) All types that are "production run" types MUST start with letters "Prod".
		/// 2) These must be preset as prefixes of Tango strategies, if using Tango.
		/// </summary>
		public enum RunNameType
		{
			ProdA
			,ProdB
			,Sim1
			,Sim2
			,Sim3
			,Test1
			,Test2
		}//RunNameType
		//
		//
		//
		//
		//
		#endregion//Public Enums


		#region no private Methods
		// *****************************************************************
		// ****                     Private Methods                     ****
		// *****************************************************************
		//				
		//
		#endregion//Private Methods
		

	}//end class
}
