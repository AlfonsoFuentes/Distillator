using Microsoft.AspNetCore.Components;
using System.Globalization;
using UnitSystem;

namespace Client.Templates.Units
{
    public partial class AmountEdit<T> where T : Amount
    {
        [Parameter] public string? Label { get; set; }
        [Parameter] public T Amount { get; set; } = default(T)!;
        [Parameter] public EventCallback<T> AmountChanged { get; set; }
        [Parameter] public bool IsReadOnly { get; set; }
        [Parameter] public bool AllowUnitChangeWhenReadOnly { get; set; }

        private string FormattedValue => Amount?.Value.ToString("0.####", CultureInfo.InvariantCulture) ?? "";
        private bool IsUnitSelectDisabled => IsReadOnly && !AllowUnitChangeWhenReadOnly;

        private async Task OnInputChanged(ChangeEventArgs e)
        {
            if (IsReadOnly)
                return;

            if (double.TryParse(e.Value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedValue))
            {
                Amount.SetValue(parsedValue, Amount.Unit);
                await AmountChanged.InvokeAsync(Amount);
            }
        }

        private async Task HandleUnitChange(ChangeEventArgs e)
        {
            var unitName = e.Value?.ToString();
            var selectedUnit = Amount.UnitsList.FirstOrDefault(u => u.Name == unitName);

            if (selectedUnit != null)
            {
                Amount.Unit = selectedUnit;
                await AmountChanged.InvokeAsync(Amount);
            }
        }
    }
}
