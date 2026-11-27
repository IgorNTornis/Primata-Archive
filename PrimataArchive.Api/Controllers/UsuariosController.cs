using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimataArchive.Api.Models;

namespace PrimataArchive.Api.Controllers;

[ApiController]
[Route("api/usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly PrimataArchiveContext _context;

    public UsuariosController(PrimataArchiveContext context) => _context = context;

    private bool TryGetUsuarioId(out int usuarioId) =>
        int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out usuarioId);

    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetUsuarios()
    {
        var usuarios = await _context.Usuarios
            .Select(u => new
            {
                idUsuario = u.IdUsuario,
                nome = u.Nome,
                email = u.Email,
                tipoUsuario = u.TipoUsuario,
                dataCadastro = u.DataCadastro
            })
            .ToListAsync();
        return Ok(usuarios);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMeuUsuario()
    {
        if (!TryGetUsuarioId(out var usuarioId)) return Unauthorized();
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == usuarioId);
        if (usuario == null) return NotFound(new { Message = "Usuário não encontrado." });

        return Ok(new
        {
            idUsuario = usuario.IdUsuario,
            nome = usuario.Nome,
            username = usuario.Nome,
            nomeExibicao = usuario.Nome,
            email = usuario.Email,
            foto = usuario.Foto,
            bio = usuario.Bio,
            tipoUsuario = usuario.TipoUsuario,
            notificacoes = usuario.Notificacoes ?? true,
            playlistsCount = await _context.Playlists.CountAsync(p => p.IdUsuario == usuarioId),
            seguindoCount = await _context.SeguirArtista.CountAsync(s => s.IdUsuario == usuarioId)
        });
    }

    [HttpGet("me/atividade")]
    [Authorize]
    public async Task<IActionResult> GetMinhaAtividade()
    {
        if (!TryGetUsuarioId(out var usuarioId)) return Unauthorized();
        var posts = await _context.Posts
            .Where(p => p.IdUsuario == usuarioId)
            .Include(p => p.IdUsuarioNavigation)
            .Include(p => p.Comentarios)
            .OrderByDescending(p => p.DataPublicacao)
            .Take(30)
            .Select(p => new PostResponse(
                p.IdPost, p.IdComunidade, p.IdComunidadeNavigation != null ? p.IdComunidadeNavigation.Nome : null,
                p.IdUsuario, p.IdUsuarioNavigation.Nome, p.Titulo, p.Conteudo, p.Imagem,
                p.Comentarios.Count, p.DataPublicacao, p.Status))
            .ToListAsync();
        return Ok(posts);
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> AtualizarMeuUsuario([FromBody] AtualizarUsuarioRequest request)
    {
        if (!TryGetUsuarioId(out var usuarioId)) return Unauthorized();
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == usuarioId);
        if (usuario == null) return NotFound(new { Message = "Usuário não encontrado." });

        if (!string.IsNullOrWhiteSpace(request.NomeFinal))
        {
            var nome = request.NomeFinal.Trim();
            if (await _context.Usuarios.AnyAsync(u => u.IdUsuario != usuarioId && u.Nome == nome))
                return Conflict(new { Message = "Este nome de usuário já está em uso." });
            usuario.Nome = nome;
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var email = request.Email.Trim().ToLowerInvariant();
            if (await _context.Usuarios.AnyAsync(u => u.IdUsuario != usuarioId && u.Email == email))
                return Conflict(new { Message = "Este e-mail já está em uso." });
            usuario.Email = email;
        }

        if (request.Bio != null) usuario.Bio = request.Bio.Trim();
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Perfil atualizado com sucesso." });
    }

    [HttpPatch("me/preferencias")]
    [Authorize]
    public async Task<IActionResult> AtualizarPreferencias([FromBody] AtualizarPreferenciaRequest request)
    {
        if (!TryGetUsuarioId(out var usuarioId)) return Unauthorized();
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == usuarioId);
        if (usuario == null) return NotFound(new { Message = "Usuário não encontrado." });
        usuario.Notificacoes = request.Valor;
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Preferência atualizada." });
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetUsuarioPorId(int id)
    {
        var usuario = await _context.Usuarios
            .Where(u => u.IdUsuario == id)
            .Select(u => new { idUsuario = u.IdUsuario, nome = u.Nome, email = u.Email, tipoUsuario = u.TipoUsuario })
            .FirstOrDefaultAsync();
        if (usuario == null) return NotFound(new { Message = "Usuário não encontrado." });
        return Ok(usuario);
    }
}
