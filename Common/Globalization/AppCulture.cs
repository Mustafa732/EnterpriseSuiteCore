using System.Configuration;
using System.Globalization;
using System.Threading;

namespace Common.Globalization
{
    public class AppCulture
    {
        public static void SetCurrentCulture()
        {
            CultureInfo newCulture = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();

            SetDateTimeCulture(ref newCulture);

            Thread.CurrentThread.CurrentCulture = newCulture;
        }

        private static void SetDateTimeCulture(ref CultureInfo culture)
        {
            culture.DateTimeFormat.AMDesignator = "AM";
            culture.DateTimeFormat.PMDesignator = "PM";

            culture.DateTimeFormat.DateSeparator = "-";
            culture.DateTimeFormat.TimeSeparator = ":";

            culture.DateTimeFormat.ShortDatePattern = "dd-MM-yy";
            culture.DateTimeFormat.LongDatePattern = ConfigurationManager.AppSettings["LongDatePattern"];

            culture.DateTimeFormat.ShortTimePattern = "hh:mm tt";
            culture.DateTimeFormat.LongTimePattern = "hh:mm:ss tt";

            culture.DateTimeFormat.FullDateTimePattern = "dd-MM-yyyy hh:mm:ss tt";
            culture.DateTimeFormat.YearMonthPattern = "MMM yyyy";
        }

        public static CultureInfo GetCulture()
        {
            return Thread.CurrentThread.CurrentCulture;
        }
    }
}
