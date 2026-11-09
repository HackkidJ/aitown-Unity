using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WallDataCollector : MonoBehaviour
{
    [Header("碰撞层Tilemap")]
    public Tilemap collisionTilemap;

    [Header("输出")]
    public List<WallRect> wallRects = new List<WallRect>();

    [System.Serializable]
    public class WallRect
    {
        public float x;
        public float y;
        public float width;
        public float height;

        public WallRect(float _x, float _y, float _w, float _h)
        {
            x = _x; y = _y; width = _w; height = _h;
        }
    }

    [System.Serializable]
    public class MapWallData
    {
        public string map_name;
        public List<WallRect> walls;
    }
    // Start is called before the first frame update

    public List<WallRect> GetWallRects()
    {
        List<WallRect> rects = new List<WallRect>();
        if (collisionTilemap == null) return rects;

        BoundsInt bounds = collisionTilemap.cellBounds;
        HashSet<Vector3Int> processed = new HashSet<Vector3Int>();

        for (int x = bounds.min.x; x < bounds.max.x; x++)
        {
            for (int y = bounds.min.y; y < bounds.max.y; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (!collisionTilemap.HasTile(cell)) continue;
                if (processed.Contains(cell)) continue;

                // 找连续水平线段
                int startX = x, endX = x;
                while (collisionTilemap.HasTile(new Vector3Int(endX + 1, y, 0))) endX++;
                while (collisionTilemap.HasTile(new Vector3Int(startX - 1, y, 0))) startX--;

                for (int tx = startX; tx <= endX; tx++)
                    processed.Add(new Vector3Int(tx, y, 0));

                Vector3 worldPos = collisionTilemap.GetCellCenterWorld(new Vector3Int(startX, y, 0));
                float width = (endX - startX + 1) * collisionTilemap.cellSize.x;

                rects.Add(new WallRect(Mathf.Round(worldPos.x - width / 2),Mathf.Round(worldPos.y - 0.5f),Mathf.Round(width),1f));
            }
        }

        Debug.Log($"收集到 {rects.Count} 个墙壁区域");
        return rects;
    }
}

