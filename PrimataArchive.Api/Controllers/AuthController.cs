using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimataArchive.Api.Models;
using PrimataArchive.Api.Services;

namespace PrimataArchive.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly PrimataArchiveContext _context;
    private readonly TokenService _tokenService;

    public AuthController(PrimataArchiveContext context, TokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    [HttpPost("registrar")]
    public async Task<IActionResult> Registrar([FromBody] RegistrarRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NomeFinal))
            return BadRequest(new { Message = "Informe um nome de usuário." });

        var email = request.Email.Trim().ToLowerInvariant();
        var nome = request.NomeFinal;

        if (await _context.Usuarios.AnyAsync(u => u.Email == email))
            return Conflict(new { Message = "Este e-mail já está cadastrado." });

        if (await _context.Usuarios.AnyAsync(u => u.Nome == nome))
            return Conflict(new { Message = "Este nome de usuário já está em uso." });

        var usuario = new Usuario
        {
            Nome = nome,
            Email = email,
            Senha = BCrypt.Net.BCrypt.HashPassword(request.Senha),
            TipoUsuario = "usuario",
            DataCadastro = DateTime.UtcNow
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        var token = _tokenService.GenerateToken(usuario);
        return Ok(new
        {
            mensagem = "Usuário registrado com sucesso.",
            token,
            usuario = new
            {
                idUsuario = usuario.IdUsuario,
                nome = usuario.Nome,
                email = usuario.Email,
                tipoUsuario = usuario.TipoUsuario
            }
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

        if (usuario == null || !BCrypt.Net.BCrypt.Verify(request.Senha, usuario.Senha))
            return Unauthorized(new { Message = "E-mail ou senha inválidos." });

        var token = _tokenService.GenerateToken(usuario);
        return Ok(new
        {
            mensagem = "Login bem-sucedido",
            token,
            usuario = new
            {
                idUsuario = usuario.IdUsuario,
                nome = usuario.Nome,
                email = usuario.Email,
                tipoUsuario = usuario.TipoUsuario
            }
        });
    }
}
