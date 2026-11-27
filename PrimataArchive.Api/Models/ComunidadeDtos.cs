using System.ComponentModel.DataAnnotations;

namespace PrimataArchive.Api.Models;

public record CriarComunidadeRequest
(
    [Required(ErrorMessage = "O nome da comunidade é obrigatório.")]
    [StringLength(100)] string Nome,
    string? Descricao,
    string? Foto,
    [StringLength(100)] string? Identificador = null,
    [StringLength(50)] string? Categoria = null,
    string? Privacidade = "publica"
);

public record ComunidadeResponse
(
    int IdComunidade,
    int? IdUsuarioCriador,
    string? NomeCriador,
    string Nome,
    string? Descricao,
    string? Foto,
    int TotalMembros,
    DateTime? DataCriacao,
    [StringLength(100)] string? Identificador = null,
    [StringLength(50)] string? Categoria = null,
    string? Privacidade = "publica"
);