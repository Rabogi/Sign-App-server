namespace Sign_App_server.lib;

using System.Diagnostics.Contracts;
using MySql.Data.MySqlClient;

public class SqlTools : SlimShady
{   
    string serverIp = "localhost";
    string username = "admin";
    string password = "root";
    string databaseName = "database1";
    string connString;

    public SqlTools(string serverIp,string username, string password, string databaseName){
        this.serverIp = serverIp;
        this.username = username;
        this.password = password;
        this.databaseName = databaseName;
        this.connString = $"server={this.serverIp};uid={this.username};pwd={this.password};database={this.databaseName};";
    }

    public bool TryConnection(){
        try{
            using(MySqlConnection conn = new MySqlConnection(connString)){
                conn.Open();
                conn.Close();
            }
        }
        catch {
            return false;
        }
        return true;
    }

    public async Task<string> Query(string query){
        try{
        using(MySqlConnection conn= new MySqlConnection(connString)){
            conn.Open();
            MySqlCommand command = new MySqlCommand(query,conn);
            string res = (string)command.ExecuteScalar();
            
            conn.Close();
            return res;
        }}
        catch {
            return "Error";
        }
    }

    public async Task<string> InitDB(){
        try{

        }
        catch {

        }
    } 
    
    public static string GetKey(string uname, string password){
        string key = "";
        

        return key;
    }
}
