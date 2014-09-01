using System;
using System.Collections.Generic;
using System.Text;

using UV.Lib.IO.Xml;
using UV.Lib.Application;
using UV.Lib.Hubs;
using UV.Lib.Engines;
using UV.Lib.BookHubs;
using UV.Lib.FrontEnds.Clusters;

namespace UV.Lib.FrontEnds
{

	/// <summary>
	/// This is a service that can subscribe to and display GUIs for IEngineHubs.
	/// Features:
	///     1) When we are shutting down, take care to shutdown each display gently.
	/// </summary>
	public class FrontEndServer : Hub, IService, IStringifiable
	{
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // My Service controls
        private AppServices m_AppServices = null;
        private ServiceStates m_ServiceState = ServiceStates.Unstarted;
        private Queue<FrontEndRequest> m_PendingRequests = new Queue<FrontEndRequest>(); 
		
        //
        // EngineHub controls:
        //              EngineHubTemplates:     Dict: iEngineHub -->  ( Dict: EngineContainerID --> EngineContainerGui )
        private Dictionary<string, IEngineHub> m_RemoteEngineHubs = new Dictionary<string, IEngineHub>();
        private Dictionary<string, Dictionary<int, GuiTemplates.EngineContainerGui>> m_EngineHubTemplates = new Dictionary<string, Dictionary<int, GuiTemplates.EngineContainerGui>>();

        //              iEngineHubName, EngineContainerId --> list of ClusterDisplayIds, containing that engine conttainer.
        private Dictionary<string, Dictionary<int, List<int>>> m_ClusterDisplayIds = new Dictionary<string, Dictionary<int, List<int>>>();
        private Dictionary<int,ClusterDisplay> m_ClusterDisplays = new Dictionary<int,ClusterDisplay>();    // List of Displays via their DisplayIDs.
        private int m_NextClusterDisplayID = -1;                     // unique IDs for each ClusterDisplay created.
        //private Dictionary<int, List<int>> m_ClusterToClusterDisplay = new Dictionary<int, List<int>>();

        // Control parameters
        private List<string> m_IgnoreEngineNamePatterns = new List<string>();

        #endregion// members

        
        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
		public FrontEndServer()
			: base("FrontEndServer", AppServices.GetInstance().Info.LogPath, false, LogLevel.ShowAllMessages)
        {
            m_AppServices = AppServices.GetInstance();
            base.m_WaitListenUpdatePeriod = 1000;

            m_IgnoreEngineNamePatterns.Add("Execution");       // ignore hubs with this name.

        }//end constructor
        //
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


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
		// *****************************************
		// ****			AddEngineHub()			****
		// *****************************************
		/// <summary>
		/// Submit a request to connect to a specific EngineHub.
		/// Currently, only one engineHub is serviced - but this may change.
        /// Called by external thread.
		/// </summary>
        /// <param name="engineHub">EngineHub to be monitored.</param>
		public void AddEngineHub(string engineHub)
		{
			FrontEndRequest request = new FrontEndRequest();
			request.Type = FrontEndRequest.Request.AddService;
			request.ObjectList = new List<object>();
            request.ObjectList.Add(engineHub);
			this.HubEventEnqueue(request);
		}//end AddEngineHub().
        //
		//
        // *****************************************
        // ****         Request Display()       ****
        // *****************************************
        /// <summary>
        /// User request creation of new display.  The request 
        /// generates a call to get the GuiTemplates, and then 
        /// automatically opens the form when completed.
        /// TODO:
        ///     1) Allow user to choose which hub to create display for.
        ///     2) Allow user to choose which Clusters are added, etc.
        /// </summary>
        public bool TryRequestDisplay(string engineHubName = null)
        {
            // Create the request
            FrontEndRequest request = new FrontEndRequest();
            request.Type = FrontEndRequest.Request.AddDisplay;
            request.ObjectList = new List<object>();
            request.ObjectList.Add(engineHubName);
            this.HubEventEnqueue(request);
            return true;

        }// TryRequestDisplay()
        //
		//
		//
        //
        //
        // *****************************************************
        // ****             Request Stop()                  ****
        // *****************************************************
        public override void Start()
        {
            FrontEndRequest request = new FrontEndRequest();
            request.Type = FrontEndRequest.Request.Start;
            this.HubEventEnqueue(request);        
 	        base.Start();
        }
        //
        // *****************************************************
        // ****             Request Stop()                  ****
        // *****************************************************
        public override void RequestStop()
        {
            FrontEndRequest request = new FrontEndRequest();
            request.Type = FrontEndRequest.Request.Stop;
            this.HubEventEnqueue(request);
        }
        //
		//
		//
        #endregion//Public Methods


