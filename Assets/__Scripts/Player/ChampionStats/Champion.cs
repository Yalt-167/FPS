using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Champion : MonoBehaviour
{
    private ChampionStats championStats;

    private delegate void PrimaryAbility();
    private event PrimaryAbility onPrimaryAbilityTriggered;

    private delegate void SecondaryAbility();
    private event SecondaryAbility onSecondaryAbilityTriggered;

    private delegate void JumpEvent();
    private event JumpEvent onJump;

    private delegate void LandEvent();
    private event LandEvent onLand;
}
}


