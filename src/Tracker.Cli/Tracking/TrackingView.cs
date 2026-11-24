namespace Tracker.Cli.Tracking;

using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Spectre.Console;
using Spectre.Console.Rendering;

// Renders a /v1/sro-rastro payload as a shipment timeline.
internal static partial class TrackingView
{
    static readonly string[] Months =
        ["jan", "fev", "mar", "abr", "mai", "jun", "jul", "ago", "set", "out", "nov", "dez"];

    public static void Render(string json)
    {
        TrackingReport? report;
        try
        {
            report = TrackingReport.Parse(json);
        }
        catch (JsonException)
        {
            report = null;
        }

        if (report?.Objetos is not { Count: > 0 } objetos)
        {
            RenderRaw(json);
            return;
        }

        foreach (var obj in objetos)
            RenderObject(obj);
    }

    static void RenderObject(TrackedObject obj)
    {
        AnsiConsole.Write(Header(obj));

        if (obj.BloqueioObjeto)
            AnsiConsole.Write(new Markup("\n[black on red] ⚠  OBJETO BLOQUEADO [/]\n"));

        var events = obj.Eventos ?? [];
        if (events.Count == 0)
        {
            AnsiConsole.Write(new Markup("\n[grey]Sem eventos registrados.[/]\n"));
            return;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[grey]histórico[/]").LeftJustified().RuleStyle("grey35"));
        AnsiConsole.WriteLine();
        AnsiConsole.Write(Timeline(events));
    }

    static Panel Header(TrackedObject obj)
    {
        var grid = new Grid();
        grid.AddColumn(new GridColumn().PadRight(6));
        grid.AddColumn(new GridColumn().RightAligned());

        var code = E(obj.CodObjeto ?? "—");
        var categoria = E(obj.TipoPostal?.Categoria ?? "Encomenda");
        var sigla = obj.TipoPostal?.Sigla is { Length: > 0 } s ? $"  [grey]·[/]  [dim]{E(s)}[/]" : "";
        var left = $"[bold yellow]{code}[/]\n[grey]{categoria}[/]{sigla}";

        var right = obj.DtPrevista is { } due
            ? $"[dim]previsão de entrega[/]\n[bold]{E(DatePt(due))}[/]\n{Eta(due)}"
            : "[dim]sem previsão\nde entrega[/]";

        grid.AddRow(new Markup(left), new Markup(right));

        return new Panel(grid)
            .Header("[bold yellow] ✉  Correios [/]", Justify.Left)
            .RoundedBorder()
            .BorderColor(Color.Yellow)
            .Padding(2, 1);
    }

    static IRenderable Timeline(IReadOnlyList<TrackingEvent> events)
    {
        var rows = new List<IRenderable>();

        for (var i = 0; i < events.Count; i++)
        {
            var ev = events[i];
            var first = i == 0;
            var (color, icon) = Style(ev);
            var when = ev.DtHrCriado ?? default;

            var bullet = first ? $"[{color}]◉[/]" : $"[{color}]●[/]";
            var head = $"{bullet} [bold]{E(DatePt(when))}[/]  [grey]{E(TimePt(when))}[/]";
            if (first)
                head += $"   [black on {color}] agora [/]  [grey]{E(Relative(when))}[/]";
            rows.Add(new Markup(head));

            var desc = E(Clean(ev.Descricao));
            var descMarkup = first ? $"[bold {color}]{desc}[/]" : $"[{color}]{desc}[/]";
            rows.Add(new Markup($"[grey35]│[/]  {icon}  {descMarkup}"));

            var route = Route(ev);
            if (route.Length > 0)
                rows.Add(new Markup($"[grey35]│[/]      [grey]{E(route)}[/]"));

            var detalhe = Clean(ev.Detalhe);
            if (detalhe.Length > 0)
                rows.Add(new Markup($"[grey35]│[/]      [grey53 italic]{E(detalhe)}[/]"));

            if (i < events.Count - 1)
                rows.Add(new Markup("[grey35]│[/]"));
        }

        return new Rows(rows);
    }

    static (string Color, string Icon) Style(TrackingEvent ev)
    {
        var d = ev.Descricao ?? "";
        return ev.Codigo switch
        {
            "PO" => ("steelblue1", "📮"),
            "RO" => ("deepskyblue1", "🚚"),
            "OEC" => ("dodgerblue1", "🛵"),
            "BDE" or "BDI" or "BDR" => ("green", "📬"),
            "PAR" when d.Contains("recebido pelos Correios", StringComparison.OrdinalIgnoreCase)
                => ("green", "🇧🇷"),
            "PAR" when d.Contains("Centro Internacional", StringComparison.OrdinalIgnoreCase)
                => ("aqua", "🛫"),
            "PAR" when d.Contains("autorizada", StringComparison.OrdinalIgnoreCase)
                    || d.Contains("concluída", StringComparison.OrdinalIgnoreCase)
                => ("green", "🛃"),
            "PAR" when d.Contains("análise", StringComparison.OrdinalIgnoreCase)
                => ("yellow", "📋"),
            "PAR" => ("yellow", "🛃"),
            _ => ("grey", "📦"),
        };
    }

    static string Route(TrackingEvent ev)
    {
        var origin = Place(ev.Unidade);
        var dest = Place(ev.UnidadeDestino);
        var line = dest.Length > 0 && dest != origin ? $"{origin}  →  {dest}" : origin;

        var kind = ev.Unidade?.Tipo;
        if (!string.IsNullOrWhiteSpace(kind) && kind != origin)
            line = line.Length > 0 ? $"{line}  ·  {kind}" : kind;

        return line;
    }

    static string Place(TrackingUnit? u)
    {
        if (u is null)
            return "";
        if (!string.IsNullOrWhiteSpace(u.Endereco?.Cidade))
            return string.IsNullOrWhiteSpace(u.Endereco.Uf)
                ? u.Endereco.Cidade!
                : $"{u.Endereco.Cidade}/{u.Endereco.Uf}";
        return u.Nome ?? "";
    }

    static string Eta(DateTime due)
    {
        var days = (due.Date - DateTime.Now.Date).Days;
        return days switch
        {
            < 0 => "[red]prazo vencido[/]",
            0 => "[green]prevista para hoje[/]",
            1 => "[green]em 1 dia[/]",
            _ => $"[green]em {days} dias[/]",
        };
    }

    static string Relative(DateTime when)
    {
        var span = DateTime.Now - when;
        if (span < TimeSpan.Zero)
            span = TimeSpan.Zero;
        if (span.TotalMinutes < 2)
            return "agora há pouco";
        if (span.TotalMinutes < 60)
            return $"há {(int)span.TotalMinutes} min";
        if (span.TotalHours < 24)
            return $"há {(int)span.TotalHours} h";
        var days = (int)span.TotalDays;
        return days == 1 ? "há 1 dia" : $"há {days} dias";
    }

    static string DatePt(DateTime d) =>
        d == default ? "—" : $"{d.Day} {Months[d.Month - 1]} {d.Year}";

    static string TimePt(DateTime d) =>
        d == default ? "" : d.ToString("HH:mm", CultureInfo.InvariantCulture);

    static string Clean(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return "";
        var text = WebUtility.HtmlDecode(TagRegex().Replace(html, ""));
        return WhitespaceRegex().Replace(text, " ").Trim();
    }

    static void RenderRaw(string json)
    {
        var body = string.IsNullOrWhiteSpace(json) ? "(resposta vazia)" : json;
        AnsiConsole.Write(new Panel(new Text(body))
            .Header("[red] resposta não reconhecida [/]")
            .RoundedBorder()
            .BorderColor(Color.Red));
    }

    static string E(string? value) => Markup.Escape(value ?? "");

    [GeneratedRegex("<.*?>", RegexOptions.Singleline)]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
