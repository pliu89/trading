using System;

namespace VioletAPI.Lib.TradingHelper
{
    public class Functions
    {

        #region Public Methods
        /// <summary>
        /// This function output the next nearest bar update datetime.
        /// </summary>
        /// <param name="currentDateTime"></param>
        /// <param name="barIntervalSeconds"></param>
        /// <returns></returns>
        public static DateTime GetNextBarUpdateDateTime(DateTime currentDateTime, int barIntervalSeconds)
        {
            int totalSeconds = (int)currentDateTime.TimeOfDay.TotalSeconds + 1;
            while (totalSeconds % barIntervalSeconds != 0)
                totalSeconds++;
            currentDateTime = currentDateTime.Date.AddSeconds(totalSeconds);
            return currentDateTime;
        }
        #endregion

    }
}
