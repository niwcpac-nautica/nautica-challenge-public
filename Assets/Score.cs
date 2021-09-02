using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Score : MonoBehaviour
{
    public TMP_Text score;
    public ScoreLog scoreLog;

    // Start is called before the first frame update
    void Start()
    {
        score.GetComponent<TMP_Text>();
        scoreLog.GetComponent<ScoreLog>();
        score.text = scoreLog.GetScore();
    }
}
