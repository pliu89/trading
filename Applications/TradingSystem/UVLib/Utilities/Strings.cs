using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.Utilities
{
    public static class Strings
    {

        #region Static String Manipulation Methods
        //
        //
        //
        public static Dictionary<string,string> ExtractKeyValuePairs(string[] arguments, char[] delimiters)
        {
            Dictionary<string, string> keyValues = new Dictionary<string, string>();
            foreach (string arg in arguments)
            {
                string[] pair = arg.Split(delimiters);
                if (pair.Length == 2)
                    keyValues[pair[0]] = pair[1];
            }
            return keyValues;
        }
        //
        //
        //
        // *****************************************************************
        // ****                 TryParseDateTime()                      ****
        // *****************************************************************
        /// <summary>
        /// This function exists because I keep forgetting how to set up all the extra Globalization crap.
        /// So, its here for convenience.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="dateFormat"></param>
        /// <param name="datetime"></param>
        /// <returns>true if dateTime is created</returns>
        public static bool TryParseDate(string s, out DateTime datetime, string dateFormat="yyyyMMdd")
        {
            return DateTime.TryParseExact(s, dateFormat, new System.Globalization.DateTimeFormatInfo(), System.Globalization.DateTimeStyles.None, out datetime);
        }
        //
        //
        #endregion// static string functions




        #region Useful Formatting
        // *****************************************************************
        // ****                Useful Formatting                        ****
        // *****************************************************************
        //        
        /// <summary>
        /// 
        /// Format for 5PM in Chicago -> "02-21-2013 17:29:54.145 -06:00"
        /// </summary>
        public const string FormatDateTimeZone = "MM-dd-yyyy HH:mm:ss.fff zzz";
        public const string FormatDateTimeFull = "MM-dd-yyyy HH:mm:ss";
        public const string FormatTime = "HH:mm:ss.fff";

        //
        #endregion//Useful Formatting


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        //       
        #endregion//Constructors


        #region no Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        #endregion//Properties


        #region no Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //
        //
        //
        //
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
