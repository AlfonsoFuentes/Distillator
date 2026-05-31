using UnitSystem;

namespace Shared.SolverQwen.Stream
{
    public class CompositionOrchestrator
    {
        private readonly IReadOnlyList<ComponentFacade> _components;

        public List<ComponentFacade> Components => _components.ToList();

        public event Action OnCompositionChanged = null!;

        // ✅ Detección de Estado Efímero Dinámico
        public bool HasChanged => _components.Any(c =>
            c.MassFraction.HasChanged ||
            c.MolarFraction.HasChanged ||
            c.MassFlow.HasChanged ||
            c.MolarFlow.HasChanged);

        public CompositionOrchestrator(IReadOnlyList<ComponentFacade> components)
        {
            _components = components ?? throw new ArgumentNullException(nameof(components));
        }

        public void CompositionChanged()
        {
            OnCompositionChanged?.Invoke();
        }

        public bool ValidateMassFractions(out string error)
        {
            error = null!;
            if (!_components.Any()) { error = "No components"; return false; }

            double sum = _components.Where(c => c.MassFraction.IsDefined)
                                    .Sum(c => c.MassFraction.Value.GetValue(PercentageUnits.Percentage) / 100.0);

            if (sum == 0) { error = "No mass fractions defined"; return false; }
            if (Math.Abs(sum - 1.0) > 1e-6) { error = $"Mass fractions sum {sum * 100:F2}% (expected 100%)"; return false; }
            return true;
        }

        public bool ValidateMoleFractions(out string error)
        {
            error = null!;
            if (!_components.Any()) { error = "No components"; return false; }

            double sum = _components.Where(c => c.MolarFraction.IsDefined)
                                    .Sum(c => c.MolarFraction.Value.GetValue(PercentageUnits.Percentage) / 100.0);

            if (sum == 0) { error = "No mole fractions defined"; return false; }
            if (Math.Abs(sum - 1.0) > 1e-6) { error = $"Mole fractions sum {sum * 100:F2}% (expected 100%)"; return false; }
            return true;
        }

        public bool IsValid
        {
            get
            {
                if (!_components.Any()) return false;

                bool allMolarDefined = _components.All(c => c.MolarFraction.IsDefined);
                bool allMassDefined = _components.All(c => c.MassFraction.IsDefined);

                if (!allMolarDefined && !allMassDefined) return false;

                if (allMassDefined && !ValidateMassFractions(out _)) return false;
                if (allMolarDefined && !ValidateMoleFractions(out _)) return false;

                return true;
            }
        }

        public void ClearComposition()
        {
            foreach (var item in _components)
            {
                item.MolarFraction.ResetProcedence();
                item.MassFraction.ResetProcedence();
                item.MolarFlow.ResetProcedence();
                item.MassFlow.ResetProcedence();
            }
        }
    }
    public class CompositionOrchestrator5
    {
        private readonly IReadOnlyList<ComponentFacade> _components;


        public List<ComponentFacade> Components => _components.ToList();

        public event Action OnCompositionChanged = null!;

        public void CompositionChanged()
        {
            // Al cambiar una variable de componente, validamos y notificamos si la composición es válida
            OnCompositionChanged?.Invoke();
        }
        public CompositionOrchestrator5(
            IReadOnlyList<ComponentFacade> components)
        {
            _components = components ?? throw new ArgumentNullException(nameof(components));

            // ✅ Sin suscripción aquí: los componentes aún no están listos
        }


        public bool ValidateMassFractions(out string error)
        {
            error = null!;

            if (!_components.Any())
            {
                error = "No components";
                return false;
            }

            double sum = _components
                .Where(c => c.MassFraction.IsDefined)
                .Sum(c => c.MassFraction.Value.GetValue(PercentageUnits.Percentage) / 100.0);

            // ✅ CORRECCIÓN: Validar explícitamente que hay algo definido
            if (sum == 0)
            {
                error = "No mass fractions defined";
                return false;
            }

            // ✅ CORRECCIÓN: Validar que suma 100% (sin proteger el error con sum > 0)
            if (Math.Abs(sum - 1.0) > 1e-6)
            {
                error = $"Mass fractions sum {sum * 100:F2}% (expected 100%)";
                return false;
            }

            return true;
        }

