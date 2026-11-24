using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Navigay.Editor
{
    public static class Phase2IosBuild
    {
        public static void Build()
        {
            try
            {
                var output = ReadOutput();
                Directory.CreateDirectory(output);
                var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
                if (scenes.Length == 0)
                {
                    throw new InvalidOperationException("No enabled scenes.");
                }

                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = output,
                    target = BuildTarget.iOS,
                    options = BuildOptions.None,
                });
                if (report.summary.result != BuildResult.Succeeded)
                {
                    throw new InvalidOperationException("iOS export failed: " + report.summary.result);
                }

                Debug.Log("navigay iOS export wrote " + output);
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                EditorApplication.Exit(1);
            }
        }

        static string ReadOutput()
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-buildOutput")
                {
                    return args[i + 1];
                }
            }

            throw new InvalidOperationException("Missing -buildOutput.");
        }
    }
}
