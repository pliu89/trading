using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UV.Lib.Hubs;
using UV.Lib.Engines;
using UV.Lib.BookHubs;
using UV.Lib.FrontEnds.Clusters;

namespace UV.Lib.FrontEnds
{
    public class FrontEndHub : Hub
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        //
        //
        // Lists of gui objects to maintain:
        //
        private object NextClusterDisplayIDLock = new object(); // lock used for all: "Next...ID" changes.
        private int m_NextClusterDisplayID = 0;                 // ensures that all owned guis have unique IDs.  
        public int currentClusterDisplayTurn = -1;
        public int currentClusterDisplayNumber = -1;
        private Dictionary<int, ClusterDisplay> m_ClusterDisplay = new Dictionary<int,ClusterDisplay>();// Only the hub thread is allowed to change this list.


        // Lists of Hubs to which some ClusterDisplays are subscribed.  Displays can subscribe to multiple hubs.        
        private Dictionary<BookHub, List<ClusterDisplay>> m_Subscriptions = new Dictionary<BookHub, List<ClusterDisplay>>();
        private Dictionary<IEngineHub, List<ClusterDisplay>> m_EngineSubscriptions = new Dictionary<IEngineHub, List<ClusterDisplay>>();

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        /// <summary>
        /// Main constructor.
        /// </summary>
		public FrontEndHub(string logDirectoryName)
            : base("FrontEnd", logDirectoryName, true, LogLevel.ShowAllMessages)
        {

        }//end constructor
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
        /// <summary>
        /// A new display is made and passed to this hub for management.
        /// Called by an external thread.
        /// </summary>
        /// <param name="aHub"></param>
        public void NewDisplay(ClusterDisplay newDisplay)
        {   
            lock (NextClusterDisplayIDLock)
            {   // Get next id number available.
                newDisplay.ID = m_NextClusterDisplayID;
                newDisplay.prevGuiTurns = currentClusterDisplayNumber;
                m_NextClusterDisplayID += 1;
            }
			newDisplay.FormClosing += new System.Windows.Forms.FormClosingEventHandler(Display_FormClosing);
            DisplayArgs args = new DisplayArgs();
            args.display = newDisplay;
            args.Request = DisplayArgs.DisplayRequest.NewDisplay;
            HubEventEnqueue(args);
        }//NewDisplay().
        //
        public override void RequestStop()
        {
            throw new NotImplementedException();
        }
        //
        //
        #endregion//Public Methods


