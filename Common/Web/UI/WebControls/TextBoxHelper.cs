namespace Common.Web.UI.WebControls
{
    public class TextBoxHelper
    {
        /// <summary>
        /// .NET 8 MVC version - string se int? nikalta hai
        /// Purane me TextBox.Text lete the, naye me direct string
        /// </summary>
        public static int? ReturnValueOrNull(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (int.TryParse(text, out int val)) return val;
            return null;
        }

        /// <summary>
        /// Object version - agar kisi control ka Text property hai toh reflection se le lega
        /// Isse tumhara purana call AppControls jaisa hi chalega
        /// </summary>
        public static int? ReturnValueOrNull(object textBox)
        {
            if (textBox == null) return null;

            // Agar direct string aayi
            if (textBox is string s)
                return ReturnValueOrNull(s);

            try
            {
                var textProp = textBox.GetType().GetProperty("Text");
                if (textProp != null)
                {
                    var textValue = textProp.GetValue(textBox)?.ToString();
                    return ReturnValueOrNull(textValue);
                }
            }
            catch { }

            return null;
        }

        // Extra helpers jo naye project me kaam ayenge
        public static decimal? ReturnDecimalOrNull(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (decimal.TryParse(text, out decimal val)) return val;
            return null;
        }

        public static decimal? ReturnDecimalOrNull(object textBox)
        {
            if (textBox == null) return null;
            if (textBox is string s) return ReturnDecimalOrNull(s);
            try
            {
                var textProp = textBox.GetType().GetProperty("Text");
                var textValue = textProp?.GetValue(textBox)?.ToString();
                return ReturnDecimalOrNull(textValue);
            }
            catch { return null; }
        }
    }
}