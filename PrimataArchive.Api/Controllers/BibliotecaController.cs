using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimataArchive.Api.Models;
namespace PrimataArchive.Api.Controllers;
[Authorize]
[ApiController]
[Route("api/biblioteca")]
public class BibliotecaController : ControllerBase
{
    private readonly PrimataArchiveContext _context;
    public BibliotecaController(PrimataArchiveContext context)
    {
        _context = context;
    }
    private int ObterUsuarioIdLogado()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idClaim, out int id) ? id : 0;
    }
    [HttpGet]
    public async Task<IActionResult> ObterMinhaBiblioteca()
    {
        int usuarioId = ObterUsuarioIdLogado();

        var itens = await _context.Bibliotecas
            .Include(b => b.IdMusicaNavigation)
            .Where(b => b.IdUsuario == usuarioId)
            .OrderByDescending(b => b.DataAdicao)
            .Select(b => new
            {
                b.IdUsuario,
                b.IdMusica,
                Titulo = b.IdMusicaNavigation != null ? b.IdMusicaNavigation.Titulo : null,
                Capa = b.IdMusicaNavigation != null ? b.IdMusicaNavigation.Capa : null,
                AudioUrl = b.IdMusicaNavigation != null ? b.IdMusicaNavigation.AudioUrl : null,
                Artista = b.IdMusicaNavigation != null ? b.IdMusicaNavigation.IdArtistaNavigation.NomeArtista : null,
                Album = b.IdMusicaNavigation != null && b.IdMusicaNavigation.IdAlbumNavigation != null ? b.IdMusicaNavigation.IdAlbumNavigation.Titulo : null,
                DataAdicao = b.DataAdicao
            })
            .ToListAsync();
        return Ok(itens);
    }

    [HttpGet("musicas")]
    public async Task<IActionResult> ObterMusicasDaBiblioteca()
    {
        int usuarioId = ObterUsuarioIdLogado();
        var musicas = await _context.MusicaFavorita
            .Where(f => f.IdUsuario == usuarioId)
            .Include(f => f.IdMusicaNavigation).ThenInclude(m => m.IdArtistaNavigation)
            .Include(f => f.IdMusicaNavigation).ThenInclude(m => m.IdAlbumNavigation)
            .OrderByDescending(f => f.DataFavorito)
            .Select(f => new
            {
                idMusica = f.IdMusica,
                titulo = f.IdMusicaNavigation.Titulo,
                capa = f.IdMusicaNavigation.Capa,
                duracao = f.IdMusicaNavigation.Duracao,
                nomeArtista = f.IdMusicaNavigation.IdArtistaNavigation.NomeArtista,
                audioUrl = f.IdMusicaNavigation.AudioUrl,
                album = f.IdMusicaNavigation.IdAlbumNavigation != null ? f.IdMusicaNavigation.IdAlbumNavigation.Titulo : null,
                dataAdicao = f.DataFavorito
            })
            .ToListAsync();
        return Ok(musicas);
    }

    [HttpGet("musicas/recentes")]
    public async Task<IActionResult> ObterMusicasRecentes()
    {
        int usuarioId = ObterUsuarioIdLogado();
        var musicas = await _context.MusicaFavorita
            .Where(f => f.IdUsuario == usuarioId)
            .Include(f => f.IdMusicaNavigation).ThenInclude(m => m.IdArtistaNavigation)
            .Include(f => f.IdMusicaNavigation).ThenInclude(m => m.IdAlbumNavigation)
            .OrderByDescending(f => f.DataFavorito)
            .Take(10)
            .Select(f => new
            {
                idMusica = f.IdMusica,
                titulo = f.IdMusicaNavigation.Titulo,
                capa = f.IdMusicaNavigation.Capa,
                duracao = f.IdMusicaNavigation.Duracao,
                nomeArtista = f.IdMusicaNavigation.IdArtistaNavigation.NomeArtista,
                audioUrl = f.IdMusicaNavigation.AudioUrl,
                album = f.IdMusicaNavigation.IdAlbumNavigation != null ? f.IdMusicaNavigation.IdAlbumNavigation.Titulo : null,
                dataAdicao = f.DataFavorito
            })
            .ToListAsync();
        return Ok(musicas);
    }

    [HttpGet("albuns")]
    public async Task<IActionResult> ObterAlbunsFavoritos()
    {
        int usuarioId = ObterUsuarioIdLogado();
        var albuns = await _context.Favoritos
            .Where(f => f.IdUsuario == usuarioId)
            .Include(f => f.IdAlbumNavigation).ThenInclude(a => a.IdArtistaNavigation)
            .OrderByDescending(f => f.DataFavorito)
            .Select(f => new
            {
                idAlbum = f.IdAlbum,
                titulo = f.IdAlbumNavigation.Titulo,
                capa = f.IdAlbumNavigation.Capa,
                tipo = f.IdAlbumNavigation.Tipo,
                nomeArtista = f.IdAlbumNavigation.IdArtistaNavigation.NomeArtista,
                dataAdicao = f.DataFavorito
            })
            .ToListAsync();
        return Ok(albuns);
    }

    [HttpGet("playlists")]
    public async Task<IActionResult> ObterPlaylistsDoUsuario()
    {
        int usuarioId = ObterUsuarioIdLogado();
        var playlists = await _context.Playlists
            .Where(p => p.IdUsuario == usuarioId)
            .Include(p => p.PlaylistMusicas)
            .OrderByDescending(p => p.DataCriacao)
            .Select(p => new
            {
                idPlaylist = p.IdPlaylist,
                nome = p.Nome,
                descricao = p.Descricao,
                capa = p.Capa,
                quantidadeMusicas = p.PlaylistMusicas.Count,
                publica = p.Publica,
                dataCriacao = p.DataCriacao
            })
            .ToListAsync();
        return Ok(playlists);
    }

    [HttpGet("artistas")]
    public async Task<IActionResult> ObterArtistasSeguidos()
    {
        int usuarioId = ObterUsuarioIdLogado();
        var artistas = await _context.SeguirArtista
            .Where(s => s.IdUsuario == usuarioId)
            .Include(s => s.IdArtistaNavigation)
            .OrderByDescending(s => s.DataSeguimento)
            .Select(s => new
            {
                idArtista = s.IdArtista,
                nomeArtista = s.IdArtistaNavigation.NomeArtista,
                foto = s.IdArtistaNavigation.Foto,
                dataSeguimento = s.DataSeguimento
            })
            .ToListAsync();
        return Ok(artistas);
    }

    [HttpGet("resumo")]
    public async Task<IActionResult> ObterResumo()
    {
        int usuarioId = ObterUsuarioIdLogado();
        var resumo = new
        {
            artistas = await _context.SeguirArtista.CountAsync(s => s.IdUsuario == usuarioId),
            musicas = await _context.MusicaFavorita.CountAsync(f => f.IdUsuario == usuarioId),
            albuns = await _context.Favoritos.CountAsync(f => f.IdUsuario == usuarioId),
            playlists = await _context.Playlists.CountAsync(p => p.IdUsuario == usuarioId)
        };
        return Ok(resumo);
    }

    [HttpPost]
    public async Task<IActionResult> Adicionar([FromBody] AdicionarMusicaBibliotecaRequest request)
    {
        int usuarioId = ObterUsuarioIdLogado();

        if (request.IdMusica <= 0)
            return BadRequest(new { Message = "Informe uma Música válida para adicionar à biblioteca." });

        var jaExiste = await _context.Bibliotecas
            .AnyAsync(b => b.IdUsuario == usuarioId && b.IdMusica == request.IdMusica);

        if (jaExiste)
            return BadRequest(new { Message = "Esta música já está na sua biblioteca." });

        var item = new Biblioteca
        {
            IdUsuario = usuarioId,
            IdMusica = request.IdMusica,
            DataAdicao = DateTime.UtcNow
        };

        _context.Bibliotecas.Add(item);
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Item adicionado à biblioteca com sucesso.", IdMusica = item.IdMusica });
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> Remover(int id)
    {
        int usuarioId = ObterUsuarioIdLogado();
        var item = await _context.Bibliotecas
            .FirstOrDefaultAsync(b => b.IdMusica == id && b.IdUsuario == usuarioId);
        if (item == null)
            return NotFound(new { Message = "Item não encontrado na biblioteca." });
        _context.Bibliotecas.Remove(item);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

