using Sign_App_server;
using Sign_App_server.lib;
using MimeTypes;
using Org.BouncyCastle.Crypto.Engines;

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
authHandler.lifetime = lifetime;

var builder = WebApplication.CreateBuilder(args);

// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();

var app = builder.Build();

// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }

app.UseHttpsRedirection();

var log = app.Logger;
log.Log(LogLevel.Information, "Lifetime is " + lifetime.ToString());
// log.Log(LogLevel.Information, "Creation key is " + userCreationKey);

string ClearFileName(string FileName)
{
    string Result = "";

    char[] name = FileName.ToCharArray();
    for (int i = name.Length - 1; i != -1; i--)
    {
        if (name[i] == '/')
            break;
        Result = name[i] + Result;
    }

    return Result;
}

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

app.MapPost("/downloadfile", async (HttpContext httpContext) =>
{
    // if (unsafeMode == true)
    // {
    //     using StreamReader reader = new StreamReader(httpContext.Request.Body);
    //     string name = await reader.ReadToEndAsync();
    //     string fname = name;
    //     name = storagePath + "/" + name;
    //     if (File.Exists(name))
    //     {
    //         await httpContext.Response.SendFileAsync(name);
    //     }
    // }
    using StreamReader reader = new StreamReader(httpContext.Request.Body);
    var data = JsonHandler.ReadJson(await reader.ReadToEndAsync());
    var res = new Dictionary<string, object>();
    if (data.ContainsKey("Key") & data.ContainsKey("Id"))
    {
        if (await authHandler.TryKey(data["Key"].ToString(), sqlHandler, true))
        {

            int id = Convert.ToInt32(data["Id"].ToString());

            var files = await sqlHandler.GetUserFiles(sqlHandler.getSession(data["Key"].ToString())[1]);
            if (files.Count > 0)
            {
                foreach (var file in files)
                {
                    if (Convert.ToInt32(file["id"].ToString()) == id)
                    {
                        var content = new ByteArrayContent(await File.ReadAllBytesAsync(file["filename"].ToString()));
                        // var content = new ByteArrayContent(await File.ReadAllBytesAsync(file["filename"].ToString()));
                        string fname = ClearFileName(file["filename"].ToString());
                        // Response.Add(content, name: fname, fileName: fname);
                        httpContext.Response.ContentType = MimeTypeMap.GetMimeType(fname);
                        log.Log(LogLevel.Information, fname);
                        log.Log(LogLevel.Information, MimeTypeMap.GetMimeType(file["filename"].ToString()));
                        log.Log(LogLevel.Information, SlimShady.Sha256Hash(File.ReadAllText(file["filename"].ToString())));
                        await httpContext.Response.SendFileAsync(file["filename"].ToString());

                        // res.Add("Result", "Success");
                        // res.Add("Info", "File transferred");
                        // return JsonHandler.MakeJson(res);
                    }
                }
                //             // res.Add("Result", "Failure");
                //             // res.Add("Info", "No available file with this id found");
                //             // Response.Add(new StringContent(JsonHandler.MakeJson(res)),"Json");
                //             // return JsonHandler.MakeJson(res);
                //         }
                //         else
                //         {
                //             // res.Add("Result", "Failure");
                //             // res.Add("Info", "No files");
                //             // Response.Add(new StringContent(JsonHandler.MakeJson(res)),"Json");
                //             // return JsonHandler.MakeJson(res);
                //         }
                //     }
                //     else
                //     {
                //         // res.Add("Result", "Request denied");
                //         // Response.Add(new StringContent(JsonHandler.MakeJson(res)),"Json");
                //         // return JsonHandler.MakeJson(res);
                //     }
                // }
                // else
                // {
                //     // res.Add("Result", "Failure");
                //     // res.Add("Result", "Malformed data");
                //     // Response.Add(new StringContent(JsonHandler.MakeJson(res)),"Json");
                //     // return JsonHandler.MakeJson(res);
                // }



            }
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
        foreach (var item in res)
        {
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

app.MapPost("/getfiles", async (HttpContext httpContext) =>
{
    StreamReader reader = new StreamReader(httpContext.Request.Body);
    var data = JsonHandler.ReadJson(await reader.ReadToEndAsync());
    var res = new Dictionary<string, object>();
    if (await authHandler.TryKey(data["Key"].ToString(), sqlHandler, true))
    {
        var files = await sqlHandler.GetUserFiles(sqlHandler.getSession(data["Key"].ToString())[1]);
        if (files != null)
        {
            res.Add("Result", "Success");
            res.Add("Info", files.Count);
            for (int i = 0; i < files.Count; i++)
            {
                res.Add(i.ToString(), JsonHandler.MakeJson(files[i]));
            }
            return JsonHandler.MakeJson(res);
        }
        else
        {
            res.Add("Result", "Failure");
            res.Add("Info", "No files");
            return JsonHandler.MakeJson(res);
        }
    }
    else
    {
        res.Add("Result", "Request denied");
        return JsonHandler.MakeJson(res);
    }

});

app.MapPost("/trykey", async (HttpContext httpContext) =>
{
    using StreamReader reader = new StreamReader(httpContext.Request.Body);
    string data = await reader.ReadToEndAsync();
    return await authHandler.TryKey(data, sqlHandler, false);
});

app.MapGet("/listusers", () =>
{
    var data = sqlHandler.SelectQuery("SELECT id , username FROM SignAppDB.users;");
    var res = new Dictionary<string, object>();
    for (int i = 0; i < data.Count; i++)
    {
        res.Add(i.ToString(), JsonHandler.MakeJson(data[i]));
    }
    return JsonHandler.MakeJson(res);
});

app.MapPost("/getkeypairs", async (HttpContext httpContext) =>
{
    using StreamReader reader = new StreamReader(httpContext.Request.Body);
    var data = JsonHandler.ReadJson(await reader.ReadToEndAsync());
    var res = new Dictionary<string, object>();
    if (await authHandler.TryKey(data["Key"].ToString(), sqlHandler, true))
    {
        var keyPairs = await sqlHandler.GetUserKeyPairs(sqlHandler.getSession(data["Key"].ToString())[1]);
        if (keyPairs.Count == 0)
        {
            res.Add("Result", "Failure");
            res.Add("Info", "No key pairs found");
            return JsonHandler.MakeJson(res);
        }
        res.Add("Result", "Success");
        for (int i = 0; i < keyPairs.Count; i++)
        {
            res.Add(keyPairs[i]["id"].ToString(), keyPairs[i]["name"].ToString());
        }
        return JsonHandler.MakeJson(res);
    }
    else
    {
        res.Add("Result", "Request denied");
        return JsonHandler.MakeJson(res);
    }
});

app.MapPost("/signfile", async (HttpContext httpContext) =>
{
    using StreamReader reader = new StreamReader(httpContext.Request.Body);
    var data = JsonHandler.ReadJson(await reader.ReadToEndAsync());
    var res = new Dictionary<string, object>();
    if (data.ContainsKey("Key") & data.ContainsKey("FileID") & data.ContainsKey("KeyPairID"))
    {
        if (await authHandler.TryKey(data["Key"].ToString(), sqlHandler, true))
        {
            var userId = sqlHandler.getSession(data["Key"].ToString())[1];
            var ownedFiles = await sqlHandler.GetUserFiles(userId);
            List<int> fileIDs = [];
            foreach (var file in ownedFiles)
            {
                fileIDs.Add(Convert.ToInt32(file["id"].ToString()));
            }
            if (!fileIDs.Contains(Convert.ToInt32(data["FileID"].ToString())))
            {
                res.Add("Result", "Failure");
                res.Add("Info", "File id is not valid");
                return JsonHandler.MakeJson(res);
            }
            var keyPairs = await sqlHandler.GetUserKeyPairs(userId);
            List<int> keyPairIDs = [];
            foreach (var item in keyPairs)
            {
                keyPairIDs.Add(Convert.ToInt32(item["id"].ToString()));
            }
            if (!keyPairIDs.Contains(Convert.ToInt32(data["KeyPairID"].ToString())))
            {
                res.Add("Result", "Failure");
                res.Add("Info", "KeyPair id is not valid");
                return JsonHandler.MakeJson(res);
            }
            string filename = sqlHandler.GetFilePath(data["FileID"].ToString())["filename"].ToString();
            var keyPair = sqlHandler.GetKeyPair(data["KeyPairID"].ToString());

            var sign = SlimShady.SignData(File.ReadAllText(filename), keyPair["prikey"].ToString());
            bool check = SlimShady.VerifySignature(File.ReadAllText(filename), sign, keyPair["pubkey"].ToString());
            if (check)
            {   
                var e = await sqlHandler.InsertSignature(userId, data["KeyPairID"].ToString(), sign, data["FileID"].ToString());
                if (e == null)
                {   
                    res.Add("Result","Success");
                    res.Add("Signature", sign);
                    res.Add("Verification", check);
                    return JsonHandler.MakeJson(res);
                }
                res.Add("Result", "Failure");
                res.Add("Info", "Error while signing");
                return JsonHandler.MakeJson(res);
            }
            res.Add("Result", "Failure");
            res.Add("Info", "Error while signing");
            return JsonHandler.MakeJson(res);
        }
        else
        {
            res.Add("Result", "Request denied");
            return JsonHandler.MakeJson(res);
        }
    }
    res.Add("Result", "Failure");
    res.Add("Info", "Malformed data");
    return JsonHandler.MakeJson(res);
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