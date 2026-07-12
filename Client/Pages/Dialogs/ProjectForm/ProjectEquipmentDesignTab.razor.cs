using Microsoft.AspNetCore.Components;

namespace Client.Pages.Dialogs.ProjectForm;

public partial class ProjectEquipmentDesignTab
{
    [Parameter] public string Standard { get; set; } = "API";
    [Parameter] public EventCallback<string> StandardChanged { get; set; }
    [Parameter] public string RatingBasis { get; set; } = "normal";
    [Parameter] public EventCallback<string> RatingBasisChanged { get; set; }
}
