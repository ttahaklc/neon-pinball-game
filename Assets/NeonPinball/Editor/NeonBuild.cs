// Tek tıkla iOS export: mevcut Builds/iOS Xcode projesinin üstüne "Append" ile yazar (imza ayarları ve Xcode değişiklikleri korunur).
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NeonPinball.Editor
{
    public static class NeonBuild
    {
        [MenuItem("Neon Pinball/Build iOS (Xcode projesini güncelle)")]
        public static void BuildIos()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
                Debug.Log("[NeonBuild] Platform iOS'a geçiyor; derleme bitince menüyü bir kez daha çalıştırın.");
                return;
            }
            string dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds/iOS");
            var o = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Main.unity" },
                locationPathName = dir,
                target = BuildTarget.iOS,
                options = Directory.Exists(dir) ? BuildOptions.AcceptExternalModificationsToPlayer : BuildOptions.None
            };
            var r = BuildPipeline.BuildPlayer(o);
            Debug.Log("[NeonBuild] iOS: " + r.summary.result + " · hata=" + r.summary.totalErrors + " · " + dir);
            if (r.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded) EditorUtility.RevealInFinder(dir);
        }
    }
}
