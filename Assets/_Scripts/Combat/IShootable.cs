using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public interface IShootable
{
    public void ReactShot(Vector3 shootingAngle, Vector3 hitPoint);
}