using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Ambre.PositionViewer.Dialogs
{
    using Ambre.PositionViewer.FrontEnds;
    using Misty.Lib.Hubs;

    public partial class FormNewFillHub : Form
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        private AmbreViewer m_ParentViewer = null;


        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public FormNewFillHub(AmbreViewer parentViewer)
        {
            InitializeComponent();
            this.Icon = Ambre.PositionViewer.Properties.Resources.user_female;
            m_ParentViewer = parentViewer;
            radioFilterAcctNumber.Checked = true;
        }
        //
        //       
        #endregion//Constructors


        #region no Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        #endregion//Properties


        #region no Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
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
        /// <summary>
        /// This method is called once the user is satified with the settings and confirms
        /// the creation of a new fill hub.
        /// Procedure:
        /// 1. Get user's parameters:
        ///     1a. Create a basic node (for Config file)
        ///     1b. Create node for initial drop.  Must include elements like "Listener".
        /// 2. Read AmbreConfig.txt, add basic node to file.
        /// 3. Create new initial Drop file.
        /// 4. Submit request for new FillHub to Ambre.
        /// </summary>
        private void CreateNewHub()
        {
            // Extract user's desired hub name.
            string hubName = string.Empty;
            if (textBoxHubName.Enabled)
                hubName = textBoxHubName.Text;
            else
                hubName = textBoxFilterString.Text;                             // BRE uses the acct# to name the drop files/hubs.

            // Extract user's desired AmbreUser name.

            // Extract reset dates          
            DateTime now = DateTime.Now;
            DateTime pnlResetTime;
            if (DateTime.TryParse(textBoxResetTime.Text, out pnlResetTime))
            {
                TimeSpan time = pnlResetTime.TimeOfDay;
                pnlResetTime = now.Subtract(now.TimeOfDay).Add(time);           // set reset time to today at user-provided time.
                if (now.CompareTo(pnlResetTime) > 0)                            // If now is already past today's reset time, 
                    pnlResetTime = pnlResetTime.AddDays(1.0);                   // reset tomorrow.
            }
            else
            {
                DialogResult diResult = System.Windows.Forms.MessageBox.Show("Failed to parse Reset Time. Try again.", "Formatting trouble", MessageBoxButtons.OK);
                textBoxResetTime.Text = "4:30 PM";
                return;
            }

            // Create the FillListener filter
            string filterType = string.Empty;
            string filterStr = string.Empty;
            if (radioFilterAcctNumber.Checked)
            {
                filterType = "FilterAccount";                                   // attribute key
                filterStr = textBoxFilterString.Text.Trim();
            }
            else if (radioFilter3.Checked)
            {
                filterType = "FilterInstrumentKey";                             // attribute key
                filterStr = textBoxFilterString.Text.Trim();                    // TODO: Allow user to drag drop TT objects?
            }


            // ORiginal versions
            //*
            // Create listener node.
            Misty.Lib.IO.Xml.Node listener = new Misty.Lib.IO.Xml.Node();
            listener.Name = "Ambre.TTServices.Fills.FillListener";
            listener.Attributes.Add("Name", "FillListener");                    // Name attribute key
            if (!string.IsNullOrEmpty(filterType))
                listener.Attributes.Add(filterType, filterStr);
            listener.Attributes.Add("LogAllFills", "False");

            // Create full, initial node
            Misty.Lib.IO.Xml.Node fillHubNode = new Misty.Lib.IO.Xml.Node();
            fillHubNode.Name = "Ambre.TTServices.Fills.FillHub";
            fillHubNode.Attributes.Add("Name", hubName);
            fillHubNode.Attributes.Add("LocalTime", now.ToString(Misty.Lib.Utilities.Strings.FormatDateTimeZone));
            fillHubNode.Attributes.Add("NextReset", pnlResetTime.ToString(Misty.Lib.Utilities.Strings.FormatDateTimeZone));
            fillHubNode.SubElements.Add(listener);

            //
            // Try to create the new drop file.
            //  1.) We create the minimal drop file, that defines a fill Listener, and no books.
            //  2.) This drop file must be PLACED where it will be loaded automatically by the starting FillHub.
            //  3.) TODO: The Drop model should have a static function to create the current drop file path that we need.
            //string fileName = string.Format("FillBooks_{0}_{1}.{2}", TTServices.TTApiService.GetInstance().LoginUserName, hubName, "txt");
            string userName = string.Format("{0}_{1}", TTServices.TTApiService.GetInstance().LoginUserName, hubName);
            string fileName = Ambre.TTServices.Fills.DropSimple.GetArchiveFileName(DateTime.Now, "FillBooks", userName);
            string dirPath = Ambre.TTServices.Fills.DropSimple.GetArchivePath(DateTime.Now, Misty.Lib.Application.AppInfo.GetInstance().DropPath);
            string dropPath = string.Format("{0}{1}", dirPath, fileName);

            if (!System.IO.Directory.Exists(dirPath))
            {
                System.IO.Directory.CreateDirectory(dirPath);
            }
            if (System.IO.File.Exists(dropPath))
            {   // Drop file should NOT already exist!
                DialogResult diResult = System.Windows.Forms.MessageBox.Show(string.Format("Drop file {0} already exists.  Should we delete it?\nSelect Cancel to stop now; Yes, to delete old file, No, to use old file.", fileName), "Fillbook name not unique?", MessageBoxButtons.YesNoCancel);
                if (diResult == System.Windows.Forms.DialogResult.Cancel)
                    return;             // stop process of creating fill hub and exit.
                else if (diResult == System.Windows.Forms.DialogResult.Yes)
                {   // Delete the file.s
                    System.IO.File.Delete(dropPath);
                }
            }
            else if (!System.IO.Directory.Exists(Misty.Lib.Application.AppInfo.GetInstance().DropPath))
            {   // First time when running, this dir may not exist yet.
                System.IO.Directory.CreateDirectory(Misty.Lib.Application.AppInfo.GetInstance().DropPath);
            }
            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(dropPath, false))
            {
                string text = fillHubNode.Stringify();
                if (m_ParentViewer.Log != null)
                    m_ParentViewer.Log.NewEntry(LogLevel.Major, text);
                writer.WriteLine(text);
                writer.Close();
            }

            //
            // If we get here, also create the start-up config file
            // 
            Misty.Lib.IO.Xml.Node basicNode = new Misty.Lib.IO.Xml.Node();              // Append to the basic config file
            basicNode.Name = "Ambre.TTServices.Fills.FillHub";
            basicNode.Attributes.Add("Name", hubName);
            string UserConfigPath = string.Format("{0}{1}", Misty.Lib.Application.AppInfo.GetInstance().UserConfigPath, m_ParentViewer.UserConfigFileName);
            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(UserConfigPath, true)) // append
            {
                writer.WriteLine(basicNode.Stringify());
                writer.Close();
            }

            // Request fill hub creation
            if (m_ParentViewer != null)
                m_ParentViewer.CreateNewFillHub(basicNode);
            Misty.Lib.Application.AppServices.GetInstance().TrySaveServicesToFile(m_ParentViewer.UserConfigFileName);
            //*/

            // Exit.
            this.Close();
        }// CreateNewHub()
        //
        //
        #endregion//Private Methods


        #region Form Event Handlers
        // *****************************************************************
        // ****                 Form Event Handlers                     ****
        // *****************************************************************
        //
        //
        //
        private void Radio_Click(object sender, EventArgs e)
        {
           

        }
        private void Radio_CheckChanged(object sender, EventArgs e)
        {
            if (radioFilterAcctNumber.Checked)
            {   // When we are filtering on account numbers, the hub is named after the account number.
                textBoxHubName.Text = string.Empty;
                textBoxHubName.Enabled = false;
            }
            else
            {
                textBoxHubName.Enabled = true;
            }
        }
        private void Button_Click(object sender, EventArgs e)
        {
            if (sender == buttonCancel)
            {               
                this.Close();
            }
            else if (sender == buttonCreateNewHub)
            {
                CreateNewHub();
            }
        }
        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            Keys keyPressed = e.KeyCode;
            if (sender == textBoxResetTime)
            {
                if (keyPressed == Keys.Enter)
                {   // To help the user, each time he hits return, we interpret his time
                    // and spit it back out at him.
                    string s = textBoxResetTime.Text;
                    DateTime dt;
                    if (DateTime.TryParse(s, out dt))
                        textBoxResetTime.Text = dt.ToShortTimeString();
                    else
                        textBoxResetTime.Text = "4:30 PM";
                }
            }
        }
        private void FormNewFillHub_FormClosing(object sender, FormClosingEventArgs e)
        {
            m_ParentViewer = null;
        }
        //
        #endregion//Event Handlers

    }
}
