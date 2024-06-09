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

    public static async Task<string> Login(string loginData, SqlTools sqlHandler, int sessionTime)
    {
        var input = JsonHandler.ReadJson(loginData);
        Dictionary<string, object> res = new Dictionary<string, object>();
        if (input != null)
        {
            if (input.ContainsKey("Key"))
            {
                if (await TryKey(input["Key"].ToString(), sqlHandler, true))
                {
                    var d = await UpdateSessionTime(input["Key"].ToString(), sqlHandler, sessionTime);
                    if (d == "Error")
                    {
                        res.Add("Result", "Error");
                        res.Add("Message", "Error updating key");
                        return JsonHandler.MakeJson(res);
                    }
                    var j = JsonHandler.ReadJson(d);
                    res.Add("Result", "Success");
                    res.Add("Key", j["sessionKey"]);
                    res.Add("ValidUntil", j["keyExpiration"]);
                    return JsonHandler.MakeJson(res);
                }
                res.Add("Result", "Error");
                res.Add("Message", "Key is not valid!");
                return JsonHandler.MakeJson(res);
            }
            else if (input.ContainsKey("Username"))
            {
                var dbdata = sqlHandler.GetUserData(input["Username"].ToString());
                if (dbdata != null)
                {
                    if (dbdata[2] == SlimShady.Sha256Hash(input["Password"].ToString()))
                    {
                        var d = DateTime.Now;
                        var key = authHandler.GenKey(input["Username"].ToString(), input["Password"].ToString(), d);
                        d = d.AddMinutes(sessionTime);
                        await sqlHandler.InsertAuthKey(dbdata[0], key, d);
                        res.Add("Result", "Success");
                        res.Add("Key", key);
                        res.Add("ValidUntil", d.ToString("yyyy-MM-dd HH:mm:ss"));
                        PruneSessions(key,sqlHandler);
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
            if (isValidSessionTime(res[2]))
            {
                return true;
            }
            else if (removeExpired == true)
            {
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

    public static async Task<string> UpdateSessionTime(string key, SqlTools sqlHandler, int minutes)
    {
        var d = DateTime.Now.AddMinutes(minutes);
        var res = sqlHandler.getSession(key);
        if (res != null)
        {
            if (isValidSessionTime(res[2]))
            {
                if (DateTime.Parse(res[2]) < d)
                {
                    res[2] = d.ToString("yyyy-MM-dd HH:mm:ss");
                }
                var newSession = JsonHandler.ConvertSessionToJson(res);
                if (await sqlHandler.UpdateSession(key, newSession) != "Error")
                {
                    return newSession;
                }
            }
        }
        return "Error";
    }

    public static async Task<string> PruneSessions(string key, SqlTools sqlHandler)
    {
        Dictionary<string,object> result = new Dictionary<string,object>();
        if (await TryKey(key, sqlHandler, true) == true)
        {   
            var curSession = sqlHandler.getSession(key);
            List<string[]> UserSessions = sqlHandler.getSessionsOfUser(curSession[1]);
            if (UserSessions.Count() > 1)
            {   
                int t = 0;
                foreach (var session in UserSessions)
                {
                    if(session[0] != curSession[0]){
                        sqlHandler.RemoveSession(session[0]);
                        t++;
                    }
                }
                result.Add("Result","Success");
                result.Add("CountRemoved", t);
                return JsonHandler.MakeJson(result);
            }
            result.Add("Result","Only 1 key");
            return JsonHandler.MakeJson(result);
        }
        result.Add("Result", "Error");
        return JsonHandler.MakeJson(result);
    }
}
