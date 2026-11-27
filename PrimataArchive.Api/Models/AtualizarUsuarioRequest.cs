namespace PrimataArchive.Api.Models;

public record AtualizarUsuarioRequest(
    string? Nome,
    string? Username,
    string? NomeExibicao,
    string? Email,
    string? Bio)
{
    public string? NomeFinal => Nome ?? NomeExibicao ?? Username;
}

public record AtualizarPreferenciaRequest(string? Chave, bool Valor);
