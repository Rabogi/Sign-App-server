namespace Sign_App_server.lib;

public abstract class authHandler
{
    // TODO move everything login related from base server file to here


    private static string GenKey(string username, string password, DateTime d)
    {
        string res = "";
        //formula is SHA256(SHA256(SHA256(username) + SHA256(password)) + datatime now)
        res = SlimShady.Sha256Hash(SlimShady.Sha256Hash(SlimShady.Sha256Hash(username) + SlimShady.Sha256Hash(password)) + d);
        return res;
    }

    public static async Task<string> Login(string loginData, SqlTools sqlHandler)
    {
        var input = JsonHandler.ReadJson(loginData);
        Dictionary<string, object> res = new Dictionary<string, object>();
        if (input != null)
        {
            if (input.ContainsKey("key"))
            {
                // if (await TryKey(input["key"].ToString(),sqlHandler,false)){
                //     res.Add("key",input["key"].ToString());
                //     res.Add("Result","Key is valid");
                //     return JsonHandler.MakeJson(res);
                // }
            }
            else if (input.ContainsKey("username"))
            {
                var dbdata = sqlHandler.GetUserData(input["username"].ToString());
                if (dbdata != null)
                {
                    if (dbdata[2] == SlimShady.Sha256Hash(input["password"].ToString()))
                    {
                        var d = DateTime.Now;
                        var key = authHandler.GenKey(input["username"].ToString(), input["password"].ToString(), d);
                        d = d.AddMinutes(30);
                        await sqlHandler.InsertAuthKey(dbdata[0], key, d);
                        res.Add("Result", "Success");
                        res.Add("Key", key);
                        res.Add("ValidUntil", d.ToString("yyyy-MM-dd HH:mm:ss"));
                        return JsonHandler.MakeJson(res);
                    }
                    else
                    {
                        res.Add("Result", "Error");
                        res.Add("Message", "Wrong password");
                        return JsonHandler.MakeJson(res);
                    }
                }
                else
                {
                    res.Add("Result", "Error");
                    res.Add("Message", "No such user in DB");
                    return JsonHandler.MakeJson(res);
                }
            }
        }
        res.Add("Result", "Error");
        res.Add("Message", "Malformed data submitted");
        return JsonHandler.MakeJson(res);
    }

    public static async Task<bool> TryKey(string key, SqlTools sqlHandler, bool removeExpired)
    {
        var res = sqlHandler.getSession(key);
        if (res != null)
        {
            if (isValidSessionTime(res[2])){
                return true;
            }
            else if (removeExpired == true){
                await sqlHandler.RemoveSession(key);
            }
            return false;
        }
        return false;
    }

    private static bool isValidSessionTime(string ExpirationTime)
    {
        return DateTime.Parse(ExpirationTime) > DateTime.Now;
    }

    // private static Task<string> updateSession(int minutes,string key){

    // }
}
