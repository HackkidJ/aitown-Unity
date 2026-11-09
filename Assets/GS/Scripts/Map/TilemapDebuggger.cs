using UnityEngine;
using UnityEngine.Tilemaps;
using System.Text;
using System.IO;

public class TilemapDebugger : MonoBehaviour
{
    [Header("碰撞层 Tilemap")]
    public Tilemap collisionTilemap;

    [Header("打印范围")]
    public int xMin = -80;
    public int xMax = 80;
    public int yMin = -30;
    public int yMax = 30;

    [ContextMenu("打印到控制台")]
    public void PrintToConsole()
    {
        if (collisionTilemap == null)
        {
            Debug.LogError("collisionTilemap 未设置");
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("========== Collision Tilemap 地图 ==========");

        for (int y = yMax; y >= yMin; y--)
        {
            string line = $"{y,3}: ";
            for (int x = xMin; x <= xMax; x++)
            {
                Vector3Int cellPos = new Vector3Int(x, y, 0);
                if (collisionTilemap.HasTile(cellPos))
                {
                    line += "#";
                }
                else
                {
                    line += ".";
                }
            }
            sb.AppendLine(line);
        }
        sb.AppendLine("============================================");

        Debug.Log(sb.ToString());
    }

    [ContextMenu("保存到文件")]
    public void SaveToFile()
    {
        if (collisionTilemap == null)
        {
            Debug.LogError("collisionTilemap 未设置");
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("========== Collision Tilemap 地图 ==========");
        sb.AppendLine($"范围: X({xMin}~{xMax}), Y({yMin}~{yMax})");
        sb.AppendLine();

        for (int y = yMax; y >= yMin; y--)
        {
            string line = $"{y,3}: ";
            for (int x = xMin; x <= xMax; x++)
            {
                Vector3Int cellPos = new Vector3Int(x, y, 0);
                if (collisionTilemap.HasTile(cellPos))
                {
                    line += "■";
                }
                else
                {
                    line += "□";
                }
            }
            sb.AppendLine(line);
        }
        sb.AppendLine("============================================");

        string path = Application.dataPath + "/CollisionMap.txt";
        File.WriteAllText(path, sb.ToString());
        Debug.Log($"地图已保存到: {path}");
    }
}