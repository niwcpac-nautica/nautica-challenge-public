using UnityEngine;
using UnityEditor;
using System.IO;

public static class ScoreLog
{
    private static string path = "Assets/FPS/Scripts/Gameplay/Nautica/ScoreLog.txt";

    public static string GetScore()
    {
        StreamReader reader = new StreamReader(path);
        string contents = reader.ReadToEnd();
        reader.Close();
        return contents;
    }

    public static void AddNewScore(string newScore)
    {
        StreamWriter writer = new StreamWriter(path, false);
        writer.WriteLine(newScore);
        writer.Close();
        AssetDatabase.ImportAsset(path);
        TextAsset file = Resources.Load<TextAsset>(path);
    }
}
