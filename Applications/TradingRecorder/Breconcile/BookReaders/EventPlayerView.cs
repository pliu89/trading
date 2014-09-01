using System;
using System.Collections.Generic;
//using System.ComponentModel;
using System.Drawing;
//using System.Data;
//using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ambre.Breconcile.BookReaders
{
    public partial class EventPlayerView : UserControl
    {

        #region no Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public EventPlayerView()
        {
            InitializeComponent();
            this.tabControl.TabPages.Clear();
            this.Button_Click(buttonCollectUserNames, EventArgs.Empty);
            dateTimePickerDate.Value = DateTime.Now.AddDays(-1);

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


        #region no Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        #endregion//Private Methods


        #region Form Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        #endregion//Event Handlers

        #region Form Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        //
        //
        // *************************************************
        // ****             Button Click()              ****
        // *************************************************
        private void Button_Click(object sender, EventArgs e)
        {
            if (sender == buttonLoadEventPlayer)
            {
                string pathBase = textBoxBasePath.Text;
                if (comboBoxUserNames.SelectedIndex < 0)
                    return;
                string userAcctName = comboBoxUserNames.SelectedItem.ToString();
                int nFilesFound = 1;        // EventPlayer.FindAllFiles(pathBase, userAcctName);
                if (nFilesFound > 0)
                {
                    /// start time
                    DateTime time1;
                    if (!DateTime.TryParse(textBoxTime.Text, out time1))
                        textBoxTime.Text = "16:00:00";
                    DateTime startDateTime = dateTimePickerDate.Value.Date;
                    startDateTime = startDateTime.Add(time1.TimeOfDay);

                    /// end time
                    DateTime time2;
                    if (!DateTime.TryParse(textBoxEndTime.Text, out time2))
                        textBoxEndTime.Text = "16:00:00";
                    DateTime endDateTime = dateTimePicker1.Value.Date;
                    endDateTime = endDateTime.Add(time2.TimeOfDay);

                    TimeSpan t = endDateTime - startDateTime;
                    double hourDiff = t.TotalHours;

                    // Create this async
                    EventSeriesView view = new EventSeriesView(pathBase, userAcctName, startDateTime);
                    // Add to page.
                    TabPage tabPage = new TabPage(userAcctName);
                    tabPage.Name = userAcctName;
                    tabControl.TabPages.Add(tabPage);
                    tabControl.SelectedTab = tabPage;

                    tabPage.Controls.Add(view);
                    view.Location = new Point(0, 0);
                    view.Dock = DockStyle.Fill;
                    view.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
                    view.Size = new Size(tabPage.ClientSize.Width, tabPage.ClientSize.Height);

                    view.BeginLoad(startDateTime, hourDiff);
                }

            }
            else if (sender == buttonCollectUserNames)
            {   // Search the base directory for a list of all usernames.
                string basePath = textBoxBasePath.Text;
                List<string> userNames;
                if (EventPlayer.TryGetAllUserNames(basePath, out userNames, 2))
                {
                    userNames.Sort();
                    object selectedItem = comboBoxUserNames.SelectedItem;           // remember what is selected
                    comboBoxUserNames.SuspendLayout();
                    comboBoxUserNames.Items.Clear();
                    comboBoxUserNames.Items.AddRange(userNames.ToArray());
                    comboBoxUserNames.ResumeLayout();

                    if (selectedItem != null && comboBoxUserNames.Items.Contains(selectedItem))
                        comboBoxUserNames.SelectedItem = selectedItem;
                    else if (comboBoxUserNames.Items.Count > 0)
                        comboBoxUserNames.SelectedIndex = 0;
                }


            }
        }

        private void ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        //
        //
        //
        #endregion//Form Event Handlers


    }
}
