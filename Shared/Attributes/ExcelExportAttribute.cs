using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ExcelExportAttribute : Attribute
    {
        public string DisplayName { get; }
        public string Format { get; } // Ej: "0.00", "dd/MM/yyyy", "min"

        public ExcelExportAttribute(string displayName, string format = "")
        {
            DisplayName = displayName;
            Format = format;
        }
    }
}
