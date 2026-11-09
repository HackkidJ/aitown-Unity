using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class TimeDisplay : MonoBehaviour
{

    [Header("显示设置")]
    public bool showDate = true;
    public bool showTime = true;
    public bool showSeason = false;
    public bool showWeekday = false;

    [Header("组件引用")]
    public Text uiText;
    // 暂时使用tmp
    public TextMeshProUGUI tmpText;


    private void Awake()
    {
    }
    // Start is called before the first frame update
    private IEnumerator  Start()
    {
        yield return null;
        if(TimeManager.Instance != null)
        {
            TimeManager.Instance.OnTimeChanged += UpdateDisplay;
            UpdateDisplay(TimeManager.Instance.CurrentTime);
        }
    }

    private IEnumerator DelayedStart()
    {
        yield return null;
        if(TimeManager.Instance == null)
        {
            Debug.LogError("TimeManager 实例不存在");
            yield break;
        }

        TimeManager.Instance.OnTimeChanged += UpdateDisplay;
        UpdateDisplay(TimeManager.Instance.CurrentTime);
    }
    private void OnDestroy()
    {
        if(TimeManager.Instance != null)
        {
            TimeManager.Instance.OnTimeChanged -= UpdateDisplay;
        }
    }

    void UpdateDisplay(DateTime time)
    {
        string displayText = "";

        if (showDate)
        {
            displayText += $"{time:yyyy/MM/dd}";
            if (showWeekday) displayText += $" {time:dddd}";
            if (showTime || showSeason) displayText += " ";
        }

        if (showSeason)
        {
            displayText += GetSeasonChinese(time.Month);
            if (showTime) displayText += " ";
        }

        if (showTime)
        {
            displayText += $"{time:HH:mm}";
        }

        if (uiText != null) uiText.text = displayText;
        if (tmpText != null) tmpText.text = displayText;
    }

    string GetSeasonChinese(int month)
    {
        if (month >= 3 && month <= 5) return "春季";
        if (month >= 6 && month <= 8) return "夏季";
        if (month >= 9 && month <= 11) return "秋季";
        return "冬季";
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
