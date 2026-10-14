using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace FnI.Core.Helper
{
    public class DecimalHelper
    {
        public enum DecimalTypes
        {
            ForAmount,
            ForQuantity
        }

        public static string GetDecimalFormat(DecimalTypes objDecimalTypes = DecimalTypes.ForQuantity)
        {
            string decimalFormat = "#,#.####";

            switch (objDecimalTypes)
            {
                case DecimalTypes.ForAmount:
                    if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["RoundingAmount"]))
                        decimalFormat = ConfigurationManager.AppSettings["RoundingAmount"];

                    break;

                default:
                    if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["RoundingDecimal"]))
                        decimalFormat = ConfigurationManager.AppSettings["RoundingDecimal"];

                    break;
            }

            decimalFormat = decimalFormat.Replace("{", "");
            decimalFormat = decimalFormat.Replace("0", "");
            decimalFormat = decimalFormat.Replace(":", "");
            decimalFormat = decimalFormat.Replace("}", "");

            return decimalFormat;
        }

        public static short GetDecimalsFormatForReports(DecimalTypes objDecimalTypes = DecimalTypes.ForQuantity)
        {
            short formatToReturn = 0; 
            string decimalFormat = GetDecimalFormat(objDecimalTypes);
            string[] decimalSeparated = decimalFormat.Split('.');

            if (decimalSeparated.Count<string>() > 1)
                formatToReturn = Convert.ToInt16(decimalSeparated[1].Count<char>());

            return formatToReturn;
        }
    }
}
