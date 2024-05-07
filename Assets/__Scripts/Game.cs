using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-10)]
public class Game : MonoBehaviour
{
    public static Game Manager;

    #region Debug

    [SerializeField] private bool debugBottomPlane;

    #endregion

    #region Player Combat List

    private List<PlayerCombatNetworked> playerCombatNetworked = new();

    public void AddPlayerCombatNetworked(PlayerCombatNetworked playerNetworked)
    {
        playerCombatNetworked.Add(playerNetworked);
    }

    public void RemovePlayerCombatNetworked(PlayerCombatNetworked playerNetworked)
    {
        if (!playerCombatNetworked.Contains(playerNetworked)) { return; }

        playerCombatNetworked.Add(playerNetworked);
    }
    public PlayerCombatNetworked GetPlayerCombatNetworkedFromNetworkObjectID(ulong playerObjectID)
    {
        var size = playerCombatNetworked.Count;
        for (int i = 0; i < size; i++)
        {
            if (playerCombatNetworked[i].NetworkObjectId == playerObjectID)
            {
                return playerCombatNetworked[i];
            }
        }

        return null;
    }

    public IEnumerable<PlayerCombatNetworked>PlayersCombatNetworked()
    {
        var size = playerCombatNetworked.Count;
        for (int i = 0; i < size; i++)
        {
            yield return playerCombatNetworked[i];
        }
    }


    #endregion


    [field: SerializeField] public Settings GameSettings { get; private set; }

    public int CurrentSceneID { get; private set; } = 1;

    private void Awake()
    {
        Manager = this;
    }


    private void Start()
    {
        Cursor.visible = false;
        //Cursor.lockState = CursorLockMode.Locked;
        Application.targetFrameRate = 120;
    }

    
    public void NextLevel()
    {
        SceneManager.LoadScene(++CurrentSceneID);
    }

    private void OnDrawGizmos()
    {
        if (debugBottomPlane)
        {

            Gizmos.color = Color.red;
            var origin = new Vector3(transform.position.x, -30, transform.position.z);
            var debugDist = 100f; // how far the plane with be rendered

            var sideward = Vector3.right * debugDist;
            var forward = Vector3.forward * debugDist;
            for (int offset = -100; offset < 101; offset += 10)
            {
                var forwardOffsetVec = new Vector3(0, 0, offset);
                Gizmos.DrawLine(origin - sideward + forwardOffsetVec, origin + sideward + forwardOffsetVec);

                var sidewardOffsetVec = new Vector3(offset, 0, 0);
                Gizmos.DrawLine(origin - forward + sidewardOffsetVec, origin + forward + sidewardOffsetVec);
            }
        }
    }
}

[Serializable]
public struct Settings // try having a save on load
{
    public bool viewBobbing;
}