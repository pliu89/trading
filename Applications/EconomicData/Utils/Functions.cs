using System;
using System.Text;

namespace EconomicBloombergProject
{
    public static class Functions
    {
        /// <summary>
        /// This function uses date time parse to get the desired date.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static DateTime StringToDateTime(string dateTime)
        {
            DateTime dateValue = DateTime.MaxValue;
            
            // Try to parse a 8 digits yyyyMMdd to system datetime type.
            try
            {
                DateTime.TryParseExact(dateTime, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dateValue);
                return dateValue;
            }

            // Catch the parsing format error exception.
            catch (FormatException)
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendFormat("Functions_StringToDateTime:There is mistake in transforming the date string to system date time type. The input string is {0}", dateTime);
                Console.WriteLine(stringBuilder.ToString());
                Logging.WriteErrorLog(stringBuilder.ToString());
            }
            return dateValue;
        }

        /// <summary>
        /// Convert date time string format to system datetime type.
        /// </summary>
        /// <param name="datetime"></param>
        /// <returns></returns>
        public static DateTime DTStringToDateTime(string datetime)
        {
            // the string must have format of "yyyyMMdd HH:mm:ss" or "yyyyMMdd HH:mm:ss.000".
            if (datetime.Length < 17)
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendFormat("EconomicDataPoint_StringToDateTime:The length of the date time string should not be less than 17. The input string is {0}", datetime);
                Console.WriteLine(stringBuilder.ToString());
                Logging.WriteErrorLog(stringBuilder.ToString());
                return DateTime.MaxValue;
            }
            else
            {
                // Try to parse the year, month, day, hour, minute and second out and make a new date time.
                int year;
                int month;
                int day;
                int hour;
                int minute;
                int second;

                // Try parse and record error if there is problem in any parse.
                if (!Int32.TryParse(datetime.Substring(0, 4), out year))
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.AppendFormat("EconomicDataPoint_StringToDateTime:The year transformation has error. The input string is {0}", datetime);
                    Console.WriteLine(stringBuilder.ToString());
                    Logging.WriteErrorLog(stringBuilder.ToString());
                    return DateTime.MaxValue;
                }
                if (!Int32.TryParse(datetime.Substring(4, 2), out month))
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.AppendFormat("EconomicDataPoint_StringToDateTime:The month transformation has error. The input string is {0}", datetime);
                    Console.WriteLine(stringBuilder.ToString());
                    Logging.WriteErrorLog(stringBuilder.ToString());
                    return DateTime.MaxValue;
                }
                if (!Int32.TryParse(datetime.Substring(6, 2), out day))
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.AppendFormat("EconomicDataPoint_StringToDateTime:The day transformation has error. The input string is {0}", datetime);
                    Console.WriteLine(stringBuilder.ToString());
                    Logging.WriteErrorLog(stringBuilder.ToString());
                    return DateTime.MaxValue;
                }
                if (!Int32.TryParse(datetime.Substring(9, 2), out hour))
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.AppendFormat("EconomicDataPoint_StringToDateTime:The hour transformation has error. The input string is {0}", datetime);
                    Console.WriteLine(stringBuilder.ToString());
                    Logging.WriteErrorLog(stringBuilder.ToString());
                    return DateTime.MaxValue;
                }
                if (!Int32.TryParse(datetime.Substring(12, 2), out minute))
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.AppendFormat("EconomicDataPoint_StringToDateTime:The minute transformation has error. The input string is {0}", datetime);
                    Console.WriteLine(stringBuilder.ToString());
                    Logging.WriteErrorLog(stringBuilder.ToString());
                    return DateTime.MaxValue;
                }
                if (!Int32.TryParse(datetime.Substring(15, 2), out second))
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.AppendFormat("EconomicDataPoint_StringToDateTime:The second transformation has error. The input string is {0}", datetime);
                    Console.WriteLine(stringBuilder.ToString());
                    Logging.WriteErrorLog(stringBuilder.ToString());
                    return DateTime.MaxValue;
                }

                // Make a new date time.
                DateTime newDateTime = new DateTime(year, month, day, hour, minute, second);
                return newDateTime;
            }
        }

        /// <summary>
        /// Convert to UNIX time stamp using inputs of date, time and bloomberg time zone ID.
        /// </summary>
        /// <param name="datestr"></param>
        /// <param name="timestr"></param>
        /// <param name="BBGTimeZoneID"></param>
        /// <returns></returns>
        public static double ConvertToUnixTimeStamp(string datestr, string timestr, int BBGTimeZoneID)
        {
            if (timestr == "N/A")
            {
                // If there is error in time string, return smallest UNIX time stamp.
                return 0;
            }
            else
            {
                string CSharpTimeZoneID = ConvertToCSharpTimeZoneID(BBGTimeZoneID);
                TimeZoneInfo CSharpTimeZone = TimeZoneInfo.FindSystemTimeZoneById(CSharpTimeZoneID);                // C# built-in function to get system time zone ID.
                string datetimestr = datestr + " " + timestr;
                DateTime datetime = Functions.DTStringToDateTime(datetimestr);
                DateTime DateTime = TimeZoneInfo.ConvertTimeToUtc(datetime, CSharpTimeZone);                        // C# built-in function to transfer to UTC time zone from other time zones.
                DateTime UNIXTimeBase = new DateTime(1970, 1, 1, 0, 0, 0, 0);                                       // Beginning of UNIX date time.
                TimeSpan UNIXTimeOffSet = DateTime - UNIXTimeBase;
                return UNIXTimeOffSet.TotalSeconds;
            }
        }

        /// <summary>
        /// Convert bloomberg time zone to C# system time zone ID.
        /// </summary>
        /// <param name="BBGTimeZone"></param>
        /// <returns></returns>
        public static string ConvertToCSharpTimeZoneID(int BBGTimeZone)
        {
            switch (BBGTimeZone)
            {
                case 1:
                    return "Dateline Standard Time";
                case 2:
                    return "Samoa Standard Time";
                case 3:
                    return "Hawaiian Standard Time";
                case 4:
                    return "Alaskan Standard Time";
                case 5:
                    return "Pacific Standard Time";
                case 6:
                    return "Mountain Standard Time";
                case 7:
                    return "US Mountain Standard Time";
                case 8:
                    return "Mexico Standard Time";
                case 9:
                    return "Central Standard Time";
                case 10:
                    return "Canada Central Standard Time";
                case 11:
                    return "Eastern Standard Time";
                case 12:
                    return "SA Pacific Standard Time";
                case 13:
                    return "Eastern Standard Time";
                case 14:
                    return "Pacific SA Standard Time";
                case 15:
                    return "Venezuela Standard Time";
                case 16:
                    return "Atlantic Standard Time";
                case 17:
                    return "Newfoundland Standard Time";
                case 18:
                    return "Argentina Standard Time";
                case 19:
                    return "E. South America Standard Time";
                case 20:
                    return "UTC-02";
                case 21:
                    return "Azores Standard Time";
                case 22:
                    return "GMT Standard Time";
                case 23:
                    return "UTC";
                case 24:
                    return "W. Europe Standard Time";
                case 25:
                    return "GTB Standard Time";
                case 26:
                    return "FLE Standard Time";
                case 27:
                    return "South Africa Standard Time";
                case 28:
                    return "Israel Standard Time";
                case 29:
                    return "Egypt Standard Time";
                case 30:
                    return "Arab Standard Time";
                case 31:
                    return "Russian Standard Time";
                case 32:
                    return "Iran Standard Time";
                case 33:
                    return "Arabian Standard Time";
                case 34:
                    return "Afghanistan Standard Time";
                case 35:
                    return "Pakistan Standard Time";
                case 36:
                    return "India Standard Time";
                case 37:
                    return "Bangladesh Standard Time";
                case 38:
                    return "SE Asia Standard Time";
                case 39:
                    return "China Standard Time";
                case 40:
                    return "China Standard Time";
                case 41:
                    return "Tokyo Standard Time";
                case 42:
                    return "Cen. Australia Standard Time";
                case 43:
                    return "AUS Central Standard Time";
                case 44:
                    return "Eastern Standard Time";
                case 45:
                    return "E. Australia Standard Time";
                case 46:
                    return "Tasmania Standard Time";
                case 47:
                    return "Vladivostok Standard Time";
                case 48:
                    return "Magadan Standard Time";
                case 49:
                    return "New Zealand Standard Time";
                case 50:
                    return "Fiji Standard Time";
                case 51:
                    return "Central Asia Standard Time";
                case 52:
                    return "Pacific SA Standard Time";
                case 53:
                    return "Montevideo Standard Time";
                case 54:
                    return "W. Central Africa Standard Time";
                case 55:
                    return "Eastern Standard Time";
                case 56:
                    return "Central America Standard Time";
                case 57:
                    return "W. Europe Standard Time";
                case 58:
                    return "South Africa Standard Time";
                case 59:
                    return "Pacific SA Standard Time";
                case 60:
                    return "Morocco Standard Time";
                case 61:
                    return "Ulaanbaatar Standard Time";
                case 62:
                    return "W. Australia Standard Time";
                case 63:
                    return "Samoa Standard Time";
                case 64:
                    return "Ekaterinburg Standard Time";
                case 65:
                    return "West Asia Standard Time";
                case 66:
                    return "Azerbaijan Standard Time";
                case 67:
                    return "Myanmar Standard Time";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Convert bloomberg time zone to exchange location name.
        /// </summary>
        /// <param name="BBGTimeZone"></param>
        /// <returns></returns>
        public static string ConvertToExchangeLocationName(int BBGTimeZone)
        {
            switch (BBGTimeZone)
            {
                case 1:
                    return "Etc/GMT+12";
                case 2:
                    return "Pacific/Midway";
                case 3:
                    return "Pacific/Honolulu";
                case 4:
                    return "America/Anchorage";
                case 5:
                    return "America/Los_Angeles";
                case 6:
                    return "America/Denver";
                case 7:
                    return "America/Phoenix";
                case 8:
                    return "America/Mexico_City";
                case 9:
                    return "America/Chicago";
                case 10:
                    return "America/Regina";
                case 11:
                    return "Etc/GMT+5";
                case 12:
                    return "America/Bogota";
                case 13:
                    return "America/New_York";
                case 14:
                    return "America/Santiago";
                case 15:
                    return "America/Caracas";
                case 16:
                    return "America/Halifax";
                case 17:
                    return "America/St_Johns";
                case 18:
                    return "America/Argentina/Buenos_Aires";
                case 19:
                    return "America/Sao_Paulo";
                case 20:
                    return "America/Noronha";
                case 21:
                    return "Atlantic/Azores";
                case 22:
                    return "Europe/London";
                case 23:
                    return "Etc/GMT";
                case 24:
                    return "Europe/Berlin";
                case 25:
                    return "Europe/Athens";
                case 26:
                    return "Europe/Kiev";
                case 27:
                    return "Africa/Johannesburg";
                case 28:
                    return "Asia/Jerusalem";
                case 29:
                    return "Africa/Cairo";
                case 30:
                    return "Asia/Riyadh";
                case 31:
                    return "Europe/Moscow";
                case 32:
                    return "Asia/Tehran";
                case 33:
                    return "Asia/Dubai";
                case 34:
                    return "Asia/Kabul";
                case 35:
                    return "Asia/Karachi";
                case 36:
                    return "Asia/Calcutta";
                case 37:
                    return "Asia/Dhaka";
                case 38:
                    return "Asia/Bangkok";
                case 39:
                    return "Asia/Shanghai";
                case 40:
                    return "Asia/Hong_Kong";
                case 41:
                    return "Asia/Tokyo";
                case 42:
                    return "Australia/Adelaide";
                case 43:
                    return "Australia/Darwin";
                case 44:
                    return "Australia/Melbourne";
                case 45:
                    return "Australia/Brisbane";
                case 46:
                    return "Australia/Hobart";
                case 47:
                    return "Asia/Vladivostok";
                case 48:
                    return "Asia/Magadan";
                case 49:
                    return "Pacific/Auckland";
                case 50:
                    return "Pacific/Fiji";
                case 51:
                    return "Asia/Almaty";
                case 52:
                    return "America/Puerto_Rico";
                case 53:
                    return "America/Montevideo";
                case 54:
                    return "Africa/Lagos";
                case 55:
                    return "America/Cayman";
                case 56:
                    return "America/Guatemala";
                case 57:
                    return "Africa/Tunis";
                case 58:
                    return "Africa/Tripoli";
                case 59:
                    return "America/Guyana";
                case 60:
                    return "Africa/Casablanca";
                case 61:
                    return "Asia/Ulaanbaatar";
                case 62:
                    return "Australia/Perth";
                case 63:
                    return "Pacific/Apia";
                case 64:
                    return "Asia/Yekaterinburg";
                case 65:
                    return "Asia/Tashkent";
                case 66:
                    return "Asia/Baku";
                case 67:
                    return "Asia/Rangoon";
                default:
                    return string.Empty;
            }
        }
    }
}
