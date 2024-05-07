using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DamageLog : MonoBehaviour
{
    private TextMeshProUGUI log;
    public RectTransform rectTransform;
    private bool dynamic;

    public float Init(DamageLogSettings damageLogSettings, TargetType targetType, string logText)
    {
        log = GetComponent<TextMeshProUGUI>();
        rectTransform = GetComponent<RectTransform>();

        log.text = logText;
        log.fontSize = damageLogSettings.DamageLogSize;
        log.fontStyle = MapTargetTypeToFontStyle(damageLogSettings, targetType);
        log.alignment = damageLogSettings.DisplayOnRight ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;

        log.color = MapTargetTypeToColor(damageLogSettings, targetType);

        rectTransform.anchoredPosition = new(damageLogSettings.DisplayOnRight ? damageLogSettings.DisplayOffset : -damageLogSettings.DisplayOffset, 0);

        return rectTransform.sizeDelta.y;
    }

    private Color MapTargetTypeToColor(DamageLogSettings damageLogSettings, TargetType targetType)
    {
        return damageLogSettings.DamageLogColors[(int)targetType];
    }

    private FontStyles MapTargetTypeToFontStyle(DamageLogSettings damageLogSettings, TargetType targetType)
    {
        return damageLogSettings.DamageLogTextModifiers[(int)targetType];
    }

    public void FixedUpdate()
    {
        if (!dynamic) { return; }
    }
}
