using AutoMapper.EntityFrameworkCore;
using CRUDzor.Model;
using CRUDzor.Repository;

namespace CRUDzor.EFCore;

public class CreateDbRepository<TEntity, TModel>(IServiceProvider serviceProvider) : DbRepository<TEntity>(serviceProvider), ICreateRepository<TModel>
    where TEntity : class, IEntity
    where TModel : CRUDModel
{
    public virtual ValueTask<TModel> InstantiateAsync(
        CancellationToken ct = default)
    {
        var model = Activator.CreateInstance<TModel>();

        return ValueTask.FromResult(model);
    }

    public async ValueTask CreateAsync(
        TModel model,
        CancellationToken ct = default)
    {
        var dbContext = await DbContextTask.Value;

        // Step 1: Persist to Store
        await dbContext.Set<TEntity>()
            .Persist(Mapper)
            .InsertOrUpdateAsync(model, ct);

        // Step 2: Save Changes
        await dbContext.SaveChangesAsync(ct);
    }
}
