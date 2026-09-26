namespace C4ModelBuilder.Sample.Target
{
    public interface IUserApplication
    {
        Task<User[]> GetUsersAsync(CancellationToken ct);
    }
}