		#region HubEvent processing
		// *************************************************************
		// ****                     Hub Event Handler               ****
		// *************************************************************
		/// <summary>
		/// Main request handling routine processing all events originating from 
		/// external and internal sources.
		/// Called only by the internal hub thread.
		/// </summary>
		/// <param name="eventArgList">Array of EventArgs to be processed.</param>
		protected override void HubEventHandler(EventArgs[] eventArgList)
		{
			foreach (EventArgs anEventArg in eventArgList)	                // process each event
			{
                if (anEventArg is FrontEndRequest)
                    ProcessFrontEndRequest((FrontEndRequest)anEventArg);    // requests from user
                else if (anEventArg is EngineEventArgs)
                    ProcessEngineEvents((EngineEventArgs)anEventArg);       // responses from strategies
                else if (anEventArg is Utilities.GuiCreator.CreateFormEventArgs)
                    ProcessCreatedForm((Utilities.GuiCreator.CreateFormEventArgs)anEventArg);
                else if (anEventArg is Application.AppServiceEventArg)
                    ProcessServiceEvent((Application.AppServiceEventArg)anEventArg);
                else
                    Log.NewEntry(LogLevel.Warning, "Unknown event type: {0}", anEventArg.ToString());			
			} 
		}//HubEventHandler()
		//
		//
		//
		// *****************************************************************
		// ****					Process EngineEvents					****
		// *****************************************************************
        /// <summary>
        /// These are events broadcast from Engines on the StrategyHub (typically).
        /// Confirmation of parameter changes must be passed to the guis that 
        /// represent them.
        /// </summary>
        /// <param name="engineEvent"></param>
		private void ProcessEngineEvents(EngineEventArgs engineEvent)
		{
			switch(engineEvent.MsgType)
            {            
                case EngineEventArgs.EventType.GetControls:
				    // *****************************************
				    // ****			Get Controls			****
				    // *****************************************
                    // Processes the response from an EngineHub providing its controls & GUIs.
                    if (engineEvent.Status != EngineEventArgs.EventStatus.Confirm)
                        return;                    
                    // Add controls to our master list for each engine hub.
                    IEngineHub iRemoteEngineHub;
                    if ( m_RemoteEngineHubs.TryGetValue(engineEvent.EngineHubName,out iRemoteEngineHub))
                    {   // Get the EngineHub that contains the engines associated with these controls.
                        // Extract controls from the event arg.
                        List<IStringifiable> getControlsData = new List<IStringifiable>();
                        try
                        {
                            foreach (object o in engineEvent.DataObjectList)
                                if (o is string)
                                    getControlsData.AddRange(Stringifiable.Create((string)o));
                        }
                        catch (Exception ex)
                        {
                            Log.NewEntry(LogLevel.Major, "ProcessEngineEvent: Exception for GetControls event. {0}", ex.Message);
                            return;
                        }
                        // Get control template for this IEngineHub (if they exist we overwrite them).
                        Dictionary<int, GuiTemplates.EngineContainerGui> hubTemplates = null;
                        if ( ! m_EngineHubTemplates.TryGetValue(engineEvent.EngineHubName, out hubTemplates))
                        {   // First time we've received controls from this engineHub. Create a template list for it.
                            hubTemplates = new Dictionary<int, GuiTemplates.EngineContainerGui>();  // containerId, gui template
                            m_EngineHubTemplates.Add(engineEvent.EngineHubName, hubTemplates);
                        }
                        Log.NewEntry(LogLevel.Minor, "ProcessEngineEvent: {0} for {1}.", engineEvent.MsgType, engineEvent.EngineHubName);
                        // Extract each bit of control data.
                        Dictionary<string,IEngineHub> engineHubPointers = new Dictionary<string,IEngineHub>();
                        foreach (object o in getControlsData)
                        {                           
                            if (o is GuiTemplates.EngineContainerGui)
                            {   // This is an engine container gui object.
                                // We will accept this as a new gui description for this EngineContainer.
                                // TODO: Why overwrite it?  How to implement new Engines appearing dynamically?
                                GuiTemplates.EngineContainerGui engContainerGui = (GuiTemplates.EngineContainerGui) o;
                                hubTemplates[engContainerGui.EngineContainerID] = engContainerGui;      // add/overwrite entries for this strategy.
                            }
                        }
                    }
                    break;
            default:
				// *************************************************
				// ****			Default EngineEvent			    ****
				// *************************************************
                // All other engine messages are simply routed to the display that 
                // containing the appropriate IEngineHub and IEngineContainer / Cluster.
                    if (engineEvent.EngineContainerID < 0)
                    {
                        //Log.NewEntry(LogLevel.Warning, "ProcessEngineEvent: Warning EngineHubName={0}, EngineContainerID={1}.", engineEvent.EngineHubName, engineEvent.EngineContainerID);
                        if (engineEvent.MsgType== EngineEventArgs.EventType.SaveEngines)
                        {                            
                            // TODO: when receiving this event (we check that we are expecting it).
                            // If we are, then we push it to the open save engine form, so the user can analyze the nodes
                            // manipulate them etc.  This is how the user can copy strategies, and edit them on the fly.
                            // Now, we simply dump the data to a file.
                            string path = string.Format("{0}SavedStrategies.txt",AppInfo.GetInstance().UserConfigPath);
                            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(path, false))
                            {
                                foreach (object o in engineEvent.DataObjectList)
                                {
                                    string s = (string)o;
                                    sw.WriteLine(s);
                                }
                                sw.Close();
                            }
                        }
                    }
                    else
                    {	// Update for specific EngineContainerId
                        List<int> displayList = null;
                        Dictionary<int, List<int>> clusterIdToDisplayList = null;
                        if (m_ClusterDisplayIds.TryGetValue(engineEvent.EngineHubName, out clusterIdToDisplayList) && clusterIdToDisplayList.TryGetValue(engineEvent.EngineContainerID, out displayList))
                        {
                            foreach (int displayID in displayList)
                            {
                                ClusterDisplay display;
                                if (m_ClusterDisplays.TryGetValue(displayID, out display))
                                {
                                    if (display.HubEventEnqueue(engineEvent))
                                        display.RegenerateNow(this, null);
                                }
                            }
                        }
                    }
                break;
            }//switch(engineEvent.MsgType)
		}//ProcessEngineEvents()
		//
		//
		//
		// *****************************************************************
		// ****					Process FrontEndRequest					****
		// *****************************************************************
		/// <summary>
		/// These are my internal requests, used to create new GUI displays, etc.
		/// </summary>
		/// <param name="request"></param>
		private void ProcessFrontEndRequest(FrontEndRequest request)
		{
			EngineEventArgs e;
			Log.NewEntry(LogLevel.Minor, "FrontEndRequest {0}", request.ToString());
			switch (request.Type)
			{
                case FrontEndRequest.Request.Start:
                    // ************************************
                    // ***          Start               ***
                    // ************************************
                    m_ServiceState = ServiceStates.Running;             // We are ready to run.
                    OnServiceStateChanged();
                    break;
				case FrontEndRequest.Request.Stop:
                    // ************************************
                    // ***          Stop                ***
                    // ************************************
                    if ( m_ServiceState != ServiceStates.Stopping)
                    {   // Shut down displays
					    m_ServiceState = ServiceStates.Stopping;            // Trying to stop.
                        OnServiceStateChanged();
                        // Tell displays to shut down.
                        foreach (ClusterDisplay display in m_ClusterDisplays.Values)
					    {
                            if (display != null && (!display.IsDisposed))
                            {
                                try
                                {
                                    display.Invoke((System.Threading.ThreadStart)delegate() { display.Close(); });
                                }
                                catch (Exception)
                                {

                                }
                            }
					    }
                    }
                    // Verify we can stop.
                    if (m_ClusterDisplays.Count == 0)
                    {   // All displays are removed and shutdown.  We can exit now.
                        m_ServiceState = ServiceStates.Stopped;
                        OnServiceStateChanged();
                        base.Stop();
                    }
                    else
                    {
                        Log.NewEntry(LogLevel.Major, "Shutting down. {0} displays remaining.", m_ClusterDisplays.Count);
                        m_PendingRequests.Enqueue(request);
                    }
					break; 
				case FrontEndRequest.Request.AddService:
					// ************************************
					// ***          Add Service         ***
					// ************************************
                    // This is called internally, each time a new service is added
                    // to the application.  We attempt to get information about it.
                    if (request.ObjectList == null)
                        return;
                    for (int i = 0; i < request.ObjectList.Count; ++i)
                    {
                        string serviceName = (string)request.ObjectList[i];
                        bool isGoodToAdd = true;
                        // First check whether this type of engine is we want to ignore.
                        foreach (string pattern in m_IgnoreEngineNamePatterns)
                            if (serviceName.Contains(pattern))
                            {
                                isGoodToAdd = false;
                                break;
                            }
                        if (isGoodToAdd == false)
                            return;
                        IService iService;
                        if (m_AppServices.TryGetService(serviceName, out iService) && iService is IEngineHub)
                        {   // We have discovered a new EngineHub
                            if (!m_RemoteEngineHubs.ContainsKey(serviceName))
                            {   // This new engineHub isunknown to us.
                                IEngineHub iEngine = (IEngineHub)iService;
                                m_RemoteEngineHubs.Add(serviceName, iEngine);
                                Log.NewEntry(LogLevel.Major, "Adding new EngineHub {0}.  Requesting all controls.", serviceName);
                                iEngine.EngineChanged += new EventHandler(this.HubEventEnqueue);
                                e = EngineEventArgs.RequestAllControls(serviceName);
                                iEngine.HubEventEnqueue(e);
                            }
                        }
                    }
					break;
				case FrontEndRequest.Request.AddDisplay:
					// ************************************
					// ***          Add Display         ***
					// ************************************
					// When a new display is requested, we search for the GuiTemplates already 
	                // stored for that new display, create the display and hand them to the display.
                    List<string> engineHubNames = new List<string>();
                    if (request.ObjectList == null || (request.ObjectList.Count == 1 && request.ObjectList[0]==null))
                        engineHubNames.AddRange(m_RemoteEngineHubs.Keys);
                    else
                    {
                        foreach (object o in request.ObjectList)
                            if (o is string)
                                engineHubNames.Add((string)o);
                    }
                    // Open displays for each hub name provided.
                    foreach (string engineName in engineHubNames)
                    {
                        Dictionary<int,GuiTemplates.EngineContainerGui>engineHubTemplates = null;
                        IEngineHub iEngineHub = null;
                        if (m_RemoteEngineHubs.TryGetValue(engineName,out iEngineHub) &&  m_EngineHubTemplates.TryGetValue(engineName, out engineHubTemplates))
                        {
                            Log.NewEntry(LogLevel.Major, "AddDisplay requesting new ClusterDisplay for {0}.", engineName);
                            List<GuiTemplates.EngineContainerGui> templates = new List<GuiTemplates.EngineContainerGui>(engineHubTemplates.Values);
                            Utilities.GuiCreator creator = Utilities.GuiCreator.Create(typeof(ClusterDisplay), this, iEngineHub, templates);
                            creator.FormCreated += new EventHandler(this.HubEventEnqueue);      // push created form on to my usual queue.
                            creator.Start();					
                        }
                        else
                            Log.NewEntry(LogLevel.Major, "AddDisplay request rejected for {0}.  No templates found.", engineName);
                    }
					break;
				case FrontEndRequest.Request.RemoveDisplay:
					// ************************************
					// ***			Remove Display		***
					// ************************************
					ClusterDisplay display2 = (ClusterDisplay)request.ObjectList[0];					
					int n = display2.ID;
                    if (m_ClusterDisplays.ContainsKey(n))
                    {
                        m_ClusterDisplays.Remove(n);
                        Log.NewEntry(LogLevel.Major, "RemoveDisplay id {1}. Remaining {0}.", m_ClusterDisplays.Count, n);
                    }
					break;
				default:
					Log.NewEntry(LogLevel.Warning, "Unknown request.");
					break;
			}//requestType switch
		}//ProcessFrontEndRequest().
		//
		//
        //
        //
        // *********************************************************
        // ****             ProcessCreatedForm()                ****
        // *********************************************************
        /// <summary>
        /// A new Form has been created by the GUI thread, and is now ready to 
        /// be employed.  We request all controls for this display (based on the type 
        /// of display it is).
        /// </summary>
        /// <param name="anEventArg"></param>
        private void ProcessCreatedForm(Utilities.GuiCreator.CreateFormEventArgs anEventArg)
        {
            //
            if (anEventArg.CreatedForm is ClusterDisplay)
            {   // We have created a new ClusterDisplay.
                ClusterDisplay display = (ClusterDisplay) anEventArg.CreatedForm;
                IEngineHub iengineHub = display.AssocEngineHub;             // EngineHub associated with this display.
                display.FormClosing += new System.Windows.Forms.FormClosingEventHandler(ClusterDisplay_FormClosing);                
                display.ID = System.Threading.Interlocked.Increment(ref m_NextClusterDisplayID);// create a unique ID for this display.
                m_ClusterDisplays.Add(display.ID,display);

                // Create quick lookup tables for engine event arg processing.
                // We create a list of display IDs that contain the controls once given the IEngineHub name
                // and IEngineContainerID.
                Dictionary<int, List<int>> clusterIdsToDislayIds = null;
                if (! m_ClusterDisplayIds.TryGetValue(iengineHub.ServiceName,out clusterIdsToDislayIds))
                {   // First form created for this engineHub.  Add entry for it.
                    clusterIdsToDislayIds = new Dictionary<int, List<int>>();
                    m_ClusterDisplayIds.Add(iengineHub.ServiceName, clusterIdsToDislayIds);
                }
                int displayID = display.ID;
                foreach (IEngineContainer iengContainer in display.GetEngineContainers())
                {
                    List<int>clusterIdList;
                    if ( ! clusterIdsToDislayIds.TryGetValue(iengContainer.EngineContainerID, out clusterIdList) )
                    {
                        clusterIdList = new List<int>();
                        clusterIdsToDislayIds.Add(iengContainer.EngineContainerID, clusterIdList);
                    }
                    clusterIdList.Add(displayID);                               // For each list add this displayID.
                }
                Log.NewEntry(LogLevel.Major, "ProcessCreatedForm: ClusterDisplay created. Requesting parameter values.");
                
                // Request all parameter values for my new display.
                // Since Display exists now, when response comes back, we have this display in the display list.
                foreach (IEngineContainer iEngineContainer in display.GetEngineContainers())
                {
                    string engineHubName = iengineHub.ServiceName;    // name of StrategyHub I am associated with.
                    foreach (EngineEventArgs e in EngineEventArgs.RequestAllParameters(engineHubName, iEngineContainer))
                    {
                        // TODO: Throttle requests we will make to StrategyHubs.
                        // 
                        display.AssocEngineHub.HubEventEnqueue(e);
                    }
                }
            }
        }//ProcessCreatedForm()
		//
        //
        // *********************************************************
        // ****             ProcessServiceEvent()               ****
        // *********************************************************
        //
        private void ProcessServiceEvent(Application.AppServiceEventArg anEventArg)
        {
            if (anEventArg.EventType == AppServiceEventType.ServiceAdded)
            {                
                AddEngineHub(anEventArg.ServiceName);
                //IService iService;
                //if (m_AppServices.TryGetService(anEventArg.ServiceName, out iService) && iService is IEngineHub)
                //    AddEngineHub(anEventArg.ServiceName);
            }
        }//ProcessServiceEvent()
		//
        //
        // *****************************************************************
        // ****                     UpdatePeriodic                      ****
        // *****************************************************************
        //
        protected override void UpdatePeriodic()
        {
            base.UpdatePeriodic();
            while (m_PendingRequests.Count > 0)
                HubEventEnqueue( m_PendingRequests.Dequeue() );

            // Invoke Regeration of Clusters periodically
            if (m_RegenerateCounter == 0)
            {
                DateTime dt = Log.GetTime();
                foreach (ClusterDisplay display in m_ClusterDisplays.Values)            // Update the clusters!
                    display.RegenerateNow(this, null);
                TimeSpan ts = Log.GetTime().Subtract(dt);
                //Log.NewEntry(LogLevel.Minor,"UpdatePeriod: Clusters regenerating took {0:0.000} ms.", ts.TotalMilliseconds);
                if (ts.TotalMilliseconds > 100)
                {                    
                    m_RegenerateCounter += m_RegenerateCounterMax;
                    m_RegenerateCounterMax += 2;
                }
                else
                    m_RegenerateCounterMax = 5;
            }
            else
                m_RegenerateCounter --;
            
        }//UpdatePeriodic()
		//
        private int m_RegenerateCounter = 0;
        private int m_RegenerateCounterMax = 5;
        //
        #endregion//HubEvent processing


