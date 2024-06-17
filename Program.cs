using System.Runtime.Intrinsics.Arm;
using System.Text;
using Sign_App_server;
using Sign_App_server.lib;

//conf TODO add read conf from file in root
string configPath = "./config.json";
var conf = JsonHandler.ReadJson(File.ReadAllText(configPath));

string storagePath = conf["storagePath"].ToString();
bool unsafeMode = conf["unsafeMode"].ToString() == "true" ? true : false;
string userCreationKey = SlimShady.Sha256Hash(conf["UserCreationKey"].ToString());

SqlTools sqlHandler = new SqlTools(
    conf["SQLserverIP"].ToString(),
    conf["SQLusername"].ToString(),
    conf["SQLpassword"].ToString(),
    conf["SQLdatabase"].ToString()
);

string[] initdata = File.ReadAllLines("./sqlinit.sql");
await sqlHandler.InitDB(initdata);


int lifetime = Convert.ToInt32(conf["SessionLifeTime"].ToString());

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

var log = app.Logger;
log.Log(LogLevel.Information, "Lifetime is " + lifetime.ToString());
// log.Log(LogLevel.Information, "Creation key is " + userCreationKey);

app.MapPost("/uploadfiles", async (HttpContext httpContext) =>
{
    string sessionKey = httpContext.Request.Form["key"];
    bool b = await authHandler.TryKey(sessionKey, sqlHandler, true);
    if (b)
    {
        var userId = sqlHandler.getSession(sessionKey)[1];
        if (!Directory.Exists(storagePath + "/" + userId))
        {
            Directory.CreateDirectory(storagePath + "/" + userId);
        }
        IFormFileCollection files = httpContext.Request.Form.Files;
        Dictionary<string, object> hashes = new Dictionary<string, object>();
        foreach (var file in files)
        {
            string fullPath = storagePath + "/" + userId + "/" + file.FileName;
            if (!File.Exists(fullPath))
                using (var fileStream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                    var hash = SlimShady.Sha256Hash(File.ReadAllText(fullPath));
                    hashes.Add(file.FileName + " sqlRes", await sqlHandler.InsertFile(userId, fullPath, hash));
                    hashes.Add(file.FileName, hash);
                }
            else
            {
                hashes.Add(file.FileName, "Not written");
            }
        }
        return JsonHandler.MakeJson(hashes);
    }
    else
    {
        Dictionary<string, object> res = new Dictionary<string, object>();
        res.Add("Result", "Request denied");
        return JsonHandler.MakeJson(res);
    }
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
    if (unsafeMode == true)
    {
        using StreamReader reader = new StreamReader(httpContext.Request.Body);
        var data = JsonHandler.ReadJson(await reader.ReadToEndAsync());
        var res = sqlHandler.SelectQuery(data["Query"].ToString());
        string res1 = "";
        foreach(var item in res){
            res1 += JsonHandler.MakeJson(item) + "\n";
        }
        return res1;
    }
    return "Error";
});

app.MapPost("/getuser", async (HttpContext httpContext) =>
{
    if (unsafeMode == true)
    {
        using StreamReader reader = new StreamReader(httpContext.Request.Body);
        string data = await reader.ReadToEndAsync();
        string[] res = sqlHandler.GetUserData(data);
        if (res != null)
        {
            return res[0] + " " + res[1] + " " + res[2] + " " + res[3];
        }
    }
    return "Error";
});

app.MapPost("/login", async (HttpContext httpContext) =>
{
    using StreamReader reader = new StreamReader(httpContext.Request.Body);
    string data = await reader.ReadToEndAsync();
    return await authHandler.Login(data, sqlHandler, lifetime);
});

