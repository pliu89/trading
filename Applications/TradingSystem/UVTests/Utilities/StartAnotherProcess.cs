using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace UVTests.Utilities
{
    public static class StartAnotherProcess
    {

        public static void Start(string[] args)
        {
            string configFilename = "Config.txt";
            string logFilename = "StartLog.txt";
            string configPath = string.Empty;


            //
            // Find a local config file.
            //
            string currentPath = Directory.GetCurrentDirectory();
            int ptr;
            if (string.IsNullOrEmpty(configPath) && (ptr = currentPath.LastIndexOf("\\bin")) >= 0)     // search outside bin dir.
            {
                string s = currentPath.Substring(0,ptr);
                string[] files = Directory.GetFiles(s, string.Format("{0}", configFilename));
                if (files.Length > 0)
                {   // We found it!
                    configPath = files[0];
                }
            }            
            if (string.IsNullOrEmpty(configPath) && (ptr = currentPath.LastIndexOf("\\Release")) >= 0)
            {
                string s = currentPath.Substring(0, ptr);                
                string[] files = Directory.GetFiles(s, string.Format("{0}", configFilename));
                if (files.Length > 0)
                {   // We found it!
                    configPath = files[0];
                }
            }
            if (string.IsNullOrEmpty(configPath))
            {
                string s = currentPath;
                string[] files = Directory.GetFiles(s, string.Format("{0}", configFilename));
                if (files.Length > 0)
                {   // We found it!
                    configPath = files[0];
                }
            }
            if (string.IsNullOrEmpty(configPath))
            {   // Failed to find local config file.
                System.Windows.Forms.MessageBox.Show(string.Format("Failed to find local file named: {0}.", configFilename),"StartCode" );
                return;
            }
            //
            // Set up a log file.
            //
            string logPath = string.Format("{0}\\{1}", configPath.Substring(0, configPath.LastIndexOf('\\')), logFilename);
            try
            {
                using (System.IO.StreamWriter writer = new StreamWriter(logPath, false))
                {
                    writer.WriteLine("Started {0}", DateTime.Now);
                    writer.WriteLine("Found config file {0}", configPath);

                }
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(string.Format("Failed to open local log file named: {0}.//{1}", logPath, e.Message), "StartCode");
                return;
            }

            
            //
            // Load config file.
            //
            string[] lines = System.IO.File.ReadAllLines(configPath);
            foreach (string aLine in lines)
            {
                string cleanLine;
                ptr = aLine.IndexOf('/');
                if (ptr >= 0)
                    cleanLine = aLine.Substring(0,ptr).Trim();
                else
                    cleanLine = aLine;

                if (! String.IsNullOrEmpty(cleanLine) && ! cleanLine.StartsWith("/")  )
                {
                    // Set working directory
                    string baseDir = cleanLine.Substring(0,cleanLine.LastIndexOf('\\'));
                    System.IO.Directory.SetCurrentDirectory(baseDir);
                    // Write log report
                    using (System.IO.StreamWriter writer = new StreamWriter(logPath,true))
                    {
                        writer.WriteLine(" ----------------------------- ");
                        writer.WriteLine("Starting task in directory {0}", baseDir);
                        writer.WriteLine("Starting task at {0}", DateTime.Now);
                        writer.WriteLine("Executable {0}", cleanLine );

                    }
                    // Process
                    Process process = new Process();
                    process.StartInfo.FileName = cleanLine;
                    process.StartInfo.WorkingDirectory = baseDir;
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                    process.Start();
                    process.WaitForExit();
                    using (System.IO.StreamWriter writer = new StreamWriter(logPath, true))
                    {
                        writer.WriteLine("Completed task at {0}", DateTime.Now);
                    }                
                }
            }
            //
            // Exit
            //
            using (System.IO.StreamWriter writer = new StreamWriter(logPath, true))
            {
                writer.WriteLine("Completed task at {0}", DateTime.Now);
            }

        }// Start()



    }
}
