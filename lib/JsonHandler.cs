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
        File.WriteAllText("./test.json",res);
        return res;
    }
}