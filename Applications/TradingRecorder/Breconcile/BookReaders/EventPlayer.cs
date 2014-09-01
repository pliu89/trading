using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Ambre.Breconcile.BookReaders
{
    using Misty.Lib.Products;               // InstrumentName
    using Misty.Lib.IO.Xml;                 // for nodes

    /// <summary>
    /// This holds a collection time series, called "SeriesList" for each instrument found
    /// inside the account of interest.
    /// I assume that all the relevent series are found a file named "HHMMSS_" + FileBase + ".txt"
    /// under directories: //PathBase//yymmdd//
    /// We collect all files within this pattern and store them in m_Files(DateTime,fileName) dictionary.
    /// The nodes arlready loaded from these files are those inclusive of the indices "m_FileStart" and "m_FileEnd".
    /// </summary>
    public class EventPlayer
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        // XML file management
        private string m_FileBase;
        private string m_PathBase;
        private SortedList<DateTime,string> m_Files = null;                 // list of all files matching base name.
        private int m_FileStart = -1;                                       // pointer to first file loaded into NodeList
        private int m_FileEnd = -1;

        public DateTime StartDate;                                          // Time provided by user to get starting file.
        public DateTime EndDate;                                            // " " ending file.

        // Series collections
        public Dictionary<InstrumentName, EventSeries> SeriesList = new Dictionary<InstrumentName, EventSeries>();
        public Dictionary<string, InstrumentName> m_KeyToName = new Dictionary<string, InstrumentName>();

        public List<Node> m_UnProcessedNodes = new List<Node>();

        // Events
        //public event EventHandler SeriesChanged;
        public event EventHandler TaskStarted;
        public event EventHandler TaskCompleted;

        #endregion// members


        #region Creators and Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        /// <summary>
        /// To begin, the constructor will load drop file at or just earlier than startDateTime; 
        /// that is, InitialState.LocalTime is earlier/equal to startDateTime!
        /// Desired file base name like "FillBooks_99990003".
        /// </summary>
        /// <param name="basePath">Path where date-directories are located.</param>
        /// <param name="fileBaseName">Account/user identifier part of the drop files.</param>
        /*
        public EventPlayer(string basePath, string fileBaseName, DateTime startDateTime, double hoursToLoad=0.50)
        {
            //
            // Collect all relevent files
            //
            m_FileBase = fileBaseName;                                  // Pattern of files to seek.
            m_PathBase = basePath;                                      // Seek files (in any directory) beneath this base directory...
            m_Files = new SortedList<DateTime, string>();
            EventPlayer.FindAllFiles(m_PathBase,m_FileBase,m_Files);// locate all desired files in all subdirectories
            if (m_Files.Count == 0)
                return;                                                 // quick bypass exit


            StartDate = startDateTime;                                  

            // Find starting file (near user provided date) and read that file.
            int n = 0;
            while (n < m_Files.Count && m_Files.Keys[n].CompareTo(StartDate) < 0) // search just past startDate, then load n-1 file.
                n++;                                
            m_FileStart = Math.Max(0, n - 1);                           // inclusive file load ptr (first file that has been loaded)
            m_FileEnd = m_FileStart;                                    // inclusize file load (last file that has been loaded).
            List<Node> nodeList = new List<Node>();                     // a list, into which we load the nodes.

            // Load the initial period of interest            
            DateTime stopDateTime = StartDate.AddHours(hoursToLoad);    // load
            this.EndDate = stopDateTime;
            int fileIndex = m_FileStart;
            while (fileIndex < m_Files.Count && m_Files.Keys[fileIndex].CompareTo(stopDateTime) < 0)
            {
                nodeList.AddRange(ReadNodesFromFile(fileIndex));      // add these nodes to the end of our list.
                m_FileEnd = fileIndex;
                fileIndex ++;
            }
            AddToEventSeries(ref nodeList, false);
        }// EventPlayer()
        */
        //
        private EventPlayer(string basePath, string fileBaseName)
        {
            m_FileBase = fileBaseName;                                  // Pattern of files to seek.
            m_PathBase = basePath;                                      // Seek files (in any directory) beneath this base directory...
            m_Files = new SortedList<DateTime, string>();
            EventPlayer.FindAllFiles(m_PathBase, m_FileBase, m_Files);  // locate all desired files in all subdirectories
        }
        /// <summary>
        /// Must call Load(startTime) after creating the instance.
        /// </summary>
        /// <param name="basePath"></param>
        /// <param name="fileBaseName"></param>
        /// <param name="createdPlayer"></param>
        /// <returns></returns>
        public static bool TryCreate(string basePath, string fileBaseName, out EventPlayer createdPlayer)
        {
            createdPlayer = null;
            EventPlayer newPlayer = new EventPlayer(basePath, fileBaseName);
            if (newPlayer.m_Files.Count == 0)
                return false;                                           // quick bypass exit
            createdPlayer = newPlayer;
            return true;
        }
        public void Load(DateTime startDateTime, double hoursToLoad = 0.50)
        {
            StartDate = startDateTime;

            // Find starting file (near user provided date) and read that file.
            int n = 0;
            while (n < m_Files.Count && m_Files.Keys[n].CompareTo(StartDate) < 0) // search just past startDate, then load n-1 file.
                n++;
            m_FileStart = Math.Max(0, n - 1);                           // inclusive file load ptr (first file that has been loaded)
            m_FileEnd = m_FileStart;                                    // inclusize file load (last file that has been loaded).
            List<Node> nodeList = new List<Node>();                     // a list, into which we load the nodes.

            // Load the initial period of interest
            DateTime stopDateTime = StartDate.AddHours(hoursToLoad);    // load
            this.EndDate = stopDateTime;
            int fileIndex = m_FileStart;
            while (fileIndex < m_Files.Count && m_Files.Keys[fileIndex].CompareTo(stopDateTime) < 0)
            {
                nodeList.AddRange(ReadNodesFromFile(fileIndex));        // Add nodes to the end of our list. // Timing: typically 500+ms
                m_FileEnd = fileIndex;
                fileIndex++;
            }

            //CheckExcludeExtraFills(ref nodeList, settlementEndDateTime);// The settlementEndDate is in local time to exclude extra fills.
            AddToEventSeries(ref nodeList, false);                      // Timing: typically ~10ms
        }// Load()
        //
        public void BeginLoad(DateTime startDateTime, double hoursToLoad = 0.50)
        {

            Action startBegin = () =>
            {
                if (this.TaskStarted != null)
                    this.TaskStarted(this, EventArgs.Empty);

                this.Load(startDateTime, hoursToLoad);
                
                if (this.TaskCompleted != null)
                    this.TaskCompleted(this, EventArgs.Empty);
            };

            //startBegin.Invoke();
            Thread t = new Thread(new ThreadStart(startBegin));
            t.Start();
        }
        //
        //
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


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        // *********************************************************
        // ****                 Try Append()                    ****
        // *********************************************************
        /// <summary>
        /// Reads files up to newEndTime and tries to append events to our series.
        /// </summary>
        /// <param name="newEndTime">DateTime we want to have within our Series window.</param>
        /// <returns></returns>
        public bool TryAppend(DateTime newEndTime)
        {
            if (m_FileEnd >= m_Files.Count - 1)
                return false;                                           // already at end of files
            EndDate = newEndTime;   
            int fileIndex = m_FileEnd + 1;                              // start by reading the next file
            List<Node> nodeList = new List<Node>();
            while (fileIndex < m_Files.Count && m_Files.Keys[fileIndex].CompareTo(EndDate) < 0) // keep reading until file is outside window.
            {
                nodeList.AddRange( ReadNodesFromFile(fileIndex) );      // add these nodes to the end of our list.
                m_FileEnd = fileIndex;
                fileIndex++;
            }
            AddToEventSeries(ref nodeList, false);
            return true;
        }// TryAppend()
        //
        public void BeginTryAppend(DateTime newStartTime)
        {
            Action startBegin = () =>
            {
                if (this.TaskStarted != null)
                    this.TaskStarted(this, EventArgs.Empty);

                this.TryAppend(newStartTime);

                if (this.TaskCompleted != null)
                    this.TaskCompleted(this, EventArgs.Empty);
            };
            Thread t = new Thread(new ThreadStart(startBegin));
            t.Start();

            //startBegin.Invoke();
        }
        //
        //
        // *********************************************************
        // ****                 Try Insert()                    ****
        // *********************************************************
        /// <summary>
        /// Loads events from files (earlier than those already loaded) and inserts them to the front of the series.
        /// </summary>
        /// <param name="newStartTime"></param>
        /// <returns></returns>
        public bool TryInsert(DateTime newStartTime)
        {
            if (m_FileStart == 0)
                return false;                                           // already at start of files
            StartDate = newStartTime;
            int fileIndex = m_FileStart - 1;                            // try to read the next file
            List<Node> nodeList = new List<Node>();
            while (fileIndex >= 0 && m_Files.Keys[fileIndex].CompareTo(StartDate) > 0) 
            {
                nodeList.InsertRange(0, ReadNodesFromFile(fileIndex));   // reads and strips down all nodes found in this file.
                m_FileStart = fileIndex;
                fileIndex--;
            }
            AddToEventSeries(ref nodeList, true);
            return true;
        }// TryInsert()
        //
        public void BeginTryInsert(DateTime newStartTime)
        {
            Action startBegin = () =>
            {
                if (this.TaskStarted != null)
                    this.TaskStarted(this, EventArgs.Empty);

                this.TryInsert(newStartTime);

                if (this.TaskCompleted != null)
                    this.TaskCompleted(this, EventArgs.Empty);
            };
            Thread t = new Thread(new ThreadStart(startBegin));
            t.Start();
            //startBegin.Invoke();
        }
        //
        #endregion//Public methods



        #region Public Static Methods
        // *****************************************************************
        // ****               Public Static Methods                     ****
        // *****************************************************************
        //
        // 
        //
        //
        // ****                 Find All Files()                    ****
        /// <summary>
        /// Locates all files below the pathBase directory, in directories formatted "yyyyMMdd", 
        /// and with fileNames formated as "HHmmss_..." containing the FileBase string.
        /// </summary>
        /// <returns>Number of files discovered.</returns>
        public static int FindAllFiles(string pathBase, string filePattern, SortedList<DateTime,string> fileList = null)
        {
            int fileCounter = 0;
            string[] dirPaths = System.IO.Directory.GetDirectories(pathBase);
            foreach (string dirPath in dirPaths)
            {
                DateTime dirDate;
                int n = dirPath.LastIndexOf("\\");
                string dirName = dirPath.Substring(n + 1, dirPath.Length - (n + 1));
                if (DateTime.TryParseExact(dirName, "yyyyMMdd", new System.Globalization.CultureInfo("en-US"), System.Globalization.DateTimeStyles.None, out dirDate))
                {
                    string[] filePaths = System.IO.Directory.GetFiles(dirPath);
                    foreach (string filePath in filePaths)
                    {
                        if (filePath.Contains(filePattern))
                        {   // This is the kinda file we want to keep track of.
                            fileCounter += 1;
                            if (fileList != null)
                            {   // We can skip these details, if we just want to COUNT the number of files present.
                                // To do this user can call 
                                n = filePath.LastIndexOf("\\");
                                string fileName = filePath.Substring(n + 1, filePath.Length - (n + 1));
                                n = fileName.IndexOf('_');
                                fileName = fileName.Substring(0, n);                 // first part, before first "_" should be the time code
                                DateTime dt2;
                                if (DateTime.TryParseExact(fileName, "HHmmss", new System.Globalization.CultureInfo("en-US"), System.Globalization.DateTimeStyles.AssumeLocal, out dt2))
                                {   // This is a file we want.

                                    DateTime fullTimeStamp = dirDate.Subtract(dirDate.TimeOfDay);             // Get the date part
                                    fullTimeStamp = fullTimeStamp.Add(dt2.TimeOfDay);                               // add the time part.
                                    while (fileList.ContainsKey(fullTimeStamp))
                                        fullTimeStamp = fullTimeStamp.AddTicks(1);
                                    fileList.Add(fullTimeStamp, filePath);                                     // add the full path to this file, with its time stamp.
                                }
                            }
                        }
                    }
                }
            }
            return fileCounter;
        }// FindAllFiles()
        //      
        //
        //
        // *********************************************************
        // ***          TryGetAllUniqueUserNames()              ****
        // *********************************************************
        /// <summary>
        /// Searches the base directory, and its subdirectories for drop files and collects all 
        /// unique user names.
        /// </summary>
        /// <param name="basePath"></param>
        /// <param name="userNames"></param>
        /// <returns></returns>
        public static bool TryGetAllUserNames(string basePath, out List<string> userNames, int daysPriorToSearch=2)
        {
            userNames = new List<string>();
            try
            {
                // Assuming the directories below the basePath are named "yyyyMmdd", we need to search only those
                // several days old (given by the parameter).
                List<string> dirPaths = new List<string>(System.IO.Directory.GetDirectories(basePath));
                if (dirPaths.Count == 0)
                    return false;                                                       // No subdirectories discovered!                
                dirPaths.Sort();                                                        // Sort these since dir names are dates!
                int dirPtr = Math.Max(0, (dirPaths.Count - daysPriorToSearch));         // Search only most recent, day or so...
                while (dirPtr < dirPaths.Count)                                         // Here we are collecting ambre user names, only.
                {
                    string[] filePaths = System.IO.Directory.GetFiles(dirPaths[dirPtr]);
                    foreach (string filePath in filePaths)
                    {
                        int fileNameStart = filePath.LastIndexOf('\\') + 1;             // first char position of file name
                        fileNameStart = filePath.IndexOf('_', fileNameStart) + 1;       // point after "HHmmss_" - (the file timestamp is first).
                        fileNameStart = filePath.IndexOf('_', fileNameStart) + 1;       // point after "FillBooks_" or "Fills_" etc... (file descriptor name)
                        string baseName = filePath.Substring(fileNameStart);            // remainder is the userName identifier (like the acct number etc).
                        baseName = baseName.Substring(0, baseName.Length - 4);          // Drop the ".txt" extension.
                        if (!userNames.Contains(baseName))
                            userNames.Add(baseName);                               // store name - this is how ambre identifies users/fill hubs.
                    }
                    dirPtr++;
                }//wend dirPtr
            }// try
            catch (Exception)
            {               
                return false;
            }
            return true;
        }// TryGetAllUserNames()
        //
        //
        //
        //
        //
        //
        #endregion//Public Static Methods



        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        // 
        //
        //
        // ****                  ReadNodesFromFile()                    ****
        /// <summary>
        /// Reads nodes from file associate with fileIndex.
        /// Some nodes (such as FillHubs) are exploded and their components are added to the list.
        /// </summary>
        private List<Node> ReadNodesFromFile(int fileIndex)
        {
            List<Node> nodeList = new List<Node>();

            Misty.Lib.IO.Xml.StringifiableReader reader = new Misty.Lib.IO.Xml.StringifiableReader(m_Files.Values[fileIndex]);
            List<Misty.Lib.IO.Xml.IStringifiable> iStrList = reader.ReadToEnd(true);    // Load stringifiable objects in there.
            foreach (IStringifiable istr in iStrList)                                   // loop thru each, storing those of interest.
            {
                Node aNode = (Node)istr;
                if (aNode.Name.Contains("FillHub"))
                {   // We don't add FillHubs directly, but do add some of their elements (BookLifo objects for example).
                    foreach (IStringifiable iStrElement in aNode.GetElements())         // loop thru elements of the FillHub.
                    {   
                        Node element = (Node)iStrElement;
                        if (element.Name.Contains("InstrumentMapEntry"))
                        {
                            string sKey;
                            InstrumentName name;
                            if (element.Attributes.TryGetValue("Key", out sKey) && !m_KeyToName.ContainsKey(sKey) && InstrumentName.TryDeserialize(element.Attributes["Name"], out name))
                                m_KeyToName.Add(sKey, name);
                        }
                        else if (element.Name.Contains("BookLifo"))
                            nodeList.Add(element);
                    } // next element in FillHub.
                }
                else
                    nodeList.Add(aNode);                                                // add everything else to the list.
            }
            return nodeList;
        }//ReadNodesFromFile()
        //
        //
        //
        //
        /// <summary>
        /// This method has the task of analyzing each node, and adding fills to the series, OR
        /// when FillBooks are encountered, test for self-consistency. 
        /// If we can't identify whose event it is, flag it as don't consume, and leave it in the list.
        /// The method is called each time a new drop file is read, and the resulting nodes are passed in.
        /// The method can be called when a file at the end is read (then we want to append nodes) or prior
        /// to the beginning files (which is an insert).  The flag needs to be set to "insert".
        /// </summary>
        /// <param name="isInsert">true to insert, and false to append (default)</param>
        /// <param name="rawNodeList"></param>
        private void AddToEventSeries(ref List<Node> rawNodeList, bool isInsert = false )
        {
            //
            // Separate all the nodes according to their InstrumentNames
            //
            Dictionary<InstrumentName,List<Node>> masterNodeList = new Dictionary<InstrumentName,List<Node>>();
            InstrumentName name = new InstrumentName();
            bool consumeThisNode = false;
            int ptr = 0;
            while (ptr < rawNodeList.Count)
            {
                Node aNode = rawNodeList[ptr];
                if (aNode.Name.Contains("FillEventArgs"))
                {
                    if (m_KeyToName.TryGetValue(aNode.Attributes["InstrumentKey"], out name))   // determine which Instrument was filled.
                        consumeThisNode = true;                             // If we recognize the instrument, we can accept this FillEventArgs.
                    else
                        consumeThisNode = false;
                }
                else if (aNode.Name.Contains("BookLifo"))
                {
                    if (InstrumentName.TryDeserialize(aNode.Attributes["InstrumentName"], out name) && m_KeyToName.ContainsValue(name) )
                        consumeThisNode = true;                             // consume only those books for which we have a Key mapping.
                    else
                        consumeThisNode = false;
                }
                else
                    consumeThisNode = false;
                //
                // Consume this node, or not.
                //
                if (consumeThisNode)
                {
                    if (!masterNodeList.ContainsKey(name))
                        masterNodeList.Add(name, new List<Node>());         // Create a new entry for this instrument.
                    masterNodeList[name].Add(aNode);
                    rawNodeList.RemoveAt(ptr);                              // this node was processed, remove it from list.
                }
                else
                    ptr++;                                                  // leave unprocessed node in the list, move to next one.
            }// while nodes remain to process.

            //
            // Load new EventSeries objects
            //
            foreach (InstrumentName aName in masterNodeList.Keys)
            {
                if ( ! SeriesList.ContainsKey(aName) )
                    SeriesList.Add(aName,new EventSeries(aName));           // Create a new series.
                if (isInsert)
                    SeriesList[aName].Insert(masterNodeList[aName]);
                else
                    SeriesList[aName].Append( masterNodeList[aName] );          // load all the events.
                masterNodeList[aName].Clear();
            }// aName

            //
            // Save all the nodes we could not process. 
            // These are usually nodes for instruments that are unknown to us.
            // TODO: How can we add these when we discover (by reading a different file)?
            if (rawNodeList.Count > 0)
            {
                if (isInsert)
                    m_UnProcessedNodes.InsertRange(0, rawNodeList);
                else
                    m_UnProcessedNodes.AddRange(rawNodeList);
            }
        }// ValidateNodes()
        //
        //
        /// <summary>
        /// This function is added by Cheng to exclude the extra fills from the report.
        /// </summary>
        /// <param name="nodeList"></param>
        //private void CheckExcludeExtraFills(ref List<Node> nodeList, DateTime settlementDateTime)
        //{
        //    string localDateTimeString;
        //    List<Node> neededRemoveNodes = new List<Node>();
        //    DateTime workingItemDateTime = DateTime.MinValue;

        //    foreach (Node node in nodeList)
        //    {
        //        if (node.Name.Contains("FillEventArgs"))
        //        {
        //            if (node.Attributes.ContainsKey("LocalTime"))
        //            {
        //                localDateTimeString = node.Attributes["LocalTime"];
        //                if (DateTime.TryParse(localDateTimeString, out workingItemDateTime))
        //                {
        //                    if (DateTime.Compare(workingItemDateTime, settlementDateTime) > 0)
        //                        neededRemoveNodes.Add(node);
        //                }
        //            }
        //        }
        //    }

        //    foreach (Node removedNode in neededRemoveNodes)
        //    {
        //        if (nodeList.Contains(removedNode))
        //            nodeList.Remove(removedNode);
        //    }
        //}
        //
        //
        //
        #endregion//Private Methods




    }
}
