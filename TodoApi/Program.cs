using Microsoft.VisualBasic;
using TodoApi.Dtos;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseHttpsRedirection();

var todos = new List<TodoGetDto>
{
 new(1,"Laern C#",true),
 new(2,"Laern ASP.NET Core",true),
 new(3,"Build A web API",true)
};

app.MapGet("/api/todos", () => Results.Ok(todos));

app.MapGet("/api/todos/{id}", (int id) =>
{
    var todo = todos.FirstOrDefault(t => t.Id == id);

    return todo is null ? Results.NotFound() : Results.Ok(todo);
});


app.MapPost("/api/todos", (TodoPostDto dto) =>
{
    var nextId = todos.Count == 0 ? 1 : todos.Max(t => t.Id + 1);

    var todo = new TodoGetDto(nextId, dto.Titel, false);
    todos.Add(todo);

    return Results.Created($"/api/todos/{todo.Id}", todo);

});

app.MapPut("api/todos/{id}", (int id,TodoPutDto dto) =>
{
    try
    {
        var index = todos.FindIndex(x => x.Id == id);

        todos[index] = todos[index] with
        {
            Titel = dto.Titel,
            Iscampleted = dto.Iscampleted
        };

        return Results.Ok(todos[index]);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }

}
);



app.Run();
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
