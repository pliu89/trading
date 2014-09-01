using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace AmbreMaintenance
{
    using Ambre.TTServices.Fills;
    using Ambre.TTServices.Markets;
    using Ambre.TTServices;

    using Misty.Lib.Application;
    using Misty.Lib.Hubs;
    using Misty.Lib.IO.Xml;
    using Misty.Lib.OrderHubs;
    using Misty.Lib.Products;

    /// <summary>
    /// This project is to enable the user to recover the positions, PnLs for each fill hub from the drop file whose date time is specified by the user.
    /// Then it loads additional fills after that date time from audit trail files given by TT and parses the information into the code.
    /// Afterwards, this application should generate an output snapshot of the positions, PnLs calculated from the starting drop file snapshot and the fills in the trail.
    /// The user has an option to accept this final snapshot.
    /// </summary>
    public partial class AmbreRecoveryViewer : Form
    {
        #region Members
        // Core members.
        private AuditTrailFillHub m_AuditTrailFillHub = null;
        private MarketTTAPI m_MarketTTAPIService = null;
        private TTApiService m_TTAPIService = null;
        private DropFilePlayer m_DropFilePlayer = null;
        private AuditTrailPlayer m_AuditTrailPlayer = null;
        private FillHubPage m_FillHubPage = null;
        private DateTime m_UserSelectedRecoveryDropFileDateTime = DateTime.MinValue;
        private DateTime m_UserSelectedPlayerEndDateTime = DateTime.Now;
        private bool m_XTraderSimMode = false;
        private bool m_DebugMode = false;
        private string m_UserName = null;
        private string m_FillHubOrFillManagerName = null;
        private string m_RepositoryDropFilePath = null;
        private string m_ReferenceDropFilePath = null;
        private string m_AuditTrailFilePath = null;
        private List<string> m_RepoFileList = null;

        // Assisting members.
        private string ApplicationName = "AmbreMaintenance";
        private string ReferenceApplicationName = "Ambre";
        private LogHub Log = null;
        #endregion

        #region Constructor
        /// <summary>
        /// GUI constructor to have both of the paths as members and log as well as drop file player, audit trail player.
        /// </summary>
        public AmbreRecoveryViewer()
        {
            InitializeComponent();
            typeof(MarketTTAPI).ToString();

            // Create log viewer.
            bool isLogViewerVisible = true;
            Log = new LogHub(ApplicationName, AppInfo.GetInstance().LogPath, isLogViewerVisible, LogLevel.ShowAllMessages);

            // Create app service and get useful paths.
            AppServices appAmbreMaintenanceServices = AppServices.GetInstance(ApplicationName, true);
            m_RepositoryDropFilePath = appAmbreMaintenanceServices.Info.DropPath;
            m_AuditTrailFilePath = appAmbreMaintenanceServices.Info.DropPath;
            if (m_DebugMode)
                m_ReferenceDropFilePath = m_RepositoryDropFilePath.Replace(ApplicationName, ReferenceApplicationName);
            else
                m_ReferenceDropFilePath = m_RepositoryDropFilePath.Replace(string.Format("\\{0}", ApplicationName), "");

            Log.NewEntry(LogLevel.Minor, "The default paths to operate are {0} and {1}.", m_RepositoryDropFilePath, m_ReferenceDropFilePath);
            if (!System.IO.Directory.Exists(m_ReferenceDropFilePath))
            {
                m_ReferenceDropFilePath = m_RepositoryDropFilePath.Replace(ApplicationName, "Ambre");
                Log.NewEntry(LogLevel.Major, "The default path for reference Ambre system does not exist, and the operating path becomes {0}.", m_ReferenceDropFilePath);
                if (!System.IO.Directory.Exists(m_ReferenceDropFilePath))
                {
                    Log.NewEntry(LogLevel.Major, "The changed path still does not exist. and the program can not proceed.");
                    return;
                }
            }

            // Create market tt api service and tt api service.
            m_MarketTTAPIService = new MarketTTAPI();
            m_MarketTTAPIService.Start();
            m_TTAPIService = TTApiService.GetInstance();
            m_TTAPIService.Log = this.Log;
            m_TTAPIService.ServiceStateChanged += new EventHandler(TTAPIService_ServiceStateChanged);
            m_TTAPIService.Start();

            // Sample for the user login name and fill hub name.
            textBoxUserName.Text = "BETSIM";
            textBoxFillHubName.Text = "2014";
            m_UserName = textBoxUserName.Text;
            m_FillHubOrFillManagerName = textBoxFillHubName.Text;

            // Create file players.
            m_DropFilePlayer = new DropFilePlayer(m_ReferenceDropFilePath, Log);

            // Add closing form event trigger.
            this.FormClosing += new FormClosingEventHandler(AmbreRecoveryViewer_Closing);

            // Find all existing audit trail files. Also in this block, toggle the earliest and latest date time to the GUI.
            m_RepoFileList = new List<string>();
            LoadAllAuditTrailFiles();
        }//AmbreRecoveryViewer()
        #endregion

        #region Private Methods
        /// <summary>
        /// Load the audit trail files from the tt default files and toggle the earliest and latest date time to the GUI.
        /// </summary>
        private void LoadAllAuditTrailFiles()
        {
            // Find all the audit trail files.
            string ttAuditTrailPath;
            if (m_XTraderSimMode)
                ttAuditTrailPath = "C:\\tt\\logfiles\\sim\\";
            else
                ttAuditTrailPath = "C:\\tt\\logfiles\\";
            string pattern = "AuditLog*.mdb";
            List<string> fileList = new List<string>();
            fileList.AddRange(System.IO.Directory.GetFiles(ttAuditTrailPath, pattern));
            string targetAuditTrailFilePath;
            string fileDateString;
            DateTime fileDate = DateTime.Now;
            int dateDelimiterStart;
            int dateDelimiterEnd;

            // Copy these files to the working directory.
            try
            {
                //DateTime startDefaultDateTime = DateTime.MaxValue;
                //DateTime endDefaultDateTime = DateTime.MinValue;
                if (fileList.Count == 0)
                {
                    //startDefaultDateTime = endDefaultDateTime = DateTime.Now;
                    Log.NewEntry(LogLevel.Major, "There are no audit trail files in the tt default path.");
                    return;
                }

                foreach (string filePath in fileList)
                {
                    dateDelimiterStart = filePath.IndexOf("_");
                    dateDelimiterEnd = filePath.IndexOf("_", dateDelimiterStart + 1);
                    if (dateDelimiterStart < filePath.Length && dateDelimiterEnd < filePath.Length)
                    {
                        fileDateString = filePath.Substring(dateDelimiterStart + 1, dateDelimiterEnd - dateDelimiterStart - 1);
                        if (DateTime.TryParseExact(fileDateString, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out fileDate))
                        {
                            //if (fileDate > endDefaultDateTime)
                            //    endDefaultDateTime = fileDate;
                            //if (fileDate < startDefaultDateTime)
                            //    startDefaultDateTime = fileDate;
                        }
                        else
                        {
                            Log.NewEntry(LogLevel.Major, "There is problem in parsing file date string:{0}.", fileDateString);
                            continue;
                        }
                    }
                    else
                    {
                        Log.NewEntry(LogLevel.Major, "There is problem in parsing file name:{0}.", filePath);
                        continue;
                    }
                    targetAuditTrailFilePath = string.Format("{0}{1}", m_RepositoryDropFilePath, System.IO.Path.GetFileName(filePath));
                    m_RepoFileList.Add(targetAuditTrailFilePath);
                    System.IO.File.Copy(filePath, targetAuditTrailFilePath, true);
                }

                // Assign the date time to the GUI.
                //DropFileStartDateTime.Value = startDefaultDateTime;
                //EndPlayingDateTime.Value = endDefaultDateTime.Add(DateTime.Now.TimeOfDay);
                //m_UserSelectedRecoveryDropFileDateTime = DropFileStartDateTime.Value;
                //m_UserSelectedPlayerEndDateTime = EndPlayingDateTime.Value;
            }
            catch (Exception ex)
            {
                Log.NewEntry(LogLevel.Major, "The target file is not copied to Ambre drop path successfully with exception {0}.", ex);
                return;
            }
        }//LoadAllAuditTrailFiles()
        //
        //
        //
        /// <summary>
        /// Locate the start date time for recovery.
        /// </summary>
        /// <param name="m_ReferenceDropFilePath"></param>
        /// <returns></returns>
        private void FindRecoveryStartDateTime(string dropPath)
        {
            // Find the fill book with the closest date time to the input date time.
            string currentFilePath = null;
            string pattern = string.Format("*FillBooks_{0}_{1}.txt", m_UserName, m_FillHubOrFillManagerName);
            DateTime searchFileDateTime = DateTime.MinValue;
            //bool isBookFound = false;

            // Get the directories with date format in the drop path.
            List<string> dirPathList = new List<string>(System.IO.Directory.GetDirectories(dropPath, "20*"));
            dirPathList.Sort();
            int dirPtr = dirPathList.Count - 1;
            string currentDirPath;
            int indexDir;

            // Create the file path list to store the searched files.
            List<string> filePathList = new List<string>();
            int indexPath;
            int filePtr;
            string fileName;
            string fileTime;
            string fileDate;
            string fileDateTime;
            DropFileDateTime.Items.Clear();

            // Start searching for the correct file. Loop through directory.
            while (dirPtr >= 0)
            {
                currentDirPath = dirPathList[dirPtr];
                filePathList.Clear();
                filePathList.AddRange(System.IO.Directory.GetFiles(currentDirPath, pattern));
                if (filePathList.Count > 0)
                {
                    filePathList.Sort();
                    filePathList.Reverse();
                    filePtr = filePathList.Count - 1;

                    // Loop through files in that directory.
                    while (filePtr >= 0)
                    {
                        currentFilePath = filePathList[filePtr];
                        indexDir = currentDirPath.LastIndexOf('\\');
                        indexPath = currentFilePath.LastIndexOf('\\');
                        fileName = currentFilePath.Substring(indexPath + 1, currentFilePath.Length - (indexPath + 1));
                        fileTime = fileName.Substring(0, fileName.IndexOf("_"));
                        fileDate = currentDirPath.Substring(indexDir + 1, currentDirPath.Length - (indexDir + 1));
                        fileDateTime = string.Format("{0}{1}", fileDate, fileTime);

                        // Parse the date time out.
                        if (!DateTime.TryParseExact(fileDateTime, "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.None, out searchFileDateTime))
                        {
                            Log.NewEntry(LogLevel.Major, "Failed to parse the file date time of {0}.", fileDateTime);
                        }
                        else
                        {
                            Log.NewEntry(LogLevel.Minor, "Successfully get the file path of {0}.", searchFileDateTime);
                            DropFileDateTime.Items.Add(searchFileDateTime);
                            //System.IO.FileInfo info = new System.IO.FileInfo(filePathList[filePtr]);
                            //isBookFound = info.Length > 0;
                        }

                        // If no desired file is found in this directory, find in the next directory.
                        filePtr--;
                        if (filePtr < 0)
                        {
                            dirPtr--;
                            break;
                        }
                    }
                }
                else
                    dirPtr--;
            }
            //if (isBookFound)
            //    return searchFileDateTime;
            //else
            //    return new DateTime(2000, 1, 1);
        }//FindRecoveryStartDateTime()
        #endregion

        #region FormEventTrigers
        /// <summary>
        /// The user clicks this button to load drop file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonLoadDropFile_Click(object sender, EventArgs e)
        {
            if (sender == this.buttonLoadDropFile)
            {
                // Load Drop File.
                Log.NewEntry(LogLevel.Minor, "The button of load drop file is clicked by user.");
                DateTime minDateTime = new DateTime(2000, 1, 1);
                if (m_UserSelectedRecoveryDropFileDateTime > minDateTime)
                {
                    Log.NewEntry(LogLevel.Minor, "The user has chosen a drop file recovery date time of {0}.", m_UserSelectedRecoveryDropFileDateTime);

                    // Check if the user name or fill hub name is empty or null.
                    if (string.IsNullOrEmpty(m_UserName) || string.IsNullOrEmpty(m_FillHubOrFillManagerName))
                    {
                        Log.NewEntry(LogLevel.Major, "There are no user name or fill hub name.");
                        return;
                    }

                    // Clear the previous audit trail fill hub.
                    if (m_AuditTrailFillHub != null)
                        m_AuditTrailFillHub.RequestStop();

                    if (m_DropFilePlayer.TryPlayDropFileForOneFillHub(m_UserName, m_FillHubOrFillManagerName, m_UserSelectedRecoveryDropFileDateTime, out m_AuditTrailFillHub))
                    {
                        // Create the books on the fill hub page and start.
                        m_UserSelectedRecoveryDropFileDateTime = m_DropFilePlayer.SelectedDropDateTime;
                        DropFileStartDateTime.Value = m_UserSelectedRecoveryDropFileDateTime;
                        Log.NewEntry(LogLevel.Minor, "Successful in reading the drop file.");
                        m_AuditTrailFillHub.Start();

                        // Try to get the final state of the fill hub.
                        if (m_FillHubPage != null)
                            m_FillHubPage.Shutdown();
                        m_FillHubPage = new FillHubPage();
                        m_FillHubPage.AddHub(m_AuditTrailFillHub);

                        // Display the fill hub page onto the GUI.
                        tabControlFillPageViewer.TabPages.Clear();
                        tabControlFillPageViewer.TabPages.Add(m_FillHubPage);
                        m_AuditTrailFillHub.CreateBooksStatic(m_MarketTTAPIService);
                    }
                    else
                    {
                        Log.NewEntry(LogLevel.Major, "Failed to read the drop file.");
                        return;
                    }
                }
            }
        }//buttonLoadDropFile_Click()

        /// <summary>
        /// This method will load all the fills from audit trail files and potentially create books that are not in the drop files.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonLoadAuditTrailFills_Click(object sender, EventArgs e)
        {
            if (sender == this.buttonLoadAuditTrailFills)
            {
                // Load fills from the audit trail and prepare books.
                Log.NewEntry(LogLevel.Minor, "The button of load audit trail fills is clicked by user.");

                DateTime minDateTime = new DateTime(2000, 1, 1);
                if (m_UserSelectedRecoveryDropFileDateTime > minDateTime)
                {
                    // Check whether the end date time is larger than the start date time.
                    if (m_UserSelectedPlayerEndDateTime < m_UserSelectedRecoveryDropFileDateTime)
                    {
                        Log.NewEntry(LogLevel.Major, "The end player date time is smaller than the start recovery date time.");
                        return;
                    }

                    // Start the listener in this fill hub.
                    m_AuditTrailPlayer = new AuditTrailPlayer(m_AuditTrailFilePath, m_UserName, m_FillHubOrFillManagerName, Log);
                    AuditTrailEventArgs fillHubRequest = new AuditTrailEventArgs();
                    fillHubRequest.auditTrailEventType = AuditTrailEventType.LoadAuditTrailFills;
                    fillHubRequest.Data = new object[5];
                    fillHubRequest.Data[0] = m_AuditTrailPlayer;
                    fillHubRequest.Data[1] = m_AuditTrailFillHub;
                    fillHubRequest.Data[2] = m_UserSelectedRecoveryDropFileDateTime;
                    fillHubRequest.Data[3] = m_UserSelectedPlayerEndDateTime;
                    fillHubRequest.Data[4] = Log;
                    m_AuditTrailFillHub.HubEventEnqueue(fillHubRequest);
                }
                else
                {
                    Log.NewEntry(LogLevel.Major, "The user does not give a start recovery date time. The date time in the code is now {0}.", m_UserSelectedRecoveryDropFileDateTime);
                    return;
                }
            }
        }//buttonLoadAuditTrailFills_Click()

        /// <summary>
        /// When user clicks the recovery button, it loads drop file and play audit trail.
        /// And it display the final state of the fill hub.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonPlayAuditTrailFills_Click(object sender, EventArgs e)
        {
            if (sender == this.buttonPlayAuditTrail)
            {
                // Start recovery.
                Log.NewEntry(LogLevel.Minor, "The button of playing audit trail fills is clicked by user.");

                DateTime minDateTime = new DateTime(2000, 1, 1);
                if (m_UserSelectedRecoveryDropFileDateTime > minDateTime)
                {
                    // Check whether the end date time is larger than the start date time.
                    if (m_UserSelectedPlayerEndDateTime < m_UserSelectedRecoveryDropFileDateTime)
                    {
                        Log.NewEntry(LogLevel.Major, "The end player date time is smaller than the start recovery date time.");
                        return;
                    }

                    // Start to play the audit trail player.
                    if (m_AuditTrailPlayer != null)
                    {
                        AuditTrailEventArgs fillHubRequest = new AuditTrailEventArgs();
                        fillHubRequest.auditTrailEventType = AuditTrailEventType.PlayAuditTrailFills;
                        fillHubRequest.Data = new object[5];
                        fillHubRequest.Data[0] = m_AuditTrailPlayer;
                        fillHubRequest.Data[1] = m_AuditTrailFillHub;
                        fillHubRequest.Data[2] = m_UserSelectedRecoveryDropFileDateTime;
                        fillHubRequest.Data[3] = m_UserSelectedPlayerEndDateTime;
                        fillHubRequest.Data[4] = Log;
                        m_AuditTrailFillHub.HubEventEnqueue(fillHubRequest);
                    }
                    else
                    {
                        Log.NewEntry(LogLevel.Major, "The user does not have a audit trail player.");
                        return;
                    }
                }
                else
                {
                    Log.NewEntry(LogLevel.Major, "The user does not give a start recovery date time. The date time in the code is now {0}.", m_UserSelectedRecoveryDropFileDateTime);
                    return;
                }
            }
        }//buttonRecoveryStart_Click()

        /// <summary>
        /// When the user clicks save books button, the program copies the files to drop paths for two projects.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSaveOutput_Click(object sender, EventArgs e)
        {
            if (sender == this.buttonSaveOutput)
            {
                // Save the output of the fill hubs for this user.
                Log.NewEntry(LogLevel.Minor, "The button of save output is clicked by user.");

                // Check whether the fill hub exists and the date time is correct.
                DateTime minDateTime = new DateTime(2000, 1, 1);
                if (m_AuditTrailFillHub == null || m_UserSelectedRecoveryDropFileDateTime <= minDateTime)
                {
                    Log.NewEntry(LogLevel.Major, "The fill hub is null or the selected recovery date time is wrong.");
                    return;
                }

                Log.NewEntry(LogLevel.Minor, "The user confirms the update.");

                // Write the final state of fill hubs to the drop path. The next time when the Ambre starts, it will use these drop files.
                Dictionary<Type, string[]> stringifyOverrideTable = new Dictionary<Type, string[]>();
                stringifyOverrideTable.Add(m_AuditTrailFillHub.GetType(), new string[] { "Ambre.TTServices.Fills.FillHub", "GetAttributesDrop", "GetElementsDrop" });
                string fillHubSnapshot = Stringifiable.Stringify(m_AuditTrailFillHub, stringifyOverrideTable);

                // Generate the target file name full path.
                if (!string.IsNullOrEmpty(m_UserName) && m_AuditTrailFillHub.HubName != null)
                {
                    string targetFileName = null;
                    string targetFileDateDirectory = null;
                    string targetFilePath = null;
                    string targetAmbreDropFileDateDirectory = null;
                    string targetAmbreDropFilePath = null;
                    targetFileName = string.Format("{0}_{1}_{2}_{3}.txt", DateTime.Now.ToString("HHmmss"), "FillBooks", m_UserName, m_FillHubOrFillManagerName);
                    targetFileDateDirectory = string.Format("{0}{1}\\", m_RepositoryDropFilePath, DateTime.Now.ToString("yyyyMMdd"));
                    targetFilePath = string.Format("{0}{1}\\{2}", m_RepositoryDropFilePath, DateTime.Now.ToString("yyyyMMdd"), targetFileName);
                    targetAmbreDropFileDateDirectory = string.Format("{0}{1}\\", m_ReferenceDropFilePath, DateTime.Now.ToString("yyyyMMdd"));
                    targetAmbreDropFilePath = string.Format("{0}{1}\\{2}", m_ReferenceDropFilePath, DateTime.Now.ToString("yyyyMMdd"), targetFileName);

                    // Check whether the hub name is different from the input fill hub name.
                    StringBuilder stringBuilder = new StringBuilder();
                    if (!m_AuditTrailFillHub.HubName.Equals(string.Empty) && !m_AuditTrailFillHub.HubName.Equals(m_FillHubOrFillManagerName))
                    {
                        Log.NewEntry(LogLevel.Major, "The fill hub name is different from the input. And the names are {0} and {1}.", m_AuditTrailFillHub.HubName, m_FillHubOrFillManagerName);
                        stringBuilder.AppendFormat("The fill hub name is different from the input. And the names are {0} and {1}.", m_AuditTrailFillHub.HubName, m_FillHubOrFillManagerName);
                    }

                    // Show confirmation dialog. Ask the user whether he likes to output the final state of fill hub page out to update Ambre position.
                    stringBuilder.AppendLine("I am going to save the final state of fill account displayed in the table, Ok?");
                    stringBuilder.AppendFormat("The output drop file will be contained in the path {0} and {1}.", targetFilePath, targetAmbreDropFilePath);
                    DialogResult result = MessageBox.Show(stringBuilder.ToString(), "Output the final state of fill account", MessageBoxButtons.YesNo);
                    if (result == System.Windows.Forms.DialogResult.No)
                    {
                        Log.NewEntry(LogLevel.Major, "The user does not confirm the drop file update.");
                        return;
                    }

                    // Try create the target file path if it does not exist.
                    if (!System.IO.Directory.Exists(targetFileDateDirectory))
                    {
                        System.IO.Directory.CreateDirectory(targetFileDateDirectory);
                    }

                    // Write the stringified fill hub snapshot to the target file path.
                    using (System.IO.StreamWriter streamWriter = new System.IO.StreamWriter(targetFilePath, false))
                    {
                        streamWriter.WriteLine(fillHubSnapshot);
                        streamWriter.Close();
                    }

                    // Copy the current file written to another file path again. Use try catch block to copy the file to another path as well.
                    if (!System.IO.Directory.Exists(targetAmbreDropFileDateDirectory))
                    {
                        System.IO.Directory.CreateDirectory(targetAmbreDropFileDateDirectory);
                    }
                    try
                    {
                        System.IO.File.Copy(targetFilePath, targetAmbreDropFilePath, true);
                    }
                    catch (Exception ex)
                    {
                        Log.NewEntry(LogLevel.Major, "The target file is not copied to Ambre drop path successfully with exception {0}.", ex);
                        return;
                    }
                }
                else
                {
                    Log.NewEntry(LogLevel.Major, "The target file name generated failed. The user name or hub name is null or empty.");
                    return;
                }
            }
        }//buttonSaveOutput_Click()

        /// <summary>
        /// User may click this button to shut down all running thread like the working fill hub.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonExit_Click(object sender, EventArgs e)
        {
            if (sender == this.buttonExit)
            {
                // Exit.
                Log.NewEntry(LogLevel.Minor, "The button of exit is clicked by user.");
                this.Close();
            }
        }//buttonExit_Click()

        /// <summary>
        /// Form closing event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AmbreRecoveryViewer_Closing(object sender, FormClosingEventArgs e)
        {
            // Exit.
            this.FormClosing -= new FormClosingEventHandler(AmbreRecoveryViewer_Closing);

            // Delete the files we copied.
            if (m_RepoFileList.Count > 0)
            {
                try
                {
                    foreach (string filePath in m_RepoFileList)
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
                catch (Exception ex)
                {
                    Log.NewEntry(LogLevel.Major, "Failed to delete copied files and the error is {0}.", ex);
                }
            }

            // Shut down the fill hub.
            if (m_AuditTrailFillHub != null)
            {
                m_AuditTrailFillHub.RequestSubstractEventHandler();
                m_AuditTrailFillHub.RequestStop();
                m_AuditTrailFillHub = null;
            }

            // Shut down the services.
            if (m_MarketTTAPIService != null)
            {
                m_MarketTTAPIService.RequestStop();
                m_MarketTTAPIService = null;
            }
            if (m_TTAPIService != null)
            {
                m_TTAPIService.ServiceStateChanged -= new EventHandler(TTAPIService_ServiceStateChanged);
                m_TTAPIService.RequestStop();
                m_TTAPIService = null;
            }

            // Shut down the log.
            if (Log != null)
            {
                Log.NewEntry(LogLevel.Minor, "The form is closing.");
                Log.Flush();
                Log.RequestStop();
                Log = null;
            }
        }//Form_Closing()

        /// <summary>
        /// This method reflects the change of status of TT connection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TTAPIService_ServiceStateChanged(object sender, EventArgs eventArgs)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new EventHandler(TTAPIService_ServiceStateChanged), new object[] { sender, eventArgs });
            }
            else
            {
                Log.NewEntry(LogLevel.Minor, "TT API is connected.");
                labelTTConnection.Text = "Connected";
                labelTTConnection.BackColor = Color.Yellow;
            }
        }//TTAPIService_ServiceStateChanged()

        /// <summary>
        /// Start all the processes automatically to recover the positions for user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonRecover_Click(object sender, EventArgs e)
        {
            if (sender == buttonRecover)
            {
                Log.NewEntry(LogLevel.Minor, "The button of recovering positions is clicked by user.");
                DateTime minDateTime = new DateTime(2000, 1, 1);
                if (m_UserSelectedRecoveryDropFileDateTime <= minDateTime)
                {
                    Log.NewEntry(LogLevel.Major, "The user does not give a start recovery date time. The date time in the code is now {0}.", m_UserSelectedRecoveryDropFileDateTime);
                    return;
                }
                else
                {
                    Log.NewEntry(LogLevel.Minor, "The user has chosen a drop file recovery date time of {0}.", m_UserSelectedRecoveryDropFileDateTime);

                    // Check if the user name or fill hub name is empty or null.
                    m_UserName = textBoxUserName.Text;
                    m_FillHubOrFillManagerName = textBoxFillHubName.Text;
                    if (string.IsNullOrEmpty(m_UserName) || string.IsNullOrEmpty(m_FillHubOrFillManagerName))
                    {
                        Log.NewEntry(LogLevel.Major, "There are no user name or fill hub name.");
                        return;
                    }

                    // Clear the previous audit trail fill hub.
                    if (m_AuditTrailFillHub != null)
                        m_AuditTrailFillHub.RequestStop();

                    if (m_DropFilePlayer.TryPlayDropFileForOneFillHub(m_UserName, m_FillHubOrFillManagerName, m_UserSelectedRecoveryDropFileDateTime, out m_AuditTrailFillHub))
                    {
                        // Create the books on the fill hub page and start.
                        m_UserSelectedRecoveryDropFileDateTime = m_DropFilePlayer.SelectedDropDateTime;
                        DropFileStartDateTime.Value = m_UserSelectedRecoveryDropFileDateTime;
                        m_AuditTrailFillHub.Start();

                        // Try to get the final state of the fill hub.
                        if (m_FillHubPage != null)
                            m_FillHubPage.Shutdown();
                        m_FillHubPage = new FillHubPage();
                        m_FillHubPage.AddHub(m_AuditTrailFillHub);

                        // Display the fill hub page onto the GUI.
                        tabControlFillPageViewer.TabPages.Clear();
                        tabControlFillPageViewer.TabPages.Add(m_FillHubPage);
                        m_AuditTrailFillHub.CreateBooksStatic(m_MarketTTAPIService);
                        Log.NewEntry(LogLevel.Minor, "Successful in reading the drop file.");
                    }
                    else
                    {
                        Log.NewEntry(LogLevel.Major, "Failed to read the drop file.");
                        return;
                    }

                    //// Load fills from the audit trail and prepare books.
                    // Check whether the end date time is larger than the start date time.
                    if (m_UserSelectedPlayerEndDateTime < m_UserSelectedRecoveryDropFileDateTime)
                    {
                        Log.NewEntry(LogLevel.Major, "The end player date time is smaller than the start recovery date time.");
                        return;
                    }

                    // Start the listener in this fill hub.
                    m_AuditTrailPlayer = new AuditTrailPlayer(m_AuditTrailFilePath, m_UserName, m_FillHubOrFillManagerName, Log);
                    AuditTrailEventArgs fillHubRequestLoad = new AuditTrailEventArgs();
                    fillHubRequestLoad.auditTrailEventType = AuditTrailEventType.LoadAuditTrailFills;
                    fillHubRequestLoad.Data = new object[5];
                    fillHubRequestLoad.Data[0] = m_AuditTrailPlayer;
                    fillHubRequestLoad.Data[1] = m_AuditTrailFillHub;
                    fillHubRequestLoad.Data[2] = m_UserSelectedRecoveryDropFileDateTime;
                    fillHubRequestLoad.Data[3] = m_UserSelectedPlayerEndDateTime;
                    fillHubRequestLoad.Data[4] = Log;
                    m_AuditTrailFillHub.BooksCreated += new EventHandler(WaitBooksCreated);
                    m_AuditTrailFillHub.HubEventEnqueue(fillHubRequestLoad);
                }//DateTime_Check()
            }
        }//buttonRecover_Click()
        //
        //
        private void DropFileStartDateTime_ValueChanged(object sender, EventArgs e)
        {
            m_UserSelectedRecoveryDropFileDateTime = DropFileStartDateTime.Value;
        }//DropFileStartDateTime_ValueChanged()
        //
        //
        private void EndPlayingDateTime_ValueChanged(object sender, EventArgs e)
        {
            m_UserSelectedPlayerEndDateTime = DropFileStartDateTime.Value;
        }//EndPlayingDateTime_ValueChanged()
        //
        //
        /// <summary>
        /// Wait the books created in the audit trail fill hub.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WaitBooksCreated(object sender, EventArgs eventArgs)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new EventHandler(WaitBooksCreated), new object[] { sender, eventArgs });
            }
            else
            {
                //// Playing the fills from the audit trail file.
                // Check whether the end date time is larger than the start date time.
                if (m_UserSelectedPlayerEndDateTime < m_UserSelectedRecoveryDropFileDateTime)
                {
                    Log.NewEntry(LogLevel.Major, "The end player date time is smaller than the start recovery date time.");
                    return;
                }

                // Start to play the audit trail player.
                if (m_AuditTrailPlayer != null)
                {
                    m_AuditTrailFillHub.BooksCreated -= new EventHandler(WaitBooksCreated);
                    AuditTrailEventArgs fillHubRequestPlay = new AuditTrailEventArgs();
                    fillHubRequestPlay.auditTrailEventType = AuditTrailEventType.PlayAuditTrailFills;
                    fillHubRequestPlay.Data = new object[5];
                    fillHubRequestPlay.Data[0] = m_AuditTrailPlayer;
                    fillHubRequestPlay.Data[1] = m_AuditTrailFillHub;
                    fillHubRequestPlay.Data[2] = m_UserSelectedRecoveryDropFileDateTime;
                    fillHubRequestPlay.Data[3] = m_UserSelectedPlayerEndDateTime;
                    fillHubRequestPlay.Data[4] = Log;
                    m_AuditTrailFillHub.HubEventEnqueue(fillHubRequestPlay);
                }
                else
                {
                    Log.NewEntry(LogLevel.Major, "The user does not have a audit trail player.");
                    return;
                }
            }
        }//WaitBooksCreated()
        //
        //
        /// <summary>
        /// When the user press return for the user name.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxUserName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return)
            {
                m_UserName = textBoxUserName.Text;
                FindRecoveryStartDateTime(m_ReferenceDropFilePath);
                m_UserSelectedRecoveryDropFileDateTime = DropFileStartDateTime.Value;
            }
        }//textBoxUserName_KeyDown()
        //
        //
        /// <summary>
        /// When the user press return for the fill hub name.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxFillHubName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return)
            {
                m_FillHubOrFillManagerName = textBoxFillHubName.Text;
                FindRecoveryStartDateTime(m_ReferenceDropFilePath);
                m_UserSelectedRecoveryDropFileDateTime = DropFileStartDateTime.Value;
            }
        }//textBoxFillHubName_KeyDown()
        //
        //
        /// <summary>
        /// Update recovery start date time when the text is changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxUserName_TextChanged(object sender, EventArgs e)
        {
            m_UserName = textBoxUserName.Text;
            FindRecoveryStartDateTime(m_ReferenceDropFilePath);
            m_UserSelectedRecoveryDropFileDateTime = DropFileStartDateTime.Value;
        }
        //
        //
        /// <summary>
        /// Update recovery start date time when the text is changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxFillHubName_TextChanged(object sender, EventArgs e)
        {
            m_FillHubOrFillManagerName = textBoxFillHubName.Text;
            FindRecoveryStartDateTime(m_ReferenceDropFilePath);
            m_UserSelectedRecoveryDropFileDateTime = DropFileStartDateTime.Value;
        }
        //
        //
        /// <summary>
        /// Choose the selected drop file date time.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DropFileDateTime_SelectedIndexChanged(object sender, EventArgs e)
        {
            DropFileStartDateTime.Value = (DateTime)DropFileDateTime.SelectedItem;
        }
        #endregion
    }
}
