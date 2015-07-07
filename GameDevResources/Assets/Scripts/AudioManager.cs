using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    #region Public Member Variables
    public AudioSource MusicSource;
    public AudioSource SFXSource;
    #endregion

    #region Private Member Variables
    private static AudioManager _Instance = null;
    #endregion

    #region Public Properties
    public static AudioManager Instance
    {
        get { return _Instance; }
    }
    #endregion

    #region Unity Methods
    private void Awake()
    {
        _Instance = this;
        GameObject.DontDestroyOnLoad(this.gameObject);
    }
    #endregion

    #region Public Methods
    public void PlayMusic(AudioClip clip, bool looping = true, float volume = 1.0f)
    {
        MusicSource.clip = clip;
        MusicSource.loop = looping;
        MusicSource.volume = volume;
        MusicSource.Play();
    }

    public void PlayOneShot(AudioClip clip, float volume)
    {
        SFXSource.PlayOneShot(clip, volume);
    }
    #endregion
}
