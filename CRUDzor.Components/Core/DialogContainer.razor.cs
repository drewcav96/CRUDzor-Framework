using Microsoft.AspNetCore.Components;

namespace CRUDzor.Components.Core;

public partial class DialogContainer
{
    #region Component Parameters

    [Parameter]
    public Type Type { get; set; } = default!;

    [Parameter(CaptureUnmatchedValues = true)]
    public IDictionary<string, object>? Parameters { get; set; }

    #endregion
}