        #region External Event Handlers
        // *****************************************************************
		// ****                     Event Handlers                      ****
		// *****************************************************************
		//
		//
		// ****				ClusterDisplay_FormClosing()			****
		//
		/// <summary>
		/// Called when the user closes a Display.  I will request it to be 
		/// removed from my list.
		/// Called by an external thread.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void ClusterDisplay_FormClosing(object sender, System.Windows.Forms.FormClosingEventArgs eventArgs)
		{
			if (sender.GetType() == typeof(ClusterDisplay))
			{
				FrontEndRequest request = new FrontEndRequest();
				request.Type = FrontEndRequest.Request.RemoveDisplay;
				request.ObjectList = new List<object>();
				request.ObjectList.Add(sender);
				HubEventEnqueue(request);
			}
		}//ClusterDisplay_FormClosing().
        //
		//
        //
        // ****                 EngineHub_ServiceStateChanged()                 ****
        // 
        /// <summary>
        /// Call back from EngineHubs that we are displaying.
        /// </summary>
        private void EngineHub_ServiceStateChanged(object sender, EventArgs eventArgs)
        {
            ServiceStateEventArgs e = (ServiceStateEventArgs) eventArgs;
            Log.NewEntry(LogLevel.Major, "EngineHub_ServiceStateChanged: {2} - State {0} -> {1}.", e.PreviousState, e.CurrentState, e.ServiceName);
            if ( (int) e.CurrentState >= (int) ServiceStates.Running)
            {
                this.AddEngineHub(e.ServiceName);
            }
        }//EngineHub_ServiceStateChanged()
        //
        //
		//
		//
        #endregion//Private Methods


