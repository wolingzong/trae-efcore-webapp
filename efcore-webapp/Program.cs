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
    
    // Add sample data if no products exist
    if (!db.Products.Any())
    {
        db.Products.AddRange(
            new Product { Name = "Laptop", Price = 999.99m },
            new Product { Name = "Mouse", Price = 29.99m },
            new Product { Name = "Keyboard", Price = 79.99m }
        );
        await db.SaveChangesAsync();
    }
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

app.MapGet("/products", async (AppDbContext db) =>
{
    var products = await db.Products.ToListAsync();
    var productRows = string.Join("", products.Select(p => 
        $"<tr><td>{p.Id}</td><td>{p.Name}</td><td>${p.Price:F2}</td></tr>"));
    
    var html = $"""
    <!doctype html>
    <html>
    <head><meta charset="utf-8"><title>Products</title></head>
    <body>
      <h1>Products List</h1>
      <a href="/">← Back to Home</a>
      <h2>Add New Product</h2>
      <form action="/products" method="post">
        <input type="text" name="name" placeholder="Product Name" required>
        <input type="number" name="price" step="0.01" placeholder="Price" required>
        <button type="submit">Add Product</button>
      </form>
      <h2>Current Products</h2>
      <table border="1" style="border-collapse: collapse; width: 100%;">
        <thead>
          <tr><th>ID</th><th>Name</th><th>Price</th></tr>
        </thead>
        <tbody>
          {productRows}
        </tbody>
      </table>
      {(products.Count == 0 ? "<p>No products found. Add some products above!</p>" : "")}
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

// Products API
app.MapGet("/api/products", async (AppDbContext db) => await db.Products.ToListAsync());

app.MapPost("/products", async (HttpContext context, AppDbContext db) =>
{
    var form = await context.Request.ReadFormAsync();
    var name = form["name"].ToString();
    var priceStr = form["price"].ToString();
    
    if (string.IsNullOrEmpty(name) || !decimal.TryParse(priceStr, out var price))
    {
        return Results.BadRequest("Invalid product data");
    }
    
    var product = new Product { Name = name, Price = price };
    db.Products.Add(product);
    await db.SaveChangesAsync();
    
    return Results.Redirect("/products");
});

app.MapPost("/api/products", async (Product product, AppDbContext db) =>
{
    db.Products.Add(product);
    await db.SaveChangesAsync();
    return Results.Created($"/api/products/{product.Id}", product);
});

app.Run();

public partial class Program { }
