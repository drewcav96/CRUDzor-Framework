using AutoMapper.EntityFrameworkCore;
using CRUDzor.Model;
using CRUDzor.Repository;

namespace CRUDzor.EFCore;

public abstract class UpdateDbRepository<TEntity, TModel>(IServiceProvider serviceProvider) : DbRepository<TEntity>(serviceProvider), IUpdateRepository<TModel>
    where TEntity : class, IEntity
    where TModel : CRUDModel
{
    public async ValueTask UpdateAsync(
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
