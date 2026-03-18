using Volo.Abp.Application.Services;
using SelfHostApp.Localization;

namespace SelfHostApp.Services;

/* Inherit your application services from this class. */
public abstract class SelfHostAppAppService : ApplicationService
{
    protected SelfHostAppAppService()
    {
        LocalizationResource = typeof(SelfHostAppResource);
    }
}