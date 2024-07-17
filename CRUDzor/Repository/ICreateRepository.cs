using CRUDzor.Model;

namespace CRUDzor.Repository;

public interface ICreateRepository<TModel>
    where TModel : CRUDModel
{
    ValueTask<TModel> InstantiateAsync(CancellationToken ct = default);

    ValueTask CreateAsync(TModel model, CancellationToken ct = default);
}