        public bool ValidateMoleFractions(out string error)
        {
            error = null!;

            if (!_components.Any())
            {
                error = "No components";
                return false;
            }

            double sum = _components
                .Where(c => c.MolarFraction.IsDefined)
                .Sum(c => c.MolarFraction.Value.GetValue(PercentageUnits.Percentage) / 100.0);

            // ✅ CORRECCIÓN: Validar explícitamente que hay algo definido
            if (sum == 0)
            {
                error = "No mole fractions defined";
                return false;
            }

            // ✅ CORRECCIÓN: Validar que suma 100% (sin proteger el error con sum > 0)
            if (Math.Abs(sum - 1.0) > 1e-6)
            {
                error = $"Mole fractions sum {sum * 100:F2}% (expected 100%)";
                return false;
            }

            return true;
        }


        public bool IsValid
        {
            get
            {
                if (!_components.Any())
                {
                    return false;
                }

                // Validar que al menos un tipo de fracción esté definido en TODOS los componentes
                bool allMolarDefined = _components.All(c => c.MolarFraction.IsDefined);
                bool allMassDefined = _components.All(c => c.MassFraction.IsDefined);

                if (!allMolarDefined && !allMassDefined) return false;

                // Validar que la fracción definida suma 100%
                if (allMassDefined && !ValidateMassFractions(out _)) return false;
                if (allMolarDefined && !ValidateMoleFractions(out _)) return false;

                return true;
            }
        }
        public void ClearComposition()
        {
            foreach (var item in _components)
            {
                item.MolarFraction.ResetProcedence();
                item.MassFraction.ResetProcedence();
                item.MolarFlow.ResetProcedence();
                item.MassFlow.ResetProcedence();

            }


        }


    }

    //public class CompositionOrchestrator2 : IProcessVariableOwner
    //{
    //    private readonly IReadOnlyList<ComponentFacade> _components;


    //    public List<ComponentFacade> Components => _components.ToList();
    //    public bool IsDefined => DataProcedence != VariableDataProcedence.Undefined;
    //    public bool IsSpecToSolver => DataProcedence == VariableDataProcedence.UserInput || DataProcedence == VariableDataProcedence.StreamCalculated;
    //    public bool IsSpecToEquilibrium => DataProcedence == VariableDataProcedence.UserInput ||
    //        DataProcedence == VariableDataProcedence.EquipmentCalculation ||
    //        DataProcedence == VariableDataProcedence.SolverAdjustment;
    //    public VariableDataProcedence DataProcedence { get; private set; }
    //    public event Action? OnCompositionChanged;


    //    private bool _isProcessing = false;
    //    public HashSet<IProcessVariable> Variables { get; } = new();
    //    public void AddVariable(IProcessVariable variable)
    //    {
    //        if (!Variables.Contains(variable) && variable.DataProcedence == VariableDataProcedence.StreamCalculated)
    //        {
    //            Variables.Add(variable);
    //        }
    //    }
    //    public void RemoveVariables(VariableDataProcedence _procedence)
    //    {
    //        var ToRemovals = Variables.Where(x => x.DataProcedence == VariableDataProcedence.StreamCalculated);
    //        foreach (var v in ToRemovals)
    //        {
    //            v.Clear(_procedence);
    //            Variables.Remove(v);
    //        }
    //        //Variables.Clear();
    //    }
    //    public CompositionOrchestrator2(
    //        IReadOnlyList<ComponentFacade> components)
    //    {
    //        _components = components ?? throw new ArgumentNullException(nameof(components));

    //        // ✅ Sin suscripción aquí: los componentes aún no están listos
    //    }

    //    /// <summary>
    //    /// Suscribe manualmente los componentes después de que existen.
    //    /// Debe llamarse UNA VEZ después de crear componentes y orchestrator.
    //    /// </summary>


