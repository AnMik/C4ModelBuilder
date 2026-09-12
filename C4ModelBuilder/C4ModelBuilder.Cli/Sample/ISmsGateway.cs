using System.Threading;
using System.Threading.Tasks;
using C4ModelBuilder.Models.Attributes;

namespace C4ModelBuilder.Examples.Sample
{
    // Интерфейс внешнего сервиса без имплементации в солюшене (аналог вызова внешней библиотеки).
    [C4Component(Description = "External SMS gateway")]
    public interface ISmsGateway
    {
        [C4Component(Description = "Send SMS message")]
        Task SendAsync(string message, CancellationToken ct);
    }
}
