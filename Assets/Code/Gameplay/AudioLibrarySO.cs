
using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "AudioLibrary",
    menuName = "Game/Audio/Audio Library")]
public sealed class AudioLibrarySO : ScriptableObject
{
    [Serializable]
    public class Sound
    {
        public string id;
        public AudioClip clip;

        [Range(0f, 1f)]
        public float volume = 1f;
    }

    [SerializeField] private Sound[] sounds;

    public Sound GetSound(string id)
    {
        if (sounds == null || string.IsNullOrEmpty(id))
            return null;

        foreach (Sound sound in sounds)
        {
            if (sound != null && sound.id == id)
                return sound;
        }

        return null;
    }
}