    //    public void OnComponentVariableChanged(IProcessVariable sender)
    //    {
    //        if (_isProcessing) return;
    //        _isProcessing = true;

    //        try
    //        {
    //            // 1. Limpiar variables calculadas previamente
    //            RemoveVariables(VariableDataProcedence.StreamCalculated);
    //            DataProcedence = VariableDataProcedence.Undefined;

    //            // 2. Validar estado actual
    //            bool validMass = ValidateMassFractions(out _);
    //            bool validMolar = ValidateMoleFractions(out _);
    //            bool validComposition = validMass || validMolar;
    //            bool hasDefinedFractions = _components.All(c => c.MassFraction.IsDefined || c.MolarFraction.IsDefined);

    //            // 3. CASO 1: Composición válida → Sincronizar fracción complementaria y notificar
    //            if (validComposition && hasDefinedFractions)
    //            {
    //                DataProcedence = sender.DataProcedence;
    //                SyncComplementaryFraction(validMass, validMolar, DataProcedence);




    //            }
    //            OnCompositionChanged?.Invoke();
    //        }
    //        finally
    //        {
    //            _isProcessing = false;
    //        }
    //    }
    //    public void OnComponentFlowChanged(IProcessVariable sender)
    //    {
    //        if (_isProcessing) return;
    //        _isProcessing = true;

    //        try
    //        {
    //            // 1. Limpiar variables calculadas previamente
    //            RemoveVariables(VariableDataProcedence.StreamCalculated);

    //            // 2. Validar: ¿TODOS los componentes tienen flujo definido?
    //            bool allMassFlowsDefined = _components.All(c =>
    //                c.MassFlow.IsDefined);

    //            bool allMolarFlowsDefined = _components.All(c =>
    //                c.MolarFlow.IsDefined);

    //            bool hasValidFlows = allMassFlowsDefined || allMolarFlowsDefined;

    //            if (hasValidFlows)
    //            {
    //                // 3. Calcular fracciones desde flujos
    //                CalculateFractionsFromFlows(allMassFlowsDefined, allMolarFlowsDefined, sender.DataProcedence);

    //                // 4. Definir autoridad
    //                DataProcedence = sender.DataProcedence;

    //                // 5. Notificar convergencia

    //            }
    //            OnCompositionChanged?.Invoke();
    //        }
    //        finally
    //        {
    //            _isProcessing = false;
    //        }
    //    }

    //    /// <summary>
    //    /// Calcula MassFraction y MolarFraction a partir de los flujos de componente.
    //    /// </summary>
    //    private void CalculateFractionsFromFlows(bool useMassFlows, bool useMolarFlows, VariableDataProcedence _procedence)
    //    {
    //        if (useMassFlows)
    //        {
    //            // Calcular fracciones másicas desde flujos másicos
    //            double totalMassFlow = _components.Sum(c =>
    //                c.MassFlow.Value.GetValue(MassFlowUnits.Kg_sg));

    //            if (totalMassFlow > 0)
    //            {
    //                foreach (var comp in _components)
    //                {
    //                    double massFlow = comp.MassFlow.Value.GetValue(MassFlowUnits.Kg_sg);
    //                    double massFrac = massFlow / totalMassFlow;

    //                    if (comp.MassFraction.DataProcedence != VariableDataProcedence.UserInput)
    //                    {
    //                        comp.MassFraction.SetValue(
    //                            new Percentage(massFrac * 100, PercentageUnits.Percentage),
    //                            _procedence);

    //                    }
    //                }

    //                // Calcular fracciones molares complementarias
    //                CalculateMolarFractionsFromMass(_procedence);
    //            }
    //        }
    //        else if (useMolarFlows)
    //        {
    //            // Calcular fracciones molares desde flujos molares
    //            double totalMolarFlow = _components.Sum(c =>
    //                c.MolarFlow.Value.GetValue(MolarFlowUnits.Kgmol_sg));

    //            if (totalMolarFlow > 0)
    //            {
    //                foreach (var comp in _components)
    //                {
    //                    double molarFlow = comp.MolarFlow.Value.GetValue(MolarFlowUnits.Kgmol_sg);
    //                    double moleFrac = molarFlow / totalMolarFlow;