        #region HubEvent Handler 
        // *****************************************************************
        // ****                 Private HubEvent Methods                ****
        // *****************************************************************
        //
        //
        // ****                     Hub Event Handler               ****
        //
        /// <summary>
        /// Main request handling routine.
        /// Called only by the internal hub thread.
        /// </summary>
        /// <param name="e"></param>
        protected override void HubEventHandler(EventArgs[] eArgList)
        {

            foreach (EventArgs eArg in eArgList) // process each event
            {
				Log.NewEntry(LogLevel.Minor, eArg.ToString());
                Type eArgType = eArg.GetType();
                if (eArgType == typeof(FrontEndHub.DisplayArgs))
                {   // ****************************************
                    // ****         Display Events         ****
                    // ****************************************
                    // These are my internal requests, used to create new GUI displays, etc.
                    FrontEndHub.DisplayArgs e = (FrontEndHub.DisplayArgs)eArg;                    
                    switch (e.Request)
                    {
                        case DisplayArgs.DisplayRequest.NewDisplay:
                            //
                            // ***          New Display         ***
                            //
                            // When a new display is requested (to be displayed from this hub), a request is pushed
                            // onto the queue and comes here.  this way the new ClusterDisplay can be created and
                            // added to the list without the need to lock the list (only the hub thread ever touches
                            // this list.)
                            IEngineHub engineHub = e.display.AssocEngineHub;    // hub whose elements we want to display
                            List<ClusterDisplay> displayList;                   // displays already associated with element hub
                            if (!m_EngineSubscriptions.TryGetValue(engineHub, out displayList))
                            {   // First time we are creating a display for this hub. There is no existing list.
                                displayList = new List<ClusterDisplay>();       // create new list.
                                m_EngineSubscriptions.Add(engineHub, displayList);
                                engineHub.EngineChanged += new EventHandler(this.HubEventEnqueue);
                            }
                            ClusterDisplay newDisplay = e.display;              // display created by event creator.
                            displayList.Add(newDisplay);
                            m_ClusterDisplay.Add(newDisplay.ID, newDisplay);    // the ID is made by original caller 
                            // Now request a complete list of the controls for engines from hub.                                                 
                            engineHub.HubEventEnqueue(Engines.EngineEventArgs.RequestAllControls(newDisplay.ID -newDisplay.prevGuiTurns));//newDisplay.ID));
                            break;
                        case DisplayArgs.DisplayRequest.RemoveDisplay:
                            //
                            // ***          Remove Display          ***
                            //
                            ClusterDisplay displayToRemove = e.display;
                            if (displayToRemove != null)
                            {   // Remove any book subscriptions
                                List<object> hubsToRemove = new List<object>();
                                foreach (BookHub hub in m_Subscriptions.Keys)
                                {
                                    if ( m_Subscriptions[hub].Contains(displayToRemove) ) 
                                    { 
                                        m_Subscriptions[hub].Remove(displayToRemove);
                                        if (m_Subscriptions[hub].Count == 0)
                                        {
                                            hub.InstrumentChanged -= this.HubEventEnqueue;
                                            hubsToRemove.Add(hub);
                                        }
                                    }                                
                                }
                                foreach (object hub in hubsToRemove) { m_Subscriptions.Remove( (BookHub) hub); }
                                // Remove engine subscriptions
                                foreach (IEngineHub hub in m_EngineSubscriptions.Keys)
                                {
                                    if (m_EngineSubscriptions[hub].Contains(displayToRemove))
                                    {
                                        m_EngineSubscriptions[hub].Remove(displayToRemove);
                                        if (m_EngineSubscriptions[hub].Count == 0)
                                        {
                                            hub.EngineChanged -= this.HubEventEnqueue;
                                            hubsToRemove.Add(hub);
                                        }
                                    }
                                } 
                                foreach (object hub in hubsToRemove) { m_EngineSubscriptions.Remove((IEngineHub)hub); } 
                                m_ClusterDisplay[displayToRemove.ID] = null;    // dump my pointer to it.
                            }
                            break;
                        default:
                            //
                            // ***          default error       ***
                            //
                            Log.NewEntry(LogLevel.Error,"Unknown DisplayArg Request.");
                            break;
                    }
                }
                else if ( eArgType == typeof(EngineEventArgs) )
                {   // *****************************************************
                    // *****            Process Engine Events           ****
                    // *****************************************************
                    EngineEventArgs e = (EngineEventArgs)eArg;
                    // Pass event to all ClusterDisplays subscribed to event-generating hub.
                    //  
                    List<ClusterDisplay> clusterDisplayList;    
                    if (m_EngineSubscriptions.TryGetValue(e.EngineHubResponding, out clusterDisplayList))
                    {   // at least some displays are subscribed to this hub.                                                
                        for (int i = 0 ; i<clusterDisplayList.Count; ++i )  
                        {
                            ClusterDisplay display = clusterDisplayList[i];
                            //List<IEngineContainer> containers = display.GetEngineContainers();  // get containers in this hub.       
                            //Dictionary<int, IEngineContainer> containersDict = display.GetEngineContainersDictionary();
                            //IEngineContainer engineContainer11;
                            if (e.EngineContainerID < 0)
                            {   // EngineContainer ID < 0 => event is meant for all containers in hub.
                                // Such events are passed to all Stratgies or Clusters, etc.
                                if (e.MsgType == EngineEventArgs.EventType.GetControls)
                                {   // Create the cluster controls for this display.
                                    // This request is made by ClusterDisplays (to the Hub they are displaying) after 
                                    // they have been created, but not completely initialized.  
                                    // Now proceed through our list of ClusterDisplays and only the first uninitialized 
                                    // display we find will have the controls inside this eventarg passed to it.
                                    bool isNewDisplay = (e.Status == EngineEventArgs.EventStatus.Confirm) &&
                                        (! display.IsInitialized);
                                    if (isNewDisplay)
                                    {    
                                        display.HubEventEnqueue(e); // This is a non-asynchronous call, display is initialized immediately.
                                        // Now request all parameter updates for new display.
                                        List<EngineEventArgs> newRequestList = EngineEventArgs.RequestAllParameters((display.ID - display.prevGuiTurns),display);//(i,display);
                                        foreach (EngineEventArgs newRequest in newRequestList)
                                        {
                                            e.EngineHubResponding.HubEventEnqueue(newRequest);
                                        }
                                        break;  // only initialize one display per GetControls event. The first uninitialized display gets initialized.
                                    }
                                }
                                else
                                {   // Every other non-GetControls event for "all clusters" processed here.
                                    // Right now, there are none of such terms.
									List<IEngineContainer> containers = display.GetEngineContainers();  // get containers in this hub.       
                                    foreach (IEngineContainer container in containers) { container.ProcessEngineEvent(e); }
                                }
                            }
							else
							{	// A specific containerID was provided. 
								// Pass along this event directly to the specific engineContainer.
								Dictionary<int, IEngineContainer> containersDict = display.GetEngineContainersDictionary();
								IEngineContainer engineContainer;
								if (containersDict.TryGetValue(e.EngineContainerID, out engineContainer))
								{
									engineContainer.ProcessEngineEvent(e);
								}
								else
								{	// Failed to find enginecontainer!
									Log.NewEntry(LogLevel.Error, "Received event for unknown engineContainerID={0}.", e.EngineContainerID);
								}
							}                                                      
                            display.RegenerateNow(this,null);   // tells the display to repaint, if needed.
                        }
                    }
                }
                else
                {   //
                    // ****         Unrecognized Event          ****
                    //
                    Log.NewEntry(LogLevel.Error, "Unknown event type: {0}" , eArgType.ToString());
                    eArg.GetType();
                }            
            }//next event arg
        }//HubEventHandler()
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
		//
		// ****             Display_FormClosing()                  ****
		//
		/// <summary>
		/// Since this display is closing, lets request to remove it 
		/// from receiving events in future.
		/// This should be called by a display that is closing.
		/// Called by external thread.
		/// </summary>
		private void Display_FormClosing(object sender, System.Windows.Forms.FormClosingEventArgs eventArg)
		{
			if (sender.GetType() == typeof(ClusterDisplay))
			{
				DisplayArgs args = new DisplayArgs();
				args.display = (ClusterDisplay)sender;
				args.Request = DisplayArgs.DisplayRequest.RemoveDisplay;
				HubEventEnqueue(args);
			}
		}// Display_FormClosing().
        //
        #endregion//Event Handlers



        #region My HubEvent Args
        // *****************************************************************
        // ****                    HubEvent Args                        ****
        // *****************************************************************
        //
        //
        // ****             DisplayArgs             ****
        //
        public class DisplayArgs : EventArgs
        {
            public ClusterDisplay display = null;
            public DisplayRequest Request = DisplayRequest.None;



            public enum DisplayRequest
            {
                NewDisplay,
                RemoveDisplay,
                None
            }
			// ****				ToString()			****
			public override string ToString()
			{
				return string.Format("DisplayArgs: {0} {1}", this.Request.ToString(),this.display.ToString() );
			}
        }//end class HubEventArgs
        //
        //
        //
        //
        //
        #endregion// HubEvent Args





    }//end class
}
