
using Shared.ProcessFlowDiagram;

//namespace Client.Services.EquipmentManagers
//{
//    public class AreaManagementService
//    {
//        public List<DiagramArea> Areas { get; } = new();
//        public DiagramArea ActiveArea { get; set; } = null!;

//        public void Initialize(string defaultAreaName)
//        {
//            var defaultArea = new DiagramArea { Name = defaultAreaName };
//            Areas.Add(defaultArea);
//            ActiveArea = defaultArea;
//        }

//        public DiagramArea CreateArea(string name)
//        {
//            var newArea = new DiagramArea { Name = name };
//            Areas.Add(newArea);
//            return newArea;
//        }

//        public bool DeleteArea(DiagramArea area)
//        {
//            if (area == null || Areas.Count <= 1) return false;
//            Areas.Remove(area);
//            return true;
//        }

//        public void RenameArea(DiagramArea area, string newName)
//        {
//            if (area != null && !string.IsNullOrWhiteSpace(newName)) area.Name = newName;
//        }

//        public bool ReorderArea(DiagramArea move, DiagramArea target)
//        {
//            int oldIdx = Areas.IndexOf(move); int newIdx = Areas.IndexOf(target);
//            if (oldIdx == -1 || newIdx == -1) return false;
//            Areas.RemoveAt(oldIdx); Areas.Insert(newIdx, move);
//            return true;
//        }
//    }
//}