using System.Text;

namespace Common.Utils
{
    public class FilterInputs
    {
        public static string FilterText(string input)
        {
            StringBuilder sbInput = new StringBuilder(input);

            if (!string.IsNullOrEmpty(input))
            {
                sbInput.Replace("'", "''");
                sbInput.Replace("<script", string.Empty);
            }

            return sbInput.ToString();
        }

        public static string FillNullValue(string input)
        {
            if (input == string.Empty)
                return null;
            else
                return FilterText(input);

        }
    }
}
