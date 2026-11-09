// MapDataManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text;
using UnityEngine.Tilemaps;
using static WallDataCollector;

[Serializable]
public class SceneObjectData
{
    public string id;
    public string type;  // building, tree, road, etc.
    public float[] position;  // [x, y, z]
    public float[] rotation;  // [x, y, z, w] quaternion
    public float[] scale;     // [x, y, z]
    public Dictionary<string, object> properties;  // 自定义属性
}

[Serializable]
public class InteractableAreaData
{
    public string id;
    public string name;
    public string type;  // shop, house, park, etc.
    public float[] center;
    public float[] size;  // width, height
    public Dictionary<string, object> properties;
}

[Serializable]
public class MapData
{
    public string map_name;
    public string version;
    public int width;
    public int height;
    public Dictionary<string, object> grid_data;
    public List<SceneObjectData> scene_objects;
    public List<InteractableAreaData> interactable_areas;
    public List<WallRect> walls;
}

[Serializable]
public class SessionData
{
    public string session_id;
    public string map_name;
    public string map_version;
    public float time_scale = 60.0f;
    public string client_version;
    public string platform;
}

[Serializable]
public class TimeUpdateData
{
    public string session_id;
    public string game_time;
}

[Serializable]
public class ApiResponse
{
    public string status;
    public string message;
    public MapData map;
    public SessionData session;
}

public class MapDataManager : MonoBehaviour
{
    public Tilemap collisionTilemap;
    private static MapDataManager _instance;
    public static MapDataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<MapDataManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("MapDataManager");
                    _instance = go.AddComponent<MapDataManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    private string currentSessionId;
    private MapData currentMapData;
    private DateTime gameTime;

    [Header("API配置")]
    public string baseUrl = "http://localhost:8000/api";
    public string clientVersion = "1.0.0";

    async void Start()
    {

        // 确保单例
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 生成会话ID
        currentSessionId = System.Guid.NewGuid().ToString();
        gameTime = DateTime.Now;
    }

    // 收集当前场景中的所有地图对象
    public MapData CollectCurrentMapData()
    {
        MapData mapData = new MapData
        {
            map_name = "MainTown",
            version = "1.0.0",
            width = 1000,
            height = 1000,
            grid_data = new Dictionary<string, object>(),
            scene_objects = new List<SceneObjectData>(),
            interactable_areas = new List<InteractableAreaData>(),
            walls = new List<WallRect>()
        };

        // 收集所有带 "MapObject" 标签的游戏对象
        GameObject[] mapObjects = GameObject.FindGameObjectsWithTag("MapObject");
        foreach (GameObject obj in mapObjects)
        {
            SceneObjectData objData = new SceneObjectData
            {
                id = obj.name + "_" + obj.GetInstanceID(),
                type = obj.tag,
                position = new float[] { Mathf.Round(obj.transform.position.x), Mathf.Round(obj.transform.position.y),Mathf.Round(obj.transform.position.z) },
                rotation = new float[] { obj.transform.rotation.x, obj.transform.rotation.y, obj.transform.rotation.z, obj.transform.rotation.w },
                scale = new float[] { obj.transform.localScale.x, obj.transform.localScale.y, obj.transform.localScale.z },
                properties = new Dictionary<string, object>()
            };

            // 添加自定义属性（如果对象有自定义脚本）
            MapObjectProperties props = obj.GetComponent<MapObjectProperties>();
            if (props != null)
            {
                objData.properties = props.GetProperties();
            }

            mapData.scene_objects.Add(objData);
        }

        // 收集可交互区域
        //GameObject[] allObjects = FindObjectsOfType<GameObject>();
        //foreach (GameObject obj in allObjects)
        //{
        //    if (obj.tag == "InteractableArea")
        //    {
        //        Debug.Log($"【找到】对象 {obj.name} 的Tag是 InteractableArea");
        //    }
        //}


        GameObject[] interactableAreas = GameObject.FindGameObjectsWithTag("InteractableArea");
      //  Debug.Log($"FindGameObjectsWithTag 找到 {interactableAreas.Length} 个对象");

        foreach (GameObject area in interactableAreas)
        {
            Collider2D collider = area.GetComponent<Collider2D>();
            if (collider != null)
            {
                InteractableAreaData areaData = new InteractableAreaData
                {
                    id = area.name + "_" + area.GetInstanceID(),
                    name = area.name,
                    type = area.tag,
                    center = new float[] { Mathf.Round(area.transform.position.x) ,Mathf.Round(area.transform.position.y)},
                    size = new float[] {Mathf.Round(collider.bounds.size.x), Mathf.Round(collider.bounds.size.y) },
                    properties = new Dictionary<string, object>()
                };

                InteractableArea areaScript = area.GetComponent<InteractableArea>();
                if (areaScript != null)
                {
                    areaData.properties["description"] = areaScript.description;
                    areaData.properties["canEnter"] = areaScript.canEnter;
                }

                mapData.interactable_areas.Add(areaData);
              //  Debug.Log($"interactable_areas 最终数量: {mapData.interactable_areas.Count}");
            }
        }
        // 添加墙壁数据
        //WallDataCollector wallCollector = new WallDataCollector();
        //wallCollector.collisionTilemap = collisionTilemap;
        //mapData.walls = wallCollector.GetWallRects();
        return mapData;
    }

