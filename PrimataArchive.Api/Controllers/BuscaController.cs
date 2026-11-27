using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimataArchive.Api.Models;

namespace PrimataArchive.Api.Controllers;

[ApiController]
[Route("api/busca")]
public class BuscaController : ControllerBase
{
    private readonly PrimataArchiveContext _context;
    public BuscaController(PrimataArchiveContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> Buscar([FromQuery] string? q)
    {
        if (string.IsNullOrWhiteSpace(q)) return Ok(Array.Empty<object>());
        var termo = q.Trim();
        var artistas = await _context.Artista.Where(a => a.NomeArtista.Contains(termo))
            .Select(a => new { id = a.IdArtista, titulo = a.NomeArtista, artista = "Artista", tipo = "artista", url = "../perfil-artista/index.html?id=" + a.IdArtista })
            .Take(10).ToListAsync();
        var musicas = await _context.Musicas.Include(m => m.IdArtistaNavigation)
            .Where(m => m.Titulo.Contains(termo) && (m.IdAlbum == null || m.IdAlbumNavigation!.Status == "publicado"))
            .Select(m => new { id = m.IdMusica, titulo = m.Titulo, artista = m.IdArtistaNavigation.NomeArtista, tipo = "música", url = "../musica/index.html?id=" + m.IdMusica })
            .Take(10).ToListAsync();
        var albuns = await _context.Albums.Include(a => a.IdArtistaNavigation)
            .Where(a => a.Titulo.Contains(termo) && a.Status == "publicado")
            .Select(a => new { id = a.IdAlbum, titulo = a.Titulo, artista = a.IdArtistaNavigation.NomeArtista, tipo = "álbum", url = "../albuns/index.html?id=" + a.IdAlbum })
            .Take(10).ToListAsync();

        var resultados = new List<object>();
        resultados.AddRange(artistas);
        resultados.AddRange(musicas);
        resultados.AddRange(albuns);
        return Ok(resultados);
    }
}
