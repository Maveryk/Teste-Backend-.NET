using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TesteBackend.Model;

namespace TesteBackend.Services;

public class TokenService
{
    public static object GenerateToken(Conta conta)
    {
        var key = Encoding.ASCII.GetBytes(Key.Secret);
        var tokenConfig = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
                      new Claim("numeroConta", conta.numeroConta.ToString()),
            }),
            Expires = DateTime.UtcNow.AddHours(3),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenConfig);
        var tokenString = tokenHandler.WriteToken(token);

        return new
        {
            token = tokenString
        };
    }

    public static Conta ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Key.Secret)),
                ValidateIssuer = false,
                ValidateAudience = false
            };

            SecurityToken validatedToken;
            var claims = tokenHandler.ValidateToken(token, validationParameters, out validatedToken).Claims;

            var numeroConta = int.Parse(claims.FirstOrDefault(claim => claim.Type == "numeroConta")?.Value);
            var email = claims.FirstOrDefault(claim => claim.Type == "email")?.Value;

            return new Conta(numeroConta, email);
        }
        catch (Exception)
        {
            throw;
        }
    }
}