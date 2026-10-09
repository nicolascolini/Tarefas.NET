using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Injeção de dependência: registra o DbContext com SQLite (arquivo tarefas.db)
builder.Services.AddDbContext<AppDb>(o => o.UseSqlite("Data Source=tarefas.db"));

var app = builder.Build();

// Cria o banco/tabelas na primeira execução (sem migrations, para simplificar)
using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<AppDb>().Database.EnsureCreated();

// Serve o front-end estático de wwwroot/index.html
app.UseDefaultFiles();
app.UseStaticFiles();

// Minimal API: um grupo de rotas com prefixo /api/tarefas
var api = app.MapGroup("/api/tarefas");

api.MapGet("/", async (AppDb db) =>
    await db.Tarefas.OrderBy(t => t.Id).ToListAsync());

api.MapPost("/", async (NovaTarefa dto, AppDb db) =>
{
    if (string.IsNullOrWhiteSpace(dto.Titulo))
        return Results.BadRequest(new { erro = "Título é obrigatório." });

    var tarefa = new Tarefa { Titulo = dto.Titulo.Trim() };
    db.Tarefas.Add(tarefa);
    await db.SaveChangesAsync();
    return Results.Created($"/api/tarefas/{tarefa.Id}", tarefa);
});

api.MapPut("/{id:int}/alternar", async (int id, AppDb db) =>
{
    var tarefa = await db.Tarefas.FindAsync(id);
    if (tarefa is null) return Results.NotFound();

    tarefa.Concluida = !tarefa.Concluida;
    await db.SaveChangesAsync();
    return Results.Ok(tarefa);
});

api.MapDelete("/{id:int}", async (int id, AppDb db) =>
{
    var tarefa = await db.Tarefas.FindAsync(id);
    if (tarefa is null) return Results.NotFound();

    db.Tarefas.Remove(tarefa);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();

// ---- Modelos (em projetos maiores, ficam em arquivos separados) ----

record NovaTarefa(string Titulo); // record = DTO imutável com uma linha

class Tarefa
{
    public int Id { get; set; }
    public string Titulo { get; set; } = "";
    public bool Concluida { get; set; }
}

class AppDb(DbContextOptions<AppDb> options) : DbContext(options) // construtor primário (C# 12+)
{
    public DbSet<Tarefa> Tarefas => Set<Tarefa>();
}
