namespace Tracker.Cli.Tracking;

using System.Text.Json;
using System.Text.Json.Serialization;

// Typed shape of the /v1/sro-rastro payload, limited to what the view renders.
internal sealed record TrackingReport(
    [property: JsonPropertyName("objetos")] IReadOnlyList<TrackedObject>? Objetos,
    [property: JsonPropertyName("quantidade")] int Quantidade,
    [property: JsonPropertyName("resultado")] string? Resultado,
    [property: JsonPropertyName("versao")] string? Versao)
{
    static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static TrackingReport? Parse(string json) =>
        JsonSerializer.Deserialize<TrackingReport>(json, Options);
}

internal sealed record TrackedObject(
    [property: JsonPropertyName("codObjeto")] string? CodObjeto,
    [property: JsonPropertyName("dtPrevista")] DateTime? DtPrevista,
    [property: JsonPropertyName("bloqueioObjeto")] bool BloqueioObjeto,
    [property: JsonPropertyName("tipoPostal")] PostalType? TipoPostal,
    [property: JsonPropertyName("eventos")] IReadOnlyList<TrackingEvent>? Eventos);

internal sealed record PostalType(
    [property: JsonPropertyName("categoria")] string? Categoria,
    [property: JsonPropertyName("descricao")] string? Descricao,
    [property: JsonPropertyName("sigla")] string? Sigla);

internal sealed record TrackingEvent(
    [property: JsonPropertyName("codigo")] string? Codigo,
    [property: JsonPropertyName("descricao")] string? Descricao,
    [property: JsonPropertyName("detalhe")] string? Detalhe,
    [property: JsonPropertyName("dtHrCriado")] DateTime? DtHrCriado,
    [property: JsonPropertyName("tipo")] string? Tipo,
    [property: JsonPropertyName("unidade")] TrackingUnit? Unidade,
    [property: JsonPropertyName("unidadeDestino")] TrackingUnit? UnidadeDestino);

internal sealed record TrackingUnit(
    [property: JsonPropertyName("nome")] string? Nome,
    [property: JsonPropertyName("tipo")] string? Tipo,
    [property: JsonPropertyName("endereco")] UnitAddress? Endereco);

internal sealed record UnitAddress(
    [property: JsonPropertyName("cidade")] string? Cidade,
    [property: JsonPropertyName("uf")] string? Uf);
