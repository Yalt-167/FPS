using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Layers
{
    public static readonly LayerMask Ground = LayerMask.GetMask("Ground");
    public static readonly LayerMask PlayerHitBoxes = LayerMask.GetMask("ShootablePlayerHitBox");
}
