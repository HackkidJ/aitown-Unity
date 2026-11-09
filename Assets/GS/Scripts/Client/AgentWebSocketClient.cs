using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;
using System;
using Unity.VisualScripting;

// 添加基础消息类
[Serializable]
public class BaseMessage
{
    public string type;
}

[Serializable]
public class AgentData
{
    public string id;
    public string name;
    public string skin_name;
    public float[] position;
    public string action;
    public int energy;
    public int happiness;
}

[Serializable]
public class AgentListMessage
{
    public string type;
    public List<AgentData> agents;
}

[Serializable]
public class AgentUpdateMessage
{
    public string type;
    public AgentUpdateData data;
}

[Serializable]
public class AgentUpdateData
{
    public string agent_id;
    public string name;
    public float[] position;
    public string action;
    public string timestamp;
    public int energy;
    public int happiness;
    public float[] destination;
    public List<PathPoint> path;
}

[Serializable]
public class ConnectionMessage
{
    public string type;
    public string message;
}

public class AgentWebSocketClient : MonoBehaviour
{
    private WebSocket websocket;

    private static AgentWebSocketClient _instance;
    public static AgentWebSocketClient Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AgentWebSocketClient>();
            }
            return _instance;
        }
    }

    async void Start()
    {
        var timeManager = TimeManager.Instance;
        var mainRoadManager = MainRoadManager.Instance;
        //先同步地图信息
        MapDataManager.Instance.OnGameStart();
        await System.Threading.Tasks.Task.Delay(1000);
       

        websocket = new WebSocket("ws://localhost:8000/ws/agents/global/");

        websocket.OnOpen += () =>
        {
            Debug.Log("WebSocket连接已打开");
            string message = "{\"type\":\"request_agent_list\"}";
            websocket.SendText(message);
        };

        websocket.OnMessage += (byte[] data) =>
        {
            string message = System.Text.Encoding.UTF8.GetString(data);
            Debug.Log($"?? [WebSocket] 收到原始消息: {message}");

            // 检查消息类型
            if (message.Contains("agent_update"))
            {
                Debug.Log("?? 检测到 agent_update 消息！");
            }

            ParseMessage(message);
        };

        websocket.OnError += (string errorMsg) =>
        {
            Debug.LogError("WebSocket错误:" + errorMsg);
        };

        await websocket.Connect();
    }
    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
