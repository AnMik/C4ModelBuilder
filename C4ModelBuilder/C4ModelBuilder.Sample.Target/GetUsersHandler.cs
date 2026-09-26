using C4ModelBuilder.Attributes;

namespace C4ModelBuilder.Sample.Target
{
    [C4Component(Description = "CQRS handler for GetUsers query")]
    public sealed class GetUsersHandler : Rds.Cqrs.Queries.IQueryHandler<GetUsers, User[]>
    {
        private readonly IUserApplication _userApplication;

        public GetUsersHandler(IUserApplication userApplication) => _userApplication = userApplication;

        public Task<User[]> HandleAsync(GetUsers query, CancellationToken ct)
            => _userApplication.GetUsersAsync(ct);
    }
}
