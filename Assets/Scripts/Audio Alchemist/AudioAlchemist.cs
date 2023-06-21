using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
/// <summary>
/// Audio Alchemist is a tool created by "NR".
/// It lets you control easily any sound from any script using a simple line of code.
/// Drag and drop from the inspector to any GameObject and review the options available
/// or check the documentation attached.
/// </summary>
/// 
public class AudioAlchemist : MonoBehaviour
{
    public static AudioAlchemist Instance;
    public SoundSubject[] soundSubjects;
    Dictionary<string, SoundSubject> soundSubjectDictionary;
    void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        soundSubjectDictionary = new Dictionary<string, SoundSubject>();
        foreach (var soundSubject in soundSubjects)
        {
            soundSubjectDictionary[soundSubject.groupName] = soundSubject;
            foreach (var sound in soundSubject.sounds)
            {
                sound.source = gameObject.AddComponent<AudioSource>();
                sound.source.clip = sound.clip;
                sound.source.volume = sound.volume;
                sound.source.pitch = sound.pitch;
                sound.source.loop = sound.loop;
            }
        }
    }

    /// <summary>
    /// Triggers any sound located in the Sound Manager, no matter the "Group Name".
    /// </summary>
    /// <param name="soundName">The clip name or string named from Sound Manager</param>
    public void PlaySound(string soundName)
    {
        foreach (var soundSubject in soundSubjects)
        {
            foreach (var sound in soundSubject.sounds)
            {
                if (sound.clipName != soundName) continue;
                if (sound.fadeIn)
                {
                    StartCoroutine(sound.FadeInRoutine(sound.source));
                    return;
                }
                sound.source.Play();
                return;
            }
        }
    }

    /// <summary>
    /// Stops any sound located in the Sound Manager, no matter the "Group Name".
    /// </summary>
    /// <param name="soundName">The clip name or string named from Sound Manager</param>
    public void StopSound(string soundName)
    {
        foreach (var soundSubject in soundSubjects)
        {
            foreach (var sound in soundSubject.sounds)
            {
                if (sound.clipName != soundName) continue;
                if (sound.fadeOut)
                {
                    StartCoroutine(sound.FadeOutRoutine(sound.source));
                    return;
                }
                sound.source.Stop();
                return;
            }
        }
    }

    /// <summary>
    /// Control all the volume from specific array from Sound Manager.
    /// Use Canvas/Slider to control the volume or any other type of buttons. Or feed directly the "Volume" with a fixed value.
    /// </summary>
    /// <param name="subjectName">The name or string typed on "Group Name" from Sound Manager</param>
    /// <param name="volume">From 0 - 1 Where 0 is no sound and 1 the highest volume available</param>
    public void UpdateSubjectVolume(string subjectName, float volume)
    {
        if (!soundSubjectDictionary.TryGetValue(subjectName, out var soundSubject)) return;
        foreach (var sound in soundSubject.sounds)
        {
            sound.source.volume = volume;
        }
    }
}

[Serializable]
public class Sounds
{
    [Tooltip("Use this string to Play/Stop the sound")]
    public string clipName;
    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume = 1f;
    [Range(.1f, 2f)]
    public float pitch = 1f;

    public bool loop;

    [Tooltip("If enabled, the sound will smoothly fade in")]
    public bool fadeIn;
    [Tooltip("If enabled, the sound will smoothly fade out")]
    public bool fadeOut;
    [Tooltip("The duration of the fade in/out. Must have any 'Fade In' or 'Fade Out' enabled")]
    public float fadeDuration;

    [HideInInspector] public AudioSource source;

    /// <summary>
    /// Coroutine to Fade in any sound from Audio Alchemist
    /// </summary>
    public IEnumerator FadeInRoutine(AudioSource source)
    {
        var currentTime = 0f;
        var startVolume = source.volume;
        source.volume = 0f;
        source.Play();

        while (currentTime < fadeDuration)
        {
            currentTime += Time.deltaTime;
            source.volume = Mathf.Lerp(0f, startVolume, currentTime / fadeDuration);
            yield return null;
        }
        source.volume = startVolume;
    }

    /// <summary>
    /// Coroutine to Fade out any sound from Audio Alchemist
    /// </summary>
    public IEnumerator FadeOutRoutine(AudioSource source)
    {
        var currentTime = 0f;
        var startVolume = source.volume;

        while (currentTime < fadeDuration)
        {
            currentTime += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, currentTime / fadeDuration);
            yield return null;
        }
        source.volume = 0f;
        source.Stop();
    }
}

[Serializable]
public class SoundSubject
{
    [Tooltip("A group of sounds of certain type. You can control this group of sounds volume," +
             "using a slider for example or by feeding directly the desire volume")]
    public string groupName;
    public Sounds[] sounds;
}