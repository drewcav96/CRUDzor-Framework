using CRUDzor.Model;
using System.Linq.Expressions;

namespace CRUDzor.Repository;

public interface IReadRepository<TModel>
    where TModel : CRUDModel
{
    ValueTask<TModel?> ReadAsync(
        Expression<Func<TModel, bool>> predicate,
        IDictionary<string, object>? parameters = null,
        CancellationToken ct = default);
}
