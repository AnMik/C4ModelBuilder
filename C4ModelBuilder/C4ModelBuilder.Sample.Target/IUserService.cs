namespace C4ModelBuilder.Sample.Target
{
    public interface IUserService
    {
        Task<User[]> GetUsersAsync(CancellationToken ct);
    }
}
