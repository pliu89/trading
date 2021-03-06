﻿using System;
using System.Collections.Generic;
//using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace UV.Lib.FrontEnds.PopUps
{
    using UV.Lib.Engines;

    public partial class ParamInteger2 : ParamControlBase
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
        public ParamInteger2(ParameterInfo pInfo)
            : base(pInfo)
        {
            InitializeComponent();
            InitializeParameter(pInfo);

        }
        public ParamInteger2() { InitializeComponent(); } // needed for designer
        //
        //
        public override void InitializeParameter(ParameterInfo pInfo)
        {
            base.InitializeParameter(pInfo);
            this.ParameterName.Text = m_ParameterInfo.DisplayName;
            this.tbParameterValue.ReadOnly = m_ParameterInfo.IsReadOnly;
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
            get { return this.ParameterName.Text; }
            set { this.ParameterName.Text = value; }
        }
        //
        public object Memory
        {
            get { return m_Memory; }
        }
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
                m_Memory = (int)o;
            }
            catch (Exception)
            {
                m_Memory = (int)currentValue;
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
            if (IsUserInputActive == false)                         // Allows us to type in this box, without updates disturbing us.
            {
                tbParameterValue.Text = m_Memory.ToString();
                tbParameterValue.ForeColor = TextColorDefault;      // default color used to signify the "correct" value is displayed.
            }            
            //IsUserInputActive = false;                          // flag that we have over-written any thing that the user was doing.
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
                string newValueStr = tb.Text;
                try
                {
                    // Test the format!
                    int newValue = Convert.ToInt32(tb.Text);
                    //int hubID = 1;  // id of the display hub that holds this
                    m_RemoteEngineHub.HubEventEnqueue(EngineEventArgs.RequestParameterChange(m_ParameterInfo, newValue));//, hubID));
                    ParameterName.Focus();			// take focus from textbox.

                }
                catch (Exception)
                {   // Failed restore old value
                    tb.Text = m_Memory.ToString();
                    IsUserInputActive = false;
                    tb.ForeColor = TextColorDefault;
                }
                IsUserInputActive = false;
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