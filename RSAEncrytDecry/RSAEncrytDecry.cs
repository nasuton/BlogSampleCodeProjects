using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace TestCSharp
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            string publicKeyPath = @".\RSAPublicKey.txt";
            string privateKeyPath = @".\RSAPrivateKey.txt";
            RSAEncrytDecry rsaEncrytDecry = new RSAEncrytDecry(privateKeyPath, publicKeyPath);
            string planText = "sampleTextほげ";
            Console.WriteLine($"暗号化したい文字列:{planText} \r\n");
            var encryptText = rsaEncrytDecry.EncryptStringRSA(planText);
            Console.WriteLine($"暗号化された文字列:{encryptText} \r\n");
            var decryptText = rsaEncrytDecry.DecryptStringRSA(encryptText);
            Console.WriteLine($"復号化された文字列:{decryptText} \r\n");
        }
    }

    internal class RSAEncrytDecry
    {
        private string privateKeyPath = string.Empty;

        private string publicKeyPath = string.Empty;

        /// <summary>
        /// 引数で受け取ったパスに対象となる
        /// </summary>
        /// <param name="_privateKeyPath">RSAで使用する秘密鍵のフルパス</param>
        /// <param name="_publicKeyPath">RSAで使用する公開鍵のフルパス</param>
        public RSAEncrytDecry(string _privateKeyPath, string _publicKeyPath) 
        {
            privateKeyPath = _privateKeyPath;
            publicKeyPath = _publicKeyPath;
            // 秘密鍵及び公開鍵が存在しない場合は作成する
            if (!File.Exists(privateKeyPath))
            {
                if (!File.Exists(publicKeyPath))
                {
                    CreateKey();
                }
            }
        }

        /// <summary>
        /// 暗号化・復号化する際に使用する鍵を生成する
        /// </summary>
        private void CreateKey()
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048);

            // 公開鍵をXML形式で取得
            String publicKey = rsa.ToXmlString(false);
            // 秘密鍵をXML形式で取得
            String privateKey = rsa.ToXmlString(true);

            byte[] bytesPublicKey = Encoding.UTF8.GetBytes(publicKey);
            byte[] bytesPrivateKey = Encoding.UTF8.GetBytes(privateKey);

            // 公開鍵を保存
            FileStream puKey = new FileStream(publicKeyPath, FileMode.Create, FileAccess.Write);
            puKey.Write(bytesPublicKey, 0, bytesPublicKey.Length);
            puKey.Close();

            // 秘密鍵を保存
            FileStream prKey = new FileStream(privateKeyPath, FileMode.Create, FileAccess.Write);
            prKey.Write(bytesPrivateKey, 0, bytesPrivateKey.Length);
            prKey.Close();

            rsa.Clear();
        }

        /// <summary>
        /// テキストを公開鍵を使用してRSA暗号化
        /// </summary>
        /// <param name="_planText">平文(暗号化したい文字列)</param>
        /// <returns>暗号化された文字列</returns>
        public string EncryptStringRSA(string _planText)
        {
            StreamReader sr = new StreamReader(publicKeyPath, Encoding.UTF8);
            string PublicKey = sr.ReadToEnd();
            sr.Close();

            // RSACryptoServiceProviderオブジェクトの作成
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048);
            // 公開鍵を指定
            rsa.FromXmlString(PublicKey);

            byte[] data = Encoding.UTF8.GetBytes(_planText);

            data = rsa.Encrypt(data, false);

            return Convert.ToBase64String(data);
        }

        /// <summary>
        /// RSAで暗号化されたものを秘密鍵を使用して復号化
        /// </summary>
        /// <param name="_cipherText">RSAで暗号化された文字列</param>
        /// <returns>復号化された文字列</returns>
        public string DecryptStringRSA(string _cipherText)
        {
            StreamReader sr = new StreamReader(privateKeyPath, Encoding.UTF8);
            string PrivateKey = sr.ReadToEnd();
            sr.Close();

            // RSACryptoServiceProviderオブジェクトの作成
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048);
            // 秘密鍵を指定
            rsa.FromXmlString(PrivateKey);  

            byte[] data = Convert.FromBase64String(_cipherText);

            data = rsa.Decrypt(data, false);

            return Encoding.UTF8.GetString(data);
        }
    }
}
