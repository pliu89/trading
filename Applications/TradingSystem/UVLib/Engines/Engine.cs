using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;



using UV.Lib.IO.Xml;                    // IStringifiable

namespace UV.Lib.Engines
{
    using UV.Lib.FrontEnds.PopUps;
    using UV.Lib.FrontEnds.Huds;
    using UV.Lib.FrontEnds.GuiTemplates;     // new way to ecode guis
    using UV.Lib.BookHubs;
    // *********************************************************************
    // ****                     Engine Class                            ****
    // *********************************************************************
    /// <summary>
    /// This is the base class for all algorithmic components of a Strategy.
    /// Its wraps all parameters of the class, and creates parameter change messaging 
    /// for the object.
    /// Usage:
    /// 1) Class that have parameters to expose to GUIs (herein called Engines)
    ///     will extend this base class (or another Engine derivative, like PricingEngine).
    /// 2) ParameterInfo objects will be created for each public property of class.
    ///     ParameterInfo table created inside the "EngineInitialize" method.
    ///     GUI Controls are also created.
    /// 3) 
    /// Notes:
    /// 1. Properties defined in this class are not displayed in the control panel.
    ///     They are added to a list of properties to exclude.
    /// </summary>
    public abstract class Engine : IEngine , IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
		//
        // Basic Engine parameters  
        protected int m_EngineID = -1;								// unique engine ID (unique per EngineContainer).
        protected string m_EngineName = string.Empty;				// user-friendly engine name.
        protected List<ParameterInfo> m_PInfo;                      // list of parameter info.

		// Associated Guis
		//protected IEngineControl m_EnginePopUpControl = null;       // holder of popup control.
        //protected HudPanel m_HudPanel = null;                       // holder of hud panel. 
        protected List<EngineGui> m_EngineGuis = new List<EngineGui>();
        
		// Parameter update objects
        protected bool m_IsUpdateRequired = false;                  // flag for needed update.
		protected List<EngineEventArgs> m_EventQueue = new List<EngineEventArgs>(); // stores spontaneous events.

        private Hubs.LogHub m_Log = null;
        private IEngineContainer m_Parent = null;
        #endregion// members

