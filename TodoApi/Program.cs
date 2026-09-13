using Microsoft.VisualBasic;
using Microsoft.EntityFrameworkCore;

using TodoApi.Dtos;
using TodoApi.Models;
using TodoApi.Data;
using System.Net.WebSockets;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseHttpsRedirection();


var todoGroup = app.MapGroup("/api/todos").WithTags("Todos");

#region In-Memmory

// var todos = new List<TodoGetDto>
// {
//  new(1,"Laern C#",true),
//  new(2,"Laern ASP.NET Core",true),
//  new(3,"Build A web API",true)
// };

// todoGroup.MapGet("/", () => Results.Ok(todos));

// todoGroup.MapGet("/{id}", (int id) =>
// {
//     var todo = todos.FirstOrDefault(t => t.Id == id);

//     return todo is null ? Results.NotFound() : Results.Ok(todo);
// });

// todoGroup.MapPost("/", (TodoPostDto dto) =>
// {
//     var nextId = todos.Count == 0 ? 1 : todos.Max(t => t.Id + 1);

//     var todo = new TodoGetDto(nextId, dto.Titel, false);
//     todos.Add(todo);

//     return Results.Created($"/api/todos/{todo.Id}", todo);

// });

// todoGroup.MapPut("/{id}", (int id, TodoPutDto dto) =>
// {
//     try
//     {
//         var index = todos.FindIndex(x => x.Id == id);

//         todos[index] = todos[index] with
//         {
//             Titel = dto.Titel,
//             Iscampleted = dto.Iscampleted
//         };

//         return Results.Ok(todos[index]);
//     }
//     catch (Exception ex)
//     {
//         return Results.Problem(ex.Message);
//     }

// }
// );
// todoGroup.MapDelete("/{id}", (int id) =>
// {
//     try
//     {
//         var todo = todos.FirstOrDefault(t => t.Id == id);
//         if (todo is null) return Results.NotFound();
//         todos.Remove(todo);
//         return Results.NoContent();

//     }

//     catch (Exception ex)
//     {
//         return Results.Problem(ex.Message);
//     }
// });

#endregion
#region Database Endpoint
todoGroup.MapGet("/", async (AppDbContext db) =>
{
    var todos = await db.Todos.ToListAsync();

    var todoGetDtos = todos.Select(t=>
    new TodoGetDto(
        t.Id,
        t.Titel,
        t.IsCompleted
    ));
    
    return todos is null ? Results.NotFound() : Results.Ok(todos);
});

todoGroup.MapPost("/", async (AppDbContext db, TodoPostDto dto) =>
{
    //read
    try
    {
        var lastTodo = await db.Todos.OrderByDescending(t => t.Id).FirstOrDefaultAsync();
        var nextId = lastTodo is null ? 1 : lastTodo.Id + 1;
        var todo = new TodoItem
        {
            Titel = dto.Titel,
            IsCompleted = false,
            CreateAt = DateTime.UtcNow
        };
        db.Todos.Add(todo);
        await db.SaveChangesAsync();

        var todoGetdto = new TodoGetDto(todo.Id, todo.Titel, todo.IsCompleted);

        return Results.Created($"{todo.Id}", todo);
    }

    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }

});

#endregion
app.Run();
