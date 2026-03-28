using BlazorDownloadFile;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Shared.Thermodynamics.Methods;
using System.Drawing;

namespace Client.Services
{
    public class ThermodynamicMethodReportService
    {
        private readonly IBlazorDownloadFileService _downloadService;

        public ThermodynamicMethodReportService(IBlazorDownloadFileService downloadService)
        {
            _downloadService = downloadService;

        }

        public async Task ExportMasterDatabaseAsync(IEnumerable<ThermodynamicMethodDto> methods, string fileName = "Thermodynamic_Methods_Master")
        {
            using var package = new ExcelPackage();

            var colorDark = Color.FromArgb(38, 50, 56);
            var colorMedium = Color.FromArgb(55, 71, 79);

            // ==========================================
            // SHEET 1: METHODS DEFINITION
            // ==========================================
            var wsMethods = package.Workbook.Worksheets.Add("Methods Master");

            int col = 1;
            AddHeaderGroup(wsMethods, ref col, "METHOD IDENTIFICATION", 3, colorDark);
            AddHeaderGroup(wsMethods, ref col, "THERMODYNAMIC MODELS", 2, colorMedium);
            AddHeaderGroup(wsMethods, ref col, "STATS", 2, colorDark);

            // SubHeaders Sheet 1
            col = 1;
            string[] methHeaders = { "ID", "Name", "Description", "Vapor Phase Model", "Liquid Phase Model", "Component Count", "Parameter Count" };
            foreach (var h in methHeaders) ApplySubHeaderStyle(wsMethods.Cells[2, col++], h);

            int row = 3;
            foreach (var m in methods)
            {
                col = 1;
                wsMethods.Cells[row, col++].Value = m.Id.ToString();
                wsMethods.Cells[row, col++].Value = m.Name;
                wsMethods.Cells[row, col++].Value = m.Description;
                wsMethods.Cells[row, col++].Value = m.VaporModel.ToString();
                wsMethods.Cells[row, col++].Value = m.LiquidModel.ToString();
                wsMethods.Cells[row, col++].Value = m.Components.Count;
                wsMethods.Cells[row, col++].Value = m.BinaryParameters.Count;
                row++;
            }
            wsMethods.Cells[1, 1, row - 1, col - 1].AutoFitColumns();

            // ==========================================
            // SHEET 2: BINARY INTERACTION DATABASE
            // ==========================================
            var wsMatrix = package.Workbook.Worksheets.Add("Binary Parameters Database");

            col = 1;
            AddHeaderGroup(wsMatrix, ref col, "METHOD ORIGIN", 1, colorDark);
            AddHeaderGroup(wsMatrix, ref col, "COMPONENT PAIR", 2, colorMedium);
            AddHeaderGroup(wsMatrix, ref col, "PARAMETER DATA", 2, colorDark);

            // SubHeaders Sheet 2
            col = 1;
            string[] paramHeaders = { "Method Name", "Component i", "Component j", "Parameter Type", "Value" };
            foreach (var h in paramHeaders) ApplySubHeaderStyle(wsMatrix.Cells[2, col++], h);

            row = 3;
            foreach (var m in methods)
            {
                foreach (var param in m.BinaryParameters)
                {
                    col = 1;
                    wsMatrix.Cells[row, col++].Value = m.Name; // Referencia cruzada
                    wsMatrix.Cells[row, col++].Value = param.ComponentI_Name;
                    wsMatrix.Cells[row, col++].Value = param.ComponentJ_Name;
                    wsMatrix.Cells[row, col++].Value = param.ParameterType.ToString(); // Ej: Aij, Bij, Alpha
                    wsMatrix.Cells[row, col++].Value = param.Value;
                    row++;
                }
            }
            wsMatrix.Cells[1, 1, row - 1, col - 1].AutoFitColumns();

            // ==========================================
            // DESCARGA DEL ARCHIVO
            // ==========================================
            var fileBytes = await package.GetAsByteArrayAsync();
            await _downloadService.DownloadFile($"{fileName}.xlsx", fileBytes, "application/octet-stream");
        }

        // Helpers de Estilos (Idénticos a los tuyos)
        private void AddHeaderGroup(ExcelWorksheet ws, ref int startCol, string title, int width, Color color)
        {
            var range = ws.Cells[1, startCol, 1, startCol + width - 1];
            range.Merge = true;
            range.Value = title;
            range.Style.Font.Bold = true;
            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(color);
            range.Style.Font.Color.SetColor(Color.White);
            range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.White);
            startCol += width;
        }

        private void ApplySubHeaderStyle(ExcelRange cell, string text)
        {
            cell.Value = text;
            cell.Style.Font.Bold = true;
            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(144, 164, 174));
            cell.Style.Font.Color.SetColor(Color.Black);
            cell.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.White);
        }
    }
}

