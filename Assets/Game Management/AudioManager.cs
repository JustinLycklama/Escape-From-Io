using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AudioManager : MonoBehaviour {
    public enum Type {
        Background1, DefenderShot, TurretShot
    }

    [SerializeField]
    private AudioSource backgroundSource;

    [SerializeField]
    private AudioSource sfx1;

    [Serializable]
    private struct ClipType {
        public Type type;
        public AudioClip audioClip;
    }

    [SerializeField]
    private ClipType[] clipTypes;

    // Start is called before the first frame update
    void Start() {

    }

    public void PlayAudio(Type type, Vector3? location = null) {
        ClipType clip = clipTypes.Where(ac => ac.type == type).SingleOrDefault();

        if (clip.audioClip == null) {
            return;
        }

        if (type == Type.Background1) {
            backgroundSource.clip = clip.audioClip;
            backgroundSource.Play();
        } else if (location != null) {
            StartCoroutine(PlayClipAtPoint(sfx1, clip.audioClip, location.Value, 2, 0.1f));
        }        
    }

    private IEnumerator PlayClipAtPoint(AudioSource source, AudioClip clip, Vector3 location, int repeat, float delay) {

        source.transform.SetPositionAndRotation(location, Quaternion.identity);
        source.clip = clip;
        for (int i = 0; i < repeat; i++) {
            source.Play();
            yield return new WaitForSeconds(delay);
        }

    }
}
