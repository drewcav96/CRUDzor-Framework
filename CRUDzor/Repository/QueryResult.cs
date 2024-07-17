using CRUDzor.Model;

namespace CRUDzor.Repository;

public readonly struct QueryResult<TModel>
    where TModel : class, IReadModel
{
    public int TotalCount { get; init; }

    public int ResultCount { get; init; }

    public IEnumerable<TModel> Data { get; init; } = [];

    public QueryResult(
        int totalCount,
        int resultCount,
        IEnumerable<TModel> data)
    {
        TotalCount = totalCount;
        ResultCount = resultCount;
        Data = data;
    }
}
