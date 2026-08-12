using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CASAPahampang.Services;



public static class JwtValidator
{
    public static (bool IsValid, string? Error, JwtSecurityToken? Token) ValidateJwt(
        string token,
        string validIssuer,
        string validAudience,
        string signingKey
    )
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = validIssuer,
                ValidateAudience = true,
                ValidAudience = validAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                RequireExpirationTime = true,
                RequireSignedTokens = true
            };

            tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            return (true, null, (JwtSecurityToken)validatedToken);
        }
        catch (SecurityTokenExpiredException)
        {
            return (false, "Token has expired", null);
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            return (false, "Invalid signature", null);
        }
        catch (SecurityTokenInvalidIssuerException)
        {
            return (false, "Invalid issuer", null);
        }
        catch (SecurityTokenInvalidAudienceException)
        {
            return (false, "Invalid audience", null);
        }
        catch(Exception ex)
        {
            return (false, $"Token validation failed: {ex.Message}", null);
        }
    }
}
