using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BulkoGame.EditorTools
{
    public static class BuildWindowsAlpha
    {
        [MenuItem("BulkoGame/Build Windows Alpha...")]
        public static void BuildAlpha()
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            string outputDirectory = EditorUtility.SaveFolderPanel("Build Windows Alpha", Path.Combine(root, "Builds", "Windows"), "Alpha_");
            if (!string.IsNullOrEmpty(outputDirectory)) Build(outputDirectory);
        }

        public static void Build(string outputDirectory)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            Directory.CreateDirectory(outputDirectory);

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(outputDirectory, "BulkoGame.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                Debug.LogError("Windows build failed: " + report.summary.result);
            else
            {
                string readmeSource = Path.Combine(projectRoot, "Builds", "Windows", "README_TEST.txt");
                if (File.Exists(readmeSource))
                    File.Copy(readmeSource, Path.Combine(outputDirectory, "README_TEST.txt"), true);
                string zipPath = Path.Combine(Directory.GetParent(outputDirectory).FullName, "BulkoGame_" + Path.GetFileName(outputDirectory) + "_Windows.zip");
                if (File.Exists(zipPath)) File.Delete(zipPath);
                ZipFile.CreateFromDirectory(outputDirectory, zipPath, System.IO.Compression.CompressionLevel.Optimal, false);
                Debug.Log("Windows build ready at " + options.locationPathName);
            }
        }
    }
}
