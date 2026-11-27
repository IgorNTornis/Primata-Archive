using System.ComponentModel.DataAnnotations;

namespace PrimataArchive.Api.Models;

public record RegistrarRequest(
    string? Nome,
    string? Username,
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Senha)
{
    public string NomeFinal => (Nome ?? Username ?? string.Empty).Trim();
}
