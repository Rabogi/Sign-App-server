namespace Sign_App_server;

using System.Security.Cryptography;
using System.Text;

public abstract class SlimShady
{
    public static string Sha256Hash(string input)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(input));
        StringBuilder sb = new StringBuilder();
        foreach (byte b in hash)
            sb.Append(b.ToString("X2"));
        return sb.ToString();
    }

    public void GenerateRSAKeyPair()
    {
        using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))
        {
            string publicKey = rsa.ToXmlString(false);
            string privateKey = rsa.ToXmlString(true);
            //save to file
            System.IO.File.WriteAllText("publicKey.xml", publicKey);
            System.IO.File.WriteAllText("privateKey.xml", privateKey);
        }
    }

    public void writeBin(string content, string filename)
    {
        var writer = new BinaryWriter(File.Open(filename, FileMode.Create));
        writer.Write(content);
        writer.Close();
    }

    public string readBin(string filename)
    {
        var reader = new BinaryReader(File.Open(filename, FileMode.Open));
        string temp = reader.ReadString();
        reader.Close();
        return temp;
    }

    //sign data
    public string SignData(string data, string privateKey)
    {
        using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))
        {
            rsa.FromXmlString(privateKey);
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            byte[] signature = rsa.SignData(dataBytes, new SHA256CryptoServiceProvider());
            return Convert.ToBase64String(signature);
        }
    }

    //verify signature
    public bool VerifySignature(string data, string signature, string publicKey)
    {
        using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))
        {
            rsa.FromXmlString(publicKey);
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            byte[] signatureBytes = Convert.FromBase64String(signature);
            return rsa.VerifyData(dataBytes, new SHA256CryptoServiceProvider(), signatureBytes);
        }
    }
}


// static void Main(string[] args){
//     switch (args[0])
//     {
//         case "-g":
//             GenerateRSAKeyPair();
//             Console.WriteLine("RSA Key Pair generated");
//             break;
//         case "-s"://-s data privateKey
//             string data = System.IO.File.ReadAllText(args[1]);
//             string privateKey = args[2];
//             string signature = SignData(data, System.IO.File.ReadAllText(privateKey));
//             Console.WriteLine("Signature: " + signature);
//             writeBin(signature, "signature.bin");
//             break;
//         case "-v"://-v data signature publickey
//             string data2 = System.IO.File.ReadAllText(args[1]);
//             string signature2 = readBin(args[2]);
//             string publicKey = args[3];
//             Console.WriteLine("Verify: " + VerifySignature(data2, signature2, System.IO.File.ReadAllText(publicKey)));
//         break;
//         case "-h":
//         Console.WriteLine("Key gen: ./rsa -g");
//         Console.WriteLine("Sign: ./rsa -s data privateKey");
//         Console.WriteLine("Verify: ./rsa -v data signature publickey");
//         break;
//         default:
//         Console.WriteLine("help: ./rsa -h");
//         break;
//     }
// }