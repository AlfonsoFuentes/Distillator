using Microsoft.AspNetCore.Components;

namespace Client.Templates.Panels
{
    public partial class ExplorerTreeNode
    {
        [Parameter, EditorRequired] public ExplorerNode Node { get; set; } = default!;
        [Parameter] public string SelectedNodeId { get; set; } = string.Empty;
        [Parameter] public EventCallback<string> OnNodeSelected { get; set; }

        private bool IsSelected => !Node.IsFolder && Node.Id == SelectedNodeId;

        private void HandleClick()
        {
            if (Node.IsFolder)
            {
                Node.IsExpanded = !Node.IsExpanded;
            }
            else
            {
                OnNodeSelected.InvokeAsync(Node.Id);
            }
        }
    }
}