    //                    if (comp.MolarFraction.DataProcedence != VariableDataProcedence.UserInput)
    //                    {
    //                        comp.MolarFraction.SetValue(
    //                            new Percentage(moleFrac * 100, PercentageUnits.Percentage),
    //                            _procedence);

    //                    }
    //                }

    //                // Calcular fracciones másicas complementarias
    //                CalculateMassFractionsFromMolar(_procedence);
    //            }
    //        }
    //    }

    //    /// <summary>
    //    /// Calcula fracciones molares a partir de las másicas (helper).
    //    /// </summary>
    //    private void CalculateMolarFractionsFromMass(VariableDataProcedence _procedence)
    //    {
    //        double sumMolarBase = _components.Sum(c =>
    //            (c.MassFraction.Value.GetValue(PercentageUnits.Percentage) / 100.0) / c.MolecularWeight);

    //        if (sumMolarBase > 0)
    //        {
    //            foreach (var comp in _components)
    //            {
    //                if (comp.MolarFraction.DataProcedence != VariableDataProcedence.UserInput)
    //                {
    //                    double massFrac = comp.MassFraction.Value.GetValue(PercentageUnits.Percentage) / 100.0;
    //                    double moleFrac = (massFrac / comp.MolecularWeight) / sumMolarBase;

    //                    comp.MolarFraction.SetValue(
    //                        new Percentage(moleFrac * 100, PercentageUnits.Percentage),
    //                        _procedence);

    //                }
    //            }
    //        }
    //    }

    //    /// <summary>
    //    /// Calcula fracciones másicas a partir de las molares (helper).
    //    /// </summary>
    //    private void CalculateMassFractionsFromMolar(VariableDataProcedence _procedence)
    //    {
    //        double sumMassBase = _components.Sum(c =>
    //            (c.MolarFraction.Value.GetValue(PercentageUnits.Percentage) / 100.0) * c.MolecularWeight);

    //        if (sumMassBase > 0)
    //        {
    //            foreach (var comp in _components)
    //            {
    //                if (comp.MassFraction.DataProcedence != VariableDataProcedence.UserInput)
    //                {
    //                    double moleFrac = comp.MolarFraction.Value.GetValue(PercentageUnits.Percentage) / 100.0;
    //                    double massFrac = (moleFrac * comp.MolecularWeight) / sumMassBase;

    //                    comp.MassFraction.SetValue(
    //                        new Percentage(massFrac * 100, PercentageUnits.Percentage),
    //                        _procedence);

    //                }
    //            }
    //        }
    //    }

    //    /// <summary>
    //    /// Sincroniza únicamente el tipo de fracción complementario (Masa ↔ Molar).
    //    /// </summary>
    //    private void SyncComplementaryFraction(bool validMass, bool validMolar, VariableDataProcedence _procedence)
    //    {
    //        if (validMass)
    //        {
    //            double sumMolarBase = _components.Sum(c =>
    //                (c.MassFraction.Value.GetValue(PercentageUnits.Percentage) / 100.0) / c.MolecularWeight);

    //            if (sumMolarBase > 0)
    //            {
    //                foreach (var comp in _components)
    //                {
    //                    if (comp.MolarFraction.DataProcedence != VariableDataProcedence.UserInput)
    //                    {
    //                        double massFrac = comp.MassFraction.Value.GetValue(PercentageUnits.Percentage) / 100.0;
    //                        double moleFrac = (massFrac / comp.MolecularWeight) / sumMolarBase;

    //                        comp.MolarFraction.SetValue(
    //                            new Percentage(moleFrac * 100, PercentageUnits.Percentage),
    //                            VariableDataProcedence.StreamCalculated);

    //                    }
    //                }
    //            }
    //        }
    //        else if (validMolar)
    //        {
    //            double sumMassBase = _components.Sum(c =>
    //                (c.MolarFraction.Value.GetValue(PercentageUnits.Percentage) / 100.0) * c.MolecularWeight);

