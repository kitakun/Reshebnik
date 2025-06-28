using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using Reshebnik.Domain.Entities;
using Reshebnik.Domain.Extensions;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Reshebnik.Handlers.Auth;

public class CreateJwtHandler
{
    public readonly record struct JwtResponseRecord(EmployeeEntity User, string Jwt, int CurrentCompanyId);

    public JwtResponseRecord CreateToken(EmployeeEntity user, IConfiguration configuration, int? currentCompanyId, DateTime? expires = null)
    {
        user.EnsurePropertyExists(p => p.Company);

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FIO),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("company", $"{currentCompanyId ?? user.CompanyId}"),
            ]),
            Expires = expires ?? DateTime.UtcNow.AddMonths(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwt = tokenHandler.WriteToken(token);
        user.Password = null!;
        user.Salt = null!;
        // TODO separate modal
        return new JwtResponseRecord(user, jwt, currentCompanyId ?? -1);
    }
}