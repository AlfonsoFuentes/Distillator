using System.Globalization;
using System.Net;

namespace Client.Templates.Units
{
    public static class ProcessVariableHelper
    {
        public static string FormatDisplayValue(double? value)
        {
            if (!value.HasValue) return "";
            var val = value.Value;
            return Math.Abs(val) >= 0.01 || val == 0
                ? val.ToString("F2", CultureInfo.InvariantCulture)
                : val.ToString("F6", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
        }

        public static double? ParseInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            var normalized = input.Replace(',', '.');
            if (double.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                return result;
            return null;
        }
    }
}
