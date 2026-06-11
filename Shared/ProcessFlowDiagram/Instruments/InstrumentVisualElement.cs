using Shared.UnitOperations;
using Shared.UnitOperations.Instruments;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.ProcessFlowDiagram.Instruments
{
    public enum InstrumentType { Pressure, Temperature, Flow, Level }



    public class InstrumentVisualElement : VisualElementBase
    {
       
        public override EquipmentType Type => EquipmentType.Instrument;
        public override string Prefix => "IT"; // Instrument Transmitter

        // 🚩 Vínculo con la tubería
        public Guid? ParentStreamId { get; set; }

        // Ubicación relativa (0.0 = inicio, 1.0 = fin de la tubería)
        public double LocationPercentage { get; set; } = 0.5;

        public InstrumentType InstrumentType { get; set; } = InstrumentType.Pressure;

        public InstrumentVisualElement()
        {
            Width = 30;
            Height = 30;
            ZIndex = 100; // Siempre por encima de las líneas

            //Facade = new InstrumentSimulationFacade { Id = this.Id, Name = "PT-101" };
        }
    }
}
