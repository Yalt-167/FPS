using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class PersistentComponent : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
