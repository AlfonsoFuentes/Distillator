
using System.Globalization;
using Shared.ProcessFlowDiagram;

//namespace Client.Services.EquipmentManagers
//{
//    public class CanvasSizeService
//    {
//        public double DiagramWidth { get; private set; } = 3000;
//        public double DiagramHeight { get; private set; } = 2000;
//        public const int GridSize = 20;

//        public double ContainerWidth { get; private set; } = 1920;
//        public double ContainerHeight { get; private set; } = 1080;

//        public double Snap(double val) => Math.Round(val / GridSize) * GridSize;

//        public void SetContainerDimensions(double width, double height)
//        {
//            ContainerWidth = width;
//            ContainerHeight = height;
//        }

//        public void SetDimensions(double width, double height)
//        {
//            DiagramWidth = width;
//            DiagramHeight = height;
//        }

//        public void UpdateDiagramSize(List<IVisualElement> elements, double globalScale)
//        {
//            double scaleFactor = (globalScale > 0.01) ? globalScale : 1.0;

//            if (elements.Count == 0)
//            {
//                DiagramWidth = ContainerWidth / scaleFactor;
//                DiagramHeight = ContainerHeight / scaleFactor;
//                return;
//            }

//            double maxX = elements.Max(e => e.X + e.Width);
//            double maxY = elements.Max(e => e.Y + e.Height);
//            double minX = elements.Min(e => e.X);
//            double minY = elements.Min(e => e.Y);

//            double neededWidth = (maxX - Math.Min(0, minX)) + 300;
//            double neededHeight = (maxY - Math.Min(0, minY)) + 300;

//            DiagramWidth = Math.Max(ContainerWidth / scaleFactor, Snap(neededWidth / scaleFactor));
//            DiagramHeight = Math.Max(ContainerHeight / scaleFactor, Snap(neededHeight / scaleFactor));
//        }

//        public string PaperStyle => string.Create(
//            CultureInfo.InvariantCulture,
//            $"width: {Math.Round(DiagramWidth)}px; height: {Math.Round(DiagramHeight)}px;");
//    }
//}