        #region Private Classes EventArgs 
        // *****************************************************************
		// ****                     EventArgs	                        ****
        // *****************************************************************
        //
		private class FrontEndRequest : EventArgs
		{	
			//
			// ***			Members					***
			//
			public Request Type;
			public List<object> ObjectList;


			//
			// ***				Enum				***
			//
			public enum Request
			{
				AddService,
				RemoveDisplay,
				AddDisplay,
                Start,
				Stop
			}
			//
			// ***			Public methods			***
			//
			public override string ToString()
			{
				string s = String.Format("Request: {0}", Type.ToString());
				return s;
			}//ToString().
			//
		}// end class
        //
		//
		//
        #endregion//Event Handlers


        #region IStringifiable
        // *************************************************************
        // ****                     IStringifiable                  ****
        // *************************************************************
        string IStringifiable.GetAttributes()
        {
            return string.Empty;
        }
        List<IStringifiable> IStringifiable.GetElements()
        {
            return null;
        }
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
            bool isTrue;
            foreach (KeyValuePair<string, string> keyVal in attributes)
            {
                if (keyVal.Key.Equals("ShowLog") && bool.TryParse(keyVal.Value, out isTrue))
                    Log.IsViewActive = isTrue;
            }
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {
        }
        #endregion//IStringifiable


