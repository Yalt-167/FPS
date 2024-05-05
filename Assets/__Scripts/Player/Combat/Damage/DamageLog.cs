using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DamageLog : MonoBehaviour
{
    private TextMeshProUGUI scoreRenderer;

    public void Init(Color color, string logText)
    {
        scoreRenderer = GetComponent<TextMeshProUGUI>();
        print("not yet");
        scoreRenderer.color = color;
        print("made it");
        scoreRenderer.fontSize = 18;
        scoreRenderer.text = logText;

        GetComponent<RectTransform>().anchoredPosition = new(0, 0);
    }
}
