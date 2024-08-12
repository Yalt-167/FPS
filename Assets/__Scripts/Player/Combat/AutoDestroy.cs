using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class AutoDestroy : MonoBehaviour
{
    public void StartDestroySequence(float lifeTime)
    {
        Destroy(gameObject, lifeTime);
    }
}
