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
}