        #region Constructor and Setup
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        /// <summary>
        /// This empty constructor provides support for Stringification.
        /// </summary>
        public Engine()
        {
            // Create a nice default name for this object. 
            //  Use the base class name and then specific name, like: "PricingEngine:Scalper"
            Type thisType = this.GetType();
            string s1 = thisType.Name;
            //string s2 = thisType.BaseType.Name;
            //if (s2.Equals("Engine", StringComparison.CurrentCultureIgnoreCase))
                m_EngineName = s1;
            //else
            //    m_EngineName = string.Format("{0}:{1}", s2, s1);
        } 
        //
        //
        // ************************************************
        // ****             SetupInitialize()          ****
        // ************************************************
        /// <summary>
        /// This is called immediately after construction, when we add this engine to its strategy.
        /// During this call the Parameter tables *must* be created, and all *Gui Templates* must be 
        /// created.
        /// If an Engine wants to create additional Engines, it should be done here.  Since *after*
        /// this initialize call (that is, during the SetupBegin() call), Engines can assume that all 
        /// other Engines have been created and added to the EngineList of the WStrategy.
        /// The short explanation:
        ///     Setup Initialize:   Strategy, StrategyHub and other ApplicationServices exist.
        ///     Setup Begin:        All Engines within my Strategy exist, and were SetupInitialize().
        ///     Setup Complete:     All Engines in all Strategies exist and were SetUpBegin().
        /// Note: 
        ///     1)  At this point, the Strategy has been created, but possibly no others.
        ///     2)  The other engines in the Strategy may also not be created at this point.
        ///         * Therefore, Engines should not look for other Strategies of other Engines.
        ///     3)  Subclasses should call this base-class method FIRST (so engineID is set) before other implementations.
        /// </summary>
        public void SetupInitialize(IEngineHub myEngineHub, IEngineContainer engineContainer, int engineID)
        {
            if (engineID >= 0)                                              // If engineID provide is positive, then 
                m_EngineID = engineID;                                      // accept ID provided by my parent Strategy. Otherwise leave it alone.
                                                                            // This is useful when its assigned at construction time already.
            m_Parent = engineContainer;
            SetupInitialize(myEngineHub, engineContainer, engineID, true);  // default is to creaet the default Gui templates.

        }// SetupInitialize()
        //
        //
        /// <summary>
        /// This should be overridden if the user does NOT want the default Gui Template.
        ///     1.) SetupGui = false will skip construction of the basic Engine Gui Template.
        /// </summary>
        protected virtual void SetupInitialize(IEngineHub myEngineHub, IEngineContainer engineContainer, int engineID, bool setupGui)
        {

            SetupParameterInfoTable(myEngineHub, engineContainer);      // Create table for all parameters
            if (setupGui)
                SetupGuiTemplates();

            if(myEngineHub is Hubs.Hub)
            {   // this is a real hub, and should have a log, which we want to use
                m_Log = ((Hubs.Hub)myEngineHub).Log;
            }

        }//SetupInitialize()
        //
        //
        // ************************************************
        // ****             SetupBegin()               ****
        // ************************************************
        /// <summary>
        /// This is called after construction, as we are adding it to a strategy when Strategy is added to StrategyHub.
        /// Note: 
        ///     1)  At this point, all Strategies have been created, along with their Engines, but each engine may 
        ///         be completely defined yet.
        ///     2)  Subclasses should not try to make linkages between them yet.  Connections to other engines 
        ///         and strategies should be done during SetupComplete() call.
        ///     3)  Subclasses should call this base-class method FIRST (so engineID is set).
        /// </summary>
        public virtual void SetupBegin(IEngineHub myEngineHub, IEngineContainer engineContainer)
        {
        }
        //
        //
        //
        // *****************************************************
        // ****             Setup Complete()                ****
        // *****************************************************
        /// <summary>
        /// Call this after all Strategy Engines have had "SetupBegin()" is called.
        /// Note:
        ///     1) 
        /// </summary>
        public virtual void SetupComplete() 
        { 
        }
        //
        //
        //
        //
        // *****************************************************
        // ****         Setup Parameter Info Table          ****
        // *****************************************************
        /// <summary>
        /// Using reflection, this function loops thru all Parameter fields of inheriting super class
        /// at construction time and creates an array of ParameterInfo objects.
        /// Notes:
        /// int ii = 0;  // this with a invoke member call is what we need to abstractize the Engine class.
        /// try:  .InvokeMember("", System.Reflection.BindingFlags.GetProperty or SetProperty, null, o, new object[] { this });  
        /// props[0].InvokeMember("", System.Reflection.BindingFlags.GetProperty or SetProperty, null, o, new object[] { this });  
        /// System.Reflection.PropertyInfo propInfo = props[0];
        /// propInfo.CanRead; //propInfo.CanWrite; //propInfo.Name;//propInfo.DeclaringType;//propInfo.PropertyType;
        /// bool isW = (bool)propInfo.GetValue(this, null);
        /// propInfo.SetValue(this, isW, null);
        /// </summary>
        private void SetupParameterInfoTable(IEngineHub hub, IEngineContainer engineContainer)
        {




            /*
            //
            // Create list of properties to NOT reveal. 
            //
            Type[] classesToNotReveal = new Type[] { typeof(IEngine), typeof(IPricingEngine) };
            List<string> propsToIgnore = new List<string>();
            foreach (Type aType in classesToNotReveal)
            {
                System.Reflection.PropertyInfo[] prList = aType.GetProperties();   // remove basic "Engine" properties
                foreach (PropertyInfo p in prList) { if (!propsToIgnore.Contains(p.Name)) { propsToIgnore.Add(p.Name); } }
            }

            //
            // Loop through MY propeties and save them.
            //
            System.Reflection.PropertyInfo[] propInfoArray = this.GetType().GetProperties();
            List<ParameterInfo> infoList = new List<ParameterInfo>();     // our internal list.
            foreach (System.Reflection.PropertyInfo propInfo in propInfoArray)
            {
                if (!propsToIgnore.Contains(propInfo.Name))    // I don't want base class properties to be shown
                {
                    ParameterInfo info = new ParameterInfo();
                    // properties of property
                    info.Name = propInfo.Name;
                    info.DisplayName = info.Name;               // default, but superclass should change.
                    info.IsReadOnly = !propInfo.CanWrite;
                    info.ValueType = propInfo.PropertyType;
                    info.ParameterID = infoList.Count;
                    // identification
                    info.EngineID = this.EngineID;
                    info.EngineContainerID = engineContainer.EngineContainerID;
                    info.EngineHub = hub;

                    // Add to our list 
                    infoList.Add(info);
                }
            }//next propInfo
            */ 
            this.m_PInfo = CreateParameterInfo(hub,engineContainer.EngineContainerID,this.EngineID,this.GetType());
        }//end SetupParameterInfoTable()
        //
        public static List<ParameterInfo> CreateParameterInfo(IEngineHub hub, int containerID, int engineID, Type engineType)
        {
            //
            // Create list of properties to NOT reveal. 
            //
            Type[] classesToNotReveal = new Type[] { typeof(IEngine), typeof(IPricingEngine) };
            List<string> propsToIgnore = new List<string>();
            foreach (Type aType in classesToNotReveal)
            {
                System.Reflection.PropertyInfo[] prList = aType.GetProperties();   // remove basic "Engine" properties
                foreach (PropertyInfo p in prList) { if (!propsToIgnore.Contains(p.Name)) { propsToIgnore.Add(p.Name); } }
            }

            //
            // Loop through MY propeties and save them.
            //
            System.Reflection.PropertyInfo[] propInfoArray = engineType.GetProperties();
            List<ParameterInfo> infoList = new List<ParameterInfo>();     // our internal list.
            foreach (System.Reflection.PropertyInfo propInfo in propInfoArray)
            {
                if (!propsToIgnore.Contains(propInfo.Name))    // I don't want base class properties to be shown
                {
                    ParameterInfo info = new ParameterInfo();
                    // properties of property
                    info.Name = propInfo.Name;
                    info.DisplayName = info.Name;               // default, but superclass should change.
                    info.IsReadOnly = !propInfo.CanWrite;
                    info.ValueType = propInfo.PropertyType;
                    info.ParameterID = infoList.Count;
                    // identification
                    info.EngineID = engineID;
                    info.EngineContainerID = containerID;
                    info.EngineHubName = hub.ServiceName;


                    // Add to our list 
                    infoList.Add(info);
                }
            }//next propInfo
            return infoList;
        }
        //
        //
        // *************************************************
        // ****         Setup Gui Templates()           ****
        // *************************************************
        /// <summary>
        /// Creates a collection of templates to describe guis associated 
        /// with this particular engine.
        /// </summary>
        protected EngineGui SetupGuiTemplates()
        {
            // Create the basic Engine Gui Template first
            
            EngineGui engineGui = new EngineGui();
            engineGui.EngineID = this.EngineID;
            engineGui.DisplayName = this.EngineName;
            engineGui.EngineFullName = this.GetType().FullName;
            engineGui.ParameterList.AddRange(this.m_PInfo);

            m_EngineGuis.Add(engineGui);
            return engineGui;
        }// SetupGuiTemplates()
        //
        //
        //
        //       
        #endregion//Constructors and Setup

        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        public int EngineID
        {
            get { return m_EngineID; }
            set { m_EngineID = value; }
        }
        //
        public string EngineName
        {
            get { return m_EngineName; }
        }
        //
        //
        // ****             IsReady             ****
        /// <summary>
        /// Override this IF the inheriting class has a non-trivial test to perform
        /// to ensure its ready to run.  Otherwise, this returns the trivial "Ready"
        /// response.
        /// </summary>
        //public virtual bool IsReady
        //{
        //    get { return true; }
        //}
        //
        // ****             IsUpdateRequired            ****
        //
		public virtual bool IsUpdateRequired
        {
            get { return m_IsUpdateRequired; }
            set { m_IsUpdateRequired = value; }
        }
		//

