using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[Serializable]
public class SubtitleLine {
    public float startTime;
    public float endTime;
    [TextArea] public string text;
}

public class SubtitleManager : MonoBehaviour
{
    [Header("References")]
    public AudioSource audioSource;
    public TextMeshProUGUI subtitleTMP;

    [Header("Subtitles Data")]
    public List<SubtitleLine> lines = new List<SubtitleLine>();

    [Header("Typewriter Effect")]
    public bool useTypewriter = true;
    [Range(1f, 200f)] public float charsPerSecond = 40f;

    public event Action OnSubtitlesCompleted;

    int _currentIndex = -1;
    float _lineStartAudioTime;
    string _currentText = "";
    int _shownChars = 0;
    bool _completed = false;

    // סף בטיחות: אל תכריז סיום אם lastEnd קטן מזה
    const float k_MinLastEnd = 0.05f;

    void Update()
    {
        if (!audioSource || !subtitleTMP || lines.Count == 0) return;

        float t = audioSource.time;
        int idx = GetActiveLineIndex(t);

        if (idx != _currentIndex)
        {
            _currentIndex = idx;
            if (_currentIndex >= 0)
            {
                _currentText = lines[_currentIndex].text;
                _shownChars = 0;
                _lineStartAudioTime = t;
                subtitleTMP.text = useTypewriter ? "" : _currentText;
                subtitleTMP.gameObject.SetActive(true);
            }
            else
            {
                subtitleTMP.text = "";
                subtitleTMP.gameObject.SetActive(false);
            }
        }

        if (_currentIndex >= 0 && useTypewriter)
        {
            float elapsed = t - _lineStartAudioTime;
            int targetChars = Mathf.Clamp(
                Mathf.FloorToInt(elapsed * charsPerSecond),
                0,
                _currentText.Length
            );
            if (targetChars != _shownChars)
            {
                _shownChars = targetChars;
                subtitleTMP.text = _currentText.Substring(0, _shownChars);
            }
        }

        // סיום כתוביות – רק אם יש lastEnd סביר
        if (!_completed && lines.Count > 0)
        {
            float lastEnd = lines[lines.Count - 1].endTime;
            if (lastEnd > k_MinLastEnd && t >= lastEnd)
            {
                _completed = true;
                subtitleTMP.text = "";
                subtitleTMP.gameObject.SetActive(false);
                OnSubtitlesCompleted?.Invoke();
            }
        }
    }

    int GetActiveLineIndex(float audioTime)
    {
        for (int i = 0; i < lines.Count; i++)
            if (audioTime >= lines[i].startTime && audioTime <= lines[i].endTime)
                return i;
        return -1;
    }

    public void StartForAudio()
    {
        _currentIndex = -1;
        _shownChars = 0;
        _completed = false;
        if (subtitleTMP)
        {
            subtitleTMP.text = "";
            subtitleTMP.gameObject.SetActive(true);
        }
        this.enabled = true;
    }

    public void StopSubtitles()
    {
        this.enabled = false;
        if (subtitleTMP)
        {
            subtitleTMP.text = "";
            subtitleTMP.gameObject.SetActive(false);
        }
    }
}
