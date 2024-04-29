using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-10)]
public class Game : MonoBehaviour
{
    public static Game Manager;

    private Stopwatch timer = new();
    public float GameTime => timer.Elapsed;

    #region Debug

    [SerializeField] private bool debugBottomPlane;

    #endregion

    [field: SerializeField] public Settings GameSettings { get; private set; }
    private float[] times;
    public int CurrentSceneID { get; private set; } = 1;

    public Transform SpawnPoint { get; set; }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Manager = this;
        times = new float[] { float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue };
    }


    private void Start()
    {
        Cursor.visible = false;
        //Cursor.lockState = CursorLockMode.Locked;
        Application.targetFrameRate = 120;
    }

    private void Update()
    {
        timer.Update(Time.deltaTime);
        TimerDisplay.Instance.UpdateText(GameTime);
    }

    public void NextLevel()
    {
        times[CurrentSceneID - 1] = GameTime;
        SceneManager.LoadScene(++CurrentSceneID);
        PlayerMovement.Instance.ResetLevel();
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

    public void StartTimer()
    {
        timer.Start();
    }

    public void ResetTimer()
    {
        timer.Reset();
    }
}

[Serializable]
public struct Settings // try having a save on load
{
    public bool viewBobbing;
}