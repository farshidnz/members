using System.IdentityModel.Tokens.Jwt;

namespace SettingsAPI.Authorization
{
    public interface ITokenValidation
    {
        JwtSecurityToken ValidateToken(string token);
    }
}