        //
		//
		//
        #endregion//Properties

        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        // *************************************************
        // ****     Market Instrument Initialized()     ****
        // *************************************************
        /// <summary>
        /// Called by the StrategyHub when our market instruments first become
        /// available.  If you extend the PricingEngine class, and override this
        /// method, call this base version first.
        /// </summary>
        /// <param name="marketBook"></param>
        /// <returns></returns>
        public virtual void MarketInstrumentInitialized(Book marketBook)
        {
        }
        #endregion // Public Methods

        #region defunct Control Management
        // *****************************************************************
        // ****              Control Management                         ****
        // *****************************************************************
        //public HudPanel GetHudPanel()
        //{
        //    return null;
        //}
        //public IEngineControl GetControl()
        //{
        //    return null;
        //}
        //
        // ****             GetControl()                ****
        //
        // <summary>
        // This implements part of IEngine.  It automatically creates a copy of the 
        // generic Pop Up control panel for displaying parameters of this engine.
        // </summary>
        // <returns></returns>
        /* 
        public IEngineControl GetControl()
        {
            if (m_EnginePopUpControl != null)
            {
                Type t = m_EnginePopUpControl.GetType();
                object p = null;
                p = t.InvokeMember("", System.Reflection.BindingFlags.CreateInstance, null, m_EnginePopUpControl, new object[] { this.EngineID, m_PInfo });
                return (IEngineControl)p;
            }
            else
                return null;
        }// GetControl().
        //
        //
        // ****             GetHudPanel()                ****
        //
        /// <summary>
        /// This implements part of IEngine.  Automatically creates a copy of the heads up panel 
        /// to display critical parameters.
        /// </summary>
        /// <returns></returns>
        public HudPanel GetHudPanel()
        {
            if (m_HudPanel != null)
            {
                Type t = m_HudPanel.GetType();
                object p = null;
                p = t.InvokeMember("", System.Reflection.BindingFlags.CreateInstance, null, m_HudPanel, new object[] { this.EngineID, m_PInfo });
                return (HudPanel)p;
            }
            else
                return null;
        }// GetControl().
        // 
        // This overload allows outsiders to reset and change EnginePanels.
        public HudPanel GetHudPanel(HudPanel newPanel)
        {
            m_HudPanel = newPanel;
            return newPanel;
        }
        */ 
        //
        //
        //
        //

