Imports EfCoreWebApp.Data
Imports EfCoreWebApp.Models
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.AspNetCore.Builder
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.AspNetCore.Http
Imports Microsoft.Extensions.Configuration

Namespace EfCoreWebApp
    Public Class Program
        Public Shared Sub Main(args As String())
            Dim builder = WebApplication.CreateBuilder(args)

            Dim conn = "Server=localhost;Database=MyWebAppDb;User ID=sa;Password=YourStrong@Password;TrustServerCertificate=True;"
            builder.Services.AddScoped(Of AppDbContext)(
                Function(sp) New AppDbContext(
                    New DbContextOptionsBuilder(Of AppDbContext)().
                        UseSqlServer(conn).
                        Options
                )
            )

            builder.Services.AddRazorPages().AddRazorRuntimeCompilation()
            Dim app = builder.Build()

            Using scope = app.Services.CreateScope()
                Dim db = scope.ServiceProvider.GetRequiredService(Of AppDbContext)()
                Try
                    db.Database.EnsureCreated()
                    If Not db.Products.Any() Then
                        db.Products.Add(New Product With {.Name = "qq", .Price = 4.0D})
                        db.SaveChanges()
                    End If
                Catch
                End Try
            End Using

            app.MapGet("/todos", Async Function(db As AppDbContext)
                                     Return Await db.Todos.ToListAsync()
                                 End Function)

            app.MapGet("/todos/{id:int}", Async Function(id As Integer, db As AppDbContext)
                                              Dim todo = Await db.Todos.FindAsync(id)
                                              If todo Is Nothing Then
                                                  Return Results.NotFound()
                                              Else
                                                  Return Results.Ok(todo)
                                              End If
                                          End Function)

            app.MapPost("/todos", Async Function(todo As Todo, db As AppDbContext)
                                      db.Todos.Add(todo)
                                      Await db.SaveChangesAsync()
                                      Return Results.Created($"/todos/{todo.Id}", todo)
                                  End Function)

            app.MapPut("/todos/{id:int}", Async Function(id As Integer, input As Todo, db As AppDbContext)
                                             Dim todo = Await db.Todos.FindAsync(id)
                                             If todo Is Nothing Then
                                                 Return Results.NotFound()
                                             End If
                                             todo.Title = input.Title
                                             todo.IsDone = input.IsDone
                                             Await db.SaveChangesAsync()
                                             Return Results.NoContent()
                                         End Function)

            app.MapDelete("/todos/{id:int}", Async Function(id As Integer, db As AppDbContext)
                                                Dim todo = Await db.Todos.FindAsync(id)
                                                If todo Is Nothing Then
                                                    Return Results.NotFound()
                                                End If
                                                db.Todos.Remove(todo)
                                                Await db.SaveChangesAsync()
                                                Return Results.NoContent()
                                            End Function)

            app.MapRazorPages()
            app.Run()
        End Sub
    End Class
End Namespace
