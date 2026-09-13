using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Scalar.AspNetCore;
using Microsoft.OpenApi;
 
using TodoApi.Dtos;
using TodoApi.Models;
using TodoApi.Data;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Components ??= new Microsoft.OpenApi.OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\""
        };

        return Task.CompletedTask;
    });
});

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);
var jwtKey = builder.Configuration["Jwt:Key"];
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
          ValidateIssuer = true,
          ValidateAudience = true,
          ValidateLifetime = true,
          ValidateIssuerSigningKey = true,
          ValidIssuer = builder.Configuration["Jwt:Issuer"],
          ValidAudience = builder.Configuration["Jwt:Audience"],
          IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    }
    );
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

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
        t.Title,
        t.IsCompleted
    ));

    return todos is null ? Results.NotFound() : Results.Ok(todos);
})
.RequireAuthorization();

todoGroup.MapPost("/", async (AppDbContext db, TodoPostDto dto) =>
{
    //read
    try
    {
        var lastTodo = await db.Todos.OrderByDescending(t => t.Id).FirstOrDefaultAsync();
        var nextId = lastTodo is null ? 1 : lastTodo.Id + 1;
        var todo = new TodoItem
        {
            Title = dto.Title,
            IsCompleted = false,
            CreateAt = DateTime.UtcNow
        };
        db.Todos.Add(todo);
        await db.SaveChangesAsync();

        var todoGetdto = new TodoGetDto(todo.Id, todo.Title, todo.IsCompleted);

        return Results.Created($"{todo.Id}", todo);
    }

    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }

})
.RequireAuthorization();
#endregion

#region Authentication Endpoints
app.MapPost("/api/login",(LoginDto dto ,IConfiguration configuration) =>
{
    if (dto.Username != "admin" || dto.Password != "password")return Results.Unauthorized();
    
    var claims = new[]
    {
        new Claim(ClaimTypes.Name,dto.Username)
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]));

    var credentials = new SigningCredentials(key,SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: configuration["Jwt:Issuer"],
        audience: configuration["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddDays(1),
        signingCredentials: credentials);

    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
    return Results.Ok(new {Token = tokenString});
}).WithTags("Authentication").WithName("Login")
.Produces<LoginResponseDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized);


#endregion
app.Run();