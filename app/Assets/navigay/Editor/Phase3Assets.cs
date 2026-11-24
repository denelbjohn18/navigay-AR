using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Navigay.Editor
{
    public static class Phase3Assets
    {
        public static void ImportTmp()
        {
            var settings = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
            if (File.Exists(settings))
            {
                Debug.Log("navigay tmp essentials already imported");
                EditorApplication.Exit(0);
                return;
            }

            var cache = Path.Combine(Directory.GetCurrentDirectory(), "Library", "PackageCache");
            var match = Directory.GetDirectories(cache, "com.unity.ugui@*").FirstOrDefault();
            if (match == null)
            {
                throw new InvalidOperationException("ugui package is missing.");
            }

            var package = Path.Combine(match, "Package Resources", "TMP Essential Resources.unitypackage");
            if (!File.Exists(package))
            {
                throw new FileNotFoundException("TMP essentials package is missing.", package);
            }

            AssetDatabase.importPackageCompleted += _ =>
            {
                Debug.Log("navigay tmp essentials imported");
                EditorApplication.Exit(0);
            };
            AssetDatabase.importPackageFailed += (_, message) =>
            {
                Debug.LogError(message);
                EditorApplication.Exit(1);
            };
            AssetDatabase.ImportPackage(package, false);
        }
    }
}
