using System;
using System.IO;
using System.Text;

namespace VioletAPI.Lib.DataValidator
{
    using UV.Lib.Hubs;

    public class MarketDataStreamWriter
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //    
        //
        // Members to interact with csv file.
        private StreamWriter m_DataStreamWriter = null;
        private bool m_IsWrittenFlag = false;
        private StringBuilder m_StringBuilder = null;
        private string m_DataStreamWriterDirectoryPath;
        private string m_DataStreamWriterFileName;
        private string m_DataStreamWriterFileFullPath;
        private const string m_MarketDataTestDirectory = "MarketDataTest";

        // Loghub.
        private LogHub Log = null;
        private bool m_IsLogViewed = true;
        private const string m_LogName = "MarketData";
        #endregion


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        //   
        /// <summary>
        /// Constructor to write data to csv file for validation.
        /// </summary>
        public MarketDataStreamWriter()
        {
            // Initialize the working directory/path for the market data stream writer.
            m_DataStreamWriterDirectoryPath = string.Format("{0}{1}", UV.Lib.Application.AppInfo.GetInstance().LogPath, m_MarketDataTestDirectory);

            // Create the directory/file if there is no existing one for market record.
            if (!Directory.Exists(m_DataStreamWriterDirectoryPath))
                Directory.CreateDirectory(m_DataStreamWriterDirectoryPath);

            // Also initialize a log hub.
            Log = new LogHub(m_LogName, UV.Lib.Application.AppInfo.GetInstance().LogDirectory, m_IsLogViewed, LogLevel.Minor);

            m_StringBuilder = new StringBuilder();
        }
        #endregion//Constructors


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //
        //
        //
        /// <summary>
        /// Open a stream writer to connect to a csv file.
        /// </summary>
        public void OpenFileStreamConnection()
        {
            try
            {
                // The file type written to is a csv file.
                m_DataStreamWriterFileName = string.Format("{0}_MarketRecord.csv", DateTime.Now.ToString("yyyyMMddHHmmss"));
                m_DataStreamWriterFileFullPath = string.Format("{0}\\{1}", m_DataStreamWriterDirectoryPath, m_DataStreamWriterFileName);
                Log.NewEntry(LogLevel.Major, "Try to open a stream writer in:{0}.", m_DataStreamWriterFileFullPath);
                m_DataStreamWriter = new StreamWriter(m_DataStreamWriterFileFullPath, false);
                m_IsWrittenFlag = true;
                m_DataStreamWriter.WriteLine(MarketDataSchema.WriteSchema());
                Log.NewEntry(LogLevel.Major, "Successfully created a stream writer in:{0}.", m_DataStreamWriterFileFullPath);
            }
            catch (Exception ex)
            {
                Log.NewEntry(LogLevel.Error, "Failed to create stream writer with full path:{0}.", m_DataStreamWriterFileFullPath);
                Log.NewEntry(LogLevel.Error, "The detailed information for the error is {0}.", ex);
                m_DataStreamWriter = null;
            }
        }

        /// <summary>
        /// This method appends data to the csv file.
        /// </summary>
        /// <param name="objectList"></param>
        /// <returns></returns>
        public bool AppendDataPointToCSV(object[] objectList)
        {
            if (objectList.Length <= MarketDataSchema.Count)
            {
                if (m_IsWrittenFlag)
                {
                    m_StringBuilder.Clear();
                    for (int dataIndex = 0; dataIndex < objectList.Length - 1; ++dataIndex)
                    {
                        m_StringBuilder.AppendFormat("{0},", objectList[dataIndex]);
                    }
                    m_StringBuilder.Append(objectList[objectList.Length - 1]);
                    m_DataStreamWriter.WriteLine(m_StringBuilder.ToString());
                    return true;
                }
                else
                    return false;
            }
            else
            {
                Log.NewEntry(LogLevel.Error, "Failed to append data point:{0}, because the length is not sufficient", objectList);
                return false;
            }
        }

        /// <summary>
        /// Close the stream writer to that csv file.
        /// </summary>
        public void CloseFileSreamConnection()
        {
            if (m_DataStreamWriter == null)
            {
                Log.NewEntry(LogLevel.Error, "The file stream writer does not exist, no close is needed.");
            }
            else
            {
                try
                {
                    Log.NewEntry(LogLevel.Major, "Try to close a stream writer in:{0}.", m_DataStreamWriterFileFullPath);
                    m_DataStreamWriter.Flush();
                    m_DataStreamWriter.Close();
                    Log.NewEntry(LogLevel.Major, "Successfully closed a stream writer in:{0}.", m_DataStreamWriterFileFullPath);
                }
                catch (Exception ex)
                {
                    Log.NewEntry(LogLevel.Error, "Failed to close the stream writer with full path:{0}.", m_DataStreamWriterFileFullPath);
                    Log.NewEntry(LogLevel.Error, "The detailed information for the error is {0}.", ex);
                }
                finally
                {
                    m_IsWrittenFlag = false;
                }
            }
        }
        #endregion//Public Methods

    }
}
