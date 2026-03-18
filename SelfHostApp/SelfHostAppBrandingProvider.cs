using Microsoft.Extensions.Localization;
using SelfHostApp.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace SelfHostApp;

[Dependency(ReplaceServices = true)]
public class SelfHostAppBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<SelfHostAppResource> _localizer;

    public SelfHostAppBrandingProvider(IStringLocalizer<SelfHostAppResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}