using System;
using System.Collections.Generic;
using System.Text;

namespace Ambre.Breconcile.Ftp
{
    using Misty.Lib.Hubs;
    using Misty.Lib.Application;

    using Renci.SshNet;
    using Renci.SshNet.Sftp;

    /// <summary>
    /// Simplifed wrapper for copying files from a remote sFTP server to a local directory.
    /// </summary>
    public class SimpleFtpReader
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // Services
        private LogHub Log = null;
        private AppInfo m_AppInfo = null;

        // sFTP control variables
        private PrivateKeyFile m_PrivateKeyFile = null;                     // key file pointer        
        //private const string m_Host = "66.150.110.84";                    // address outside of RCG network
        // Original 
        //private const string m_Host = "10.64.33.164";                     // address internal to RCG network - original version
        //private const string m_UserName = "bretrading";
        //private const int m_PortID = 34022;
        //private string m_PrivateKeyFileName = "bretrading.priv";

        // New version - Oct 2013
        private const string m_Host = "files.rcgdirect.com";                // new version Oct 2013
        private const int m_PortID = 34022;
        //private const string m_UserName = "boss_bretrading";
        //private string m_PrivateKeyFileName = "bretrading.priv";
                          

        // Target file patterns - sync files that contain these keywords.
        public string[] m_FilePatterns = new string[] { "POS"};             // desired file name patterns.
        public string[] m_FileNameSuffixes = new string[] { "CSV" };        // desired file extensions



        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public SimpleFtpReader(LogHub log = null)
        {
            m_AppInfo = AppInfo.GetInstance();
            Log = log;
        }
        //
        //       
        #endregion//Constructors



        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// <param name="outDirPath">Local directory containing synched files to compare, string.Format("{0}{1}", m_AppInfo.UserPath, "Statements\\");</param>
        /// <param name="localFileNamesCopied">Full path names of any new files found on ftp site, null if method is false.</param>
        /// <returns>true, if attempt was successful, but if no new files were found, localFileNamesCopied.Count = 0.</returns>
        /// </summary>
        public bool TryCopyNewRemoteFilesToLocal(string FTPUserName, string outDirPath, out List<string> localFileNamesCopied, string FTPKeyPath)
        {
            localFileNamesCopied = null;                            // filepaths that were copied locally to out directory.
            if (!TryInitialize(FTPKeyPath))                                   // Prepare for ftp connection.
                return false;

            // Get list of files on ftp server.
            List<SftpFile> ftpFileList = null;
            if (!TryGetRemoteFileNames(FTPUserName, out ftpFileList))
                return false;

            // Read local files.            
            if (!System.IO.Directory.Exists(outDirPath))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(outDirPath);
                }
                catch (Exception ex)
                {
                    if ( Log!=null)
                        Log.NewEntry(LogLevel.Error, "FtpReader: Cannot find statement directory {0}.  Exception {1}", outDirPath, ex.Message);
                    return false;
                }
            }
            List<string> localFileList = new List<string>();
            string[] fileName = System.IO.Directory.GetFiles(outDirPath);
            foreach (string s in fileName)
            {
                int nPtr = s.LastIndexOf('\\');
                localFileList.Add(s.Substring(nPtr + 1, s.Length - (nPtr + 1)));            // keep file name only.   
            }

            //
            // Determine files we are missing.
            //
            List<SftpFile> filesWeWant = new List<SftpFile>();
            if (Log!=null)
                Log.BeginEntry(LogLevel.Minor, "FtpReader: Discovered");
            foreach (SftpFile ftpFile in ftpFileList)                                       // loop thru files on ftp server...
            {
                string ftpFileName = ftpFile.Name;
                bool isFileOfInterest = true;
                if (isFileOfInterest && m_FilePatterns.Length > 0)                          // test filename prefix.
                {
                    isFileOfInterest = false;
                    foreach (string prefix in m_FilePatterns)
                        isFileOfInterest = isFileOfInterest || ftpFileName.ToUpper().Contains(prefix);
                }
                if (isFileOfInterest && m_FileNameSuffixes.Length > 0)                      // test filename suffix
                {
                    isFileOfInterest = false;
                    foreach (string suffix in m_FileNameSuffixes)
                        isFileOfInterest = isFileOfInterest || ftpFileName.EndsWith(suffix, StringComparison.CurrentCultureIgnoreCase);
                }
                if (isFileOfInterest && !localFileList.Contains(ftpFileName))
                {
                    if (Log!=null)
                        Log.AppendEntry(" {0}", ftpFile.Name);
                    filesWeWant.Add(ftpFile);
                }
            }// next ftpFile
            if (Log != null)
            {
                Log.AppendEntry(" {0} files.", filesWeWant.Count);
                Log.EndEntry();
            }

            // Tries to copy the files in filesWeWant.
            TryCopyFilesWeWant(FTPUserName, filesWeWant, outDirPath, out localFileNamesCopied);





            if (Log != null)
                Log.NewEntry(LogLevel.Minor, "FtpReader: Finished getting all files.");
            return true;
        } // TryCopyNewFilesToLocal()
        //
        //
        //
        // ****         TryCopyFilesWeWant()            ****
        //
        public bool TryCopyFilesWeWant(string FTPUserName, List<SftpFile> filesWeWant, string localTargetDirPath, out List<string> localFileNamesCopied)
        {
            //
            // Try to get from FTP server the files we are missing.
            //
            localFileNamesCopied = new List<string>();
            List<string> linesRead = new List<string>();
            SftpClient client;
            using (client = new SftpClient(m_Host, m_PortID, FTPUserName, m_PrivateKeyFile))
            {
                try
                {
                    client.Connect();
                    foreach (SftpFile file in filesWeWant)
                    {
                        // Read ftp file.
                        linesRead.Clear();
                        linesRead.AddRange(client.ReadLines(file.Name));
                        // Write local file.
                        string localFileName = string.Format("{0}{1}", localTargetDirPath, file.Name);
                        System.IO.File.WriteAllLines(localFileName, linesRead);
                        if (Log != null)
                            Log.NewEntry(LogLevel.Minor, "FtpReader: Downloaded file {0}.", localFileName);
                        localFileNamesCopied.Add(localFileName);
                    }
                    client.Disconnect();
                }
                catch (Exception ex)
                {
                    if (Log != null)
                        Log.NewEntry(LogLevel.Major, "FtpReader: Failed to read. {0}", ex.Message);
                    return false;
                }
            }//using ftpclient
            return true;
        }

        //
        //
        //
        //
        // ****             TryGetRemoteFileNames()               ****
        //
        /// <summary>
        /// Returns a list of all files found on the ftp server.
        /// </summary>
        public bool TryGetRemoteFileNames(string FTPUserName, out List<Renci.SshNet.Sftp.SftpFile> fileList)
        {
            fileList = null;
            SftpClient client;
            using (client = new SftpClient(m_Host, m_PortID, FTPUserName, m_PrivateKeyFile))
            {
                try
                {
                    client.Connect();
                    if (Log != null)
                        Log.NewEntry(LogLevel.Major, "FtpReader: Connected to {0}", m_Host);
                    string workingDir = client.WorkingDirectory;
                    var listDirectory = client.ListDirectory(workingDir);
                    fileList = new List<Renci.SshNet.Sftp.SftpFile>(client.ListDirectory(workingDir));
                    client.Disconnect();
                }
                catch (Exception ex)
                {
                    if (Log != null)
                        Log.NewEntry(LogLevel.Major, "FtpReader: Failed to connect. {0}", ex.Message);
                    return false;
                }
            }
            // Exit
            return true;
        } // TryGetRemoteFileNames()
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
        // *************************************************************
        // ****                 Try Initialize()                    ****
        // *************************************************************
        /// <summary>
        /// Confirms existance and sets up private key file.
        /// </summary>
        /// <returns></returns>
        private bool TryInitialize(string FTPKeyPath)
        {
            // Create the private key file.
            //string privateKeyFilePath = string.Format("{0}{1}", m_AppInfo.UserPath, m_PrivateKeyFileName);
            string privateKeyFilePath = FTPKeyPath;
            if (!System.IO.File.Exists(privateKeyFilePath))
            {
                if (Log != null)
                    Log.NewEntry(LogLevel.Warning, "FtpReader: Failed to locate the private key file at {0}.", privateKeyFilePath);
                return false;
            }
            m_PrivateKeyFile = new PrivateKeyFile(privateKeyFilePath);
            return true;
        }// TryInitialize()
        //
        //

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
