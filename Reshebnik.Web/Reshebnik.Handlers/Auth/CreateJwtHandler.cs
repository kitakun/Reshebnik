using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using Reshebnik.Domain.Entities;
using Reshebnik.Domain.Extensions;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Reshebnik.Handlers.Auth;

public class CreateJwtHandler(IConfiguration configuration)
{
    public readonly record struct JwtResponseRecord(EmployeeEntity User, string Jwt, int CurrentCompanyId);

    public JwtResponseRecord CreateToken(EmployeeEntity user, int? currentCompanyId, DateTime? expires = null)
    {
        user.EnsurePropertyExists(p => p.Company);


        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: null,
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FIO),
                new Claim("user-role", user.Role.ToString()),
                new Claim("company", $"{currentCompanyId ?? user.CompanyId}")
            ],
            expires: expires ?? DateTime.UtcNow.AddMonths(1),
            signingCredentials: creds
        );

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        
        var handler = new JwtSecurityTokenHandler();
        var token2 = handler.ReadJwtToken(jwt);
        Console.WriteLine(token2.ValidTo);
        
        user.Password = null!;
        user.Salt = null!;
        // TODO separate modal
        return new JwtResponseRecord(user, jwt, currentCompanyId ?? -1);
    }
}