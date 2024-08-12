using System.Collections;
using System.Collections.Generic;

public sealed class Stopwatch
{
    public float Elapsed { get; private set; }
    public bool Running { get; private set; }
    private List<float> laps = new List<float>();

    public void Start()
    {
        Running = true;
    }

    public void Pause()
    {
        Running = false;
    }

    public void Stop()
    {
        Pause();
    }

    public float Lap()
    {    
        laps.Add(Elapsed);
        return Elapsed;
    }

    public void Update(float deltaTime)
    {
        if (Running)
        {
            Elapsed += deltaTime;
        }
    }

    public float Reset()
    {
        var toReturn = Elapsed;
        Elapsed = 0;
        laps.Clear();
        Running = false;
        return toReturn;
    }
}
