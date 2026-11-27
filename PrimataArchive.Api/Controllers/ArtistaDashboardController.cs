using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimataArchive.Api.Models;

namespace PrimataArchive.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/artista")]
public class ArtistaDashboardController : ControllerBase
{
    private readonly PrimataArchiveContext _context;
    public ArtistaDashboardController(PrimataArchiveContext context) => _context = context;

    private async Task<Artistum?> ArtistaLogado()
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var usuarioId)) return null;
        var artista = await _context.Artista.FirstOrDefaultAsync(a => a.IdUsuario == usuarioId);
        if (artista != null) return artista;

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == usuarioId);
        if (usuario == null) return null;

        artista = new Artistum
        {
            IdUsuario = usuarioId,
            EmailArtista = usuario.Email,
            NomeArtista = usuario.Nome,
            Biografia = usuario.Bio ?? string.Empty,
            DataCadastro = DateTime.UtcNow
        };
        _context.Artista.Add(artista);
        usuario.TipoUsuario = "artista";
        await _context.SaveChangesAsync();
        return artista;
    }

    [HttpGet("musicas")]
    public async Task<IActionResult> MinhasMusicas()
    {
        var artista = await ArtistaLogado();
        if (artista == null) return NotFound(new { Message = "Esta conta ainda não possui perfil de artista." });
        var musicas = await _context.Musicas.Where(m => m.IdArtista == artista.IdArtista)
            .OrderByDescending(m => m.DataCadastro)
            .Select(m => new { idMusica = m.IdMusica, titulo = m.Titulo, duracao = m.Duracao, reproducoes = m.Reproducoes, dataCadastro = m.DataCadastro })
            .ToListAsync();
        return Ok(musicas);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var artista = await ArtistaLogado();
        if (artista == null) return NotFound(new { Message = "Esta conta ainda não possui perfil de artista." });
        return Ok(new
        {
            artista = artista.NomeArtista,
            musicas = await _context.Musicas.CountAsync(m => m.IdArtista == artista.IdArtista),
            albuns = await _context.Albums.CountAsync(a => a.IdArtista == artista.IdArtista),
            reproducoes = await _context.Musicas.Where(m => m.IdArtista == artista.IdArtista).SumAsync(m => (long?)(m.Reproducoes ?? 0)) ?? 0,
            seguidores = await _context.SeguirArtista.CountAsync(s => s.IdArtista == artista.IdArtista)
        });
    }

    [HttpGet("estatisticas")]
    public async Task<IActionResult> Estatisticas()
    {
        var artista = await ArtistaLogado();
        if (artista == null) return NotFound(new { Message = "Esta conta ainda não possui perfil de artista." });
        var musicas = await _context.Musicas.Where(m => m.IdArtista == artista.IdArtista)
            .OrderByDescending(m => m.Reproducoes)
            .Select(m => new { titulo = m.Titulo, reproducoes = m.Reproducoes ?? 0 })
            .ToListAsync();
        return Ok(new { totalMusicas = musicas.Count, totalReproducoes = musicas.Sum(m => m.reproducoes), musicas });
    }
}
