// BlinkNotifier.Core — toast dispatch abstraction (ARCH.md § Components, Epic 2)
namespace BlinkNotifier.Core.Toast;

public interface IToastDispatcher
{
    Task ShowAsync(CancellationToken ct = default);
}
