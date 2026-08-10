using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

/// <summary>
/// Unity and editor-only avatar packages can infer Android permissions from APIs
/// that V-STIPA never calls. Remove those permissions from every generated source
/// manifest before Gradle performs its final merge.
/// </summary>
public sealed class QuestAndroidManifestPostprocessor : IPostGenerateGradleAndroidProject
{
    private static readonly HashSet<string> RemovedPermissions = new HashSet<string>
    {
        "android.permission.INTERNET",
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

            if (toRemove.Count > 0) document.Save(manifestPath);
        }

        Debug.Log($"[QuestManifest] Removed {removals} unused network, microphone, and eye-tracking manifest entries.");
    }
}
