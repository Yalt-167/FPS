using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-9)]
public class BoxCasterManager : MonoBehaviour
{
    public static BoxCasterManager Instance;

    private List<BoxCaster> boxCasters = new();
    private readonly WaitForSeconds discardDelay = new(.1f);

    private void Awake()
    {
        Instance = this;
    }

    public void RegistedBoxCaster(BoxCaster boxCaster)
    {
        boxCasters.Add(boxCaster);
    }

    public BoxCaster RetrieveBoxCasterFromInstance(BoxCasterInstances wantedBoxCasterInstance)
    {
        foreach (var boxCaster in boxCasters)
        {
            if (boxCaster.Instance == wantedBoxCasterInstance)
            {
                StartCoroutine(DiscardAssignedBoxCaster(boxCaster));
                return boxCaster;
            }
        }

        throw new System.Exception($"There should be one {wantedBoxCasterInstance}; Game can t be played with insufficient collider amount");
    }

    private IEnumerator DiscardAssignedBoxCaster(BoxCaster assignedBoxCaster)
    {
        yield return discardDelay; // await the end of the foreach to ensure not throwing an error du to modifying list while iterating over it
        boxCasters.Remove(assignedBoxCaster);
    }

}
