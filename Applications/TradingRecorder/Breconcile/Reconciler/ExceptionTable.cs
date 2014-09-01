using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

    /// <summary>
    /// This class represents a table to list all exception InstrumentNames (Products) 
    /// we will ignore.  
    /// </summary>
namespace Ambre.Breconcile.Reconciler
{
    using Misty.Lib.Products;
    public class ExceptionTable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        // Control Variables
        //
        public string FilePath = string.Empty;

        //
        // public variables
        public List<InstrumentName> m_ExceptionList = new List<InstrumentName>();

        #endregion// members

        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath">Full path of file with table.</param>
        public ExceptionTable(string filePath)
        {
            this.FilePath = filePath;
            ReadTable();
        }
        //
        //       
        #endregion//Constructors


        private void ReadTable()
        {
            // Check whether the file exists, if otherwise, add a file with blank.
            if (!System.IO.File.Exists(this.FilePath))
            {
                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(this.FilePath, true))
                {
                    writer.WriteLine("// END");
                    writer.Close();
                }
            }

            // The above code ensures that the file exists.
            using (System.IO.StreamReader reader = new System.IO.StreamReader(this.FilePath))
            {
                string aLine;
                bool continueReading = true;
                while (continueReading)
                {
                    aLine = reader.ReadLine();
                    if (aLine == null)
                        break;
                    aLine = aLine.Trim();
                    if (string.IsNullOrWhiteSpace(aLine))
                        continue;                                   // skip blank lines
                    if (aLine.StartsWith("// END", StringComparison.CurrentCultureIgnoreCase)) // this signals end of file at the moment.
                    {
                        continueReading = false;
                        continue;
                    }
                    else if (aLine.Contains("//"))
                    {
                        int n = aLine.IndexOf("//");
                        if (n == 0)
                            continue;
                        else if (n > 0)
                            aLine = aLine.Substring(0, n);
                    }

                    //
                    // Extract table entries
                    //
                    InstrumentName instrumentName;
                    if (InstrumentName.TryDeserialize(aLine.Trim(), out instrumentName))
                    {
                        m_ExceptionList.Add(instrumentName);
                    }
                }
                reader.Close();
            }//wend
        }

    }// ReadTable()

}