    //            if (sumMassBase > 0)
    //            {
    //                foreach (var comp in _components)
    //                {
    //                    if (comp.MassFraction.DataProcedence != VariableDataProcedence.UserInput)
    //                    {
    //                        double moleFrac = comp.MolarFraction.Value.GetValue(PercentageUnits.Percentage) / 100.0;
    //                        double massFrac = (moleFrac * comp.MolecularWeight) / sumMassBase;

    //                        comp.MassFraction.SetValue(
    //                            new Percentage(massFrac * 100, PercentageUnits.Percentage),
    //                             VariableDataProcedence.StreamCalculated);

    //                    }
    //                }
    //            }
    //        }
    //    }




    //    public bool ValidateMassFractions(out string error)
    //    {
    //        error = null!;

    //        if (!_components.Any())
    //        {
    //            error = "No components";
    //            return false;
    //        }

    //        double sum = _components
    //            .Where(c => c.MassFraction.IsDefined)
    //            .Sum(c => c.MassFraction.Value.GetValue(PercentageUnits.Percentage) / 100.0);

    //        // ✅ CORRECCIÓN: Validar explícitamente que hay algo definido
    //        if (sum == 0)
    //        {
    //            error = "No mass fractions defined";
    //            return false;
    //        }

    //        // ✅ CORRECCIÓN: Validar que suma 100% (sin proteger el error con sum > 0)
    //        if (Math.Abs(sum - 1.0) > 1e-6)
    //        {
    //            error = $"Mass fractions sum {sum * 100:F2}% (expected 100%)";
    //            return false;
    //        }

    //        return true;
    //    }

    //    public bool ValidateMoleFractions(out string error)
    //    {
    //        error = null!;

    //        if (!_components.Any())
    //        {
    //            error = "No components";
    //            return false;
    //        }

    //        double sum = _components
    //            .Where(c => c.MolarFraction.IsDefined)
    //            .Sum(c => c.MolarFraction.Value.GetValue(PercentageUnits.Percentage) / 100.0);

    //        // ✅ CORRECCIÓN: Validar explícitamente que hay algo definido
    //        if (sum == 0)
    //        {
    //            error = "No mole fractions defined";
    //            return false;
    //        }

    //        // ✅ CORRECCIÓN: Validar que suma 100% (sin proteger el error con sum > 0)
    //        if (Math.Abs(sum - 1.0) > 1e-6)
    //        {
    //            error = $"Mole fractions sum {sum * 100:F2}% (expected 100%)";
    //            return false;
    //        }

    //        return true;
    //    }
    //    /// <summary>
    //    /// Indica si la composición es válida: hay componentes, al menos un tipo de fracción definido en todos, y suma 100%.
    //    /// </summary>
    //    public bool IsValid
    //    {
    //        get
    //        {
    //            if (!_components.Any()) return false;

    //            // Validar que al menos un tipo de fracción esté definido en TODOS los componentes
    //            bool allMolarDefined = _components.All(c => c.MolarFraction.IsDefined);
    //            bool allMassDefined = _components.All(c => c.MassFraction.IsDefined);

    //            if (!allMolarDefined && !allMassDefined) return false;

    //            // Validar que la fracción definida suma 100%
    //            if (allMassDefined && !ValidateMassFractions(out _)) return false;
    //            if (allMolarDefined && !ValidateMoleFractions(out _)) return false;

    //            return true;
    //        }
    //    }



    //}
    /// </summary>
    //public class CompositionOrchestrator2
    //{
    //    private readonly IReadOnlyList<ComponentFacade> _components;
    //    private readonly ProcessVariable<MassFlow> _totalMassFlow;
    //    private readonly ProcessVariable<MolarFlow> _totalMolarFlow;

    //    public List<ComponentFacade> Components => _components.ToList(); // Exposición segura, sin permitir modificación externa
    //    public VariableState State => CalculateGlobalState();

