namespace Shared.ProcessFlowDiagram
{
    public class OffPageConnectorElement : VisualElementBase
    {

        public override EquipmentType Type => EquipmentType.None;
        // 👇 La magia: Sabe a qué otra pantalla y a qué conector exacto debe brincar
        public Guid TargetAreaId { get; set; }
        public Guid TargetConnectorId { get; set; }

        public OffPageConnectorElement()
        {
            Width = 40;
            Height = 40;

            // Tiene un solo puerto genérico. Si es entrada, teletransporta la materia hacia afuera.
            // Si es salida, recibe la materia teletransportada desde otro lado.
            AddPort("Transfer", PortType.Inlet, 0, 20, PortDirection.Left);
        }

        public override string Prefix => throw new NotImplementedException();
    }
}
