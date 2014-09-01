using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

using UV.Lib.Engines;


namespace UV.Lib.FrontEnds.PopUps
{
    public partial class ParamBool2 : ParamControlBase
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        private bool IsBoolReadOnly = false;
        private string m_Text;

        // Rules for on-off.
        private Color[] m_Colors = new Color[] { Color.DarkRed, Color.DarkGreen };
        private BorderStyle[] m_BorderStyles = new BorderStyle[] { BorderStyle.Fixed3D, BorderStyle.FixedSingle };
        // Rules for read-only
        private Color[] m_ReadOnlyColors = new Color[] { Color.Black, Color.Bisque };


        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        public ParamBool2(ParameterInfo pInfo) : base(pInfo)
        {
            InitializeComponent();

            m_ParameterInfo = pInfo;
            InitializeParameter(pInfo);
            this.bParameter.Text = pInfo.DisplayName;
            this.parameterName.Text = pInfo.DisplayName;

        }
        public ParamBool2() 
        { 
            InitializeComponent(); 
        } // needed for designer
        //
        public override void InitializeParameter(ParameterInfo pInfo)
        {
            base.InitializeParameter(pInfo);            
            IsBoolReadOnly = pInfo.IsReadOnly;
            if (IsBoolReadOnly)
                this.bParameter.ForeColor = m_ReadOnlyColors[0];
            else
                this.bParameter.ForeColor = m_ReadOnlyColors[1];
        }
        #endregion//constructor


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        [Browsable(true)]
        [Category("Appearance")]
        [Description("Gets and sets text")]
        [DefaultValue("text")]
        [Localizable(true)]
        public override string Text
        {
            get 
            {
                return m_Text;
                //return this.bParameter.Text;
            }
            set
            {
                m_Text = value;
                this.bParameter.Text = value;
                this.parameterName.Text = value;
            }
        }
        //
        [Browsable(true)]
        [Category("Appearance")]
        [Description("Hides label")]
        public bool HideLabel
        {
            get { return ! this.parameterName.Visible; }
            set
            {
                this.parameterName.Visible = ! value;
            }
        }

        //
        //
        //
        #endregion//properties


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        // ****             SetValue()              ****
        //
        /// <summary>
        /// Implements ParamControlBase.
        /// This method updates the internal memory of this control, and so
        /// can be called by any thread.
        /// </summary>
        /// <param name="o"></param>
        public override void SetValue(object o)
        {
            object currentValue = m_Memory;
            try
            {
                m_Memory = (bool)o;
            }
            catch (Exception)
            {
                m_Memory = (bool)currentValue;
            }


        }
        //
        // ****             Regenerate()            ****
        //
        /// <summary>
        /// This method replaces the displayed quantity with that stored in memory.
        /// This must be called by windows thread only!
        /// </summary>
        public override void Regenerate()
        {
            bool b = (bool)m_Memory;
            if (b)
            {   // ON
                this.bParameter.BackColor = m_Colors[1];
                this.bParameter.BorderStyle = m_BorderStyles[1];
                //this.bParameter.Text = "On";
            }
            else
            {   // OFF
                this.bParameter.BackColor = m_Colors[0];
                this.bParameter.BorderStyle = m_BorderStyles[0];
                //this.bParameter.Text = "Off";

            }

            //this.checkBox.Checked = b;
        }
        //
        //
        #endregion//public methods




        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        private void checkBox_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox tb = (CheckBox)sender;
            bool newValue = !tb.Checked;       // a user click is an attempt to toggle the state.
            //            
            m_RemoteEngineHub.HubEventEnqueue(EngineEventArgs.RequestParameterChange(m_ParameterInfo, newValue));

        }

        private void bParameter_Click(object sender, EventArgs e)
        {
            if (!IsBoolReadOnly)
            {   // Assume that user wants to toggle the current result.
                bool newValue = !(bool)m_Memory;
                m_RemoteEngineHub.HubEventEnqueue(EngineEventArgs.RequestParameterChange(m_ParameterInfo, newValue));
            }
        }

        private void Button_Resized(object sender, EventArgs e)
        {
            int width = Math.Max(bParameter.Width, parameterName.Width);
            int height = this.Height;
            this.ClientSize = new Size(width, height);
        }//checkBox_CheckedChanged()
        //
        //
        //
        #endregion//public methods



    }
}
