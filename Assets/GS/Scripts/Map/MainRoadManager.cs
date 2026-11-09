// MainRoadManager.cs
using UnityEngine;
using System.Collections.Generic;

public class MainRoadManager : MonoBehaviour
{
    public static MainRoadManager Instance;

    [Header("主干道设置")]
    public float roadY = 0f;           // 主干道的 Y 坐标
    public float roadMinX = -41.5f;      // 主干道 X 范围
    public float roadMaxX = 57.6f;

    [Header("建筑入口配置")]
    public List<BuildingEntranceConfig> buildingEntrances = new List<BuildingEntranceConfig>();

    [System.Serializable]
    public class BuildingEntranceConfig
    {
        public string buildingName;
        public Vector2 entrancePosition;  // 门口位置（在建筑边上）
        public Vector2 buildingMin;           // 建筑左下角
        public Vector2 buildingMax;           // 建筑右上角
    }

    private Dictionary<string, BuildingEntranceConfig> entranceDict = new Dictionary<string, BuildingEntranceConfig>();

    void Awake()
    {
        Instance = this;
        foreach (var config in buildingEntrances)
        {
            entranceDict[config.buildingName] = config;
        }
    }

    public Vector2? GetEntrance(string buildingName)
    {
        if (entranceDict.TryGetValue(buildingName, out var config))
        {
            return config.entrancePosition;
        }
        return null;
    }

    public Vector2? GetBuildingCenter(string buildingName)
    {
        if (entranceDict.TryGetValue(buildingName, out var config))
        {
            return (config.buildingMax+config.buildingMin)/2;
        }
        return null;
    }

    public Vector2 GetPointOnRoad(float x)
    {
        return new Vector2(x, roadY);
    }

    public bool IsOnRoad(Vector2 position)
    {
        return Mathf.Abs(position.y - roadY) < 0.5f;
    }
}