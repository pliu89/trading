using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EconomicBloombergProject
{
    public class TickerList
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        // List of tickers that are going to be listened.
        private List<string> m_Tickers = null;
        private List<string> m_NewTickers = null;
        private List<string> m_UnixTickers = null;
        private List<List<string>> m_TickerChunkList = null;
        private List<List<string>> m_NewTickerChunkList = null;

        // Limit for the maximum tickers that we are going to listen to.
        private int m_TickerLimit = 20;

        // Database reader.
        private DataBaseReader m_DataBaseReader = null;
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        //    
        /// <summary>
        /// This function adds the tickers into the list that we want to listen.
        /// </summary>
        public TickerList(DataBaseReader dataBaseReader, string countryCode)
        {
            // Create lists for tickers.
            m_Tickers = new List<string>();
            m_NewTickers = new List<string>();
            m_UnixTickers = new List<string>();
            m_TickerChunkList = new List<List<string>>();
            m_NewTickerChunkList = new List<List<string>>();

            // Get database reader member.
            m_DataBaseReader = dataBaseReader;
        }
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        // Use properties to limit the number of the tickers that we are going to listen.
        public List<string> Tickers
        {
            get
            {
                if (m_Tickers.Count >= m_TickerLimit)
                {
                    return m_Tickers.Take(m_TickerLimit).ToList();
                }
                else
                {
                    return m_Tickers;
                }
            }
        }
        public List<string> NewTickers
        {
            get
            {
                if (m_NewTickers.Count >= m_TickerLimit)
                {
                    return m_NewTickers.Take(m_TickerLimit).ToList();
                }
                else
                {
                    return m_NewTickers;
                }
            }
        }
        public List<string> WrongTickers
        {
            get { return m_UnixTickers; }
        }
        public List<List<string>> TickerChunkList
        {
            get { return m_TickerChunkList; }
        }
        public List<List<string>> NewTickerChunkList
        {
            get { return m_NewTickerChunkList; }
        }
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
        /// Return all tickers from one country.
        /// </summary>
        public void SetTickersToChunks(string countryCode)
        {
            // Loop through ticker information table keys to find the ticker.
            foreach (int row in m_DataBaseReader.EconomicTickerInfo.Keys)
            {
                List<string> stringList;
                m_DataBaseReader.EconomicTickerInfo.TryGetValue(row, out stringList);

                // Add the tickers from one country to the corresponding list.
                if (stringList[TickerInfoField.CountryCode] == countryCode && stringList[TickerInfoField.IsGood] == TickerState.GoodTickerWithHistory)
                {
                    m_Tickers.Add(stringList[TickerInfoField.TickerName]);
                }
                if (stringList[TickerInfoField.CountryCode] == countryCode && stringList[TickerInfoField.IsGood] == TickerState.NewlyInsertedTickerWithoutHistory)
                {
                    m_NewTickers.Add(stringList[TickerInfoField.TickerName]);
                }
            }

            // Set tickers by chunk.
            m_TickerChunkList = GetChunkTickers(m_Tickers);
            m_NewTickerChunkList = GetChunkTickers(m_NewTickers);
        }

        /// <summary>
        /// Return all tickers from one country.
        /// </summary>
        public void SetTickersToChunks()
        {
            // Loop through ticker information table keys to find the ticker.
            foreach (int row in m_DataBaseReader.EconomicTickerInfo.Keys)
            {
                List<string> stringList;
                m_DataBaseReader.EconomicTickerInfo.TryGetValue(row, out stringList);

                // Add the tickers from all country to the corresponding list.
                if (stringList[TickerInfoField.IsGood] == TickerState.GoodTickerWithHistory)
                {
                    m_Tickers.Add(stringList[TickerInfoField.TickerName]);
                }
                if (stringList[TickerInfoField.IsGood] == TickerState.NewlyInsertedTickerWithoutHistory)
                {
                    m_NewTickers.Add(stringList[TickerInfoField.TickerName]);
                }
            }

            // Set tickers by chunk.
            m_TickerChunkList = GetChunkTickers(m_Tickers);
            m_NewTickerChunkList = GetChunkTickers(m_NewTickers);
        }

        /// <summary>
        /// Only pick one ticker for test.
        /// </summary>
        /// <param name="ticker"></param>
        /// <param name="isNewTicker"></param>
        public void TryOneTicker(string ticker, bool isNewTicker)
        {
            m_Tickers.Clear();
            m_NewTickers.Clear();
            if (isNewTicker)
                m_NewTickers.Add(ticker);
            else
                m_Tickers.Add(ticker);

            // Set tickers by chunk.
            m_TickerChunkList = GetChunkTickers(m_Tickers);
            m_NewTickerChunkList = GetChunkTickers(m_NewTickers);
        }

        /// <summary>
        /// Make a list of tickers that have unixtimestamp of 0.
        /// </summary>
        public void PickUpUnixTimeStampErrorTickers()
        {
            // Loop through line in the data base to find where the unix time stamp is equal to 0.
            foreach (long line in m_DataBaseReader.EconomicDataCollections.Keys)
            {
                string unixTimeStamp = m_DataBaseReader.EconomicDataCollections[line][EconomicDataField.UnixTime];
                if (unixTimeStamp == Convert.ToString(0))
                {
                    string tickerId = m_DataBaseReader.EconomicDataCollections[line][EconomicDataField.EconomicTickerID];
                    int chosenId = -1;

                    // Find the ticker id for that line with wrong unix time stamp.
                    foreach (int id in m_DataBaseReader.EconomicTickerInfo.Keys)
                    {
                        if (m_DataBaseReader.EconomicTickerInfo[id][TickerInfoField.TickerID] == tickerId)
                        {
                            chosenId = id;
                            break;
                        }
                    }

                    // Add the found ticker name to the unix ticker list.
                    if (chosenId != -1)
                    {
                        string errorTicker = m_DataBaseReader.EconomicTickerInfo[chosenId][TickerInfoField.TickerName];
                        if (!m_UnixTickers.Contains(errorTicker))
                            m_UnixTickers.Add(errorTicker);
                    }
                }
            }
        }
        #endregion//Public Methods


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// Separate the tickers to chunks.
        /// </summary>
        private List<List<string>> GetChunkTickers(List<string> tickers)
        {
            List<List<string>> chunkList = new List<List<string>>();

            // Separate the tickers into chunks.
            int tickerInEachChunk = 10;
            int tickerCount = tickers.Count;
            int i = 0;
            List<string> oneChunk = new List<string>();
            for (; i < tickerCount; ++i)
            {
                oneChunk.Add(tickers[i]);
                if ((i + 1) % tickerInEachChunk == 0)
                {
                    chunkList.Add(oneChunk);
                    oneChunk = new List<string>();
                }
            }
            if (oneChunk.Count > 0)
            {
                chunkList.Add(oneChunk);
            }

            return chunkList;
        }
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
