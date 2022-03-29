using System;
using System.Globalization;
using System.Windows.Controls;

namespace WpfPaint
{
    public class Validation : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            int val = 0;
            try
            {
                string sValue = value as string;
                if (sValue == "")
                    return ValidationResult.ValidResult;
                if (sValue.Length > 0 && sValue.Length < 9)
                    val = int.Parse(sValue);
            }
            catch (Exception e)
            {
                return new ValidationResult(false, $"Illegal characters or {e.Message}");
            }

            if (val < 1)
            {
                return new ValidationResult(false,
                  $"Please enter in the range: 1-99999999.");
            }
            return ValidationResult.ValidResult;
        }
    }
}
