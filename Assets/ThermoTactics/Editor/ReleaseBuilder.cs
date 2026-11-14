using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ThermoTactics.EditorTools
{
    /// <summary>
    /// Produces a controlled release build: sets the version, rebuilds the scenes, builds a release
    /// (non-development) Android APK named after the version, and writes a build record next to it.
    /// Menu: Thermo-Tactics ▸ Release ▸ Build Android APK.
    /// Versioning (MAJOR.MINOR.PATCH): bug fix → PATCH, new working feature → MINOR,
    /// change that breaks saved data → MAJOR (after 1.0.0). See CHANGELOG.md.
    /// </summary>
    public static class ReleaseBuilder
    {
        /// <summary>The version being released. Change this, then run Build Android APK.</summary>
        public const string Version = "0.4.0";
        public const string AndroidPackage = "com.group26.thermotactics";
        public const string OutputFolder = "Builds";

        /// <summary>Android versionCode: MAJOR*10000 + MINOR*100 + PATCH (0.3.1 → 301), always increasing.</summary>
        public static int VersionCode
        {
            get
            {
                string[] p = Version.Split('.');
                return int.Parse(p[0]) * 10000 + int.Parse(p[1]) * 100 + int.Parse(p[2]);
            }
        }

        public static string ApkName => $"Thermo-Tactics-v{Version}.apk";

        [MenuItem("Thermo-Tactics/Release/Apply Version Number")]
        public static void ApplyVersion()
        {
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.Android.bundleVersionCode = VersionCode;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AndroidPackage);

            // Landscape only (both directions): the UI is laid out for 1280 × 720 landscape.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            AssetDatabase.SaveAssets();
            Debug.Log($"[Release] Version set to {Version} (Android versionCode {VersionCode}, package {AndroidPackage}, landscape only).");
        }

        [MenuItem("Thermo-Tactics/Release/Build Android APK")]
        public static void BuildApk()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[Release] Leave Play mode before building."); return; }
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("[Release] Android Build Support is not installed. Add it in Unity Hub ▸ Installs ▸ 6000.0.84f1 ▸ Add modules.");
                return;
            }

            ApplyVersion();
            ThermoTacticsBuilder.BuildAll();   // scenes and data rebuilt from code, so the build matches the source

            string[] scenes = EnabledScenes();
            Directory.CreateDirectory(OutputFolder);
            string apk = Path.Combine(OutputFolder, ApkName);

            EditorUserBuildSettings.buildAppBundle = false;
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apk,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,   // release build: no development console, no profiler
            };

            DateTime start = DateTime.Now;
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary s = report.summary;

            var log = new StringBuilder();
            log.AppendLine($"Thermo-Tactics release build record");
            log.AppendLine($"Version:         {Version} (Android versionCode {VersionCode})");
            log.AppendLine($"Package:         {AndroidPackage}");
            log.AppendLine($"Artifact:        {apk}");
            log.AppendLine($"Result:          {s.result}");
            log.AppendLine($"Started:         {start:yyyy-MM-dd HH:mm:ss}");
            log.AppendLine($"Build time:      {s.totalTime}");
            log.AppendLine($"Unity:           {Application.unityVersion}");
            log.AppendLine($"Target:          Android, min API {(int)PlayerSettings.Android.minSdkVersion}, {PlayerSettings.Android.targetArchitectures}, {PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android)}");
            log.AppendLine($"Development:     no");
            log.AppendLine($"Errors/Warnings: {s.totalErrors} / {s.totalWarnings}");
            log.AppendLine("Scenes:");
            foreach (string sc in scenes) log.AppendLine($"  - {sc}");
            if (s.result == BuildResult.Succeeded && File.Exists(apk))
            {
                long bytes = new FileInfo(apk).Length;
                log.AppendLine($"Size:            {bytes:N0} bytes ({bytes / 1048576f:0.0} MB)");
                log.AppendLine($"SHA-256:         {Sha256(apk)}");
            }
            File.WriteAllText(Path.Combine(OutputFolder, $"Thermo-Tactics-v{Version}-build.txt"), log.ToString());

            if (s.result == BuildResult.Succeeded) Debug.Log($"[Release] Built {apk}.\n{log}");
            else Debug.LogError($"[Release] Build {s.result}: {s.totalErrors} error(s). See the Console.\n{log}");
        }

        private static string[] EnabledScenes()
        {
            var list = new System.Collections.Generic.List<string>();
            foreach (EditorBuildSettingsScene sc in EditorBuildSettings.scenes)
                if (sc.enabled) list.Add(sc.path);
            return list.ToArray();
        }

        private static string Sha256(string path)
        {
            using var sha = SHA256.Create();
            using FileStream f = File.OpenRead(path);
            return BitConverter.ToString(sha.ComputeHash(f)).Replace("-", "").ToLowerInvariant();
        }
    }
}
