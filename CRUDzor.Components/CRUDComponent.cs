using CRUDzor.Components.Core;
using CRUDzor.Model;
using CRUDzor.Repository;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Radzen;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace CRUDzor.Components;

public abstract class CRUDComponent<TModel> : RadzenComponentBase
    where TModel : CRUDModel
{
    private IDisposable? _locationChangedHandler;

    protected TModel? EditModel { get; set; }

    protected EditContext? EditContext { get; set; }

    protected abstract Expression<Func<TModel, bool>> QueryExpression { get; }

    protected virtual IDictionary<string, object>? MappingParameters { get; }

    #region Component Parameters

    [Parameter]
    public TModel? Model { get; set; }

    [Parameter]
    public EState LoadState { get; set; } = EState.Read;

    [Parameter]
    public EventCallback<TModel> OnSaved { get; set; }

    [Parameter]
    public EventCallback<TModel> OnDeleted { get; set; }

    [Parameter]
    public EventCallback OnClosed { get; set; }

    #endregion

    #region Component State

    protected EState State { get; set; }

    protected ECapability CreateCapability { get; set; }

    protected ECapability ReadCapability { get; set; }

    protected ECapability UpdateCapability { get; set; }

    protected ECapability DeleteCapability { get; set; }

    protected bool IsLoadingData { get; set; }

    protected bool IsValidating { get; set; }

    protected override bool IsBusy
    {
        get =>
            base.IsBusy || IsLoadingData;
    }

    public enum EState
    {
        Unloaded,
        Create,
        Read,
        Update,
        Delete
    }

    protected enum ECapability
    {
        Restricted,
        Unauthorized,
        Allowed
    }

    #endregion

    #region UI State

    protected bool HasChanges { get; set; }

    [MemberNotNullWhen(true, nameof(EditContext), nameof(EditModel))]
    protected bool HasEditContext
    {
        get =>
            EditContext is not null;
    }

    protected bool ShowCreateButton
    {
        get =>
            CreateCapability is not ECapability.Restricted;
    }

    protected bool DisableCreateButton
    {
        get =>
            CreateCapability is not ECapability.Unauthorized;
    }

    protected bool ShowUpdateButton
    {
        get =>
            UpdateCapability is not ECapability.Restricted;
    }

    protected bool DisableUpdateButton
    {
        get =>
            UpdateCapability is not ECapability.Unauthorized;
    }

    protected bool ShowDeleteButton
    {
        get =>
            DeleteCapability is not ECapability.Restricted;
    }

    protected bool DisableDeleteButton
    {
        get =>
            DeleteCapability is not ECapability.Unauthorized;
    }

    protected bool ShowCancelButton
    {
        get =>
            State is EState.Create or EState.Update;
    }

    protected bool ShowSubmitButton
    {
        get =>
            State is EState.Create or EState.Update;
    }

    #endregion

    #region Component Methods

    protected override async ValueTask InitializeAsync()
    {
        _locationChangedHandler = NavigationManager.RegisterLocationChangingHandler(OnLocationChanging);

        switch (LoadState)
        {
            case EState.Create:
            {
                await CreateAsync();
                break;
            }

            case EState.Read:
            {
                await ReadAsync();
                break;
            }

            case EState.Update:
            {
                await ReadAsync();
                await UpdateAsync();
                break;
            }

            case EState.Delete:
            {
                await ReadAsync();
                await DeleteAsync();
                break;
            }
        }
    }

    protected override void Dispose(bool isDisposing)
    {
        if (!IsDisposed)
        {
            if (isDisposing)
            {
                _locationChangedHandler?.Dispose();

                UnregisterEditContextEvents();
            }
        }

        base.Dispose(isDisposing);
    }

    #endregion

    #region Create State

    internal async ValueTask CreateAsync()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        IsLoadingData = true;
        Logger?.LogTrace("BEGIN: [CRUDComponent] CreateAsync...");

        try
        {
            CreateCapability = await GetCreateCapabilityAsync();

            ThrowIfIncapable(CreateCapability);

            Model = await NewModelAsync();

            State = EState.Create;

            await AfterCreateAsync();
        }
        catch (RestrictedCapabilityException ex)
        {
            State = EState.Unloaded;
            Logger?.LogWarning(ex, "RESTRICTED: [CRUDComponent] CreateAsync.");
            await DialogService.Alert(
                string.Format(TEXT_Restricted, "Create"),
                TITLE_Restricted);
        }
        catch (UnauthorizedCapabilityException ex)
        {
            State = EState.Unloaded;
            Logger?.LogWarning(ex, "UNAUTHORIZED: [CRUDComponent] CreateAsync.");
            await DialogService.Alert(
                string.Format(TEXT_Unauthorized, "Create"),
                TITLE_Unauthorized);
        }
        catch (Exception ex)
        {
            State = EState.Unloaded;
            Logger?.LogError(ex, "EXCEPTION: [CRUDComponent] CreateAsync!");
            await DialogService.Alert(
                ex.Message,
                TITLE_UnexpectedError);
        }
        finally
        {
            IsLoadingData = false;
            Logger?.LogTrace("COMPLETE: [CRUDComponent] CreateAsync.");
        }
    }

    internal virtual async ValueTask<TModel> NewModelAsync()
    {
        var repository = ScopedServices.GetRequiredService<ICreateRepository<TModel>>();

        return await repository.InstantiateAsync(ComponentUnloaded);
    }

    private async ValueTask<ECapability> GetCreateCapabilityAsync()
    {
        if (typeof(TModel) is not ICreateModel)
        {
            return ECapability.Restricted;
        }

        var hasNoConstraints = await EvaluateCreateConstraintsAsync();

        if (!hasNoConstraints)
        {
            return ECapability.Restricted;
        }

        var hasAuthorization = await EvaluateCreateAuthorizationAsync();

        if (!hasAuthorization)
        {
            return ECapability.Unauthorized;
        }

        return ECapability.Allowed;
    }

    // User-Overridden methods

    protected virtual ValueTask AfterCreateAsync()
    {
        return ValueTask.CompletedTask;
    }

    protected virtual ValueTask<bool> EvaluateCreateConstraintsAsync()
    {
        return ValueTask.FromResult(true);
    }

    protected virtual ValueTask<bool> EvaluateCreateAuthorizationAsync()
    {
        return ValueTask.FromResult(true);
    }

    #endregion

    #region Read State

    internal async ValueTask ReadAsync()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        IsLoadingData = true;
        Logger?.LogTrace("BEGIN: [CRUDComponent] ReadAsync...");

        try
        {
            ReadCapability = await GetReadCapabilityAsync();

            ThrowIfIncapable(ReadCapability);

            var repository = ScopedServices.GetRequiredService<IReadRepository<TModel>>();

            Model = await repository.ReadAsync(QueryExpression, MappingParameters, ComponentUnloaded);

            if (Model is null)
            {
                throw new ModelNotFoundException();
            }

            State = EState.Read;

            CreateCapability = await GetCreateCapabilityAsync();
            UpdateCapability = await GetUpdateCapabilityAsync();
            DeleteCapability = await GetDeleteCapabilityAsync();

            await AfterReadAsync();
        }
        catch (RestrictedCapabilityException ex)
        {
            State = EState.Unloaded;
            Logger?.LogWarning(ex, "RESTRICTED: [CRUDComponent] ReadAsync.");
            await DialogService.Alert(
                string.Format(TEXT_Restricted, "Read"),
                TITLE_Restricted);
        }
        catch (UnauthorizedCapabilityException ex)
        {
            State = EState.Unloaded;
            Logger?.LogWarning(ex, "UNAUTHORIZED: [CRUDComponent] ReadAsync.");
            await DialogService.Alert(
                string.Format(TEXT_Unauthorized, "Read"),
                TITLE_Unauthorized);
        }
        catch (ModelNotFoundException ex)
        {
            State = EState.Unloaded;
            Logger?.LogInformation(ex, "NOT FOUND: [CRUDComponent] ReadAsync.");
            await DialogService.Alert(
                TEXT_NotFound,
                TITLE_NotFound);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "EXCEPTION: [CRUDComponent] ReadAsync!");
            await DialogService.Alert(
                ex.Message,
                TITLE_UnexpectedError);
        }
        finally
        {
            IsLoadingData = false;
            Logger?.LogTrace("COMPLETE: [CRUDComponent] ReadAsync.");
        }
    }

    private async ValueTask<ECapability> GetReadCapabilityAsync()
    {
        if (typeof(TModel) is not IReadModel)
        {
            return ECapability.Restricted;
        }

        var hasNoConstraints = await EvaluateReadConstraintsAsync();

        if (!hasNoConstraints)
        {
            return ECapability.Restricted;
        }

        var hasAuthorization = await EvaluateReadAuthorizationAsync();

        if (!hasAuthorization)
        {
            return ECapability.Unauthorized;
        }

        return ECapability.Allowed;
    }

    // User-Overridden methods

    protected virtual ValueTask AfterReadAsync()
    {
        return ValueTask.CompletedTask;
    }

    protected virtual ValueTask<bool> EvaluateReadConstraintsAsync()
    {
        return ValueTask.FromResult(true);
    }

    protected virtual ValueTask<bool> EvaluateReadAuthorizationAsync()
    {
        return ValueTask.FromResult(true);
    }

    #endregion

    #region Update State

    internal async ValueTask UpdateAsync()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        IsLoadingData = true;
        Logger?.LogTrace("BEGIN: [CRUDComponent] UpdateAsync...");

        try
        {
            UpdateCapability = await GetUpdateCapabilityAsync();

            ThrowIfIncapable(UpdateCapability);

            State = EState.Update;

            await AfterUpdateAsync();
        }
        catch (RestrictedCapabilityException ex)
        {
            State = EState.Read;
            Logger?.LogWarning(ex, "RESTRICTED: [CRUDComponent] UpdateAsync.");
            await DialogService.Alert(
                string.Format(TEXT_Restricted, "Update"),
                TITLE_Restricted);
        }
        catch (UnauthorizedCapabilityException ex)
        {
            State = EState.Read;
            Logger?.LogWarning(ex, "UNAUTHORIZED: [CRUDComponent] UpdateAsync.");
            await DialogService.Alert(
                string.Format(TEXT_Unauthorized, "Update"),
                TITLE_Unauthorized);
        }
        catch (Exception ex)
        {
            State = EState.Read;
            Logger?.LogError(ex, "EXCEPTION: [CRUDComponent] UpdateAsync!");
            await DialogService.Alert(
                ex.Message,
                TITLE_UnexpectedError);
        }
        finally
        {
            IsLoadingData = false;
            Logger?.LogTrace("COMPLETE: [CRUDComponent] UpdateAsync.");
        }
    }

    private async ValueTask<ECapability> GetUpdateCapabilityAsync()
    {
        if (typeof(TModel) is not IUpdateModel)
        {
            return ECapability.Restricted;
        }

        var hasNoConstraints = await EvaluateUpdateConstraintsAsync();

        if (!hasNoConstraints)
        {
            return ECapability.Restricted;
        }

        var hasAuthorization = await EvaluateUpdateAuthorizationAsync();

        if (!hasAuthorization)
        {
            return ECapability.Unauthorized;
        }

        return ECapability.Allowed;
    }

    // User-Overridden methods

    protected virtual ValueTask AfterUpdateAsync()
    {
        return ValueTask.CompletedTask;
    }

    protected virtual ValueTask<bool> EvaluateUpdateConstraintsAsync()
    {
        return ValueTask.FromResult(true);
    }

    protected virtual ValueTask<bool> EvaluateUpdateAuthorizationAsync()
    {
        return ValueTask.FromResult(true);
    }

    #endregion

    #region Delete State

    internal async ValueTask DeleteAsync()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ArgumentNullException.ThrowIfNull(Model);

        IsLoadingData = true;
        Logger?.LogTrace("BEGIN: [CRUDComponent] DeleteAsync...");

        try
        {
            DeleteCapability = await GetDeleteCapabilityAsync();

            ThrowIfIncapable(DeleteCapability);

            var result = await PromptConfirmDeleteAsync();

            if (!result)
            {
                return;
            }

            var repository = ScopedServices.GetRequiredService<IDeleteRepository<TModel>>();

            await repository.DeleteAsync(Model, ComponentUnloaded);

            State = EState.Delete;

            await AfterDeleteAsync();

            await OnDeleted.InvokeAsync(Model);
        }
        catch (RestrictedCapabilityException ex)
        {
            State = EState.Read;
            Logger?.LogWarning(ex, "RESTRICTED: [CRUDComponent] DeleteAsync.");
            await DialogService.Alert(
                string.Format(TEXT_Restricted, "Delete"),
                TITLE_Restricted);
        }
        catch (UnauthorizedCapabilityException ex)
        {
            State = EState.Read;
            Logger?.LogWarning(ex, "UNAUTHORIZED: [CRUDComponent] DeleteAsync.");
            await DialogService.Alert(
                string.Format(TEXT_Unauthorized, "Delete"),
                TITLE_Unauthorized);
        }
        catch (Exception ex)
        {
            State = EState.Read;
            Logger?.LogError(ex, "EXCEPTION: [CRUDComponent] DeleteAsync!");
            await DialogService.Alert(
                ex.Message,
                TITLE_UnexpectedError);
        }
        finally
        {
            IsLoadingData = false;
            Logger?.LogTrace("COMPLETE: [CRUDComponent] DeleteAsync.");
        }
    }

    private async ValueTask<ECapability> GetDeleteCapabilityAsync()
    {
        if (typeof(TModel) is not IDeleteModel)
        {
            return ECapability.Restricted;
        }

        var hasNoConstraints = await EvaluateDeleteConstraintsAsync();

        if (!hasNoConstraints)
        {
            return ECapability.Restricted;
        }

        var hasAuthorization = await EvaluateDeleteAuthorizationAsync();

        if (!hasAuthorization)
        {
            return ECapability.Unauthorized;
        }

        return ECapability.Allowed;
    }

    // User-Overridden methods

    protected virtual ValueTask AfterDeleteAsync()
    {
        return ValueTask.CompletedTask;
    }

    protected virtual ValueTask<bool> EvaluateDeleteConstraintsAsync()
    {
        return ValueTask.FromResult(true);
    }

    protected virtual ValueTask<bool> EvaluateDeleteAuthorizationAsync()
    {
        return ValueTask.FromResult(true);
    }

    private async ValueTask<bool> PromptConfirmDeleteAsync()
    {
        return await DialogService.Confirm(
            TEXT_ConfirmDelete,
            TITLE_ConfirmDelete,
            options: new()
            {
                OkButtonText = "Delete"
            }) ?? false;
    }

    #endregion

    #region Save Logic

    internal async ValueTask SaveAsync()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ArgumentNullException.ThrowIfNull(EditModel);

        if (State is not EState.Create or EState.Update)
        {
            NotificationService.Notify(
                NotificationSeverity.Info,
                TITLE_InvalidSaveState,
                TEXT_InvalidSaveState);
        }

        IsLoadingData = true;
        Logger?.LogTrace("BEGIN: [CRUDComponent] SaveAsync...");

        try
        {
            if (State is EState.Create)
            {
                CreateCapability = await GetCreateCapabilityAsync();

                ThrowIfIncapable(CreateCapability);

                var repository = ScopedServices.GetRequiredService<ICreateRepository<TModel>>();

                await repository.CreateAsync(EditModel, ComponentUnloaded);
            }
            else if (State is EState.Update)
            {
                UpdateCapability = await GetUpdateCapabilityAsync();

                ThrowIfIncapable(UpdateCapability);

                var repository = ScopedServices.GetRequiredService<IUpdateRepository<TModel>>();

                await repository.UpdateAsync(EditModel, ComponentUnloaded);
            }

            await AfterSaveAsync();

            await OnSaved.InvokeAsync(Model);

            await ReadAsync();
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "EXCEPTION: [CRUDComponent] SaveAsync!");
            await DialogService.Alert(
                ex.Message,
                TITLE_UnexpectedError);
        }
        finally
        {
            IsLoadingData = false;
            Logger?.LogTrace("COMPLETE: [CRUDComponent] SaveAsync.");
        }
    }

    // User-Overridden methods

    protected virtual ValueTask AfterSaveAsync()
    {
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Cancel Logic

    internal async ValueTask CancelAsync()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (State is not EState.Create or EState.Update)
        {
            NotificationService.Notify(
                NotificationSeverity.Info,
                TITLE_InvalidCancelState,
                TEXT_InvalidCancelState);
        }

        IsLoadingData = true;
        Logger?.LogTrace("BEGIN: [CRUDComponent] CancelAsync...");

        try
        {
            if (State is EState.Create)
            {
                await CloseAsync();

                return;
            }

            BuildEditModel();

            await AfterCancelAsync();

            await ReadAsync();
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "EXCEPTION: [CRUDComponent] CancelAsync!");
            await DialogService.Alert(
                ex.Message,
                TITLE_UnexpectedError);
        }
        finally
        {
            IsLoadingData = false;
            Logger?.LogTrace("COMPLETE: [CRUDComponent] CancelAsync.");
        }
    }

    // User-Overridden methods

    protected virtual ValueTask AfterCancelAsync()
    {
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Close Logic

    internal async ValueTask CloseAsync()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        IsLoadingData = true;
        Logger?.LogTrace("BEGIN: [CRUDComponent] CloseAsync...");

        try
        {
            if (State is EState.Create or EState.Update && HasChanges)
            {
                var result = await PromptConfirmDiscardAsync();

                if (!result)
                {
                    return;
                }
            }

            Model = null;

            await OnClosed.InvokeAsync();

            State = EState.Unloaded;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "EXCEPTION: [CRUDComponent] CloseAsync!");
            await DialogService.Alert(
                ex.Message,
                TITLE_UnexpectedError);
        }
        finally
        {
            IsLoadingData = false;
            Logger?.LogTrace("COMPLETE: [CRUDComponent] CloseAsync.");
        }
    }

    #endregion

    #region Event Handlers

    private async ValueTask OnLocationChanging(LocationChangingContext context)
    {
        if (HasChanges)
        {
            var result = await PromptConfirmDiscardAsync();

            if (!result)
            {
                context.PreventNavigation();
            }
        }
    }

    private void OnFieldChanged(object? _, FieldChangedEventArgs args)
    {
        HasChanges = EditContext?.IsModified(args.FieldIdentifier) ?? false;
        StateHasChanged();
    }

    private void OnValidationRequested(object? _, ValidationRequestedEventArgs args)
    {
        IsValidating = true;
        StateHasChanged();
    }

    private void OnValidationStateChanged(object? _, ValidationStateChangedEventArgs args)
    {
        IsValidating = false;
        StateHasChanged();
    }

    protected async ValueTask OnSubmit()
    {
        if (HasEditContext)
        {
            var validationResult = await EditContext.GetValidationResultAsync();

            if (validationResult.IsValid)
            {
                await SaveAsync();
            }
            else
            {
                OnInvalidSubmit();
            }
        }
    }

    protected void OnInvalidSubmit()
    {
        NotificationService.Notify(
            NotificationSeverity.Warning,
            TITLE_InvalidSubmit,
            TEXT_InvalidSubmit);
    }

    #endregion

    #region UI Handlers

    protected async ValueTask ButtonCreateOnClick()
    {
        if (ShowCreateButton && !DisableCreateButton)
        {
            await CreateAsync();
        }
    }

    protected async ValueTask ButtonUpdateOnClick()
    {
        if (ShowUpdateButton && !DisableUpdateButton)
        {
            await UpdateAsync();
        }
    }

    protected async ValueTask ButtonDeleteOnClick()
    {
        if (DisableDeleteButton && !DisableDeleteButton)
        {
            await DeleteAsync();
        }
    }

    protected async ValueTask ButtonCancelOnClick()
    {
        if (ShowCancelButton)
        {
            await CancelAsync();
        }
    }

    #endregion

    private async ValueTask<bool> PromptConfirmDiscardAsync()
    {
        return await DialogService.Confirm(
            TEXT_ConfirmDiscard,
            TITLE_ConfirmDiscard,
            options: new()
            {
                OkButtonText = "Discard"
            }) ?? false;
    }

    private void BuildEditModel()
    {
        UnregisterEditContextEvents();

        if (Model is null)
        {
            throw new ArgumentNullException(nameof(Model));
        }

        EditModel = Model with { };
        EditContext = new(EditModel);
        HasChanges = false;

        RegisterEditContextEvents();
    }

    private void RegisterEditContextEvents()
    {
        if (EditContext is not null)
        {
            EditContext.OnFieldChanged += OnFieldChanged;
            EditContext.OnValidationRequested += OnValidationRequested;
            EditContext.OnValidationStateChanged += OnValidationStateChanged;
        }
    }

    private void UnregisterEditContextEvents()
    {
        if (EditContext is not null)
        {
            EditContext.OnFieldChanged -= OnFieldChanged;
            EditContext.OnValidationRequested -= OnValidationRequested;
            EditContext.OnValidationStateChanged -= OnValidationStateChanged;
        }
    }

    private static void ThrowIfIncapable(ECapability capability)
    {
        if (capability is ECapability.Restricted)
        {
            throw new RestrictedCapabilityException();
        }
        else if (capability is ECapability.Unauthorized)
        {
            throw new UnauthorizedCapabilityException();
        }
    }
}
