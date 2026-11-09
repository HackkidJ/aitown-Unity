using UnityEngine;
using System.Collections.Generic;

public class BuildingEntranceManager : MonoBehaviour
{
    public static BuildingEntranceManager Instance;
    [Header("是否弃用")]
    public bool isDeprecated;

    [Header("自动扫描设置")]
    public string interactableAreaTag = "InteractableArea";
    public bool autoScanOnStart = true;

    [Header("手动配置入口（如果自动扫描失败，使用这个）")]
    public List<ManualEntrance> manualEntrances = new List<ManualEntrance>();

    private Dictionary<string, BuildingEntrance> entranceDict = new Dictionary<string, BuildingEntrance>();

    [System.Serializable]
    public class ManualEntrance
    {
        public string buildingName;
        public Vector2 entrancePosition;
        public Vector2 buildingMin;  // 建筑左下角
        public Vector2 buildingMax;  // 建筑右上角
    }

    public class BuildingEntrance
    {
        public string buildingName;
        public Vector2 entrancePosition;
        public Rect buildingBounds;
    }

    void Awake()
    {
        Instance = this;

        // 优先使用手动配置
        if (manualEntrances.Count > 0)
        {
            BuildFromManualConfig();
        }
        else if (autoScanOnStart)
        {
            ScanAndBuild();
        }

        Debug.Log($"?? BuildingEntranceManager 初始化完成，共 {entranceDict.Count} 个建筑");
    }

    void BuildFromManualConfig()
    {
        entranceDict.Clear();

        foreach (var config in manualEntrances)
        {
            Rect bounds = new Rect(
                config.buildingMin.x,
                config.buildingMin.y,
                config.buildingMax.x - config.buildingMin.x,
                config.buildingMax.y - config.buildingMin.y
            );

            entranceDict[config.buildingName] = new BuildingEntrance
            {
                buildingName = config.buildingName,
                entrancePosition = config.entrancePosition,
                buildingBounds = bounds
            };

          //  Debug.Log($"? 手动添加建筑: {config.buildingName}, 入口: {config.entrancePosition}, 范围: {bounds}");
        }
    }

    [ContextMenu("扫描并构建建筑数据")]
    public void ScanAndBuild()
    {
        entranceDict.Clear();

        GameObject[] areas = GameObject.FindGameObjectsWithTag(interactableAreaTag);
        Debug.Log($"找到 {areas.Length} 个 InteractableArea");

        foreach (GameObject area in areas)
        {
            string name = area.name.Replace("Zone", "");
            Collider2D collider = area.GetComponent<Collider2D>();

            if (collider == null)
            {
                Debug.LogWarning($"区域 {area.name} 没有 Collider2D");
                continue;
            }

            Bounds bounds = collider.bounds;
            Rect buildingRect = new Rect(bounds.min.x, bounds.min.y, bounds.size.x, bounds.size.y);

            // 默认入口在建筑底边中点
            Vector2 defaultEntrance = new Vector2(bounds.center.x, bounds.min.y - 0.5f);

            entranceDict[name] = new BuildingEntrance
            {
                buildingName = name,
                entrancePosition = defaultEntrance,
                buildingBounds = buildingRect
            };

            Debug.Log($"? 自动添加建筑: {name}, 范围: {buildingRect}, 默认入口: {defaultEntrance}");
        }
    }

    public bool IsInsideBuilding(Vector2 position, out string buildingName)
    {
        foreach (var kvp in entranceDict)
        {
            if (kvp.Value.buildingBounds.Contains(position))
            {
                buildingName = kvp.Key;
                return true;
            }
        }
        buildingName = "";
        return false;
    }

    public Vector2? GetEntrancePosition(string buildingName)
    {
        if (entranceDict.TryGetValue(buildingName, out var entrance))
        {
            return entrance.entrancePosition;
        }
        return null;
    }
}