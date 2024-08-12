using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public sealed class DamageLog : MonoBehaviour
{
    private TextMeshProUGUI log;
    public RectTransform rectTransform;
    private static WaitForFixedUpdate awaitFixedUpdate = new();
    private static float gravity = -.7f;
    private static float xMovement = .05f;
    // + rotation

    public int Init(DamageLogSettings damageLogSettings, TargetType targetType, string logText, int verticalOffset)
    {
        log = GetComponent<TextMeshProUGUI>();
        rectTransform = GetComponent<RectTransform>();

        log.text = logText;
        log.fontSize = damageLogSettings.DamageLogSize;
        log.fontStyle = MapTargetTypeToFontStyle(damageLogSettings, targetType);
        log.alignment = damageLogSettings.DisplayOnRight ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;

        log.color = MapTargetTypeToColor(damageLogSettings, targetType);

        rectTransform.sizeDelta = new(log.preferredWidth, log.preferredHeight);
        rectTransform.pivot = new(damageLogSettings.DisplayOnRight ? 0f : 1f, .5f);
        rectTransform.anchoredPosition = new(damageLogSettings.DisplayOnRight ? damageLogSettings.DisplayOffset : -damageLogSettings.DisplayOffset, damageLogSettings.GoingUp ? verticalOffset : -verticalOffset);

        if (damageLogSettings.DynamicLog)
        {
            StartCoroutine(Behave(damageLogSettings.DisplayOnRight));
            return 0;
        }

        return (int)rectTransform.sizeDelta.y;
    }

    private Color MapTargetTypeToColor(DamageLogSettings damageLogSettings, TargetType targetType)
    {
        return damageLogSettings.DamageLogColors[(int)targetType];
    }

    private FontStyles MapTargetTypeToFontStyle(DamageLogSettings damageLogSettings, TargetType targetType)
    {
        return damageLogSettings.DamageLogTextModifiers[(int)targetType];
    }

    private IEnumerator Behave(bool onRight)
    {
        var lifeTimeSoFar = 0f;
        var velocity = new Vector2(0f, 10f);
        var addedVelocityX = onRight ? xMovement : -xMovement;
        for(; ; )
        {
            yield return awaitFixedUpdate;
            lifeTimeSoFar += Time.fixedDeltaTime;
            velocity += new Vector2(addedVelocityX, gravity);
            rectTransform.anchoredPosition += velocity;
        }
    }
}
