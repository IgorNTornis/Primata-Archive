using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PrimataArchive.Api.Models;

namespace PrimataArchive.Api.Services;

public class TokenService
{
    private readonly string _secret;

    public TokenService(IConfiguration configuration)
    {
        _secret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("A chave JWT não foi configurada (Jwt:Secret).");

        if (Encoding.UTF8.GetByteCount(_secret) < 32)
            throw new InvalidOperationException("A chave JWT precisa ter pelo menos 32 bytes.");
    }

    public string GenerateToken(Usuario usuario)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secret);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
            new(ClaimTypes.Email, usuario.Email ?? string.Empty),
            new(ClaimTypes.Name, usuario.Nome ?? string.Empty)
        };

        if (!string.IsNullOrWhiteSpace(usuario.TipoUsuario))
            claims.Add(new Claim(ClaimTypes.Role, usuario.TipoUsuario));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(8),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
    }

    public string GerarToken(Usuario usuario) => GenerateToken(usuario);
}
