using Shared.Thermodynamics.ControlledVariables;

namespace Shared.UnitOperations.Streams
{
    public class StreamComposition
    {
        public ComponentInputType InputType { get; set; } = ComponentInputType.None;
        public List<ComponentComposition> Components { get; set; } = new();

        public Action<INewNewVariable>? OnAddLocalCalculatedVariable { get; set; }

        void AddLocalCalculatedVariable(INewNewVariable variable)
        {
            OnAddLocalCalculatedVariable?.Invoke(variable);
        }

        public StreamComposition() { }

        public StreamComposition(List<ComponentComposition> components)
        {
            Components = components;
        }

        // ✅ AttachEvents() - Mantener suscripciones existentes
        public void AttachEvents()
        {
            foreach (var comp in Components)
            {
                comp.MolarFlowSolver.ExecuteStreamCalculation -= UpdateFractionsFromFlows;
                comp.MolarFlowSolver.ExecuteStreamCalculation += UpdateFractionsFromFlows;

                comp.MolarFractionSolver.ExecuteStreamCalculation -= UpdateMassFractions;
                comp.MolarFractionSolver.ExecuteStreamCalculation += UpdateMassFractions;



                comp.MolarFlowSolver.AddToDefinedList -= AddLocalCalculatedVariable;
                comp.MolarFlowSolver.AddToDefinedList += AddLocalCalculatedVariable;

                comp.MolarFractionSolver.AddToDefinedList -= AddLocalCalculatedVariable;
                comp.MolarFractionSolver.AddToDefinedList += AddLocalCalculatedVariable;
            }
        }

        internal NewNewVariableComposition? ParentVariable { get; set; }
        internal VariableComposition? NewParentVariable { get; set; }
        /// <summary>
        /// Calcula fracciones másicas a partir de fracciones molares y actualiza el estado del ParentVariable.
        /// Se dispara cuando MolarFractionSolver es actualizado por cálculo local o solver.
        /// 🔥 FIX: Acepta fuentes mixtas (UI, Stream, EquipmentSolver, GeneralSolver)
        /// </summary>
        public void UpdateMassFractions()
        {
            // 🔹 1. 🔥 VALIDACIÓN CORREGIDA: Usar IsDefined (cualquier fuente), no flags específicos
            if (Components.Any(c => !c.MolarFractionSolver.IsDefined))
            {
                ParentVariable?.ClearFromStream();
                foreach (var comp in Components)
                {
                    comp.MassFractionSolver.ClearFromStream();
                    comp.MolarFlowSolver.ClearFromStream();
                }
                return;
            }

            // 🔹 2. Calcular suma de fracciones molares para normalización
            var moleSum = Components.Sum(c => c.MolarFractionSolver.Value);

            // 🔹 3. Solo calcular si la suma está cerca de 100% (tolerancia 1%)
            if (moleSum < 99 || moleSum > 101)
                return;

            // 🔹 4. Calcular suma de (z_i * MW_i) para conversión a base másica
            var sum = Components.Sum(c => c.MolarFractionSolver.Value * c.MolecularWeight);
            if (sum <= 0)
                return;

            // 🔹 5. Calcular fracciones másicas: w_i = (z_i * MW_i) / sum * 100
            foreach (var component in Components)
            {
                double wi = (component.MolarFractionSolver.Value * component.MolecularWeight) / sum * 100;
                if (!component.MassFractionSolver.IsDefinedByUI)
                {
                    component.MassFractionSolver.SetValueFromStream(wi, "");
                }
            }

            // 🔹 6. 🔥 MARCAR PADRE CON LÓGICA MIXTA: No exigir que todos tengan la misma fuente
            if (Components.Count > 0)
            {
                // Determinar qué fuentes están presentes (aceptar mezcla)
                bool hasEquipmentSolver = Components.Any(c => c.MolarFractionSolver.IsDefinedByEquipmentSolver);
                bool hasGeneralSolver = Components.Any(c => c.MolarFractionSolver.IsDefinedByGeneralSolver);
                bool hasStream = Components.Any(c => c.MolarFractionSolver.IsDefinedByStream);
                bool hasUI = Components.Any(c => c.MolarFractionSolver.IsDefinedByUI);

                // 🔥 Prioridad: EquipmentSolver > GeneralSolver > Stream > UI
                if (hasEquipmentSolver)
                {
                    // ✅ Si el solver de equipo participó, marcar como EquipmentSolver
                    ParentVariable?.SetValueFromEquipmentSolver(0);
                }
                else if (hasGeneralSolver)
                {
                    ParentVariable?.SetValueFromGeneralSolver(0);
                }
                else if (hasStream)
                {
                    ParentVariable?.SetValueFromStream(ParentVariable.Value, "CompositionCalculated");
                }
                else if (hasUI)
                {
                    ParentVariable?.SetValueFromUI(ParentVariable.Value);
                }
            }
        }

