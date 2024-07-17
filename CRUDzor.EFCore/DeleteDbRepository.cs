using AutoMapper.EntityFrameworkCore;
using CRUDzor.Model;
using CRUDzor.Repository;

namespace CRUDzor.EFCore;

public class DeleteDbRepository<TEntity, TModel>(IServiceProvider serviceProvider) : DbRepository<TEntity>(serviceProvider), IDeleteRepository<TModel>
    where TEntity : class, IEntity
    where TModel : CRUDModel
{
    public async ValueTask DeleteAsync(
        TModel model,
        CancellationToken ct = default)
    {
        var dbContext = await DbContextTask.Value;

        // Step 1: Persist to Store
        await dbContext.Set<TEntity>()
            .Persist(Mapper)
            .RemoveAsync(model, ct);

        // Step 2: Save Changes
        await dbContext.SaveChangesAsync(ct);
    }
}
