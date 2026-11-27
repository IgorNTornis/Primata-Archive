using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimataArchive.Api.Models;

namespace PrimataArchive.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/lancamentos")]
public class LancamentosController : ControllerBase
{
    private readonly PrimataArchiveContext _context;
    private readonly IWebHostEnvironment _environment;

    public LancamentosController(PrimataArchiveContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [HttpPost]
    [RequestSizeLimit(60_000_000)]
    public async Task<IActionResult> Criar([FromForm] CriarLancamentoForm request)
    {
        if (string.IsNullOrWhiteSpace(request.Titulo))
            return BadRequest(new { Message = "Informe o título do lançamento." });
        if (request.Audio == null || request.Audio.Length == 0)
            return BadRequest(new { Message = "Selecione um arquivo de áudio." });
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var usuarioId))
            return Unauthorized();

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == usuarioId);
        if (usuario == null) return Unauthorized();

        var artista = await _context.Artista.FirstOrDefaultAsync(a => a.IdUsuario == usuarioId);
        if (artista == null)
        {
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
        }

        var uploadFolder = Path.Combine(_environment.ContentRootPath, "wwwroot", "uploads");
        Directory.CreateDirectory(uploadFolder);

        string? capaUrl = null;
        if (request.Capa is { Length: > 0 })
        {
            var extension = Path.GetExtension(request.Capa.FileName).ToLowerInvariant();
            if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(extension))
                return BadRequest(new { Message = "A capa precisa ser JPG, PNG ou WEBP." });
            var fileName = $"{Guid.NewGuid():N}{extension}";
            await using var stream = System.IO.File.Create(Path.Combine(uploadFolder, fileName));
            await request.Capa.CopyToAsync(stream);
            capaUrl = "/uploads/" + fileName;
        }

        var audioExtension = Path.GetExtension(request.Audio.FileName).ToLowerInvariant();
        if (!new[] { ".mp3", ".wav", ".ogg", ".m4a", ".aac" }.Contains(audioExtension))
            return BadRequest(new { Message = "Formato de áudio não suportado. Use MP3, WAV, OGG, M4A ou AAC." });

        var audioName = $"{Guid.NewGuid():N}{audioExtension}";
        await using (var stream = System.IO.File.Create(Path.Combine(uploadFolder, audioName)))
            await request.Audio.CopyToAsync(stream);

        var tipo = request.Tipo.Trim().ToLowerInvariant() switch
        {
            "ep" => "ep",
            "álbum" or "album" => "album",
            _ => "single"
        };

        var album = new Album
        {
            IdArtista = artista.IdArtista,
            Titulo = request.Titulo.Trim(),
            Capa = capaUrl,
            Descricao = request.Descricao,
            Genero = request.Genero,
            DataLancamento = request.DataLancamento,
            Tipo = tipo,
            Status = string.Equals(request.Status, "RASCUNHO", StringComparison.OrdinalIgnoreCase) ? "rascunho" : "publicado",
            DataCadastro = DateTime.UtcNow
        };
        _context.Albums.Add(album);
        await _context.SaveChangesAsync();

        var musica = new Musica
        {
            IdArtista = artista.IdArtista,
            IdAlbum = album.IdAlbum,
            Titulo = request.Titulo.Trim(),
            Genero = request.Genero,
            Capa = capaUrl,
            AudioUrl = "/uploads/" + audioName,
            DataLancamento = request.DataLancamento,
            Reproducoes = 0,
            DataCadastro = DateTime.UtcNow
        };
        _context.Musicas.Add(musica);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = album.Status == "rascunho" ? "Rascunho salvo com sucesso." : "Lançamento publicado com sucesso.",
            idAlbum = album.IdAlbum,
            idMusica = musica.IdMusica,
            audioUrl = musica.AudioUrl,
            capaUrl = musica.Capa
        });
    }
}
