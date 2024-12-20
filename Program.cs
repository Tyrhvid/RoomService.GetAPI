var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:4952");

builder.Services
    .AddCors(options =>
        options.AddPolicy(
            "TyrSecretRoom",
            policy =>
                policy
                    .WithOrigins("https://localhost:7777")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
        )
    );

var app = builder.Build();

app.UseCors("TyrSecretRoom");

// Forward query parameters to the repository's /tickets endpoint
app.MapGet("/rooms", async (HttpContext context) =>
{
    // Retrieve query parameters
    var start = context.Request.Query["start"];
    var count = context.Request.Query["count"];

    // Validate the parameters (optional)
    if (!int.TryParse(start, out var startValue) || !int.TryParse(count, out var countValue))
    {
        return Results.BadRequest("Invalid 'start' or 'count' query parameter.");
    }
    
    var repoUri = Environment.GetEnvironmentVariable("REPO_URI") ?? "http://repo:4050";
    using var httpClient = new HttpClient
    {
        BaseAddress = new Uri(repoUri)
    };

    // Forward the request to the repository service
    var response = await httpClient.GetAsync($"/rooms?start={start}&count={count}");
    if (!response.IsSuccessStatusCode)
    {
        return Results.StatusCode((int)response.StatusCode);
    }

    // Return the data from the repository service
    var data = await response.Content.ReadAsStringAsync();
    return Results.Content(data, "application/json");
});

app.Run();