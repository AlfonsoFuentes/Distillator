using BlazorDownloadFile;
using OfficeOpenXml;
using Shared.Attributes;
using System.Reflection;
using UnitSystem;

namespace Client.Services.HttpServices
{
    public class ExcelExportService
    {
        private readonly IBlazorDownloadFileService _downloadService;

        public ExcelExportService(IBlazorDownloadFileService downloadService)
        {
            _downloadService = downloadService;
        }

        public async Task ExportToExcel<T>(IEnumerable<T> data, string fileName)
        {

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Data");

            // Filtrar SOLO las propiedades que tengan nuestro atributo
            var props = typeof(T).GetProperties()
                .Where(p => p.GetCustomAttribute<ExcelExportAttribute>() != null)
                .Select(p => new
                {
                    Property = p,
                    Attr = p.GetCustomAttribute<ExcelExportAttribute>()
                }).ToList();

            // CABECERAS INDUSTRIALES
            for (int i = 0; i < props.Count; i++)
            {
                var cell = worksheet.Cells[1, i + 1];
                cell.Value = props[i].Attr?.DisplayName ?? string.Empty;
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(55, 71, 79));
                cell.Style.Font.Color.SetColor(System.Drawing.Color.White);
            }

            // DATOS
            var row = 2;
            foreach (var item in data)
            {
                for (int i = 0; i < props.Count; i++)
                {
                    var value = props[i].Property.GetValue(item);
                    var cell = worksheet.Cells[row, i + 1];

                    if (value == null)
                    {
                        cell.Value = null;
                    }
                    else if (value is Amount amount) // Aquí es donde ocurre la "magia"
                    {
                        cell.Value = amount.ToString(); // Enviamos el número puro
                    }
                    else if (value.GetType().IsEnum)
                    {
                        cell.Value = value.ToString(); // Los Enums los pasamos como texto
                    }
                    else
                    {
                        cell.Value = value; // Tipos primitivos (string, int, bool)
                    }
                }
                row++;
            }

            worksheet.Cells.AutoFitColumns();
            var fileBytes = await package.GetAsByteArrayAsync();
            await _downloadService.DownloadFile($"{fileName}.xlsx", fileBytes, "application/octet-stream");
        }
    }
 
}
