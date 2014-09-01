using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace UV.Lib.FrontEnds.Clusters
{
    using UV.Lib.FrontEnds.PopUps;
    using UV.Lib.Engines;

    public partial class Header : UserControl
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // Header identification.
        public IEngineContainer m_ParentCluster = null;
        private int m_EngineContainerID = -1;                                               // Strategy/Cluster ID.   
        public bool IsRegenerateRequired = false;

        // My controls 
        private List<IEngine> m_EngineControlList = new List<IEngine>();
        private Dictionary<string,IPopUp> m_PopupList = new Dictionary<string,IPopUp>();            // complete list of popups.
        private Dictionary<int, List<string>> m_EngineIDToPopups = new Dictionary<int, List<string>>();   // Mapping of engineID to list of assoc popups

        
        //private ConcurrentDictionary<int, IPopUp> m_PopUpList = new ConcurrentDictionary<int, IPopUp>();        // list of popups by EngineID
        // My internal layout
        private System.Drawing.Point m_NextMenuButtonLoc = new System.Drawing.Point(228, 0);// location of first button

        #endregion// members

        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        /// <summary>
        /// Constructor for the Header in a Cluster object.   This is called from 
        /// a Cluster constructor and therefore by a GUI thread.  
        /// The Cluster constructor is called using reflection but uses an invoke on the
        /// GUI thread. 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="container">The container (think Strategy) associated with this Cluster.</param>
        public Header(IEngineContainer parent, GuiTemplates.EngineContainerGui container)
        {
            m_ParentCluster = parent;                   // Cluster in which I belong.
            if ( m_ParentCluster is Cluster)
            {
                this.ContextMenuStrip = ((Cluster)m_ParentCluster).ClusterContextMenuStrip;
            }

            InitializeComponent();

            // Initialize Header - unique in each Cluster, 1-to-1 with EngineContainer.
            this.txtName.Text = parent.EngineContainerName;
            m_EngineContainerID = parent.EngineContainerID;

            // Create controls (per engine) for this EngineContainer.
            this.SuspendLayout();
            List<GuiTemplates.EngineGui> engineList = container.m_Engines;
            foreach (GuiTemplates.EngineGui engineGui in engineList)
            {

                // Create Popup displays.
                Control newControl = null; 
                Type guiType = typeof(EngineControl);
                if (UV.Lib.IO.Xml.Stringifiable.TryGetType(engineGui.HeaderControlFullName, out guiType))
                {   // We have found the desired control to create.
                    //if (Utilities.GuiCreator.TryCreateControl(out newControl, guiType, engineGui.EngineID, engineGui.ParameterList))
                    if (Utilities.GuiCreator.TryCreateControl(out newControl, guiType, engineGui))
                    {   // We have successfully created the desired control.
                        m_EngineControlList.Add((IEngine)newControl);

                        
                        // Create unique name for popup.
                        string uniqueName = string.Format("{0}",engineGui.DisplayName);     // starting name.
                        if (m_PopupList.ContainsKey(uniqueName))
                        {
                            int n = 0;
                            while (m_PopupList.ContainsKey(uniqueName))
                                uniqueName = string.Format("{0} {1}", engineGui.DisplayName,n++);// new name with additional index.
                        }
                        // Create a popup window to contain control.
                        PopUp1 popup = new PopUps.PopUp1(this);                             // load any IPopUp object here!                                            
                        //popup.Title = uniqueName;                        
                        popup.Title = engineGui.DisplayName;
                        popup.Text = m_ParentCluster.EngineContainerName;	                // Text on the top of Window.
                        popup.AddControl((IEngineControl)newControl);
                        // Add this new popup to my lists.
                        m_PopupList.Add(uniqueName, popup);                                 // unique popupname to popup form.
                        if ( ! m_EngineIDToPopups.ContainsKey(engineGui.EngineID) )
                            m_EngineIDToPopups.Add(engineGui.EngineID,new List<string>());  // create mapping for engineID -> popup names.
                        m_EngineIDToPopups[engineGui.EngineID].Add(uniqueName) ;            // store each popup name.                        

                        // Create a new buttons to open/close this new popup.
                        this.comboBoxEngines.Items.Add( uniqueName );
                    }
                }
                

            }//next engine
            this.ResumeLayout(false);
        }//constructor
        //
        //
        /*
         * // defunct
        public Header(IEngineContainer container)
        {
            m_ParentCluster = container;
            InitializeComponent();

            // Initialize Header - unique in each Cluster, 1-to-1 with EngineContainer.
            this.txtName.Text = container.EngineContainerName;
            m_EngineContainerID = container.EngineContainerID;

            // Create controls (per engine) for this EngineContainer.
            this.SuspendLayout();
            List<IEngine> engineList = container.GetEngines();
            foreach (IEngine engine in engineList)
            {
                IEngineControl pControl = engine.GetControl();          // Get popup insert control for this engine.
                m_EngineControlList.Add((IEngine)pControl);              // allow "null" if the engine lacks a control,
                if (pControl != null)                                   // each engine has entry here, even if its null. (but why?)
                {   // Create a popup control
                    PopUp1 popup = new PopUps.PopUp1(this);             // load any IPopUp object here!
                    //popup.Title = string.Format("{0} {1}",container.EngineContainerName,engine.EngineName);
                    popup.Title = string.Format("{0}", engine.EngineName);// Text on small, custom top blue bar.
                    popup.Text = m_ParentCluster.EngineContainerName;	// Text on the top of Window.
                    popup.AddControl(pControl);
                    int engineID = ((IEngine)pControl).EngineID;
                    m_PopUpList.TryAdd(engineID,popup);                 // look up table for processing events.                    
                    //Label button = CreateMenuButton(engine);          // Create a button to get popup.
                }
                // new buttons
                comboBoxEngines.Items.Add(string.Format("{0}: {1}", engine.EngineID.ToString(), engine.EngineName));
                //comboBoxEngines.Items.Add(engine.EngineName);

            }//next engine
            this.ResumeLayout(false);
        }//constructor
        */
        //
        //
        //       
        #endregion//Constructors

        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        // ****         Process EngineEvent ()          ****
        //
        /// <summary>
        /// Called from cluster to process engine events it received from StrategyHub.
        /// </summary>
        /// <param name="eArgs"></param>
        public bool ProcessEngineEvent(EngineEventArgs eArgs)
        {
            bool regenerateNow = false;
            List<string> popUpNames = null;
            if (m_EngineIDToPopups.TryGetValue(eArgs.EngineID, out popUpNames))
            {
                foreach(string popUpName in popUpNames)
                {
                    IPopUp popUp = m_PopupList[popUpName];
                    IEngine engineControl = (IEngine)popUp.CustomControl;
                    
                    engineControl.ProcessEvent(eArgs);
                    //if (popUpName.Contains("Pricing"))
                    //    regenerateNow = popUp.Visible || regenerateNow;                    
                    IsRegenerateRequired = IsRegenerateRequired || engineControl.IsUpdateRequired;
                    bool isEngineUpdateDesired = engineControl.IsUpdateRequired && popUp.Visible && (engineControl is FrontEnds.Graphs.ZGraphControl)==false;
                    //regenerateNow = engineControl.IsUpdateRequired || regenerateNow;
                    regenerateNow = isEngineUpdateDesired || regenerateNow;                    
                    //Form form = (Form)popUp;
                    //if (popUp.Visible && form.WindowState != FormWindowState.Minimized)
                    //{
                    //       popUp.CustomControl.Regenerate(this, null);
                    //}
                }
            }
            //IsRegenerateRequired = regenerateNow || IsRegenerateRequired;
            return regenerateNow;
        }//ProcessEngineEvent()        
        //
        //
        //
        public List<IEngine> GetEngines() { return m_EngineControlList; }
        //
        //
        /*
        public IPopUp[] GetPopUps()
        {
            IPopUp[] popUps = new IPopUp[m_PopUpList.Count];
            m_PopUpList.Values.CopyTo(popUps, 0);
            return popUps;
        }
        */ 
        //
        public void RegenerateNow()
        {
            foreach (IPopUp popUp in m_PopupList.Values)
            {
                if (popUp.Visible)
                    popUp.CustomControl.Regenerate(this, null);
            }
            IsRegenerateRequired = false;
        }//RegenerateNow()  
        //
        #endregion//Public Methods


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        // ****         CreateMenuButton            ****
        //
        /// <summary>
        /// This creates and places a small click-able label used as a menu entry
        /// to have access to a paritcular engine parameter control panel.
        /// </summary>
        /*
        private Label CreateMenuButton(IEngine engine)
        {
            Label label = new System.Windows.Forms.Label();

            label.Anchor = System.Windows.Forms.AnchorStyles.Right;
            label.BackColor = System.Drawing.Color.Transparent;
            label.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            //label.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            label.Font = new System.Drawing.Font("Segoe Condensed", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            //this.bParameter.Font = new System.Drawing.Font("Wingdings", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
            label.ForeColor = System.Drawing.Color.LightGray;

            label.Margin = new System.Windows.Forms.Padding(0);
            label.Name = engine.EngineID.ToString();                // use button name to get associated popup!
            label.Size = new System.Drawing.Size(20, 20);
            label.TabIndex = 2;
            label.Text = engine.EngineName.Substring(0, 1);          // 
            label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            label.Click += new System.EventHandler(MenuButton_Click);
            this.Controls.Add(label);

            // location
            label.Location = new System.Drawing.Point(m_NextMenuButtonLoc.X, m_NextMenuButtonLoc.Y);
            m_NextMenuButtonLoc = new System.Drawing.Point(m_NextMenuButtonLoc.X - label.Width, m_NextMenuButtonLoc.Y);

            // Exit
            return label;
        }//CreateMenuButton()
        */
        //
        //
        // *****************************************************
        // ****             Close All PopUps                ****
        // *****************************************************
        private void CloseAllPopUps(IPopUp exceptThisPopUp)
        {
            foreach (IPopUp popUp in m_PopupList.Values)
                if (!popUp.Equals(exceptThisPopUp)) { popUp.Visible = false; }
        }//CloseAllPopUps
        //
        //
        //
        #endregion//Private Methods


        #region Form Event Handlers
        // *****************************************************************
        // ****                     Form Event Handlers                 ****
        // *****************************************************************
        //
        //
        //
        // ****         MenuButton_Click()          ****
        //
        // <summary>
        // One of the buttons I have created for opening a Popup has been 
        // clicked.  From the buttons name, try to locate the desired popup and
        // open it.
        // </summary>
        /*
        private void MenuButton_Click(object sender, EventArgs e)
        {
            Label button = (Label)sender;
            int  = Convert.ToInt32(button.Name);
            IPopUp popUp = null;
            if (m_PopUpList.TryGetValue(id, out popUp))
            {
                if (popUp.Visible)
                    popUp.Visible = false;
                else
                    popUp.ShowMe(this.Parent);
            }
        }
        */ 
        //
        //
        //
        // *********************************************************************
        // ****         comboBoxEngines_SelectionChangeCommitted()          ****
        // *********************************************************************
        /// <summary>
        /// The user has selected a specific engine name from the dropdown list.
        /// Find the Popup associated with this engine name, and show it.
        /// </summary>
        private void comboBoxEngines_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (comboBoxEngines.SelectedIndex < 0)
                return;
            string s = (string)comboBoxEngines.Items[comboBoxEngines.SelectedIndex];
            IPopUp popUp = null;
            if (m_PopupList.TryGetValue(s, out popUp))
            {
                if (popUp.Visible)
                    popUp.Visible = false;
                else
                    popUp.ShowMe(this.Parent);
            }
        }//comboBoxEngines_SelectionChangeCommitted()
        //
        //
        //
        //
        //
        //
        private void txtName_DoubleClick(object sender, EventArgs e)
        {
            //EngineEventArgs.RequestAllParameters(m_ParentCluster.ClusterConfiguation.GuiID,m_ParentCluster.EngineContainerID);
            //if (m_EngineControlList.Count > 0 && m_EngineControlList[0] is EngineControl)
            //{
            //	EngineControl engControl = (EngineControl)m_EngineControlList[0];
            //	engControl.Par
            //	m_ParameterInfo.EngineHub.HubEventEnqueue(EngineEventArgs.RequestParameterChange(m_ParameterInfo, newValue));
            //
            //}
        }
        //
        //
        //
        //
        #endregion//Event Handlers


    }//end class
}