#endif
    }
    [System.Serializable]
    public class TestDialogueRequest
    {
        public string type;
        public string agent_id;
        public string other_id;
    }
    public void TestDialogue(string agentId,string otherId)
    {
         Debug.Log($"?? 发送测试对话请求: {agentId} ? {otherId}");

        var data = new Dictionary<string, object>
    {
        {"type", "test_dialogue"},
        {"agent_id", agentId},
        {"other_id", otherId}
    };

        string jsonMessage = JsonUtility.ToJson(new TestDialogueRequest
        {
            type = "test_dialogue",
            agent_id = agentId,
            other_id = otherId
        });

        SendMessageToServer(jsonMessage);
    }

    void ParseMessage(string jsonMessage)
    {
        try
        {
            // 先解析出type字段
            BaseMessage baseMsg = JsonUtility.FromJson<BaseMessage>(jsonMessage);

            if (baseMsg == null || string.IsNullOrEmpty(baseMsg.type))
            {
                Debug.LogError("无法解析消息类型: " + jsonMessage);
                return;
            }

            Debug.Log($"解析到的消息类型: {baseMsg.type}");

            switch (baseMsg.type)
            {
                case "agent_list":
                    HandleAgentList(jsonMessage);
                    break;
                case "agent_update":
                    HandleAgentUpdate(jsonMessage);
                    break;
                case "dialogue_event":
                    HandleDialogueEvent(jsonMessage);
                    break;
                case "connection_established":
                    HandleConnectionMessage(jsonMessage);
                    break;
                case "game_time":
                    HandleGameTime(jsonMessage);
                    break;
                case "arrival":
                    HandleArrival(jsonMessage);
                    break;
                default:
                    Debug.Log($"未知消息类型: {baseMsg.type}");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"解析JSON失败: {e.Message}\n原始消息: {jsonMessage}");
        }
    }
    void HandleArrival(string jsonMessage)
    {
        Debug.Log($" 收到到达通知: {jsonMessage}");
        // Python 端会处理，Unity 端不需要额外操作
    }
    void HandleDialogueEvent(string jsonMessage)
    {
        var stop_walking = false;
        Debug.Log($"?? HandleDialogueEvent 被调用, 原始消息: {jsonMessage}"); 
        try
        {
            DialogueEventMessage msg = JsonUtility.FromJson<DialogueEventMessage>(jsonMessage);

            GameObject speaker = AgentManager.Instance.GetAgentById(msg.data.speaker_id);
            GameObject listener = AgentManager.Instance.GetAgentById(msg.data.listener_id);
            stop_walking = msg.data.stop_walking;
            AgentController speakerController = speaker.GetComponent<AgentController>();
            AgentController listenerController = listener.GetComponent<AgentController>();
            if(stop_walking && speakerController && listenerController)
            {
                speakerController.StopMoving();
                listenerController.StopMoving();
            }
            if (speaker != null && listener != null)
            {
                // 触发对话 UI
                DialogueManager.Instance.ShowDialogue(
                    speaker.name,
                    listener.name,
                    msg.data.speaker_lines,
                    msg.data.listener_lines
                );
            }
        }
        catch(Exception e) { Debug.LogError($"处理对话事件出错: {e.Message}"); }
    }

    [Serializable]
    public class DialogueEventMessage
    {
        public string type;
        public DialogueData data;
    }

    [Serializable]
    public class DialogueData
    {
        public string speaker_id;
        public string speaker_name;
        public string listener_id;
        public string listener_name;
        public List<string> speaker_lines;
        public List<string> listener_lines;
        public string timestamp;
        public bool stop_walking;
    }

    void HandleGameTime(string jsonMessage)
    {
        Debug.Log($"收到原始游戏时间消息：{jsonMessage}");
        try
        {
            GameTimeMessage msg = JsonUtility.FromJson<GameTimeMessage>(jsonMessage);
            // 增加空值检查
            if (msg == null)
            {
                Debug.LogError("解析 GameTimeMessage 失败，msg 为 null");
                return;
            }
            if (msg.data == null)
            {
                Debug.LogError("解析 GameTimeMessage 失败，data 为 null");
                return;
            }
            // 构建 DateTime
            DateTime serverTime = new DateTime(
                msg.data.year,
                msg.data.month,
                msg.data.day,
                msg.data.hour,
                msg.data.minute,
                msg.data.second
            );
            Debug.Log($"?? 收到服务器时间: {serverTime:yyyy-MM-dd HH:mm:ss}");

            // 确保 TimeManager 存在
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.InitFromServer(serverTime);
            }
            else
            {
                Debug.LogError("TimeManager 实例不存在");
            }
        }
        catch(Exception e)
        {
            Debug.LogError($"处理游戏时间时出现异常：{e.Message}\n原始消息: {jsonMessage}");
        }
    }

    [Serializable]
    public class GameTimeMessage
    {
        public string type;
        public GameTimeData data;
    }

    [Serializable]
    public class GameTimeData
    {
        public int year;
        public int month;
        public int day;
        public int hour;
        public int minute;
        public int second;
        public string weekday;
        public string timestamp;
    }

    void HandleConnectionMessage(string jsonMessage)
    {
        try
        {
            ConnectionMessage msg = JsonUtility.FromJson<ConnectionMessage>(jsonMessage);
            Debug.Log("连接成功: " + msg.message);
        }
        catch
        {
            Debug.Log("连接成功: " + jsonMessage);
        }
    }
    private HashSet<string> processedAgentIds = new HashSet<string>();
    void HandleAgentList(string jsonMessage)
    {
        try
        {
            AgentListMessage msg = JsonUtility.FromJson<AgentListMessage>(jsonMessage);
            Debug.Log($"收到智能体列表，共 {msg.agents.Count} 个");

            foreach (var agentData in msg.agents)
            {
                // ? 防止重复处理同一个智能体
                if (processedAgentIds.Contains(agentData.id))
                {
                    Debug.Log($"?? 跳过重复的智能体: {agentData.name}, id={agentData.id}");
                    continue;
                }
                processedAgentIds.Add(agentData.id);

                AgentManager.Instance.UpdateAgent(agentData);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("处理AgentList时出错: " + e.Message);
        }
    }

    void HandleAgentUpdate(string jsonMessage)
    {
        try
        {
            AgentUpdateMessage msg = JsonUtility.FromJson<AgentUpdateMessage>(jsonMessage);

            // ? 增加空值检查
            if (msg == null || msg.data == null)
            {
                Debug.LogError($"msg 或 msg.data 为 null, json: {jsonMessage}");
                return;
            }

            if (AgentManager.Instance == null)
            {
                Debug.LogError("AgentManager.Instance 为 null");
                return;
            }

            GameObject agent = AgentManager.Instance.GetAgentById(msg.data.agent_id);

            if (agent == null)
            {
                Debug.LogWarning($"找不到智能体: {msg.data.agent_id}");
                return;
            }

            if (msg.data.destination == null || msg.data.destination.Length < 2)
            {
                Debug.LogWarning($"位置数据无效");
                return;
            }

            AgentController controller = agent.GetComponent<AgentController>();
            if (controller != null)
            {
                Vector3 targetPos = new Vector3(msg.data.destination[0], msg.data.destination[1], 0);
                controller.MoveToWithPathfinding(targetPos, msg.data.action);
            }
            else
            {
                Debug.LogWarning($"{agent.name} 没有 AgentController 组件");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"处理AgentUpdate时出错：{e.Message}\n堆栈: {e.StackTrace}");
        }
    }

    public async void SendMessageToServer(string jsonMessage)
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            Debug.Log($"?? 发送消息: {jsonMessage}");
            await websocket.SendText(jsonMessage);
        }
        else
        {
            Debug.LogWarning("WebSocket未连接，无法发送消息");
        }
    }

    public async void SubscribeToAgent(string agentId)
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            string jsonMessage = "{\"type\":\"subscribe_agent\", \"agent_id\":\"" + agentId + "\"}";
            Debug.Log("发送订阅消息: " + jsonMessage);
            await websocket.SendText(jsonMessage);
        }
    }

    private async void OnDestroy()
    {
        if (websocket != null)
        {
            await websocket.Close();
        }
    }
}