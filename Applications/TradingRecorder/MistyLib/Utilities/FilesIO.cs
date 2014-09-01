using System;
using System.Collections.Generic;
using System.Text;

namespace Misty.Lib.Utilities
{
    /// <summary>
    /// These are static and helper functions for common IO stuff I like.
    /// </summary>
    public class FilesIO
    {




        #region Static Public Methods
        // *****************************************************************
        // ****                     Static Methods                      ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// baseLogPath = "C:\User\Log\" is the expected path.
        /// </summary>
        /// <param name="baseLogPath">The log path that new directories will be put into.</param>
        /// <param name="daysToKeepLogs">Number of days to keep previous dated directories.</param>
        /// <returns>Log dir for today "2012-May-08\\"</returns>
        public static string GetTodaysLogDirAndClean(string baseLogPath, int daysToKeepLogs=14)
        {
            // Validate
            if (!baseLogPath.EndsWith("\\"))
                baseLogPath = baseLogPath + "\\";

            // Find all log directories.
            string today = DateTime.Now.ToString("yyyy-MMM-dd");
            if (System.IO.Directory.Exists(baseLogPath))
            {
                string[] dirList = System.IO.Directory.GetDirectories(baseLogPath);
                DateTime dtToday = DateTime.Parse(today);	// makes this first moment of today!
                foreach (string dirName in dirList)
                {
                    string[] dateStr = dirName.Split('\\');
                    //DateTime dt1 = DateTime.Parse(dateStr[dateStr.Length - 1]);
                    DateTime dt1;
                    if (DateTime.TryParse(dateStr[dateStr.Length - 1], out dt1))	// Only mess with directories of the prescribed form.
                    {
                        DateTime dt2 = dt1.AddDays(daysToKeepLogs);
                        int isOld = dt2.CompareTo(dtToday);
                        if (isOld < 0) { System.IO.Directory.Delete(dirName, true); }	// recursively delete directory!
                    }
                }//next dirName
            }

            // return today's full path
            //return baseLogPath + today + "\\";
            return today + "\\";
        }// GetTodaysLogDirAndClean()
        //
        //
        //
        public static string GetPathToDirName(string fullPath, string dirName, bool useExactDirName)
        {
            string[] pathDirs = fullPath.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            int n = 0;
            bool keepLooking = true;
            while (n < pathDirs.Length && keepLooking)
            {
                if (useExactDirName && pathDirs[n].Equals(dirName))		// user wants exact match
                    keepLooking = false;
                else if (pathDirs[n].Contains(dirName))					// user wants substring match only
                    keepLooking = false;
                n++;	// finaly n = min(length,MrDataLevel+1)
            }
            StringBuilder basePath = new StringBuilder();
            int m = 0;
            while (m < n)
            {
                basePath.AppendFormat("{0}\\", pathDirs[m]);
                m++;
            }
            return basePath.ToString();
        }// GetPathToDirName()
        //
        //
        //
        #endregion//Public Methods



    }//end class
}
