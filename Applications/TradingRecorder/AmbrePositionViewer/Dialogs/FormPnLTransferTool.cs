using System;
using System.Collections.Generic;
//using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Ambre.PositionViewer.Dialogs
{
    using Misty.Lib.Products;
    using Ambre.TTServices.Fills;

    public partial class FormPnLTransferTool : Form
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //  
        private ControlPnLTransferTool m_LeftControl = null;
        private ControlPnLTransferTool m_RightControl = null;
        private bool m_IsRightControlVisible = false;



        private Dictionary<string, FillHub> m_FillHubs = new Dictionary<string, FillHub>();     // FillHubs user can manipulate
        private List<InstrumentName> m_InstrumentNames = new List<InstrumentName>();



        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public FormPnLTransferTool()
        {
            InitializeComponent();
            this.Icon = Ambre.PositionViewer.Properties.Resources.user_female;
        }
        public FormPnLTransferTool(List<FillHub> fillHubs)
        {
            InitializeComponent();
            this.Icon = Ambre.PositionViewer.Properties.Resources.user_female;
            // Create my local fillHub list.
            foreach (FillHub fillHub in fillHubs)
            {
                string name = fillHub.ServiceName;          // this should always be unique in future.
                string uniqueName = name;
                int n = 0;
                while (m_FillHubs.ContainsKey(uniqueName))
                    uniqueName = string.Format("{0}{1}",name,n++);
                m_FillHubs.Add(uniqueName,fillHub);
            }
            // Create the left panel.
            m_LeftControl = new ControlPnLTransferTool( fillHubs );
            this.Controls.Add(m_LeftControl);
            m_LeftControl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Top)));
            m_LeftControl.Location = new System.Drawing.Point(0, 0);
            int width = m_LeftControl.Size.Width;
            m_LeftControl.ButtonClick += new EventHandler(ControlPanel_ButtonClicked);

            m_RightControl = new ControlPnLTransferTool(fillHubs);
            this.Controls.Add(m_RightControl);
            m_RightControl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Top)));
            m_RightControl.Location = new System.Drawing.Point(1+width, 0);
            m_RightControl.Visible = false;
            m_RightControl.Description = "[transfer to]";
            m_RightControl.AllowSubmit = false;             // this control can't manually submit changes.

            int maxY = m_LeftControl.Location.Y + m_LeftControl.Size.Height;
            this.ClientSize = new System.Drawing.Size( this.ClientSize.Width, maxY);

            UpdateLocationAndViewOfControls();

        }
        //
        //
        //
        // ****             UpdateLocationAndViewOfControls()               ****
        //
        private void UpdateLocationAndViewOfControls()
        {
            int clientWidth = m_LeftControl.Location.X + m_LeftControl.Size.Width;   
            this.SuspendLayout();
            m_RightControl.Visible = m_IsRightControlVisible;
            if ( m_IsRightControlVisible )
            {
                clientWidth = m_RightControl.Location.X + m_RightControl.Size.Width;
                buttonGrowShrink.Text = "<<";
                m_LeftControl.Description = "[transfer from]";
                m_LeftControl.AllowManualValues = false;
            }
            else
            {
                m_LeftControl.Description = "[manual changes]";
                m_LeftControl.AllowManualValues = true;
                buttonGrowShrink.Text = ">>";
            }
            // Sidebar is anchored to the right edge of form, so it will automatically slide there after window is resized.
            // Resize width of window
            clientWidth += groupBoxSideBar.Size.Width;
            clientWidth += 3;
            this.ClientSize = new System.Drawing.Size(clientWidth, this.ClientSize.Height);
            this.ResumeLayout();
        }// 
        //
        //
        //
        //       
        #endregion//Constructors


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************        
        //
        //
        //
        //
        public void ControlPanel_ButtonClicked(object sender, EventArgs eventArgs)
        {
            if (!(sender is Button))
                return;
            Button button = (Button)sender;
            try
            {
                if (button.Parent != m_LeftControl)
                    return;                                 // at present, only buttons active on left control

                //
                // Collect the state of the LEFT control
                //
                FillHub leftFillHub;
                InstrumentName leftInstrumentName;
                List<double> leftDoubles;
                CollectStateInfo(m_LeftControl, out leftFillHub, out leftInstrumentName, out leftDoubles);

                //
                // Collect the state of the RIGHT control
                //
                FillHub rightFillHub = null;
                InstrumentName rightInstrumentName = new InstrumentName();
                List<double> rightDoubles = new List<double>();
                if (m_IsRightControlVisible)
                {
                    CollectStateInfo(m_RightControl, out rightFillHub, out rightInstrumentName, out rightDoubles);
                }

                // 
                // Process and submit request
                //
                string buttonName = button.Name;
                if (m_IsRightControlVisible)
                {   // Transfer mode: transfer between left "from account" (left) to the "to account" (right)
                    // Transfer from account:
                    Misty.Lib.OrderHubs.OrderHubRequest leftRequest = new Misty.Lib.OrderHubs.OrderHubRequest(Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestRealPnLReset);
                    leftRequest.Data = new object[3];
                    if (! leftInstrumentName.IsEmpty)
                        leftRequest.Data[0] = leftInstrumentName;
                    else
                    {
                        System.Windows.Forms.MessageBox.Show("Transfers must be from one Instrument to another.", "Request not submitted");
                        return;                                 // Now, only transfers from specific instr to another is allowed.
                    }
                    double moneyToTransfer = 0;
                    if (buttonName.Equals("buttonSubmitRealPnL"))
                    {
                        if (!double.IsNaN(leftDoubles[0]))          // previous value
                            moneyToTransfer = leftDoubles[0];
                        leftRequest.Data[1] = 0;                    // new value is zero.
                        leftRequest.Data[2] = null;                 // leave starting-pnl alone.
                    }
                    else if (buttonName.Equals("buttonSubmitStartRealPnL"))
                    {
                        if (!double.IsNaN(leftDoubles[2]))
                            moneyToTransfer = leftDoubles[2];
                        leftRequest.Data[1] = null;
                        leftRequest.Data[2] = 0;                    // new value is zero.
                    }
                    else
                    {
                        System.Windows.Forms.MessageBox.Show(string.Format("Unknown button {0}",buttonName), "Request not submitted");
                        return;   
                    }
                    // Transfer to account:
                    Misty.Lib.OrderHubs.OrderHubRequest rightRequest = new Misty.Lib.OrderHubs.OrderHubRequest(Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestRealPnLReset);
                    rightRequest.Data = new object[3];
                    if (! rightInstrumentName.IsEmpty)
                        rightRequest.Data[0] = rightInstrumentName;
                    else
                    {
                        MessageBox.Show("Transfers must be from one Instrument to another.", "Request not submitted");
                        return;                                 // Now, only transfers from specific instr to another is allowed.
                    }
                    double currentMoney = 0;
                    if (buttonName.Equals("buttonSubmitRealPnL"))
                    {
                        if (!double.IsNaN(rightDoubles[0]))         // previous value
                            currentMoney = rightDoubles[0];
                        else
                        {
                            MessageBox.Show(" \"Transfer to\" account has bad value for current PnL.", "Request not submitted");
                            return;
                        }
                        rightRequest.Data[1] = currentMoney + moneyToTransfer;// new value is sum
                        rightRequest.Data[2] = null;                 // leave starting-pnl alone.
                    }
                    else if (buttonName.Equals("buttonSubmitStartRealPnL"))
                    {
                        if (!double.IsNaN(rightDoubles[2]))
                            currentMoney = rightDoubles[2];
                        else
                        {
                            MessageBox.Show(" \"Transfer to\" account has bad value for current PnL.", "Request not submitted");
                            return;
                        }
                        rightRequest.Data[1] = null;                    // leave value alone
                        rightRequest.Data[2] = currentMoney + moneyToTransfer;// new value
                    }                   
                    // Submit both requests now.
                    StringBuilder msg = new StringBuilder();
                    msg.AppendFormat("From: \r\nAccount: {0} \r\nInstrument:{1}",leftFillHub.ServiceName,leftRequest.Data[0]);
                    if ( leftRequest.Data[1] != null)
                        msg.AppendFormat("\r\nDaily PnL: {0}",leftRequest.Data[1]);
                    else if (leftRequest.Data[2] != null)
                        msg.AppendFormat("\r\nStarting PnL: {0}", leftRequest.Data[2]);
                    else 
                    {
                        MessageBox.Show(" \"Transfer from\" request has no values in data.", "Request not submitted");
                        return;
                    }
 
                    msg.Append("\r\n");
                    msg.AppendFormat("To: \r\nAccount: {0} \r\nInstrument:{1}", rightFillHub.ServiceName, rightRequest.Data[0]);
                    if (rightRequest.Data[1] != null)
                        msg.AppendFormat("\r\nDaily PnL: {0}", rightRequest.Data[1]);
                    else if (rightRequest.Data[2] != null)
                        msg.AppendFormat("\r\nStarting PnL: {0}", rightRequest.Data[2]);
                    else
                    {
                        MessageBox.Show(" \"Transfer to\" request has no values in data.", "Request not submitted");
                        return;
                    }
                    msg.Append("\r\nPlease confirm these final amounts.");
                    DialogResult result = MessageBox.Show(msg.ToString(),"Confirm transfer",MessageBoxButtons.YesNo);
                    if (result == DialogResult.Yes)
                    {
                        leftFillHub.Request(leftRequest);           // Subtract the money first.
                        rightFillHub.Request(rightRequest);
                    }

                }
                else
                {   // User supplied MANUAL values for PnL.
                    if (buttonName.Equals("buttonSubmitRealPnL"))
                    {
                        Misty.Lib.OrderHubs.OrderHubRequest request = new Misty.Lib.OrderHubs.OrderHubRequest(Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestRealPnLReset);
                        request.Data = new object[3];
                        if (!leftInstrumentName.IsEmpty)
                            request.Data[0] = leftInstrumentName;
                        else
                            request.Data[0] = null;             // means update all instruments.
                        if (!double.IsNaN(leftDoubles[1]))
                            request.Data[1] = leftDoubles[1];  // value to update all instrument pnl.
                        else
                        {
                            MessageBox.Show("Manual value is not set.", "Request not submitted");
                            return;
                        }
                        request.Data[2] = null;                 // null means leave starting PnL alone.
                        leftFillHub.Request(request);
                    }
                    else if (buttonName.Equals("buttonSubmitStartRealPnL"))
                    {
                        Misty.Lib.OrderHubs.OrderHubRequest request = new Misty.Lib.OrderHubs.OrderHubRequest(Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestRealPnLReset);
                        request.Data = new object[3];
                        if (!leftInstrumentName.IsEmpty)
                            request.Data[0] = leftInstrumentName;
                        else
                            request.Data[0] = null;             // means update all instruments.
                        request.Data[1] = null;                 // null means leave daily real pnl alone.
                        if (!double.IsNaN(leftDoubles[3]))     // update the starting pnl.
                            request.Data[2] = leftDoubles[3];
                        else
                        {
                            MessageBox.Show("Manual value is not set.", "Request not submitted");
                            return;
                        }
                        leftFillHub.Request(request);
                    }

                }

            }
            catch (Exception)
            {
            }
        }
        //
        private void CollectStateInfo(ControlPnLTransferTool control,out FillHub leftFillHub,out InstrumentName leftInstrumentName,out List<double>leftDoubles)
        {
            leftFillHub = null;
            leftInstrumentName = new InstrumentName();
            leftDoubles = new List<double>();

            int ptr = 0;
            List<object> leftStates = control.GetCurrentState();
            string fillHubName = leftStates[ptr].ToString();
            ptr++;
            if (!m_FillHubs.TryGetValue(fillHubName, out leftFillHub))
                return;
            //bool leftInstrumentSelected = false;
            if (leftStates[ptr] is InstrumentName)
            {
                leftInstrumentName = (InstrumentName)leftStates[ptr];
                //leftInstrumentSelected = true;
            }
            else
                leftInstrumentName = new InstrumentName();
            ptr++;
            // Load all the remaining numbers in data set.

            while (ptr < leftStates.Count)
            {
                double x = double.NaN;
                if (double.TryParse(leftStates[ptr].ToString(), out x))
                    leftDoubles.Add(x);
                ptr++;
            }
        }
        //
        //
        #endregion//Private Methods


        #region Form Event Handlers
        // *****************************************************************
        // ****                 Form Event Handlers                     ****
        // *****************************************************************
        //
        //
        // ****                 Button_Click()                  ****
        // 
        private void Button_Click(object sender, EventArgs e)
        {
            if (sender is Button)
            {
                Button button = (Button)sender;
                if (button == buttonGrowShrink)
                {
                    m_IsRightControlVisible = !m_IsRightControlVisible;
                    UpdateLocationAndViewOfControls();
                    m_LeftControl.RefreshDetails();
                    if (m_IsRightControlVisible)
                        m_RightControl.RefreshDetails();
                }
                else if (button == buttonRefresh)
                {
                    m_LeftControl.RefreshDetails();
                    if (m_IsRightControlVisible)
                        m_RightControl.RefreshDetails();
                }


            }
            /*
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
            */ 
        }//Button_Click()

        private void CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            /*
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
            */ 
        }
        //
        #endregion//Event Handlers


      
    }
}
