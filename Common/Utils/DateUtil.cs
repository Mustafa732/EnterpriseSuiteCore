using System;
using System.Configuration;
using System.Globalization;
using System.Text;
using System.Threading;

using Common.Config;

namespace Common.Utils
{
	public class DateUtil
    {
        #region Constructor

        public DateUtil()
		{
		}

        #endregion

        #region Static Variables

        private static string[] m_MonthsArray = new string[]{"January",
															"February",
															"March",
															"April",
															"May",
															"June",
															"July",
															"August",
															"September",
															"October",
															"November",
															"December"
															};

        #endregion

        public static string ConvertToString(object date)
        {
            if (date == null || date == DBNull.Value)
                return string.Empty;

            try { return ConvertToString(Convert.ToDateTime(date)); }
            catch { return string.Empty; }
        }
		
        /// <summary>
        /// Converts a datetime value into string with/without specified date pattern
        /// </summary>
        /// <param name="strDate">DateTime object to convert</param>
        /// <param name="strPattern">Pattern in which string required (By default loaded from the configuration file, key named as "LongDatePattern")</param>
        /// <returns>String object</returns>
        public static string ConvertToString(DateTime date, bool isDatabasePattern=false, string strPattern = null)
        {
            try
            {
                if (isDatabasePattern)
                    strPattern = ConfigurationManager.AppSettings["DatabasePattern"];
                else if (string.IsNullOrEmpty(strPattern))
                    strPattern = ConfigurationManager.AppSettings["LongDatePattern"];

                string toReturn = string.Empty;

                if (!date.Year.Equals(1))
                    toReturn = date.ToString(strPattern);

                return toReturn;
            }
            catch (FormatException fex)
            {
                throw new BusinessLogicException(string.Format("Pattern: {0}. {1}", strPattern, fex.Message));
            }
        }
        
        /// <summary>
        /// Converts a string representation into DateTime with specified date pattern
        /// </summary>
        /// <param name="strDate">Date in string representation</param>
        /// <param name="strPattern">Pattern in which the string has the date (By default loaded from the configuration file, key named as "LongDatePattern")</param>
        /// <returns>DateTime object</returns>
        public static DateTime ConvertToDate(string strDate, string strPattern = null)
        {
            try
            {
                if (string.IsNullOrEmpty(strPattern))
                    strPattern = ConfigurationManager.AppSettings["LongDatePattern"];

                System.Globalization.DateTimeFormatInfo dateInfo = new System.Globalization.DateTimeFormatInfo();
                dateInfo.ShortDatePattern = strPattern;

                DateTime date = Convert.ToDateTime(strDate, dateInfo);
                return date;
            }
            catch (Exception)
            {
                throw new BusinessLogicException(string.Format(Constants.InvalidDateErrorMessage, string.Empty, strDate, strPattern));
            }
        }

        /// <summary>
        /// Converts an object representation into DateTime with specified date pattern
        /// </summary>
        /// <param name="objDate">Date in object representation</param>
        /// <param name="strPattern">Pattern in which the string has the date (By default loaded from the configuration file, key named as "LongDatePattern")</param>
        /// <returns>DateTime object</returns>
        public static DateTime ConvertToDate(object objDate, string strPattern = null)
        {
            try
            {
                if (string.IsNullOrEmpty(strPattern))
                    strPattern = ConfigurationManager.AppSettings["LongDatePattern"];

                System.Globalization.DateTimeFormatInfo dateInfo = new System.Globalization.DateTimeFormatInfo();
                dateInfo.ShortDatePattern = strPattern;

                DateTime date = Convert.ToDateTime(objDate, dateInfo);
                return date;
            }
            catch (Exception)
            {
                throw new BusinessLogicException(string.Format(Constants.InvalidDateErrorMessage, string.Empty, objDate.ToString(), strPattern));
            }
        }

		public static DateTime GetWeekBeginDate(DateTime inputDate)
		{
			Thread.CurrentThread.CurrentCulture = new CultureInfo(ConfigurationManager.AppSettings["Language-Country"]);
            int dayOfWeek = GetDayOfWeek(inputDate.DayOfWeek);

			switch (dayOfWeek)
			{
                case 0: return inputDate.Subtract(TimeSpan.FromDays(0.0));
                case 1: return inputDate.Subtract(TimeSpan.FromDays(1.0));
                case 2: return inputDate.Subtract(TimeSpan.FromDays(2.0));
                case 3: return inputDate.Subtract(TimeSpan.FromDays(3.0));
                case 4: return inputDate.Subtract(TimeSpan.FromDays(4.0));
                case 5: return inputDate.Subtract(TimeSpan.FromDays(5.0));
                case 6: return inputDate.Subtract(TimeSpan.FromDays(6.0));
			}

            return inputDate;
		}
		
		public static int GetDayOfWeek(DayOfWeek inputDayOfWeek)
		{
            switch (inputDayOfWeek)
            {
                case DayOfWeek.Monday: return 0;
                case DayOfWeek.Tuesday: return 1;
                case DayOfWeek.Wednesday: return 2;
                case DayOfWeek.Thursday: return 3;
                case DayOfWeek.Friday: return 4;
                case DayOfWeek.Saturday: return 5;
                case DayOfWeek.Sunday: return 6;
            }

			return 0;
		}

		public static string GetMonthTitle(int monthID)
		{
            if (monthID > 0 && monthID <= 12)
                return m_MonthsArray[monthID - 1];

			return string.Empty;
		}

        public static DateTime ToUpper(DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day, 23, 59, 59, 997);
        }

        public static DateTime ToLower(DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day);
        }

        public static short GetMonthDifference(DateTime startDate, DateTime endDate)
        {
            return Convert.ToInt16(Math.Ceiling(endDate.Subtract(startDate).TotalDays / 365));
        }

        public static long ConvertDateToTicks(DateTime date)
        {
            TimeSpan ts;

            ts = date.TimeOfDay;
            long ticksValue = ts.Ticks;
            return ticksValue;
        }

        public static DateTime ConvertTicksToDate(long ticks)
        {
            DateTime Date = new DateTime(ticks);
            return Date;
        }
	}
}
