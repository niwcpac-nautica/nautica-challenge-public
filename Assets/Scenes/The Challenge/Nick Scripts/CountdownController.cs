using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//using Assets/FPS/Scripts/Game/Managers/GameFlowManager;
public class CountdownController : MonoBehaviour
{

    public int countdownTime;
    public Text countdownDisplay;

    // Start is called before the first frame update
    private void Start()
    {
        StartCoroutine(CountdownToStart());    
    }

    IEnumerator CountdownToStart()
    {
        while(countdownTime > 0)
        {
            countdownDisplay.text = countdownTime.ToString();

            yield return new WaitForSeconds(1f);

            countdownTime--;
        }

       countdownDisplay.text = "GO!";

       yield return new WaitForSeconds(1f);

       countdownDisplay.gameObject.SetActive(false);

        
    }



    // Update is called once per frame
    //void Update()
   // {
        
   // }
}
