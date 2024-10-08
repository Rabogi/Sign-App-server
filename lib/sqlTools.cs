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

    public async Task<bool[]>? InitDB(string[] initdata)
    {
        bool[] res = [];
        try
        {
            foreach (var query in initdata)
            {
                if (await Query(query) != "Error")
                {
                    res.Append(true);
                }
                else
                {
                    res.Append(false);
                }
            }
            return res;
        }
        catch
        {
            return null;
        }
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
                string[] found = ["", "", ""];
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

    public async Task<string>? InsertUser(string username, string password, string level)
    {
        string query = "INSERT INTO `SignAppDB`.`users` (`username`, `password`, `level`) VALUES ('" + username + "', '" + password + "', '" + level + "');";
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
            return "Error + " + query;
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

    public async Task<string>? InsertKeyPair(string userId, string name, Dictionary<string, string> keyPair)
    {
        if (SelectQuery("SELECT * FROM SignAppDB.userKeys where name = '" + name + "' and userid = '" + userId + "';").Count > 0)
        {
            return "Key name is taken";
        }
        string query = "INSERT INTO `SignAppDB`.`userKeys` (`userid`, `name`, `pubkey`, `prikey`,`hash`) VALUES ('" + userId + "', '" + name + "', '" + keyPair["PublicKey"] + "', '" + keyPair["PrivateKey"] + "','" + SlimShady.Sha256Hash(keyPair["PrivateKey"]) + "');";
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
            return query;
        }
    }


    // public async Task<bool> CheckKeyPairName(string name){
    //     bool res = false;

    // } 

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

    public async Task<string> UpdateSession(string key, string session)
    {
        var data = JsonHandler.ReadJson(session);
        string query = "UPDATE `SignAppDB`.`sessions` SET `keyExpiration` = '" + data["keyExpiration"] + "' WHERE (`sessionKey` = '" + key + "');";
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

    public async Task<string> InsertFile(string userId, string filename, string hash)
    {
        string query = "INSERT INTO `SignAppDB`.`files` (`filename`, `hash`, `owner`, `creationtime`) VALUES ('" + filename + "', '" + hash + "', '" + userId + "', '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "');";
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

    public async Task<List<Dictionary<string, object>>> GetUserFiles(string userId)
    {
        var ownedFiles = SelectQuery("SELECT * FROM SignAppDB.files WHERE OWNER = '" + userId + "';");
        var foreignPerms = SelectQuery("SELECT * FROM SignAppDB.perms where user = '" + userId + "';");
        foreach (var perm in foreignPerms)
        {
            ownedFiles.Add(SelectQuery("SELECT * FROM SignAppDB.files where id = '" + perm["fileid"].ToString() + "';")[0]);
        }
        return ownedFiles;
    }

    public async Task<List<Dictionary<string, object>>> GetUserOwnedFiles(string userId){
        var ownedFiles = SelectQuery("SELECT * FROM SignAppDB.files WHERE OWNER = '" + userId + "';");
        return ownedFiles;
    }

    public async Task<List<Dictionary<string, object>>> GetUserKeyPairs(string userId)
    {
        return SelectQuery("SELECT * FROM SignAppDB.userKeys where userid = '" + Convert.ToInt32(userId) + "';");
    }

    public List<Dictionary<string, object>> SelectQuery(string query)
    {
        List<Dictionary<string, object>> res = new List<Dictionary<string, object>>();

        using (MySqlConnection conn = new MySqlConnection(connString))
        {
            conn.Open();
            MySqlCommand command = new MySqlCommand(query, conn);
            using (MySqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    Dictionary<string, object> row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader[i].ToString();
                    }
                    res.Add(row);
                }
            }
        }

        return res;
    }

    public async Task<string> InsertSignature(string userId, string keyPairID, string signature, string fileId)
    {   
        if(checkIfSigned(userId, keyPairID, signature, fileId)){
            return "Already signed";
        }
        string query = "INSERT INTO `SignAppDB`.`signatures` (`userid`, `keyid`, `signature`, `fileid`, `creationtime`) VALUES ('" + userId + "', '" + keyPairID + "', '" + signature + "', '" + fileId + "', '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "');";
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

    public Dictionary<string, object> GetFilePath(string fileId)
    {
        return SelectQuery("SELECT filename FROM SignAppDB.files WHERE id = '" + fileId + "';")[0];
    }

    public Dictionary<string, object> GetKeyPair(string keyPairId)
    {
        return SelectQuery("SELECT pubkey,prikey FROM SignAppDB.userKeys where id = '" + keyPairId + "';")[0];
    }

    public bool checkIfSigned(string userId, string keyPairID, string signature, string fileId)
    {
        string query = "SELECT id FROM SignAppDB.signatures where userid = '"+userId+"' and keyid = '"+keyPairID+"' and signature = '"+signature+"' and fileid = '"+fileId+"';";
        var sign = SelectQuery(query);
        return sign.Count > 0 ? true : false;
    }

    public List<Dictionary<string, object>> GetSingsOfFile(string fileid){
        return SelectQuery("SELECT * FROM SignAppDB.signatures WHERE fileid = '"+fileid+"';");
    }

    public string checkSignature(string signId){
        string query = "SELECT * FROM SignAppDB.signatures WHERE id = '"+signId+"';";
        var sign = SelectQuery(query)[0];
        if(sign == null){
            return "NotFound";
        }
        var keyPair = GetKeyPair(sign["keyid"].ToString());
        if(keyPair == null){
            return "ErrorInDB";
        }
        string filename = GetFilePath(sign["fileid"].ToString())["filename"].ToString();
        return SlimShady.VerifySignature(File.ReadAllText(filename),sign["signature"].ToString(),keyPair["pubkey"].ToString()) ? "True" : "False";
    }

    public async Task<string> InsertPermission(string userId, string fileId)
    {
        string query = "INSERT INTO `SignAppDB`.`perms` (`user`, `readperm`, `writeperm`, `delperm`, `fileid`) VALUES ('"+userId+"', '1', '0', '0', '"+fileId+"');";
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