        #endregion//Control Management

        #region Engine Event Processing
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************        
        //
        //
        //
        // ****             ProcessEvent()              ****
        //
        /// <summary>
        /// This method is the way that EngineEventArgs are passed to an engine to process.
        /// The action taken depends on the type of engine.  
        /// The implementation is required by IEngine; engines can override this implementation.  
        /// Current implementation:
        ///  1. This implementation is most common, for a Strategy engine that receives from outsiders
        ///  (normally user requests for parameter changes) to change its internal parameters.  
        ///     A. It tries to make the requested changes and respond to the caller the results.
        ///  2. If an engine overrides this with additional functionality, this method can still be 
        ///  called to process all EngineEventArgs events, it does nothing and returns when passed
        ///  any other type of event.
        /// </summary>
        /// <param name="e">Request passed in via an EngineEventArg.</param>
        public virtual void ProcessEvent(EventArgs e)
        {
            if (e.GetType() != typeof(EngineEventArgs)) return; // here is processing of EngineEventArgs only!
            EngineEventArgs eArgs = (EngineEventArgs) e;
            
            EngineEventArgs.EventType requestType = eArgs.MsgType;
            EngineEventArgs.EventStatus requestStatus = eArgs.Status;
            // Validate this message
            if (requestStatus != EngineEventArgs.EventStatus.Request)
				return;// I ignore all but requests.
            // 
            // Process the request.
            //
            switch (requestType)
            {
                    //
                    // ****         Parameter Value Request            ****
                    //
                case EngineEventArgs.EventType.ParameterValue:
                    // This is a request to broadcast the values of a single, multiple or all parameters.
                    // eArgs.DataIntA contains parameter IDs for each parameter requested, or
                    // eArgs.DataIntA = null, if all are requested.
                    eArgs.DataObjectList = new List<object>();
                    if (GetParameterValue(ref eArgs.DataIntA, ref eArgs.DataObjectList))
                        eArgs.Status = EngineEventArgs.EventStatus.Confirm; // found parameters
                    else
                        eArgs.Status = EngineEventArgs.EventStatus.Failed;  // did not find parameters
                    break;
                    //
                    // ****         Parameter Change Request            ****
                    //
                case EngineEventArgs.EventType.ParameterChange:
                    // This is a request to change the value of one or more parameters.
                    if (eArgs.DataIntA == null)
                    {   // Here, cannot blindly change all parameter values, this should NOT be null.
                        eArgs.Status = EngineEventArgs.EventStatus.Failed;
                    }
                    else
                    {   // eArgs.DataIntA contains list of parameter IDs that user wants changed.
                        // eArgs.DataObjectList contains new values desired.
                        bool logEnabled = false;
                        if (m_Log!=null)
                            logEnabled = m_Log.BeginEntry(Hubs.LogLevel.Major, "ProcessEvent: {2} {0} #{1} parameter change request: ", m_EngineName, m_EngineID, 
                                (m_Parent!= null ? string.Format("{0} #{1}",m_Parent.EngineContainerName,m_Parent.EngineContainerID) : string.Empty));
                        for (int i = 0; i < eArgs.DataIntA.Length; ++i) //ith parameter in the list inside the event.
                        {
                            int pID = eArgs.DataIntA[i];                // propertyID of parameter
                            ParameterInfo pInfo = m_PInfo[pID];          
                            PropertyInfo property = this.GetType().GetProperty(pInfo.Name);
                            if (logEnabled)
                                m_Log.AppendEntry("[{0}, requested value={1},", pInfo.Name, eArgs.DataObjectList[i]);

                            if (property.CanWrite)
                            {   // If writable, try to set value of this parameter in *this* object.
								try
								{
									property.SetValue(this, eArgs.DataObjectList[i], null);
								}
								catch(Exception)
								{
								}
                            }
                            // Regardless of result, get the current value
                            object o = property.GetValue(this, null);
                            eArgs.DataObjectList[i] = o;
                            if (logEnabled)
                                m_Log.AppendEntry(" final value={0}] ", eArgs.DataObjectList[i]);
                        }
                        if (logEnabled)
                            m_Log.EndEntry();
                        // Set status to confirm signifying that the values in the message 
                        // are the correct, confirmed values from the engine.
                        eArgs.Status = EngineEventArgs.EventStatus.Confirm;
                        this.IsUpdateRequired = true;   // signal that we need updating!
                    }
                    break;
                default:
                    eArgs.Status = EngineEventArgs.EventStatus.Failed;
                    break;
            }
        }//end ProcessEngineEvent()
        //
        //
        //
        //
        // ****                 Add Spontaneous Events()            ****
        //       
        /// <summary>
        /// The Engine class that inherits this base class, can spontaneously
        /// add messages based on its state changes etc, and periodically the 
        /// StrategyHub thread will call this, requesting that all events queued up
        /// in the EventQueue be added its "eventList" which is passed as an argument.
        /// Finally, the StrategyHub is responsible for sending these to EngineEvent subscribers.
        /// Notes:
        ///     1. Overriding this method should call this base method first, which will 
        ///     purge the queued events first.  New events might then be added to the StrategyHub
        ///     eventList (rather than the just purged m_EventQueue).
        /// </summary>
        /// <param name="eventList"></param>
        public virtual void AddSpontaneousEngineEvents(List<EngineEventArgs> eventList)
        {
            if (m_EventQueue.Count > 0) 
            { 
                eventList.AddRange(m_EventQueue);
                m_EventQueue.Clear();
            }
        }// AddSpontaneousEngineEvents
        //
        //
        // 
        //
        // ****         GuiTemplates            ****
        //
        /// <summary>
        /// New way to implement controls for Engines is via these
        /// Gui templates that define guis.
        /// </summary>
        /// <returns></returns>
        public List<EngineGui> GetGuiTemplates()
        {
            List<EngineGui> engineGuiList = new List<EngineGui>();
            engineGuiList.AddRange(this.m_EngineGuis);
            return engineGuiList;
        }
        //
        //
        // ****         To String()         ****
        //
        // <summary>
        // This simplifies life while debugging.
        // </summary>
        // <returns></returns>
        //public string ToLongString()
        //{
		//	return (string.Format("{0}",GetParameterValue(false)));
        //}

