using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace UV.Lib.FrontEnds.Clusters
{
    using UV.Lib.Engines;
    using UV.Lib.Hubs;

    public partial class ClusterDisplay : Form, IEngineHub
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // My associated instrument hub 
        //
        public readonly Hub ParentHub;				// Owner of this display, usually a FrontEndServer
        private int m_ID = -1;                      // ID assigned to me by my FrontEndHub, only after its created, and registered with FrontEndhub
        public readonly IEngineHub AssocEngineHub;  // Hub to which we send events   


        //
        // My clusters 
        // 
        // ClusterList: simple list of all clusters in this GUI.
        // InstrIDToClusters : List of controls subscribed to each instrument.
        private List<Cluster> m_ClusterList = new List<Cluster>();
        private Dictionary<int, List<Cluster>> m_EngineContainerIDToClusters = new Dictionary<int, List<Cluster>>();
        private List<IEngineContainer> m_EngineContainerList = new List<IEngineContainer>();
        private Dictionary<int, IEngineContainer> m_EngineContainerDict = new Dictionary<int, IEngineContainer>();

        private EventHandler m_RegenerateNowDelegate = null;

        //
        // Multi-graph Holder
        //
        public Graphs.GraphHolder GraphDisplay = null;

        //
        // Cluster Layout parameters
        //
        // Cluster layout - ClusterPos: gives the Left-to-right, top-to-bottom ordering of clusters, using their IDs.
        private List<int> m_ClusterPos = new List<int>();// List[n]=ClusterID --> ClusterID located at x=(int)(n%MaxColumns), y=(n/MaxColumns).
        private int m_MaxColumns = 4;       // number of columns to fill before making a new row.
        private int m_MaxClusterWidth = 0;  // keep track of largest cluster, so all clusters are evenly spaced.
        private int m_MaxClusterHeight = 0;

        //
        // Internal variables
        //
        public Keys m_KeyPressed = Keys.None;
        #endregion// members


        #region Constructors and Layout Methods
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        /// <summary>
        /// Note: 
        /// 1) Since this method is called from the GuiCreator, any changes to its signature 
        /// must be implemented in the GuiCreator call as well!
        /// </summary>
        public ClusterDisplay(Hub myParentFrontEndHub, IEngineHub associatedHub, List<GuiTemplates.EngineContainerGui> guiList)
        {
            ParentHub = myParentFrontEndHub;		// hub that owns this display
            AssocEngineHub = associatedHub;			// hub to which events generated here are sent.
            m_RegenerateNowDelegate = new EventHandler(RegenerateNow);

            InitializeComponent();
            if (UV.Lib.Application.AppServices.GetInstance().AppIcon != null)
                this.Icon = UV.Lib.Application.AppServices.GetInstance().AppIcon;

            InitializeMenu();

            InitializeDisplay(guiList);

            this.KeyPreview = true;
            this.KeyDown += new KeyEventHandler(Cluster_KeyDown);
            this.KeyUp += new KeyEventHandler(Cluster_KeyUp);
            
        }// Constructor
        //       
        //
        //
        // ****         Regenerate Layout()             ****
        //
        /// <summary>
        /// Retile and update the cluster layout.
        /// </summary>
        private void RegenerateLayout()
        {
            this.SuspendLayout();
            //
            // Tile all the clusters on the form
            //
            int upperSpace = this.menuStrip1.Size.Height + 1;
            int maxY = 0;                   // store the size of the viewable region.
            int maxX = 0;
            for (int i = 0; i < m_ClusterPos.Count; ++i)
            {
                int id = m_ClusterPos[i];   // id of cluster at this position.
                if (id >= 0)
                {
                    Cluster aCluster = m_ClusterList[id];
                    ClusterConfiguration config = aCluster.ClusterConfiguation;
                    if ((config.GuiColumn < 0) || (config.GuiRow < 0))
                    {
                        int x = i % m_MaxColumns;
                        int y = i / m_MaxColumns;
                        aCluster.Location = new System.Drawing.Point(x * m_MaxClusterWidth, y * m_MaxClusterHeight + upperSpace);
                        if (maxX < (x + 1) * m_MaxClusterWidth) maxX = (x + 1) * m_MaxClusterWidth;
                        if (maxY < (y + 1) * m_MaxClusterHeight) maxY = (y + 1) * m_MaxClusterHeight;
                    }
                    else
                    {
                        int x = config.GuiColumn;
                        int y = config.GuiRow;
                        aCluster.Location = new System.Drawing.Point(x * m_MaxClusterWidth, y * m_MaxClusterHeight + upperSpace);
                        if (maxX < (x + 1) * m_MaxClusterWidth) maxX = (x + 1) * m_MaxClusterWidth;
                        if (maxY < (y + 1) * m_MaxClusterHeight) maxY = (y + 1) * m_MaxClusterHeight;
                    }
                }
            }//next position i
            //
            // Resize the form
            //
            this.ClientSize = new System.Drawing.Size(maxX, maxY + upperSpace);
            int edge = (this.Width - this.ClientSize.Width) / 2;                      // border width.
            int titlebarHeight = this.Height - this.ClientSize.Height - 2 * edge;
            //this.MinimumSize = new System.Drawing.Size(maxX + 2 * edge, maxY + titlebarHeight + 2 * edge);
            //this.MaximumSize = new System.Drawing.Size(maxX + 2 * edge, maxY + titlebarHeight + 2 * edge);
            this.Text = String.Format("Tramp {0}", this.ID.ToString());
            // Exit
            this.ResumeLayout(false);
            this.PerformLayout();
        }//Regenerate Layout().
        //
        //
        //
        // ****             Initialize Display()            ****
        //
        private void InitializeDisplay(List<GuiTemplates.EngineContainerGui> guiList)
        {
            this.SuspendLayout();
            foreach (GuiTemplates.EngineContainerGui gui in guiList)
            {   //
                // Get each cluster
                //                              
                Cluster aCluster = new Cluster(gui);
                aCluster.AcceptNewParentDisplay(this);

                m_ClusterPos.Add(m_ClusterList.Count);      // store cluster id into the next open position.
                m_ClusterList.Add(aCluster);                // store cluster in order - this is its id.
                m_EngineContainerList.Add(aCluster);
                m_EngineContainerDict.Add(aCluster.EngineContainerID, aCluster);
                // Add to lookup list useful for event updates.
                List<Cluster> aList = null;                 // list of clusters subscribed to this engineContainerId.
                if (!m_EngineContainerIDToClusters.TryGetValue(aCluster.EngineContainerID, out aList))
                {   // No cluster is yet subscribed to this engineContainerId...
                    aList = new List<Cluster>();            // So create one.
                    m_EngineContainerIDToClusters.Add(aCluster.EngineContainerID, aList);
                }
                if ( ! aList.Contains(aCluster))            // List this cluster subscribe to this instrumentID
                    aList.Add(aCluster);
                this.Controls.Add(aCluster);                // Note: This must be called by GUI thread!

                // Determine size of visible area
                if (aCluster.Size.Width > m_MaxClusterWidth) { m_MaxClusterWidth = aCluster.Size.Width; }
                if (aCluster.Size.Height > m_MaxClusterHeight) { m_MaxClusterHeight = aCluster.Size.Height; }
            }
            this.ResumeLayout(false);
            this.PerformLayout();
            RegenerateLayout();
        }// end InitializeDisplay()
        // 
        //
        //
        private void InitializeMenu()
        {
            /*
            int width = 152;
            int height = emailRecipientsToolStripMenuItem.Size.Height;
            foreach (string s in m_EMailRecipients)
            {
                ToolStripMenuItem menuItem = new ToolStripMenuItem();		
                menuItem.Name = "menuItemEmailRecp"+ m_EmailRecipientMenuItems.Count.ToString();
                menuItem.Text = s;
                menuItem.Checked = true;
                menuItem.CheckOnClick = true;
                menuItem.CheckState = CheckState.Checked;

                menuItem.Size = new System.Drawing.Size(width, height);
                //menuItem.Size =  = new System.Drawing.Size(152, 22);

                m_EmailRecipientMenuItems.Add(menuItem);
            }
            // Add to super-menuitem
            this.emailRecipientsToolStripMenuItem.DropDownItems.AddRange( m_EmailRecipientMenuItems.ToArray() );
            */
        }//InitializeMenu()
        //       
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        public int ID
        {
            get { return m_ID; }
            set
            {
                m_ID = value;
                this.Name = string.Format("ClusterDisplay{0}", m_ID.ToString());
                Utilities.ControlTools.SetText(this, string.Format("Display {0}", m_ID));
            }
        }
        //public bool IsInitialized
        //{
        //    get { return (m_ClusterList.Count > 0); }
        //}
        //
        //
        #endregion//Properties


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        // ****                 ToString()                  ****
        //
        public override string ToString()
        {
            return String.Format("ClusterDisplay{0}", ID.ToString());
        }
        //
        //
        //
        //
        // ****                 Regenerate Now                  ****
        //
        /// <summary>
        /// Immediately update the visual components of the Clusters.
        /// Loops through each cluster in my list, calling their "RegenerateNow()" methods, 
        /// which will update their displays.
        /// Threads: When called by non-GUI thread, then an invoke is made for GUI thread.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eArgs"></param>
        public void RegenerateNow(object sender, EventArgs eArgs)
        {
            if (this.InvokeRequired)
            {   // Not the gui thread!  Set up an invoke.
                //EventHandler d = new EventHandler(RegenerateNow);
                try
                {
                    //this.Invoke(d, new object[] { sender, eArgs });
                    this.Invoke(m_RegenerateNowDelegate, sender, eArgs);
                }
                catch (Exception)
                {

                }
            }
            else
            {   // This is the GUI thread.
                foreach (Cluster aCluster in m_ClusterList) // TODO: need to put lock here on ClusterList!
                {                    
                    aCluster.RegenerateNow();
                }
            }
        }// RegenerateNow().

        //
        //
        #endregion//Public Methods


        #region No Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        //
        //
        //
        //
        #endregion//Private Methods



        #region IEngineHub
        // *************************************************************
        // ****             IEngineHub implementation               ****
        // *************************************************************
        //
        // Notes: 
        // 1. A ClusterDisplay holds many Clusters, so it must implement IEngineHub.
        //     A ClusterDisplay does not have its own thread; it shares a single 
        //     thread (of the FrontEndServer) with several other ClusterDisplays 
        //
        //
        public string ServiceName
        {
            get { return this.Name; }           // this is not used
        }
        public List<IEngineContainer> GetEngineContainers() 
        { 
            return m_EngineContainerList; 
        }
        public Dictionary<int, IEngineContainer> GetEngineContainersDictionary() 
        { 
            return m_EngineContainerDict; 
        }
        //
        // *************************************************
        // ****             HubEventEnqueue             ****
        // *************************************************
        /// <summary>
        /// ClusterDisplay events are passed here by the FrontEndServer thread.
        /// The calling thread can update the internal values of clusters, and popups, 
        /// but cannont directly update the Controls themselves.
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        public bool HubEventEnqueue(EventArgs eventArgs)
        {
            bool regenerateNow = false;
            if (eventArgs is EngineEventArgs)
            {
                EngineEventArgs e = (EngineEventArgs)eventArgs;
                if (e.Status == EngineEventArgs.EventStatus.Confirm)
                {   // I process only confirmed requests.
                    if (e.MsgType == EngineEventArgs.EventType.GetControls)
                    {   // TODO: new controls that appear spontaneously may be allowed later.
                        // Need to invoke the Control creator, then when we reeceive the call back, 
                        // create a request to the FrontEndServer to add it to my list of Clusters 
                        // in this display.  Why so complicated?  1) The UI thread must create the form, 
                        // add it to the diplay list of controls, and then the FrontEndServer thread must 
                        // add the created control to our lists so it can receive event messages.
                    }
                    else
                    {   // Most Engine EventArgs are simply passed to the appropriate Cluster.
                        IEngineContainer cluster;
                        if (m_EngineContainerDict.TryGetValue(e.EngineContainerID, out cluster))
                            regenerateNow = cluster.ProcessEvent(eventArgs) || regenerateNow;
                    }
                }
            }
            return regenerateNow;
        }
        //
        //
        public event EventHandler EngineChanged;
        //
        //
        public void OnEngineChanged(EventArgs e)
        {
            if (EngineChanged != null) { EngineChanged(this, e); }
        }
        //
        //
        //
        //
        //
        #endregion//IEngineHub implementation


        #region Form Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        //
        //
        // *********************************************
        // *****		Monitor key press			****
        // *********************************************
        private void Cluster_KeyUp(object sender, KeyEventArgs e)
        {
            m_KeyPressed = Keys.None;
        }
        private void Cluster_KeyDown(object sender, KeyEventArgs e)
        {
            m_KeyPressed = (Keys)e.KeyCode;
        }
        //
        //
        // *********************************************
        // ****				Form Closing			****
        // *********************************************
        private void ClusterDisplay_FormClosing(object sender, FormClosingEventArgs e)
        {
        }//ClusterDisplay_FormClosing()
        //
        //
        //
        // *********************************************
        // ****				Load					****
        // *********************************************
        private void ClusterDisplay_Load(object sender, EventArgs e)
        {

        }
        //
        //
        // ************************************************
        // ****            MenuItem_CLick              ****
        // ************************************************
        /// <summary>
        /// This event handles all Menu Item selections by user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_CLick(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem)
            {
                ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;
                if (menuItem == menuItemNewGraphWindow)
                    NewGraphWindow_Click(sender, e);
                else if (menuItem == menuItemSaveStrategies)
                {
                    if (AssocEngineHub != null)
                        AssocEngineHub.HubEventEnqueue(EngineEventArgs.RequestSaveEngines(AssocEngineHub.ServiceName));
                }

            }
        }
        //
        //
        //
        // *****************************************************************
        // ****             NewGraphWindow_Click()                      ****
        // *****************************************************************
        private void NewGraphWindow_Click(object sender, EventArgs e)
        {
            if (GraphDisplay == null)
            {
                Graphs.GraphHolder holder = new Graphs.GraphHolder();
                GraphDisplay = holder;

                holder.FormClosed += new FormClosedEventHandler(GraphDisplay_FormClosed);
                holder.IsMdiContainer = true;
                if (m_ClusterList.Count < holder.m_NumberOfColumns)
                    holder.m_NumberOfColumns = m_ClusterList.Count;
                GraphDisplay.Show();

                List<Form> m_GraphForms = new List<Form>();
                foreach (Cluster cluster in m_ClusterList)
                {
                    List<IEngine> engineList = cluster.m_Header.GetEngines();
                    foreach (IEngine eng in engineList)
                    {
                        if (eng is Graphs.ZGraphControl)
                        {
                            Graphs.ZGraphControl zcontrol = (Graphs.ZGraphControl)eng;                            
                            if (zcontrol.ParentForm != null)
                            {
                                zcontrol.ParentForm.MdiParent = holder;
                                ((FrontEnds.PopUps.IPopUp)zcontrol.ParentForm).ShowMe(cluster.m_Header);
                            }
                        }
                    }
                }

            }
        }
        //
        // *********************************************************
        // ****             GraphDisplay_FormClosed()           ****
        // *********************************************************
        /// <summary>
        /// Event handler for user closing out the graph window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void GraphDisplay_FormClosed(object sender, FormClosedEventArgs e)
        {
            GraphDisplay = null;
        }

        //
        #endregion//Event Handlers

    }
}
