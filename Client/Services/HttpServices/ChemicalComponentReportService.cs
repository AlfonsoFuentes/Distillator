using BlazorDownloadFile;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Shared.PropertiesDtos.Components;
using System.Drawing;

namespace Client.Services.HttpServices
{
    public class ChemicalComponentReportService
    {
        private readonly IBlazorDownloadFileService _downloadService;

        public ChemicalComponentReportService(IBlazorDownloadFileService downloadService)
        {
            _downloadService = downloadService;
        }

        public async Task ExportMasterDatabaseAsync(IEnumerable<ChemicalComponentDto> components, string fileName = "Chemical_Master_Database")
        {
  
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Chemical Master Data");

            // --- FILA 1: CABECERAS DE GRUPO (MERGED) ---
            int currentColumn = 1;

            // Colores de la paleta industrial (Gris pizarra y Azul oscuro)
            var colorDark = Color.FromArgb(38, 50, 56);
            var colorMedium = Color.FromArgb(55, 71, 79);
            var colorAccent = Color.FromArgb(69, 90, 100);

            AddHeaderGroup(worksheet, ref currentColumn, "IDENTIFICATION", 6, colorDark);
            AddHeaderGroup(worksheet, ref currentColumn, "CRITICAL & STATE PROPERTIES", 9, colorMedium);
            AddHeaderGroup(worksheet, ref currentColumn, "THERMODYNAMICS OF FORMATION", 4, colorDark);

            string[] correlations = {
            "VAPOR PRESSURE", "HEAT OF VAPORIZATION", "LIQUID CP", "GAS CP",
            "LIQUID VISCOSITY", "GAS VISCOSITY", "LIQUID THERMAL COND",
            "GAS THERMAL COND", "DENSITY", "SURFACE TENSION"
        };

            foreach (var corr in correlations)
            {
                AddHeaderGroup(worksheet, ref currentColumn, corr, 9, colorAccent);
            }

            // --- FILA 2: SUB-CABECERAS TÉCNICAS ---
            WriteSubHeaders(worksheet);

            // --- FILA 3+: LLENADO DE DATOS ---
            int row = 3;
            foreach (var comp in components)
            {
                int col = 1;

                // 1. Identification
                worksheet.Cells[row, col++].Value = comp.Name;
                worksheet.Cells[row, col++].Value = comp.Formula;
                worksheet.Cells[row, col++].Value = comp.StructuralFormula;
                worksheet.Cells[row, col++].Value = comp.Family;
                worksheet.Cells[row, col++].Value = comp.SecondaryFamily;
                worksheet.Cells[row, col++].Value = comp.MolecularWeight;

                // 2. Critical & State
                worksheet.Cells[row, col++].Value = comp.CriticalTemperature.ToString();
                worksheet.Cells[row, col++].Value = comp.CriticalPressure.ToString();
                worksheet.Cells[row, col++].Value = comp.BoilingPoint.ToString();
                worksheet.Cells[row, col++].Value = comp.MeltingPoint.ToString();
                worksheet.Cells[row, col++].Value = comp.CriticalVolume.ToString();
                worksheet.Cells[row, col++].Value = comp.VolumeAsterisk.ToString();
                worksheet.Cells[row, col++].Value = comp.CriticalZ;
                worksheet.Cells[row, col++].Value = comp.AcentricFactor;
                worksheet.Cells[row, col++].Value = comp.AcentricFactorPitzer;

                // 3. Thermodynamics
                worksheet.Cells[row, col++].Value = comp.EnthalpyForm.ToString();
                worksheet.Cells[row, col++].Value = comp.GibbsForm.ToString();
                worksheet.Cells[row, col++].Value = comp.EntropyForm.ToString();
                worksheet.Cells[row, col++].Value = comp.CombustionEnthalpy.ToString();

                // 4. Las 10 Correlaciones (9 columnas cada una)
                WriteCorrelationRow(worksheet, row, ref col, comp.VaporPressure);
                WriteCorrelationRow(worksheet, row, ref col, comp.HeatOfVaporization);
                WriteCorrelationRow(worksheet, row, ref col, comp.LiquidHeatCapacity);
                WriteCorrelationRow(worksheet, row, ref col, comp.GasHeatCapacity);
                WriteCorrelationRow(worksheet, row, ref col, comp.LiquidViscosity);
                WriteCorrelationRow(worksheet, row, ref col, comp.GasViscosity);
                WriteCorrelationRow(worksheet, row, ref col, comp.LiquidThermalCond);
                WriteCorrelationRow(worksheet, row, ref col, comp.GasThermalCond);
                WriteCorrelationRow(worksheet, row, ref col, comp.Density);
                WriteCorrelationRow(worksheet, row, ref col, comp.SurfaceTension);

                row++;
            }

            // Estilo final: Auto-ajuste y bordes
            worksheet.Cells[1, 1, row - 1, currentColumn - 1].AutoFitColumns();
            var fileBytes = await package.GetAsByteArrayAsync();
            await _downloadService.DownloadFile($"{fileName}.xlsx", fileBytes, "application/octet-stream");
        }

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

        private void WriteSubHeaders(ExcelWorksheet ws)
        {
            int col = 1;
            // Identification
            string[] idHeaders = { "Name", "Formula", "Structural Formula", "Family", "Secondary Family", "MW" };
            foreach (var h in idHeaders) ApplySubHeaderStyle(ws.Cells[2, col++], h);

            // Critical
            string[] critHeaders = { "Tc", "Pc", "Tb", "Tm", "Vc", "V*", "Zc", "ω", "ω Pitzer" };
            foreach (var h in critHeaders) ApplySubHeaderStyle(ws.Cells[2, col++], h);

            // Thermo
            string[] thermoHeaders = { "ΔHf", "ΔGf", "Sf", "ΔHc" };
            foreach (var h in thermoHeaders) ApplySubHeaderStyle(ws.Cells[2, col++], h);

            // Repetir C1-C7, Tmin, Tmax para las 10 correlaciones
            string[] coefHeaders = { "C1", "C2", "C3", "C4", "C5", "C6", "C7", "Tmin", "Tmax" };
            for (int i = 0; i < 10; i++)
            {
                foreach (var h in coefHeaders) ApplySubHeaderStyle(ws.Cells[2, col++], h);
            }
        }

        private void ApplySubHeaderStyle(ExcelRange cell, string text)
        {
            cell.Value = text;
            cell.Style.Font.Bold = true;
            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(144, 164, 174)); // Gris claro para contraste
            cell.Style.Font.Color.SetColor(Color.Black);
            cell.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.White);
        }

        private void WriteCorrelationRow(ExcelWorksheet ws, int row, ref int col, CorrelationCoefficientsDto dto)
        {
            ws.Cells[row, col++].Value = dto.C1;
            ws.Cells[row, col++].Value = dto.C2;
            ws.Cells[row, col++].Value = dto.C3;
            ws.Cells[row, col++].Value = dto.C4;
            ws.Cells[row, col++].Value = dto.C5;
            ws.Cells[row, col++].Value = dto.C6;
            ws.Cells[row, col++].Value = dto.C7;
            ws.Cells[row, col++].Value = dto.Tmin.ToString();
            ws.Cells[row, col++].Value = dto.Tmax.ToString();
        }
    }
  
}
