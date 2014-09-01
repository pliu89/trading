using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace AmbreMaintenance
{
    using Ambre.TTServices.Fills;
    using Ambre.TTServices;

    using AuditTrailReading;

    using Misty.Lib.Hubs;
    using Misty.Lib.OrderHubs;
    using Misty.Lib.Products;

    using InstrumentKey = TradingTechnologies.TTAPI.InstrumentKey;

    public class AuditTrailPlayer
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        //
        // Basic members in this class.
        private string m_AuditTrailPath = null;
        private string m_UserName = null;
        private string m_FillHubName = null;
        private List<FillEventArgs> m_FillEventArgsList = null;
        public List<InstrumentKey> m_NeededBookInstrumentList = null;
        private LogHub Log = null;

        // Necessary constant strings that are used to filter audit trail row.
        private const string m_LocalTimeZoneString = "Central Standard Time";
        private const string m_OrderStatus_OK = "OK";
        private const string m_Action_Fill = "Fill";
        private const string m_Action_PartialFill = "Partial Fill";
        private const string m_OrderSide_Buy = "B";
        private const string m_OrderSide_Sell = "S";
        private const string m_Product_AutoSpreader = "Autospreader";
        private const string m_ExchangeName_AlgoSE = "AlgoSE";
        private string[] m_Contract_Special = new string[] { "Calendar", "Spread", "Butterfly", "/" };
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        //   
        /// <summary>
        /// Constructor to create audit trail player input user name, fill hub name and audit trail path.
        /// </summary>
        /// <param name="auditTrailPath"></param>
        /// <param name="userName"></param>
        /// <param name="fillHubName"></param>
        /// <param name="log"></param>
        public AuditTrailPlayer(string auditTrailPath, string userName, string fillHubName, LogHub log)
        {
            m_AuditTrailPath = auditTrailPath;
            m_UserName = userName;
            m_FillHubName = fillHubName;
            m_FillEventArgsList = new List<FillEventArgs>();
            m_NeededBookInstrumentList = new List<InstrumentKey>();
            Log = log;
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
        /// This function reads all lines in the audit trail file, generating fill event args and store them.
        /// In addition to that, it also collect the last most recent fill information and update the position for the next step.
        /// </summary>
        /// <param name="startDateTime"></param>
        /// <param name="endDateTime"></param>
        /// <returns></returns>
        public bool TryReadAuditTrailFills(DateTime startDateTime, DateTime endDateTime, AuditTrailFillHub auditTrailFillHub)
        {
            // This method will load the audit trail fills from TT given start and end date time automatically.
            // The results will be added to the audit trail fills dictionary.
            // It will also construct the fill hub name by user dictionary.

            // Get the target audit trail files to read. The file name format is AuditLog_yyyy-mm-dd_N.
            DateTime startDate;
            DateTime endDate;
            startDate = startDateTime.Subtract(startDateTime.TimeOfDay);
            while (startDate.DayOfWeek == DayOfWeek.Saturday || startDate.DayOfWeek == DayOfWeek.Sunday)
                startDate = startDate.AddDays(-1);
            endDate = endDateTime.Subtract(endDateTime.TimeOfDay).AddDays(1);
            while (endDate.DayOfWeek == DayOfWeek.Saturday || endDate.DayOfWeek == DayOfWeek.Sunday)
                endDate = endDate.AddDays(1);
            if (!System.IO.Directory.Exists(m_AuditTrailPath))
                System.IO.Directory.CreateDirectory(m_AuditTrailPath);
            string pattern = "AuditLog*.mdb";
            List<string> fileList = new List<string>();
            List<string> targetFileList = new List<string>();
            List<string> targetTableList = new List<string>();
            List<DateTime> targetDateList = new List<DateTime>();
            fileList.AddRange(System.IO.Directory.GetFiles(m_AuditTrailPath, pattern));
            int dateDelimiterStart;
            int dateDelimiterEnd;
            DateTime fileDate = DateTime.MinValue;
            string fileDateString;

            // Loop through the file collection and choose the desired files.
            foreach (string fileName in fileList)
            {
                dateDelimiterEnd = fileName.LastIndexOf("_");
                dateDelimiterStart = fileName.LastIndexOf("_", dateDelimiterEnd - 1);

                if (dateDelimiterStart < fileName.Length && dateDelimiterEnd < fileName.Length)
                {
                    fileDateString = fileName.Substring(dateDelimiterStart + 1, dateDelimiterEnd - dateDelimiterStart - 1);
                    if (DateTime.TryParseExact(fileDateString, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out fileDate))
                    {
                        if (startDate <= fileDate && fileDate <= endDate)
                        {
                            Log.NewEntry(LogLevel.Minor, "Include the file name:{0}.", fileName);
                            targetFileList.Add(fileName);
                            targetTableList.Add(fileDate.ToString("MMMdd"));
                            targetDateList.Add(fileDate);
                        }
                        else
                        {
                            Log.NewEntry(LogLevel.Major, "Exclude the file name:{0}.", fileName);
                            continue;
                        }
                    }
                    else
                    {
                        Log.NewEntry(LogLevel.Major, "There is problem in parsing file date string:{0}.", fileDateString);
                        continue;
                    }
                }
                else
                {
                    Log.NewEntry(LogLevel.Major, "There is problem in parsing file name:{0}.", fileName);
                    continue;
                }
            }

            // Build the connections and load data from the file collection.
            m_FillEventArgsList.Clear();
            bool isBadRow;
            List<InstrumentKey> instrumentKeyList = new List<InstrumentKey>();
            bool readAllFillAccounts = false;
            if (auditTrailFillHub.HubName == string.Empty)
                readAllFillAccounts = true;
            for (int fileIndex = 0; fileIndex < targetFileList.Count; ++fileIndex)
            {
                // Read from the audit file.
                List<List<object>> dataCollections = new List<List<object>>();
                if (AccessReader.TryReadAccessFile(targetFileList[fileIndex], targetTableList[fileIndex], out dataCollections))
                {
                    // Get the information from the output. All the values are in object type.
                    int rowNumber = dataCollections.Count;
                    for (int rowIndex = 0; rowIndex < rowNumber; ++rowIndex)
                    {
                        isBadRow = false;

                        // The valid data types are explored in detail by test program.
                        string localTimeStamp = (string)dataCollections[rowIndex][ObjectListSchema.LocalTimeStamp];
                        string exchangeName = (string)dataCollections[rowIndex][ObjectListSchema.ExchangeName];
                        string orderStatus = (string)dataCollections[rowIndex][ObjectListSchema.OrderStatus];
                        string orderAction = (string)dataCollections[rowIndex][ObjectListSchema.OrderAction];
                        string orderSide = (string)dataCollections[rowIndex][ObjectListSchema.OrderSide];
                        int orderQty = (int)dataCollections[rowIndex][ObjectListSchema.OrderQty];
                        string product = (string)dataCollections[rowIndex][ObjectListSchema.Product];
                        string contract = (string)dataCollections[rowIndex][ObjectListSchema.Contract];
                        string orderPrice = (string)dataCollections[rowIndex][ObjectListSchema.OrderPrice];
                        string accountName = (string)dataCollections[rowIndex][ObjectListSchema.AccountName];
                        string userName = (string)dataCollections[rowIndex][ObjectListSchema.UserName];
                        string exchangeTime = (string)dataCollections[rowIndex][ObjectListSchema.ExchangeTime];
                        string exchangeDate = (string)dataCollections[rowIndex][ObjectListSchema.ExchangeDate];
                        string tradeSource = (string)dataCollections[rowIndex][ObjectListSchema.TradeSource];
                        string ttOrderKey = (string)dataCollections[rowIndex][ObjectListSchema.TTOrderKey];
                        string ttSeriesKey = (string)dataCollections[rowIndex][ObjectListSchema.TTSeriesKey];

                        // Check whether the account is desired.
                        if (!readAllFillAccounts && !accountName.Equals(m_FillHubName))
                            continue;

                        // Check whether the exchange name includes AlgoSE sub string.
                        if (exchangeName.Contains(m_ExchangeName_AlgoSE))
                            continue;

                        // Check whether it is a fill event.
                        if (!orderAction.Equals(m_Action_Fill) && !orderAction.Equals(m_Action_PartialFill))
                            continue;

                        // Check whether it is a OK status.
                        if (!orderStatus.Equals(m_OrderStatus_OK))
                            continue;

                        // Check whether the product type is future.
                        if (product.Equals(m_Product_AutoSpreader))
                            continue;

                        // Check whether the contract string contains Calendar string to avoid duplicate fills.
                        foreach (string specialString in m_Contract_Special)
                        {
                            if (contract.Contains(specialString))
                            {
                                isBadRow = true;
                                if (exchangeName.Contains("TOCOM") && specialString.Equals("/") && isBadRow)
                                    isBadRow = false;
                                break;
                            }
                        }
                        if (isBadRow)
                            continue;

                        // Try parse some necessary variables.
                        DateTime localDateTimeValid = DateTime.MinValue;
                        DateTime exchangeTimeValid = DateTime.MinValue;
                        DateTime exchangeDateValid = DateTime.MinValue;
                        double fillPrice = double.NaN;
                        if (!DateTime.TryParseExact(localTimeStamp, "HH:mm:ss.fff", null, System.Globalization.DateTimeStyles.None, out localDateTimeValid))
                        {
                            Log.NewEntry(LogLevel.Major, "Failed to parse the utc time of {0}.", localTimeStamp);
                            continue;
                        }
                        if (!DateTime.TryParseExact(exchangeTime, "HH:mm:ss.fff", null, System.Globalization.DateTimeStyles.None, out exchangeTimeValid))
                        {
                            Log.NewEntry(LogLevel.Major, "Failed to parse the exchange time of {0}.", exchangeTime);
                            continue;
                        }
                        if (!DateTime.TryParseExact(exchangeDate, "ddMMMyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out exchangeDateValid))
                        {
                            Log.NewEntry(LogLevel.Major, "Failed to parse the exchange time of {0}.", exchangeDate);
                            continue;
                        }
                        if (!double.TryParse(orderPrice, out fillPrice))
                        {
                            Log.NewEntry(LogLevel.Major, "Failed to parse the order price of {0}.", orderPrice);
                            continue;
                        }
                        localDateTimeValid = targetDateList[fileIndex].Add(localDateTimeValid.TimeOfDay);
                        exchangeTimeValid = exchangeDateValid.Add(exchangeTimeValid.TimeOfDay);

                        // Edit the market.
                        int validEndIndex = exchangeName.LastIndexOf("-");
                        if (validEndIndex >= 0)
                            exchangeName = exchangeName.Substring(0, validEndIndex);

                        // Create fill.
                        Fill fill = new Fill();
                        fill.ExchangeTime = exchangeTimeValid;
                        //TimeZoneInfo localTimeZone = TimeZoneInfo.FindSystemTimeZoneById(m_LocalTimeZoneString);
                        //DateTime localDateTime = TimeZoneInfo.ConvertTimeFromUtc(localDateTimeValid, localTimeZone);
                        fill.LocalTime = localDateTimeValid;
                        fill.Price = fillPrice;
                        fill.Qty = orderSide.Equals(m_OrderSide_Buy) ? orderQty : (orderSide.Equals(m_OrderSide_Sell) ? -orderQty : 0);

                        // Create fill event args.
                        InstrumentKey instrumentKey = new InstrumentKey(exchangeName, TradingTechnologies.TTAPI.ProductType.Future, product, ttSeriesKey);
                        FillEventArgs fillEventArgs = new FillEventArgs();
                        fillEventArgs.Fill = fill;
                        fillEventArgs.AccountID = accountName;
                        //fillEventArgs.FillKey = ttOrderKey;
                        fillEventArgs.Type = FillType.Historic;
                        fillEventArgs.TTInstrumentKey = instrumentKey;

                        // Add the instrument key to the list.
                        if (startDateTime.AddSeconds(-3) <= localDateTimeValid && localDateTimeValid <= endDateTime.AddSeconds(3))
                        {
                            if (!TTConvert.CheckExistenceOfInstrumentKey(instrumentKeyList, instrumentKey))
                            {
                                instrumentKeyList.Add(instrumentKey);
                            }

                            // Add the fill event args to the list.
                            string rowInfo = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15}.", localTimeStamp, exchangeName, orderStatus, orderAction, orderSide, orderQty, product, contract, orderPrice, accountName, userName, exchangeTime, exchangeDate, tradeSource, ttOrderKey, ttSeriesKey);
                            Log.NewEntry(LogLevel.Minor, "Record:{0}", rowInfo);
                            m_FillEventArgsList.Add(fillEventArgs);
                        }
                    }
                }
                else
                {
                    Log.NewEntry(LogLevel.Major, "Failed to read access file with name:{0}.", targetFileList[fileIndex]);
                    continue;
                }

                Log.NewEntry(LogLevel.Minor, "Complete reading access file with name:{0}.", targetFileList[fileIndex]);
            }

            // Get the last local time of fill for each instrument in initial fill hub.
            m_NeededBookInstrumentList.Clear();
            //foreach (InstrumentKey ttKeyInWaitList in auditTrailFillHub.InitialWaitDictionary.Keys)
            //{
            //    // These fills that do not have books will be requested to create new books if possible. To ensure they have books, create them if necessary here.
            //    if (!auditTrailFillHub.TryCheckExistenceOfInstrumentKey(ttKeyInWaitList) && !TTConvert.CheckExistenceOfInstrumentKey(neededBookInstrumentList, ttKeyInWaitList))
            //    {
            //        neededBookInstrumentList.Add(ttKeyInWaitList);
            //    }
            //}

            // For the fills from the audit trail files, also get instruments that need books.
            foreach (InstrumentKey instrumentKeyInAuditTrail in instrumentKeyList)
            {
                if (!auditTrailFillHub.TryCheckExistenceOfInstrumentKey(instrumentKeyInAuditTrail) && !TTConvert.CheckExistenceOfInstrumentKey(m_NeededBookInstrumentList, instrumentKeyInAuditTrail))
                    m_NeededBookInstrumentList.Add(instrumentKeyInAuditTrail);
            }

            // Clear all trades in the waiting list because they are all included in the audit trail files.
            auditTrailFillHub.InitialWaitDictionary.Clear();

            // Create books if necessary.
            // The instrument that need book creation is those got from FillEventArgs from drop file and the new fills listened in the audit trail file.
            // It is crucial to get all the books ready before updating positions.
            foreach (InstrumentKey key in m_NeededBookInstrumentList)
            {
                OrderHubRequest creatFillBookRequest = new OrderHubRequest(OrderHubRequest.RequestType.RequestCreateFillBook);
                creatFillBookRequest.Data = new object[1];
                creatFillBookRequest.Data[0] = key;
                auditTrailFillHub.HubEventEnqueue(creatFillBookRequest);
            }

            Log.NewEntry(LogLevel.Minor, "Complete loading audit trail fills.");
            return true;
        }

        /// <summary>
        /// This function plays all the effective fill event args that are in specified date time range by getting initial fill hub and outputting final fill hub.
        /// </summary>
        /// <param name="initialFillHub"></param>
        /// <param name="startDateTime"></param>
        /// <param name="endDateTime"></param>
        /// <param name="workingFillHub"></param>
        /// <returns></returns>
        public bool TryPlayAuditTrailFillsForFillHub(AuditTrailFillHub initialFillHub, DateTime startDateTime, DateTime endDateTime, out AuditTrailFillHub workingFillHub)
        {
            // This method will load fill hub and add audit trail fills to each instrument in it.
            Dictionary<InstrumentKey, DateTime> instrumentLastExchangeDateTimeDictionary = new Dictionary<InstrumentKey, DateTime>();
            DateTime minDateTime = new DateTime(2000, 1, 1);

            workingFillHub = null;
            DateTime thisFillLocalDateTime;
            DateTime thisFillExchangeDateTime;
            DateTime lastExchangeFillDateTime;

            //foreach (InstrumentKey ttKeyInWaitList in initialFillHub.InitialWaitDictionary.Keys)
            //{
            //    // The fill event args dictionary contains exchange date time as keys.
            //    SortedList<DateTime, FillEventArgs> fillEventArgsDictionary = initialFillHub.InitialWaitDictionary[ttKeyInWaitList];
            //    DateTime exchangeLastDateTime = new DateTime(2000, 1, 1);
            //    foreach (DateTime exchangeDateTime in fillEventArgsDictionary.Keys)
            //    {
            //        DateTime thisLocalDateTime = fillEventArgsDictionary[exchangeDateTime].Fill.LocalTime;
            //        DateTime thisExchangeDateTime = fillEventArgsDictionary[exchangeDateTime].Fill.ExchangeTime;
            //        if (thisLocalDateTime <= endDateTime)
            //        {
            //            initialFillHub.m_Listener.OnFilled(fillEventArgsDictionary[exchangeDateTime]);
            //            exchangeLastDateTime = thisExchangeDateTime;
            //        }
            //    }

            //    // Add entries to the local date time dictionary.
            //    if (exchangeLastDateTime > minDateTime)
            //    {
            //        if (!instrumentLastExchangeDateTimeDictionary.ContainsKey(ttKeyInWaitList))
            //            instrumentLastExchangeDateTimeDictionary.Add(ttKeyInWaitList, exchangeLastDateTime);
            //    }

            //    //// These fills that do not have books will be requested to create new books if possible. To ensure they have books, create them if necessary here.
            //    //if (!auditTrailFillHub.TryCheckExistenceOfInstrumentKey(ttKeyInWaitList) && !TTConvert.CheckExistenceOfInstrumentKey(m_NeededBookInstrumentList, ttKeyInWaitList))
            //    //{
            //    //    m_NeededBookInstrumentList.Add(ttKeyInWaitList);
            //    //}
            //}

            // Loop through the books to get last fill local date time if there is no fill event in the waiting list.
            List<InstrumentName> instrumentNameList = new List<InstrumentName>();
            //List<InstrumentKey> instrumentListWithBooks = new List<InstrumentKey>();
            initialFillHub.GetInstrumentNames(ref instrumentNameList);
            foreach (InstrumentName instrumentName in instrumentNameList)
            {
                IFillBook book;
                InstrumentKey ttInstrumentKey;
                if (initialFillHub.TryEnterReadBook(instrumentName, out book) && book is BookLifo && initialFillHub.TryGetInstrumentKey(instrumentName, out ttInstrumentKey))
                {
                    //if (!TTConvert.CheckExistenceOfInstrumentKey(instrumentListWithBooks, ttInstrumentKey))
                    //    instrumentListWithBooks.Add(ttInstrumentKey);
                    if (!instrumentLastExchangeDateTimeDictionary.ContainsKey(ttInstrumentKey))
                        instrumentLastExchangeDateTimeDictionary.Add(ttInstrumentKey, ((BookLifo)book).ExchangeTimeLast);
                    //else
                    //{
                    //    if (((BookLifo)book).ExchangeTimeLast > instrumentLastExchangeDateTimeDictionary[ttInstrumentKey])
                    //    {
                    //        Log.NewEntry(LogLevel.Major, "The recorded exchange time in book:{0} is later than recorded new fill exchange time:{1}.", ((BookLifo)book).ExchangeTimeLast, instrumentLastExchangeDateTimeDictionary[ttInstrumentKey]);
                    //        instrumentLastExchangeDateTimeDictionary[ttInstrumentKey] = ((BookLifo)book).ExchangeTimeLast;
                    //    }
                    //}
                    initialFillHub.ExitReadBook(instrumentName);
                }
            }

            // Sort event args into dictionary by instruments.
            Dictionary<InstrumentKey, SortedList<DateTime, FillEventArgs>> m_FillEventArgsByInstrumentKey = new Dictionary<InstrumentKey, SortedList<DateTime, FillEventArgs>>();
            foreach (FillEventArgs e in m_FillEventArgsList)
            {
                if (!m_FillEventArgsByInstrumentKey.ContainsKey(e.TTInstrumentKey))
                {
                    m_FillEventArgsByInstrumentKey.Add(e.TTInstrumentKey, new SortedList<DateTime, FillEventArgs>());
                }
                while (m_FillEventArgsByInstrumentKey[e.TTInstrumentKey].ContainsKey(e.Fill.ExchangeTime))
                    e.Fill.ExchangeTime = e.Fill.ExchangeTime.AddTicks(1L);
                m_FillEventArgsByInstrumentKey[e.TTInstrumentKey].Add(e.Fill.ExchangeTime, e);
            }

            // Play the fill event args in the program.
            foreach (InstrumentKey ttKey in m_FillEventArgsByInstrumentKey.Keys)
            {
                if (instrumentLastExchangeDateTimeDictionary.ContainsKey(ttKey))
                {
                    lastExchangeFillDateTime = instrumentLastExchangeDateTimeDictionary[ttKey];
                }
                else
                {
                    // Sometimes, there are other instruments that we do not the know the last local date time. This may due to that there are no books in the drop file.
                    // This is a new instrument and we should accept all the fills.
                    lastExchangeFillDateTime = startDateTime.AddMilliseconds(-1);
                }

                foreach (DateTime sortedDateTime in m_FillEventArgsByInstrumentKey[ttKey].Keys )
                {
                    thisFillLocalDateTime = m_FillEventArgsByInstrumentKey[ttKey][sortedDateTime].Fill.LocalTime;
                    thisFillExchangeDateTime = sortedDateTime;

                    if (thisFillExchangeDateTime > lastExchangeFillDateTime && thisFillLocalDateTime <= endDateTime)
                        initialFillHub.m_Listener.OnFilled(m_FillEventArgsByInstrumentKey[ttKey][sortedDateTime]);
                }
            }

            //// Play the fill event args in the program.
            //foreach (FillEventArgs fillEventArg in m_FillEventArgsList)
            //{
            //    thisFillLocalDateTime = fillEventArg.Fill.LocalTime;
            //    thisFillExchangeDateTime = fillEventArg.Fill.ExchangeTime;
            //    InstrumentKey ttKeyInAuditTrail = fillEventArg.TTInstrumentKey;
            //    if (instrumentLastExchangeDateTimeDictionary.ContainsKey(ttKeyInAuditTrail))
            //    {
            //        lastExchangeFillDateTime = instrumentLastExchangeDateTimeDictionary[ttKeyInAuditTrail];
            //    }
            //    else
            //    {
            //        // Sometimes, there are other instruments that we do not the know the last local date time. This may due to that there are no books in the drop file.
            //        // This is a new instrument and we should accept all the fills.
            //        lastExchangeFillDateTime = startDateTime.AddMilliseconds(-1);
            //    }

            //    if (thisFillExchangeDateTime > lastExchangeFillDateTime && thisFillLocalDateTime <= endDateTime)
            //        initialFillHub.m_Listener.OnFilled(fillEventArg);
            //}



            workingFillHub = initialFillHub;
            return true;
        }
        #endregion//Public Methods


        #region no Private Methods
        
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
