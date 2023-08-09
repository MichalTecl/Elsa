﻿using System;
using System.Security.Cryptography;

namespace Elsa.Common
{
    public static class PasswordHashHelper
    {/// <summary>
     /// Size of salt
     /// </summary>
        private const int c_saltSize = 16;

        /// <summary>
        /// Size of hash
        /// </summary>
        private const int c_hashSize = 20;

        /// <summary>
        /// Creates a hash from a password
        /// </summary>
        /// <param name="password">the password</param>
        /// <param name="iterations">number of iterations</param>
        /// <returns>the hash</returns>
        public static string Hash(string password, int iterations)
        {
            //create salt
            byte[] salt;
            new RNGCryptoServiceProvider().GetBytes(salt = new byte[c_saltSize]);

            //create hash
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations);
            var hash = pbkdf2.GetBytes(c_hashSize);

            //combine salt and hash
            var hashBytes = new byte[c_saltSize + c_hashSize];
            Array.Copy(salt, 0, hashBytes, 0, c_saltSize);
            Array.Copy(hash, 0, hashBytes, c_saltSize, c_hashSize);

            //convert to base64
            var base64Hash = Convert.ToBase64String(hashBytes);

            //format hash with extra information
            return $"$MYHASH$V1${iterations}${base64Hash}";
        }
        /// <summary>
        /// Creates a hash from a password with 10000 iterations
        /// </summary>
        /// <param name="password">the password</param>
        /// <returns>the hash</returns>
        public static string Hash(string password)
        {
            return Hash(password, 10000);
        }

        /// <summary>
        /// Check if hash is supported
        /// </summary>
        /// <param name="hashString">the hash</param>
        /// <returns>is supported?</returns>
        public static bool IsHashSupported(string hashString)
        {
            return hashString.Contains("$MYHASH$V1$");
        }

        /// <summary>
        /// verify a password against a hash
        /// </summary>
        /// <param name="password">the password</param>
        /// <param name="hashedPassword">the hash</param>
        /// <returns>could be verified?</returns>
        public static bool Verify(string password, string hashedPassword)
        {
            //check hash
            if (!IsHashSupported(hashedPassword))
            {
                return password == hashedPassword;
            }

            //extract iteration and Base64 string
            var splittedHashString = hashedPassword.Replace("$MYHASH$V1$", "").Split('$');
            var iterations = int.Parse(splittedHashString[0]);
            var base64Hash = splittedHashString[1];

            //get hashbytes
            var hashBytes = Convert.FromBase64String(base64Hash);

            //get salt
            var salt = new byte[c_saltSize];
            Array.Copy(hashBytes, 0, salt, 0, c_saltSize);

            //create hash with given salt
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations);
            byte[] hash = pbkdf2.GetBytes(c_hashSize);

            //get result
            for (var i = 0; i < c_hashSize; i++)
            {
                if (hashBytes[i + c_saltSize] != hash[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
