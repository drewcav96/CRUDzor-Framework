using AutoMapper.QueryableExtensions;
using CRUDzor.Model;
using CRUDzor.Repository;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

namespace CRUDzor.EFCore;

public abstract class QueryDbRepository<TEntity, TModel>(IServiceProvider serviceProvider) : DbRepository<TEntity>(serviceProvider), IQueryRepository<TModel>
    where TEntity : class, IEntity
    where TModel : class, IReadModel
{
    public async ValueTask<QueryResult<TModel>> QueryAsync(
        string? queryFilters = null,
        string? orderBy = null,
        int? skip = null,
        int? take = null,
        IDictionary<string, object>? parameters = null,
        CancellationToken ct = default)
    {
        var dbContext = await DbContextTask.Value;

        // Step 0: Start new Query
        var query = dbContext.Set<TEntity>().AsQueryable();

        // Step 1: Get Total Count
        var totalCount = await query.CountAsync(ct);

        // Step 2: Apply Query Filter
        if (!string.IsNullOrWhiteSpace(queryFilters))
        {
            query = query.Where(queryFilters);
        }

        // Step 3: Get Result Count
        var resultCount = await query.CountAsync(ct);

        // Step 4: Apply OrderBy
        if (!string.IsNullOrEmpty(orderBy))
        {
            query = query.OrderBy(orderBy);
        }

        // Step 5: Apply Paging
        if (skip.HasValue && take.HasValue)
        {
            query = query.Skip(skip.Value);
            query = query.Take(take.Value);
        }

        // Step 6: Enumerate Results
        var data = await query
            .ProjectTo<TModel>(Mapper.ConfigurationProvider, parameters)
            .ToArrayAsync(ct);

        return new(totalCount, resultCount, data);
    }
}
