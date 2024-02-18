using SettingsAPI.Service.Interface;
using Unleash;
using Unleash.Internal;

namespace SettingsAPI.Service
{
    public class UnleashFeatureToggleService : IFeatureToggleService
    {
        private readonly IUnleash _proxy;

        public UnleashFeatureToggleService(IUnleash unleash)
        {
            _proxy = unleash;
        }

        public bool IsEnable(string featureName)
        {
            var tog = _proxy.FeatureToggles;
            return _proxy.IsEnabled(featureName);
        }

        public Variant GetVariant(string featureName)
        {
            return _proxy.GetVariant(featureName);
        }
    }
}
