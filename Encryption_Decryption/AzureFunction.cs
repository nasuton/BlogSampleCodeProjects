using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System;
using System.Text;

public static async Task<IActionResult> Run(HttpRequest req, ILogger log)
{
    // log.LogInformation("C# HTTP trigger function processed a request.");

    string value = req.Query["value"];

    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
    dynamic data = JsonConvert.DeserializeObject(requestBody);
    value = value ?? data?.value;

    var plain = DecryptStringAES(value);
    var responseText = plain + "AzureResponse";
    var response = EncryptStringAES(responseText);
    
    var resDictionary = new Dictionary<string, string>();
    resDictionary.Add("result", response);

    var responseJson = JsonConvert.SerializeObject(resDictionary);

    return new ContentResult() { Content = responseJson, ContentType = "application/json", StatusCode = (int)HttpStatusCode.OK };
}

private static string EncryptStringAES(string plainText)
{
    var keyValue = "01234567890123456789012345678901";
    var ivValue = "0123456789012345";
    var keybytes = Encoding.UTF8.GetBytes(keyValue);
    var iv = Encoding.UTF8.GetBytes(ivValue);

    var encryptedFromJavascript = EncryptStringToBytes(plainText, keybytes, iv);
    var decrypted = Convert.ToBase64String(encryptedFromJavascript);
    return decrypted;
}

private static string DecryptStringAES(string cipherText)
{
    var keyValue = "01234567890123456789012345678901";
    var ivValue = "0123456789012345";
    var keybytes = Encoding.UTF8.GetBytes(keyValue);
    var iv = Encoding.UTF8.GetBytes(ivValue);

    var encrypted = Convert.FromBase64String(cipherText);
    var decriptedFromJavascript = DecryptStringFromBytes(encrypted, keybytes, iv);
    return decriptedFromJavascript;
}

private static byte[] EncryptStringToBytes(string plainText, byte[] key, byte[] iv)
{
    if (plainText == null || plainText.Length <= 0)
    {
        throw new ArgumentNullException("plainText");
    }
    if (key == null || key.Length <= 0)
    {
        throw new ArgumentNullException("key");
    }
    if (iv == null || iv.Length <= 0)
    {
        throw new ArgumentNullException("key");
    }
    byte[] encrypted;

    // 指定されたキーとIVを使用してRijndaelManagedオブジェクトを作成
    using (var rijAlg = new RijndaelManaged())
    {
        rijAlg.Mode = CipherMode.CBC;
        rijAlg.Padding = PaddingMode.PKCS7;
        //rijAlg.FeedbackSize = 128;
        rijAlg.BlockSize = 128;
        // 暗号化方式はAES-256を採用
        rijAlg.KeySize = 256;
        rijAlg.Key = key;
        rijAlg.IV = iv;

        var encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);

        using (var msEncrypt = new MemoryStream())
        {
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            {
                using (var swEncrypt = new StreamWriter(csEncrypt))
                {
                    swEncrypt.Write(plainText);
                }
                encrypted = msEncrypt.ToArray();
            }
        }
    }
    return encrypted;
}

private static string DecryptStringFromBytes(byte[] cipherText, byte[] key, byte[] iv)
{
    if (cipherText == null || cipherText.Length <= 0)
    {
        throw new ArgumentNullException("cipherText");
    }
    if (key == null || key.Length <= 0)
    {
        throw new ArgumentNullException("key");
    }
    if (iv == null || iv.Length <= 0)
    {
        throw new ArgumentNullException("key");
    }

    string plaintext = null;

    // 指定されたキーとIVを使用してRijndaelManagedオブジェクトを作成
    using (var rijAlg = new RijndaelManaged())
    {
        // RijndaelManagedの設定
        rijAlg.Mode = CipherMode.CBC;
        rijAlg.Padding = PaddingMode.PKCS7;
        rijAlg.BlockSize = 128;
        // 暗号化方式はAES-256を採用
        rijAlg.KeySize = 256;
        //rijAlg.FeedbackSize = 128;
        rijAlg.Key = key;
        rijAlg.IV = iv;

        var decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);
        try
        {
            using (var msDecrypt = new MemoryStream(cipherText))
            {
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (var srDecrypt = new StreamReader(csDecrypt))
                    {
                        // 復号化されたバイトを復号化ストリームから読み取り、文字列に設定
                        plaintext = srDecrypt.ReadToEnd();
                    }
                }
            }
        }
        catch
        {
            plaintext = "keyError";
        }
    }
    return plaintext;
}