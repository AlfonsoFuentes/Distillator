using Shared.PropertiesDtos.Enums;
using Shared.PropertiesDtos.Methods;
using Shared.Thermodynamics.Componentes;
using UnitSystem;

namespace Shared.Thermodynamics.Phases
{
    public abstract class Phase : ThermodynamicBase
    {
        public Phase() : base()
        {

        }
        protected abstract IReadOnlyList<ChemicalComponentNode> ComponentsForPropagation { get; }

        public ThermodynamicMethodFullDto ThermoMethod { get; protected set; } = null!;
        public LiquidPhaseModel LiquidModel => ThermoMethod.LiquidModel;
        public VaporPhaseModel VapourModel => ThermoMethod.VaporModel;
        public void SetThermodynamicMethod(ThermodynamicMethodFullDto _method)
        {
            ThermoMethod = _method;
            SetComponentsProperties(_method);
        }
        public abstract void SetComponentsProperties(ThermodynamicMethodFullDto _method);
        public bool IsMethodDefined => ThermoMethod != null;
        // 👇 NUEVO: Método público para "des-definir" desde el Facade o UI
        public void ClearThermodynamicMethod()
        {
            ClearThermodynamicMethodInternal();
        }

        // 👇 Interno: hace el trabajo sucio de limpiar
        protected virtual void ClearThermodynamicMethodInternal()
        {
            ThermoMethod = null!;
            // Las clases hijas sobrescriben para limpiar sus componentes
        }

        public override void SetTemperature(Temperature? temperature)
        {
            base.SetTemperature(temperature);
            foreach (var component in ComponentsForPropagation)
            {
                component.SetTemperature(temperature);
            }
        }

        public override void SetPressure(Pressure? pressure)
        {
            base.SetPressure(pressure);
            foreach (var component in ComponentsForPropagation)
            {
                component.SetPressure(pressure);
            }
        }

        public override void SetMolarFlow(MolarFlow? molarFlow)
        {
            base.SetMolarFlow(molarFlow);
            foreach (var component in ComponentsForPropagation)
            {
                component.SetMolarFlow(molarFlow);
            }
        }

        public override void SetMassFlow(MassFlow? massFlow)
        {
            base.SetMassFlow(massFlow);
            foreach (var component in ComponentsForPropagation)
            {
                component.SetMassFlow(massFlow);
            }
        }

        public override void SetVolumetricFlow(VolumetricFlow? volumetricFlow)
        {
            base.SetVolumetricFlow(volumetricFlow);
            foreach (var component in ComponentsForPropagation)
            {
                component.SetVolumetricFlow(volumetricFlow);
            }
        }
        public abstract void ClearComponentsProperties();
    }
}
