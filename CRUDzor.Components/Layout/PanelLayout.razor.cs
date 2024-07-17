using Microsoft.AspNetCore.Components;

namespace CRUDzor.Components.Layout;

public partial class PanelLayout
{
    public object TitleSection { get; } = new();

    public object ActionSection { get; } = new();

    #region Component Parameters

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    #endregion
}