    //    /// <summary>
    //    /// Owner global: quién modificó por última vez la composición exitosamente.
    //    /// </summary>
    //    public VariableOwner Owner { get; private set; } = VariableOwner.Default;
    //    public event Action? OnCompositionChanged;
    //    public CompositionOrchestrator2(
    //        IReadOnlyList<ComponentFacade> components,
    //        ProcessVariable<MassFlow> totalMassFlow,
    //        ProcessVariable<MolarFlow> totalMolarFlow)
    //    {
    //        _components = components ?? throw new ArgumentNullException(nameof(components));
    //        _totalMassFlow = totalMassFlow ?? throw new ArgumentNullException(nameof(totalMassFlow));
    //        _totalMolarFlow = totalMolarFlow ?? throw new ArgumentNullException(nameof(totalMolarFlow));
    //    }

    //    // ─────────────────────────────────────────────────────────
    //    // 🔹 CÁLCULO DEL ESTADO GLOBAL
    //    // ─────────────────────────────────────────────────────────

    //    private VariableState CalculateGlobalState()
    //    {
    //        if (!_components.Any()) return VariableState.Undefined;

    //        // 1. Validar consistencia primero
    //        if (!ValidateMassFractions(out _))
    //            return VariableState.Undefined;

    //        // 2. Verificar si todas las fracciones están definidas
    //        bool allDefined = _components.All(c => c.MassFraction.State != VariableState.Undefined);
    //        if (!allDefined) return VariableState.Undefined;

    //        // 3. Determinar origen: ¿UI o Calculated?
    //        bool allUserSpecified = _components.All(c => c.MassFraction.State == VariableState.UserSpecified);
    //        bool allCalculated = _components.All(c => c.MassFraction.State == VariableState.Calculated);

    //        if (allUserSpecified) return VariableState.UserSpecified;
    //        if (allCalculated) return VariableState.Calculated;

    //        // Mezcla: si hay al menos una UI, priorizamos UserSpecified
    //        if (_components.Any(c => c.MassFraction.State == VariableState.UserSpecified))
    //            return VariableState.UserSpecified;

    //        return VariableState.Calculated;
    //    }

    //    // ─────────────────────────────────────────────────────────
    //    // 🔹 VALIDACIÓN TIPADA (Internamente usa Amount.Value, pero API limpia)
    //    // ─────────────────────────────────────────────────────────

    //    /// <summary>
    //    /// Valida que las fracciones másicas sumen 100% ± 1e-6.
    //    /// Retorna false con error descriptivo si falla.
    //    /// </summary>
    //    public bool ValidateMassFractions(out string error)
    //    {
    //        error = null!;
    //        if (!_components.Any()) { error = "No components registered"; return false; }

    //        double sum = 0;
    //        foreach (var comp in _components)
    //        {
    //            // Amount.Value ya es Percentage; extraemos el valor en escala 0-100 y normalizamos
    //            double frac = comp.MassFraction.Value.GetValue(PercentageUnits.Percentage) / 100.0;

    //            if (frac < -1e-9) { error = $"Negative mass fraction for component '{comp.Name}'"; return false; }
    //            sum += frac;
    //        }

    //        if (Math.Abs(sum - 1.0) > 1e-6)
    //        {
    //            error = $"Mass fractions sum to {sum * 100:F4}%, expected 100% ±0.0001%";
    //            return false;
    //        }
    //        return true;
    //    }

    //    /// <summary>
    //    /// Valida que las fracciones molares sumen 100% ± 1e-6.
    //    /// </summary>
    //    public bool ValidateMoleFractions(out string error)
    //    {
    //        error = null!;
    //        if (!_components.Any()) { error = "No components registered"; return false; }

    //        double sum = 0;
    //        foreach (var comp in _components)
    //        {
    //            double frac = comp.MolarFraction.Value.GetValue(PercentageUnits.Percentage) / 100.0;

    //            if (frac < -1e-9) { error = $"Negative mole fraction for component '{comp.Name}'"; return false; }
    //            sum += frac;
    //        }

    //        if (Math.Abs(sum - 1.0) > 1e-6)
    //        {
    //            error = $"Mole fractions sum to {sum * 100:F4}%, expected 100% ±0.0001%";
    //            return false;
    //        }
    //        return true;
    //    }

