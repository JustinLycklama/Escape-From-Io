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
    public AudioSource backgroundSource = null;

    [SerializeField]
    private AudioSource sfx1 = null;

    [Serializable]
    private struct ClipType {
#pragma warning disable 0649
        public Type type;
        public AudioClip audioClip;
#pragma warning restore 0649
    }

    [SerializeField]
    private ClipType[] clipTypes = null;

    // Start is called before the first frame update
    void Start() {

    }

    public void PlayAudio(Type type, Vector3? location = null) {

        // Lets not have any extra sound effects for now
        if (type != Type.Background1) {
            return;
        }

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
