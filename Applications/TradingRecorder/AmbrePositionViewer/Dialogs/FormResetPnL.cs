using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace Ambre.PositionViewer.Dialogs
{
    using Ambre.TTServices.Fills;
    using Misty.Lib.Products;

    public partial class FormResetPnL : Form
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //  
        private Dictionary<string, FillHub> m_FillHubs = new Dictionary<string, FillHub>();
        private List<InstrumentName> m_InstrumentNames = new List<InstrumentName>();


        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public FormResetPnL(List<FillHub> fillHubs)
        {
            InitializeComponent();

            try
            {
                comboBoxFillHubs.SuspendLayout();
                foreach (FillHub fillHub in fillHubs)
                {
                    m_FillHubs.Add(fillHub.Name, fillHub);
                    comboBoxFillHubs.Items.Add(fillHub.Name);
                }
                comboBoxFillHubs.ResumeLayout();
                if (comboBoxFillHubs.Items.Count > 0)
                    comboBoxFillHubs.SelectedIndex = 0;
            }
            catch (Exception)
            {

            }
            this.CheckBox_CheckedChanged(checkBoxDailyPnL, EventArgs.Empty);
            this.CheckBox_CheckedChanged(checkBoxStartingPnL, EventArgs.Empty);
            this.Radio_CheckChanged(this, EventArgs.Empty);
        }
        //
        //       
        #endregion//Constructors


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        private void ResetPnLTextBoxes()
        {
            bool isPnLTextBoxesEnabled = checkBoxDailyPnL.Checked;
            textBoxRealPnL.Text = "0.00";
            textBoxRealStartingPnL.Text = "0.00";
            if (radioButton1.Checked)
            {
                textBoxRealStartingPnL.Enabled = false;
                textBoxRealPnL.Enabled = false;
            }
            else if (radioButton2.Checked)
            {
                textBoxRealStartingPnL.Enabled = false;
                textBoxRealPnL.Enabled = false;
            }
            else if (radioButton3.Checked)
            {
                textBoxRealStartingPnL.Enabled = isPnLTextBoxesEnabled;
                textBoxRealPnL.Enabled = isPnLTextBoxesEnabled;
            }
        }// ResetPnLTextBoxes()
        //
        //
        //
        #endregion//Private Methods


        #region Form Event Handlers
        // *****************************************************************
        // ****                 Form Event Handlers                     ****
        // *****************************************************************
        //
        private void box_SelectedIndexChanged(object sender, EventArgs e)
        {
            string fillHubName = string.Empty;
            if (comboBoxFillHubs.SelectedItem != null)
            {
                fillHubName = comboBoxFillHubs.SelectedItem.ToString();
                FillHub fillHub;
                if (m_FillHubs.TryGetValue(fillHubName, out fillHub))
                {
                    m_InstrumentNames.Clear();
                    fillHub.GetInstrumentNames(ref m_InstrumentNames);
                    comboBoxInstruments.SuspendLayout();
                    comboBoxInstruments.Items.Clear();
                    foreach (InstrumentName name in m_InstrumentNames)
                        comboBoxInstruments.Items.Add(name);
                    comboBoxInstruments.ResumeLayout();
                }
            }

        }

        private void Radio_CheckChanged(object sender, EventArgs e)
        {

            if (radioButton1.Checked)
            {
                comboBoxFillHubs.SelectedIndex = -1;
                comboBoxInstruments.SelectedIndex = -1;                
                comboBoxFillHubs.Enabled = false;
                comboBoxInstruments.Enabled = false;
            }
            else if (radioButton2.Checked)
            {
                if (comboBoxFillHubs.Items.Count > 0)
                    comboBoxFillHubs.SelectedIndex = 0;
                comboBoxInstruments.SelectedIndex = -1;                
                comboBoxFillHubs.Enabled = true;
                comboBoxInstruments.Enabled = false;
            }
            else if (radioButton3.Checked)
            {
                comboBoxFillHubs.Enabled = true;
                comboBoxInstruments.Enabled = true;
            }
        }
        //
        // ****                 Button_Click()                  ****
        // 
        private void Button_Click(object sender, EventArgs e)
        {
            if (sender == buttonSubmit)
            {
                if (radioButton1.Checked)
                {   // reset all hubs now
                    foreach (FillHub fillHub in m_FillHubs.Values)
                    {
                        Misty.Lib.OrderHubs.OrderHubRequest request = new Misty.Lib.OrderHubs.OrderHubRequest(Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestRealPnLReset);
                        request.Data = new object[3];
                        request.Data[0] = null;             // means update all instruments.
                        double x;
                        if (checkBoxDailyPnL.Checked && double.TryParse(textBoxRealPnL.Text, out x))
                            request.Data[1] = x;
                        else
                            request.Data[1] = null;
                        if (checkBoxStartingPnL.Checked && double.TryParse(textBoxRealStartingPnL.Text, out x))
                            request.Data[2] = x;
                        else
                            request.Data[2] = null;
                        // Submit request
                        fillHub.Request(request);
                    }
                } else if (radioButton2.Checked)
                {
                    FillHub fillHub;
                    if ( comboBoxFillHubs.SelectedItem != null && m_FillHubs.TryGetValue((string) comboBoxFillHubs.SelectedItem,out fillHub) )
                    {
                        Misty.Lib.OrderHubs.OrderHubRequest request = new Misty.Lib.OrderHubs.OrderHubRequest(Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestRealPnLReset);
                        request.Data = new object[3];
                        request.Data[0] = null;             // means update all instruments.
                        double x;
                        if (checkBoxDailyPnL.Checked && double.TryParse(textBoxRealPnL.Text, out x))
                            request.Data[1] = x;
                        else
                            request.Data[1] = null;
                        if (checkBoxStartingPnL.Checked && double.TryParse(textBoxRealStartingPnL.Text, out x))
                            request.Data[2] = x;
                        else
                            request.Data[2] = null;
                        // Submit request
                        fillHub.Request(request);

                    }
                }
                else if (radioButton3.Checked)
                {
                    FillHub fillHub;
                    if ( comboBoxFillHubs.SelectedItem != null && m_FillHubs.TryGetValue((string) comboBoxFillHubs.SelectedItem,out fillHub) )
                    {   // The user has selected a fillHub.
                        if ( comboBoxInstruments.SelectedItem != null )
                        {   // The user has selected an instrumentName
                            InstrumentName name = (InstrumentName) comboBoxInstruments.SelectedItem;
                            Misty.Lib.OrderHubs.OrderHubRequest request = new Misty.Lib.OrderHubs.OrderHubRequest(Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestRealPnLReset);
                            request.Data = new object[3];
                            request.Data[0] = name;             // means update all instruments.
                            double x;
                            if (checkBoxDailyPnL.Checked && double.TryParse(textBoxRealPnL.Text, out x))
                                request.Data[1] = x;
                            else
                                request.Data[1] = null;
                            if (checkBoxStartingPnL.Checked && double.TryParse(textBoxRealStartingPnL.Text, out x))
                                request.Data[2] = x;
                            else
                                request.Data[2] = null;
                            // Submit request
                            fillHub.Request(request);                        
                        }

                    }
                }
            }
        }

        private void CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (sender == checkBoxDailyPnL)
            {
                textBoxRealPnL.Enabled = checkBoxDailyPnL.Checked;
                if (!textBoxRealPnL.Enabled)
                    textBoxRealPnL.Text = "0.00";

            }
            else if (sender == checkBoxStartingPnL)
            {
                textBoxRealStartingPnL.Enabled = checkBoxStartingPnL.Checked;
                if (!textBoxRealStartingPnL.Enabled)
                    textBoxRealStartingPnL.Text = "0.00";
            }
        }
        //
        #endregion//Event Handlers

    }
}
