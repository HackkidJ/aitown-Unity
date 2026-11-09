// AgentController.cs - 控制智能体移动和动画
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using static UnityEngine.RuleTile.TilingRuleOutput;
using UnityEngine.Rendering;



[System.Serializable]
public class PathPoint
{
    public float x;
    public float y;
    public string direction;  // "up", "down", "left", "right"
}

[System.Serializable]
public class DecisionResponse
{
    public string goal;
    public string destination;
    public float[] destination_coordinates;
    public string reason;
    public List<PathPoint> path;  // 新增路径
}
public class AgentController : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5f;
    public float arrivalDistance = 0.3f;  // 增大到达距离

    [Header("动画设置")]
    public Animator animator;

    private Vector3 targetPosition;
    private Vector3 originalTarget;  
    private string currentAction;     
    private bool isMoving = false;
    private Vector2 lastMoveDirection = Vector2.down;
    // 根据计算方向设置动画状态
    private const string ANIM_IDLE = "Idle";
    private const string ANIM_WALK_DOWN = "Walk_Down";
    private const string ANIM_WALK_UP = "Walk_Up";
    private const string ANIM_WALK_LEFT = "Walk_Left";
    private const string ANIM_WALK_RIGHT = "Walk_Right";
    // 入口引导
    private Vector3 currentTarget;
    private Vector3 finalTarget;
    private bool isMovingToEntrance = false;

    // A*算法相关
    private List<(int steps, string direction)> currentPathSteps;
    private int currentStepIndex;
    private float currentStepRemainingSteps;
    private bool isMovingByPath = false;
    private List<Vector3> worldPathPoints;  // 存储世界坐标路径点
    private int currentPointIndex;

    // 路径移动相关
    private Queue<PathPoint> pathQueue = new Queue<PathPoint>();

    // 直接移动相关
    private Vector3 directTarget;
    private bool isDirectMoving = false;

    // 建筑范围检测
    private Dictionary<string,Rect> buildingBoundsDict = new Dictionary<string,Rect>();

    // 卡墙检测
    private Vector3 lastStuckCheckPos;
    private float stuckTimer = 0f;
    private enum MoveState
    {
        None,
        MovingToEntrance,
        MovingToRoad,
        MovingOnRoad,
        MovingFromRoadToExit,
        MovingToTarget
    }
    private MoveState currentMoveState = MoveState.None;
    private Vector3 targetOnRoad;
    private string targetBuilding;

    void Start()
    {
        InitializeBuildingBounds();
    }

    private void InitializeBuildingBounds()
    {
        if (MainRoadManager.Instance == null) return;

        foreach(var config in MainRoadManager.Instance.buildingEntrances)
        {
            Rect bounds = new Rect(
            config.buildingMin.x,
            config.buildingMin.y,
            config.buildingMax.x - config.buildingMin.x,
            config.buildingMax.y - config.buildingMin.y
                );
            buildingBoundsDict[config.buildingName] = bounds;
        }
    }

    private bool IsPointInBuilding(Vector3 point,out string buildingName)
    {
        foreach (var kvp in buildingBoundsDict)
        {
            if (kvp.Value.Contains(point))
            {
                buildingName = kvp.Key;
                return true;
            }
        }
        buildingName = "";
        return false;
    }

    private Vector3 GetRandomPointInBuilding(Rect bounds, float margin = 0.5f)
    {
        // 在建筑范围内随机生成点，边缘留出 margin 距离
        float x = Random.Range(bounds.xMin + margin, bounds.xMax - margin);
        float y = Random.Range(bounds.yMin + margin, bounds.yMax - margin);
        return new Vector3(x, y, 0);
    }
    // 测试walk动画

    private void SendArrivalNotification()
    {
        if (AgentWebSocketClient.Instance == null)
        {
            Debug.LogWarning("AgentWebSocketClient 实例不存在，无法发送到达通知");
            return;
        }

        var arrivalData = new ArrivalData
        {
            type = "arrival",
            agent_id = GetComponent<AgentIdentifier>().agentId,
            position = new float[] { transform.position.x, transform.position.y },
            destination_name = targetBuilding ?? "未知"
        };

        string jsonMessage = JsonUtility.ToJson(arrivalData);
        AgentWebSocketClient.Instance.SendMessageToServer(jsonMessage);

        Debug.Log($"?? 发送到达通知: {gameObject.name} 到达 {transform.position}");
    }

    [System.Serializable]
    public class ArrivalData
    {
        public string type;
        public string agent_id;
        public float[] position;
        public string destination_name;
    }

    void TestWalkAnimation()
    {
        animator.SetFloat("moveX", 1);
        animator.SetFloat("moveY", 0);
        animator.SetFloat("speed", 2f);
    }

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        targetPosition = transform.position;
        Debug.Log($"Animator 组件: {(animator != null ? "已找到" : "未找到")}");
    }

    void Update()
    {
        if (!isMovingByPath) return;
        float distance = Vector3.Distance(transform.position, currentTarget);

        if (distance <= arrivalDistance)
        {
            // 到达当前路点，前往下一个
            currentPointIndex++;
            MoveToNextPathPoint();
        }
        else
        {
            // 移动
            float step = moveSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, currentTarget, step);

            // 更新动画方向
            Vector3 direction = (currentTarget - transform.position).normalized;
            UpdateAnimationDirection(new Vector2(direction.x, direction.y));
           // Debug.Log($"实际移动方向: ({direction.x:F2}, {direction.y:F2}) -> {GetDirectionName(direction)}");

        }
        //旧版移动
        //if (!isMoving) return;

        ////  根据当前状态更新目标点（用于主干道连续移动）
        //UpdateCurrentTargetBasedOnState();

        //float distance = Vector3.Distance(transform.position, currentTarget);

        //if (distance <= arrivalDistance)
        //{
        //    // 到达当前目标，切换到下一阶段
        //    AdvanceToNextState();
        //}
        //else
        //{
        //    // 继续移动
        //    float step = moveSpeed * Time.deltaTime;
        //    transform.position = Vector3.MoveTowards(transform.position, currentTarget, step);

        //    // 根据当前移动状态计算方向
        //    Vector2 moveDir = GetMoveDirection();
        //    UpdateAnimationDirection(moveDir);
        //}
        //if (animator != null)
        //{
        //    float currentMoveX = animator.GetFloat("moveX");
        //    float currentMoveY = animator.GetFloat("moveY");
        //    float currentSpeed = animator.GetFloat("speed");
        //}
    }
    private Vector3 GetStepTargetPosition(string direction, int steps)
    {
        Vector3 stepDir = GetDirectionVector(direction);
        return transform.position + stepDir * steps;
    }
    public void MoveToWithPathfinding(Vector3 newPosition,string action = "walk")
    {
        Debug.Log($" MoveToWithPathfinding: {gameObject.name}, 目标: {newPosition}, 当前位置: {transform.position}");

        string buildingName;
        if (IsPointInBuilding(newPosition, out buildingName))
        {
            Rect bounds = buildingBoundsDict[buildingName];
            Vector3 randomPoint = GetRandomPointInBuilding(bounds);
            Debug.Log($"目标点在 {buildingName} 内，从 {newPosition} 随机偏移到 {randomPoint}");
            newPosition = randomPoint;
        }

        if (Vector3.Distance(newPosition, transform.position) < 0.1f)
        {
            Debug.Log("起点终点一致 不移动");
            return;
        }

        // 获取格式化路径
        List<Vector3> pathSteps = AStarPathfinding.Instance.FindPath(transform.position, newPosition);

        //if (pathSteps == null || pathSteps.Count == 0)
        //{
        //    Debug.LogWarning($"无法找到从 {transform.position} 到 {newPosition} 的路径，使用直接移动");
        //    // 降级到原移动方式
        //    MoveTo(newPosition, action);
        //    return;
        //}

        if (pathSteps==null ||pathSteps.Count == 0)
        {
            Debug.LogWarning("无法找到路径，使用直接移动");
            MoveTo(newPosition, action);
            return;
        }
        // 保存路径步骤
        worldPathPoints = pathSteps;
        currentStepIndex = 0;
        isMovingByPath = true;

        // 开始执行第一步
        MoveToNextPathPoint();

        Debug.Log($" 路径规划成功，共 {pathSteps.Count} 步");
    }
    private void MoveToNextPathPoint()
    {
        if (currentPointIndex >= worldPathPoints.Count)
        {
            // 到达终点
            isMovingByPath = false;
            isMoving = false;
            animator.SetFloat("speed", 0);
            animator.Play(ANIM_IDLE);
            Debug.Log($" {gameObject.name} 到达目标");
            SendArrivalNotification();
            return;
        }

        currentTarget = worldPathPoints[currentPointIndex];

        // 计算方向并设置动画
        //Vector3 direction = (currentTarget - transform.position).normalized;
        //string dirName = GetDirectionName(direction);

        //SetAnimationDirection(dirName);
        animator.SetFloat("speed", moveSpeed);
    }
    private string GetDirectionName(Vector3 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            return dir.x > 0 ? "right" : "left";
        }
        else
        {
            return dir.y > 0 ? "up" : "down";
        }
    }
    /// <summary>
    /// 执行下一步路径
    /// </summary>
    private void ExecuteNextStep()
    {
        if (currentStepIndex >= currentPathSteps.Count)
        {
            // 路径执行完毕
            isMovingByPath = false;
            isMoving = false;
            animator.SetFloat("speed", 0);
            Debug.Log($" {gameObject.name} 到达目标");
            SendArrivalNotification();
            return;
        }

        var step = currentPathSteps[currentStepIndex];
        currentStepRemainingSteps = step.steps;
        Debug.Log($"执行步骤 {currentStepIndex + 1}: 方向={step.direction}, 步数={step.steps}");
        Debug.Log($"设置动画方向: {step.direction}");
        SetAnimationDirection(step.direction);
        animator.SetFloat("speed", moveSpeed);

        Debug.Log($"动画参数: moveX={animator.GetFloat("moveX")}, moveY={animator.GetFloat("moveY")}, speed={animator.GetFloat("speed")}");
    }
        /// <summary>
        /// 将方向字符串转换为向量
        /// </summary>
        private Vector3 GetDirectionVector(string direction)
        {
            switch (direction)
            {
                case "right": return Vector3.right;
                case "left": return Vector3.left;
                case "up": return Vector3.up;
                case "down": return Vector3.down;
                default: return Vector3.zero;
            }
        }


    /// <summary>
    /// 获取当前移动状态对应的方向
    /// </summary>
    Vector2 GetMoveDirection()
    {
        switch (currentMoveState)
        {
            case MoveState.MovingToEntrance:
            case MoveState.MovingFromRoadToExit:
                // 垂直移动（上下）
                return new Vector2(0, Mathf.Sign(currentTarget.y - transform.position.y));

            case MoveState.MovingToRoad:
            case MoveState.MovingOnRoad:
                // 水平移动（左右）
                return new Vector2(Mathf.Sign(currentTarget.x - transform.position.x), 0);

            case MoveState.MovingToTarget:
                // 最终移动
                Vector3 dir = (currentTarget - transform.position).normalized;
                return new Vector2(dir.x, dir.y);

            default:
                return lastMoveDirection;
        }
    }
    void SetAnimationDirectionImmediate(float moveX, float moveY)
    {
        // 强制设置，不清除另一个方向
        animator.SetFloat("moveX", moveX);
        animator.SetFloat("moveY", moveY);
        animator.SetFloat("speed", moveSpeed);

        Debug.Log($"?? 强制设置动画: moveX={moveX}, moveY={moveY}");
    }
    /// <summary>
    /// 切换到下一个移动状态
    /// </summary>
    void AdvanceToNextState()
    {
        switch (currentMoveState)
        {
            case MoveState.MovingToEntrance:
                // 到达门口，下一步上主干道
                float roadX = transform.position.x;
                Vector2 roadPoint = MainRoadManager.Instance.GetPointOnRoad(roadX);
                currentTarget = new Vector3(roadPoint.x, roadPoint.y, 0);
                currentMoveState = MoveState.MovingToRoad;
                UpdateAnimationDirection(new Vector2(0, Mathf.Sign(currentTarget.y - transform.position.y)));
                Debug.Log($" 到达门口，上主干道 {currentTarget}");
                break;

            case MoveState.MovingToRoad:
                // 到达主干道，开始水平移动
                if (targetBuilding != null)
                {
                    Vector2? targetEntrance = MainRoadManager.Instance.GetEntrance(targetBuilding);
                    if (targetEntrance.HasValue)
                    {
                        currentTarget = new Vector3(targetEntrance.Value.x, MainRoadManager.Instance.roadY, 0);
                        currentMoveState = MoveState.MovingOnRoad;
                        Debug.Log($"??? 主干道上水平移动，目标 X: {targetEntrance.Value.x}");
                    }
                }
                UpdateAnimationDirection(new Vector2(Mathf.Sign(currentTarget.x - transform.position.x), 0));
                SetAnimationDirectionImmediate(0, Mathf.Sign(currentTarget.y - transform.position.y));
                break;

            case MoveState.MovingOnRoad:
                // 到达目标建筑对应的主干道位置，下主干道
                Vector2? entrance = MainRoadManager.Instance.GetEntrance(targetBuilding);
                if (entrance.HasValue)
                {
                    currentTarget = new Vector3(entrance.Value.x, entrance.Value.y, 0);
                    currentMoveState = MoveState.MovingFromRoadToExit;
                    Debug.Log($"?? 下主干道，走向门口 {currentTarget}");
                }
                UpdateAnimationDirection(new Vector2(Mathf.Sign(currentTarget.x - transform.position.x), 0));
                break;

            case MoveState.MovingFromRoadToExit:
                // 到达目标建筑门口，进入建筑
                Vector2? center = MainRoadManager.Instance.GetBuildingCenter(targetBuilding);
                if (center.HasValue)
                {
                    currentTarget = new Vector3(center.Value.x, center.Value.y, 0);
                    currentMoveState = MoveState.MovingToTarget;
                    Debug.Log($"?? 进入建筑 {currentTarget}");
                }
                UpdateAnimationDirection(new Vector2(0, Mathf.Sign(currentTarget.y - transform.position.y)));
                SetAnimationDirectionImmediate(0, Mathf.Sign(currentTarget.y - transform.position.y));
                break;

            case MoveState.MovingToTarget:
                // 到达最终目标
                isMoving = false;
                currentMoveState = MoveState.None;
                animator.SetFloat("speed", 0);
                Debug.Log($"? {gameObject.name} 到达目标");
                SendArrivalNotification();
                break;
        }
    }
    void UpdateCurrentTargetBasedOnState()
    {
        if (currentMoveState == MoveState.MovingOnRoad && targetBuilding != null)
        {
            Vector2? targetEntrance = MainRoadManager.Instance.GetEntrance(targetBuilding);
            if (targetEntrance.HasValue)
            {
                float targetX = targetEntrance.Value.x;
                float currentX = transform.position.x;

                // 如果还没到达目标 X 坐标，动态更新目标点
                if (Mathf.Abs(currentX - targetX) > arrivalDistance)
                {
                    float stepX = Mathf.Sign(targetX - currentX) * moveSpeed * Time.deltaTime;
                    float newX = currentX + stepX;

                    // 确保不超过目标 X
                    if (Mathf.Abs(newX - targetX) < Mathf.Abs(stepX))
                    {
                        newX = targetX;
                    }

                    currentTarget = new Vector3(newX, MainRoadManager.Instance.roadY, 0);
                }
            }
        }
    }
    string GetBuildingAtPosition(Vector3 pos)
    {
        foreach (var config in MainRoadManager.Instance.buildingEntrances)
        {
            if (pos.x >= config.buildingMin.x && pos.x <= config.buildingMax.x &&
                pos.y >= config.buildingMin.y && pos.y <= config.buildingMax.y)
            {
                return config.buildingName;
            }
        }
        return null;
    }

    void UpdateAnimationDirection(Vector2 moveDir)
    {
        if (moveDir.magnitude < 0.05f)
        {
            PlayIdleAnimation();
            return;
        }

        // ? 斜向运动时优先水平方向
        string targetAnim = ANIM_IDLE;
        // 优先判断水平方向（只要 X 方向有移动，就优先水平）
        if (Mathf.Abs(moveDir.x) > 0.1f)
        {
            // 水平方向优先
            if (moveDir.x > 0)
            {
                targetAnim = ANIM_WALK_RIGHT;
            }
            else
            {
                targetAnim = ANIM_WALK_LEFT;
            }
        }
        else if (Mathf.Abs(moveDir.y) > 0.1f)
        {
            // 垂直方向
            if (moveDir.y > 0)
            {
                targetAnim = ANIM_WALK_UP;
            }
            else
            {
                targetAnim = ANIM_WALK_DOWN;
            }
        }

       // Debug.Log($"?? 优先水平: moveDir=({moveDir.x:F2}, {moveDir.y:F2}) → 动画: {targetAnim}");

        // 直接播放动画
        animator.Play(targetAnim);

        // 更新参数（备用）
        Vector2 simplifiedDir;
        if (targetAnim == ANIM_WALK_RIGHT)
            simplifiedDir = new Vector2(1, 0);
        else if (targetAnim == ANIM_WALK_LEFT)
            simplifiedDir = new Vector2(-1, 0);
        else if (targetAnim == ANIM_WALK_UP)
            simplifiedDir = new Vector2(0, 1);
        else
            simplifiedDir = new Vector2(0, -1);

        lastMoveDirection = simplifiedDir;
        animator.SetFloat("moveX", simplifiedDir.x);
        animator.SetFloat("moveY", simplifiedDir.y);
        animator.SetFloat("speed", moveSpeed);
    }
    void PlayIdleAnimation()
    {
        animator.Play(ANIM_IDLE);
        animator.SetFloat("speed", 0);
    }
    void SetAnimatorParams(float moveX, float moveY, float speed)
    {
        lastMoveDirection = new Vector2(moveX, moveY);
        animator.SetFloat("moveX", moveX);
        animator.SetFloat("moveY", moveY);
        animator.SetFloat("speed", speed);
    }
    void SetAnimationDirection(string direction)
    {
        switch (direction)
        {
            case "right":
                animator.SetFloat("moveX", 1);
                animator.SetFloat("moveY", 0);
                break;
            case "left":
                animator.SetFloat("moveX", -1);
                animator.SetFloat("moveY", 0);
                break;
            case "up":
                animator.SetFloat("moveX", 0);
                animator.SetFloat("moveY", 1);
                break;
            case "down":
                animator.SetFloat("moveX", 0);
                animator.SetFloat("moveY", -1);
                break;
        }
    }

    /// <summary>
    /// 直接移动到目标点（无路径）
    /// </summary>        Debug.Log($"?? MoveTo 被调用: {gameObject.name}, 目标: {newPosition}, 当前位置: {transform.position}");

