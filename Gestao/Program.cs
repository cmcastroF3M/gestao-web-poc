using System.Diagnostics;
using System.Reflection;
using Gestao;

const string endereco = "http://127.0.0.1:5080";

var ligacao = BaseDados.ObterLigacao();
try
{
    BaseDados.Garantir(ligacao);
}
catch (Exception ex)
{
    Console.WriteLine("Não foi possível ligar ao SQL Server.");
    Console.WriteLine(ex.Message);
    Console.WriteLine();
    Console.WriteLine("Verifique se o SQL Server Express está instalado e a funcionar.");
    Console.WriteLine("Prima uma tecla para fechar.");
    Console.ReadKey();
    return;
}

string html;
using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("index.html")!)
using (var r = new StreamReader(s))
    html = r.ReadToEnd();

var repo = new ClienteRepositorio(ligacao);

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(endereco);
builder.Logging.SetMinimumLevel(LogLevel.Warning);
var app = builder.Build();

app.MapGet("/", () => Results.Content(html, "text/html; charset=utf-8"));

app.MapGet("/api/clientes", (string? q) => repo.Listar(q));

app.MapPost("/api/clientes", (Cliente c) =>
{
    try
    {
        c.Id = repo.Adicionar(c);
        return Results.Ok(c);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { erro = ex.Message });
    }
});

app.MapPut("/api/clientes/{id:long}", (long id, Cliente c) =>
{
    c.Id = id;
    try
    {
        return repo.Atualizar(c) ? Results.Ok(c) : Results.NotFound();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { erro = ex.Message });
    }
});

app.MapDelete("/api/clientes/{id:long}", (long id) =>
    repo.Remover(id) ? Results.NoContent() : Results.NotFound());

app.Lifetime.ApplicationStarted.Register(() =>
{
    Console.WriteLine("Gestão de Clientes a funcionar em " + endereco);
    Console.WriteLine("Feche esta janela para terminar a aplicação.");
    try { Process.Start(new ProcessStartInfo(endereco) { UseShellExecute = true }); }
    catch { }
});

try
{
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine("Não foi possível iniciar o servidor (a porta 5080 pode estar ocupada).");
    Console.WriteLine(ex.Message);
    Console.WriteLine("Prima uma tecla para fechar.");
    Console.ReadKey();
}
