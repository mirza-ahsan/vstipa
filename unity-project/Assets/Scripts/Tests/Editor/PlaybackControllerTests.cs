using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class PlaybackControllerTests
{
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
}
