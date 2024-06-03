namespace Sign_App_server.lib;

using System.Diagnostics.Contracts;
using MySql.Data.MySqlClient;

public class authTools : SlimShady
{   
    string serverIp = "localhost";
    string username = "admin";
    string password = "root";
    string databaseName = "database1";
    string connString;

    public authTools(string serverIp,string username, string password, string databaseName){
        connString = $"server={serverIp};uid={username};pwd={password};database={databaseName};";
    }

    public bool TryConnection(){
        try{
            using(MySqlConnection conn = new MySqlConnection(connString)){
                conn.Open();
            }
        }
        catch {
            return false;
        }
        return true;
    }
    
    public static string GetKey(string uname, string password){
        string key = "";
        

        return key;
    }
}
