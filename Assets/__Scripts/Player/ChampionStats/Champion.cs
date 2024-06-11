using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Champion : MonoBehaviour
{
    private ChampionStats championStats;

    public interface IPrimaryAbililtyParam { }
    private delegate void PrimaryAbility(IPrimaryAbililtyParam param);
    private event PrimaryAbility OnPrimaryAbilityTriggered;

    public interface ISecondaryAbiltyParam { }
    private delegate void SecondaryAbility(ISecondaryAbiltyParam param);
    private event SecondaryAbility OnSecondaryAbilityTriggered;

    public interface IOnJumpParam { }
    private delegate void JumpEvent(IOnJumpParam param);
    private event JumpEvent OnJump;

    public interface IOnLandParam { }
    private delegate void LandEvent(IOnLandParam param);
    private event LandEvent OnLand;

    public interface IOnDashParam { }
    private delegate void DashEvent(IOnDashParam param);
    private event DashEvent OnDash;
}








