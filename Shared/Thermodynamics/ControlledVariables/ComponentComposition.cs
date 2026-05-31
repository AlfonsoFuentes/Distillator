using UnitSystem;

namespace Shared.Thermodynamics.ControlledVariables
{
    // 2. DTO para entrada desde UI
    public class ComponentComposition
    {
        // ─────────────────────────────────────────────────────────
        // 🔹 IDENTIFICACIÓN
        // ─────────────────────────────────────────────────────────
        public Guid ComponentId { get; set; }
        public string ComponentName { get; set; } = string.Empty;
        public double MolecularWeight { get; set; }  // Necesario para conversiones

      
        // ─────────────────────────────────────────────────────────
        public MassFlow? MassFlowValue { get; set; } = new MassFlow(0);
        public MolarFlow? MolarFlowValue { get; set; } = new MolarFlow(0);
        public VolumetricFlow? VolumetricFlowValue { get; set; }

        public NewNewVariableAmount<VolumetricFlow> VolumetricFlowSolver { get; set; }
        public NewNewVariableAmount<MassFlow> MassFlowSolver { get; set; }
        public NewNewVariableAmount<MolarFlow> MolarFlowSolver { get; set; }
        public NewNewVariableDouble MolarFractionSolver { get; set; }
        public NewNewVariableDouble MassFractionSolver { get; set; }
        public ComponentComposition()
        {
            MolarFlowSolver = new NewNewVariableAmount<MolarFlow>(new MolarFlow(),
               MolarFlowUnits.Kgmol_hr,
               MolarFlowUnits.Kgmol_hr,
               (v, u) => new MolarFlow(v, u)
           );
            MassFlowSolver = new NewNewVariableAmount<MassFlow>(new MassFlow(),
                MassFlowUnits.Kg_hr,
                MassFlowUnits.Kg_hr,
                (v, u) => new MassFlow(v, u)
            );
            VolumetricFlowSolver = new NewNewVariableAmount<VolumetricFlow>(new VolumetricFlow(),
                VolumetricFlowUnits.m3_hr,
                VolumetricFlowUnits.m3_hr,
                (v, u) => new VolumetricFlow(v, u)
            );
            MolarFractionSolver = new NewNewVariableDouble(0);
            MassFractionSolver = new NewNewVariableDouble(0);
        }

        public ComponentComposition(Guid componentId, string componentName, double molecularWeight)
        {
            ComponentId = componentId;
            ComponentName = componentName;
            MolecularWeight = molecularWeight;
            MolarFlowSolver = new NewNewVariableAmount<MolarFlow>(new MolarFlow(),
                MolarFlowUnits.Kgmol_hr,
                MolarFlowUnits.Kgmol_hr,
                (v, u) => new MolarFlow(v, u)
            );
            MassFlowSolver = new NewNewVariableAmount<MassFlow>(new MassFlow(),
               MassFlowUnits.Kg_hr,
               MassFlowUnits.Kg_hr,
               (v, u) => new MassFlow(v, u)
           );
            VolumetricFlowSolver = new NewNewVariableAmount<VolumetricFlow>(new VolumetricFlow(),
                VolumetricFlowUnits.m3_hr,
                VolumetricFlowUnits.m3_hr,
                (v, u) => new VolumetricFlow(v, u)
            );
            MolarFractionSolver = new NewNewVariableDouble(0);
            MassFractionSolver = new NewNewVariableDouble(0);
        }

        public ComponentComposition Clone()
        {
            var clone = new ComponentComposition(this.ComponentId, this.ComponentName, this.MolecularWeight)
            {
                MassFractionSolver = new NewNewVariableDouble(this.MassFractionSolver.Value),
                //MolarFraction = this.MolarFraction
                MolarFractionSolver = new NewNewVariableDouble(this.MolarFractionSolver.Value)

            };
            if (this.MolarFractionSolver.IsDefinedByUI)
            {
                clone.MolarFractionSolver.SetValueFromUI(this.MolarFractionSolver.Value);

            }
            else if (this.MolarFractionSolver.IsDefinedByStream)
            {
                clone.MolarFractionSolver.SetValueFromStream(this.MolarFractionSolver.Value,"");
            }
            // Después del bloque de MolarFractionSolver en Clone():
            if (this.MassFractionSolver.IsDefinedByUI)
            {
                clone.MassFractionSolver.SetValueFromUI(this.MassFractionSolver.Value);
            }
            else if (this.MassFractionSolver.IsDefinedByStream)
            {
                clone.MassFractionSolver.SetValueFromStream(this.MassFractionSolver.Value,"");
            }


            // 2. Copiar el objeto Amount subyacente (valor + unidad)
            if (this.MolarFlowSolver.Value != null)
            {
                clone.MolarFlowSolver = new NewNewVariableAmount<MolarFlow>(
                    new MolarFlow(this.MolarFlowSolver.Value.Value, this.MolarFlowSolver.Value.UnitName),
                    this.MolarFlowSolver.UnitForUI,
                    this.MolarFlowSolver.UnitForSolver,
                    (v, u) => new MolarFlow(v, u)
                );

            }
            if (this.MassFlowSolver.Value != null)
            {
                clone.MassFlowSolver = new NewNewVariableAmount<MassFlow>(
                    new MassFlow(this.MassFlowSolver.Value.Value, this.MassFlowSolver.Value.UnitName),
                    this.MassFlowSolver.UnitForUI,
                    this.MassFlowSolver.UnitForSolver,
                    (v, u) => new MassFlow(v, u)
                );

            }

            
            if (this.VolumetricFlowSolver.Value != null)
            {
                clone.VolumetricFlowSolver = new NewNewVariableAmount<VolumetricFlow>(
                    new VolumetricFlow(this.VolumetricFlowSolver.Value.Value, this.VolumetricFlowSolver.Value.UnitName),
                    this.VolumetricFlowSolver.UnitForUI,
                    this.VolumetricFlowSolver.UnitForSolver,
                    (v, u) => new VolumetricFlow(v, u)
                );

            }
            return clone;
        }


    }
}
