using Microsoft.AspNetCore.Components;

namespace CRUDzor.Components.Layout;

public partial class TitleSection
{
    #region Component Parameters

    [CascadingParameter]
    internal ISectionHeaderLayout Layout { get; private set; } = default!;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    #endregion
}