        //
        //
        //
        // ****         GetParameterValue()         ****
        //
        /// <summary>
        /// Returns a string-serialization of the Engine parameters for saving.
        /// </summary>
        /// <param name="ignoreReadOnlyFields">true will not return read-only parameters.</param>
        /// <returns>a string of parameter name, value pairs.</returns>
        public string GetParameterValue(bool ignoreReadOnlyFields)
        {
            string result = String.Empty;
            int[] paramID = new int[0];
            List<object> paramValue = null;
            if (GetParameterValue(ref paramID, ref paramValue) && paramID.Length > 0)
            {
                StringBuilder aLine = new StringBuilder();
                aLine.AppendFormat("{0}", this.EngineName);   // engine name
                int pid =  paramID[0];
                //aLine.AppendFormat(",{0},{1}", m_PInfo[pid].Name, paramValue[0].ToString());
                for (int i = 0; i < paramID.Length; ++i)
                {
                    pid = paramID[i];
                    Type typeOfParameter = m_PInfo[pid].ValueType;
                    if (  ! (ignoreReadOnlyFields && m_PInfo[pid].IsReadOnly) )
                    {
                        if (typeOfParameter.IsPrimitive)    // this is easy to print - implement more complex types.
                            aLine.AppendFormat(",{0},{1}", m_PInfo[pid].Name, paramValue[i].ToString());
                    }
                }
                result = aLine.ToString();
            }
            return result;
        }// GetParameterValue()
        //
        //
        //
        //
        // 
        // ****           Get Event For Parameter Values                ****
        // 
        /// <summary>
        /// This method allows an outsider to ask the Engine to automatically create 
        /// the appropriate EngineEventArg that can be used to have it broadcast its 
        /// current parameter values. 
        /// This is useful when the Engine's parameters have been changed internally 
        /// by some method, orther than the usual EngineEventArg parameter Change request, 
		/// which will usually produce a response event automatically anyway.
		/// For example, use this when an engine parameter spontaneously changes (from
		/// a market event, or fill, or by the action of a model) to send the new parameter
		/// values to all GUIs, even though no one requested one.
        /// </summary>
        public virtual EngineEventArgs GetEventForParameterValues( IEngineContainer parent) 
        {
            EngineEventArgs newEvent = new EngineEventArgs();
            newEvent.MsgType = EngineEventArgs.EventType.ParameterValue;
			newEvent.Status = EngineEventArgs.EventStatus.Request;
			newEvent.EngineContainerID = parent.EngineContainerID;
            newEvent.EngineID = this.EngineID;
            return newEvent;
        }// GetEventForParameterValues();
		//
		//
		public virtual EngineEventArgs GetEventForParameterValues(IEngineContainer parent, int propertyID)
		{
			EngineEventArgs newEvent = new EngineEventArgs();
			newEvent.MsgType = EngineEventArgs.EventType.ParameterValue;
			newEvent.Status = EngineEventArgs.EventStatus.Request;
			newEvent.EngineContainerID = parent.EngineContainerID;
			newEvent.EngineID = this.EngineID;
			newEvent.DataIntA = new int[] { propertyID };
			return newEvent;
		}// GetEventForParameterValues();
        //
		//
		/// <summary>
		/// This method allows an engine to broadcast all of his own parameters even if not
		/// requested by the user.  This is useful when the engine changed its parameters automatically, 
		/// and now wants to ensure the user knows about it.
		/// </summary>
		/// <param name="hub"></param>
		/// <param name="parent"></param>
		public void BroadcastAllParameters(IEngineHub hub, IEngineContainer parent)
		{
			EngineEventArgs eArgs = new EngineEventArgs();
			eArgs.MsgType = EngineEventArgs.EventType.ParameterValue;
			eArgs.Status = EngineEventArgs.EventStatus.Confirm;
            eArgs.EngineHubName = hub.ServiceName;
			eArgs.EngineContainerID = parent.EngineContainerID;
			eArgs.EngineID = this.EngineID;
			if ( GetParameterValue(ref eArgs.DataIntA, ref eArgs.DataObjectList) )
				m_EventQueue.Add(eArgs);
		}// BroadcastParameters()
        //
        //
        /// <summary>
        /// This methods allows an engine to broadcast a single parameter that has not been requested by the user
        /// If a parameter is changed internally (programatically) this can be used to transmit to the gui.
        /// </summary>
        /// <param name="hub"></param>
        /// <param name="parent"></param>
        /// <param name="propertyId"></param>
        public void BroadcastParameter(IEngineHub hub, IEngineContainer parent, int propertyId)
        {
            EngineEventArgs eArgs = new EngineEventArgs();
            eArgs.MsgType = EngineEventArgs.EventType.ParameterValue;
            eArgs.Status = EngineEventArgs.EventStatus.Confirm;
            eArgs.EngineHubName = hub.ServiceName;
            eArgs.EngineContainerID = parent.EngineContainerID;
            eArgs.EngineID = this.EngineID;
            eArgs.DataIntA = new int[] { propertyId };
            eArgs.DataObjectList = new List<object>();
            if (GetParameterValue(ref eArgs.DataIntA, ref eArgs.DataObjectList))
                m_EventQueue.Add(eArgs);
        }
		//
        //
        public void BroadcastParameter(IEngineHub hub, IEngineContainer parent, string parameterName)
        {
            int id = GetParameterId(parameterName);
            if (id > -1)
                BroadcastParameter(hub, parent, id);
        }
        //
        //
        // ****				Get Parameter Info()			****
		//
		public virtual ParameterInfo GetParameterInfo(string parameterName)
		{
			ParameterInfo pInfo = null;
			foreach (ParameterInfo p in m_PInfo)
			{
				if (p.Name.Equals(parameterName))
				{
					pInfo = p;
					break;
				}
			}
			// Exit.
			return pInfo;
		}//GetParameterInfo()
		//
		//
		//
		// ****				Get Property Id()			****
		/// <summary>
		/// 
		/// </summary>
        /// <param name="parameterName"></param>
		/// <returns>PropertyId value, else -1 if no property has provided name.</returns>
		public virtual int GetParameterId(string parameterName)
		{
			ParameterInfo pInfo = GetParameterInfo(parameterName);
			if (pInfo == null)
				return -1;
			else
				return pInfo.ParameterID;
		}//end GetPropertyId();
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
        // ****             Get Parameter Value             ****
        //
        /// <summary>
        /// Given an array of parameterIDs and a list, the current values of the parameters
        /// are loaded into the list.  IF!! the array parameterID[] is empty or a null, then
        /// all parameterIDs and their values are returned.
        /// </summary>
        /// <param name="parameterID"></param>
        /// <param name="valueList"></param>
        /// <returns>true if successfully found parameter values.</returns>
        protected bool GetParameterValue(ref int[] parameterID, ref List<object> valueList)
        {
            if (parameterID != null && parameterID.Length > 0)
            {   // Request is for a specific parameter value.
                // Even if we fail for a specific ID, need to insert null into list to make it correct length.
                for (int i = 0; i < parameterID.Length; ++i)
                {
                    int paramID = parameterID[i];
                    if (paramID >= 0 && paramID < m_PInfo.Count)
                    {
                        ParameterInfo pInfo = m_PInfo[paramID];
                        PropertyInfo property = this.GetType().GetProperty(pInfo.Name);
                        if (property.CanRead)
                        {
                            object o = property.GetValue(this, null);
                            valueList.Add(o);
                        }
                        else                        
                            valueList.Add(null);            // need entry in list to make it one-to-one with parameterID array.                       
                    }
                    else
                        valueList.Add(null);    
                }
                return true;
            }
            else
            {   // Request is for all paramter values.
                valueList = new List<object>();
                parameterID = new int[m_PInfo.Count];
                for (int i = 0; i < m_PInfo.Count; ++i)
                {
                    ParameterInfo pInfo = m_PInfo[i];
                    PropertyInfo property = this.GetType().GetProperty(pInfo.Name);
                    if (property.CanRead)
                    {
                        object o = property.GetValue(this, null);
                        valueList.Add(o);
                        parameterID[i] = i;
                    }
                }
                return true;
            }
        }// GetParameterValue()
        //
		//
		//
        //
        #endregion//Private Methods

        #region IStringifiable interface 
        public virtual string GetAttributes()
        {
            if (m_EngineID < 0)
                return string.Empty;
            else
                return string.Format("EngineId={0}", m_EngineID);
        }

        public virtual List<IStringifiable> GetElements()
        {
            return null;
        }

        public virtual void SetAttributes(Dictionary<string, string> attributes)
        {
            int n = 0;
            foreach (KeyValuePair<string, string> attr in attributes)
            {
                if (attr.Key == "EngineId" && int.TryParse(attr.Value, out n))
                    this.m_EngineID = n;
            }
        }

        public virtual void AddSubElement(IStringifiable subElement)
        {
            
        }
        #endregion//IStringifiable



    }//end class
}

