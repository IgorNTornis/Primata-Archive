using System.ComponentModel.DataAnnotations;

namespace PrimataArchive.Api.Models;

public record ComentarioFrontendRequest(
    int? PostId,
    int? PublicacaoId,
    int? IdPost,
    [Required, MinLength(1)] string Conteudo)
{
    public int? PostAlvo => PostId ?? PublicacaoId ?? IdPost;
}
