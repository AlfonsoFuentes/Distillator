using Distillator.Domain.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Client.Pages.Dialogs.ProjectForm
{
    public partial class NamingMigrationDialog
    {
        [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
        [Parameter] public Project Project { get; set; } = default!;
        [Parameter] public bool RequiresDiagramNumbers { get; set; }

        private List<DiagramMigrationItem> _diagrams = new();
        private bool _renameExisting = true;
        private string? _validationError;

        private bool IsValid => _diagrams.All(d => !string.IsNullOrWhiteSpace(d.DiagramNumber) && !d.HasError) && string.IsNullOrEmpty(_validationError);

        protected override void OnInitialized()
        {
            if (!RequiresDiagramNumbers)
            {
                return;
            }

            foreach (var fs in Project.Flowsheets)
            {
                _diagrams.Add(new DiagramMigrationItem
                {
                    Flowsheet = fs,
                    DiagramNumber = fs.DiagramNumber?.Trim() ?? string.Empty
                });
            }

            Validate();
        }

        private void OnDiagramNumberChanged(DiagramMigrationItem item, string? value)
        {
            item.DiagramNumber = value?.Trim() ?? string.Empty;
            Validate();
        }

        private void Validate()
        {
            _validationError = null;

            foreach (var item in _diagrams)
            {
                item.HasError = string.IsNullOrWhiteSpace(item.DiagramNumber);
            }

            var duplicatesInNew = _diagrams
                .Where(d => !string.IsNullOrWhiteSpace(d.DiagramNumber))
                .GroupBy(d => d.DiagramNumber, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicatesInNew.Any())
            {
                foreach (var item in _diagrams.Where(d => duplicatesInNew.Contains(d.DiagramNumber, StringComparer.OrdinalIgnoreCase)))
                {
                    item.HasError = true;
                }
                _validationError = "Duplicate numbers found in the list.";
                return;
            }

            var existingNumbers = Project.Flowsheets
                .Where(f => !string.IsNullOrWhiteSpace(f.DiagramNumber) && !_diagrams.Any(d => d.Flowsheet.Id == f.Id))
                .Select(f => f.DiagramNumber.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var item in _diagrams.Where(d => !string.IsNullOrWhiteSpace(d.DiagramNumber)))
            {
                if (existingNumbers.Contains(item.DiagramNumber))
                {
                    item.HasError = true;
                    _validationError = $"Number '{item.DiagramNumber}' is already in use by another diagram.";
                }
            }
        }

        private void Cancel() => MudDialog.Cancel();

        private void Confirm()
        {
            Validate();
            if (!IsValid) return;

            var result = new NamingMigrationResult
            {
                UpdatedDiagrams = _diagrams.Select(d => (d.Flowsheet, d.DiagramNumber)).ToList(),
                RenameExisting = _renameExisting
            };

            MudDialog.Close(DialogResult.Ok(result));
        }

        public class DiagramMigrationItem
        {
            public IFlowsheet Flowsheet { get; set; } = default!;
            public string DiagramNumber { get; set; } = string.Empty;
            public bool HasError { get; set; }
        }
    }

    public class NamingMigrationResult
    {
        public List<(IFlowsheet Flowsheet, string DiagramNumber)> UpdatedDiagrams { get; set; } = new();
        public bool RenameExisting { get; set; }
    }
}
