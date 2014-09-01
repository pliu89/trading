using System;
using System.Collections.Generic;

namespace AmbreMaintenance
{
    using Ambre.TTServices.Fills;
    using Ambre.TTServices.Markets;

    using Misty.Lib.Hubs;
    using Misty.Lib.IO.Xml;

    public class DropFilePlayer
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //   
        //
        private DateTime m_SelectedDropFileDateTime;
        private string m_DropPath = null;
        private LogHub Log = null;
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        //   
        /// <summary>
        /// Constructor for drop file player. Only drop path is needed.
        /// </summary>
        /// <param name="dropPath"></param>
        /// <param name="log"></param>
        public DropFilePlayer(string dropPath, LogHub log)
        {
            m_DropPath = dropPath;
            Log = log;
        }
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        public DateTime SelectedDropDateTime
        {
            get { return m_SelectedDropFileDateTime; }
        }
        #endregion//Properties


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //
        /// <summary>
        /// Get drop file specified by user using a Ambre position recovery start date time.
        /// It loads the most recent drop file before this date time.
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="fillHubName"></param>
        /// <param name="selectedDateTime"></param>
        /// <param name="fillHub"></param>
        /// <returns></returns>
        public bool TryPlayDropFileForOneFillHub(string userName, string fillHubName, DateTime selectedDateTime, out AuditTrailFillHub fillHub)
        {
            fillHub = null;

            // Find the fill book with the closest date time to the input date time.
            string currentFilePath = null;
            string pattern = string.Format("*FillBooks_{0}_{1}.txt", userName, fillHubName);
            DateTime searchFileDateTime = DateTime.MinValue;
            bool isBookFound = false;

            // Get the directories with date format in the drop path.
            List<string> dirPathList = new List<string>(System.IO.Directory.GetDirectories(m_DropPath, "20*"));
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

            // Start searching for the correct file. Loop through directory.
            while (!isBookFound && dirPtr >= 0)
            {
                currentDirPath = dirPathList[dirPtr];
                filePathList.Clear();
                filePathList.AddRange(System.IO.Directory.GetFiles(currentDirPath, pattern));
                if (filePathList.Count > 0)
                {
                    filePathList.Sort();
                    filePtr = filePathList.Count - 1;

                    // Loop through files in that directory.
                    while (!isBookFound && filePtr >= 0)
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
                            System.IO.FileInfo info = new System.IO.FileInfo(filePathList[filePtr]);
                            m_SelectedDropFileDateTime = searchFileDateTime;
                            isBookFound = searchFileDateTime <= selectedDateTime && info.Length > 0;
                        }

                        // If no desired file is found in this directory, find in the next directory.
                        filePtr--;
                        if (!isBookFound && filePtr < 0)
                        {
                            dirPtr--;
                            break;
                        }
                    }
                }
                else
                    dirPtr--;
            }

            // Check finally whether we have found the file.
            if (!isBookFound || string.IsNullOrEmpty(currentFilePath))
            {
                Log.NewEntry(LogLevel.Major, "Failed to locate the file before date time of {0}.", selectedDateTime);
                return false;
            }
            else
            {
                // Start to load every element from the file.
                Log.NewEntry(LogLevel.Minor, "Start load elements from drop file.");
                List<Node> nodeList;
                try
                {
                    using (StringifiableReader reader = new StringifiableReader(currentFilePath))
                    {
                        nodeList = reader.ReadNodesToEnd();
                        reader.Close();
                    }
                }
                catch (Exception e)
                {
                    Log.NewEntry(LogLevel.Major, "TryPlayDropFileForOneFillHub: Strigifiable Reader Exception: {0}", e.Message);
                    return false;
                }

                // Go through the node list backward, looking for the last fill hub node.
                Node hubNode = null;
                string fillHubTypeName = typeof(FillHub).FullName;
                int ptr = nodeList.Count - 1;
                while (ptr >= 0)
                {
                    if (hubNode != null)
                    {
                        nodeList.RemoveAt(ptr);
                    }
                    else if (nodeList[ptr].Name == fillHubTypeName)
                    {
                        hubNode = nodeList[ptr];
                        nodeList.RemoveAt(ptr);
                    }
                    ptr--;
                }

                // Extract the information from the fill hub node.
                fillHub = new AuditTrailFillHub(fillHubName, false);
                List<IStringifiable> objectList = new List<IStringifiable>();
                ((IStringifiable)fillHub).SetAttributes(hubNode.Attributes);
                foreach (IStringifiable subElement in hubNode.SubElements)
                {
                    IStringifiable obj = Stringifiable.Create((Node)subElement);
                    objectList.Add(obj);
                }

                foreach (Node anode in nodeList)
                {
                    IStringifiable obj = Stringifiable.Create(anode);
                    objectList.Add(obj);
                }

                // Load objects we found.
                if (objectList != null && objectList.Count > 0)
                {
                    foreach (IStringifiable obj in objectList)
                        if (obj is InstrumentMapEntry)
                            ((IStringifiable)fillHub).AddSubElement(obj);
                    foreach (IStringifiable obj in objectList)
                        if (!(obj is InstrumentMapEntry))
                            ((IStringifiable)fillHub).AddSubElement(obj);
                    return true;
                }
                else
                    return false;

            }
        }
        #endregion//Public Methods


        #region no Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        #endregion//Private Methods


        #region no Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        #endregion//Event Handlers

    }
}
