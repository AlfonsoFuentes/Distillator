using Microsoft.AspNetCore.Components;

namespace Client.Templates.Panels
{


    public partial class ExplorerPanel
    {
        [Parameter] public List<ExplorerNode> Nodes { get; set; } = new();
        [Parameter] public string HeaderTitle { get; set; } = "Explorer";
        [Parameter] public string EmptyMessage { get; set; } = "No items available";
        [Parameter] public string EmptyIcon { get; set; } = "";
        [Parameter] public string EmptyTitle { get; set; } = "No Item Selected";
        [Parameter] public string EmptyDescription { get; set; } = "Select an item from the explorer to view details.";
        [Parameter] public string SelectedNodeId { get; set; } = string.Empty;
        [Parameter] public EventCallback<string> SelectedNodeIdChanged { get; set; }
        [Parameter] public bool IsLoading { get; set; }
        [Parameter] public string LoadingText { get; set; } = "Loading...";

        // 🔥 NUEVO: Estado del panel colapsable
        private bool IsTreePanelCollapsed { get; set; } = false;
        private bool _hasAutoCollapsed = false;

        private ExplorerNode? SelectedNode { get; set; }

        protected override void OnParametersSet()
        {
            SelectedNode = FindNodeById(Nodes, SelectedNodeId);
        }

        private void ToggleTreePanel()
        {
            IsTreePanelCollapsed = !IsTreePanelCollapsed;
            _hasAutoCollapsed = false;
            StateHasChanged();
        }

        private void HandleNodeSelected(string nodeId)
        {
            SelectedNodeId = nodeId;
            SelectedNode = FindNodeById(Nodes, nodeId);

            // 🔥 AUTO-COLAPSO: Solo la primera vez que se selecciona un gráfico
            if (!_hasAutoCollapsed && SelectedNode?.Content != null)
            {
                IsTreePanelCollapsed = false;
                _hasAutoCollapsed = false;
            }

            SelectedNodeIdChanged.InvokeAsync(nodeId);
            StateHasChanged();
        }

        private ExplorerNode? FindNodeById(List<ExplorerNode> nodes, string id)
        {
            foreach (var node in nodes)
            {
                if (node.Id == id) return node;
                if (node.IsFolder)
                {
                    var found = FindNodeById(node.Children, id);
                    if (found != null) return found;
                }
            }
            return null;
        }
    }
}
