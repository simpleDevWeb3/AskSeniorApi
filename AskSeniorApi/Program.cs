using AskSeniorApi.Helper;
using Scalar.AspNetCore;
using Supabase;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<Supabase.Client>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();

    var url = config["Supabase:Url"];
    var key = config["Supabase:ApiKey"];

    var options = new SupabaseOptions
    {
        AutoConnectRealtime = true,
        AutoRefreshToken = true
    };


    var client = new Supabase.Client(url, key, options);

   
    client.InitializeAsync().Wait();

    return client;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174") // frontend port
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
builder.Services.AddScoped<ICommentService, CommentService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthorization();

app.MapControllers();

app.Run();