        public void UpdateFractionsFromFlows()
        {
            double total = Components.Sum(c => c.MolarFlowSolver.SolverValue);
            if (total <= 0) return;

            foreach (var comp in Components)
            {
                double zi = comp.MolarFlowSolver.SolverValue / total;
                if (double.IsNaN(zi) || double.IsInfinity(zi)) continue;
                comp.MolarFractionSolver.SetValueFromStream(zi * 100, "");
            }

            CalculateMassFractionsFromMolar();

            // 🔥 Verificar si TODOS los componentes fueron actualizados por el solver (aceptar mezcla)
            if (Components.Count > 0)
            {
                bool allSpecified = Components.All(c => c.MolarFlowSolver.IsDefined);

                if (allSpecified)
                {
                    // Determinar origen predominante para marcar el padre
                    bool hasEquipmentSolver = Components.Any(c => c.MolarFlowSolver.IsDefinedByEquipmentSolver);
                    bool hasGeneralSolver = Components.Any(c => c.MolarFlowSolver.IsDefinedByGeneralSolver);
                    bool hasStream = Components.Any(c => c.MolarFlowSolver.IsDefinedByStream);

                    if (hasEquipmentSolver)
                        ParentVariable?.SetValueFromEquipmentSolver(0);
                    else if (hasGeneralSolver)
                        ParentVariable?.SetValueFromGeneralSolver(0);
                    else if (hasStream)
                        ParentVariable?.SetValueFromStream(ParentVariable.Value, "FlowsCalculated");
                }
                else
                {
                    ParentVariable?.ClearFromStream();
                }
            }
        }

       

        public StreamComposition Clone()
        {
            var clonedComposition = new StreamComposition
            {
                InputType = this.InputType
            };

            foreach (var comp in this.Components)
            {
                clonedComposition.Components.Add(comp.Clone());
            }

            // 🔥 NUEVO: Re-attach eventos y copiar referencia al padre en el clone
            clonedComposition.AttachEvents();
            clonedComposition.ParentVariable = this.ParentVariable;

            return clonedComposition;
        }

        void CalculateMolarFractionsFromMass()
        {
            if (Components.Any(c => !c.MassFractionSolver.IsDefined))
                return;

            var massSum = Components.Sum(c => c.MassFractionSolver.Value);
            if (massSum < 99 || massSum > 101) return;

            var sum = Components.Sum(c => c.MassFractionSolver.Value / c.MolecularWeight);
            if (sum <= 0) return;

            foreach (var component in Components)
            {
                double zi = (component.MassFractionSolver.Value / component.MolecularWeight) / sum * 100;
                component.MolarFractionSolver.SetValueFromStream(zi, "");
            }
        }

        void CalculateMassFractionsFromMolar()
        {
            if (Components.Any(c => !c.MolarFractionSolver.IsDefined))
                return;

            var moleSum = Components.Sum(c => c.MolarFractionSolver.Value);
            if (moleSum < 99 || moleSum > 101) return;

            var sum = Components.Sum(c => c.MolarFractionSolver.Value * c.MolecularWeight);
            if (sum <= 0) return;

            foreach (var component in Components)
            {
                double wi = (component.MolarFractionSolver.Value * component.MolecularWeight) / sum * 100;
                component.MassFractionSolver.SetValueFromStream(wi, "");
            }
        }

        public bool IsEthanolWaterMixture()
        {
            if (Components?.Count != 2) return false;
            var names = Components.Select(c => c.ComponentName.ToLower()).OrderBy(n => n).ToList();
            return names.Contains("ethanol") && names.Contains("water");
        }
        public void CalculateMassMolarFractions()
        {
            // 🔹 Validación de seguridad
            if (Components == null || Components.Count == 0) return;
            if (InputType == ComponentInputType.None) return;

            // 🔹 CASO A: Entrada por Fracción Másica → Calcular Fracción Molar
            if (InputType == ComponentInputType.MassFraction)
            {
                // Validar que TODOS los componentes tengan fracción másica definida
                if (Components.Any(c => !c.MassFractionSolver.IsDefined)) return;

                // Validar que la suma esté cerca de 100%
                var massSum = Components.Sum(c => c.MassFractionSolver.Value);
                if (massSum < 99 || massSum > 101) return;

                // Calcular denominador para conversión: Σ(z_i/MW_i)
                var sum = Components.Sum(c => c.MassFractionSolver.Value / c.MolecularWeight);
                if (sum <= 0) return;

                // Calcular y marcar fracciones molares como definidas por UI
                foreach (var comp in Components)
                {
                    double zi = (comp.MassFractionSolver.Value / comp.MolecularWeight) / sum * 100;
                    // 🔥 CLAVE: SetValueFromUI para marcar como "definido por UI" (no por Stream)
                    comp.MolarFractionSolver.SetValueFromUINotEvents(zi);
                }
            }
            // 🔹 CASO B: Entrada por Fracción Molar → Calcular Fracción Másica
            else if (InputType == ComponentInputType.MolarFraction)
            {
                // Validar que TODOS los componentes tengan fracción molar definida
                if (Components.Any(c => !c.MolarFractionSolver.IsDefined)) return;

                // Validar que la suma esté cerca de 100%
                var moleSum = Components.Sum(c => c.MolarFractionSolver.Value);
                if (moleSum < 99 || moleSum > 101) return;

                // Calcular denominador para conversión: Σ(z_i*MW_i)
                var sum = Components.Sum(c => c.MolarFractionSolver.Value * c.MolecularWeight);
                if (sum <= 0) return;

                // Calcular y marcar fracciones másicas como definidas por UI
                foreach (var comp in Components)
                {
                    double wi = (comp.MolarFractionSolver.Value * comp.MolecularWeight) / sum * 100;
                    comp.MassFractionSolver.SetValueFromUINotEvents(wi);
                }
            }
        }

        /// <summary>
        /// 🔥 MÉTODO DE LIMPIEZA: Limpia ambas fracciones cuando se desdefine la composición
        /// </summary>
        public void ClearMassMolarFractions()
        {
            foreach (var comp in Components)
            {
                // Limpiar ambas fracciones (independientemente de cuál fue la entrada)
                comp.MassFractionSolver.ClearFromUINoEvents();
                comp.MolarFractionSolver.ClearFromUINoEvents();
            }
            InputType = ComponentInputType.None;
        }
    }
    
}
