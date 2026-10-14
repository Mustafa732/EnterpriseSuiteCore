using System.Diagnostics;
using System.Configuration;
using System;

namespace Common.Config
{
    /// <summary>
    /// Summary description for Config.
    /// </summary>
    public sealed class Constants
    {
        public Constants() { }

        //Tracer Constants
        public static TraceSwitch TRACELEVEL = new TraceSwitch("AppTraceLevel", "Application Trace Level Switch");
        public static string LOGFILENAME = SystemParameters.GetParameter("LOGFILENAME");
        public static string LOGPATH = SystemParameters.GetParameter("LOGFILEPATH");
        public static string TRACEEVENTSOURCE = SystemParameters.GetParameter("TRACEEVENTSOURCE");
        public static string APPLOGFILENAME = ConfigurationManager.AppSettings["APPLOGFILENAME"];
        public static string APPLOGPATH = ConfigurationManager.AppSettings["APPLOGFILEPATH"];
        public static bool ALLOWEVENTLOG = true;
        public const string CRLF = "\r\n";
        public static string APPLICATIONPATH = ".";
        public static string DEBUG_LEVEL = "Debug";

        public const string TaxYear_ErrorMessage = "No tax year info defined for selected contract date";
        /// <summary>
        /// This value is used for successfull insertion of data
        /// </summary>
        public const string Success_AddMessage = "Record is successfully inserted.";
        /// <summary>
        /// This value is used for successfull updation of data
        /// </summary>
        public const string Success_UpdateMessage = "Record is successfully updated.";
        /// <summary>
        /// This value is used for successfull deletion of data
        /// </summary>
        public const string Success_DeleteMessage = "Record is successfully deleted.";
        /// <summary>
        /// This value is used for failed insertion of data
        /// </summary>
        public const string Failure_AddMessage = "Failed to insert a record.";
        /// <summary>
        /// This value is used for failed updation of data
        /// 
        /// 
        /// </summary>
        public const string Failure_UpdateMessage = " Failed to update a record .";
        /// <summary>
        /// 
        /// This value is used for failed deletion of data
        /// </summary>
        public const string Failure_DeleteMessage = " Failed  delete a record .  ";
        /// <summary>
        /// This value is used for unknown error occured
        /// </summary>
        public const string Unkown_Message = "Unknown error occured.";
        /// <summary>
        /// This value is used for unique index insert error occured
        /// </summary>
        public const string UniqueIndexInsertErrorMessage = "Cannot insert duplicate record, {0} already exsist";
        /// <summary>
        /// This value is used for unique index update error occured
        /// </summary>
        public const string UniqueIndexUpdateErrorMessage = "Cannot update duplicate record, {0} already exsist";
                
        /// <summary>
        /// This value is used for Child record delete
        /// </summary>
        public const string DeleteChildRecord_ErrorMessage = "Cannot delete a record, child record {0} is already exsist.";

        /// <summary>
        /// This value is used for Child record update
        /// </summary>
        public const string UpdateChildRecord_ErrorMessage = "Cannot update a record, child record {0} is already exsist.";

        /// <summary>
        /// The value used to represent a null DateTime value
        /// </summary>        
        public static DateTime NullDateTime = DateTime.MinValue;
        /// <summary>
        /// Invalid date Message
        /// </summary>
        public const string InvalidDateErrorMessage = "Invalid {0} {1} format, eg {2}";
        public const string IsNullOrEmptyControl_CutomMessage = "Invalid {0} Field, {0} Field Is Required";
        /// <summary>
        /// This value is used for failed searching of data
        /// </summary>
        public const string Failure_SearchErrorMessage = "Failed to search a record.";
        /// <summary>
        /// The value used to represent a null decimal value
        /// </summary>
        public static decimal NullDecimal = decimal.MinValue;
        /// <summary>
        /// The value used to represent a null double value
        /// </summary>
        public static double NullDouble = double.MinValue;

        /// <summary>
        /// The value used to represent a null Guid value
        /// </summary>
        public static Guid NullGuid = Guid.Empty;

        /// <summary>
        /// The value used to represent a null int value
        /// </summary>
        public static int NullInt = int.MinValue;

        /// <summary>
        /// The value used to represent a null long value
        /// </summary>
        public static long NullLong = long.MinValue;

        /// <summary>
        /// The value used to represent a null float value
        /// </summary>
        public static float NullFloat = float.MinValue;

        /// <summary>
        /// The value used to represent a null string value
        /// </summary>
        public static string NullString = string.Empty;

        public static object GetNullConstant(object obj)
        {
            if (obj == null)
                return obj;

            switch (obj.GetType().Name)
            {
                case "DateTime":
                    return NullDateTime;

                case "Decimal":
                    return NullDecimal;

                case "Double":
                    return NullDouble;

                case "float":
                    return NullFloat;

                case "Guid":
                    return NullGuid;

                case "int":
                    return NullInt;

                case "long":
                    return NullLong;

                case "String":
                    return NullString;
            }

            return obj;
        }
    }
}
