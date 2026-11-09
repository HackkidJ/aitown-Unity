using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    private static TimeManager _instance;
    public static TimeManager Instance
    {
        get
        {
            if(_instance == null)
            {
                _instance = FindObjectOfType<TimeManager>();
                if(_instance == null)
                {
                    GameObject go = new GameObject("TimeManager");
                    _instance = go.AddComponent<TimeManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    [Header("时间设置")]
    public float timeScale = 60f; // 现实一秒 为游戏内timescale秒
    public bool autoSync = true; // 是否自动与服务器同步

    // 当前游戏时间
    private DateTime currentTime;
    public DateTime CurrentTime => currentTime;

    // 时间事件
    public event Action<DateTime> OnTimeChanged;
    public event Action<int> OnHourChanged;
    public event Action<int> OnDayChanged;

    private float lastSyncTime;
    private float syncInterval = 60f;
    private int lastHour = -1;
    private int lastDay = -1;
    private bool isInitialized = false;
    private bool isSyncing = false;
    private void Awake()
    {
        Debug.Log($"TimeManager Awake, Instance ID: {GetInstanceID()}");
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

    }

    // Start is called before the first frame update
    void Start()
    {
        currentTime = DateTime.Now;
        lastSyncTime = Time.time;
        isInitialized = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!isInitialized) return;
        if (autoSync)
        {
            float delta = Time.deltaTime * timeScale;
            currentTime = currentTime.AddSeconds(delta);
            float timeSinceLast = Time.time - lastSyncTime;
            // 检查小时变化
            if (currentTime.Hour != lastHour)
            {
                lastHour = currentTime.Hour;
                OnHourChanged?.Invoke(lastHour);
                Debug.Log($"时间变化: {currentTime:HH:mm}");
            }

            // 检查天数变化
            if(currentTime.Day != lastDay)
            {
                lastDay = currentTime.Day;
                OnDayChanged?.Invoke(lastDay);
                Debug.Log($"日期变化: {currentTime:yyyy-MM-dd}");
            }
            OnTimeChanged?.Invoke(currentTime);

            // 定时同步时间
            if(Time.time - lastSyncTime >= syncInterval) 
            {
                Debug.Log($"触发同步: syncInterval={syncInterval}, 时间差={Time.time - lastSyncTime}");
                SyncTimeToServer();
                lastSyncTime = Time.time;
            }
        }
    }

    // 从服务器初始化时间
    public void InitFromServer(DateTime serverTime)
    {
        currentTime = serverTime;
        lastHour = currentTime.Hour;
        lastDay = currentTime.Day;
        isInitialized = true;
        Debug.Log($"?? 时间同步: {currentTime:yyyy-MM-dd HH:mm:ss}");
        OnTimeChanged?.Invoke(currentTime);
    }

    // 同步时间到服务器
    public void SyncTimeToServer()
    {
        if (isSyncing) return;
        StartCoroutine(SyncCoroutine());
    }

    System.Collections.IEnumerator SyncCoroutine()
    {
        isSyncing = true;
        var data = new TimeSyncData
        {
            session_id = MapDataManager.Instance.GetSessionId(),
            game_time = currentTime.ToString("yyyy-MM-ddTHH:mm:ss")
        };

        string jsonData = JsonUtility.ToJson(data);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        using (var request = new UnityEngine.Networking.UnityWebRequest("http://localhost:8000/api/session/update-time/", "POST"))
        {
            request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.Log($"?? 时间同步成功: {currentTime:HH:mm:ss}");
            }
            else
            {
                Debug.LogWarning($"时间同步失败: {request.error}");
            }
        }
        isSyncing = false;
    }

    // 格式化显示时间
    public string GetFormattedTime()
    {
        return currentTime.ToString("HH:mm");
    }

    public string GetFormateedDate()
    {
        return currentTime.ToString("yyyy/MM/dd dddd");
    }

    public string GetSeason()
    {
        int month = currentTime.Month;
        if (month >= 3 && month <= 5) return "春";
        if (month >= 6 && month <= 8) return "夏";
        if (month >= 9 && month <= 11) return "秋";
        return "冬";
    }

    [System.Serializable]
    public class TimeSyncData
    {
        public string session_id;
        public string game_time;
    }
}
