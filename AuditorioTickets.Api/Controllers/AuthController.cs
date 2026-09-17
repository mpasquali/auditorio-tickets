using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuditorioTickets.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace AuditorioTickets.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;

    public AuthController(IConfiguration config) => _config = config;

    [HttpPost("login")]
    public ActionResult<LoginResponseDto> Login(LoginRequestDto dto)
    {
        // Hardcodeado por ahora vía appsettings (AdminSeed). El día que quieras
        // más de un admin, esto se reemplaza por una tabla + hash de password real.
        var usuarioEsperado = _config["AdminSeed:Usuario"];
        var passwordEsperada = _config["AdminSeed:Password"];

        if (dto.Usuario != usuarioEsperado || dto.Password != passwordEsperada)
            return Unauthorized(new { mensaje = "Usuario o contraseña incorrectos." });

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, dto.Usuario),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expira = DateTime.UtcNow.AddMinutes(double.Parse(_config["Jwt:ExpiraMinutos"]!));

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expira,
            signingCredentials: creds);

        return Ok(new LoginResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiraEn = expira
        });
    }
}