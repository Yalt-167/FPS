using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

[DefaultExecutionOrder(-10)]
public class Game : MonoBehaviour
{
    public static Game Manager;

    #region Debug

    [SerializeField] private bool debugBottomPlane;

    #endregion

    #region Player Combat List

    private List<WeaponHandler> networkedWeaponHandlers = new();

    public void AddNetworkedWeaponHandler(WeaponHandler networkedWeaponHandler)
    {
        networkedWeaponHandlers.Add(networkedWeaponHandler);
    }

    public void DiscardNetworkedWeaponHandler(WeaponHandler networkedWeaponHandler)
    {
        if (!networkedWeaponHandlers.Contains(networkedWeaponHandler)) { return; }

        networkedWeaponHandlers.Add(networkedWeaponHandler);
    }

    public WeaponHandler GetNetworkedWeaponHandlerFromNetworkObjectID(ulong playerObjectID) // ? make it so they are sorted ? -> slower upon adding it but faster to select it still
    {
        var size = networkedWeaponHandlers.Count;
        for (int i = 0; i < size; i++)
        {
            if (networkedWeaponHandlers[i].NetworkObjectId == playerObjectID)
            {
                return networkedWeaponHandlers[i];
            }
        }

        return null;
    }

    public IEnumerable<WeaponHandler> GetNetworkedWeaponHandlers()
    {
        for (int i = 0; i < networkedWeaponHandlers.Count; i++)
        {
            yield return networkedWeaponHandlers[i];
        }
    }


    #endregion

    #region Respawn Logic

    private Dictionary<ushort, List<SpawnPoint>> spawnPoints = new Dictionary<ushort, List<SpawnPoint>>();


    public void AddRespawnPoint(SpawnPoint spawnPoint)
    {
        if (!spawnPoints.ContainsKey(spawnPoint.TeamID))
        {
            spawnPoints.Add(spawnPoint.TeamID, new());
        }

        spawnPoints[spawnPoint.TeamID].Add(spawnPoint);
    }


    public void DiscardRespawnPoint(SpawnPoint spawnPoint)
    {
        spawnPoints[spawnPoint.TeamID].Remove(spawnPoint);
    }

    public Vector3 GetSpawnPosition(ushort teamID)
    {
        print(0);
        var relevantSpawnPoints = spawnPoints[teamID];
        var relevantSpawnPointsCount = relevantSpawnPoints.Count;
        print(1);

        // filtering the active ones
        var activeRelevantSpawnPoints = new List<SpawnPoint>();
        for (int i = 0; i < relevantSpawnPointsCount; i++)
        {
            if (relevantSpawnPoints[i].Active)
            {
                activeRelevantSpawnPoints.Add(relevantSpawnPoints[i]);
            }
        }
        print(2);
        var activeRelevantSpawnPointsCount = activeRelevantSpawnPoints.Count;
        if (activeRelevantSpawnPointsCount == 0) { throw new Exception($"There s no checkpoint available for this player with team ID: {teamID}"); }
        print(3);

        return activeRelevantSpawnPoints[Random.Range(0, activeRelevantSpawnPointsCount - 1)].SpawnPosition;
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