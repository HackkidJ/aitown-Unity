// Pathfinding.cs
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Tilemaps;


// 网格地图使用A寻路算法
public class Pathfinding : MonoBehaviour
{
    [Header("寻路设置")]
    public Tilemap navTilemap;

    private static Pathfinding _instance;
    public static Pathfinding Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<Pathfinding>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("Pathfinding");
                    _instance = go.AddComponent<Pathfinding>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    [Header("网格设置")]
    public float nodeSize = 0.5f;
    private int gridWidth;
    private int gridHeight;

    private Node[,] grid;
    private LayerMask obstacleMask;

    void Awake()
    {
        if (navTilemap == null)
        {
            navTilemap = GameObject.Find("Navigation")?.GetComponent<Tilemap>();
            if (navTilemap == null)
            {
                Debug.LogError("找不到 Navigation Tilemap，请手动拖拽赋值");
            }
        }
        obstacleMask = LayerMask.GetMask("Collision"); // 设置碰撞层
        CreateGrid();
    }


    // 通过新建导航层 绘画可走的方格规避有墙有障碍的地方 以此解决collision层过于复杂 a寻路算法失效问题
    void CreateGrid()
    {
        BoundsInt bounds = navTilemap.cellBounds;

        // 如果边界无效，尝试压缩 Tilemap
        if (bounds.size.x == 0 || bounds.size.y == 0)
        {
            navTilemap.CompressBounds();
            bounds = navTilemap.cellBounds;
        }

        if (bounds.size.x == 0 || bounds.size.y == 0)
        {
            Debug.LogError($"导航 Tilemap 边界无效！请检查 Navigation 层是否正确绘制了 Tile。");
            return;
        }

        Vector3 worldMin = navTilemap.CellToWorld(bounds.min);
        Vector3 worldMax = navTilemap.CellToWorld(bounds.max);

        gridWidth = Mathf.CeilToInt((worldMax.x - worldMin.x) / nodeSize);
        gridHeight = Mathf.CeilToInt((worldMax.y - worldMin.y) / nodeSize);

        Debug.Log($"网格尺寸: {gridWidth} x {gridHeight}, 世界范围: X({worldMin.x}~{worldMax.x}), Y({worldMin.y}~{worldMax.y})");

        if (gridWidth <= 0 || gridHeight <= 0)
        {
            Debug.LogError($"网格尺寸无效！gridWidth={gridWidth}, gridHeight={gridHeight}");
            return;
        }

        grid = new Node[gridWidth, gridHeight];

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 worldPoint = worldMin + new Vector3(x * nodeSize, y * nodeSize, 0);
                Vector3Int cellPos = navTilemap.WorldToCell(worldPoint);
                bool walkable = navTilemap.HasTile(cellPos);
                grid[x, y] = new Node(walkable, worldPoint, x, y);
            }
        }
    }
    void OnDrawGizmos()
    {
        if (grid == null) return;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Node node = grid[x, y];
                if (node == null) continue;

                // 可行走：绿色，不可行走：红色
                Gizmos.color = node.walkable ? Color.green : Color.red;
                Gizmos.DrawWireCube(node.worldPosition, Vector3.one * nodeSize * 0.8f);
            }
        }
    }
    public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        Node startNode = NodeFromWorldPoint(startPos);
        Node targetNode = NodeFromWorldPoint(targetPos);

        if (startNode == null || targetNode == null || !targetNode.walkable)
            return null;

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet.OrderBy(n => n.fCost).First();

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
            {
                return RetracePath(startNode, targetNode);
            }

            foreach (Node neighbor in GetNeighbors(currentNode))
            {
                if (!neighbor.walkable || closedSet.Contains(neighbor))
                    continue;

                int newCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor);

                if (newCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newCostToNeighbor;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        return null;
    }

    List<Vector3> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }

        path.Reverse();
        return path.Select(n => n.worldPosition).ToList();
    }

    List<Node> GetNeighbors(Node node)
    {
        List<Node> neighbors = new List<Node>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;

                int checkX = node.gridX + x;
                int checkY = node.gridY + y;

                if (checkX >= 0 && checkX < gridWidth && checkY >= 0 && checkY < gridHeight)
                {
                    neighbors.Add(grid[checkX, checkY]);
                }
            }
        }

        return neighbors;
    }

    Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        float percentX = (worldPosition.x + gridWidth / 2) / gridWidth;
        float percentY = (worldPosition.y + gridHeight / 2) / gridHeight;

        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((gridWidth - 1) * percentX);
        int y = Mathf.RoundToInt((gridHeight - 1) * percentY);

        if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
            return null;

        return grid[x, y];
    }

    int GetDistance(Node a, Node b)
    {
        int dstX = Mathf.Abs(a.gridX - b.gridX);
        int dstY = Mathf.Abs(a.gridY - b.gridY);

        if (dstX > dstY)
            return 14 * dstY + 10 * (dstX - dstY);
        return 14 * dstX + 10 * (dstY - dstX);
    }

    public class Node
    {
        public bool walkable;
        public Vector3 worldPosition;
        public int gridX;
        public int gridY;
        public int gCost;
        public int hCost;
        public Node parent;

        public int fCost => gCost + hCost;

        public Node(bool _walkable, Vector3 _worldPos, int _gridX, int _gridY)
        {
            walkable = _walkable;
            worldPosition = _worldPos;
            gridX = _gridX;
            gridY = _gridY;
        }
    }
}