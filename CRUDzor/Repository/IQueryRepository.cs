using CRUDzor.Model;

namespace CRUDzor.Repository;

public interface IQueryRepository<TModel>
    where TModel : class, IReadModel
{
    ValueTask<QueryResult<TModel>> QueryAsync(
        string? filters = null,
        string? orderBy = null,
        int? skip = null,
        int? take = null,
        IDictionary<string, object>? parameters = null,
        CancellationToken ct = default);
}
