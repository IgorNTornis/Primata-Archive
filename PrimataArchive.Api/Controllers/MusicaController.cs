using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimataArchive.Api.Models;

namespace PrimataArchive.Api.Controllers;

[ApiController]
[Route("api/musicas")]
public class MusicaController : ControllerBase
{
    private readonly PrimataArchiveContext _context;

    public MusicaController(PrimataArchiveContext context)
    {
        _context = context;
    }


    [HttpGet("destaques")]
    public Task<IActionResult> GetDestaques() => GetRanking(12);

    [HttpGet("top")]
    public Task<IActionResult> GetTop() => GetRanking(20);

    private async Task<IActionResult> GetRanking(int limite)
    {
        var musicas = await _context.Musicas
            .Where(m => m.IdAlbum == null || m.IdAlbumNavigation!.Status == "publicado")
            .Include(m => m.IdArtistaNavigation)
            .Include(m => m.IdAlbumNavigation)
            .OrderByDescending(m => m.Reproducoes ?? 0)
            .ThenBy(m => m.Titulo)
            .Take(limite)
            .Select(m => new MusicaResponse(
                m.IdMusica, m.IdArtista, m.IdArtistaNavigation.NomeArtista,
                m.IdAlbum, m.IdAlbumNavigation != null ? m.IdAlbumNavigation.Titulo : null,
                m.Titulo, m.Duracao, m.Genero, m.Capa, m.NumeroFaixa,
                m.DataLancamento, m.Reproducoes, m.DataCadastro, m.AudioUrl))
            .ToListAsync();
        return Ok(musicas);
    }

    [HttpPost("{id:int}/curtir")]
    [Authorize]
    public async Task<IActionResult> Curtir(int id)
    {
        var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(idClaim, out var usuarioId)) return Unauthorized();
        if (!await _context.Musicas.AnyAsync(m => m.IdMusica == id && (m.IdAlbum == null || m.IdAlbumNavigation!.Status == "publicado")))
            return NotFound(new { Message = "Música não encontrada." });

        var favorito = await _context.MusicaFavorita.FindAsync(usuarioId, id);
        if (favorito != null) return Ok(new { Message = "Música já está curtida." });

