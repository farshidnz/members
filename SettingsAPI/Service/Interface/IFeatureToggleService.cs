using Unleash.Internal;

namespace SettingsAPI.Service.Interface
{
    public interface IFeatureToggleService
    {
        bool IsEnable(string featureName);
        Variant GetVariant(string featureName);
    }
}
