using Org.BouncyCastle.Asn1.Cms;

namespace Sign_App_server.lib;

public abstract class authHandler
{
    // TODO move everything login related from base server file to here

    public static string GenKey(string username, string password,DateTime d){
        string res = "";
        //formul is SHA256(SHA256(SHA256(username) + SHA256(password)) + datatime now)
        res = SlimShady.Sha256Hash(SlimShady.Sha256Hash(SlimShady.Sha256Hash(username) + SlimShady.Sha256Hash(password)) + d);
        return res;
    }
}
