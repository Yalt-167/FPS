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

        log.text = logText;
        log.fontSize = damageLogSettings.DamageLogSize;
        log.fontStyle = MapTargetTypeToFontStyle(damageLogSettings, targetType);
        log.alignment = damageLogSettings.DisplayOnRight ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;

        log.color = MapTargetTypeToColor(damageLogSettings, targetType);

        rectTransform = GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new(damageLogSettings.DisplayOnRight ? damageLogSettings.DisplayOffset : -damageLogSettings.DisplayOffset, 0);

        return rectTransform.sizeDelta.y;
    }

    private Color MapTargetTypeToColor(DamageLogSettings damageLogSettings, TargetType targetType)
    {
        return damageLogSettings.DamageLogColors[(int)targetType];
    }

    private FontStyles MapTargetTypeToFontStyle(DamageLogSettings damageLogSettings, TargetType targetType)
    {
        return damageLogSettings.DamageLogTextModifiers[(int)targetType]; //switch
        //{
        //    TextModifier.ITALIC => FontStyles.Italic,
        //    TextModifier.BOLD => FontStyles.Bold,
        //    TextModifier.UNDERLINED => FontStyles.Underline,
        //    TextModifier.STRIKED => FontStyles.Strikethrough,
        //    TextModifier.VANILLA or TextModifier.NONE or _ => FontStyles.Normal,
        //};
    }

    public void FixedUpdate()
    {
        if (!dynamic) { return; }
    }
}