public void MoveTo(Vector3 newPosition, string action = "walk")
{
        Debug.Log($"?? MoveTo: {gameObject.name}, 目标: {newPosition}, 当前位置: {transform.position}");

        if (newPosition == transform.position)
        {
            Debug.Log("起点终点一致 不移动");
            return;
        }
        Debug.Log($"?? MoveTo: {gameObject.name}, 目标: {newPosition}");
        if (BuildingEntranceManager.Instance == null)
        {
            Debug.LogError("BuildingEntranceManager.Instance 为 null！");
            // 降级：直接移动
            currentTarget = newPosition;
            isMoving = true;
            finalTarget = newPosition;
            Vector3 direction = (currentTarget - transform.position).normalized;
            UpdateAnimationDirection(new Vector2(direction.x, direction.y));
            animator.SetFloat("speed", moveSpeed);
            return;
        }
        finalTarget = newPosition;

    // 检查当前位置和目标位置的建筑归属
    string currentBuilding = GetBuildingAtPosition(transform.position);
    string targetBuildingName = GetBuildingAtPosition(newPosition);

    if (currentBuilding != null && targetBuildingName != null && currentBuilding != targetBuildingName)
    {
        // 情况1：从一个建筑到另一个建筑
        StartBuildingToBuildingMove(currentBuilding, targetBuildingName);
    }
    else if (currentBuilding != null && targetBuildingName == null)
    {
        // 情况2：从建筑到外部
        StartBuildingToOutsideMove(currentBuilding);
    }
    else if (currentBuilding == null && targetBuildingName != null)
    {
        // 情况3：从外部到建筑
        StartOutsideToBuildingMove(targetBuildingName);
    }
    else
    {
        // 情况4：外部到外部，直接移动
        StartDirectMove(newPosition);
    }
        isMoving = true;
    }
    void StartBuildingToBuildingMove(string fromBuilding, string toBuilding)
    {
        targetBuilding = toBuilding;

        // 获取当前建筑的门口位置
        Vector2? entrance = MainRoadManager.Instance.GetEntrance(fromBuilding);
        if (!entrance.HasValue) return;

        currentTarget = new Vector3(entrance.Value.x, entrance.Value.y, 0);
        currentMoveState = MoveState.MovingToEntrance;

        Debug.Log($"?? {gameObject.name}: {fromBuilding} → 门口");
    }

    void StartBuildingToOutsideMove(string fromBuilding)
    {
        Vector2? entrance = MainRoadManager.Instance.GetEntrance(fromBuilding);
        if (!entrance.HasValue) return;

        currentTarget = new Vector3(entrance.Value.x, entrance.Value.y, 0);
        currentMoveState = MoveState.MovingToEntrance;

        Debug.Log($"?? {gameObject.name}: {fromBuilding} → 门口");
    }

    void StartOutsideToBuildingMove(string toBuilding)
    {
        targetBuilding = toBuilding;
        Vector2? entrance = MainRoadManager.Instance.GetEntrance(toBuilding);
        if (!entrance.HasValue) return;

        // 先移动到主干道上对应 X 的位置
        float targetX = entrance.Value.x;
        Vector2 roadPoint = MainRoadManager.Instance.GetPointOnRoad(targetX);
        targetOnRoad = new Vector3(roadPoint.x, roadPoint.y, 0);

        currentTarget = targetOnRoad;
        currentMoveState = MoveState.MovingToRoad;

        Debug.Log($"??? {gameObject.name}: 移动到主干道 {targetOnRoad}");
    }

    void StartDirectMove(Vector3 target)
    {
        currentTarget = target;
        currentMoveState = MoveState.MovingToTarget;
        isMoving = true;
        Debug.Log($"?? {gameObject.name}: 直接移动");
    }

    public void StopMoving()
    {
        isMoving = false;
        isDirectMoving = false;
        pathQueue.Clear();
        animator.SetFloat("speed", 0);
    }
    // 旧版移动实现
    // 平滑移动
    //if (isMoving)
    //{
    //    float distance = Vector3.Distance(transform.position, targetPosition);

    //    if (distance <= arrivalDistance)
    //    {
    //        isMoving = false;
    //        transform.position = targetPosition;
    //        animator.SetFloat("speed", 0);
    //        directionLocked = false;
    //    }
    //    else
    //    {
    //        float step = moveSpeed * Time.deltaTime;
    //        transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);
    //        // 使用锁定方向
    //        if (directionLocked)
    //        {
    //            animator.SetFloat("moveX", lockedMoveDirection.x);
    //            animator.SetFloat("moveY", lockedMoveDirection.y);
    //            animator.SetFloat("speed", 2f);
    //        }
    //    }
    //}
    //else
    //{
    //    animator.SetFloat("speed", 0);
    //} 



    Vector2 SimplifyDirection(Vector2 direction)
    {
        float absX = Mathf.Abs(direction.x);
        float absY = Mathf.Abs(direction.y);

        if(absX > absY + 0.1f)
        {
            return new Vector2 (Mathf.Sign(direction.x), 0);
        }
        else if (absY > absX + 0.1f)
        {
            return new Vector2(0,Mathf.Sign(direction.y));
        }
        else
        {
            // 斜向优先方向  水平
            return new Vector2(Mathf.Sign(direction.x), 0);
        }
    }


    //旧版实现
    //targetPosition = newPosition;
    //isMoving = true;
    //directionLocked = false;

    //// 预计算方向
    //Vector3 direction = (targetPosition-transform.position).normalized;
    //Vector2 moveDir = new Vector2 (direction.x, direction.z);

    //lockedMoveDirection = SimplifyDirection(moveDir);
    //directionLocked = true;

    //animator.SetFloat("moveX", lockedMoveDirection.x);
    //animator.SetFloat("moveY", lockedMoveDirection.y);
    //animator.SetFloat("speed", 2f);

    //Debug.Log($"MoveTo: 目标 {targetPosition}, 锁定方向 ({lockedMoveDirection.x}, {lockedMoveDirection.y})");

    /// <summary>
    /// 执行动作（对话、交互等）
    /// </summary>
    public void PerformAction(string action, string dialogue = null)
    {
        if (action == "talk")
        {
            StartCoroutine(ShowDialogueAndTalk(dialogue));
        }

    }

    IEnumerator ShowDialogueAndTalk( string dialogue)
    {
        animator.SetBool("isTalking", true);

        ShowDialogueBubble(dialogue);

        yield return new WaitForSeconds(2f);

        animator.SetBool("isTalking", false);
    }

    /// <summary>
    /// 显示对话气泡
    /// </summary>
    void ShowDialogueBubble(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        // 创建临时文本
        GameObject bubble = new GameObject("DialogueBubble");
        bubble.transform.SetParent(transform);
        bubble.transform.localPosition = new Vector3(0, 1.5f, 0);

        TextMesh textMesh = bubble.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.fontSize = 24;
        textMesh.color = Color.black;

        // 5秒后销毁
        Destroy(bubble, 5f);


    }

    /// <summary>
    /// 更新状态UI
    /// </summary>
    public void UpdateStatus(int energy, int happiness)
    {
        // 可以更新头顶的状态条
        // 这里简化处理
        Debug.Log($" {gameObject.name} - 精力:{energy}, 快乐:{happiness}");
    }
}