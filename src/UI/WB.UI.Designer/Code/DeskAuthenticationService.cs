using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Pdf;
using WB.Core.GenericSubdomains.Portable;
using WB.UI.Designer.Services;

namespace WB.UI.Designer.Code
{
    public class DeskAuthenticationService : IDeskAuthenticationService
    {
        private readonly IOptions<DeskSettings> deskSettings;

        public DeskAuthenticationService(IOptions<DeskSettings> deskSettings)
        {
            this.deskSettings = deskSettings;
        }

        public string GetReturnUrl(Guid userId, string userName, string userEmail, DateTime expiryDate)
        {
            var json = JsonConvert.SerializeObject(new Dictionary<string, string>{
                {"uid", userId.FormatGuid() },
                {"expires", expiryDate.ToString("o")},
                {"customer_email", userEmail },
                {"customer_name", userName }
            });

            string deskReturnUrl;
            using (AesManaged myAes = new AesManaged())
            {
                byte[] encryptedJson = this.Encrypt(json, this.GenerateEncryptionKey(), myAes.IV);

                byte[] combined = new byte[myAes.IV.Length + encryptedJson.Length];
                Array.Copy(myAes.IV, 0, combined, 0, myAes.IV.Length);
                Array.Copy(encryptedJson, 0, combined, myAes.IV.Length, encryptedJson.Length);

                var multipass = Convert.ToBase64String(combined);

                byte[] encrypted_signature = this.SignMultipass(multipass);
                var signature = Convert.ToBase64String(encrypted_signature);

                multipass = Uri.EscapeDataString(multipass);
                signature = Uri.EscapeDataString(signature);

                deskReturnUrl = string.Format(this.deskSettings.Value.ReturnUrlFormat, this.deskSettings.Value.SiteKey, multipass, signature);
            }

            return deskReturnUrl;
        }


        private byte[] Encrypt(string json, byte[] Key, byte[] IV)
        {
            byte[] encrypted;

            using (AesManaged aesAlg = new AesManaged())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create a decryptor to perform the stream transform
                using (ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV))
                {
                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                            {
                                swEncrypt.Write(json);
                            }

                            encrypted = msEncrypt.ToArray();
                        }
                    }
                }
            }

            return encrypted;
        }

        private byte[] GenerateEncryptionKey()
        {
            byte[] key;
            byte[] salt = Encoding.UTF8.GetBytes(this.deskSettings.Value.MultipassKey + this.deskSettings.Value.SiteKey);

            using (SHA1 sha1 = new SHA1CryptoServiceProvider())
            {
                key = sha1.ComputeHash(salt);
                Array.Resize(ref key, 16);
            }

            return key;
        }

        private byte[] SignMultipass(string unsignedMultipass)
        {
            byte[] signature;

            using (HMACSHA1 hmac = new HMACSHA1(Encoding.UTF8.GetBytes(this.deskSettings.Value.MultipassKey)))
            {
                using (MemoryStream msHmac = new MemoryStream(Encoding.UTF8.GetBytes(unsignedMultipass)))
                {
                    signature = hmac.ComputeHash(msHmac);
                }
            }

            return signature;
        }
    }
}