using CRUDzor.Model;

namespace CRUDzor.Repository;

public interface IUpdateRepository<TModel>
    where TModel : CRUDModel
{
    ValueTask UpdateAsync(
        TModel model,
        CancellationToken ct = default);
}
