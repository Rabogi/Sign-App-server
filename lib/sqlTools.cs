namespace Sign_App_server.lib;

using MySql.Data.MySqlClient;

public class SqlTools : SlimShady
{
    string serverIp = "localhost";
    string username = "admin";
    string password = "root";
    string databaseName = "database1";
    string connString;

    public SqlTools(string serverIp, string username, string password, string databaseName)
    {
        this.serverIp = serverIp;
        this.username = username;
        this.password = password;
        this.databaseName = databaseName;
        this.connString = $"server={this.serverIp};uid={this.username};pwd={this.password};database={this.databaseName};";
    }

    public bool TryConnection()
    {
        try
        {
            using (MySqlConnection conn = new MySqlConnection(connString))
            {
                conn.Open();
                conn.Close();
            }
        }
        catch
        {
            return false;
        }
        return true;
    }

    public async Task<string> Query(string query)
    {
        try
        {
            using (MySqlConnection conn = new MySqlConnection(connString))
            {
                conn.Open();

                MySqlCommand command = new MySqlCommand(query, conn);
                string res = (string)command.ExecuteScalar();

                conn.Close();
                return res;
            }
        }
        catch
        {
            return "Error";
        }
    }

    public string[]? getSession(string key)
    {
        try
        {
            using (MySqlConnection conn = new MySqlConnection(connString))
            {
                conn.Open();
                string query = "SELECT * FROM SignAppDB.sessions WHERE sessionKey = '" + key + "';";
                MySqlCommand command = new MySqlCommand(query, conn);
                string[] found = ["","",""];
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    // reader ran only once since having multiple session with same key is irregular
                    reader.Read();
                    found[0] = reader["sessionKey"].ToString();
                    found[1] = reader["userId"].ToString();
                    found[2] = reader["keyExpiration"].ToString();
                }
                return found;
            }
        }
        catch
        {
            return null;
        }
    }

    public List<string[]>? getSessionsOfUser(string user)
    {
        try
        {
            using (MySqlConnection conn = new MySqlConnection(connString))
            {
                conn.Open();
                string query = "SELECT * FROM SignAppDB.sessions WHERE userid = '" + user + "';";
                MySqlCommand command = new MySqlCommand(query, conn);

                List<string[]> found = new List<string[]>();
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        found.Add([reader["sessionKey"].ToString(), reader["userId"].ToString(), reader["keyExpiration"].ToString()]);
                    }
                }
                return found;
            }
        }
        catch
        {
            return null;
        }
    }

    public string[]? GetUserData(string username)
    {
        try
        {
            using (MySqlConnection conn = new MySqlConnection(connString))
            {
                conn.Open();
                string query = "SELECT * FROM SignAppDB.users WHERE username = '" + username + "';";
                MySqlCommand command = new MySqlCommand(query, conn);

                List<string[]> found = new List<string[]>();
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        found.Add([reader["id"].ToString(), reader["username"].ToString(), reader["password"].ToString(), reader["level"].ToString()]);
                    }
                }
                // Return only first found as having more than one user with the unique name is irregular and will not be allowed in bd
                return found[0];
                conn.Close();
            }
        }
        catch
        {
            return null;
        }
    }

    public async Task<string> InsertAuthKey(string userId, string Key, DateTime until)
    {
        string query = "INSERT INTO `SignAppDB`.`sessions` (`sessionKey`, `userid`, `keyExpiration`) VALUES ('" + Key + "', '" + userId + "', '" + until.ToString("yyyy-MM-dd HH:mm:ss") + "')";
        try
        {
            using (MySqlConnection conn = new MySqlConnection(connString))
            {
                conn.Open();

                MySqlCommand command = new MySqlCommand(query, conn);
                string res = (string)command.ExecuteScalar();

                conn.Close();
                return res;
            }
        }
        catch (Exception e)
        {
            return "Error";
        }
    }

    public async Task<string> RemoveSession(string key)
    {
        string query = "DELETE FROM `SignAppDB`.`sessions` WHERE (`sessionKey` = '" + key + "');";
        try
        {
            using (MySqlConnection conn = new MySqlConnection(connString))
            {
                conn.Open();

                MySqlCommand command = new MySqlCommand(query, conn);
                string res = (string)command.ExecuteScalar();

                conn.Close();
                return res;
            }
        }
        catch (Exception e)
        {
            return "Error";
        }
    }

    public async Task<string> UpdateSession(string key, string session){
        var data = JsonHandler.ReadJson(session);
        string query = "UPDATE `SignAppDB`.`sessions` SET `keyExpiration` = '"+data["keyExpiration"]+"' WHERE (`sessionKey` = '"+key+"');";
        try
        {
            using (MySqlConnection conn = new MySqlConnection(connString))
            {
                conn.Open();

                MySqlCommand command = new MySqlCommand(query, conn);
                string res = (string)command.ExecuteScalar();

                conn.Close();
                return res;
            }
        }
        catch (Exception e)
        {
            return "Error";
        }
    }
}
