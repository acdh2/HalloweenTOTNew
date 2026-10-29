using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;

    [Serializable]
    public struct AudioEvent
    {
        public string name;
        public AudioClip[] audioClips;
    }

    private AudioSource audioSource;
    public List<AudioEvent> audioEvents = new();
    private Dictionary<string, AudioClip[]> audioDictionary = new();

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        audioSource = GetComponent<AudioSource>();

        foreach (var audioEvent in audioEvents)
        {
            if (!string.IsNullOrEmpty(audioEvent.name) && !audioDictionary.ContainsKey(audioEvent.name))
            {
                audioDictionary.Add(audioEvent.name, audioEvent.audioClips);
            }
        }
    }

    public static void PlaySound(string soundName)
    {
        if (instance == null)
        {
            Debug.LogWarning("No AudioManager present in the scene.");
            return;
        }

        if (instance.audioDictionary.TryGetValue(soundName, out var clips) && clips != null && clips.Length > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, clips.Length);
            instance.audioSource.PlayOneShot(clips[randomIndex]);
        }
        else
        {
            Debug.LogWarning($"AudioEvent with name '{soundName}' does not exist or contains no clips.");
        }
    }
}