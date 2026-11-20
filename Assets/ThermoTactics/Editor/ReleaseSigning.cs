using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ThermoTactics.EditorTools
{
    /// <summary>
    /// Release signing (v0.6.0). The keystore lives OUTSIDE the project folder, so it can never be committed:
    ///   %USERPROFILE%\ThermoTactics-keys\thermotactics-release.keystore
    /// The password is typed into this window each Unity session. It is kept only in memory (Unity does not save
    /// keystore passwords to ProjectSettings), and it is never written to any file in the repository.
    /// Menu: Thermo-Tactics ▸ Release ▸ Release Signing.
    /// </summary>
    public class ReleaseSigning : EditorWindow
    {
        public const string Alias = "thermotactics";
        public static string KeyFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "ThermoTactics-keys");
        public static string KeystorePath => Path.Combine(KeyFolder, "thermotactics-release.keystore");

        private string _password = "", _confirm = "";
        private string _message = "";

        [MenuItem("Thermo-Tactics/Release/Release Signing")]
        public static void Open() => GetWindow<ReleaseSigning>(true, "Release Signing").minSize = new Vector2(460, 300);

        /// <summary>True once the keystore exists and its password has been entered this session.</summary>
        public static bool Ready => File.Exists(KeystorePath) && !string.IsNullOrEmpty(PlayerSettings.Android.keystorePass);

        /// <summary>Points the Android player settings at the release keystore (password already entered).</summary>
        public static void Apply()
        {
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = KeystorePath;
            PlayerSettings.Android.keyaliasName = Alias;
        }

        private void OnGUI()
        {
            bool exists = File.Exists(KeystorePath);
            EditorGUILayout.LabelField("Keystore", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(KeystorePath, EditorStyles.textField, GUILayout.Height(18));
            EditorGUILayout.LabelField(exists ? "Status: keystore found." : "Status: no keystore yet. Create it once below.");
            EditorGUILayout.Space();

            _password = EditorGUILayout.PasswordField("Password", _password);
            if (!exists) _confirm = EditorGUILayout.PasswordField("Confirm password", _confirm);
            EditorGUILayout.HelpBox("At least 6 characters. Keep it in a safe place and back up the keystore file privately. " +
                                    "Losing either means future updates cannot install over this version.", MessageType.Info);

            if (!exists)
            {
                if (GUILayout.Button("Create release keystore")) Create();
            }
            else if (GUILayout.Button("Use this keystore for release builds"))
            {
                if (_password.Length < 6) _message = "Enter the keystore password.";
                else
                {
                    PlayerSettings.Android.keystorePass = _password;
                    PlayerSettings.Android.keyaliasPass = _password;
                    Apply();
                    _message = "Release signing is on for this Unity session. You can now run Build Android APK.";
                    Debug.Log("[Release] Release keystore selected (password kept in memory only).");
                }
            }
            if (!string.IsNullOrEmpty(_message)) EditorGUILayout.HelpBox(_message, MessageType.None);
        }

        private void Create()
        {
            if (_password.Length < 6) { _message = "The password needs at least 6 characters."; return; }
            if (_password != _confirm) { _message = "The two passwords do not match."; return; }
            string keytool = FindKeytool();
            if (keytool == null) { _message = "keytool was not found. Install the Android module's OpenJDK in Unity Hub."; return; }

            Directory.CreateDirectory(KeyFolder);
            var psi = new ProcessStartInfo(keytool,
                $"-genkeypair -v -keystore \"{KeystorePath}\" -alias {Alias} -keyalg RSA -keysize 2048 -validity 10000 " +
                "-dname \"CN=Thermo-Tactics, OU=Group 26, O=UPHSL, L=Binan, C=PH\" -storepass:env TT_PASS -keypass:env TT_PASS")
            { UseShellExecute = false, RedirectStandardError = true, RedirectStandardOutput = true, CreateNoWindow = true };
            psi.EnvironmentVariables["TT_PASS"] = _password;   // passed through the environment, never on the command line
            using (Process p = Process.Start(psi))
            {
                string err = p.StandardError.ReadToEnd();
                p.WaitForExit();
                if (p.ExitCode != 0 || !File.Exists(KeystorePath)) { _message = "keytool failed: " + err; return; }
            }
            PlayerSettings.Android.keystorePass = _password;
            PlayerSettings.Android.keyaliasPass = _password;
            Apply();
            _confirm = "";
            _message = "Keystore created and selected. Back up the file in " + KeyFolder + " privately (not in GitHub).";
            Debug.Log("[Release] Release keystore created at " + KeystorePath);
        }

        private static string FindKeytool()
        {
            string exe = Application.platform == RuntimePlatform.WindowsEditor ? "keytool.exe" : "keytool";
            string jdk = EditorPrefs.GetString("JdkPath");
            if (string.IsNullOrEmpty(jdk))
                jdk = Path.Combine(Path.GetDirectoryName(EditorApplication.applicationPath), "Data", "PlaybackEngines", "AndroidPlayer", "OpenJDK");
            string path = Path.Combine(jdk, "bin", exe);
            return File.Exists(path) ? path : null;
        }
    }
}
