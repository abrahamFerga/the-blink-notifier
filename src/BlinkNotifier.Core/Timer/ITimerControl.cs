// BlinkNotifier.Core — timer control surface consumed by App-layer ViewModels (ARCH.md § Components)
namespace BlinkNotifier.Core.Timer;

public interface ITimerControl
{
    void Start();
    void Stop();
    void ResetTimer();
}
