using EfCoreWebApp.Data;
using EfCoreWebApp.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer("Server=localhost;Database=MyWebAppDb;User ID=sa;Password=YourStrong@Password;TrustServerCertificate=True;"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.MapGet("/", () =>
{
    var html = """
    <!doctype html>
    <html>
    <head><meta charset="utf-8"><title>Home</title></head>
    <body>
      <h1>Home</h1>
      <a href="/products">Products</a>
    </body>
    </html>
    """;
    return Results.Content(html, "text/html");
});

app.MapGet("/products", () =>
{
    var html = """
    <!doctype html>
    <html>
    <head><meta charset="utf-8"><title>Products</title></head>
    <body>
      <h1>Products List</h1>
    </body>
    </html>
    """;
    return Results.Content(html, "text/html");
});

app.MapGet("/todos", async (AppDbContext db) => await db.Todos.ToListAsync());

app.MapGet("/todos/{id:int}", async (int id, AppDbContext db) =>
{
    var todo = await db.Todos.FindAsync(id);
    return todo is null ? Results.NotFound() : Results.Ok(todo);
});

app.MapPost("/todos", async (Todo todo, AppDbContext db) =>
{
    db.Todos.Add(todo);
    await db.SaveChangesAsync();
    return Results.Created($"/todos/{todo.Id}", todo);
});

app.MapPut("/todos/{id:int}", async (int id, Todo input, AppDbContext db) =>
{
    var todo = await db.Todos.FindAsync(id);
    if (todo is null) return Results.NotFound();
    todo.Title = input.Title;
    todo.IsDone = input.IsDone;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/todos/{id:int}", async (int id, AppDbContext db) =>
{
    var todo = await db.Todos.FindAsync(id);
    if (todo is null) return Results.NotFound();
    db.Todos.Remove(todo);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();

public partial class Program { }
