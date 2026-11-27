using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimataArchive.Api.Models;

namespace PrimataArchive.Api.Controllers;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly PrimataArchiveContext _context;
    public AdminController(PrimataArchiveContext context) => _context = context;

    [HttpGet("usuarios")]
    public async Task<IActionResult> Usuarios()
    {
        var data = await _context.Usuarios.OrderByDescending(u => u.DataCadastro)
            .Select(u => new { idUsuario = u.IdUsuario, nome = u.Nome, email = u.Email, perfil = u.TipoUsuario, dataCadastro = u.DataCadastro })
            .ToListAsync();
        return Ok(data);
    }

    [HttpGet("conteudos")]
    public async Task<IActionResult> Conteudos()
    {
        var musicas = await _context.Musicas.Include(m => m.IdArtistaNavigation)
            .OrderByDescending(m => m.DataCadastro)
            .Select(m => new { id = m.IdMusica, titulo = m.Titulo, tipo = "Música", autor = m.IdArtistaNavigation.NomeArtista, reproducoes = m.Reproducoes, dataCadastro = m.DataCadastro })
            .ToListAsync();
        return Ok(musicas);
    }

    [HttpGet("denuncias")]
    public async Task<IActionResult> Denuncias()
    {
        var data = await _context.Denuncia.Include(d => d.IdUsuarioNavigation)
            .OrderByDescending(d => d.DataDenuncia)
            .Select(d => new
            {
                idDenuncia = d.IdDenuncia,
                idUsuario = d.IdUsuario,
                usuario = d.IdUsuarioNavigation.Nome,
                motivo = d.Motivo,
                descricao = d.Descricao,
                status = d.Status,
                dataDenuncia = d.DataDenuncia,
                idPost = d.IdPost,
                idComentario = d.IdComentario
            })
            .ToListAsync();
        return Ok(data);
    }

    [HttpPatch("denuncias/{id:int}")]
    public async Task<IActionResult> AtualizarDenuncia(int id, [FromBody] AtualizarDenunciaRequest request)
    {
        var status = request.Status.Trim().ToLowerInvariant();
        var permitidos = new[] { "pendente", "analisando", "resolvida", "recusada" };
        if (!permitidos.Contains(status))
            return BadRequest(new { Message = "Status inválido." });

        var denuncia = await _context.Denuncia.FindAsync(id);
        if (denuncia == null) return NotFound(new { Message = "Denúncia não encontrada." });
        denuncia.Status = status;
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Denúncia atualizada." });
    }

    [HttpGet("comunidades")]
    public async Task<IActionResult> Comunidades()
    {
        var data = await _context.Comunidades.Include(c => c.ComunidadeMembros)
            .OrderBy(c => c.Nome)
            .Select(c => new { idComunidade = c.IdComunidade, nome = c.Nome, identificador = c.Identificador, categoria = c.Categoria, privacidade = c.Privacidade, descricao = c.Descricao, totalMembros = c.ComunidadeMembros.Count, dataCriacao = c.DataCriacao })
            .ToListAsync();
        return Ok(data);
    }


    [HttpGet("comunidades/resumo")]
    public async Task<IActionResult> ResumoComunidades()
    {
        return Ok(new
        {
            totalComunidades = await _context.Comunidades.CountAsync(),
            totalMembros = await _context.ComunidadeMembros.CountAsync(),
            totalPublicacoes = await _context.Posts.CountAsync(p => p.IdComunidade != null)
        });
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        return Ok(new
        {
            usuarios = await _context.Usuarios.CountAsync(),
            artistas = await _context.Artista.CountAsync(),
            musicas = await _context.Musicas.CountAsync(),
            albuns = await _context.Albums.CountAsync(),
            comunidades = await _context.Comunidades.CountAsync(),
            denunciasPendentes = await _context.Denuncia.CountAsync(d => d.Status == "pendente")
        });
    }
}
