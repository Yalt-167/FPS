using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    public Dictionary<string, AudioSource> AudioSources = new();


    public void RegisterSound(string name, AudioSource sound)
    {
        AudioSources.Add(name, sound);
    }

    public void DiscardSound(string name)
    {
        AudioSources.Remove(name);
    }

    public void ManageSoundVolume(string name, int volumePercentage)
    {
        AudioSources[name].volume = volumePercentage / 100;
    }
}
