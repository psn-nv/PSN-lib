using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PSN
{
    /// <summary>
    /// Шифрование
    /// </summary>
    public static class Crypto
    {
        /// <summary>
        /// Заполучить MD5 хэш.
        /// </summary>
        /// <param name="input_string">Строка, которая подвергнется хэшированию.</param>
        /// <returns>Хэш строки.</returns>
        public static string GetMD5Hash(String input_string)
        {
            MD5 md5Hasher = MD5.Create();
            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input_string));            
            StringBuilder result = new StringBuilder();            
            for (int i = 0; i < data.Length; i++)
            {
                // x2 - шестнадцатиричный формат
                result.Append(data[i].ToString("x2"));
            }
            return result.ToString();
        }

        /// <summary>
        /// Проверка совпадения MD5-хэша входной строки и контрольного хэша.
        /// </summary>
        /// <param name="input">Строка, MD5-хэш которой подвергнется сравнению.</param>
        /// <param name="hash">MD5-хэш, с которым сравнивают</param>
        /// <returns>Истина, если совпали.</returns>
        public static bool VerifyMD5HashEquality(string input, string hash)
        {
            string hashOfInput = GetMD5Hash(input);
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;
            if (0 == comparer.Compare(hashOfInput, hash))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Преобразование строки в формат BASE64
        /// </summary>
        /// <param name="plain_string">Строка, которую требуется закодировать.</param>
        /// <returns>Закодированная строка.</returns>
        public static string ConvertToBase64(string plain_string)
        {
            string result = "";
            if (plain_string.Length > 0)
            {
                byte[] data = Encoding.Default.GetBytes(plain_string);
                result = Convert.ToBase64String(data, Base64FormattingOptions.None);
            }
            return result;
        }

        /// <summary>
        /// Преобразование строки из BASE64 в обычный формат.
        /// </summary>
        /// <param name="base64_string">Строка, закодированная в BASE64.</param>
        /// <returns>Раскодированная строка.</returns>
        public static string ConvertFromBase64(string base64_string)
        {
            string result = "";
            if (base64_string.Length > 0)
            {
                byte[] data = Convert.FromBase64String(base64_string);
                result = Encoding.Default.GetString(data);
            }
            return result;
        }

        private static byte[] GetKeyForCryptoProvider(string key, int length)
        {
            SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider();
            byte[] result = new byte[length];
            byte[] ch = sha1.ComputeHash(ASCIIEncoding.Default.GetBytes(key));
            if (ch.Length <= result.Length)
            {
                ch.CopyTo(result, 0);
            }
            else
            {
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = ch[i];
                }
            }
            return result;
        }

        /// <summary>
        /// Шифрование строки по алгоритму DES.
        /// </summary>
        /// <param name="input_string">Строка, которая подвергается шифрованию.</param>
        /// <param name="key">Ключ шифра.</param>
        /// <returns>Зашифрованная строка.</returns>
        public static string EncodeStringViaDES(string input_string, string key)
        {
            string result = "";

            TripleDESCryptoServiceProvider tDESalg = new TripleDESCryptoServiceProvider();

            tDESalg.Key = GetKeyForCryptoProvider(key, tDESalg.KeySize / 8);
            tDESalg.IV = GetKeyForCryptoProvider("", tDESalg.BlockSize / 8);

            using (MemoryStream mstrm = new MemoryStream())
            {
                using (CryptoStream cStream = new CryptoStream(mstrm,
                                                                new TripleDESCryptoServiceProvider().CreateEncryptor(tDESalg.Key, tDESalg.IV),
                                                                CryptoStreamMode.Write))
                {
                    using (StreamWriter swr = new StreamWriter(cStream))
                    {
                        byte[] toEncrypt = new ASCIIEncoding().GetBytes(input_string);
                        cStream.Write(toEncrypt, 0, toEncrypt.Length);
                        cStream.FlushFinalBlock();
                        result = Convert.ToBase64String(mstrm.ToArray(), Base64FormattingOptions.None);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Дешифрует строку по алгоритму DES.
        /// </summary>
        /// <param name="crypted_string">Зашифрованная строка.</param>
        /// <param name="key">Ключ шифра.</param>
        /// <returns>Расшифрованная строка.</returns>
        public static string DecodeStringViaDES(string crypted_string, string key)
        {
            string result = "";

            TripleDESCryptoServiceProvider tDESalg = new TripleDESCryptoServiceProvider();

            tDESalg.Key = GetKeyForCryptoProvider(key, tDESalg.KeySize / 8);
            tDESalg.IV = GetKeyForCryptoProvider("", tDESalg.BlockSize / 8);

            byte[] bytes = Convert.FromBase64String(crypted_string);
            using (MemoryStream mstrm = new MemoryStream(bytes))
            {
                using (CryptoStream cStream = new CryptoStream(mstrm,
                                                                new TripleDESCryptoServiceProvider().CreateDecryptor(tDESalg.Key, tDESalg.IV),
                                                                CryptoStreamMode.Read))
                {

                    byte[] fromEncrypt = new byte[bytes.Length];
                    cStream.Read(fromEncrypt, 0, fromEncrypt.Length);

                    result = new ASCIIEncoding().GetString(fromEncrypt);
                    result = result.Replace("\0", "");
                }
            }

            return result;
        }
    }
}
