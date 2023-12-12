using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    private int score;
    [SerializeField] private TMP_Text scoreText; 

    public void ScorePoint()
    {
        score++;
        scoreText.text = score.ToString();

    }
}
