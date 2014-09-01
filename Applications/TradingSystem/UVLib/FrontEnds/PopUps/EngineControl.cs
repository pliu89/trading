using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UV.Lib.FrontEnds.PopUps
{
    using UV.Lib.Engines;
    using UV.Lib.FrontEnds.GuiTemplates;
    using UV.Lib.Application;

    //
    // ****             Engine Control class                ****
    //
    //
    /// <summary>
    /// This class represents a control panel displayed inside the PopUp1 form.
    /// This object holds a list of "parameter controls", one for each engine parameter.
    /// </summary>
    public partial class EngineControl : UserControl, IEngine, IEngineControl
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // 
        private List<ParameterInfo> m_ParamInfoList = null;  // parameter information
        private List<ParamControlBase> m_ParamControl = new List<ParamControlBase>(); // list of parameter sub-controls
        private int m_EngineID = -1;                                // my id
        private string m_EngineName = string.Empty;                 // my name
        //private bool m_IsRegenerationRequired = false;

        private bool m_IsUpdateRequired = false;

        // **** Layout control ****
        private int m_nColumns = 3;
        private int m_TopMargin = 2;
        private int m_LeftMargin = 2;



        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        public EngineControl(EngineGui engineGui)
        {
            m_EngineID = engineGui.EngineID;
            m_ParamInfoList = engineGui.ParameterList;


            InitializeComponent();

            this.SuspendLayout();
            InitializeLayout();
            this.ResumeLayout(false);
        }
        /*
        public EngineControl(int engineID, List<ParameterInfo> pInfoList)
        {
            m_EngineID = engineID;
            m_ParamInfoList = pInfoList;

            InitializeComponent();

            this.SuspendLayout();
            InitializeLayout();
            this.ResumeLayout(false);
        }//end constructor()
        */
        //
        /// <summary>
        /// Now its time to create the controls.
        /// Note:
        ///     Must be called by the UI thread.
        /// </summary>
        private void InitializeLayout()
        {
            // Controls will need to know to where they should send their events.
            //AppServices appService = AppServices.GetInstance();

            // Load the controls for each parameter.            
            for (int i = 0; i < m_ParamInfoList.Count; ++i)
            {
                ParameterInfo pInfo = m_ParamInfoList[i];
                TypeCode type = Type.GetTypeCode(pInfo.ValueType);
                ParamControlBase control;
                switch (type)
                {
                    case TypeCode.Boolean:
                        control = new ParamBool2(pInfo);
                        m_ParamControl.Add(control);
                        this.Controls.Add(control);
                        break;
                    case TypeCode.String:
                        control = new ParamString(pInfo);
                        m_ParamControl.Add(control);
                        this.Controls.Add(control);
                        break;
                    case TypeCode.Int16:
                        control = new ParamInteger2(pInfo);
                        m_ParamControl.Add(control);
                        this.Controls.Add(control);
                        break;
                    case TypeCode.Int32:
                        if (pInfo.ValueType.BaseType == typeof(Enum))
                            control = new ParamEnum(pInfo);
                        else
                            control = new ParamInteger2(pInfo);
                        if (control != null)
                        {
                            m_ParamControl.Add(control);
                            this.Controls.Add(control);
                        }
                        break;
                    case TypeCode.Double:
                        control = new ParamDouble2(pInfo);
                        m_ParamControl.Add(control);
                        this.Controls.Add(control);
                        break;
                    case TypeCode.Object:				// handle special parameter types.						
                        CreateObjectControl(pInfo);
                        break;
                    default:
                        control = new ParamUnknown(pInfo);
                        m_ParamControl.Add(control);
                        this.Controls.Add(control);
                        break;
                }
            }//next i
            //
            // Create the layout
            //
            int maxWidth = 0;       // find the widest control, use that as the column width.
            int maxHeight = 0;
            foreach (Control control in m_ParamControl)
            {
                if (control.Width > (maxWidth)) { maxWidth = control.Width; }
                if (control.Height > maxHeight) { maxHeight = control.Height; }
            }
            int trueWidth = 0;
            int trueHeight = 0;
            int xLoc = m_LeftMargin;
            int yLoc = -maxHeight;         // set negative yLoc, since we increment first!
            for (int i = 0; i < m_ParamControl.Count; ++i)
            {
                // Detect new row.
                if (i % m_nColumns == 0)
                {
                    xLoc = m_LeftMargin;            // reset to far left.
                    yLoc += maxHeight + m_TopMargin;
                }
                m_ParamControl[i].Location = new System.Drawing.Point(xLoc, yLoc);
                // Remember the largest size
                if (yLoc + m_ParamControl[i].Height > trueHeight) { trueHeight = yLoc + m_ParamControl[i].Height; }// take note of where bottom (max y) is.
                if (xLoc + m_ParamControl[i].Width > trueWidth) { trueWidth = xLoc + m_ParamControl[i].Width; }// take note of where bottom (max y) is.

                // Increment cursor.
                xLoc += maxWidth + m_LeftMargin;									// increment x location. 
                //xLoc += m_ParamControl[i].Width + m_LeftMargin;                   // increment x location. 
            }
            // 
            // Set size of this control.
            //
            int columns = Math.Min(m_nColumns, m_ParamControl.Count);
            //this.Size = new System.Drawing.Size( (maxWidth+m_LeftMargin)*columns, (maxHeight+m_TopMargin));
            this.Size = new System.Drawing.Size((trueWidth + m_LeftMargin), (trueHeight + m_TopMargin));

        }//end InitializeLayout()
        //
        //
        //
        // ****			CreateObjectControl			****
        //
        /// <summary>
        /// If the parameter info type is an object, we search for known object types
        /// and create the appropriate control here. Otherwise, we ignore it.
        /// </summary>
        /// <param name="pInfo"></param>
        private void CreateObjectControl(ParameterInfo pInfo)
        {
            ParamControlBase control = null;
            string typeName = pInfo.ValueType.Name;
            // TODO: See if the object is derived from some interface, or search for
            // a method that returns a control that we can use, and call it to give us
            // such an control, and use it.
            // For Now:  I added this by hand:
            //if (typeName.Equals(typeof(BGTLib.Utilities.PositionBookEventArgs).Name))
            //    control = new ParamPositionBook(pInfo);
            //else
            control = new ParamUnknown(pInfo);


            // Add control if appropriate control was found.
            if (control != null)
            {
                m_ParamControl.Add(control);
                this.Controls.Add(control);
            }
        }//CreateObjectControl()
        //
        //
        //
        //
        //
        //
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        /// <summary>
        /// Implements IEngine.
        /// </summary>
        public bool IsUpdateRequired
        {
            get { return m_IsUpdateRequired; }
            set { m_IsUpdateRequired = value; }
        }
        //
        //
        #endregion//Properties


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        public void Regenerate(object sender, EventArgs eArgs)
        {
            if (this.InvokeRequired)
            {   // Not the window thread.
                /*
                if (this.m_IsUpdateRequired)
                {
                    EventHandler d = new EventHandler(Regenerate);
                    try
                    {
                        this.Invoke(d, EventArgs.Empty);
                        this.m_IsUpdateRequired = false;
                    }
                    catch (Exception)
                    {
                        this.m_IsUpdateRequired = false;
                        return;
                    }
                }
                */ 
            }
            else
            {   // windows thread.
                foreach (ParamControlBase pcontrol in m_ParamControl) { pcontrol.Regenerate(); }
            }
        }// Regenerate()
        //
        //
        //
        public void AcceptPopUp(Form parentForm)
        {
            parentForm.FormBorderStyle = FormBorderStyle.None;
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


        #region Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        // ****             EngineControl_Click             ****
        //
        /// <summary>
        /// Allows user to close the popup by clicking anywhere inside the visible area.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TitleBar_Click(object sender, EventArgs e)
        {
            MouseEventArgs mouseArg = (MouseEventArgs)e;
            if (mouseArg.Button == MouseButtons.Left)
                this.Parent.Visible = false;
        }
        //
        #endregion//Event Handlers


        #region IEngine implementation
        // *****************************************************************************
        // ****                         IEngine Implementation                      ****
        // *****************************************************************************
        //
        public int EngineID
        {
            get { return m_EngineID; }
        }
        public string EngineName
        {
            get { return m_EngineName; }
        }
        //public string ToLongString()
        //{
        //    return ToString();
        //}
        //
        public bool IsReady { get { return true; } }
        //
        //
        //public EngineControl GetControl(){ return this; }
        //public IEngineControl GetControl() { return null; }
        //public Huds.HudPanel GetHudPanel() { return null; }
        //
        //
        /// <summary>
        /// Implementation for IEngine
        /// </summary>
        public void SetupComplete()
        {
        }
        //
        //
        // ****          Process Event           ****
        //
        public virtual void ProcessEvent(EventArgs e)
        {
            if (e.GetType() != typeof(EngineEventArgs)) { return; }
            EngineEventArgs eArgs = (EngineEventArgs)e;

            EngineEventArgs.EventType eventType = eArgs.MsgType;
            EngineEventArgs.EventStatus eventStatus = eArgs.Status;
            if (eventStatus != EngineEventArgs.EventStatus.Confirm) { return; }
            switch (eventType)
            {
                case EngineEventArgs.EventType.ParameterChange:
                    if (eArgs.DataIntA != null)
                    {
                        for (int i = 0; i < eArgs.DataIntA.Length; ++i)
                        {
                            int id = eArgs.DataIntA[i];
                            object newValue = eArgs.DataObjectList[i];  // last argument was 0, fixed.
                            m_ParamControl[id].SetValue(newValue);
                        }
                        m_IsUpdateRequired = true;
                    }
                    break;
                case EngineEventArgs.EventType.ParameterValue:
                    if (eArgs.DataIntA != null)
                    {
                        for (int i = 0; i < eArgs.DataIntA.Length; ++i)
                        {
                            int id = eArgs.DataIntA[i];
                            object newValue = eArgs.DataObjectList[i];  // last argument was 0, fixed.
                            m_ParamControl[id].SetValue(newValue);
                        }
                        m_IsUpdateRequired = true;
                    }
                    break;
                default:
                    break;
            }

        }
        //
        //
        //
        //
        #endregion


    }
}