        #region IService
        // *********************************************************
        // ****                     IService                    ****
        // *********************************************************
        string IService.ServiceName
        {
            get { return m_HubName;}
        }
        void IService.Connect()
        {
            // Locate all IEngineHubs            
            m_AppServices.ServiceAdded += new EventHandler(HubEventEnqueue);
            m_AppServices.ServiceStopped += new EventHandler(HubEventEnqueue);
            foreach (IService iService in m_AppServices.GetServices())
            {
                if (iService is IEngineHub)
                {
                    Log.NewEntry(LogLevel.Minor, "Found EngineHub {0}", iService.ServiceName);
                    iService.ServiceStateChanged += new EventHandler(EngineHub_ServiceStateChanged);
                }
            }           
        }
        //void IService.RequestStop() - public method above.
        //public event EventHandler Stopping; - part of Hub base class.
        //
        //
        // 
        public event EventHandler ServiceStateChanged;
        //
        //
        private void OnServiceStateChanged()
        {
            Log.NewEntry(LogLevel.Major, "StateChanged {0}", m_ServiceState);
            if (this.ServiceStateChanged != null)
            {
                ServiceStateEventArgs e = new ServiceStateEventArgs(this,m_ServiceState,ServiceStates.None);
                this.ServiceStateChanged(this, e);
            }
        }//OnServiceStateChanged()
        //
        // 
        #endregion//IService


    }//end class
}
