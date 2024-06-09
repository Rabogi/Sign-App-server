using System.Text.Json.Nodes;
using Flurl.Util;

namespace Sign_App_server;

public abstract class JsonHandler
{
    public static Dictionary<string,object> ReadJson(string jsonData){
        var json = new Dictionary<string,object>();
        foreach (var item in JsonNode.Parse(jsonData).ToKeyValuePairs()){
            json.Add(item.Key,item.Value);
        }
        return json;
    }

    public static string MakeJson(Dictionary<string,object> data){
        string res = "{";
        foreach (var item in data){
            res += "\n\t\"" + item.Key + "\"" + ":" + "\"" + item.Value + "\",";
        }
        res = res.Remove(res.Length-1);
        res += "\n}";
        // File.WriteAllText("./test.json",res);
        return res;
    }

    public static string ConvertSessionToJson(string[] session){
        Dictionary<string,object> data = new Dictionary<string, object>
        {
            { "sessionKey", session[0] },
            { "userID", session[1] },
            { "keyExpiration", session[2] }
        };
        return MakeJson(data); 
    }
}