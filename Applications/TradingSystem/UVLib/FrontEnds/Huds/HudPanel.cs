using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace UV.Lib.FrontEnds.Huds
{
    using UV.Lib.Engines;
    using UV.Lib.FrontEnds.PopUps;
    using UV.Lib.FrontEnds.GuiTemplates;


    public partial class HudPanel : UserControl
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        public int EngineId = -1;
        protected Dictionary<int, ParamControlBase> m_ParamControl = new Dictionary<int, ParamControlBase>();
        private bool m_IsRegenerationRequired = false;

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public HudPanel(EngineGui engineGui) : this()
        {
            InitializeComponent(); 
            this.EngineId = engineGui.EngineID;

            // Automatically add ParamBaseControls not already added by user. 
            //foreach (Control control in this.Controls)
            //{
             //   if (control is ParamControlBase)
            //    {
            //        ParamControlBase controlBase = (ParamControlBase) control;
            //        if (m_ParamControl.ContainsKey(controlBase.m_ParameterInfo.EngineID) == false)
            //            m_ParamControl.Add(controlBase.m_ParameterInfo.EngineID, controlBase);
            //    }
            //}

            // Load controls into this page TODO: Move this to base class
            //Dictionary<string, Control> controls = new Dictionary<string, Control>();
            //foreach (Control c in this.Controls)
            //    controls.Add(c.Name, c);



        }
        public HudPanel() 
        { 
            InitializeComponent(); 
        }
        public void Initialize(EngineGui engineGui)
        {
            foreach (ParameterInfo pInfo in engineGui.ParameterList)
            {
                foreach (Control c in this.Controls)
                    if (c is ParamControlBase && c.Name.Contains(pInfo.Name))
                    {
                        ParamControlBase controlBase = (ParamControlBase)c;
                        m_ParamControl.Add(pInfo.ParameterID, controlBase);
                        controlBase.InitializeParameter(pInfo);
                    }
            }
        }
        //
        //       
        #endregion//Constructors

        #region Properties
        //
        //
        //
        /// <summary>
        /// Returns smallest size needed to display its controls.
        /// </summary>
        public Size SmallestSize
        {
            get
            {
                int maxX = 0;
                int maxY = 0;
                foreach (Control control in this.Controls)
                {
                    maxX = Math.Max(maxX, control.Location.X + control.Width);
                    maxY = Math.Max(maxY, control.Location.Y + control.Height);
                }
                return new Size(maxX, maxY);
            }
        }

        #endregion

        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //
        //
        // ****          Process EngineEvent           ****
        //
        public bool ProcessEngineEvent(EngineEventArgs eArgs)
        {
            EngineEventArgs.EventType eventType = eArgs.MsgType;
            EngineEventArgs.EventStatus eventStatus = eArgs.Status;
            if (eventStatus != EngineEventArgs.EventStatus.Confirm) { return false; }
            switch (eventType)
            {
                case EngineEventArgs.EventType.ParameterChange:
                    if (eArgs.DataIntA != null)
                    {
                        for (int i = 0; i < eArgs.DataIntA.Length; ++i)
                        {
                            ParamControlBase control;
                            if (m_ParamControl.TryGetValue(eArgs.DataIntA[i], out control))
                            {
                                control.SetValue(eArgs.DataObjectList[i]);
                                m_IsRegenerationRequired = true;
                            }
                        }

                    }
                    break;
                case EngineEventArgs.EventType.ParameterValue:
                    if (eArgs.DataIntA != null)
                    {
                        for (int i = 0; i < eArgs.DataIntA.Length; ++i)
                        {
                            ParamControlBase control;
                            if (m_ParamControl.TryGetValue(eArgs.DataIntA[i], out control))
                            {
                                control.SetValue(eArgs.DataObjectList[i]);
                                m_IsRegenerationRequired = true;
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
            return m_IsRegenerationRequired;
        }// ProcessEngineEvent()
        //
        //
        //
        // ****                     Regenerate()                            ****
        //
        //
        public void Regenerate(object sender, EventArgs eArgs)
        {
            if (this.InvokeRequired)
            {   // Not the window thread.
                if (this.m_IsRegenerationRequired)
                {
                    EventHandler d = new EventHandler(Regenerate);
                    try
                    {
                        this.m_IsRegenerationRequired = false;
                        this.BeginInvoke(d, EventArgs.Empty);
                    }
                    catch (Exception)
                    {
                        this.m_IsRegenerationRequired = false;
                        return;
                    }
                }
            }
            else
            {   // windows thread.
                foreach (ParamControlBase pcontrol in m_ParamControl.Values)
                    pcontrol.Regenerate();
            }
        }
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
