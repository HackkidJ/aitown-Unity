using NUnit.Framework.Constraints;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentManager : MonoBehaviour
{
    private static AgentManager _instance;
    // 单例模式
    public static AgentManager Instance
    {
        get
        {
            if(_instance == null)
            {
                _instance = FindObjectOfType<AgentManager>();
                if(_instance != null)
                {
                    GameObject go = new GameObject("AgentManager");
                    _instance = go.AddComponent<AgentManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    [Header("智能体预制体映射")]
    public List<AgentPrefabMapping> agentPrefabs;
    private Dictionary<string, GameObject> prefabDict = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> activeAgents = new Dictionary<string, GameObject>();

    private void Awake()
    {
        Debug.Log("AgentManager Awake 开始");
        if (_instance != null && _instance != this)
        {
            Debug.Log("销毁重复的AgentManager实例");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        InitializePrefabDictionary();
        Debug.Log("AgentManager Awake 完成");
    }

    void InitializePrefabDictionary()
    {
        Debug.Log("开始初始化预制体字典");
        prefabDict.Clear();
        if (agentPrefabs == null)
        {
            Debug.LogError("agentPrefabs 列表为空！请在Inspector中配置");
            return;
        }

        Debug.Log($"agentPrefabs 列表长度: {agentPrefabs.Count}");
        foreach (var mapping in agentPrefabs)
        {
            if (mapping == null)
            {
               // Debug.LogWarning("发现空的mapping项");
                continue;
            }
            //Debug.Log($"处理映射: skinName='{mapping.skinName}', prefab={mapping.prefab?.name}");
            if (!string.IsNullOrEmpty(mapping.skinName) && mapping.prefab != null) {
                if (prefabDict.ContainsKey(mapping.skinName))
                {
                    Debug.LogWarning($"皮肤名称 '{mapping.skinName}' 已存在，将被覆盖");
                }

                prefabDict[mapping.skinName] = mapping.prefab;
               // Debug.Log($"注册预制体：{mapping.skinName} -> {mapping.prefab.name}");
            }
            else
            {
                if (string.IsNullOrEmpty(mapping.skinName))
                    Debug.LogWarning("? 跳过：skinName为空");
                if (mapping.prefab == null)
                    Debug.LogWarning("? 跳过：prefab为null");
            }
        }
        Debug.Log($"初始化完成，字典中共有 {prefabDict.Count} 个预制体");
    }

    public void UpdateAgent(AgentData agentData)
    {
        if (activeAgents.ContainsKey(agentData.id)) {
            UpdateExistingAgent(agentData);
        }
        else
        {
            CreateNewAgent(agentData);
        }
    }

    void CreateNewAgent(AgentData agentData)
    {


        if (Instance != this)
        {
            Debug.LogWarning($"非活动 AgentManager 实例被调用，忽略");
            return;
        }

        // 检查字典是否已初始化
        if (prefabDict == null)
        {
            Debug.LogError("prefabDict 为 null！");
            return;
        }

      //  Debug.Log($"当前字典中有 {prefabDict.Count} 个预制体:");
        foreach (var key in prefabDict.Keys)
        {
        //    Debug.Log($"  - {key}");
        }

        GameObject prefab = GetPrefabBySkinName(agentData.skin_name);
        if (prefab == null)
        {
            Debug.LogError($"找不到皮肤名称为'{agentData.skin_name}'的预制体");
            return;
        }


        GameObject agent = Instantiate(prefab);
        agent.name = agentData.name;
        AgentIdentifier identifier = agent.GetComponent<AgentIdentifier>();
        if(identifier == null)
        {
            identifier = agent.AddComponent<AgentIdentifier>();

        }
        else
        {

        }

        if (agentData.position != null && agentData.position.Length >= 2)
        {
            agent.transform.position = new Vector3(
                agentData.position[0],
                agentData.position[1],
                0
          );
            Debug.Log($"设置位置: ({agentData.position[0]}, {agentData.position[1]})");

            // 由于预制件包含多个子对象
        if (agent.transform.childCount > 0)
            {
                Transform child = agent.transform.GetChild(0);
                child.localPosition = Vector3.zero;
                Debug.Log($"设置子对象 {child.name} 的局部位置为原点");
            }
        }
        identifier.agentId = agentData.id;
        identifier.agentName = agentData.name;
        identifier.skinName = agentData.skin_name;
        int serverEnergy = agentData.energy > 0 ? agentData.energy : 100;
        int serverHappiness = agentData.happiness > 0 ? agentData.happiness : 80;
        identifier.InitFromServer(serverEnergy, serverHappiness);
        SpriteRenderer childRenderer = agent.GetComponentInChildren<SpriteRenderer>();
        if (childRenderer != null)
        {
            identifier.characterRenderer = childRenderer;
        }

        activeAgents[agentData.id] = agent;

        Debug.Log($"创建智能体：{agentData.name} (皮肤:{agentData.skin_name})在位置({agentData.position[0]},{agentData.position[1]})");

        Debug.Log($"父对象位置: {agent.transform.position}");
        if (agent.transform.childCount > 0)
        {
            Transform child = agent.transform.GetChild(0);
            Debug.Log($"子对象 {child.name} 世界位置: {child.position}, 局部位置: {child.localPosition}");
        }


    }

    public GameObject GetAgentById(string agentId)
    {
        if (activeAgents.ContainsKey(agentId))
        {
            return activeAgents[agentId];
        }
        return null;
    }
    void UpdateExistingAgent(AgentData agentData)
    {
        GameObject agent = activeAgents[agentData.id];

        if(agentData.position != null && agentData.position.Length >= 2) { 
            Vector3 newPos = new Vector3(
                agentData.position[0],
                agentData.position[1],
                0);
            //暂时采取平滑移动
            agent.transform.position = newPos;
        }

        if (agent.name != agentData.name)
        {
            agent.name = agentData.name;
        }
        Debug.Log($"更新智能体: {agentData.name} 位置到 ({agentData.position[0]}, {agentData.position[1]})");
    }

    GameObject GetPrefabBySkinName(string skinName)
    {
        if (string.IsNullOrEmpty(skinName))
        {
            Debug.LogWarning("skinName为空，尝试使用默认预制体");
            skinName = "default";
        }
        if (prefabDict.ContainsKey(skinName))
        {
            Debug.Log($"找到皮肤 '{skinName}' 的预制体");
            return prefabDict[skinName];
        }

        Debug.LogWarning($"未找到皮肤'{skinName}',使用默认预制体");
        if (prefabDict.ContainsKey("default"))
        {
            return prefabDict["default"];
        }

        Debug.LogError($"也没有找到默认预制体 'default'！");
        return null;
    }

    // 清除所有智能体 重新加载场景可能会使用
    public void ClearAllAgents()
    {
        foreach (var agent in activeAgents.Values)
        {
            Destroy(agent);
        }
        activeAgents.Clear();
    }

    [System.Serializable]
    public class AgentPrefabMapping
    {
        public string skinName;
        public GameObject prefab;
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log($"AgentManager Start - 字典中有 {prefabDict.Count} 个预制体");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