app.MapPost("/adduser", async (HttpContext httpContext) =>
{
    {
        using StreamReader reader = new StreamReader(httpContext.Request.Body);
        var data = JsonHandler.ReadJson(await reader.ReadToEndAsync());
        Dictionary<string, object> res = new Dictionary<string, object>();

        if (data.ContainsKey("AccessKey"))
        {
            if (SlimShady.Sha256Hash(data["AccessKey"].ToString()) == userCreationKey)
            {
                if (data.ContainsKey("Username") & data.ContainsKey("Password"))
                {
                    if (data["Username"].ToString().Length > 0)
                    {
                        if (sqlHandler.GetUserData(data["Username"].ToString()) == null)
                        {
                            string level = data.ContainsKey("Level") ? data["Level"].ToString() : "1";
                            var output = await sqlHandler.InsertUser(data["Username"].ToString(), SlimShady.Sha256Hash(data["Password"].ToString()), level);
                            if (output == null)
                            {
                                res.Add("Result", "Success");
                                res.Add("Info", "New user added");
                                return res;
                            }
                            else
                            {
                                res.Add("Result", "Failure");
                                res.Add("Info", "SQLerror");
                                res.Add("SQL", output);
                                return res;
                            }
                        }
                        else
                        {
                            res.Add("Result", "Failure");
                            res.Add("Info", "Username taken");
                            return res;
                        }
                    }
                    else
                    {
                        res.Add("Result", "Failure");
                        res.Add("Info", "Not a valid username!");
                        return res;
                    }
                }
                else
                {
                    res.Add("Result", "Failure");
                    res.Add("Info", "Malformed data");
                    return res;
                }
            }
            else
            {
                res.Add("Result", "Failure");
                res.Add("Info", "Request denied");
                return res;
            }
        }
        else
        {
            res.Add("Result", "Failure");
            res.Add("Info", "Malformed data");
            return res;
        }
    }
});

app.MapGet("/time", () =>
{
    return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
});

app.MapPost("/newkeypair", async (HttpContext httpContext) =>
{
    using StreamReader reader = new StreamReader(httpContext.Request.Body);
    var data = JsonHandler.ReadJson(await reader.ReadToEndAsync());
    Dictionary<string, object> res = new Dictionary<string, object>();
    if (data.ContainsKey("Key"))
    {
        if (await authHandler.TryKey(data["Key"].ToString(), sqlHandler, true))
        {
            if (data.ContainsKey("PairName"))
            {
                if (data["PairName"] != null & data["PairName"].ToString().Length > 0)
                {
                    var userData = sqlHandler.getSession(data["Key"].ToString());
                    var pair = SlimShady.GetKeyRSAKeyPair(2048);
                    string output = await sqlHandler.InsertKeyPair(userData[1], data["PairName"].ToString(), pair);
                    if (output == null)
                    {
                        res.Add("Result", "Success");
                        res.Add("Info", "New pair added");
                        return res;
                    }
                    else
                    {
                        res.Add("Result", "Failure");
                        res.Add("Info", "SQLerror");
                        res.Add("SQL", output);
                        return res;
                    }
                }
                else
                {
                    res.Add("Result", "Failure");
                    res.Add("Info", "Invalid name");
                    return res;
                }
            }
            else
            {
                res.Add("Result", "Failure");
                res.Add("Info", "Malformed data");
                return res;
            }
        }
        else
        {
            res.Add("Result", "Failure");
            res.Add("Info", "Invalid key");
            return res;
        }
    }
    else
    {
        res.Add("Result", "Failure");
        res.Add("Info", "Malformed data");
        return res;
    }
});

app.MapPost("/trykey", async (HttpContext httpContext) =>
{
    using StreamReader reader = new StreamReader(httpContext.Request.Body);
    string data = await reader.ReadToEndAsync();
    return await authHandler.TryKey(data,sqlHandler,false);
});

app.Run();
// record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
// {
//     public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
// }
// var summaries = new[]
// {
//     "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
// };

// app.MapGet("/weatherforecast", () =>
// {
//     var forecast = Enumerable.Range(1, 5).Select(index =>
//         new WeatherForecast
//         (
//             DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
//             Random.Shared.Next(-20, 55),
//             summaries[Random.Shared.Next(summaries.Length)]
//         ))
//         .ToArray();
//     return forecast;
// });

// app.MapPost("/hash256", async (HttpContext httpContext) =>
// {
//     using StreamReader reader = new StreamReader(httpContext.Request.Body);
//     string data = await reader.ReadToEndAsync();
//     return (SlimShady.Sha256Hash(data));
// });