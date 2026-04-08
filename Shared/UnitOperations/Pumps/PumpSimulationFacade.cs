using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.UnitOperations.Pumps
{
    public enum PumpStateType
    {
        Created,
        PartiallyConnected,
        ReadyToCalculate,
        Solved
    }
    public class PumpSimulationFacade : IEquipmentFacade
    {
        // 1. IDENTIDAD
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "P-101";

        // 2. VARIABLES DEL EQUIPO
        public double DeltaPressure { get; set; } = 2.5; // bar
        public double AdiabaticEfficiency { get; set; } = 75.0; // %
        public double PowerConsumed { get; set; } = 0.0; // kW

        // 👇 EL NUEVO ESTADO DE LA MÁQUINA
        public PumpStateType State { get; private set; } = PumpStateType.Created;

        // 3. ESTADO VISUAL (Aplicando tu lógica de colores)
        public string StatusText => State switch
        {
            PumpStateType.Created => "Ready",
            PumpStateType.PartiallyConnected => "Underspecified",
            PumpStateType.ReadyToCalculate => "Ready to Solve",
            PumpStateType.Solved => "Converged",
            _ => "Unknown"
        };

        public string StatusColor => State switch
        {
            PumpStateType.Created => "#CBD5E0",              // Gris
            PumpStateType.PartiallyConnected => "#F6AD55",   // Naranja
            PumpStateType.ReadyToCalculate => "#63B3ED",     // Azul
            PumpStateType.Solved => "#34D399",               // Verde
            _ => "#CBD5E0"
        };

        public Dictionary<string, string> GetQuickViewData()
        {
            var data = new Dictionary<string, string>();

            data.Add("ΔP", $"{DeltaPressure} bar");
            data.Add("Efficiency", $"{AdiabaticEfficiency} %");

            // Solo mostramos la potencia si ya está resuelta, si no, mostramos rayitas
            data.Add("Power", State == PumpStateType.Solved ? $"{Math.Round(PowerConsumed, 2)} kW" : "-- kW");

            return data;
        }

        // 4. TOPOLOGÍA DE SIMULACIÓN
        public IEquipmentFacade? SuctionStream { get; private set; }
        public IEquipmentFacade? DischargeStream { get; private set; }

        public Action? OnTopologyChanged { get; set; } 

        public void AttachConnection(string portName, IEquipmentFacade connectedFacade)
        {
            if (portName == "Suction") SuctionStream = connectedFacade;
            else if (portName == "Discharge") DischargeStream = connectedFacade;

            EvaluateAutoCalculation();
            OnTopologyChanged?.Invoke();
        }

        public void DetachConnection(string portName)
        {
            if (portName == "Suction") SuctionStream = null;
            else if (portName == "Discharge") DischargeStream = null;

            EvaluateAutoCalculation();
            OnTopologyChanged?.Invoke();
        }

        // 👇 LA LÓGICA DE TRANSICIÓN DE ESTADOS QUE PENSASTE
        private void EvaluateAutoCalculation()
        {
            // Caso 1: Tiene ambas conexiones
            if (SuctionStream != null && DischargeStream != null)
            {
                // En un simulador real, aquí validarías:
                // if (SuctionStream.State == StreamStateType.MethodDefined) ...

                State = PumpStateType.ReadyToCalculate;

                // Simulamos que el motor de cálculo hizo su trabajo
                PowerConsumed = 45.5;
                State = PumpStateType.Solved; // Cambia a verde automáticamente
            }
            // Caso 2: Tiene al menos una conexión, pero le falta la otra
            else if (SuctionStream != null || DischargeStream != null)
            {
                State = PumpStateType.PartiallyConnected; // Cambia a naranja
                PowerConsumed = 0;
            }
            // Caso 3: Está huérfana en el lienzo
            else
            {
                State = PumpStateType.Created; // Vuelve a gris
                PowerConsumed = 0;
            }
        }
    }
}
