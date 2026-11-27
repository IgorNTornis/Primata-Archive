using Microsoft.AspNetCore.Http;

namespace PrimataArchive.Api.Models;

public class CriarLancamentoForm
{
    public string Titulo { get; set; } = string.Empty;
    public string Tipo { get; set; } = "single";
    public DateOnly? DataLancamento { get; set; }
    public string? Genero { get; set; }
    public string? Descricao { get; set; }
    public string? Status { get; set; }
    public IFormFile? Capa { get; set; }
    public IFormFile? Audio { get; set; }
}
