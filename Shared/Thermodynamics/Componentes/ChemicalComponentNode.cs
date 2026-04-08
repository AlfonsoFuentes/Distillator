using Shared.PropertiesDtos.Enums;
using Shared.Thermodynamics.Phases;
using Shared.Thermodynamics.PureComponents;

namespace Shared.Thermodynamics.Componentes
{
    public abstract class ChemicalComponentNode : ThermodynamicBase, INode, ICompositionFraction
    {
        public Guid Id => PureComponentData?.Id ?? Guid.Empty;
        public string Name { get; private set; } = string.Empty;
        public PureComponentData PureComponentData { get; private set; } = null!;
        // 👇 NUEVO: Trackear si este componente tiene datos aplicados
        public bool IsInitialized { get; private set; }
        protected ChemicalComponentNode() : base()
        {


        }
        public LiquidPhaseModel LiquidModel { get; protected set; }
        public VaporPhaseModel VaporModel { get; protected set; }

        public void SetComponentData(PureComponentData data, LiquidPhaseModel liquidModel, VaporPhaseModel vaporModel)
        {
            PureComponentData = data;
            CriticalMolarVolume = data.CriticalVolume;
            CriticalTemperature = data.CriticalTemperature;
            CriticalPressure = data.CriticalPressure;
            MolecularWeight = data.MolecularWeight;
            Name = data.Name;

            LiquidModel = liquidModel;
            VaporModel = vaporModel;

            IsInitialized = true; // 👇 Marcamos como "listo"
        }

        // 👇 NUEVO: Limpia solo lo dependiente del método, mantiene datos puros
        public void ClearComponentData()
        {
            LiquidModel = LiquidPhaseModel.None;
            VaporModel = VaporPhaseModel.None;
            IsInitialized = false;
            PureComponentData = null!;
            // 👇 Aquí en el futuro podrías limpiar diccionarios de coeficientes:
            // MethodCoefficients?.Clear();
        }
        public double MassFraction { get; set; } // w_i
        public double MolarFraction { get; set; } // z_i, x_i, y_i

    }
}