    // 同步地图信息到服务器
    public IEnumerator SyncMapToServer(MapData mapData)
    {
        string jsonData = JsonUtility.ToJson(mapData);
        Debug.Log("准备发送的地图数据: " + jsonData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(baseUrl + "/map/sync/", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("地图同步成功: " + request.downloadHandler.text);

                // 解析响应
                ApiResponse response = JsonUtility.FromJson<ApiResponse>(request.downloadHandler.text);
                if (response.status == "success")
                {
                    // 同步成功后，开始游戏会话
                    StartCoroutine(StartGameSession(mapData.map_name, response.map.version));
                }
            }
            else
            {
                Debug.LogError("地图同步失败: " + request.error);
            }
        }
    }

    // 开始游戏会话
    public IEnumerator StartGameSession(string mapName, string mapVersion)
    {
        SessionData sessionData = new SessionData
        {
            session_id = currentSessionId,
            map_name = mapName,
            map_version = mapVersion,
            time_scale = 1.0f,
            client_version = clientVersion,
            platform = Application.platform.ToString()
        };

        string jsonData = JsonUtility.ToJson(sessionData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(baseUrl + "/session/start/", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("游戏会话已创建: " + request.downloadHandler.text);

                // 开始定时更新时间
              //  StartCoroutine(UpdateGameTimePeriodically());
            }
            else
            {
                Debug.LogError("创建会话失败: " + request.error);
            }
        }
    }

    // 定期更新游戏时间
    private IEnumerator UpdateGameTimePeriodically()
    {
        int retryCount = 0;
        const int maxRetries = 3;
        while (true)
        {
            yield return new WaitForSeconds(1f); // 每秒更新一次

            gameTime = gameTime.AddSeconds(1); // 游戏内时间推进

            TimeUpdateData timeData = new TimeUpdateData
            {
                session_id = currentSessionId,
                game_time = gameTime.ToString("yyyy-MM-dd HH:mm:ss")
            };

            string jsonData = JsonUtility.ToJson(timeData);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

            using (UnityWebRequest request = new UnityWebRequest(baseUrl + "/session/update-time/", "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {

                    retryCount = 0;

                }
            }
        }
    }
    // 添加响应类
    [System.Serializable]
    public class TimeUpdateResponse
    {
        public string status;
        public string game_time;
        public string session_id;
    }
    // 记录地图更新
    public void LogMapUpdate(string updateType, string objectId, Dictionary<string, object> objectData)
    {
        StartCoroutine(SendMapUpdateLog(updateType, objectId, objectData));
    }

    private IEnumerator SendMapUpdateLog(string updateType, string objectId, Dictionary<string, object> objectData)
    {
        var logData = new Dictionary<string, object>
        {
            {"session_id", currentSessionId},
            {"update_type", updateType},
            {"object_id", objectId},
            {"object_data", objectData}
        };

        string jsonData = JsonUtility.ToJson(logData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(baseUrl + "/map/update-log/", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("更新日志已记录");
            }
        }
    }

    // 在游戏开始时调用
    public void OnGameStart()
    {
        Debug.Log("游戏开始，收集并同步地图信息...");

        // 收集当前场景的地图数据
        MapData currentMap = CollectCurrentMapData();

        // 同步到服务器
        StartCoroutine(SyncMapToServer(currentMap));
    }

    public string GetSessionId()
    {
        return currentSessionId;
    }
    void DebugCheckTags()
    {
        // 查找所有游戏对象，打印它们的Tag
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        Debug.Log("=== 场景中所有对象的Tag ===");
        foreach (GameObject obj in allObjects)
        {
            if (obj.CompareTag("InteractableArea")) // 这样也能检查
            {
                Debug.Log($"找到InteractableArea: {obj.name}, Tag: {obj.tag}, 激活状态: {obj.activeInHierarchy}");
            }
            else
            {
                // 打印所有对象的Tag，看看有没有拼写错误
                Debug.Log($"对象: {obj.name}, Tag: {obj.tag}");
            }
        }
    }

}
