using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FnI.BusinessAccessLayer
{
    public static class ExtensionMethods
    {
        public static decimal ToDecimalOrZero(this String str)
        {
            decimal dec = 0;
            Decimal.TryParse(str, out dec);
            return dec;
        }

        public static decimal? ToDecimalOrNull(this String str)
        {
            decimal nulldec = 0;
            Decimal.TryParse(str, out nulldec);
            if (nulldec == 0)
                return null;
            else
                return nulldec;
        }

        public static float ToFloatOrZero(this String str)
        {
            float Float = 0;
            float.TryParse(str, out Float);
            return Float;
        }
        public static float? ToFloatOrNull(this String str)
        {
            float NullFloat = 0;
            float.TryParse(str, out NullFloat);
            if (NullFloat == 0)
                return null;
            else
                return NullFloat;
        }
        public static int ToIntOrZero(this String str)
        {
            int integer = 0;
            int.TryParse(str, out integer);
            return integer;
        }

        public static int? ToIntOrNull(this String str)
        {
            int nullinteger = 0;
            int.TryParse(str, out nullinteger);
            if (nullinteger == 0)
                return null;
            else
                return nullinteger;
        }
        
    }
}