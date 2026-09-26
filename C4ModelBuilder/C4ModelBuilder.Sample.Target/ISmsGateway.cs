using C4ModelBuilder.Attributes;

namespace C4ModelBuilder.Sample.Target
{
    // Интерфейс внешнего сервиса без имплементации в солюшене (аналог вызова внешней библиотеки).
    [C4Component(Description = "External SMS gateway")]
    public interface ISmsGateway
    {
        [C4Component(Description = "Send SMS message")]
        Task SendAsync(string message, CancellationToken ct);
    }
}
