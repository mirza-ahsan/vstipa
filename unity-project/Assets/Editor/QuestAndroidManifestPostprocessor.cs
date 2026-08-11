using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

/// <summary>
/// Keep the one network permission required by optional role-based interviews while
/// removing unrelated avatar-package permissions before Gradle's final merge.
/// </summary>
public sealed class QuestAndroidManifestPostprocessor : IPostGenerateGradleAndroidProject
{
    private static readonly HashSet<string> RemovedPermissions = new HashSet<string>
    {
        "android.permission.ACCESS_NETWORK_STATE",
        "android.permission.RECORD_AUDIO",
        "com.oculus.permission.EYE_TRACKING"
    };

    public int callbackOrder => 1000;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        int removals = 0;
        foreach (string manifestPath in Directory.GetFiles(path, "AndroidManifest.xml", SearchOption.AllDirectories))
        {
            var document = new XmlDocument { PreserveWhitespace = true };
            document.Load(manifestPath);
            XmlElement root = document.DocumentElement;
            if (root == null) continue;

            XmlElement application = root.SelectSingleNode("application") as XmlElement;
            if (application != null)
            {
                application.SetAttribute("usesCleartextTraffic", "http://schemas.android.com/apk/res/android", "true");
            }

            var toRemove = new List<XmlNode>();
            foreach (XmlNode child in root.ChildNodes)
            {
                if (child is not XmlElement element) continue;
                string androidName = element.GetAttribute("name", "http://schemas.android.com/apk/res/android");
                if ((element.Name == "uses-permission" && RemovedPermissions.Contains(androidName)) ||
                    (element.Name == "uses-feature" &&
                        (androidName == "oculus.software.eye_tracking" || androidName == "android.hardware.microphone")))
                {
                    toRemove.Add(element);
                }
            }

            foreach (XmlNode node in toRemove)
            {
                root.RemoveChild(node);
                removals++;
            }

            document.Save(manifestPath);
        }

        Debug.Log($"[QuestManifest] Preserved live-backend internet access and removed {removals} unused network-state, microphone, and eye-tracking entries.");
    }
}
