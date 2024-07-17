using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace CRUDzor.Components.Core;

public class ComponentBaseWithCancellation : OwningComponentBase, IDisposable
{
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isSettingParameters;
    private bool _isInitializing;

    /// <summary>
    /// The Component logger instance.
    /// </summary>
    protected ILogger? Logger { get; private set; }

    /// <summary>
    /// A Cancellation Token that is canceled when this Component gets disposed.
    /// </summary>
    protected CancellationToken ComponentUnloaded =>
        (_cancellationTokenSource ??= new()).Token;

    #region Component State

    /// <summary>
    /// Whether a critical background task is running and should block UI interactivity.
    /// <para>
    /// Override this and perform logical OR on <code>base.IsBusy</code> with your Component state values to use as a UI blocking indicator.
    /// </para>
    /// </summary>
    protected virtual bool IsBusy
    {
        get =>
            _isSettingParameters || _isInitializing;
    }

    #endregion

    #region Component Parameters

    [Inject]
    private ILoggerFactory? LoggerFactory { get; set; }

    [Inject]
    protected NavigationManager NavigationManager { get; private set; } = default!;

    #endregion

    #region Component Lifecycle

    public sealed override async Task SetParametersAsync(ParameterView parameters)
    {
        _isSettingParameters = true;

        await base.SetParametersAsync(parameters);

        Logger = LoggerFactory?.CreateLogger(GetType());
    }

    protected sealed override void OnInitialized()
    {
        _isInitializing = true;
        Logger?.LogTrace("Initializing: {Name}", GetType().Name);
    }

    protected sealed override async Task OnInitializedAsync()
    {
        await InitializeAsync();

        _isInitializing = false;
        Logger?.LogTrace("Initialization complete: {Name}", GetType().Name);
    }

    protected sealed override void OnParametersSet() { }

    protected sealed override async Task OnParametersSetAsync()
    {
        await ParametersSetAsync();

        _isSettingParameters = false;
        Logger?.LogTrace("Parameters set: {Name}", GetType().Name);
    }

    protected sealed override void OnAfterRender(bool firstRender) { }

    protected sealed override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await FirstRenderAsync();

            Logger?.LogTrace("First render: {Name}", GetType().Name);
        }

        await AfterRenderAsync();
        Logger?.LogTrace("After render: {Name}", GetType().Name);
    }

    #endregion

    #region Component Methods

    protected virtual ValueTask InitializeAsync()
    {
        return ValueTask.CompletedTask;
    }

    protected virtual ValueTask ParametersSetAsync()
    {
        return ValueTask.CompletedTask;
    }

    protected virtual ValueTask FirstRenderAsync()
    {
        return ValueTask.CompletedTask;
    }

    protected virtual ValueTask AfterRenderAsync()
    {
        return ValueTask.CompletedTask;
    }

    protected override void Dispose(bool isDisposing)
    {
        if (!IsDisposed)
        {
            if (isDisposing)
            {
                _cancellationTokenSource?.Dispose();
            }

            Logger?.LogTrace("Disposed: {Name}", GetType().Name);
        }
    }

    #endregion
}
