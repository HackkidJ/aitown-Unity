using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaEffect : MonoBehaviour
{
    [Header("区域设置")]
    public string areaName = "住宅";
    public string effectType = "rest"; // rest,shop,park,etc.

    [Header("效果参数")]
    public int energyRegenPerHour = 10; // 每小时回复体力
    public int happinessRegenPerHour = 5; // 每小时回复快乐值
    public int energyCostPerHour = 0; //每小时消耗体力
    public int happinessCostPerHour = 0; //每小时消耗快乐值

    [Header("同步设置")]
    public float syncInterval = 60f; // 每60秒同步一次到服务器
    public float effectInterval = 60f; // 每60秒触发一次效果

    private Coroutine effectCoroutine;
    private AgentIdentifier currentAgent;
    private float lastSyncTime;
    private int pendingEnergyChange = 0;
    private int pendingHappinessChange = 0;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 打印该对象上的所有组件
        Component[] components = other.gameObject.GetComponents<Component>();
        AgentIdentifier agent = other.GetComponent<AgentIdentifier>();
        if(agent != null)
        {
            currentAgent = agent;
            StartAreaEffect();
        }
        else
        {
            Debug.LogWarning($"❌ 对象 {other.name} 没有 AgentIdentifier 组件");
        }
    }
    

    private void OnTriggerExit2D(Collider2D other)
    {
        AgentIdentifier agent = other.GetComponent<AgentIdentifier>();  
        if(agent != null && agent == currentAgent)
        {
            StopAreaEffect();
            // 离开时也同步一次状态
            SyncStatusToServer();
            Debug.Log($"{agent.agentName}离开{areaName}，停止执行{effectType}");
        }
    }

    void StartAreaEffect()
    {
        if(effectCoroutine != null)
        {
            StopCoroutine(effectCoroutine);
        }
        effectCoroutine = StartCoroutine(ApplyEffectPeriodically());
    }

    void StopAreaEffect()
    {
        if(effectCoroutine != null)
        {
            StopCoroutine(effectCoroutine);
        }
        effectCoroutine = null; 
    }

    IEnumerator ApplyEffectPeriodically()
    {
        Debug.Log($"⏰ {areaName} 开始协程，effectInterval={effectInterval}秒");
        lastSyncTime = Time.time;
        pendingEnergyChange = 0;
        pendingHappinessChange = 0;

        while (true)
        {
            yield return new WaitForSeconds(effectInterval);
            if(currentAgent == null)
            {
                Debug.Log($"⚠️ {areaName} currentAgent 为空，退出协程");
                yield break;
            }

            int energyDelta = energyRegenPerHour;
            int happinessDelta = happinessRegenPerHour;
            Debug.Log($"📊 游戏1小时过去，恢复精力+{energyDelta}, 快乐+{happinessDelta}");

            if (energyDelta != 0)
            {
                currentAgent.ModifyEnergy(energyDelta);
                pendingEnergyChange += energyDelta;
                Debug.Log($"⚡ 精力 {currentAgent.energy}");
            }

            if (happinessDelta != 0)
            {
                currentAgent.ModifyHappiness(happinessDelta);
                pendingHappinessChange += happinessDelta;
            }

            if (Time.time - lastSyncTime >= syncInterval)
            {
                SyncStatusToServer();
                lastSyncTime = Time.time;
            }

        }
    }

    string GetStatusSymbol(int delta)
    {
        if (delta > 0) return "+";
        if (delta < 0) return "-";
        return "";
    }

    void SyncStatusToServer()
    {
        if (currentAgent == null) return;
        if (pendingEnergyChange == 0 && pendingHappinessChange == 0) return;
        StartCoroutine(SyncCoroutine());
    }

    System.Collections.IEnumerator SyncCoroutine()
    {
        var data = new StatusUpdateData
        {
            agent_id = currentAgent.agentId,
            energy = currentAgent.energy,
            happiness = currentAgent.happiness
        };
        string jsonData = JsonUtility.ToJson(data);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        using (var request = new UnityEngine.Networking.UnityWebRequest("http://localhost:8000/api/agent/status/", "POST"))
        {
            request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.Log($"📤 {currentAgent.agentName} 状态同步成功: 精力={currentAgent.energy}, 快乐={currentAgent.happiness}");
                pendingEnergyChange = 0;
                pendingHappinessChange = 0;
            }
            else
            {
                Debug.LogWarning($"同步失败: {request.error}");
            }
        }
    }

    [System.Serializable]
    public class StatusUpdateData
    {
        public string agent_id;
        public int energy;
        public int happiness;
    }    


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
