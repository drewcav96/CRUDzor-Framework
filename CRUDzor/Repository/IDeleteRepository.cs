using CRUDzor.Model;

namespace CRUDzor.Repository;

public interface IDeleteRepository<TModel>
    where TModel : CRUDModel
{
    ValueTask DeleteAsync(
        TModel model,
        CancellationToken ct = default);
}
