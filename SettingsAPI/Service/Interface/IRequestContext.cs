using System.Threading.Tasks;

namespace SettingsAPI.Service.Interface
{
    public interface IRequestContext
    {
        Task<int> GetMemberIdFromContext();

        Task<(int?, int)> GetPersonIdAndMemberIdFromContext();

        string CognitoId { get; }
    }
}