using System;
using System.Collections.Generic;
using System.Collections.Concurrent;            // for queue
using System.Text;

namespace UV.Lib.IO.Drops
{
    using UV.Lib.Hubs;
    using UV.Lib.Utilities;

    /// <summary>
    /// The simplest queued line-writer .  It could be used for logs, or drop copies.
    /// It has no log of its own, optionally, a LogHub can be handed to it at construction.
    /// It has a concurrent queue, to events (with messages to write, perhaps) can be pushed on 
    /// a macroscopic lock.  It also implements RecyclingFactor for its private event args to reduce
    /// overhead (and time).
    /// Usage:
    ///     1. Choose a fileName, directoryPath for use for output.
    ///     2. Set values to control if/when to flush buffer and perform write.
    ///         2a. MinimumLinesToWrite - must have at least this many lines in buffer to write.
    ///         2b. WriteDelaySecs - number of seconds to wait between check writing condition.
    ///     3. Flushes buffers automatically when Stop is requested.
    /// </summary>
    public class DropQueueWriter : HubBase
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // Components
        //
        protected LogHub Log = null;                               // optional outside Log. Pass LogHub in during construction, if you like.
        private ConcurrentQueue<DropQueueWriterEventArgs> m_WriteQueue = new ConcurrentQueue<DropQueueWriterEventArgs>();   // outgoing queue to write
        private RecycleFactory<DropQueueWriterEventArgs> m_Factory = new RecycleFactory<DropQueueWriterEventArgs>();        // recycling for user eventArgs.

        //
        // Control parameters
        //        
        public string m_PathName = string.Empty;                    // full path to base "drop" location.
        public string m_FileName = string.Empty;
        
