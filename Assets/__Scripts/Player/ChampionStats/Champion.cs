using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Champion : MonoBehaviour
{
    private ChampionStats championStats;

    public interface IPrimaryAbilityParam { }
    public delegate void PrimaryAbility(IPrimaryAbilityParam param);
    public event PrimaryAbility OnPrimaryAbilityTriggered;
    public abstract IPrimaryAbilityParam GetPrimaryAbilityParam();
    public void TriggerPrimaryAbility(IPrimaryAbilityParam param)
    {
        OnPrimaryAbilityTriggered?.Invoke(param);
    }

    public interface ISecondaryAbilityParam { }
    public delegate void SecondaryAbility(ISecondaryAbilityParam param);
    public event SecondaryAbility OnSecondaryAbilityTriggered;
    public abstract ISecondaryAbilityParam GetSecondaryAbilityParam();
    public void TriggerSecondaryAbility(ISecondaryAbilityParam param)
    {
        OnSecondaryAbilityTriggered?.Invoke(param);
    }

    public interface IOnJumpParam { }
    public delegate void JumpEvent(IOnJumpParam param);
    public event JumpEvent OnJump;
    public abstract IOnJumpParam GetOnJumpParam();
    public void TriggerJump(IOnJumpParam param)
    {
        OnJump?.Invoke(param);
    }

    public interface IOnLandParam { }
    public delegate void LandEvent(IOnLandParam param);
    public event LandEvent OnLand;
    public abstract IOnLandParam GetOnLandParam();
    public void TriggerLand(IOnLandParam param)
    {
        OnLand?.Invoke(param);
    }

    public interface IOnDashParam { }
    public delegate void DashEvent(IOnDashParam param);
    public event DashEvent OnDash;
    public abstract IOnDashParam GetOnDashParam();
    public void TriggerDash(IOnDashParam param)
    {
        OnDash?.Invoke(param);
    }
}








