using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimataArchive.Api.Models;

namespace PrimataArchive.Api.Controllers;

[ApiController]
[Route("api/comunidades")]
public class ComunidadeController : ControllerBase
{
    private readonly PrimataArchiveContext _context;

    public ComunidadeController(PrimataArchiveContext context)
    {
        _context = context;
    }

    private int ObterUsuarioIdLogado()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idClaim, out int id) ? id : 0;
    }

    [HttpGet]
    public async Task<IActionResult> ObterComunidades()
    {
        int usuarioId = ObterUsuarioIdLogado();
        var comunidades = await _context.Comunidades
            .Where(c => c.Privacidade != "privada" || c.IdUsuarioCriador == usuarioId || c.ComunidadeMembros.Any(m => m.IdUsuario == usuarioId))
            .Include(c => c.IdUsuarioCriadorNavigation)
            .Include(c => c.ComunidadeMembros)
            .Select(c => new ComunidadeResponse(
                c.IdComunidade,
                c.IdUsuarioCriador,
                c.IdUsuarioCriadorNavigation != null ? c.IdUsuarioCriadorNavigation.Nome : null,
                c.Nome,
                c.Descricao,
                c.Foto,
                c.ComunidadeMembros.Count,
                c.DataCriacao,
                c.Identificador,
                c.Categoria,
                c.Privacidade
            ))
            .ToListAsync();

        return Ok(comunidades);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObterComunidadePorId(int id)
    {
        var comunidade = await _context.Comunidades
            .Include(c => c.IdUsuarioCriadorNavigation)
            .Include(c => c.ComunidadeMembros)
            .FirstOrDefaultAsync(c => c.IdComunidade == id);

        if (comunidade == null)
            return NotFound(new { Message = "Comunidade não encontrada" });

        int usuarioIdLogado = ObterUsuarioIdLogado();
        bool ehMembro = comunidade.ComunidadeMembros.Any(cm => cm.IdUsuario == usuarioIdLogado);
        if (comunidade.Privacidade == "privada" && comunidade.IdUsuarioCriador != usuarioIdLogado && !ehMembro && !User.IsInRole("admin"))
            return NotFound(new { Message = "Comunidade não encontrada." });

        return Ok(new
        {
            idComunidade = comunidade.IdComunidade,
            idUsuarioCriador = comunidade.IdUsuarioCriador,
            nomeCriador = comunidade.IdUsuarioCriadorNavigation?.Nome,
            nome = comunidade.Nome,
            descricao = comunidade.Descricao,
            foto = comunidade.Foto,
            identificador = comunidade.Identificador,
            categoria = comunidade.Categoria,
            privacidade = comunidade.Privacidade,
            totalMembros = comunidade.ComunidadeMembros.Count,
            dataCriacao = comunidade.DataCriacao,
            ehMembro
        });
    }


    [HttpGet("{id:int}/publicacoes")]
    public async Task<IActionResult> ObterPublicacoesDaComunidade(int id)
    {
        int usuarioId = ObterUsuarioIdLogado();
        var comunidade = await _context.Comunidades
            .Include(c => c.ComunidadeMembros)
            .FirstOrDefaultAsync(c => c.IdComunidade == id);
        if (comunidade == null) return NotFound(new { Message = "Comunidade não encontrada." });
        bool ehMembro = comunidade.ComunidadeMembros.Any(m => m.IdUsuario == usuarioId);
        if (comunidade.Privacidade == "privada" && comunidade.IdUsuarioCriador != usuarioId && !ehMembro && !User.IsInRole("admin"))
            return NotFound(new { Message = "Comunidade não encontrada." });

        var posts = await _context.Posts
            .Where(p => p.IdComunidade == id && p.Status == "publicado")
            .Include(p => p.IdUsuarioNavigation)
            .Include(p => p.Comentarios)
            .OrderByDescending(p => p.DataPublicacao)
            .Select(p => new PostResponse(
                p.IdPost, p.IdComunidade, p.IdComunidadeNavigation != null ? p.IdComunidadeNavigation.Nome : null,
                p.IdUsuario, p.IdUsuarioNavigation.Nome, p.Titulo, p.Conteudo, p.Imagem,
                p.Comentarios.Count, p.DataPublicacao, p.Status))
            .ToListAsync();
        return Ok(posts);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CriarComunidade([FromBody] CriarComunidadeRequest request)
    {
        int usuarioId = ObterUsuarioIdLogado();

        var nome = request.Nome.Trim();
        var identificador = (request.Identificador ?? nome)
            .Trim().TrimStart('@').ToLowerInvariant()
            .Replace(" ", "-");
        if (string.IsNullOrWhiteSpace(identificador))
            return BadRequest(new { Message = "Informe um identificador para a comunidade." });

        if (await _context.Comunidades.AnyAsync(c => c.Nome == nome))
            return Conflict(new { Message = "Já existe uma comunidade com esse nome." });
        if (await _context.Comunidades.AnyAsync(c => c.Identificador == identificador))
            return Conflict(new { Message = "Este identificador já está em uso." });

        var comunidade = new Comunidade
        {
            IdUsuarioCriador = usuarioId,
            Nome = nome,
            Identificador = identificador,
            Categoria = request.Categoria,
            Privacidade = string.Equals(request.Privacidade, "privada", StringComparison.OrdinalIgnoreCase) ? "privada" : "publica",
            Descricao = request.Descricao,
            Foto = request.Foto,
            DataCriacao = DateTime.UtcNow
        };

        _context.Comunidades.Add(comunidade);
        await _context.SaveChangesAsync();

        var membroCriador = new ComunidadeMembro
        {
            IdComunidade = comunidade.IdComunidade,
            IdUsuario = usuarioId,
            DataIngresso = DateTime.UtcNow
        };

        _context.ComunidadeMembros.Add(membroCriador);
        await _context.SaveChangesAsync();

        var response = new ComunidadeResponse(
            comunidade.IdComunidade,
            comunidade.IdUsuarioCriador,
            User.Identity?.Name,
            comunidade.Nome,
            comunidade.Descricao,
            comunidade.Foto,
            1,
            comunidade.DataCriacao,
            comunidade.Identificador,
            comunidade.Categoria,
            comunidade.Privacidade
        );

        return CreatedAtAction(nameof(ObterComunidadePorId), new { id = comunidade.IdComunidade }, response);
    }

    [HttpPost("{id}/entrar")]
    [Authorize]
    public async Task<IActionResult> EntrarNaComunidade(int id)
    {
        int usuarioId = ObterUsuarioIdLogado();

        var comunidade = await _context.Comunidades.FindAsync(id);
        if (comunidade == null)
            return NotFound(new { Message = "Comunidade não encontrada." });
        if (comunidade.Privacidade == "privada")
            return Forbid();

        bool jaEhMembro = await _context.ComunidadeMembros
            .AnyAsync(cm => cm.IdComunidade == id && cm.IdUsuario == usuarioId);

        if (jaEhMembro)
            return BadRequest(new { Message = "Você já é membro dessa comunidade." });

        var membro = new ComunidadeMembro
        {
            IdComunidade = id,
            IdUsuario = usuarioId,
            DataIngresso = DateTime.UtcNow
        };

        _context.ComunidadeMembros.Add(membro);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Você entrou na comunidade com sucesso." });
    }

    [HttpDelete("{id}/sair")]
    [Authorize]
    public async Task<IActionResult> SairDaComunidade(int id)
    {
        int usuarioId = ObterUsuarioIdLogado();

        var membro = await _context.ComunidadeMembros
            .FirstOrDefaultAsync(cm => cm.IdComunidade == id && cm.IdUsuario == usuarioId);

        if (membro == null)
            return BadRequest(new { Message = "Você não é membro desta comunidade." });

        _context.ComunidadeMembros.Remove(membro);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}