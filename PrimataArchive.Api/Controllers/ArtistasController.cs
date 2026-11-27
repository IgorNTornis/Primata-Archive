using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using PrimataArchive.Api.Models;

namespace PrimataArchive.Api.Controllers;

[ApiController]
[Route("api/artistas")]
public class ArtistasController : ControllerBase
{
    private readonly PrimataArchiveContext _context;

    public ArtistasController(PrimataArchiveContext context)
    {
        _context = context;
    }


    [HttpGet("destaques")]
    public async Task<IActionResult> GetDestaques()
    {
        var artistas = await _context.Set<Artistum>()
            .OrderByDescending(a => a.Musicas.Count)
            .ThenBy(a => a.NomeArtista)
            .Take(12)
            .Select(a => new ArtistaResponse(a.IdArtista, a.NomeArtista, a.Foto, a.DataCadastro))
            .ToListAsync();
        return Ok(artistas);
    }

    [HttpGet("{id:int}/musicas")]
    public async Task<IActionResult> GetMusicasDoArtista(int id)
    {
        var musicas = await _context.Musicas
            .Where(m => m.IdArtista == id && (m.IdAlbum == null || m.IdAlbumNavigation!.Status == "publicado"))
            .Include(m => m.IdArtistaNavigation)
            .Include(m => m.IdAlbumNavigation)
            .OrderBy(m => m.NumeroFaixa)
            .Select(m => new MusicaResponse(
                m.IdMusica, m.IdArtista, m.IdArtistaNavigation.NomeArtista,
                m.IdAlbum, m.IdAlbumNavigation != null ? m.IdAlbumNavigation.Titulo : null,
                m.Titulo, m.Duracao, m.Genero, m.Capa, m.NumeroFaixa,
                m.DataLancamento, m.Reproducoes, m.DataCadastro))
            .ToListAsync();
        return Ok(musicas);
    }

    [HttpPost("{id:int}/seguir")]
    [Authorize]
    public async Task<IActionResult> SeguirArtista(int id)
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(idClaim, out var usuarioId)) return Unauthorized();

        if (!await _context.Set<Artistum>().AnyAsync(a => a.IdArtista == id))
            return NotFound(new { Message = "Artista não encontrado." });

        var existe = await _context.SeguirArtista.AnyAsync(s => s.IdUsuario == usuarioId && s.IdArtista == id);
        if (existe) return Ok(new { Message = "Você já segue este artista." });

        _context.SeguirArtista.Add(new SeguirArtistum
        {
            IdUsuario = usuarioId,
            IdArtista = id,
            DataSeguimento = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Artista seguido com sucesso." });
    }

    [HttpGet]
    public async Task<IActionResult> GetArtistas()
    {
        var artistas = await _context.Set<Artistum>()
            .Select(a => new ArtistaResponse(
                a.IdArtista,
                a.NomeArtista,
                a.Foto,
                a.DataCadastro
            ))
            .ToListAsync();

        return Ok(artistas);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetArtistaById(int id)
    {
        var artista = await _context.Set<Artistum>()
            .FirstOrDefaultAsync(a => a.IdArtista == id);

        if (artista == null)
            return NotFound(new { Message = "Artista não encontrado." });

        var response = new ArtistaResponse(
            artista.IdArtista,
            artista.NomeArtista,
            artista.Foto,
            artista.DataCadastro
        );

        return Ok(response);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CriarArtista([FromBody] ArtistaRequest request)
    {
        var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;
        if (!int.TryParse(usuarioIdClaim, out var usuarioId))
            return Unauthorized();

        if (await _context.Artista.AnyAsync(a => a.IdUsuario == usuarioId))
            return Conflict(new { Message = "Esta conta já possui um perfil de artista." });

        var emailArtista = string.IsNullOrWhiteSpace(request.EmailArtista) ? emailClaim : request.EmailArtista;
        if (string.IsNullOrWhiteSpace(emailArtista))
            return BadRequest(new { Message = "Informe um e-mail para o perfil de artista." });

        var novoArtista = new Artistum
        {
            IdUsuario = usuarioId,
            EmailArtista = emailArtista,
            NomeArtista = request.NomeArtista,
            Biografia = request.Biografia ?? string.Empty,
            Foto = request.Foto,
            DataCadastro = DateTime.UtcNow
        };

        _context.Set<Artistum>().Add(novoArtista);
        await _context.SaveChangesAsync();

        var response = new ArtistaResponse(
            novoArtista.IdArtista,
            novoArtista.NomeArtista,
            novoArtista.Foto,
            novoArtista.DataCadastro
        );

        return CreatedAtAction(nameof(GetArtistaById), new { id = novoArtista.IdArtista }, response);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> AtualizarArtista(int id, [FromBody] ArtistaRequest request)
    {
        var artista = await _context.Set<Artistum>().FindAsync(id);
        if (artista == null)
            return NotFound(new { Message = "Artista não encontrado." });

        artista.NomeArtista = request.NomeArtista;
        artista.Foto = request.Foto;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeletarArtista(int id)
    {
        var artista = await _context.Set<Artistum>().FindAsync(id);
        if (artista == null)
            return NotFound(new { Message = "Artista não encontrado." });

        _context.Set<Artistum>().Remove(artista);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}