using System;
using System.Data.SqlClient;

namespace Common.Utils
{
    public class ApplicationRuleException : ApplicationException
    {
        public ApplicationRuleException(string message)
            : base(message)
        {
        }
        public ApplicationRuleException()
            : base()
        {
        }
    }

    public class BusinessLogicException : ApplicationRuleException
    {
        public BusinessLogicException(string message)
            : base(message)
        {
        }
        public BusinessLogicException()
            : base("An error occurred while communicating with middleware.")
        {
        }
    }
}
