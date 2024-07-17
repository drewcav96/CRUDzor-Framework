using Microsoft.JSInterop;

namespace CRUDzor.Components;

public class TimeZoneProvider : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    public TimeZoneProvider(IJSRuntime jsRuntime)
    {
        moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/CRUDzor.Components/timeZoneProvider.js").AsTask());
    }

    public async ValueTask<string> GetLocalTimeZoneIANA()
    {
        var module = await moduleTask.Value;

        return await module.InvokeAsync<string>("getTimeZone");
    }

    public async ValueTask<string?> GetLocalTimeZoneWindows()
    {
        var timeZoneIANA = await GetLocalTimeZoneIANA();
        var hasTimeZone = TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneIANA, out var timeZoneId);

        return hasTimeZone
            ? timeZoneId
            : null;
    }

    public async ValueTask DisposeAsync()
    {
        if (moduleTask.IsValueCreated)
        {
            var module = await moduleTask.Value;
            await module.DisposeAsync();
        }
    }
}
