using Shared.PipingRoutes;
using Shared.UnitOperations.Pipes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.ProcessFlowDiagram.Pipes
{
    public class PipeVisualElement : VisualElementBase
    {
        public bool ShowTechnicalLabel { get; set; } = true;
        public override string Prefix => "PIPE";

        // 1. ANCLAJES (Datos de dibujo)
        public Guid SourceElementId { get; set; } = Guid.Empty;
        public string SourcePortName { get; set; } = string.Empty;

        public Guid TargetElementId { get; set; } = Guid.Empty;
        public string TargetPortName { get; set; } = string.Empty;
        public List<CanvasPoint> CalculatedRoute { get; set; } = new();

        // 👇 Helper: Obtiene los segmentos de la tubería
        public IEnumerable<(CanvasPoint Start, CanvasPoint End)> GetSegments()
        {
            for (int i = 0; i < CalculatedRoute.Count - 1; i++)
            {
                yield return (CalculatedRoute[i], CalculatedRoute[i + 1]);
            }
        }

        // =========================================================
        // 2. PROPIEDADES DE NAVEGACIÓN EN TIEMPO DE EJECUCIÓN 
        // (No se guardan en la DB, se "hidratan" al cargar el plano)
        // =========================================================

        // Usamos JsonIgnore (si usas System.Text.Json) para que no intente guardar el objeto entero
        [System.Text.Json.Serialization.JsonIgnore]
        public IVisualElement? SourceElement { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public IVisualElement? TargetElement { get; set; }



        // 👇 Restricciones (Marcadas como 'virtual' para que la Bomba pueda decir "yo no roto")
        public override bool AllowFreeRotation => false;
        public override bool AllowFlipHorizontal => false;
        public override bool AllowFlipVertical => false;
        public override bool IsResizable => false; // Los equipos P&ID rara vez cambian de tamaño
        private PipeDesignFacade? PipeFacade => Facade as PipeDesignFacade;

        // 3. IDENTIDAD
        public override string Label
        {
            get
            {
                if (PipeFacade == null) return "Undefined Pipe";
                // Usamos las propiedades exactas del Facade
                return $"{PipeFacade.Diameter}\" - {PipeFacade.FluidName} - {PipeFacade.Material}";
            }
            set
            {
                if (PipeFacade != null) PipeFacade.Name = value;
            }
        }

        // Asegúrate que en VisualElementBase esto sea 'public virtual bool IsMovable'
      

        public PipeVisualElement()
        {
            Width = 0;
            Height = 0;
            // Importante: Instanciar el facade correcto
            Facade = new PipeDesignFacade { Name = "PIPE-000" };
        }
    }
}
