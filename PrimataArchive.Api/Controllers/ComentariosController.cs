using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimataArchive.Api.Models;

namespace PrimataArchive.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/comentarios")]
public class ComentariosController : ControllerBase
{
    private readonly PrimataArchiveContext _context;

    public ComentariosController(PrimataArchiveContext context) => _context = context;

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] ComentarioFrontendRequest request)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var usuarioId))
            return Unauthorized();

        if (request.PostAlvo is not int postId || postId <= 0)
            return BadRequest(new { Message = "Informe o ID da publicação." });

        var postExiste = await _context.Posts.AnyAsync(p => p.IdPost == postId && (p.Status == "publicado" || p.IdUsuario == usuarioId));
        if (!postExiste) return NotFound(new { Message = "Publicação não encontrada." });

        var comentario = new Comentario
        {
            IdPost = postId,
            IdUsuario = usuarioId,
            Conteudo = request.Conteudo.Trim(),
            DataComentario = DateTime.UtcNow
        };

        _context.Comentarios.Add(comentario);
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Comentário publicado.", IdComentario = comentario.IdComentario });
    }
}
