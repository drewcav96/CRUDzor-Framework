using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Radzen;

namespace CRUDzor.Components.Core;

public class RadzenComponentBase : ComponentBaseWithCancellation
{
    #region Component Parameters

    [Inject]
    protected NotificationService NotificationService { get; private set; } = default!;

    [Inject]
    protected DialogService DialogService { get; private set; } = default!;

    [Inject]
    protected TooltipService TooltipService { get; private set; } = default!;

    #endregion

    #region Component Methods

    protected async ValueTask OpenDialogAsync<TComponent>(Dictionary<string, object>? parameters = null, DialogOptions? options = null)
        where TComponent : ComponentBase
    {
        options ??= new()
        {
            CloseDialogOnOverlayClick = false,
            ShowTitle = false,
            Width = "1140px",
            CssClass = "dialog-no-padding"
        };

        var dialogParameters = new Dictionary<string, object?>
        {
            { nameof(DialogContainer.Type), typeof(TComponent) },
            { nameof(DialogContainer.Parameters), parameters },
        };

        Logger?.LogTrace("Opening dialog: {Name}", typeof(TComponent).Name);

        await DialogService.OpenAsync<DialogContainer>(string.Empty, dialogParameters, options);
    }

    #endregion
}
