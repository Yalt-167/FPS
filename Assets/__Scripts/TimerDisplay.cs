using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public sealed class TimerDisplay : MonoBehaviour
{
    public static TimerDisplay Instance;
    private TextMeshProUGUI textMeshPro;
    private void Awake()
    {
        Instance = this;
        textMeshPro = GetComponent<TextMeshProUGUI>();
    }

    public void UpdateText(float currentTime)
    {
        textMeshPro.text = GetFormattedTime(currentTime);
    }

    private string GetFormattedTime(float timeInSeconds)
    {
        var timeInMilliseconds = Mathf.RoundToInt(timeInSeconds * 1000);

        return string.Format("{0:00}:{1:00}.{2:000}", timeInMilliseconds / 60000, timeInMilliseconds % 60000 / 1000, timeInMilliseconds % 1000);
    }
}