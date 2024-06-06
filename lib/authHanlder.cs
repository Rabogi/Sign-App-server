using Org.BouncyCastle.Asn1.Cms;

namespace Sign_App_server.lib;

public abstract class authHandler
{
    // TODO move everything login related from base server file to here


    private static string GenKey(string username, string password,DateTime d){
        string res = "";
        //formula is SHA256(SHA256(SHA256(username) + SHA256(password)) + datatime now)
        res = SlimShady.Sha256Hash(SlimShady.Sha256Hash(SlimShady.Sha256Hash(username) + SlimShady.Sha256Hash(password)) + d);
        return res;
    }

    public static async Task<string> Login(string loginData,SqlTools sqlHandler){
    var input = JsonHandler.ReadJson(loginData);
    if (input != null)
    {
        if (input.ContainsKey("username"))
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
                    return key;
                }

            }
        }
    }
    return "Error";
    }

    public static async Task<bool> TryKey(string key, SqlTools sqlHandler, bool removeExpired){
        var sessions = sqlHandler.getSessions(key);
        List<string[]> expired = new List<string[]>();
        List<string[]> valid = new List<string[]>();
        if (sessions != null){
            foreach (var session in sessions){
                if (isValidSession(session[2]) == false){
                    expired.Add(session);
                }
                else{
                    valid.Add(session);
                }
            }
        }
        if (expired.Count > 0 & removeExpired == true){
            foreach (var session in expired){
                await sqlHandler.RemoveSession(session[2]);
            }
        }
        if (valid.Count > 0){
            return true;
        }
        return false;
    }

    private static bool isValidSession(string ExpirationTime){
        return DateTime.Parse(ExpirationTime) > DateTime.Now;
    }
}
