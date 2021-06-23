using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Timer : MonoBehaviour
{
    public float timeValue = 1;
    public Text timerText;
    [Header("Lose")] [Tooltip("This string has to be the name of the scene you want to load when losing")]
    public string LoseSceneName = "Lose";
    //public GameObject tick;


    // Update is called once per frame
    void Start()
    {
        timerText = GetComponent<Text>();
    }
    void awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }
    void Update()
    {
        
        if (timeValue > 0)
        {
            timeValue -= Time.deltaTime;
        } 
        else if(timeValue == 0)
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
