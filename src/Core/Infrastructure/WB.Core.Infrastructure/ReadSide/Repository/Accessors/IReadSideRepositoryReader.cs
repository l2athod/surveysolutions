namespace WB.Core.Infrastructure.ReadSide.Repository.Accessors
{
    /// <summary>
    /// Accessor for read-side repository which should be used to perform queries.
    /// </summary>
    public interface IReadSideRepositoryReader<TEntity>
        where TEntity : class, IReadSideRepositoryEntity
    {
        int Count();

        TEntity GetById(string id);
    }
}