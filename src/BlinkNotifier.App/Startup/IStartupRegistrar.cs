// BlinkNotifier.App — startup registration abstraction for testability (ARCH.md § Solution layout)
namespace BlinkNotifier.App.Startup;

public interface IStartupRegistrar
{
    Task EnableAsync();
    Task DisableAsync();
}
