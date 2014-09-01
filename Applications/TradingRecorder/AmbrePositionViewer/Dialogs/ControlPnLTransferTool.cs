using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ambre.PositionViewer.Dialogs
{
    using Misty.Lib.Products;
    using Ambre.TTServices.Fills;

    public partial class ControlPnLTransferTool : UserControl
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        private Dictionary<string, FillHub> m_FillHubs = new Dictionary<string, FillHub>();
        private List<InstrumentName> m_InstrumentList = new List<InstrumentName>();         // temp workspace for instruments in list.
        
        private bool m_SubmitAllowed = true;                // denotes whether submit buttons are visible.
        private bool m_ManualValuesAllowed = true;          // denotes whether user-entry of values is allowed

        public event EventHandler ButtonClick;              // triggers when button is clicked

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public ControlPnLTransferTool(List<FillHub> fillHubs)
        {
            InitializeComponent();
            // Create my local fillHub list.
            foreach (FillHub fillHub in fillHubs)
            {
                string name = fillHub.ServiceName;          // this should always be unique in future.
                string uniqueName = name;
                int n = 0;
                while (m_FillHubs.ContainsKey(uniqueName))
                    uniqueName = string.Format("{0}{1}", name, n++);
                m_FillHubs.Add(uniqueName, fillHub);
            }

            // Update the FillHub combo box.
            comboBoxFillHubs.SuspendLayout();
            foreach (string name in m_FillHubs.Keys)
                comboBoxFillHubs.Items.Add(name);
            comboBoxFillHubs.ResumeLayout();
            if (comboBoxFillHubs.Items.Count > 0)
                comboBoxFillHubs.SelectedIndex = 0;

        }
        //
        //       
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        public string Description
        {
            get { return textDescription.Text; }
            set
            {
                textDescription.Text = value;
            }
        }
        public bool AllowSubmit
        {
            get 
            {
                return m_SubmitAllowed;
            }
            set
            {
                m_SubmitAllowed = value;
                // 
                buttonSubmitRealPnL.Enabled = m_SubmitAllowed;
                buttonSubmitRealPnL.Visible = m_SubmitAllowed;
                buttonSubmitStartRealPnL.Visible = m_SubmitAllowed;
                buttonSubmitStartRealPnL.Enabled = m_SubmitAllowed;

                textBoxRealPnLNew.Visible = m_SubmitAllowed && m_ManualValuesAllowed;
                textBoxStartRealPnLNew.Visible = m_SubmitAllowed && m_ManualValuesAllowed;     
            }
        }
        public bool AllowManualValues
        {
            get
            {
                return m_ManualValuesAllowed;
            }
            set
            {
                m_ManualValuesAllowed = value;
                textBoxRealPnLNew.Visible = m_SubmitAllowed && m_ManualValuesAllowed;
                textBoxStartRealPnLNew.Visible = m_SubmitAllowed && m_ManualValuesAllowed;                
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
        public List<object> GetCurrentState()
        {
            List<object> state = new List<object>();
            
            // Combo boxes
            if (comboBoxFillHubs.SelectedItem != null)
                state.Add(comboBoxFillHubs.SelectedItem);
            else
                state.Add(null);

            if (comboBoxInstruments.SelectedItem != null && comboBoxInstruments.SelectedItem is InstrumentName)
                state.Add(comboBoxInstruments.SelectedItem);
            else
                state.Add(null);

            // PnL entries
            state.Add(textBoxRealPnL.Text);
            state.Add(textBoxRealPnLNew.Text);
            state.Add(textBoxStartRealPnL.Text);
            state.Add(textBoxStartRealPnLNew.Text);

            return state;
        }
        public void RefreshDetails()
        {
            UpdateInstrumentDetails();
        }
        //
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
        //
        // ****         Update Instrument List          ****
        //
        /// <summary>
        /// Updates comboBox when new instruments are to be displayed.
        /// </summary>
        private void UpdateInstrumentList()
        {
            string fillHubName = comboBoxFillHubs.SelectedItem.ToString();
            FillHub fillHub;
            if (m_FillHubs.TryGetValue(fillHubName, out fillHub))
            {
                m_InstrumentList.Clear();
                fillHub.GetInstrumentNames(ref m_InstrumentList);
                comboBoxInstruments.SuspendLayout();
                comboBoxInstruments.Items.Clear();
                comboBoxInstruments.Items.Add("All instruments");
                foreach (InstrumentName name in m_InstrumentList)
                    comboBoxInstruments.Items.Add(name);
                comboBoxInstruments.ResumeLayout();
            }
        }// UpdateInstruments()
        //
        //
        private void UpdateInstrumentDetails()
        {
            // Get selected fill hub.
            if (comboBoxFillHubs.SelectedItem == null)
                return;            
            string fillHubName = comboBoxFillHubs.SelectedItem.ToString();
            FillHub fillHub = null;
            if (! m_FillHubs.TryGetValue(fillHubName, out fillHub))
                return;
            
            // Get selected instrument
            if (comboBoxInstruments.SelectedItem == null)
                return;
            InstrumentName instrument = new InstrumentName();
            if (comboBoxInstruments.SelectedItem is InstrumentName)
                instrument = (InstrumentName)comboBoxInstruments.SelectedItem;
            
            // Get information about this instrument.
            IFillBook posBook = null;

            if ((!instrument.IsEmpty) && fillHub.TryEnterReadBook(instrument, out posBook))
            {
                double x = posBook.RealizedDollarGains;
                this.textBoxRealPnL.Text = x.ToString();
                this.textBoxRealPnLNew.Text = "0";

                x = posBook.RealizedStartingDollarGains;
                this.textBoxStartRealPnL.Text = x.ToString();
                this.textBoxStartRealPnLNew.Text = "0";

                fillHub.ExitReadBook(instrument);
            }
            else
            {   // No specific instrument is selected
                this.textBoxRealPnL.Text = "0";
                this.textBoxRealPnLNew.Text = "0";
                this.textBoxStartRealPnL.Text = "0";
                this.textBoxStartRealPnLNew.Text = "0";
            }

        }// UpdateInstrumentDetails()
        //
        #endregion//Private Methods


        #region Control Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        // ****             ComboBox Selected Index Changed             ****
        //
        private void ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sender is ComboBox)
            {
                ComboBox comboBox = (ComboBox)sender;
                if (comboBox == comboBoxFillHubs)
                {
                    UpdateInstrumentList();
                }
                else if (comboBox == comboBoxInstruments)
                {
                    UpdateInstrumentDetails();
                }
            }//if sender is combobox
        }
        //
        // ****                 Button_Click()                          ****
        //
        private void Button_Click(object sender, EventArgs e)
        {
            if (sender is Button)
            {
                Button button = (Button) sender;
                if (button == this.buttonSubmitRealPnL || button == this.buttonSubmitStartRealPnL)
                {
                    if (this.ButtonClick != null)
                        this.ButtonClick(sender, EventArgs.Empty);
                }
            }
        }
        //
        //
        //
        //
        #endregion//Event Handlers

    }
}
