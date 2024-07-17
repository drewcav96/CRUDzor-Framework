using AutoMapper.QueryableExtensions;
using CRUDzor.Model;
using CRUDzor.Repository;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CRUDzor.EFCore;

public abstract class ReadDbRepository<TEntity, TModel>(IServiceProvider serviceProvider) : DbRepository<TEntity>(serviceProvider), IReadRepository<TModel>
    where TEntity : class, IEntity
    where TModel : CRUDModel
{
    public async ValueTask<TModel?> ReadAsync(
        Expression<Func<TModel, bool>> predicate,
        IDictionary<string, object>? parameters = null,
        CancellationToken ct = default)
    {
        var dbContext = await DbContextTask.Value;

        // Step 1: Query Entity Predicate and Map to Model
        return await dbContext.Set<TEntity>()
            .ProjectTo<TModel>(Mapper.ConfigurationProvider, parameters)
            .Where(predicate)
            .SingleOrDefaultAsync(ct);
    }
}
