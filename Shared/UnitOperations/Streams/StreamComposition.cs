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
                component.MassFractionSolver.SetValueFromStream(wi, "");
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

        public void SetInputType(ComponentInputType inputType)
        {
            InputType = inputType;
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

        public void CalculateMolarFractionsFromMass()
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

        public void CalculateMassFractionsFromMolar()
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
    }
    public class StreamComposition2
    {
        public ComponentInputType InputType { get; set; } = ComponentInputType.None;
        public List<ComponentComposition> Components { get; set; } = new();

        public Action<INewNewVariable>? OnAddLocalCalculatedVariable { get; set; }

        void AddLocalCalculatedVariable(INewNewVariable variable)
        {
            OnAddLocalCalculatedVariable?.Invoke(variable);
        }
        public StreamComposition2()
        {
        }

        public StreamComposition2(List<ComponentComposition> components)
        {
            Components = components;

        }
        // ✅ REEMPLAZAR AttachEvents() por esto:
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
        /// <summary>
        /// Calcula fracciones másicas a partir de fracciones molares y actualiza el estado del ParentVariable.
        /// Se dispara cuando MolarFractionSolver es actualizado por cálculo local o solver.
        /// </summary>
        public void UpdateMassFractions()
        {
            // 🔹 1. Validar que todos tengan MolarFraction disponible (fuente para calcular masa)
            if (Components.Any(c => !c.MolarFractionSolver.IsDefinedByEquipmentSolver))
            {
                ParentVariable?.ClearFromEquipmentSolver();
                return;
            }
            if (Components.Any(c => !c.MolarFractionSolver.IsDefinedByGeneralSolver))
            {
                ParentVariable?.ClearFromGeneralSolver();
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

                // 🔥 Actualizar con origen "cálculo local" para propagación correcta
                component.MassFractionSolver.SetValueFromStream(wi, "");
            }

            // 🔹 6. Verificar si la composición completa está especificada por solver
            // Usamos MolarFractionSolver como fuente de verdad (ya que las másicas son derivadas)
            if (Components.Count > 0)
            {
                bool allMolarSpecifiedBySolver = Components.All(c => c.MolarFractionSolver.IsDefinedByGeneralSolver || c.MolarFractionSolver.IsDefinedByEquipmentSolver);

                if (allMolarSpecifiedBySolver)
                {
                    // ✅ Composición completa resuelta → marcar como especificada por solver
                    ParentVariable?.SetValueFromGeneralSolver(0); // valor dummy
                }
                else
                {
                    // ❌ Al menos una fracción molar no fue especificada por solver → desmarcar
                    ParentVariable?.ClearFromGeneralSolver();
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

                if (double.IsNaN(zi) || double.IsInfinity(zi))
                    continue;

                comp.MolarFractionSolver.SetValueFromStream(zi * 100, "");
            }

            CalculateMassFractionsFromMolar();

            // 🔥 NUEVO: Verificar si TODOS los componentes fueron actualizados por el solver
            if (Components.Count > 0)
            {
                bool allSpecified = Components.All(c => c.MolarFlowSolver.IsDefinedByGeneralSolver || c.MolarFlowSolver.IsDefinedByEquipmentSolver);

                if (allSpecified)
                {
                    // ✅ Todos listos → marcar composición como especificada por solver
                    ParentVariable?.SetValueFromGeneralSolver(0); // valor dummy
                }
                else
                {
                    // ❌ Al menos uno fue limpiado → desmarcar composición
                    ParentVariable?.ClearFromGeneralSolver();
                }
            }
        }

        // 🔥 IMPORTANTE: Reiniciar contador cuando se cargue nueva composición
        public void SetInputType(ComponentInputType inputType)
        {
            InputType = inputType;

        }


        public StreamComposition2 Clone()
        {
            // 1. Creamos una nueva instancia del contenedor
            var clonedComposition = new StreamComposition2
            {
                InputType = this.InputType // Respetamos si el usuario metió masa o moles
            };

            // 2. Iteramos sobre los componentes y CLONAMOS CADA UNO individualmente
            foreach (var comp in this.Components)
            {
                // Aquí es donde se llama al método Clone() de ComponentComposition que ya arreglaste
                clonedComposition.Components.Add(comp.Clone());
            }

            return clonedComposition;
        }

        public void CalculateMolarFractionsFromMass()
        {
            // 1. Validar que todos tengan MassFraction
            if (Components.Any(c => !c.MassFractionSolver.IsDefined))
                return;

            // 2. 👇 CLAVE: Calcular suma de fracciones másicas
            var massSum = Components.Sum(c => c.MassFractionSolver.Value);

            // 3. 👇 Solo calcular si la suma está cerca de 1.0 (tolerancia 1%)
            if (massSum < 99 || massSum > 101)
                return;  // 👇 NO calcular si suma ≠ 1.0

            // 4. Calcular suma de (w_i / MW_i) para normalización molar
            var sum = Components.Sum(c => c.MassFractionSolver.Value / c.MolecularWeight);

            if (sum <= 0)
                return;

            // 5. Calcular z_i = (w_i / MW_i) / sum
            foreach (var component in Components)
            {
                component.MolarFractionSolver.SetValueFromStream((component.MassFractionSolver.Value / component.MolecularWeight) / sum * 100, "");
            }
        }

        // ─────────────────────────────────────────────────────────
        // 🔹 CONVERSIÓN: Molar → Mass (CORREGIDO)
        // ─────────────────────────────────────────────────────────
        public void CalculateMassFractionsFromMolar()
        {
            // 1. Validar que todos tengan MolarFraction
            if (Components.Any(c => !c.MolarFractionSolver.IsDefined))
                return;

            // 2. 👇 CLAVE: Calcular suma de fracciones molares
            var moleSum = Components.Sum(c => c.MolarFractionSolver.Value);

            // 3. 👇 Solo calcular si la suma está cerca de 1.0 (tolerancia 1%)
            if (moleSum < 99 || moleSum > 101)
                return;  // 👇 NO calcular si suma ≠ 1.0

            // 4. Calcular suma de (z_i * MW_i) para normalización másica
            var sum = Components.Sum(c => c.MolarFractionSolver.Value * c.MolecularWeight);

            if (sum <= 0)
                return;

            // 5. Calcular w_i = (z_i * MW_i) / sum
            foreach (var component in Components)
            {
                component.MassFractionSolver.SetValueFromStream((component.MolarFractionSolver.Value * component.MolecularWeight) / sum * 100, "");
            }
        }

        // ─────────────────────────────────────────────────────────
        // 🔹 VALIDACIÓN: Suma de fracciones ≈ 1.0
        // ─────────────────────────────────────────────────────────


        /// <summary>
        /// Normaliza las fracciones para que sumen exactamente 1.0
        /// </summary>

        public bool IsEthanolWaterMixture()
        {
            if (Components?.Count != 2) return false;

            var names = Components.Select(c => c.ComponentName.ToLower()).OrderBy(n => n).ToList();
            return names.Contains("ethanol") && names.Contains("water");
            // O por ComponentId si es más confiable:
            // return Components.Any(c => c.ComponentId == EthanolGuid) && 
            //        Components.Any(c => c.ComponentId == WaterGuid);
        }
    }
}