        public bool AppendToDropFile = true;                        // Append to file FileName, if it already exists, otherwise delete.
        public int MinimumLinesToWrite = 0;                         // Writes drop file after elapsed time, if there are at least this many lines to write.        

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pathName">User provides directory base path (with trailing \\).</param>
        /// <param name="fileName"></param>
        /// <param name="logToUse"></param>
        public DropQueueWriter(string pathName, string fileName, LogHub logToUse=null)
            : base("DropQueueWriter")
        {
            if (logToUse != null)
                this.Log = logToUse;
            m_WaitListenUpdatePeriod = 60 * 1000;               // 60 seconds write checking.
            
            m_PathName = pathName;                              // Set output file name...
            m_FileName = fileName;
            RequestChangeFileName(m_PathName, m_FileName);      // Tell the thread (about to start) to initialize the current path/filename as soon as it starts.

        }
        //
        //       
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        protected int WriteDelaySecs
        {
            get { return base.m_WaitListenUpdatePeriod / 1000; }
            set { m_WaitListenUpdatePeriod = Math.Max(1000, value * 1000); }    // convert value to msecs.
        }
        protected string FilePath
        {
            get { return string.Format("{0}{1}", m_PathName, m_FileName); }
        }
        //
        //
        #endregion//Properties


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        // Note: All these public methods, are assumed to be called by an outside thread.
        //
        //
        /// <summary>
        /// Users call this function to push a line to write onto queue.
        /// Note that the order that requests are received, they are processed.
        /// </summary>
        /// <param name="aLineToWrite"></param>
        /// <returns></returns>
        public bool RequestEnqueue(string aLineToWrite)
        {
            if (base.ListenState == WaitListenState.Stopping)
                return false;                               // Queue no longer accepting new requests.
            DropQueueWriterEventArgs eventArgs = GetRequest(DropQueueWriterRequestType.WriteLine);
            eventArgs.Message.Append(aLineToWrite);
            this.HubEventEnqueue(eventArgs);                // doing it this way gives more work to hub thread, but maintains the ordering of all tasks.
            return true;
        }// RequestEnqueue()
        public bool RequestEnqueue(string format, object arg)
        { 
            return RequestEnqueue(string.Format(format, arg)); 
        }
        public bool RequestEnqueue(string format, object[] args) 
        { 
            return RequestEnqueue(string.Format(format, args)); 
        }
        //
        //
        //
        public bool RequestFlushNow()
        {
            return this.HubEventEnqueue(GetRequest(DropQueueWriterRequestType.FlushNow));
        }
        //
        //
        /// <summary>
        /// Requests that the current drop file be copied to specified directory/filename.
        /// (If either of these strings are string.Empty, then the current value is used;
        /// that is, if you want to save the current drop file named, say, "Drop.txt" into a directory
        /// called "C:\\Archive\\" with the same file name, call with arguments ("C:\\Archive\\",string.Empty)
        /// </summary>
        /// <param name="targetPath">Archival directory path (with trailing "\\")</param>
        /// <param name="targetFileName">Archival file name.</param>
        /// <returns></returns>
        public bool RequestCopyTo(string targetPath, string targetFileName)
        {
            DropQueueWriterEventArgs eventArgs = GetRequest(DropQueueWriterRequestType.CopyTo);
            eventArgs.Message.Append(targetFileName);
            eventArgs.Message2.Append(targetPath);
            return this.HubEventEnqueue(eventArgs);
        }
        /// <summary>
        /// Request that all files in local path (that contain the substring pattern provided) are copied to the
        /// target path directory.
        /// </summary>
        /// <param name="targetPath">has trailing "\\"</param>
        /// <param name="fileSubstringPattern"></param>
        /// <returns></returns>
        public bool RequestCopyAllFiles(string targetPath, string fileSubstringPattern)
        {
            DropQueueWriterEventArgs eventArgs = GetRequest(DropQueueWriterRequestType.CopyAllFiles);
            eventArgs.Message.Append(fileSubstringPattern);
            eventArgs.Message2.Append(targetPath);
            return this.HubEventEnqueue(eventArgs);
        }
        public bool RequestMoveTo(string targetPath, string targetFileName)
        {
            DropQueueWriterEventArgs eventArgs = GetRequest(DropQueueWriterRequestType.MoveTo);
            eventArgs.Message.Append(targetFileName);
            eventArgs.Message2.Append(targetPath);
            return this.HubEventEnqueue(eventArgs);
        }
        //
        //
        //
        public bool RequestChangeFileName(string targetPath, string targetFileName)
        {
            DropQueueWriterEventArgs eventArgs = GetRequest(DropQueueWriterRequestType.ChangeFileName);
            eventArgs.Message.Append(targetFileName);
            eventArgs.Message2.Append(targetPath);
            return this.HubEventEnqueue(eventArgs);
        }
        //
        //
        //public override void Start()
        //{
        //    //if (!TrySetOutputFile(m_PathName,m_FileName))
            //    return;
        //    base.Start();                                       // start my thread.
        //}// Start().
        //
        public override void RequestStop()
        {
            this.HubEventEnqueue(GetRequest(DropQueueWriterRequestType.Stop));
        }
        //
        //
        public void SetLog(LogHub newLogHub)
        {
            this.Log = newLogHub;
        }
        //
        //
        //
        //
        //
        //
        #endregion//Public Methods


