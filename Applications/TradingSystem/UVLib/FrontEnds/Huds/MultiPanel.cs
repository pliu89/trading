using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;



namespace UV.Lib.FrontEnds.Huds
{
    using UV.Lib.IO.Xml;
    using UV.Lib.Engines;
    using UV.Lib.FrontEnds.GuiTemplates;
    using UV.Lib.FrontEnds.PopUps;


    public partial class MultiPanel : UserControl
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //   
        private int m_EngineContainerID = -1;           // my strategy ID.
        public bool IsRegenerationRequired = false;
        private Dictionary<int, HudPanel> m_EngineControlList = new Dictionary<int, HudPanel>();

        // Layout variables.
        private int m_NextPanelXLoc = 0;                // location of next panel to be placed.
        private int m_NextPanelYLoc = 1;

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        public MultiPanel(IEngineContainer parent, EngineContainerGui guiContainer) : this()
        {
            InitializeMultiPanel(parent, guiContainer);


        }// constructor.
        // Basic constructor for designer.
        public MultiPanel() 
        { 
            InitializeComponent(); 
        }
        //
        //
        //
        // *****************************************************************
        // ****                 Initialize MultiPanel()                 ****
        // *****************************************************************
        private void InitializeMultiPanel(IEngineContainer parentContainer, EngineContainerGui guiContainer)
        {
            m_EngineContainerID = parentContainer.EngineContainerID;
            
            // Try to create any hudpanels discovered
            List<HudPanel> newHudPanels = new List<HudPanel>();
            foreach (EngineGui engineGui in guiContainer.m_Engines)
            {
                if (string.IsNullOrEmpty(engineGui.LowerHudFullName))
                    continue;
                Control newControl = null; 
                Type guiType = typeof(EngineControl);
                if (Stringifiable.TryGetType(engineGui.LowerHudFullName, out guiType))     // Check whether this control is known to us!
                {   
                    if (Utilities.GuiCreator.TryCreateControl(out newControl, guiType, engineGui))
                    {   // We have successfully created the desired control.
                        if (newControl is HudPanel)
                            newHudPanels.Add((HudPanel) newControl);
                    }
                }
            }
            
            
            
            
            
            int maxX = 0;
            int maxY = 0;
            this.SuspendLayout();
            foreach (HudPanel control in newHudPanels)
            {
                if (control != null)                                    // each engine has entry here, even if its null. (but why?)
                {
                    m_EngineControlList.Add(control.EngineId, control);
                    control.Size = control.SmallestSize;                // force panel to be smallest size possible.
                    control.Location = new Point(m_NextPanelXLoc, m_NextPanelYLoc);
                    // update layout control parameters.
                    m_NextPanelXLoc += control.Size.Width;
                    maxX = Math.Max(maxX, control.Location.X + control.Width);
                    maxY = Math.Max(maxY, control.Location.Y + control.Height);
                    this.Controls.Add(control);
                }
            }//next engine
            // Resize myself.
            this.ClientSize = new Size(maxX, maxY);     // make this as small as possible, Cluster will resize again.
            if (m_EngineControlList.Count == 0)
                this.Visible = false;

            this.ResumeLayout(false);
        }//InitializeMultiPanel()
        //       
        //
        //
        //
        #endregion//Constructors

        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        public void RegenerateNow()
        {
            foreach (HudPanel hud in m_EngineControlList.Values)
            {
                hud.Regenerate(this, null);
            }
            this.IsRegenerationRequired = false;
        }
        //
        //
        //
        //
        // ****         Process EngineEvent ()          ****
        //
        /// <summary>
        /// Called from cluster to process engine events.
        /// </summary>
        /// <param name="eArgs"></param>
        public bool ProcessEngineEvent(EngineEventArgs eArgs)
        {
            bool regenerateNow = false;
            HudPanel panel = null;
            if (m_EngineControlList.TryGetValue(eArgs.EngineID, out panel))
            {   // We have a panel for this EngineID!
                regenerateNow = panel.ProcessEngineEvent(eArgs) || regenerateNow;// tell panel to examine event args.
                //panel.Regenerate(this, null);           // tell panel to repaint itself.
            }
            IsRegenerationRequired = regenerateNow;
            return regenerateNow;
        }
        //
        //
        //
        //
        //
        #endregion//Public Methods


        #region no Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        #endregion//Private Methods


        #region no Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        #endregion//Event Handlers

    }//end class
}
