using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentIdentifier : MonoBehaviour
{
    public string agentId;
    public string agentName;
    public string skinName;

    private int _energy;
    private int _happiness;

    public int energy => _energy;
    public int happiness => _happiness;

    // 状态变化事件
    public System.Action<int, int> OnStatusChanged;
    // 其他智能体相关组件
    public Animator animator;
    public SpriteRenderer characterRenderer;

    [Header("对话气泡")]
    public DialogueBubble dialogueBubble;

    private void Awake()
    {
        // 尝试获取 DialogueBubble 组件
        if (dialogueBubble == null)
            dialogueBubble = GetComponent<DialogueBubble>();

        // 如果没有，则添加
        if (dialogueBubble == null)
            dialogueBubble = gameObject.AddComponent<DialogueBubble>();
    }

    // 显示对话的方法
    public void Say(string text, float duration = 3f)
    {
        if (dialogueBubble != null)
        {
            dialogueBubble.ShowDialogue(text, duration);
        }
    }
    public void InitFromServer(int serverEnergy,int serverHappiness)
    {
        _energy = Mathf.Clamp(serverEnergy, 0, 100);
        _happiness = Mathf.Clamp(serverHappiness, 0, 100);
        OnStatusChanged?.Invoke(_energy, _happiness);
    }
    public void UpdateStatus(int newEnergy,int newHappiness)
    {
        _energy = Mathf.Clamp(newEnergy, 0, 100);
        _happiness = Mathf.Clamp(newHappiness, 0, 100);
        OnStatusChanged?.Invoke(_energy,_happiness);
    }
    public void ModifyEnergy(int delta)
    {
        int oldEnergy = _energy;
        _energy = Mathf.Clamp(_energy + delta, 0, 100);
        OnStatusChanged?.Invoke(_energy, _happiness);
    }

    public void ModifyHappiness(int delta)
    {
        int oldHappiness = _happiness;
        _happiness = Mathf.Clamp(_happiness + delta, 0, 100);
        OnStatusChanged?.Invoke(_energy, _happiness);
        Debug.Log($"?? {agentName} 快乐: {oldHappiness} -> {_happiness} (Δ{delta})");
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