        #region Private Utility Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// This method gives the caller a eventarg object to use.  It may be new
        /// or recycled from previous usage.
        /// </summary>
        /// <param name="requestType"></param>
        /// <returns></returns>
        private DropQueueWriterEventArgs GetRequest(DropQueueWriterRequestType requestType)
        {
            DropQueueWriterEventArgs eventArgs = m_Factory.Get();
            eventArgs.Clear();
            eventArgs.Request = requestType;
            return eventArgs;
        }
        //
        //
        private bool TrySetOutputFile(string path, string filename)
        {
            // update path
            if (!path.EndsWith("\\"))
                path = string.Format("{0}\\", path);

            // Update file name
            if (string.IsNullOrEmpty(filename) || filename.Length < 1)
                filename = string.Format("DropQueueWriterOutput.txt");      // the user has given us a directory, not a file name.

            if (IsDirectoryExists(path))                                    // Checks path, or tries to create the dir.
            {
                m_PathName = path;
                m_FileName = filename;
                try
                {
                    if (!this.AppendToDropFile && System.IO.File.Exists(this.FilePath))
                    {
                        if (Log != null)
                            Log.NewEntry(LogLevel.Minor, "{0}: Deleting old drop file {1} as desired.", this.m_HubName, this.FilePath);
                        System.IO.File.Delete(this.FilePath);
                    }
                    return true;
                }
                catch (Exception e)
                {
                    if (Log != null)
                        Log.NewEntry(LogLevel.Minor, "{0}: Failed to delete old drop file {1}. Exception {2}", this.m_HubName, this.FilePath,e.Message);
                    return false;
                }
            }
            else
                return false;
        } // TrySetOutputFile() 
        //
        //
        // ****             Is Directory Exists()               ****
        //
        private bool IsDirectoryExists(string directoryPath)
        {
            if (!System.IO.Directory.Exists(directoryPath))                     // Confirm existence of the output directory. 
            {
                try
                {
                    System.IO.Directory.CreateDirectory(directoryPath);
                }
                catch (Exception e)
                {
                    if (Log != null)
                        Log.NewEntry(LogLevel.Major, "{2}: Unable to create directory {0}. Exception = {1}", directoryPath,e.Message.Trim(), m_HubName);
                    return false;
                }
            }
            return true;
        } // IsDirectoryExists
        //
        //
        //
        #endregion // private methods


