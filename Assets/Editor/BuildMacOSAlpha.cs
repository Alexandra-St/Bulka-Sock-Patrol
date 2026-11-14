using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BulkoGame.EditorTools
{
    public static class BuildMacOSAlpha
    {
        [MenuItem("Bulka: Sock Patrol/Build macOS Alpha...")]
        public static void BuildAlpha()
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            string outputDirectory = EditorUtility.SaveFolderPanel("Build macOS Alpha", Path.Combine(root, "Builds", "macOS"), "Alpha_");
            if (!string.IsNullOrEmpty(outputDirectory)) Build(outputDirectory);
        }

        public static void Build(string outputDirectory)
        {
            Directory.CreateDirectory(outputDirectory);
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(outputDirectory, "Bulka Sock Patrol.app"),
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                Debug.LogError("macOS build failed: " + report.summary.result);
            else
            {
                string zipPath = Path.Combine(Directory.GetParent(outputDirectory).FullName, "BulkaSockPatrol_Alpha_0.7_macOS.zip");
                if (File.Exists(zipPath)) File.Delete(zipPath);
                ZipFile.CreateFromDirectory(outputDirectory, zipPath, System.IO.Compression.CompressionLevel.Optimal, false);
                Debug.Log("macOS build ready at " + options.locationPathName);
            }
        }
    }
}
