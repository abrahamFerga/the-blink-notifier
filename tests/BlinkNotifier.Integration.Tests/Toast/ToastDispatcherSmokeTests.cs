// BlinkNotifier.Integration.Tests — toast dispatch smoke test (ARCH.md § Components, ADR-0003)
using BlinkNotifier.Core.Toast;

namespace BlinkNotifier.Integration.Tests.Toast;

public sealed class ToastDispatcherSmokeTests
{
    [Fact]
    public async Task ShowAsync_DoesNotThrowRegardlessOfAumidState()
    {
        // ToastDispatcher.ShowAsync wraps all WinRT calls in try/catch — it must
        // never surface an unhandled exception regardless of AUMID registration state.
        var sut = new ToastDispatcher(NullLogger<ToastDispatcher>.Instance);

        var ex = await Record.ExceptionAsync(() => sut.ShowAsync());

        Assert.Null(ex);
    }
}
