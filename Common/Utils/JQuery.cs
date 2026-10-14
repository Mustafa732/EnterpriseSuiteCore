using System.Configuration;

namespace Common.Utils
{
    public class JQuery
    {
        public static string DateConversion(string input = null)
        {
            if (string.IsNullOrEmpty(input))
                input = ConfigurationManager.AppSettings["LongDatePattern"].ToString();

            #region Date used in this comment:
            /*
                *  Date used in this comment:
                *
                *  .NET    JQueryUI        Output      Comment
                *  --------------------------------------------------------------
                *  d       d               5           day of month(No leading zero)
                *  dd      dd              05          day of month(two digit)
                *  ddd     D               Thu         day short name
                *  dddd    DD              Thursday    day long name
                *  M       m               11          month of year(No leading zero)
                *  MM      mm              11          month of year(two digit)
                *  MMM     M               Nov         month name short
                *  MMMM    MM              November    month name long.
                *  yy      y               09          Year(two digit)
                *  yyyy    yy              2009        Year(four digit) 
                */
            #endregion

            string currentFormat = input;

            // Convert the date
            currentFormat = currentFormat.Replace("dddd", "DD");
            currentFormat = currentFormat.Replace("ddd", "D");

            // Convert month
            if (currentFormat.Contains("MMMM"))
            {
                currentFormat = currentFormat.Replace("MMMM", "MM");
            }
            else if (currentFormat.Contains("MMM"))
            {
                currentFormat = currentFormat.Replace("MMM", "M");
            }
            else if (currentFormat.Contains("MM"))
            {
                currentFormat = currentFormat.Replace("MM", "mm");
            }
            else
            {
                currentFormat = currentFormat.Replace("M", "m");
            }

            // Convert year
            currentFormat = currentFormat.Contains("yyyy") ? currentFormat.Replace("yyyy", "yy") : currentFormat.Replace("yy", "y");
            return currentFormat;
        }
    }
}
