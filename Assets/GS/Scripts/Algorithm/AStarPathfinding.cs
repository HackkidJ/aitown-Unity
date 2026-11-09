using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using Unity.VisualScripting;

public class Node
{
    // 基础属性
    public bool walkable;
    public Vector3 worldPos;

    // 网格坐标
    public int gridX;
    public int gridY;

    // 算法代价
    public int gCost; // 起点到当前节点
    public int hCost; // 当前节点到终点
    public int FCost { get { return gCost + hCost; } } //总

    // 父节点 回溯
    public Node parent;


    public Node(bool _walkable, Vector3 _worldPos, int _gridX, int _gridY)
    {
        walkable = _walkable;
        worldPos = _worldPos;
        gridX = _gridX;
        gridY = _gridY;
    }
}
public class AStarPathfinding : MonoBehaviour
{
    [Header("引用")]
    public Tilemap collisionTilemap;
    [Header("网格设置")]
    public float nodeSize = 1f;

    private Node[,] grid;
    private int gridWidth;
    private int gridHeight;

    private int gridOriginX;
    private int gridOriginY;

    private List<Vector3> testPath;

    [Header("单元调试选项")]
    public bool showDebugGrid = true;
    public bool showSearchProcess = true;
    private Vector3 debugStartPos;
    private Vector3 debugTargetPos;
    private List<Node> debugOpenSet;
    private HashSet<Node> debugClosedSet;
    private static AStarPathfinding _instance;
    public static AStarPathfinding Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AStarPathfinding>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("Pathfinding");
                    _instance = go.AddComponent<AStarPathfinding>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        CreateGrid();
        //Vector3 startTestPos = new Vector3(-37,-14, 0);
        //Vector3 endPo = new Vector3(36,-20, 0);
        //testPath = FindPath(startTestPos, endPo);
        //PrintPath(testPath);
        //var temp =  GetFormattedPath(startTestPos, endPo);
        //foreach(var step in temp)
        //{
        //    Debug.Log($"{step.steps},{step.direction}");
        //}
    }

    private void OnDrawGizmos()
    {
        if(!showDebugGrid || grid == null) return;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Node node = grid[x, y];
                if (node == null) continue;

                // 绿色=可行走，红色=障碍物
                Gizmos.color = node.walkable ? Color.green : Color.red;

                // 绘制一个立方体线框
                Gizmos.DrawWireCube(node.worldPos, Vector3.one * nodeSize * 0.9f);
            }
        }

        // 绘制未检查列表
        if (showSearchProcess && debugOpenSet != null)
        {
            Gizmos.color = new Color(0, 0, 1, 0.5f);  // 半透明蓝色
            foreach (Node node in debugOpenSet)
            {
                if (node != null)
                    Gizmos.DrawCube(node.worldPos, Vector3.one * nodeSize * 0.8f);
            }
        }

        // 绘制已检查列表
        {
            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);  // 半透明灰色
            foreach (Node node in debugClosedSet)
            {
                if (node != null)
                    Gizmos.DrawCube(node.worldPos, Vector3.one * nodeSize * 0.8f);
            }
        }
        // 绘制起点
        Node startNode = NodeFromWorldPoint(debugStartPos);
        if (startNode != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawCube(startNode.worldPos, Vector3.one * nodeSize * 0.9f);
        }
        // 绘制终点
        Node targetNode = NodeFromWorldPoint(debugTargetPos);
        if (targetNode != null)
        {
            Gizmos.color = Color.black;
            Gizmos.DrawCube(targetNode.worldPos, Vector3.one * nodeSize * 0.9f);
        }
        // 最终路径用亮黄色粗线
        if (testPath != null && testPath.Count > 0)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < testPath.Count - 1; i++)
            {
                Gizmos.DrawLine(testPath[i], testPath[i + 1]);
            }

            // 路径点用橙色小球
            Gizmos.color = new Color(1, 0.5f, 0);
            foreach (Vector3 point in testPath)
            {
                Gizmos.DrawWireSphere(point, 0.15f);
            }
        }
    }

    public void CreateGrid()
    {
        BoundsInt bounds = collisionTilemap.cellBounds;
        // 手动计算实际有瓦片的最小/最大范围
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;
        bool hasAnyTile = false;


        // 遍历所有瓦片，找到实际范围
        for (int x = bounds.min.x; x < bounds.max.x; x++)
        {
            for (int y = bounds.min.y; y < bounds.max.y; y++)
            {
                Vector3Int cellPos = new Vector3Int(x, y, 0);
                if (collisionTilemap.HasTile(cellPos))
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }


        gridOriginX = minX;
        gridOriginY = minY;

        // 原代码
        gridWidth = maxX - minX + 1;
        gridHeight = maxY - minY + 1;
        grid = new Node[gridWidth, gridHeight];
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                int tileX = gridOriginX + x;
                int tileY = gridOriginY + y;
                Vector3Int cellPos = new Vector3Int(tileX, tileY, 0);
                
                bool walkable = !collisionTilemap.HasTile(cellPos);
                Vector3 worldPos = collisionTilemap.GetCellCenterWorld(cellPos);

                grid[x,y] = new Node(walkable,worldPos,x,y);


                //Vector3Int cellPos = new Vector3Int(bounds.min.x + x, bounds.min.y + y, 0);
                //bool walkable = !collisionTilemap.HasTile(cellPos);
                //Vector3 worldPos = collisionTilemap.GetCellCenterWorld(cellPos);
                //grid[x, y] = new Node(walkable, worldPos, x, y);
            }
        }
    }

    Node NodeFromWorldPoint(Vector3 worldPos)
    {
        //// 获取边界原点
        //BoundsInt bounds = collisionTilemap.cellBounds;
        //Vector3 worldOrigin = collisionTilemap.GetCellCenterWorld(bounds.min);

        //Debug.Log($"世界坐标: {worldPos}, 原点: {worldOrigin}");

        //// 计算在网格中的坐标
        //float percentX = (worldPos.x - worldOrigin.x) / nodeSize;
        //float percentY = (worldPos.y - worldOrigin.y) / nodeSize;

        //Debug.Log($"百分比: ({percentX:F2}, {percentY:F2})");

        // 获取原点对应世界坐标
        Vector3Int originCell = new Vector3Int(gridOriginX, gridOriginY, 0);
        Vector3 worldOrigin = collisionTilemap.GetCellCenterWorld(originCell);

        float offsetX = worldPos.x - worldOrigin.x;
        float offsetY = worldPos.y - worldOrigin.y;


        //int x = Mathf.RoundToInt(percentX);
        //int y = Mathf.RoundToInt(percentY);

        int x = Mathf.RoundToInt(offsetX / nodeSize);
        int y = Mathf.RoundToInt(offsetY / nodeSize);



        // 边界检查
        if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
            return null;
        return grid[x, y];
    }

    Node GetLowestFCostNode(List<Node> openSet)
    {
        Node lowest = openSet[0];
        for(int i = 1;i < openSet.Count; i++)
        {
            if (openSet[i].FCost < lowest.FCost)
            {
                lowest = openSet[i];
            }
        }
        return lowest;
    }

    List<Node> GetNeighbors(Node node)
    {
        List<Node> neighbors = new List<Node>();
        //Debug.Log($"GetNeighbors: 当前节点 ({node.gridX}, {node.gridY})");
        int[] dx = { 0, 0, -1, 1 };
        int[] dy = { 1, -1, 0, 0 };

        for(int i = 0;i<4;i++)
        {
            int checkX = node.gridX + dx[i];
            int checkY = node.gridY + dy[i];
           // Debug.Log($"  检查偏移 ({dx[i]},{dy[i]}) -> 坐标 ({checkX},{checkY})");
            if (checkX >= 0 && checkX < gridWidth &&  checkY >= 0 && checkY < gridHeight)
            {
                neighbors.Add(grid[checkX,checkY]);
            }
        }
        return neighbors;
    }

    // 曼哈顿距离 四方向移动
    int GetDistance(Node a,Node b)
    {
        int dstX = Mathf.Abs(a.gridX - b.gridX);
        int dstY = Mathf.Abs(a.gridY - b.gridY);
        
        // 取10步长方便计算
        return (dstX + dstY) * 10;
    }

    // 回溯生成路径
    List<Vector3> RetracePath(Node startNode,Node endNode)
    {
        List<Vector3> path = new List<Vector3>();
        Node currentNode = endNode;
        while(currentNode != startNode)
        {
            path.Add(currentNode.worldPos);
            currentNode = currentNode.parent;
        }

        path.Reverse();
        return path;
    }

    public List<Vector3> FindPath(Vector3 startPos,Vector3 targetPos)
    {
        // 记录调试数据
        debugStartPos = startPos;
        debugTargetPos = targetPos;

        Node startNode = NodeFromWorldPoint(startPos);
        Node targetNode = NodeFromWorldPoint(targetPos);
        // 有效性检查
        if (startNode == null || targetNode == null) return null;
        if(!startNode.walkable || !targetNode.walkable) return null;

        // 初始化检查列表
        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();

        openSet.Add(startNode);

        int iterations = 0;
        int maxIterations = 10000;

        while(openSet.Count > 0 && iterations < maxIterations)
        {
            iterations++;
            Node currentNode = GetLowestFCostNode(openSet);

            //到达终点
            if(currentNode == targetNode)
            {
                return RetracePath(startNode, targetNode);
            }
            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            foreach(Node neighbor in GetNeighbors(currentNode))
            {
                if (!neighbor.walkable || closedSet.Contains(neighbor)) {
                    continue;
                }

                int newCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor);

                // 新路径更短或邻居不在未检查列表中
                if (newCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor)){
                    neighbor.gCost = newCostToNeighbor;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
            debugOpenSet = new List<Node>(openSet);
            debugClosedSet = new HashSet<Node>(closedSet);
        }
        if (openSet.Count == 0)
        {
            Debug.Log("开放列表为空，无法到达目标！");
        }

        return null;

    }

    void PrintPath(List<Vector3> path,string title = "路径信息")
    {
        if (path == null)
        {
            Debug.Log($"{title}: 路径为空");
            return;
        }

       // Debug.Log($"=== {title} ===");
       // Debug.Log($"路径点数量: {path.Count}");

        for (int i = 0; i < path.Count; i++)
        {
            //Debug.Log($"点 {i}: ({path[i].x:F2}, {path[i].y:F2})");
        }

        // 计算总距离
        float totalDistance = 0;
        for (int i = 0; i < path.Count - 1; i++)
        {
            totalDistance += Vector3.Distance(path[i], path[i + 1]);
        }
        Debug.Log($"总距离: {totalDistance:F2}");
        Debug.Log($"=================");
    }

    ///<summary>
    /// 将路径合并格式化为行动列表 格式如下
    /// (步数，方向)
    ///</summary>

    public List<(int steps,string direction)> FormatPath(List<Vector3> path)
    {
        List<(int steps,string direction)> formattedPath = new List<(int steps, string direction)> ();
        if(path == null || path.Count < 2) { return formattedPath; }

        List<Node> nodePath = ConvertToNodePath(path);
        if(nodePath.Count < 2) return formattedPath;

        int steps = 1;
        Vector2 currentDir = GetDirection(nodePath[0], nodePath[1]);
        for (int i = 1; i < nodePath.Count - 1; i++)
        {
            Vector2 nextDir = GetDirection(nodePath[i], nodePath[i + 1]);

            if (nextDir == currentDir)
            {
                steps++;
            }
            else
            {
                formattedPath.Add((steps, DirectionToString(currentDir)));
                steps = 1;
                currentDir = nextDir;
            }
        }
        formattedPath.Add((steps, DirectionToString(currentDir)));
        return formattedPath;
    }
    /// <summary>
    /// 将世界坐标路径转换为节点路径
    /// </summary>
    private List<Node> ConvertToNodePath(List<Vector3> worldPath)
    {
        List<Node> nodePath = new List<Node>();

        foreach (Vector3 point in worldPath)
        {
            Node node = NodeFromWorldPoint(point);
            if (node != null)
            {
                nodePath.Add(node);
            }
        }

        return nodePath;
    }

    /// <summary>
    /// 获取两个节点之间的方向
    /// </summary>
    private Vector2 GetDirection(Node from, Node to)
    {
        int dx = to.gridX - from.gridX;
        int dy = to.gridY - from.gridY;

        if (dx > 0) return Vector2.right;
        if (dx < 0) return Vector2.left;
        if (dy > 0) return Vector2.up;
        if (dy < 0) return Vector2.down;
        return Vector2.zero;
    }

    /// <summary>
    /// 将方向向量转换为字符串
    /// </summary>
    private string DirectionToString(Vector2 dir)
    {
        if (dir == Vector2.right) return "right";
        if (dir == Vector2.left) return "left";
        if (dir == Vector2.up) return "up";
        if (dir == Vector2.down) return "down";
        return "none";
    }

    public List<(int steps, string direction)> GetFormattedPath(Vector3 startPos, Vector3 targetPos)
    {
        List<Vector3> path = FindPath(startPos, targetPos);
        return FormatPath(path);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
