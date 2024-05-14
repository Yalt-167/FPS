using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    public Dictionary<string, AudioClip> Sounds = new();


    public void RegisterSound(string name, AudioClip sound)
    {
        Sounds.Add(name, sound);
    }

    public void DiscardSound(string name)
    {
        Sounds.Remove(name);
    }

    public void ManageSoundVolume(string name)
    {
        //Sounds[name].
    }
}
