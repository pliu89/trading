using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BRE.Lib.Utilities
{
    using BRE.Lib.TermStructures;

    using UV.Lib.Hubs;
    using UV.Lib.Products;
    using BaseQTMath = UV.Lib.Utilities.QTMath;

    public class QTMath
    {

        #region no Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        //       
        /// <summary>
        /// Empty default constructor -- not used in a static class.
        /// </summary>
        public QTMath() { }
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
        /// This function will parse any of the MMMYY from a string to MY.MY...
        /// This is often used to generate the product expiry name for spreads.
        /// </summary>
        /// <param name="test"></param>
        /// <param name="monthCodeYears"></param>
        /// <returns></returns>
        public static bool TryParseMMMYYToMonthCode(string MonthYear, out string monthCodeYears)
        {
            bool isSuccess = false;
            monthCodeYears = null;
            StringBuilder monthCodeYearsBuilder = new StringBuilder();
            DateTime dt = DateTime.Now;
            string monthCode;
            List<string> monthYears = new List<string>();

            if (MonthYear.Length > 5)
            {   // something different was handed to us, lets try and extract the monthYear using regex
                string regExpSearch = @"[a-zA-Z]{3}[0-9]{2}";
                foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(MonthYear, regExpSearch))
                    monthYears.Add(match.Value);
            }

            for (int monthYearIndex = 0; monthYearIndex < monthYears.Count; ++monthYearIndex)
            {
                string monthYear = monthYears[monthYearIndex];
                // Determine Year length
                int startYearPtr = monthYear.Length - 1;
                while (startYearPtr >= 0 && Char.IsDigit(monthYear[startYearPtr]))
                    startYearPtr--;
                startYearPtr++;             // now startYearPtr points to the start of the numeric part.

                if (monthYear.Length == 5 && DateTime.TryParseExact(monthYear, "MMMyy", null, System.Globalization.DateTimeStyles.None, out dt))
                { // two digit year code must have been used, and we parsed it okay
                    isSuccess = true;
                    if (isSuccess && UV.Lib.Utilities.QTMath.TryGetMonthCode(dt, out monthCode))
                    { // try and get a month code from our datetime.
                        string year = dt.Year.ToString();
                        char lastYearDigit = year[year.Length - 1];
                        monthCodeYearsBuilder.Append(string.Format("{0}{1}", monthCode, lastYearDigit));
                    }
                }
                else if (monthYear.Length == 4 && DateTime.TryParseExact(monthYear, "MMMyy", null, System.Globalization.DateTimeStyles.None, out dt))
                { // singled digit year code..
                    isSuccess = true;
                    if (isSuccess && UV.Lib.Utilities.QTMath.TryGetMonthCode(dt, out monthCode))
                    { // try and get a month code from our datetime.
                        string year = dt.Year.ToString();
                        char lastYearDigit = year[year.Length - 1];
                        monthCodeYearsBuilder.Append(string.Format("{0}{1}", monthCode, lastYearDigit));
                    }
                }
                else if (monthYear.Length == 3 && startYearPtr == 1)
                {   // This seems to have form "H12"
                    isSuccess = true;
                    monthCodeYearsBuilder.Append(string.Format("{0}{1}", monthYear[0], monthYear[2]));
                }
                else if (monthYear.Length == 2 && startYearPtr == 1)
                {   // This already has form  "H2"
                    isSuccess = true;
                    monthCodeYearsBuilder.Append(monthYear);
                }

                if (isSuccess && monthYearIndex < monthYears.Count - 1)
                    monthCodeYearsBuilder.Append(".");
            }

            monthCodeYears = monthCodeYearsBuilder.ToString();
            return isSuccess;
        }
        //
        //
        //
        /// <summary>
        /// This function is the inverse of the above.
        /// It will convert U4 to Sep14 like this.
        /// </summary>
        /// <param name="monthCodeYear"></param>
        /// <param name="MMMYY"></param>
        /// <returns></returns>
        public static bool TryParseMonthCodeToMMMYY(string monthCodeYear, out string MMMYY)
        {
            bool isSuccess = false;
            MMMYY = null;
            if (monthCodeYear.Length == 2)
            {
                string monthCode = monthCodeYear.Substring(0, 1);
                string year = monthCodeYear.Substring(1, 1);
                int yearNumber;
                if (int.TryParse(year, out yearNumber))
                {
                    double years = (DateTime.Now.Year - 2000) / 10;
                    yearNumber += 10 * (int)Math.Floor(years);
                    int monthNumber = BaseQTMath.GetMonthNumberFromCode(monthCode);
                    string monthName;
                    if (BaseQTMath.TryGetMonthName(monthNumber, out monthName))
                    {
                        MMMYY = string.Format("{0}{1}", monthName, yearNumber);
                        isSuccess = true;
                    }
                }
            }
            return isSuccess;
        }
        //
        //
        //
        /// <summary>
        /// This function is used to composed the instrument name using instrument name from data base.
        /// It also needs a quote instrument because it needs the exchange information. This deficiency should be removed in the future in some way.
        /// This function is used when we read hedge options from the data base.
        /// </summary>
        /// <param name="instrumentDataBaseName"></param>
        /// <param name="quoteInstrument"></param>
        /// <param name="composedInstrumentName"></param>
        /// <returns></returns>
        public static bool TryComposeInstrumentName(string instrumentDataBaseName, InstrumentName quoteInstrument, LogHub log, out InstrumentName composedInstrumentName)
        {
            bool isSuccess = false;
            composedInstrumentName = quoteInstrument;
            char seperator = '.';
            string[] parts;
            string[] instrumentComponents = instrumentDataBaseName.Split(new char[] { seperator }, StringSplitOptions.RemoveEmptyEntries);
            int instrumentCount = instrumentComponents.Length;
            StringBuilder seriesNameBuilder = new StringBuilder();
            bool productFirstAdded = false;

            // Construct the instrument name.
            switch (instrumentCount)
            {
                case 1:
                    // Future:
                    string futureContract = instrumentComponents[0];
                    seperator = 'x';
                    parts = futureContract.Split(new char[] { seperator }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        string futureName = parts[1];
                        seperator = '_';
                        parts = futureName.Split(new char[] { seperator }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 2)
                        {
                            string productName = parts[0];
                            string monthCodeYear = parts[1];
                            string monthYear;
                            if (QTMath.TryParseMonthCodeToMMMYY(monthCodeYear, out monthYear))
                            {
                                if (!productFirstAdded)
                                {
                                    seriesNameBuilder.Append(monthYear);
                                    productFirstAdded = true;
                                }
                            }
                            else
                            {
                                log.NewEntry(LogLevel.Error, "month year code failed for string {0}.", monthCodeYear);
                                return isSuccess;
                            }
                        }
                        else
                        {
                            log.NewEntry(LogLevel.Error, "instrument parsed failed for future name {0}.", futureName);
                            return isSuccess;
                        }
                    }
                    else
                    {
                        log.NewEntry(LogLevel.Error, "instrument parsed failed for future contract {0}.", futureContract);
                        return isSuccess;
                    }
                    composedInstrumentName = new InstrumentName(new Product(quoteInstrument.Product.Exchange, quoteInstrument.Product.ProductName, ProductTypes.Future),
                        seriesNameBuilder.ToString());
                    isSuccess = true;
                    break;
                case 2:
                    // Calendar:
                    seriesNameBuilder.Append("Calendar: ");
                    foreach (string instrument in instrumentComponents)
                    {
                        seperator = 'x';
                        parts = instrument.Split(new char[] { seperator }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 2)
                        {
                            string weightString = parts[0];
                            string calendarName = parts[1];
                            seperator = '_';
                            parts = calendarName.Split(new char[] { seperator }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length == 2)
                            {
                                string productName = parts[0];
                                string monthCodeYear = parts[1];
                                string monthYear;
                                if (QTMath.TryParseMonthCodeToMMMYY(monthCodeYear, out monthYear))
                                {
                                    if (!productFirstAdded)
                                    {
                                        seriesNameBuilder.AppendFormat("{0}x{1} {2}", weightString, productName, monthYear);
                                        productFirstAdded = true;
                                    }
                                    else
                                        seriesNameBuilder.AppendFormat(":{0}x{1}", weightString, monthYear);
                                }
                                else
                                {
                                    log.NewEntry(LogLevel.Error, "month year code failed for string {0}.", monthCodeYear);
                                    return isSuccess;
                                }
                            }
                            else
                            {
                                log.NewEntry(LogLevel.Error, "instrument parsed failed for calendar name {0}.", calendarName);
                                return isSuccess;
                            }

                        }
                        else
                        {
                            log.NewEntry(LogLevel.Error, "instrument parsed failed for calendar contract {0}.", instrument);
                            return isSuccess;
                        }
                    }
                    composedInstrumentName = new InstrumentName(new Product(quoteInstrument.Product.Exchange, quoteInstrument.Product.ProductName, ProductTypes.Spread),
                        seriesNameBuilder.ToString());
                    isSuccess = true;
                    break;
                case 3:
                    // Butterfly:
                    seriesNameBuilder.Append("Butterfly: ");
                    foreach (string instrument in instrumentComponents)
                    {
                        seperator = 'x';
                        parts = instrument.Split(new char[] { seperator }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 2)
                        {
                            string weightString = parts[0];
                            string butterflyName = parts[1];
                            seperator = '_';
                            parts = butterflyName.Split(new char[] { seperator }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length == 2)
                            {
                                string productName = parts[0];
                                string monthCodeYear = parts[1];
                                string monthYear;
                                if (QTMath.TryParseMonthCodeToMMMYY(monthCodeYear, out monthYear))
                                {
                                    if (!productFirstAdded)
                                    {
                                        seriesNameBuilder.AppendFormat("{0}x{1} {2}", weightString, productName, monthYear);
                                        productFirstAdded = true;
                                    }
                                    else
                                    {
                                        int weightNumber;
                                        if (int.TryParse(weightString, out weightNumber))
                                        {
                                            if (weightNumber > 0)
                                                weightString = string.Format("+{0}", weightString);
                                            seriesNameBuilder.AppendFormat(":{0}x{1}", weightString, monthYear);
                                        }
                                        else
                                        {
                                            log.NewEntry(LogLevel.Error, "weight parsed failed for string {0}.", weightString);
                                            return isSuccess;
                                        }
                                    }
                                }
                                else
                                {
                                    log.NewEntry(LogLevel.Error, "month year code failed for string {0}.", monthCodeYear);
                                    return isSuccess;
                                }
                            }
                            else
                            {
                                log.NewEntry(LogLevel.Error, "instrument parsed failed for butterfly name {0}.", butterflyName);
                                return isSuccess;
                            }

                        }
                        else
                        {
                            log.NewEntry(LogLevel.Error, "instrument parsed failed for butterfly contract {0}.", instrument);
                            return isSuccess;
                        }
                    }
                    composedInstrumentName = new InstrumentName(new Product(quoteInstrument.Product.Exchange, quoteInstrument.Product.ProductName, ProductTypes.Spread),
                        seriesNameBuilder.ToString());
                    isSuccess = true;
                    break;
                case 4:
                    // Double Butterfly:
                    seriesNameBuilder.Append("Double Butterfly: ");
                    foreach (string instrument in instrumentComponents)
                    {
                        seperator = 'x';
                        parts = instrument.Split(new char[] { seperator }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 2)
                        {
                            string weightString = parts[0];
                            string doubleButterflyName = parts[1];
                            seperator = '_';
                            parts = doubleButterflyName.Split(new char[] { seperator }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length == 2)
                            {
                                string productName = parts[0];
                                string monthCodeYear = parts[1];
                                string monthYear;
                                if (QTMath.TryParseMonthCodeToMMMYY(monthCodeYear, out monthYear))
                                {
                                    if (!productFirstAdded)
                                    {
                                        seriesNameBuilder.AppendFormat("{0}x{1} {2}", weightString, productName, monthYear);
                                        productFirstAdded = true;
                                    }
                                    else
                                    {
                                        int weightNumber;
                                        if (int.TryParse(weightString, out weightNumber))
                                        {
                                            if (weightNumber > 0)
                                                weightString = string.Format("+{0}", weightString);
                                            seriesNameBuilder.AppendFormat(":{0}x{1}", weightString, monthYear);
                                        }
                                        else
                                        {
                                            log.NewEntry(LogLevel.Error, "weight parsed failed for string {0}.", weightString);
                                            return isSuccess;
                                        }
                                    }
                                }
                                else
                                {
                                    log.NewEntry(LogLevel.Error, "month year code failed for string {0}.", monthCodeYear);
                                    return isSuccess;
                                }
                            }
                            else
                            {
                                log.NewEntry(LogLevel.Error, "instrument parsed failed for double butterfly name {0}.", doubleButterflyName);
                                return isSuccess;
                            }

                        }
                        else
                        {
                            log.NewEntry(LogLevel.Error, "instrument parsed failed for double butterfly contract {0}.", instrument);
                            return isSuccess;
                        }
                    }
                    composedInstrumentName = new InstrumentName(new Product(quoteInstrument.Product.Exchange, quoteInstrument.Product.ProductName, ProductTypes.Spread),
                        seriesNameBuilder.ToString());
                    isSuccess = true;
                    break;
                default:
                    log.NewEntry(LogLevel.Error, "instrument component weird for {0}.", instrumentDataBaseName);
                    return isSuccess;
            }
            return isSuccess;
        }
        //
        //
        //
        /// <summary>
        /// Convert instrument name to instrument data base name.
        /// </summary>
        /// <param name="instrumentName"></param>
        /// <param name="instrumentNameDatabase"></param>
        /// <returns></returns>
        public static bool TryConvertInstrumentNameToInstrumentNameDatabase(InstrumentName instrumentName, LogHub log, out string instrumentNameDatabase)
        {
            bool isSuccess = false;
            instrumentNameDatabase = null;
            string productName = instrumentName.Product.ProductName;
            string seriesName = instrumentName.SeriesName;
            ProductTypes productTypes = instrumentName.Product.Type;
            string monthYearCode;
            switch (productTypes)
            {
                case ProductTypes.Future:

                    if (BaseQTMath.TryConvertMonthYearToCodeY(seriesName, out monthYearCode))
                    {
                        instrumentNameDatabase = string.Format("1x{0}_{1}", productName, monthYearCode);
                        isSuccess = true;
                    }
                    else
                    {
                        log.NewEntry(LogLevel.Error, "Parse month year code wrong for string {0}.", seriesName);
                        return isSuccess;
                    }
                    break;
                case ProductTypes.Spread:
                    // Target: Calendar: 1xGE Sep14:-1xDec14
                    List<string> matchedResults = new List<string>();
                    if (seriesName.Contains("Calendar") || seriesName.Contains("Butterfly") || seriesName.Contains("Double Butterfly"))
                    {
                        string pattern = @"-?\d+x[^:]+(?=(:?))";
                        MatchCollection matchCollection = Regex.Matches(seriesName, pattern);
                        foreach (Match match in matchCollection)
                        {
                            string matchedValue = match.Value;
                            matchedResults.Add(matchedValue);
                        }

                        StringBuilder instrumentDatabaseNameBuilder = new StringBuilder();
                        bool isFirst = true;
                        foreach (string matchedString in matchedResults)
                        {
                            char seperator = 'x';
                            string[] parts = matchedString.Split(new char[] { seperator }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length == 2)
                            {
                                string weightString = parts[0];
                                int weight;
                                if (int.TryParse(weightString, out weight))
                                {
                                    string monthYear = parts[1];
                                    pattern = @"(?!x)[a-zA-Z]{3}[0-9]{2}\z";
                                    matchCollection = Regex.Matches(monthYear, pattern);
                                    string matchedValue = null;
                                    foreach (Match match in matchCollection)
                                    {
                                        if (match.Success)
                                        {
                                            matchedValue = match.Value;
                                            break;
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(matchedValue))
                                    {
                                        if (BaseQTMath.TryConvertMonthYearToCodeY(matchedValue, out monthYearCode))
                                        {
                                            if (!isFirst)
                                                instrumentDatabaseNameBuilder.Append(".");
                                            else
                                                isFirst = false;
                                            instrumentDatabaseNameBuilder.AppendFormat("{0}x{1}_{2}", weight, productName, monthYearCode);
                                        }
                                        else
                                        {
                                            log.NewEntry(LogLevel.Error, "month year parsed wrong for {0}.", matchedValue);
                                            return isSuccess;
                                        }
                                    }
                                    else
                                    {
                                        log.NewEntry(LogLevel.Error, "month year matched wrong for {0}.", monthYear);
                                        return isSuccess;
                                    }
                                }
                                else
                                {
                                    log.NewEntry(LogLevel.Error, "weight parsed wrong for {0}.", weightString);
                                    return isSuccess;
                                }
                            }
                            else
                            {
                                log.NewEntry(LogLevel.Error, "string {0} does not contain weight and month year.", matchedString);
                                return isSuccess;
                            }
                        }
                        instrumentNameDatabase = instrumentDatabaseNameBuilder.ToString();
                        isSuccess = true;
                    }
                    else
                    {
                        log.NewEntry(LogLevel.Error, "Unhandled product type for string {0}.", seriesName);
                        return isSuccess;
                    }
                    break;
                default:
                    break;
            }
            return isSuccess;
        }
        //
        //
        //
        /// <summary>
        /// Convert instrument name to instrument data base name.
        /// </summary>
        /// <param name="instrumentName"></param>
        /// <param name="instrumentNameDatabase"></param>
        /// <returns></returns>
        public static bool TryConvertInstrumentNameToInstrumentNameDatabase(string instrumentFullName,LogHub log, out string instrumentNameDatabase)
        {
            bool isSuccess = false;
            instrumentNameDatabase = null;
            InstrumentName instrumentName;
            if (InstrumentName.TryDeserialize(instrumentFullName, out instrumentName))
            {
                if (TryConvertInstrumentNameToInstrumentNameDatabase(instrumentName,log, out instrumentNameDatabase))
                {
                    isSuccess = true;
                }
                else
                {
                    log.NewEntry(LogLevel.Error, "Failed to convert instrument to instrument database name for string {0}.", instrumentName);
                    return isSuccess;
                }
            }
            else
            {
                log.NewEntry(LogLevel.Error, "Failed to deserialize instrument for string {0}.", instrumentFullName);
                return isSuccess;
            }
            return isSuccess;
        }
        //
        //
        //
        /// <summary>
        /// This function will be responsible for generating the resulting instrument input from two instruments and their weights.
        /// </summary>
        /// <param name="quoteInstrument"></param>
        /// <param name="quoteRatio"></param>
        /// <param name="hedgeInstrument"></param>
        /// <param name="hedgeRatio"></param>
        /// <param name="resultingInstrument"></param>
        /// <returns></returns>
        public static bool TryComposeResultingInstrument(InstrumentName quoteInstrument, int quoteRatio,
            InstrumentName hedgeInstrument, int hedgeRatio, LogHub log, out ResultingInstrument resultingInstrument)
        {
            bool isSuccess = false;
            resultingInstrument = null;
            Dictionary<DateTime, int> quoteDateTimeToWeight;
            Dictionary<DateTime, int> hedgeDateTimeToWeight;
            Dictionary<DateTime, int> resultingDateTimeToWeight;
            if (!TryGetMonthYearWeightsFromInstrument(quoteInstrument, log, out quoteDateTimeToWeight))
            {
                log.NewEntry(LogLevel.Error, "Failed to find weight and expiry for instrument {0}.", quoteInstrument);
                return isSuccess;
            }
            if (!TryGetMonthYearWeightsFromInstrument(hedgeInstrument, log, out hedgeDateTimeToWeight))
            {
                log.NewEntry(LogLevel.Error, "Failed to find weight and expiry for instrument {0}.", hedgeInstrument);
                return isSuccess;
            }
            resultingDateTimeToWeight = new Dictionary<DateTime, int>();
            foreach (DateTime dt in quoteDateTimeToWeight.Keys)
            {
                int weight = quoteDateTimeToWeight[dt];
                if (resultingDateTimeToWeight.ContainsKey(dt))
                {
                    resultingDateTimeToWeight[dt] += quoteRatio * weight;
                }
                else
                {
                    resultingDateTimeToWeight.Add(dt, quoteRatio * weight);
                }
            }
            foreach (DateTime dt in hedgeDateTimeToWeight.Keys)
            {
                int weight = hedgeDateTimeToWeight[dt];
                if (resultingDateTimeToWeight.ContainsKey(dt))
                {
                    resultingDateTimeToWeight[dt] += hedgeRatio * weight;
                }
                else
                {
                    resultingDateTimeToWeight.Add(dt, hedgeRatio * weight);
                }
            }

            List<DateTime> dateTimesToAdd = new List<DateTime>(resultingDateTimeToWeight.Keys);
            dateTimesToAdd.Sort();
            int dateTimeAddingCount = dateTimesToAdd.Count;
            string resultingInstrumentPrefix = "";
            StringBuilder resultingExpirySeriesBuilder = new StringBuilder();
            InstrumentName resultingInstrumentName = new InstrumentName();
            switch (dateTimeAddingCount)
            {
                case 0:
                    log.NewEntry(LogLevel.Error, "Resulting instrument has no component for quote {0} and hedge {1}."
                        , quoteInstrument, hedgeInstrument);
                    return isSuccess;
                case 1:
                    // Future
                    DateTime futureDateTime = dateTimesToAdd[0];
                    resultingInstrumentName = new InstrumentName(quoteInstrument.Product, futureDateTime.ToString("MMMyy"));
                    break;
                case 2:
                    // Calendar spread
                    resultingInstrumentPrefix = "Calendar: ";
                    break;
                case 3:
                    // Butterfly
                    resultingInstrumentPrefix = "Butterfly: ";
                    break;
                case 4:
                    // Double butterfly
                    resultingInstrumentPrefix = "Double Butterfly: ";
                    break;
                default:
                    log.NewEntry(LogLevel.Error, "Resulting instrument component count is not handled in this case for quote {0} and hedge {1}."
                        , quoteInstrument, hedgeInstrument);
                    return isSuccess;
            }

            if (dateTimeAddingCount > 1)
            {
                resultingExpirySeriesBuilder.Append(resultingInstrumentPrefix);
                bool isFirstComponent = true;
                for (int dateTimeIndex = 0; dateTimeIndex < dateTimeAddingCount; ++dateTimeIndex)
                {
                    DateTime dateTime = dateTimesToAdd[dateTimeIndex];
                    int weight = resultingDateTimeToWeight[dateTime];
                    if (isFirstComponent)
                    {
                        isFirstComponent = false;
                    }
                    else
                    {
                        resultingExpirySeriesBuilder.Append(":");
                        if (weight > 0)
                            resultingExpirySeriesBuilder.Append("+");
                    }
                    resultingExpirySeriesBuilder.AppendFormat("{0}x{1}", weight, dateTime.ToString("MMMyy"));
                }
                resultingInstrumentName = new InstrumentName(quoteInstrument.Product, resultingExpirySeriesBuilder.ToString());
            }

            // Generate resulting instrument.
            string instrumentDatabaseName;
            if (TryConvertInstrumentNameToInstrumentNameDatabase(resultingInstrumentName, log, out instrumentDatabaseName))
            {
                resultingInstrument = new ResultingInstrument();
                resultingInstrument.ResultingInstrumentName = resultingInstrumentName;
                resultingInstrument.ResultingInstrumentNameDataBase = instrumentDatabaseName;
                resultingInstrument.ResultingInstrumentNameTT = resultingInstrumentName.FullName;
                isSuccess = true;
            }
            else
            {
                log.NewEntry(LogLevel.Error, "Cannot get instrument database name for resulting instrument {0}.", resultingInstrumentName);
                return isSuccess;
            }
            return isSuccess;
        }
        //
        //
        //
        /// <summary>
        /// This function will get all month year weights from tt instrument name.
        /// Currently, it assumes the product for each component for this instrument is the same if the input is a spread.
        /// In the future, this function may be improved further. So the function should be enough for calendar spread.
        /// </summary>
        /// <param name="instrumentName"></param>
        /// <param name="weightsByDataTime"></param>
        /// <returns></returns>
        public static bool TryGetMonthYearWeightsFromInstrument(InstrumentName instrumentName, LogHub log, out Dictionary<DateTime, int> weightsByDataTime)
        {
            bool isSuccess = false;
            weightsByDataTime = null;
            ProductTypes productTypes = instrumentName.Product.Type;
            string productName = instrumentName.Product.ProductName;
            string seriesName = instrumentName.SeriesName;
            string pattern;

            switch (productTypes)
            {
                case ProductTypes.Future:
                    // Target: Sep14
                    pattern = @"[a-zA-Z]{3}[0-9]{2}";
                    Match anyMatch = Regex.Match(seriesName, pattern);
                    if (anyMatch.Success)
                    {
                        string matchedValue = anyMatch.Value;
                        DateTime dateTime;
                        if (DateTime.TryParseExact(matchedValue, "MMMyy", null, DateTimeStyles.None, out dateTime))
                        {
                            if (weightsByDataTime == null)
                                weightsByDataTime = new Dictionary<DateTime, int>();
                            weightsByDataTime.Add(dateTime, 1);
                            isSuccess = true;
                        }
                        else
                        {
                            log.NewEntry(LogLevel.Error, "Failed to parse expiry name for string {0}.", matchedValue);
                            return isSuccess;
                        }
                    }
                    else
                    {
                        log.NewEntry(LogLevel.Error, "Failed to match expiry series for string {0}.", seriesName);
                        return isSuccess;
                    }
                    break;
                case ProductTypes.Spread:
                    // Target: Calendar: 1xGE Sep14:-1xDec14
                    List<string> matchedResults = new List<string>();
                    pattern = @"-?\d+x[^:]+(?=(:?))";
                    MatchCollection matchCollection = Regex.Matches(seriesName, pattern);
                    foreach (Match match in matchCollection)
                    {
                        string matchedValue = match.Value;
                        matchedResults.Add(matchedValue);
                    }
                    for (int matchIndex = 0; matchIndex < matchedResults.Count; ++matchIndex)
                    {
                        string value = matchedResults[matchIndex];
                        char separater = 'x';
                        string[] parts = value.Split(new char[] { separater }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 2)
                        {
                            string weightString = parts[0];
                            int weight;
                            if (int.TryParse(weightString, out weight))
                            {
                                string expiryString = parts[1];
                                pattern = @"[a-zA-Z]{3}[0-9]{2}";
                                anyMatch = Regex.Match(expiryString, pattern);
                                if (anyMatch.Success)
                                {
                                    string matchedValue = anyMatch.Value;
                                    DateTime dateTime;
                                    if (DateTime.TryParseExact(matchedValue, "MMMyy", null, DateTimeStyles.None, out dateTime))
                                    {
                                        if (weightsByDataTime == null)
                                            weightsByDataTime = new Dictionary<DateTime, int>();

                                        if (!weightsByDataTime.ContainsKey(dateTime))
                                            weightsByDataTime.Add(dateTime, weight);
                                        else
                                        {
                                            log.NewEntry(LogLevel.Error, "Failed to add expiry for instrument {0}.", instrumentName);
                                            return isSuccess;
                                        }
                                    }
                                    else
                                    {
                                        log.NewEntry(LogLevel.Error, "Failed to parse expiry name for string {0}.", matchedValue);
                                        return isSuccess;
                                    }
                                }
                                else
                                {
                                    log.NewEntry(LogLevel.Error, "Failed to match expiry series for string {0}.", seriesName);
                                    return isSuccess;
                                }
                            }
                            else
                            {
                                log.NewEntry(LogLevel.Error, "Failed to find weight for string {0}.", weightString);
                                return isSuccess;
                            }
                        }
                        else
                        {
                            log.NewEntry(LogLevel.Error, "Failed to find weight and expiry for string {0}.", value);
                            return isSuccess;
                        }
                    }
                    isSuccess = true;
                    break;
                default:
                    break;
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
}
