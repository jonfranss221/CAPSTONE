using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThermoTactics
{
    /// <summary>
    /// The admin role (researchers and developers). Exporting quiz results, the full Results table and the phone letter are
    /// admin only. Entering the admin code in Settings unlocks them until the app is closed (the state is never saved), or
    /// until LOCK is pressed. The code itself is not stored in the game or the repository, only a salted PBKDF2-SHA256 hash
    /// of it (100,000 rounds), so it cannot be read back out of the APK and is slow to guess offline. After 3 wrong codes
    /// the field waits 30 seconds, so it cannot be guessed by quick retries either. Unlock, lock and wrong tries are logged
    /// (never the code itself).
    /// </summary>
    public static class AdminSession
    {
        // PBKDF2-SHA256 of the admin code (upper case, spaces trimmed) with the salt below. To change the code, put the
        // hash of the new one here (same salt and rounds). Never write the code itself in the project.
        private const string CodeHash = "cfeb1654f14d8c8a2d918c500f1206f0c92b8fe183d1b9376ed3acd07a2e8984";
        private const string Salt = "ThermoTactics.Admin.v1";
        private const int Rounds = 100000;
        public const int MaxTries = 3;
        public const float WaitSeconds = 30f;

        public static bool Unlocked { get; private set; }
        private static int _wrong;
        private static DateTime _waitUntil = DateTime.MinValue;

        public static event Action Changed;

        /// <summary>Seconds left before another code may be tried (0 when trying is allowed).</summary>
        public static int WaitLeft => Math.Max(0, (int)Math.Ceiling((_waitUntil - DateTime.Now).TotalSeconds));

        /// <summary>Checks a code. True unlocks admin mode; 3 wrong codes in a row start a 30-second wait.</summary>
        public static bool TryUnlock(string code)
        {
            if (WaitLeft > 0)
            {
                Debug.LogWarning("[Admin] Unlock refused: still waiting after too many wrong codes.");
                return false;
            }
            if (SlowEquals(Hash(code), CodeHash))
            {
                _wrong = 0;
                Unlocked = true;
                Debug.Log("[Admin] Admin tools unlocked.");   // never logs the code itself
                Changed?.Invoke();
                return true;
            }
            Debug.LogWarning($"[Admin] Wrong admin code ({_wrong + 1} of {MaxTries}).");
            if (++_wrong >= MaxTries)
            {
                _wrong = 0;
                _waitUntil = DateTime.Now.AddSeconds(WaitSeconds);
                Debug.LogWarning($"[Admin] Too many wrong codes: admin unlock blocked for {WaitSeconds:0} s.");
            }
            return false;
        }

        public static void Lock()
        {
            if (Unlocked) Debug.Log("[Admin] Admin tools locked.");
            Unlocked = false;
            Changed?.Invoke();
        }

        /// <summary>The salted PBKDF2-SHA256 hash of a code, as lower-case hex.</summary>
        public static string Hash(string code)
        {
            string normal = (code ?? "").Trim().ToUpperInvariant();
            using var kdf = new Rfc2898DeriveBytes(normal, Encoding.UTF8.GetBytes(Salt), Rounds, HashAlgorithmName.SHA256);
            byte[] h = kdf.GetBytes(32);
            var sb = new StringBuilder(h.Length * 2);
            foreach (byte b in h) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        /// <summary>Compares every character, so the time taken does not show how much of a guess was right.</summary>
        private static bool SlowEquals(string a, string b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }

#if UNITY_EDITOR
        /// <summary>Editor tests only (not in the APK): unlocks without the code, so no test has to contain it.</summary>
        public static void DevUnlock()
        {
            Unlocked = true;
            Changed?.Invoke();
        }

        /// <summary>Editor tests only: clears the wrong-code count and the wait.</summary>
        public static void DevResetTries()
        {
            _wrong = 0;
            _waitUntil = DateTime.MinValue;
        }

        /// <summary>
        /// Editor tests only: the real code for the "right code unlocks" test, read from the git-ignored file
        /// UserSettings/admin_code.txt (or the TT_ADMIN_CODE environment variable). Empty when neither is set.
        /// </summary>
        public static string DevTestCode()
        {
            string env = Environment.GetEnvironmentVariable("TT_ADMIN_CODE");
            if (!string.IsNullOrWhiteSpace(env)) return env.Trim();
            const string file = "UserSettings/admin_code.txt";
            return System.IO.File.Exists(file) ? System.IO.File.ReadAllText(file).Trim() : "";
        }
#endif
    }
}
