using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CRUDzor.EFCore;

public class DbRepository<TEntity> : IAsyncDisposable
    where TEntity : class, IEntity
{
    protected readonly ILogger Logger;
    protected readonly IMapper Mapper;
    protected readonly Lazy<Task<DbContext>> DbContextTask;

    protected bool IsDisposed;

    internal DbRepository(IServiceProvider serviceProvider)
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var dbFactory = serviceProvider.GetRequiredService<IDbContextFactory<DbContext>>();

        Logger = loggerFactory.CreateLogger(GetType());
        DbContextTask = new(() => dbFactory.CreateDbContextAsync());
        Mapper = serviceProvider.GetRequiredService<IMapper>();
    }

    protected virtual ValueTask DisposeAsync(bool isDisposing)
    {
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (!IsDisposed)
        {
            await DisposeAsync(isDisposing: true);

            if (DbContextTask.IsValueCreated)
            {
                var dbContext = await DbContextTask.Value;

                await dbContext.DisposeAsync();
            }
        }

        GC.SuppressFinalize(this);
    }
}