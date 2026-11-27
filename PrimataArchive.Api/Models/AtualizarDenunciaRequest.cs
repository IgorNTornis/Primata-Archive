using System.ComponentModel.DataAnnotations;

namespace PrimataArchive.Api.Models;

public record AtualizarDenunciaRequest(
    [Required] string Status);