    //    // ─────────────────────────────────────────────────────────
    //    // 🔹 REGLA DE ORO: ¿Puede este Owner modificar esta variable?
    //    // ─────────────────────────────────────────────────────────

    //    private bool CanModify<T>(ProcessVariable<T> variable, VariableOwner caller) where T : Amount
    //    {
    //        return !(variable.Owner == VariableOwner.UI && caller != VariableOwner.UI);
    //    }

    //    // ─────────────────────────────────────────────────────────
    //    // 🔹 OPERACIONES TIPADAS (void SetValue, sin out reason)
    //    // ─────────────────────────────────────────────────────────

    //    /// <summary>
    //    /// Recalcula flujos de componentes a partir del Flujo Total y las Fracciones.
    //    /// Respeta Owner: si una fracción es UI, el flujo derivado hereda esa protección.
    //    /// Retorna true si todo fue exitoso, false con error en 'out errorMessage'.
    //    /// </summary>
    //    public bool RecalculateComponentFlows(VariableOwner caller, out string errorMessage)
    //    {
    //        errorMessage = null!;

    //        if (!ValidateMassFractions(out string validationError))
    //        {
    //            errorMessage = $"Invalid composition: {validationError}";
    //            return false;
    //        }

    //        // Leer flujo total como Amount tipado
    //        MassFlow totalMassAmount = _totalMassFlow.Value;
    //        double totalMass = totalMassAmount.GetValue(MassFlowUnits.Kg_sg);
    //        double totalMoles = 0;

    //        foreach (var comp in _components)
    //        {
    //            // Leer fracción másica como Amount tipado
    //            Percentage massFracAmount = comp.MassFraction.Value;
    //            double massFrac = massFracAmount.GetValue(PercentageUnits.Percentage) / 100.0;
    //            double compMassFlow = totalMass * massFrac;

    //            // ESCRITURA TIPADA con verificación de Owner
    //            if (CanModify(comp.MassFraction, caller))
    //            {
    //                // Crear Amount tipado para el flujo másico del componente
    //                var compMassFlowAmount = new MassFlow(compMassFlow, MassFlowUnits.Kg_sg);
    //                comp.MassFlow.SetValue(compMassFlowAmount, caller);

    //                // Conversión Mass → Molar usando MW del componente
    //                double mw = comp.MolecularWeight;
    //                if (mw <= 0)
    //                {
    //                    errorMessage = $"Invalid MolecularWeight for {comp.Name}: {mw}";
    //                    return false;
    //                }

    //                double compMolarFlow = compMassFlow / mw;
    //                var compMolarFlowAmount = new MolarFlow(compMolarFlow, MolarFlowUnits.Kgmol_sg);
    //                comp.MolarFlow.SetValue(compMolarFlowAmount, caller);

    //                totalMoles += compMolarFlow;
    //            }
    //        }

    //        // Actualizar total molar de la corriente
    //        var totalMolarAmount = new MolarFlow(totalMoles, MolarFlowUnits.Kgmol_sg);
    //        _totalMolarFlow.SetValue(totalMolarAmount, caller);

    //        // Recalcular fracciones molares si es necesario
    //        if (totalMoles > 0)
    //        {
    //            foreach (var comp in _components)
    //            {
    //                if (CanModify(comp.MolarFraction, caller))
    //                {
    //                    MolarFlow compMolarFlowAmount = comp.MolarFlow.Value;
    //                    double compMolarFlow = compMolarFlowAmount.GetValue(MolarFlowUnits.Kgmol_sg);
    //                    double moleFrac = compMolarFlow / totalMoles;

    //                    var moleFracAmount = new Percentage(moleFrac * 100, PercentageUnits.Percentage);
    //                    comp.MolarFraction.SetValue(moleFracAmount, caller);
    //                }
    //            }
    //        }

    //        // Actualizar Owner global solo si fue exitoso
    //        Owner = caller;
    //        return true;
    //    }