        _context.MusicaFavorita.Add(new MusicaFavoritum
        {
            IdUsuario = usuarioId,
            IdMusica = id,
            DataFavorito = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Música curtida com sucesso." });
    }

    [HttpGet]
    public async Task<IActionResult> GetMusica()
    {
        var musicas = await _context.Musicas
            .Where(m => m.IdAlbum == null || m.IdAlbumNavigation!.Status == "publicado")
            .Include(m => m.IdArtistaNavigation)
            .Include(m => m.IdAlbumNavigation)
            .Select(m => new MusicaResponse(
                m.IdMusica,
                m.IdArtista,
                m.IdArtistaNavigation != null ? m.IdArtistaNavigation.NomeArtista : null,
                m.IdAlbum,
                m.IdAlbumNavigation != null ? m.IdAlbumNavigation.Titulo : null,
                m.Titulo,
                m.Duracao,
                m.Genero,
                m.Capa,
                m.NumeroFaixa,
                m.DataLancamento,
                m.Reproducoes,
                m.DataCadastro,
                m.AudioUrl
            ))
            .ToListAsync();

        return Ok(musicas);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetMusicaById(int id)
    {
        var musica = await _context.Musicas
            .Include(m => m.IdArtistaNavigation)
            .Include(m => m.IdAlbumNavigation)
            .FirstOrDefaultAsync(m => m.IdMusica == id && (m.IdAlbum == null || m.IdAlbumNavigation!.Status == "publicado"));

        if (musica == null)
            return NotFound(new { Message = "Música não encontrada." });

        var response = new MusicaResponse(
            musica.IdMusica,
            musica.IdArtista,
            musica.IdArtistaNavigation?.NomeArtista,
            musica.IdAlbum,
            musica.IdAlbumNavigation?.Titulo,
            musica.Titulo,
            musica.Duracao,
            musica.Genero,
            musica.Capa,
            musica.NumeroFaixa,
            musica.DataLancamento,
            musica.Reproducoes,
            musica.DataCadastro,
            musica.AudioUrl
        );

        return Ok(response);
    }

    [HttpGet("album/{idAlbum}")]
    public async Task<IActionResult> GetMusicasPorAlbum(int idAlbum)
    {
        var musicas = await _context.Musicas
            .Where(m => m.IdAlbum == idAlbum && m.IdAlbumNavigation!.Status == "publicado")
            .Include(m => m.IdArtistaNavigation)
            .Include(m => m.IdAlbumNavigation)
            .Select(m => new MusicaResponse(
                m.IdMusica,
                m.IdArtista,
                m.IdArtistaNavigation != null ? m.IdArtistaNavigation.NomeArtista : null,
                m.IdAlbum,
                m.IdAlbumNavigation != null ? m.IdAlbumNavigation.Titulo : null,
                m.Titulo,
                m.Duracao,
                m.Genero,
                m.Capa,
                m.NumeroFaixa,
                m.DataLancamento,
                m.Reproducoes,
                m.DataCadastro,
                m.AudioUrl
            ))
            .ToListAsync();

        return Ok(musicas);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CriarMusica([FromBody] CriarMusicaRequest request)
    {
        bool artistaExiste = await _context.Set<Artistum>().AnyAsync(a => a.IdArtista == request.IdArtista);
        if (!artistaExiste)
            return BadRequest(new { Message = "O artista informado não existe." });

        if (request.IdAlbum.HasValue)
        {
            bool albumExiste = await _context.Albums.AnyAsync(a => a.IdAlbum == request.IdAlbum.Value);
            if (!albumExiste)
                return BadRequest(new { Message = "O álbum informado não existe." });
        }

        var novaMusica = new Musica
        {
            IdArtista = request.IdArtista,
            IdAlbum = request.IdAlbum,
            Titulo = request.Titulo,
            Duracao = request.Duracao,
            Genero = request.Genero,
            Capa = request.Capa,
            NumeroFaixa = request.NumeroFaixa,
            DataLancamento = request.DataLancamento,
            Reproducoes = 0,
            DataCadastro = DateTime.UtcNow,
            AudioUrl = request.AudioUrl
        };

        _context.Musicas.Add(novaMusica);
        await _context.SaveChangesAsync();

        var response = new MusicaResponse(
            novaMusica.IdMusica,
            novaMusica.IdArtista,
            null,
            novaMusica.IdAlbum,
            null,
            novaMusica.Titulo,
            novaMusica.Duracao,
            novaMusica.Genero,
            novaMusica.Capa,
            novaMusica.NumeroFaixa,
            novaMusica.DataLancamento,
            novaMusica.Reproducoes,
            novaMusica.DataCadastro,
            novaMusica.AudioUrl
        );

        return CreatedAtAction(nameof(GetMusicaById), new { id = novaMusica.IdMusica }, response);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> AtualizarMusica(int id, [FromBody] CriarMusicaRequest request)
    {
        var musica = await _context.Musicas.FindAsync(id);
        if (musica == null)
            return NotFound(new { Message = "Música não encontrada." });

        bool artistaExiste = await _context.Set<Artistum>().AnyAsync(a => a.IdArtista == request.IdArtista);
        if (!artistaExiste)
            return BadRequest(new { Message = "O artista informado não existe." });

        if (request.IdAlbum.HasValue)
        {
            bool albumExiste = await _context.Albums.AnyAsync(a => a.IdAlbum == request.IdAlbum.Value);
            if (!albumExiste)
                return BadRequest(new { Message = "O álbum informado não existe." });
        }

        musica.IdArtista = request.IdArtista;
        musica.IdAlbum = request.IdAlbum;
        musica.Titulo = request.Titulo;
        musica.Duracao = request.Duracao;
        musica.Genero = request.Genero;
        musica.Capa = request.Capa;
        musica.NumeroFaixa = request.NumeroFaixa;
        musica.DataLancamento = request.DataLancamento;
        musica.AudioUrl = request.AudioUrl;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeletarMusica(int id)
    {
        var musica = await _context.Musicas.FindAsync(id);
        if (musica == null)
            return NotFound(new { Message = "Música não encontrada." });

        _context.Musicas.Remove(musica);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}