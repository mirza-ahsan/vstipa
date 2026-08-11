using System;
using System.Collections.Generic;

[Serializable]
public class QuestionItemData
{
    public int id;
    public string question;
    public string tone;
    public string gesture;
    public string audio_file;
}

[Serializable]
public class PersonaManifestData
{
    public string persona;
    public string persona_name;
    public int total_questions;
    public string role;
    public string source;
    public string model;
    public string session_id;
    public List<QuestionItemData> questions;
}

[Serializable]
public class RoleInterviewRequestData
{
    public string role;
    public string persona;
}
