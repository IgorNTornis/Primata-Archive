using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimataArchive.Api.Models;
namespace PrimataArchive.Api.Controllers;
[ApiController]
[Route("api/publicacoes")]
public class PostController : ControllerBase
{
    private readonly  PrimataArchiveContext _context;

    public PostController(PrimataArchiveContext context)
    {
        _context = context;
    }

    private int ObterUsuarioIdLogado()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idClaim, out int id) ? id : 0;
    }

    [HttpGet]
    public async Task<IActionResult> ObterPosts([FromQuery] int? idComunidade)
    {
        var query = _context.Posts.Where(p => p.Status == "publicado").AsQueryable();

        if(idComunidade.HasValue)
        query = query.Where(p => p.IdComunidade == idComunidade.Value);

        var posts = await query
            .Include(p => p.IdUsuarioNavigation)
            .Include(p => p.IdComunidadeNavigation)
            .Include(p => p.Comentarios)
            .OrderByDescending(p => p.IdPost)
            .Select(p => new PostResponse(
                p.IdPost,
                p.IdComunidade,
                p.IdComunidadeNavigation != null ? p.IdComunidadeNavigation.Nome : null,
                p.IdUsuario,
                p.IdUsuarioNavigation != null ? p.IdUsuarioNavigation.Nome : null,
                p.Titulo,
                p.Conteudo,
                p.Imagem,
                p.Comentarios.Count,
                p.DataPublicacao,
                p.Status
            ))
            .ToListAsync();
            return Ok(posts);
    }

    [HttpGet("recentes")]
    public async Task<IActionResult> ObterPostsRecentes()
    {
        var posts = await _context.Posts
            .Include(p => p.IdUsuarioNavigation)
            .Include(p => p.IdComunidadeNavigation)
            .Include(p => p.Comentarios)
            .Where(p => p.Status == "publicado")
            .OrderByDescending(p => p.DataPublicacao)
            .Take(30)
            .Select(p => new PostResponse(
                p.IdPost, p.IdComunidade, p.IdComunidadeNavigation != null ? p.IdComunidadeNavigation.Nome : null,
                p.IdUsuario, p.IdUsuarioNavigation.Nome, p.Titulo, p.Conteudo, p.Imagem,
                p.Comentarios.Count, p.DataPublicacao, p.Status))
            .ToListAsync();
        return Ok(posts);
    }

    [HttpGet("{id:int}/comentarios")]
    public async Task<IActionResult> ObterComentarios(int id)
    {
        var usuarioId = ObterUsuarioIdLogado();
        var existe = await _context.Posts.AnyAsync(p => p.IdPost == id && (p.Status == "publicado" || p.IdUsuario == usuarioId));
        if (!existe) return NotFound(new { Message = "Publicação não encontrada." });

        var comentarios = await _context.Comentarios
            .Where(c => c.IdPost == id)
            .Include(c => c.IdUsuarioNavigation)
            .OrderBy(c => c.DataComentario)
            .Select(c => new ComentarioResponse(
                c.IdComentario, c.IdPost, c.IdUsuario,
                c.IdUsuarioNavigation.Nome, c.Conteudo, c.DataComentario))
            .ToListAsync();
        return Ok(comentarios);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObterPostPorId (int id)
    {
        var usuarioId = ObterUsuarioIdLogado();
        var post = await _context.Posts
        .Include(p => p.IdUsuarioNavigation)
        .Include(p => p.IdComunidadeNavigation)
        .Include(p => p.Comentarios)
        .ThenInclude(c => c.IdUsuarioNavigation)
        .FirstOrDefaultAsync(p => p.IdPost == id && (p.Status == "publicado" || p.IdUsuario == usuarioId));
        if(post == null)
        return NotFound(new {Message = "Post não encontrado."});
        return Ok(new PostResponse(
            post.IdPost,
            post.IdComunidade,
            post.IdComunidadeNavigation?.Nome,
            post.IdUsuario,
            post.IdUsuarioNavigation?.Nome,
            post.Titulo,
            post.Conteudo,
            post.Imagem,
            post.Comentarios.Count,
            post.DataPublicacao,
            post.Status
        ));
    }
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CriarPost([FromBody] CriarPostRequest request)
    {
        int usuarioId = ObterUsuarioIdLogado();

        if (request.idComunidade.HasValue)
        {
            bool comunidadeExiste = await _context.Comunidades
                .AnyAsync(c => c.IdComunidade == request.idComunidade.Value);

            if (!comunidadeExiste)
                return BadRequest(new { Message = "Comunidade informada não existe." });

            var comunidade = await _context.Comunidades
                .Include(c => c.ComunidadeMembros)
                .FirstAsync(c => c.IdComunidade == request.idComunidade.Value);
            if (comunidade.Privacidade == "privada" &&
                comunidade.IdUsuarioCriador != usuarioId &&
                !comunidade.ComunidadeMembros.Any(m => m.IdUsuario == usuarioId))
                return Forbid();
        }
        var post = new Post
        {
            IdUsuario = usuarioId,
            IdComunidade = request.idComunidade,
            Titulo = request.Titulo,
            Conteudo = request.Conteudo,
            Imagem = request.Imagem,
            Status = string.Equals(request.Status, "RASCUNHO", StringComparison.OrdinalIgnoreCase) ? "rascunho" : "publicado",
            DataPublicacao = DateTime.UtcNow
        };
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();
        var response = new PostResponse(
            post.IdPost,
            post.IdComunidade,
            null,
            post.IdUsuario,
            User.Identity?.Name,
            post.Titulo,
            post.Conteudo,
            post.Imagem,
            0,
            post.DataPublicacao,
            post.Status
        );
        return CreatedAtAction(nameof(ObterPostPorId), new { id = post.IdPost }, response);
    }

   [HttpPost("{id}/comentarios")]
    [Authorize]
    public async Task<IActionResult> AdicionarComentario(int id, [FromBody] CriarComentarioRequest request)
    {
        int usuarioId = ObterUsuarioIdLogado();
        bool postExiste = await _context.Posts.AnyAsync(p => p.IdPost == id && (p.Status == "publicado" || p.IdUsuario == usuarioId));
        if(!postExiste)
        return NotFound(new {Message = "Post não encontrado."});
        var comentario = new Comentario
        {
            IdPost = id,
            IdUsuario = usuarioId,
            Conteudo = request.Conteudo,
            DataComentario = DateTime.UtcNow
        };
        _context.Comentarios.Add(comentario);
        await _context.SaveChangesAsync();
        return Ok(new {Message = "Comentário adicionado com sucesso.", IdComentario = comentario.IdComentario});
    }
}