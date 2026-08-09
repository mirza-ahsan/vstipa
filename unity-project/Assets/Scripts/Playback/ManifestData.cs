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
    public List<QuestionItemData> questions;
}
