namespace C4ModelBuilder.Sample.Target
{
    public interface IUserDataSource
    {
        Task<User[]> FetchAll(CancellationToken ct);
    }
}
