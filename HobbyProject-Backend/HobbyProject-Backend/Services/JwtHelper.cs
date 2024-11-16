using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

public static class JwtHelper
{
    // Ensure the key is at least 256 bits (32 bytes)
    private static readonly string secretKey = "your-very-strong-secret-key-that-is-long-enough"; // Must be >= 256 bits (32 bytes)

    private static readonly string issuer = "your-app";
    private static readonly string audience = "your-audience";

    public static string GenerateToken(string username)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, "User")
        };

        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
