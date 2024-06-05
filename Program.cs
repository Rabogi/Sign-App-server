using System.Text;
using Sign_App_server;
using Sign_App_server.lib;

//conf TODO add read conf from file in root
string configPath = "./config.json";
var conf = JsonHandler.ReadJson(File.ReadAllText(configPath));

string storagePath = conf["storagePath"].ToString();
bool unsafeMode = conf["unsafeMode"].ToString() == "true" ? true : false ;

SqlTools sqlHandler = new SqlTools(
    conf["SQLserverIP"].ToString(),
    conf["SQLusername"].ToString(),
    conf["SQLpassword"].ToString(),
    conf["SQLdatabase"].ToString()
);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
});

app.MapPost("/hash256", async (HttpContext httpContext) =>
{
    using StreamReader reader = new StreamReader(httpContext.Request.Body);
    string data = await reader.ReadToEndAsync();
    return (SlimShady.Sha256Hash(data));
});

app.MapPost("/uploadfiles", async (HttpContext httpContext) =>
{
    IFormFileCollection files = httpContext.Request.Form.Files;

    foreach (var file in files)
    {

        string fullPath = storagePath + "/" + file.FileName;

        using (var fileStream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }
    }

    return "All files written";
});

app.MapPost("/downloadfiles", async (HttpContext httpContext) =>
{
    if (unsafeMode == true)
    {
        using StreamReader reader = new StreamReader(httpContext.Request.Body);
        string name = await reader.ReadToEndAsync();
        string fname = name;
        name = storagePath + "/" + name;
        if (File.Exists(name))
        {
            await httpContext.Response.SendFileAsync(name);
        }
    }
});

app.MapPost("/sql", async (HttpContext httpContext) =>
{
    using StreamReader reader = new StreamReader(httpContext.Request.Body);
    string data = await reader.ReadToEndAsync();
    return (sqlHandler.Query(data));
});



app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}