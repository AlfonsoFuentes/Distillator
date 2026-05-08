using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Shared.Thermodynamics.ControlledVariables;

namespace Client.Templates.Units
{
    public abstract class DoubleVariableBase : ComponentBase
    {
        [Parameter] public string Label { get; set; } = string.Empty;

        // 👇 Usamos la variable controlada para valores simples (double)
        [Parameter] public ControlledVariable<double> Variable { get; set; } = default!;
        [Parameter] public EventCallback<ControlledVariable<double>> VariableChanged { get; set; }

        [Parameter] public bool IsReadOnly { get; set; } = false;

        // 👇 Validaciones opcionales para fracciones (ej: 0 a 1)
        [Parameter] public double? MinValue { get; set; }
        [Parameter] public double? MaxValue { get; set; }

        protected string? _tempInputValue;
        protected bool _isEditing;
        protected ElementReference _inputRef;

        protected bool IsEffectivelyReadOnly => IsReadOnly || (Variable?.Source == MethodSource.Other);
        protected string GetSourceClass() => $"source-{Variable?.Source.ToString().ToLower() ?? "none"}";

        protected string GetDisplayValue()
        {
            if (_isEditing) return _tempInputValue ?? "";

            if (Variable == null || !Variable.IsDefined)
                return "<Not defined>";

            // Formateo a 4 decimales estándar para fracciones
            return Variable.Value.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
        }

        protected void HandleInput(ChangeEventArgs e) => _tempInputValue = e.Value?.ToString();

        protected async Task HandleKeyUp(KeyboardEventArgs e)
        {
            if (IsEffectivelyReadOnly) return;

            if (e.Key == "Delete")
            {
                Variable?.ClearValue();
                _isEditing = false;
                _tempInputValue = null;
                if (VariableChanged.HasDelegate) await VariableChanged.InvokeAsync(Variable);
                return;
            }

            if (e.Key == "Enter" || e.Key == "NumpadEnter")
            {
                await CommitValue();
            }
            else if (e.Key == "Escape")
            {
                _isEditing = false;
                _tempInputValue = null;
                StateHasChanged();
            }
        }

        protected async Task CommitValue()
        {
            if (IsEffectivelyReadOnly || !_isEditing) return;

            if (_tempInputValue == null)
            {
                _isEditing = false;
                StateHasChanged();
                return;
            }

            if (string.IsNullOrWhiteSpace(_tempInputValue))
            {
                _tempInputValue = null;
                _isEditing = false;
                StateHasChanged();
                return;
            }

            // Parseo seguro a Double
            if (double.TryParse(_tempInputValue.Replace(",", "."),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out double newVal))
            {
                // Validamos límites si fueron definidos
                if (MinValue.HasValue && newVal < MinValue.Value) newVal = MinValue.Value;
                if (MaxValue.HasValue && newVal > MaxValue.Value) newVal = MaxValue.Value;

                Variable?.SetValue(newVal, MethodSource.UserInterface);
            }

            _isEditing = false;
            _tempInputValue = null;
            if (VariableChanged.HasDelegate) await VariableChanged.InvokeAsync(Variable);
        }

        protected async Task EnterEditMode()
        {
            if (!IsEffectivelyReadOnly)
            {
                _isEditing = true;

                if (Variable != null && Variable.IsDefined)
                {
                    _tempInputValue = Variable.Value.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                {
                    _tempInputValue = "";
                }

                StateHasChanged();

                await Task.Delay(50);
                try { await _inputRef.FocusAsync(); } catch { }
            }
        }
    }
}
