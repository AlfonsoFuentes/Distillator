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

        protected bool IsEffectivelyReadOnly => IsReadOnly || (Variable.Source == MethodSource.Other);
        protected string GetSourceClass() => $"source-{Variable.Source.ToString().ToLower() ?? "none"}";

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
    public abstract class NewDoubleVariableBase : ComponentBase
    {
        [Parameter] public string Label { get; set; } = string.Empty;

        // 🔥 NUEVO: Usamos NewNewVariable<double> en lugar de ControlledVariable<double>
        [Parameter] public NewNewVariableDouble Variable { get; set; } = default!;
        [Parameter] public EventCallback<NewNewVariableDouble> VariableChanged { get; set; }
        [Parameter] public bool IsReadOnly { get; set; } = false;

        // 👇 Validaciones opcionales para fracciones (ej: 0 a 1)
        [Parameter] public double? MinValue { get; set; }
        [Parameter] public double? MaxValue { get; set; }

        // Estado interno del componente (idéntico al anterior)
        protected string? _tempInputValue;
        protected bool _isEditing;
        protected ElementReference _inputRef;

        // 🔥 NUEVO: Read-only si es readonly param O si NO fue definido por UI
        // (Solo lo que definió el usuario se puede editar)
        protected bool IsEffectivelyReadOnly => IsReadOnly || !Variable?.IsDefinedByUI == true;

        // 🔥 NUEVO: Clase CSS basada en flags del nuevo sistema
        protected string GetSourceClass()
        {
            if (Variable == null) return "source-none";
            if (Variable.IsDefinedByUI) return "source-userinterface";
            if (Variable.IsDefinedByStream || Variable.IsDefinedByEquipmentSolver || Variable.IsDefinedByGeneralSolver)
                return "source-other";
            return "source-none";
        }

        // 🔥 NUEVO: Obtener valor formateado para display
        protected string GetDisplayValue()
        {
            if (_isEditing) return _tempInputValue ?? "";
            if (Variable == null || !Variable.IsDefined) return "<Not defined>";

            // Mismo formato: 4 decimales, cultura invariante
            return Variable.Value.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
        }

        // ========== MÉTODOS DE INTERACCIÓN (Lógica de UI idéntica) ==========

        protected void HandleInput(ChangeEventArgs e) => _tempInputValue = e.Value?.ToString();

        protected async Task HandleKeyUp(KeyboardEventArgs e)
        {
            if (IsEffectivelyReadOnly) return;

            // 🔥 Delete para borrar definición (comportamiento idéntico)
            if (e.Key == "Delete")
            {
                Variable?.ClearFromUI(); // 🔥 NUEVO: ClearFromUI en lugar de ClearValue
                _isEditing = false;
                _tempInputValue = null;
                if (VariableChanged.HasDelegate) await VariableChanged.InvokeAsync(Variable);
                return;
            }

            // Enter para confirmar
            if (e.Key == "Enter" || e.Key == "NumpadEnter")
            {
                await CommitValue();
            }
            // Escape para cancelar
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

            // Si el usuario hizo clic y se fue sin tocar nada
            if (_tempInputValue == null)
            {
                _isEditing = false;
                StateHasChanged();
                return;
            }

            // Si borró todo con backspace (no con Delete), cancelar edición sin borrar
            if (string.IsNullOrWhiteSpace(_tempInputValue))
            {
                _tempInputValue = null;
                _isEditing = false;
                StateHasChanged();
                return;
            }

            // Parseo seguro a double (mismo método que antes)
            if (double.TryParse(_tempInputValue.Replace(",", "."),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out double newVal))
            {
                // Validar límites si fueron definidos
                if (MinValue.HasValue && newVal < MinValue.Value) newVal = MinValue.Value;
                if (MaxValue.HasValue && newVal > MaxValue.Value) newVal = MaxValue.Value;

                // 🔥 NUEVO: SetValueFromUI en lugar de SetValue
                Variable?.SetValueFromUI(newVal);
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
                await Task.Delay(50); // Dar tiempo a Blazor para renderizar el input
                try { await _inputRef.FocusAsync(); } catch { }
            }
        }
    }
}
