using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Score : MonoBehaviour
{
    public TMP_Text score;

    void Start()
    {
        score.GetComponent<TMP_Text>();
        score.text = ScoreLog.GetScore();
    }
}
