using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Shared.Thermodynamics.ControlledVariables;
using UnitSystem;

namespace Client.Templates.Units
{
    public abstract class VariableBase<T> : ComponentBase where T : Amount
    {
        [Parameter] public string Label { get; set; } = string.Empty;
        [Parameter] public ControlledAmountVariable<T> Variable { get; set; } = default!;
        [Parameter] public EventCallback<ControlledAmountVariable<T>> VariableChanged { get; set; }
        [Parameter] public bool IsReadOnly { get; set; } = false;

        protected string? _tempInputValue;
        protected bool _isEditing;
        protected bool _showUnitSelector;
        protected ElementReference _inputRef; // Referencia nativa de Blazor para el foco

        protected bool IsEffectivelyReadOnly => IsReadOnly || (Variable?.Source == MethodSource.Other);
        protected string GetSourceClass() => $"source-{Variable?.Source.ToString().ToLower() ?? "none"}";

        protected string GetDisplayValue()
        {
            if (_isEditing) return _tempInputValue ?? "";

            if (Variable == null || !Variable.IsDefined || Variable.Value == null)
                return "<Not defined>";

            return ProcessVariableHelper.FormatDisplayValue(Variable.GetDisplayValueAsDouble());
        }

        protected void HandleInput(ChangeEventArgs e) => _tempInputValue = e.Value?.ToString();

        protected async Task HandleKeyUp(KeyboardEventArgs e)
        {
            if (IsEffectivelyReadOnly) return;

            // EL BOTÓN DE AUTODESTRUCCIÓN: Única y exclusivamente con "Delete" (Suprimir)
            if (e.Key == "Delete")
            {
                Variable?.ClearValue();
                _isEditing = false;
                _tempInputValue = null;
                if (VariableChanged.HasDelegate) await VariableChanged.InvokeAsync(Variable);
                return;
            }

            // 1. Confirmar con Enter
            if (e.Key == "Enter" || e.Key == "NumpadEnter")
            {
                await CommitValue();
            }
            // 2. Cancelar edición con Escape
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

            // EL SALVAVIDAS: Si es null, hizo clic y se fue sin tocar nada
            if (_tempInputValue == null)
            {
                _isEditing = false;
                StateHasChanged();
                return;
            }

            // EL ABORTO POR BACKSPACE: 
            // Si usted borró todo el texto hacia atrás pero no presionó Delete, 
            // no desdefinimos. Simplemente cancelamos la edición y restauramos el valor.
            if (string.IsNullOrWhiteSpace(_tempInputValue))
            {
                _tempInputValue = null;
                _isEditing = false;
                StateHasChanged();
                return;
            }

            // EL GUARDADO NORMAL
            var newVal = ProcessVariableHelper.ParseInput(_tempInputValue);
            if (newVal.HasValue && Variable?.Value != null)
            {
                var currentUnit = Variable.GetDisplayUnit();
                if (currentUnit != null)
                {
                    var newValue = (T)Activator.CreateInstance(typeof(T), newVal.Value, currentUnit)!;
                    Variable.SetValue(newValue, MethodSource.UserInterface);
                }
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

                // Clonamos el valor actual o dejamos vacío si no existe
                if (Variable != null && Variable.IsDefined && Variable.Value != null)
                {
                    _tempInputValue = ProcessVariableHelper.FormatDisplayValue(Variable.GetDisplayValueAsDouble());
                }
                else
                {
                    _tempInputValue = "";
                }

                StateHasChanged(); // Le decimos a Blazor que renderice el input

                // LE DAMOS 50ms A BLAZOR PARA ASEGURAR QUE EL INPUT EXISTA EN PANTALLA ANTES DEL FOCO
                await Task.Delay(50);
                try
                {
                    await _inputRef.FocusAsync();
                }
                catch { /* Prevención por si el componente se destruye muy rápido */ }
            }
        }

        protected async Task SelectUnit(UnitMeasure unit)
        {
            if (Variable?.Value == null) return;
            Variable.SetPreferredUnit(unit);
            _showUnitSelector = false;
            if (VariableChanged.HasDelegate) await VariableChanged.InvokeAsync(Variable);
        }
    }
    public abstract class NewVariableBase<T> : ComponentBase where T : Amount
    {
        [Parameter] public string Label { get; set; } = string.Empty;
        [Parameter] public NewNewVariableAmount<T> Variable { get; set; } = default!;
        [Parameter] public EventCallback<NewNewVariableAmount<T>> VariableChanged { get; set; }
        [Parameter] public bool IsReadOnly { get; set; } = false;

        // Estado interno del componente (igual que antes)
        protected string? _tempInputValue;
        protected bool _isEditing;
        protected bool _showUnitSelector;
        protected ElementReference _inputRef;

        // 🔥 NUEVO: Determinar si es readonly basado en flags del nuevo sistema
        protected bool IsEffectivelyReadOnly => IsReadOnly || Variable?.IsDefinedByStream == true;

        // 🔥 NUEVO: Obtener clase CSS basada en fuente (mapeo de flags a string)
        protected string GetSourceClass()
        {
            if (Variable == null) return "source-none";
            if (Variable.IsDefinedByUI) return "source-userinterface";
            if (Variable.IsDefinedByStream || Variable.IsDefinedByEquipmentSolver || Variable.IsDefinedByGeneralSolver)
                return "source-other";
            return "source-none";
        }

        // 🔥 NUEVO: Obtener valor para display usando el nuevo método
        protected string GetDisplayValue()
        {
            if (_isEditing) return _tempInputValue ?? "";
            if (Variable == null || !Variable.IsDefined) return "<Not defined>";
            return ProcessVariableHelper.FormatDisplayValue(Variable.GetDisplayValue());
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

            // Parsear y guardar valor
            var newVal = ProcessVariableHelper.ParseInput(_tempInputValue);
            if (newVal.HasValue && Variable?.Value != null)
            {
                var currentUnit = Variable.UnitForUI; // 🔥 NUEVO: UnitForUI en lugar de GetPreferredUnit
                if (currentUnit != null)
                {
                    // Usar el factory para crear nueva instancia del tipo T
                    var newValue = Variable.GetValue(newVal.Value);
                    Variable.SetValueFromUI(newValue); // 🔥 NUEVO: SetValueFromUI
                }
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
                    _tempInputValue = ProcessVariableHelper.FormatDisplayValue(Variable.GetDisplayValue());
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

        protected async Task SelectUnit(UnitMeasure unit)
        {
            if (Variable == null) return;
            Variable.ChangeUnitForUI(unit); // 🔥 NUEVO: ChangeUnitForUI en lugar de SetPreferredUnit
            _showUnitSelector = false;
            if (VariableChanged.HasDelegate) await VariableChanged.InvokeAsync(Variable);
            StateHasChanged(); // Forzar re-render para actualizar display
        }
    }
}
