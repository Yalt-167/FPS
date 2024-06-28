using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChampionStats", menuName = "ScriptableObjects/ChampionStats")]
public class ChampionStats : ScriptableObject
{
    public ChampionMovementStats MovementStats;
    public ChampionCooldowns Cooldowns;
}


[Serializable]
public struct ChampionMovementStats
{
    public ChampionSpeedStats SpeedStats;
    public ChampionJumpStats JumpStats;
    public ChampionDashStats DashStats;
}

[Serializable]
public struct ChampionSpeedStats
{
    public float RunningSpeed;
    public float StrafingSpeed;
    public float BackwardSpeed;
    public float WallRunningSpeed;
}

[Serializable]
public struct ChampionJumpStats
{
    public float JumpForce;
    public float SideWallJumpForce;
    public float UpwardWallJumpForce;
}


[Serializable]
public struct ChampionDashStats
{
    public bool HasDash;
    public float DashVelocity;
    public float DashDuration;
    public float DashCooldown;
}


[Serializable]
public struct ChampionCooldowns
{
    public float PrimaryAbilityCooldown;
    public float SecondaryAbilityCooldown;
}



// do a Champion abstract class
// bind event to action (OnJump, OnDash, OnLand, etc)
// each champion inherits it

// also account for damage in the stats