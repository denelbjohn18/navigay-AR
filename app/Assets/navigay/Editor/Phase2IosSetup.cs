using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.ARKit;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;

namespace Navigay.Editor
{
    public static class Phase2IosSetup
    {
        const string ArKitLoader = "UnityEngine.XR.ARKit.ARKitLoader";

        public static void Apply()
        {
            try
            {
                ApplyPlayerSettings();
                EnableArKit();
                AssetDatabase.SaveAssets();
                Debug.Log(
                    "navigay phase 2 applied"
                    + " bundle=" + PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS)
                    + " os=" + PlayerSettings.iOS.targetOSVersionString
                    + " backend=" + PlayerSettings.GetScriptingBackend(NamedBuildTarget.iOS)
                    + " arch=" + PlayerSettings.GetArchitecture(NamedBuildTarget.iOS)
                    + " arkit=" + ARKitSettings.currentSettings.requirement);
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                EditorApplication.Exit(1);
            }
        }

        static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "denelbjohn";
            PlayerSettings.productName = "navigay";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.denelbjohn.navigay");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetArchitecture(NamedBuildTarget.iOS, 1);
            PlayerSettings.iOS.targetOSVersionString = "17.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            PlayerSettings.iOS.appleDeveloperTeamID = "WL4F5536UJ";
            PlayerSettings.iOS.cameraUsageDescription = "navigay uses the camera to show directions on the path.";
            PlayerSettings.iOS.locationUsageDescription = "navigay uses your location to guide you around GHS.";
        }

        static void EnableArKit()
        {
            var perBuild = GetOrCreateSettings();
            var group = BuildTargetGroup.iOS;
            if (!perBuild.HasSettingsForBuildTarget(group))
            {
                perBuild.CreateDefaultSettingsForBuildTarget(group);
            }

            if (!perBuild.HasManagerSettingsForBuildTarget(group))
            {
                perBuild.CreateDefaultManagerSettingsForBuildTarget(group);
            }

            var manager = perBuild.ManagerSettingsForBuildTarget(group);
            if (!XRPackageMetadataStore.IsLoaderAssigned(ArKitLoader, group))
            {
                if (!XRPackageMetadataStore.AssignLoader(manager, ArKitLoader, group))
                {
                    throw new InvalidOperationException("ARKit loader was not assigned.");
                }
            }

            var settings = ARKitSettings.currentSettings;
            if (settings == null)
            {
                throw new InvalidOperationException("ARKit settings were not created.");
            }

            settings.requirement = ARKitSettings.Requirement.Required;
            settings.faceTracking = false;
            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(perBuild);
        }

        static XRGeneralSettingsPerBuildTarget GetOrCreateSettings()
        {
            var method = typeof(XRGeneralSettingsPerBuildTarget).GetMethod(
                "GetOrCreate",
                BindingFlags.Static | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new MissingMethodException(nameof(XRGeneralSettingsPerBuildTarget), "GetOrCreate");
            }

            return (XRGeneralSettingsPerBuildTarget)method.Invoke(null, null);
        }
    }
}