        #region HubEventHandler and Processing 
        // *********************************************************
        // ****             Hub Event Handler()                 ****
        // *********************************************************
        protected override void HubEventHandler(EventArgs[] eventArgList)
        {
            foreach (EventArgs eventArg in eventArgList)
            {
                Type eventType = eventArg.GetType();
                if (eventType == typeof(DropQueueWriterEventArgs))
                {
                    DropQueueWriterEventArgs e = (DropQueueWriterEventArgs)eventArg;
                    switch (e.Request)
                    {
                        case DropQueueWriterRequestType.WriteLine:
                            m_WriteQueue.Enqueue(e);                    // Store the entire eventArg containing message in queue (easier to recycle later).
                            break;
                        case DropQueueWriterRequestType.Stop:                            
                            ProcessFlushNow();
                            base.Stop();
                            break;
                        case DropQueueWriterRequestType.FlushNow:
                            ProcessFlushNow();
                            break;
                        case DropQueueWriterRequestType.CopyTo:
                            ProcessCopyTo(e);
                            break;
                        case DropQueueWriterRequestType.CopyAllFiles:
                            ProcessCopyAllFiles(e);
                            break;
                        case DropQueueWriterRequestType.MoveTo:
                            ProcessMoveTo(e);
                            break;
                        case DropQueueWriterRequestType.ChangeFileName:
                            ProcessTargetFileChange(e);
                            break;
                        default:
                            if (Log!=null)
                                Log.NewEntry(LogLevel.Major,"{0}: Unknown request {1}.",this.m_HubName,e.Request);
                            break;
                    }//switch()
                }

            }
        }// HubEventHandler()
        //
        //
        // *********************************************************************
        // ****                     Process Write Now()                     ****
        // *********************************************************************
        /// <summary>
        /// Force a write of the contents of the message queue to the output file now.
        /// </summary>
        private void ProcessFlushNow()
        {
            if (string.IsNullOrEmpty(this.FilePath) || m_WriteQueue.Count == 0)
                return;
            if (Log != null)
                Log.NewEntry(LogLevel.Minor, "{0}.ProcessFlushNow: ", this.m_HubName);
            try
            {
                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(this.FilePath, true, Encoding.ASCII))
                {
                        DropQueueWriterEventArgs e;
                        while (m_WriteQueue.TryDequeue(out e))
                        {
                            writer.WriteLine(e.Message);
                            m_Factory.Recycle(e);
                        }
                        writer.Close();
                }//using StreamWriter
            }
            catch (Exception e)
            {
                if (Log != null)
                    Log.NewEntry(LogLevel.Error, "{0}: Exception writing to drop file. {1}", this.m_HubName, e.Message);
            }

        }// ProcessWriteNow().
        //
        //
        // *********************************************************************
        // ****                     Process CopyTo()                        ****
        // *********************************************************************
        /// <summary>
        /// 
        /// </summary>
        private void ProcessCopyTo(DropQueueWriterEventArgs eventArgs)
        {
            // Create name for archival file.
            string archiveDirPath;
            string archivalFileName;
            if (eventArgs.Message2.Length == 0)              // Check if user didn't provided path to copy file to
                archiveDirPath = m_PathName;                // keep the same old path, then.
            else
            {                                               // User copying to new dir.                
                if (eventArgs.Message2[eventArgs.Message2.Length-1].Equals('\\'))
                    archiveDirPath = eventArgs.Message2.ToString();
                else
                {
                    eventArgs.Message2.Append("\\");
                    archiveDirPath = eventArgs.Message2.ToString();
                }
            }
            if (eventArgs.Message.Length == 0)              // If no fileName supplied, keep the current filename. //(string.IsNullOrEmpty(eventArgs.Message))
                archivalFileName = m_FileName;
            else
                archivalFileName = eventArgs.Message.ToString();

            // Confirm target directory exists.
            if ( ! IsDirectoryExists(archiveDirPath))
            {
                if (Log != null)                            // Cannot find/create target directory.  Just fail quietly.
                    Log.NewEntry(LogLevel.Major, "{1}: Failed to copying drop to {0}.", archiveDirPath, this.m_HubName);
            }
            else
            {   // Copy current file to archival location.
                string currentFilePath = string.Format("{0}{1}", m_PathName, m_FileName);
                if (System.IO.File.Exists(currentFilePath))
                {
                    string archiveFilePath = string.Format("{0}{1}", archiveDirPath, archivalFileName);
                    try
                    {
                        if (System.IO.File.Exists(archiveFilePath))
                        {
                            System.IO.FileInfo fileInfoArchive = new System.IO.FileInfo(archiveFilePath);
                            System.IO.FileInfo fileInfoCurrent = new System.IO.FileInfo(currentFilePath);
                            if (fileInfoCurrent.Length > fileInfoArchive.Length)
                            {
                                if (Log != null)
                                    Log.NewEntry(LogLevel.Major, "DropQueueWriter: Copying drop to archive directory {0}.", archiveFilePath);
                                System.IO.File.Copy(currentFilePath, archiveFilePath, true);
                            }
                            else if (Log != null)
                                Log.NewEntry(LogLevel.Major, "DropQueueWriter: File in archival area seems bigger, will not over-write it!");

                        }
                        else
                        {
                            if (Log != null)
                                Log.NewEntry(LogLevel.Major, "DropQueueWriter: Copying drop to archive directory {0}.", archiveFilePath);
                            System.IO.File.Copy(currentFilePath, archiveFilePath,true);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (Log != null)
                            Log.NewEntry(LogLevel.Major, "{2}: Failed to move drop to archive directory {0}. {1}", archiveFilePath, ex.Message, this.m_HubName);
                    }
                }
                else if (Log != null)
                    Log.NewEntry(LogLevel.Major, "DropQueueWriter.ProcessCopyTo: File to copy does not exist {0}.", currentFilePath);
            }// if archiveDirPath exists
            // Exit.
            m_Factory.Recycle(eventArgs);
        }// ProcessCopyTo()
        //
        //
        //
        /// <summary>
        /// Copies all files in the local Path (that match a given filename pattern) to
        /// another path.
        /// </summary>
        /// <param name="eventArgs"></param>
        private void ProcessCopyAllFiles(DropQueueWriterEventArgs eventArgs)
        {
            // Create name for archival file.
            string archiveDirPath;
            if (eventArgs.Message2.Length == 0)
            {
                m_Factory.Recycle(eventArgs);
                return;                                                 // User must provide a target DirPath!
            }
            else
            {
                if (eventArgs.Message2[eventArgs.Message2.Length - 1].Equals('\\')) // .EndsWith("\\"))
                    archiveDirPath = eventArgs.Message2.ToString();     // user provided a target path
                else
                    archiveDirPath = string.Format("{0}\\", eventArgs.Message2);
            }
            try
            {
                if (!IsDirectoryExists(archiveDirPath))                 // Confirm target directory exists.
                {   // Cannot find/create target directory.  
                    if (Log != null)
                        Log.NewEntry(LogLevel.Major, "{1}: Failed to copy all files to {0}.  Failed to find/create that directory.", archiveDirPath, this.m_HubName);
                    m_Factory.Recycle(eventArgs);
                    return;                                             // nothing we can do but quit.
                }
                // Get Local filenames to copy.
                List<string> fileNamesWeCopied = new List<string>();
                string[] localFileNameArray;
                if (eventArgs.Message.Length > 0)                      // pattern provided in ".FileName" property
                    localFileNameArray = System.IO.Directory.GetFiles(this.m_PathName, eventArgs.Message.ToString());
                else
                    localFileNameArray = System.IO.Directory.GetFiles(this.m_PathName);// no pattern provided.
                foreach (string s in localFileNameArray)
                {
                    string fileName = s.Substring(s.LastIndexOf('\\')+1);                           // local file name.                    
                    string archiveFilePath = string.Format("{0}{1}", archiveDirPath, fileName);     // archive file path
                    try
                    {
                        if (System.IO.File.Exists(archiveFilePath))
                        {   // This file already exists in the output directory.
                            System.IO.FileInfo infoRemote = new System.IO.FileInfo(archiveFilePath);
                            System.IO.FileInfo infoLocal = new System.IO.FileInfo(s);
                            if (infoLocal.Length > infoRemote.Length)
                            {   // The local file is bigger, so we better do the copy!
                                if (Log != null)
                                    Log.NewEntry(LogLevel.Major, "{1}.ProcessCopyAllFiles: Local copy of {0} is larger than remote, so we will copy to remote.", fileName, this.m_HubName);
                                System.IO.File.Copy(s, archiveFilePath,true);
                                fileNamesWeCopied.Add(fileName);
                            }
                        }
                        else
                        {   // File doesn't already exist, so just copy it.
                            System.IO.File.Copy(s, archiveFilePath,true);
                            fileNamesWeCopied.Add(fileName);
                        }
                    }
                    catch (Exception e)
                    {
                        if (Log!=null)
                            Log.NewEntry(LogLevel.Major, "{1}.ProcessCopyAllFiles: Failed to copy file to {0}.  Exception {2}.", archiveDirPath, this.m_HubName,e.Message);
                    }
                }
                // Report our results
                if (Log!=null && Log.BeginEntry(LogLevel.Minor) )
                {
                    Log.AppendEntry("{1}.ProcessCopyAllFiles: Copied {0} files [",fileNamesWeCopied.Count,this.m_HubName);
                    foreach (string s in fileNamesWeCopied)
                        Log.AppendEntry(" {0}", s);
                    Log.AppendEntry("].");
                    Log.EndEntry();                        
                }
            }
            catch (Exception e)
            {
                if (Log != null)
                    Log.NewEntry(LogLevel.Major, "{2}: Copying all files from {0} to {1} failed. Exception: {2}", this.FilePath, archiveDirPath, e.Message, this.m_HubName);
            }
            // Exit
            m_Factory.Recycle(eventArgs);
        }// ProcessCopyAllFiles()
        //
        //
        // *********************************************************************
        // ****                     Process MoveTo()                        ****
        // *********************************************************************
        private void ProcessMoveTo(DropQueueWriterEventArgs eventArgs)
        {
            string currentFilePath = string.Format("{0}{1}", m_PathName, m_FileName); 
            // Set target path
            string targetPath;
            if (eventArgs.Message2.Length == 0)  //string.IsNullOrEmpty(eventArgs.Message2))
                targetPath = m_PathName;                        // keep original path.
            else if (eventArgs.Message2[eventArgs.Message2.Length - 1].Equals('\\'))
                targetPath = eventArgs.Message2.ToString();     // user provided a target path
            else
                targetPath = string.Format("{0}\\", eventArgs.Message2);
            // Set target file name.
            string targetFileName;
            if (eventArgs.Message.Length == 0)   //string.IsNullOrEmpty(eventArgs.Message))
                targetFileName = m_FileName;                    // Keep original filename
            else
                targetFileName = eventArgs.Message.ToString();  // User provided filename also.
            // Move the file now.
            try
            {
                // Create directory
                if (!System.IO.Directory.Exists(targetPath))
                    System.IO.Directory.CreateDirectory(targetPath);
                // Move file
                string outFilePath = string.Format("{0}{1}", targetPath, targetFileName);
                if (System.IO.File.Exists(currentFilePath))
                {
                    if (System.IO.File.Exists(outFilePath))
                        System.IO.File.Delete(outFilePath);                 // delete any existing file in backup output area
                    System.IO.File.Move(currentFilePath, outFilePath);      // stick current file there - for recovery purposes.
                }
            }
            catch (Exception e)
            {
                if (Log != null)
                    Log.NewEntry(LogLevel.Major, "{1}: ProcessMoveTo failed.  Exception: {0}", e.Message,this.m_HubName);
            }
            // Exit.
            m_Factory.Recycle(eventArgs);
        }//ProcessMoveFile()
        //
        //
        //
        //
        // *****************************************************************************
        // ****                 Process Target File Change()                        ****
        // *****************************************************************************        
        private void ProcessTargetFileChange(DropQueueWriterEventArgs eventArgs)
        {
            // Flush the buffer, before we change the output target.
            if (m_WriteQueue.Count > 0)
                ProcessFlushNow();
            try
            {
                // Change target dir
                if (eventArgs.Message2.Length > 0)  //(!string. IsNullOrEmpty(eventArgs.Message2))
                {   // User wants to change target directory.
                    if (IsDirectoryExists(eventArgs.Message2.ToString()))
                        m_PathName = eventArgs.Message2.ToString(); // todo: confirm the trailing \\
                    else if (Log != null)
                        Log.NewEntry(LogLevel.Major, "{1}: Failed to created new directory {0}", eventArgs.Message2, this.m_HubName);
                }
                if (eventArgs.Message.Length > 0)//(!string.IsNullOrEmpty(eventArgs.Message))
                    m_FileName = eventArgs.Message.ToString();
                // Check if file exists
                string filePath = string.Format("{0}{1}", m_PathName, m_FileName);
                if (!this.AppendToDropFile && System.IO.File.Exists(filePath))
                {
                    if (Log!=null)
                        Log.NewEntry(LogLevel.Minor, "{1}: Deleting pre-existing drop file {0}", filePath,this.m_HubName);
                    System.IO.File.Delete(filePath);
                }
            }
            catch (Exception e)
            {
                if (Log != null)
                    Log.NewEntry(LogLevel.Major, "{1}: ProcessTargetFileChange failed.  Exception: {0}",e.Message,this.m_HubName);
            }
            // Exit
            m_Factory.Recycle(eventArgs);
        } // ProcessTargetFileChange()
        //
        //
        //
        // *****************************************************************************
        // ****                         Update Periodic()                           ****
        // *****************************************************************************
        protected override void UpdatePeriodic()
        {
            if (!m_WriteQueue.IsEmpty && m_WriteQueue.Count > MinimumLinesToWrite)
                this.HubEventEnqueue( this.GetRequest(DropQueueWriterRequestType.FlushNow) );   //this.HubEventEnqueue(new DropQueueWriterEventArgs(DropQueueWriterRequestType.FlushNow));
        }//UpdatePeriodic().
        //
        //
        //
        #endregion//Private Methods




    }
}
