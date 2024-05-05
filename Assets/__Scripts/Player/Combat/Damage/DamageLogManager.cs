using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DamageLogManager : MonoBehaviour
{
    [Tooltip("Order: Head(Shielded) -> Body(Shielded) -> Legs(Shielded) -> Head -> Body -> Legs -> Object(Weakpoint) -> Object")][SerializeField] private Color[] hitColors;
    [SerializeField] private GameObject damageLogPrefab;
    private List<GameObject> activeDamageLogs = new();

    private WaitForSeconds hitMarkerLifeTime;
    private RectTransform rectTransform;



    public void SummonDamageLog(Vector3 position, TargetType targetType, int damage)
    {
        GetComponent<RectTransform>().position = position;
        var damageLog = Instantiate(damageLogPrefab, transform);
        damageLog.GetComponent<DamageLog>().Init(MapTargetTypeToColor(targetType), MapHitToLogText(targetType, damage));
        StartCoroutine(HandleDamageLog(damageLog));
    }

    private Color MapTargetTypeToColor(TargetType targetType)
    {
        return hitColors[(int)targetType];
    }

    private string MapHitToLogText(TargetType targetType, int damage)
    {
        return targetType switch {
            TargetType.HEAD_SHIELDED or TargetType.HEAD => $"{damage}!",
            TargetType.BODY_SHIELDED or TargetType.BODY => $"{damage}",
            TargetType.Legs_SHIELDED or TargetType.LEGS => $"{damage}?",
            TargetType.OBJECT_WEAKPOINT => $"{damage}",
            TargetType.OBJECT => $"{damage}",
            _ => ""  
        };
    }

    private IEnumerator HandleDamageLog(GameObject damageLog)
    {
        activeDamageLogs.Add(damageLog);

        yield return hitMarkerLifeTime;

        activeDamageLogs.Remove(damageLog);
        Destroy(damageLog);
    }

    public void UpdatePlayerSettings(HitMarkerSettings playerHitMarkerSettings)
    {
        hitMarkerLifeTime = new(playerHitMarkerSettings.HitMarkerDuration);
    }


}
