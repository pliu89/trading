using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace UV.Lib.FrontEnds.PopUps
{
    using UV.Lib.Engines;


    public partial class ParamString : ParamControlBase
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        //
        private bool IsUserInputActive = false;                 // true = user currently typing 


        // Formatting
        private Color TextColorDefault = Color.Black;
        private Color TextColorHiLite = Color.Red;
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        public ParamString(ParameterInfo pInfo)
            : base(pInfo)
        {
            InitializeComponent();
            InitializeParameter(pInfo);

            this.tbParameter.KeyUp += new System.Windows.Forms.KeyEventHandler(this.textBox_KeyUp);
        }
        public ParamString() : base() { InitializeComponent(); } // needed for designer
        //
        public override void InitializeParameter(ParameterInfo pInfo)
        {
            base.InitializeParameter(pInfo);
            this.txtParameterName.Text = m_ParameterInfo.DisplayName;
            this.tbParameter.ReadOnly = m_ParameterInfo.IsReadOnly;

        }
        //       
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        public override string Text
        {
            get
            {
                return txtParameterName.Text;
            }
            set
            {
                txtParameterName.Text = value;
            }
        }
        //
        #endregion//Properties


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        // ****                 Set Value()                     ****
        //
        public override void SetValue(object o)
        {
            object currentValue = m_Memory;
            try
            {
                m_Memory = (string)o;
            }
            catch (Exception)
            {
                m_Memory = (string)currentValue;
            }
        }
        //
        // ****             Regenerate()                ****
        //
        /// <summary>
        /// Copies the value of the parameter from internal memory to the text box.
        /// This must be called by windows thread only!
        /// </summary>
        public override void Regenerate()
        {
            if(m_Memory != null)
                this.tbParameter.Text = m_Memory.ToString();
            this.tbParameter.ForeColor = TextColorDefault;      // default color used to signify the "correct" value is displayed.
            IsUserInputActive = false;                          // flag that we have over-written any thing that the user was doing.
        }
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
        //
        //
        #endregion//Private Methods


        #region Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        // ****         textBox_KeyUp     ****
        //
        /// <summary>
        /// Handles when a user types into the textbox.
        /// </summary>
        private void textBox_KeyUp(object sender, KeyEventArgs e)
        {
            TextBox tb = (TextBox)sender;       // sender of this event is text box

            if (e.KeyCode == Keys.Enter)
            {   // "enter key" was pressed!
                // Generate a parameter change request for our associated engine.
                string newValue = tb.Text;
                //m_ParameterInfo.EngineHub.HubEventEnqueue(EngineEventArgs.RequestParameterChange(m_ParameterInfo, newValue));//, hubID));
                m_RemoteEngineHub.HubEventEnqueue(EngineEventArgs.RequestParameterChange(m_ParameterInfo, newValue));//, hubID));
            }
            else if (e.KeyCode == Keys.Escape)
            {   // "escape key"
                tb.Text = m_Memory.ToString();
                IsUserInputActive = false;
                tb.ForeColor = TextColorDefault;
            }
            else
            {   // the key pressed was NOT the enter key.
                if (!IsUserInputActive)
                {   // first time we started typing here.
                    // tb.Clear();
                    IsUserInputActive = true;   // flag that user is typing.
                    tb.ForeColor = TextColorHiLite;
                }

            }

        }//end textBox_KeyUp()
        //
        #endregion//Event Handlers





    }
}
