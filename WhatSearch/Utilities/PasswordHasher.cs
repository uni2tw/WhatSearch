using System;
using System.Security.Cryptography;

namespace WhatSearch.Utilities
{
    public static class PasswordHasher
    {
        const int SaltSize = 16;
        const int HashSize = 32;
        const int Iterations = 100_000;

        public static string Hash(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
            return string.Format("{0}.{1}.{2}", Iterations, Convert.ToBase64String(salt), Convert.ToBase64String(hash));
        }

        public static bool Verify(string password, string hashedValue)
        {
            if (string.IsNullOrEmpty(hashedValue))
            {
                return false;
            }
            string[] parts = hashedValue.Split('.');
            if (parts.Length != 3)
            {
                return false;
            }
            int iterations = int.Parse(parts[0]);
            byte[] salt = Convert.FromBase64String(parts[1]);
            byte[] expectedHash = Convert.FromBase64String(parts[2]);
            byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
    }
}
