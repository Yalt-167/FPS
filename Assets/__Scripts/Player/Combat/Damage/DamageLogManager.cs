using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TMPro;

public class DamageLogManager : MonoBehaviour
{
    [SerializeField] private GameObject damageLogPrefab;
    private List<GameObject> activeDamageLogs = new();

    [Space(10)]
    [Tooltip("Order: Head(Shielded) -> Body(Shielded) -> Legs(Shielded) -> Head -> Body -> Legs -> Object(Weakpoint) -> Object")][SerializeField] private IgnorableNonNullableType<Color>[] baseDamageLogsColors;
    [Tooltip("Order: Head(Shielded) -> Body(Shielded) -> Legs(Shielded) -> Head -> Body -> Legs -> Object(Weakpoint) -> Object")][SerializeField] private IgnorableNonNullableType<FontStyles>[] baseDamageLogsTextModifiers;

    private DamageLogSettings currentSettings;
    private WaitForSeconds damageLogLifetime;

    public void SummonDamageLog(Vector3 position, TargetType targetType, int damage)
    {
        var damageLog = Instantiate(damageLogPrefab, transform);
        damageLog.GetComponent<DamageLog>().Init(currentSettings, targetType, MapHitToLogText(targetType, damage));
        StartCoroutine(HandleDamageLog(damageLog));
    }

    private string MapHitToLogText(TargetType targetType, int damage)
    {
        return targetType switch {
            TargetType.HEAD_SHIELDED or TargetType.HEAD => $"{damage}!",
            TargetType.BODY_SHIELDED or TargetType.BODY => $"{damage}",
            TargetType.LEGS_SHIELDED or TargetType.LEGS => $"{damage}?",
            TargetType.OBJECT_WEAKPOINT or TargetType.OBJECT => $"{damage}",
            _ => ""  
        };
    }

    private IEnumerator HandleDamageLog(GameObject damageLog)
    {
        activeDamageLogs.Add(damageLog);

        yield return damageLogLifetime;

        activeDamageLogs.Remove(damageLog);
        Destroy(damageLog);
    }

    #region Player Settings Modifiers

    public void UpdatePlayerSettings(DamageLogSettings playerDamageLogsSettings)
    {
        currentSettings.DisplayOnRight = playerDamageLogsSettings.DisplayOnRight;

        currentSettings.DisplayOffset = playerDamageLogsSettings.DisplayOffset;

        UpdateDamageLogsColor(playerDamageLogsSettings.DamageLogColors);

        UpdateDamagelogsTextModifiers(playerDamageLogsSettings.DamageLogTextModifiers);

        currentSettings.DynamicLog = playerDamageLogsSettings.DynamicLog;

        damageLogLifetime = new(playerDamageLogsSettings.DamageLogDuration);

        currentSettings.DamageLogSize = playerDamageLogsSettings.DamageLogSize;
    }

    private void UpdateDamageLogsColor(IgnorableNonNullableType<Color>[] playerCustomColors)
    {
        currentSettings.DamageLogColors = new IgnorableNonNullableType<Color>[8];
        for (int i = 0; i < 8; i++)
        {
            currentSettings.DamageLogColors[i] = playerCustomColors[i].Ignore ? baseDamageLogsColors[i] : playerCustomColors[i];
        }
    }

    private void UpdateDamagelogsTextModifiers(IgnorableNonNullableType<FontStyles>[] playerDamageLogsTextModifiers)
    {
        currentSettings.DamageLogTextModifiers = new IgnorableNonNullableType<FontStyles>[8];
        for (int i = 0; i < 8; i++)
        {
            currentSettings.DamageLogTextModifiers[i] = playerDamageLogsTextModifiers[i].Ignore ? baseDamageLogsTextModifiers[i] : playerDamageLogsTextModifiers[i];
        }
    }

    #endregion
}