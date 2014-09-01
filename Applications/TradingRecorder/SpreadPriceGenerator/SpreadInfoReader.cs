using System;
using System.Collections.Generic;

namespace SpreadPriceGenerator
{
    using Misty.Lib.Hubs;

    public class SpreadInfoReader
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        //
        //
        // The spread information reader reads basic information including product name, first date delimiter and second date delimiter.
        private static List<string> m_ProductNameList = null;
        private static Dictionary<string, string> m_FirstDateDelimiters = null;
        private static Dictionary<string, string> m_SecondDateDelimiters = null;
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        //      
        /// <summary>
        /// No input for spread information reader class constructor.
        /// </summary>
        public SpreadInfoReader()
        {
            // The constructor instantiate the three objects.
            m_ProductNameList = new List<string>();
            m_FirstDateDelimiters = new Dictionary<string, string>();
            m_SecondDateDelimiters = new Dictionary<string, string>();
        }
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
        //
        //
        //
        //
        /// <summary>
        /// This function reads lines from full path provided.
        /// It will also have logging function.
        /// It creates a output of spread information reader type.
        /// </summary>
        /// <param name="fullPath"></param>
        /// <param name="log"></param>
        /// <param name="outputReader"></param>
        /// <returns></returns>
        public static bool TryReadSpreadInfoTable(string fullPath, LogHub log, out SpreadInfoReader outputReader)
        {
            outputReader = null;
            bool isSuccessful = true;
            if (System.IO.File.Exists(fullPath))
            {
                log.NewEntry(LogLevel.Minor, "The file path of {0} is found successfully", fullPath);

                // Create a IO reader for the provided path.
                using (System.IO.StreamReader reader = new System.IO.StreamReader(fullPath))
                {
                    string lineRead;
                    string[] words;
                    char[] wordDelimiters = new char[] { ',' };
                    bool headerRead = false;
                    string productName;
                    while ((lineRead = reader.ReadLine()) != null)
                    {
                        words = lineRead.Split(wordDelimiters);

                        // The first line read is header line.
                        if (!headerRead)
                        {
                            if (words.Length != SpreadInfoField.ColumnNumber)
                            {
                                log.NewEntry(LogLevel.Warning, "The column number in excel is {0} different with the desired number of {1}", words.Length, SpreadInfoField.ColumnNumber);
                                isSuccessful = false;
                                break;
                            }
                            headerRead = true;
                            outputReader = new SpreadInfoReader();
                        }
                        // The lines after the header line are body, containing field information.
                        else
                        {
                            productName = words[SpreadInfoField.ProductName].Trim();
                            if (!m_ProductNameList.Contains(productName))
                                m_ProductNameList.Add(productName);
                            if (!m_FirstDateDelimiters.ContainsKey(productName))
                                m_FirstDateDelimiters.Add(productName, words[SpreadInfoField.FirstDateDelimiter].Trim());
                            if (!m_SecondDateDelimiters.ContainsKey(productName))
                                m_SecondDateDelimiters.Add(productName, words[SpreadInfoField.SecondDateDelimiter].Trim());
                        }
                    }
                    reader.Close();
                }
            }
            else
            {
                log.NewEntry(LogLevel.Warning, "The file path of {0} does not exist.", fullPath);
                isSuccessful = false;
            }

            return isSuccessful;
        }

        /// <summary>
        /// Determine whether a product exists in the spread information file.
        /// </summary>
        /// <param name="productName"></param>
        /// <returns></returns>
        public bool TryDetectProductName(string productName)
        {
            return m_ProductNameList.Contains(productName);
        }

        /// <summary>
        /// Get the first date delimiter for the product.
        /// </summary>
        /// <param name="productName"></param>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public bool TryGetFirstDateDelimiter(string productName, out string delimiter)
        {
            bool isSuccess = false;
            delimiter = null;
            if (m_ProductNameList.Contains(productName) && m_FirstDateDelimiters.ContainsKey(productName))
            {
                delimiter = m_FirstDateDelimiters[productName];
                isSuccess = true;
            }
            return isSuccess;
        }

        /// <summary>
        /// Get the second date delimiter for the product.
        /// </summary>
        /// <param name="productName"></param>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public bool TryGetSecondDateDelimiter(string productName, out string delimiter)
        {
            bool isSuccess = false;
            delimiter = null;
            if (m_ProductNameList.Contains(productName) && m_SecondDateDelimiters.ContainsKey(productName))
            {
                delimiter = m_SecondDateDelimiters[productName];
                isSuccess = true;
            }
            return isSuccess;
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

    /// <summary>
    /// This is a static class to record spread information field.
    /// </summary>
    public static class SpreadInfoField
    {
        public static int ProductName = 0;
        public static int FirstDateDelimiter = 1;
        public static int SecondDateDelimiter = 2;

        // The final member shows the number of the field contained.
        public static int ColumnNumber = 3;
    }
}
