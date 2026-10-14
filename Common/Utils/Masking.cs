using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Common.Utils
{
    public class Masking
    {
        /// <summary>
        /// Returns mask value for date controls (if all parameters are not defined, then default key will be extracted)
        /// </summary>
        /// <param name="configurationKey">Key specified within configuration file</param>
        /// <param name="datePattern">Pattern format (e.g. dd/MM/yyyy)</param>
        /// <returns>Mask value (e.g. 99/99/9999)</returns>
        public static string GetDateMask(string configurationKey, string datePattern)
        {
            StringBuilder sbMaskToReturn = null;

            if (!string.IsNullOrEmpty(datePattern))
                sbMaskToReturn = new StringBuilder(datePattern);
            else if (!string.IsNullOrEmpty(configurationKey))
                sbMaskToReturn = new StringBuilder(ConfigurationManager.AppSettings[configurationKey]);
            else
                sbMaskToReturn = new StringBuilder(ConfigurationManager.AppSettings["LongDatePattern"]);

            sbMaskToReturn = sbMaskToReturn.Replace("d", "9");
            sbMaskToReturn = sbMaskToReturn.Replace("M", "9");
            sbMaskToReturn = sbMaskToReturn.Replace("y", "9");
            //sbMaskToReturn = sbMaskToReturn.Replace("/", @"\/");
            return sbMaskToReturn.ToString();
        }
    }
}
