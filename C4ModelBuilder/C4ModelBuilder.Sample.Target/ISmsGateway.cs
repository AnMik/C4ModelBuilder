using C4ModelBuilder.Attributes;

namespace C4ModelBuilder.Sample.Target
{
    // Интерфейс внешнего сервиса без имплементации в солюшене (аналог вызова внешней библиотеки).
    [C4Component("External SMS gateway", technology: "Twilio Gateway")]
    public interface ISmsGateway
    {
        [C4Component("Send SMS message")]
        Task SendAsync(string message, CancellationToken ct);
    }
}
