using System;
using System.Collections.Generic;

namespace SpreadPriceGenerator
{
    using Misty.Lib.Hubs;
    using Misty.Lib.Products;

    public class BootStrappingRule
    {
        /// <summary>
        /// This function uses the series name for each instrument name object and create an expiry point object.
        /// </summary>
        /// <param name="expirySeries"></param>
        /// <param name="expiryPoint"></param>
        /// <returns></returns>
        public static bool TryExtractExpiryYearMonth(string expirySeries, out ExpiryPoint expiryPoint)
        {
            expiryPoint = null;
            bool isSuccess = false;

            // The parsing object should contain MMMyy or yyyy/MM format only.
            DateTime dateTime;
            if (DateTime.TryParseExact(expirySeries, "MMMyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dateTime))
            {
                // Firstly, we consider whether it is in format of MMMyy because in most cases, it has date format of MMMyy.
                isSuccess = true;
            }
            else
            {
                if (DateTime.TryParseExact(expirySeries, "yyyy/MM", null, System.Globalization.DateTimeStyles.None, out dateTime))
                {
                    // Then we consider whether the date format is yyyy/MM.
                    isSuccess = true;
                }
                else
                {
                    // If both cases failed, we consider that we failed to parse the date time string.
                    isSuccess = false;
                }
            }

            if (isSuccess)
            {
                // If successful, we create the expiry point required.
                int year = dateTime.Year;
                int month = dateTime.Month;
                expiryPoint = new ExpiryPoint(year - 2000, month);
            }
            return isSuccess;
        }

        /// <summary>
        /// This function analyzes any possible position of expiry date for one product.
        /// </summary>
        /// <param name="existingFutureCollections"></param>
        /// <returns></returns>
        public static bool GetExpirySeriesForProduct(Dictionary<string, InstrumentName> existingFutureCollections, out List<ExpiryPoint> result)
        {
            result = null;
            ExpiryPoint expiryPoint = null;

            // Existing future contracts shows the existence of expiry series.
            foreach (InstrumentName instrumentName in existingFutureCollections.Values)
            {
                if (result == null)
                    result = new List<ExpiryPoint>();

                // We extract the expiry by the series name of the instrument, which contains the expiry date.
                if (TryExtractExpiryYearMonth(instrumentName.SeriesName, out expiryPoint) && !result.Contains(expiryPoint))
                    result.Add(expiryPoint);
            }
            return (result != null);
        }

        /// <summary>
        /// This function contains input of two expiry point, a list of expiry series containing expiry dates for the contract, and output all possible spread combinations.
        /// It uses a method of enumeration to calculate all the paths, making a list of any possible path if the starting point is fixed.
        /// It contains multiple steps to finish the paths from starting point to end point. Once a path ends at the end point, it constitutes a possible spread path.
        /// The program will add to the output list if the path ends, and continue to run other paths until they end.
        /// The expiry series contains a series number of expiry month that is possible.
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="expirySeries"></param>
        /// <param name="spreadCombinations"></param>
        /// <returns></returns>
        public static bool TryGetAllPathsByEnumeration(ExpiryPoint p1, ExpiryPoint p2, List<ExpiryPoint> expirySeries, int maxStep, out List<List<ExpiryPoint>> spreadCombinations)
        {
            spreadCombinations = null;
            List<ExpiryPoint> expiryPath = null;

            // We see the true distance of expiry point as 1 unit. This can be one month or three months.
            // The total true distance is sum of intervals from the start date to the end date.
            int iTotalDistance = TrueDistanceBetweenExpiryPoint(p1, p2, expirySeries, out expiryPath);

            if (iTotalDistance > 0)
            {
                spreadCombinations = new List<List<ExpiryPoint>>();
                List<List<int>> iPathCombination = new List<List<int>>();
                List<List<int>> iTempCombination = new List<List<int>>();
                List<List<int>> iPathResult = new List<List<int>>();
                List<int> initialPath = new List<int>();
                initialPath.Add(0);
                iPathCombination.Add(initialPath);
                int iStep = 0;

                // We do a truncate and only consider a spread combination path that contains limited steps.
                // This algorithm enumerates all the possible paths. If the path reaches the end point, add them to the final result list.
                while (iStep <= iTotalDistance && iStep < maxStep)
                {
                    iTempCombination.Clear();
                    foreach (List<int> iPath in iPathCombination)
                    {
                        // Get the current path and enumerate all the possible step for the next move.
                        int currentPos = iPath[iPath.Count - 1];
                        for (int iDistance = 1; iDistance <= iTotalDistance - currentPos; iDistance++)
                        {
                            List<int> uPath = new List<int>();
                            uPath.AddRange(iPath);
                            uPath.Add(currentPos + iDistance);

                            // If the next move reaches the end, add the path to the final result.
                            if (iDistance == iTotalDistance - currentPos)
                            {
                                iPathResult.Add(uPath);
                                continue;
                            }
                            iTempCombination.Add(uPath);
                        }
                    }
                    iPathCombination = new List<List<int>>(iTempCombination);
                    iStep++;
                }

                // Transform the integral path to expiry point path.
                foreach (List<int> iPath in iPathResult)
                {
                    List<ExpiryPoint> path = new List<ExpiryPoint>();
                    foreach (int pos in iPath)
                    {
                        ExpiryPoint expiryPoint = expiryPath[pos];
                        path.Add(expiryPoint);
                    }
                    spreadCombinations.Add(path);
                }
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// This function requires that the end expiry point date is larger than the start expiry point date.
        /// It gets input of possible expiry months in integral numbers and output the true distance betweeen two expiry points and also the path. 
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="expirySeries"></param>
        /// <param name="expiryPath"></param>
        /// <returns></returns>
        private static int TrueDistanceBetweenExpiryPoint(ExpiryPoint p1, ExpiryPoint p2, List<ExpiryPoint> expirySeries, out List<ExpiryPoint> expiryPath)
        {
            // The total true distance is the sum of the intervals between start point and end point.
            expiryPath = null;
            int trueDistance = 0;
            if (expirySeries == null || expirySeries.Count == 0)
                return trueDistance;

            // This is the physical distance in units of month.
            int physicalDistance = (p2.Year.YearInNumber - p1.Year.YearInNumber) * 12 + (p2.Month.MonthInNumber - p1.Month.MonthInNumber);
            if (physicalDistance <= 0)
                return trueDistance;

            // The output path contains true expiry path given whole expiry series and start/end points.
            expiryPath = new List<ExpiryPoint>();
            for (int iDistance = 0; iDistance <= physicalDistance; iDistance++)
            {
                ExpiryPoint newPoint = ExpiryPoint.AddCreateExpiryPoint(p1, iDistance);
                if (ExpiryPoint.ContainDetermine(expirySeries, newPoint))
                {
                    expiryPath.Add(newPoint);
                    trueDistance++;
                }
            }
            return trueDistance - 1;
        }
    }

    public class ExpiryPoint
    {
        public Month Month;
        public Year Year;

        /// <summary>
        /// Constructor for the expiry point object. It contains a smart way to transform month (larger than 12) to its corresponding desired goal.
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        public ExpiryPoint(int year, int month)
        {
            while (month > 12)
            {
                year++;
                month -= 12;
            }

            Month = new Month(month);
            Year = new Year(year);
        }

        /// <summary>
        /// This is to stringify the expiry point to MMMyy.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("{0}{1}", Month.MonthInString, Year.YearInNumber);
        }

        /// <summary>
        /// This function create a new expiry point given a current expiry point and month increment.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="month"></param>
        /// <returns></returns>
        public static ExpiryPoint AddCreateExpiryPoint(ExpiryPoint p, int month)
        {
            ExpiryPoint np = new ExpiryPoint(p.Year.YearInNumber, p.Month.MonthInNumber + month);
            return np;
        }

        /// <summary>
        /// This function will check whether the expiry point is contained in a list of expiry list with a more accurate way.
        /// </summary>
        /// <param name="expiryList"></param>
        /// <param name="expiry"></param>
        /// <returns></returns>
        public static bool ContainDetermine(List<ExpiryPoint> expiryList, ExpiryPoint expiry)
        {
            if (expiryList == null || expiryList.Count < 1)
                return false;

            // We must loop through all the points in the list and compare the month part and the year part for each of them.
            foreach (ExpiryPoint eachPoint in expiryList)
            {
                if (eachPoint.Month.MonthInNumber == expiry.Month.MonthInNumber && eachPoint.Year.YearInNumber == expiry.Year.YearInNumber)
                    return true;
            }

            return false;
        }
    }

    public class Month
    {
        public int MonthInNumber;
        public string MonthInString;

        /// <summary>
        /// Constructor for month given a month index and initialize both string version and intergral version of month.
        /// </summary>
        /// <param name="number"></param>
        public Month(int number)
        {
            MonthInNumber = number;
            TransformToMonthlyString();
        }

        /// <summary>
        /// This function transform the integral month number to MMM format of month.
        /// </summary>
        private void TransformToMonthlyString()
        {
            // Transform the monthly number to MMM format by switch block.
            switch (MonthInNumber)
            {
                case 1:
                    MonthInString = "Jan";
                    break;
                case 2:
                    MonthInString = "Feb";
                    break;
                case 3:
                    MonthInString = "Mar";
                    break;
                case 4:
                    MonthInString = "Apr";
                    break;
                case 5:
                    MonthInString = "May";
                    break;
                case 6:
                    MonthInString = "Jun";
                    break;
                case 7:
                    MonthInString = "Jul";
                    break;
                case 8:
                    MonthInString = "Aug";
                    break;
                case 9:
                    MonthInString = "Sep";
                    break;
                case 10:
                    MonthInString = "Oct";
                    break;
                case 11:
                    MonthInString = "Nov";
                    break;
                case 12:
                    MonthInString = "Dec";
                    break;
                default:
                    MonthInString = string.Empty;
                    break;
            }
        }
    }

    public class Year
    {
        public int YearInNumber;
        public string YearInString;

        /// <summary>
        /// The year object constructor only contains the last two number. So it creates yyyy string.
        /// The number is in two number but the string is in four characters.
        /// </summary>
        /// <param name="lastTwoNumbers"></param>
        public Year(int lastTwoNumbers)
        {
            YearInNumber = lastTwoNumbers;
            YearInString = (2000 + lastTwoNumbers).ToString();
        }
    }
}
