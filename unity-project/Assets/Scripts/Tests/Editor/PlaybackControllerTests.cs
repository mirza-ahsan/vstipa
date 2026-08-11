using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class PlaybackControllerTests
{
    [Test]
    public void TestBakedWavIsReadableForLipSync()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "questions", "warm", "q01.wav");
        Assert.IsTrue(File.Exists(path));

        AudioClip clip = WavUtility.ToAudioClip(File.ReadAllBytes(path), "warm_q01_test");
        Assert.IsNotNull(clip);
        Assert.Greater(clip.length, 1f);
        Assert.AreEqual(24000, clip.frequency);
        Assert.AreEqual(1, clip.channels);

        Object.DestroyImmediate(clip);
    }

    [Test]
    public void TestManifestJsonDeserialization()
    {
        string json = @"{
            ""persona"": ""warm"",
            ""persona_name"": ""Warm & Encouraging Interviewer"",
            ""total_questions"": 2,
            ""questions"": [
                {
                    ""id"": 1,
                    ""question"": ""Tell me about yourself."",
                    ""tone"": ""warm"",
                    ""gesture"": ""nod"",
                    ""audio_file"": ""q01.wav""
                },
                {
                    ""id"": 2,
                    ""question"": ""What is your biggest accomplishment?"",
                    ""tone"": ""warm"",
                    ""gesture"": ""smile"",
                    ""audio_file"": ""q02.wav""
                }
            ]
        }";

        PersonaManifestData data = JsonUtility.FromJson<PersonaManifestData>(json);

        Assert.IsNotNull(data);
        Assert.AreEqual("warm", data.persona);
        Assert.AreEqual(2, data.total_questions);
        Assert.AreEqual(2, data.questions.Count);
        Assert.AreEqual("nod", data.questions[0].gesture);
        Assert.AreEqual("q02.wav", data.questions[1].audio_file);
    }

    [Test]
    public void TestQuestionAdvancementBounds()
    {
        GameObject go = new GameObject("TestPlayback");
        QuestionPlaybackController controller = go.AddComponent<QuestionPlaybackController>();

        controller.currentManifest = new PersonaManifestData
        {
            persona = "test",
            persona_name = "Test Persona",
            total_questions = 2,
            questions = new List<QuestionItemData>
            {
                new QuestionItemData { id = 1, question = "Q1", tone = "warm", gesture = "nod", audio_file = "q01.wav" },
                new QuestionItemData { id = 2, question = "Q2", tone = "warm", gesture = "nod", audio_file = "q02.wav" }
            }
        };

        Assert.AreEqual(-1, controller.currentQuestionIndex);

        controller.AdvanceToNextQuestion();
        Assert.AreEqual(0, controller.currentQuestionIndex);

        controller.AdvanceToNextQuestion();
        Assert.AreEqual(1, controller.currentQuestionIndex);

        controller.AdvanceToNextQuestion();
        Assert.AreEqual(1, controller.currentQuestionIndex);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void TestLiveAudioUrlIsUsedWithoutStreamingAssetsPrefix()
    {
        GameObject go = new GameObject("TestLiveAudioUrl");
        QuestionPlaybackController controller = go.AddComponent<QuestionPlaybackController>();
        QuestionItemData item = new QuestionItemData
        {
            id = 1,
            audio_file = "http://127.0.0.1:8001/api/interviews/session/audio/q01.wav"
        };

        Assert.AreEqual(item.audio_file, controller.ResolveAudioUrl(item));
        Object.DestroyImmediate(go);
    }

    [Test]
    public void TestRoleInterviewRequestSerialization()
    {
        RoleInterviewRequestData request = new RoleInterviewRequestData
        {
            role = "Senior Backend Engineer",
            persona = "neutral"
        };

        string json = JsonUtility.ToJson(request);
        StringAssert.Contains("Senior Backend Engineer", json);
        StringAssert.Contains("neutral", json);
    }

    [Test]
    public void TestSeatedPoseBindsAndBendsBothLegs()
    {
        GameObject avatar = new GameObject("Avatar");
        Transform hips = Child(avatar.transform, "Hips");
        Transform leftUpperLeg = Child(hips, "LeftUpLeg");
        Transform leftLowerLeg = Child(leftUpperLeg, "LeftLeg");
        Transform rightUpperLeg = Child(hips, "RightUpLeg");
        Transform rightLowerLeg = Child(rightUpperLeg, "RightLeg");
        SeatedInterviewerPose pose = avatar.AddComponent<SeatedInterviewerPose>();

        Assert.IsTrue(pose.BindRig());
        pose.ApplySeatedPose();

        Assert.That(Quaternion.Angle(Quaternion.identity, leftUpperLeg.localRotation), Is.GreaterThan(70f));
        Assert.That(Quaternion.Angle(Quaternion.identity, rightUpperLeg.localRotation), Is.GreaterThan(70f));
        Assert.That(Quaternion.Angle(Quaternion.identity, leftLowerLeg.localRotation), Is.GreaterThan(70f));
        Assert.That(Quaternion.Angle(Quaternion.identity, rightLowerLeg.localRotation), Is.GreaterThan(70f));
        Object.DestroyImmediate(avatar);
    }

    [Test]
    public void TestQuestManifestPreservesLiveBackendInternetOnly()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), $"vstipa-manifest-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        string manifestPath = Path.Combine(tempDirectory, "AndroidManifest.xml");
        File.WriteAllText(manifestPath, @"<?xml version=""1.0"" encoding=""utf-8""?>
<manifest xmlns:android=""http://schemas.android.com/apk/res/android"">
  <uses-permission android:name=""android.permission.INTERNET"" />
  <uses-permission android:name=""android.permission.ACCESS_NETWORK_STATE"" />
  <uses-permission android:name=""android.permission.RECORD_AUDIO"" />
  <application android:label=""V-STIPA"" />
</manifest>");

        try
        {
            new QuestAndroidManifestPostprocessor().OnPostGenerateGradleAndroidProject(tempDirectory);
            string processed = File.ReadAllText(manifestPath);
            StringAssert.Contains("android.permission.INTERNET", processed);
            StringAssert.Contains("usesCleartextTraffic=\"true\"", processed);
            StringAssert.DoesNotContain("android.permission.ACCESS_NETWORK_STATE", processed);
            StringAssert.DoesNotContain("android.permission.RECORD_AUDIO", processed);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    private static Transform Child(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child.transform;
    }
}
