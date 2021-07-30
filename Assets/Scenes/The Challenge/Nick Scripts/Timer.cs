using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Timer : MonoBehaviour
{
    /// <summary>
    /// You need to change timeValue here! It cant be done in the scene because it has to be static accross all scenes.
    /// </summary>
    static public float timeValue = 900;
    // This is where you put in the text object in the scene so it shows up in game.
    public Text timerText;
    [Header("Lose")] [Tooltip("This string has to be the name of the scene you want to load when losing")]
    public string LoseSceneName = "Lose";
    

    // Update is called once per frame
    void Start()
    {
        timerText = GetComponent<Text>();
    }
    void awake()
    {
        DontDestroyOnLoad(transform.gameObject);
    }
    void Update()
    {
        
        if (timeValue > 0)
        {
            timeValue -= Time.deltaTime;
        } 
        if(timeValue <= 0)
        {
            SceneManager.LoadScene(LoseSceneName);
        }

        DisplayTime(timeValue);
    }

    void DisplayTime(float timeToDisplay)
    {
        if(timeToDisplay < 0)
        {
            timeToDisplay = 0;
        }
        
        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);

        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