    //    /// <summary>
    //    /// Aplica fracciones molares desde una fuente externa (Flash/Solver).
    //    /// Bloquea si alguna fracción está fijada por UI.
    //    /// Valida que Σ = 100% antes de aplicar.
    //    /// Retorna true si todo fue exitoso.
    //    /// </summary>
    //    public bool ApplyMoleFractions(
    //        IEnumerable<(ComponentFacade Component, Percentage MoleFractionAmount)> updates,
    //        VariableOwner caller,
    //        out string errorMessage)
    //    {
    //        errorMessage = null!;

    //        var updateList = updates?.ToList() ?? new List<(ComponentFacade, Percentage)>();
    //        if (!updateList.Any()) return true; // No-op exitoso

    //        // 1. Validar que no intentamos sobrescribir valores de UI
    //        foreach (var (comp, _) in updateList)
    //        {
    //            if (!CanModify(comp.MolarFraction, caller))
    //            {
    //                errorMessage = $"Cannot modify {comp.Name}.MolarFraction: locked by UI";
    //                return false;
    //            }
    //        }

    //        // 2. Validar suma = 100% (en los valores de entrada, escala Percentage)
    //        double sum = updateList.Sum(u => u.MoleFractionAmount.GetValue(PercentageUnits.Percentage));
    //        if (Math.Abs(sum - 100.0) > 0.01) // Tolerancia 0.01%
    //        {
    //            errorMessage = $"Mole fractions sum to {sum:F4}%, expected 100% ±0.01%";
    //            return false;
    //        }

    //        // 3. Aplicar actualizaciones (tipadas)
    //        foreach (var (comp, moleFracAmount) in updateList)
    //        {
    //            comp.MolarFraction.SetValue(moleFracAmount, caller);
    //        }

    //        // Actualizar Owner global
    //        Owner = caller;

    //        // 4. Si hay flujo total definido, recalcular flujos de componentes
    //        if (_totalMassFlow.State != VariableState.Undefined)
    //        {
    //            return RecalculateComponentFlows(caller, out errorMessage);
    //        }

    //        return true;
    //    }

    //    // ─────────────────────────────────────────────────────────
    //    // 🔹 LECTURAS PARA CONSUMO EXTERNO (Flash/Solver)
    //    // ─────────────────────────────────────────────────────────

    //    /// <summary>
    //    /// Obtiene fracciones molares en escala decimal (0-1) para consumo externo.
    //    /// Retorna lista tipada con referencia al componente.
    //    /// </summary>
    //    public List<(ComponentFacade Component, double MoleFractionDecimal)> GetMoleFractionsForFlash()
    //    {
    //        return _components.Select(comp => (
    //            Component: comp,
    //            MoleFractionDecimal: comp.MolarFraction.Value.GetValue(PercentageUnits.Percentage) / 100.0
    //        )).ToList();
    //    }

    //    /// <summary>
    //    /// Obtiene fracciones másicas en escala decimal (0-1).
    //    /// </summary>
    //    public List<(ComponentFacade Component, double MassFractionDecimal)> GetMassFractions()
    //    {
    //        return _components.Select(comp => (
    //            Component: comp,
    //            MassFractionDecimal: comp.MassFraction.Value.GetValue(PercentageUnits.Percentage) / 100.0
    //        )).ToList();
    //    }

    //    /// <summary>
    //    /// Obtiene pesos moleculares en el mismo orden que GetMoleFractionsForFlash().
    //    /// </summary>
    //    public List<double> GetMolecularWeights()
    //    {
    //        return _components.Select(comp => comp.MolecularWeight).ToList();
    //    }

    //    /// <summary>
    //    /// Obtiene flujos másicos de componentes en kg/s.
    //    /// </summary>
    //    public List<(ComponentFacade Component, double MassFlowKgPerSec)> GetComponentMassFlows()
    //    {
    //        return _components.Select(comp => (
    //            Component: comp,
    //            MassFlowKgPerSec: comp.MassFlow.Value.GetValue(MassFlowUnits.Kg_sg)
    //        )).ToList();
    //    }
    //}
}