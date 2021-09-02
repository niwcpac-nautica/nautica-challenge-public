using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class ScoreLog : MonoBehaviour
{
    private string path = "Assets/FPS/Scripts/Gameplay/Nautica/ScoreLog.txt";

    public string GetScore()
    {
        StreamReader reader = new StreamReader(path);
        string contents = reader.ReadToEnd();
        reader.Close();
        return contents;
    }

    public void AddNewScore(string newScore)
    {
        StreamWriter writer = new StreamWriter(path, false);
        writer.WriteLine(newScore);
        writer.Close();
        AssetDatabase.ImportAsset(path);
        TextAsset file = Resources.Load<TextAsset>(path);
    }
}
