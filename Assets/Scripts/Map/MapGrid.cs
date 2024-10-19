using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 网格，主要包含顶点和格子
/// </summary>
public class MapGrid
{
    // 顶点数据
    public Dictionary<Vector2Int, MapVertex> vertexDic = new Dictionary<Vector2Int, MapVertex>();
    // 格子数据
    public Dictionary<Vector2Int, MapCell> cellDic = new Dictionary<Vector2Int, MapCell>();

    public MapGrid(int mapHeight, int mapWdith, float cellSize)
    {
        MapHeight = mapHeight;
        MapWidth = mapWdith;
        CellSize = cellSize;

        // 生成顶点数据
        for (int x = 1; x < mapWdith; x++)
        {
            for (int z = 1; z < mapHeight; z++)
            {
                AddVertex(x, z);
                AddCell(x, z);
            }
        }
        // 增加一行一列
        for (int x = 1; x <= mapWdith; x++)
        {
            AddCell(x, mapHeight);
        }
        for (int z = 1; z < mapWdith; z++)
        {
            AddCell(mapWdith, z);
        }

    }

    public int MapHeight { get; private set; }
    public int MapWidth { get; private set; }
    public float CellSize { get; private set; }


    #region 顶点
    private void AddVertex(int x, int y)
    {
        vertexDic.Add(new Vector2Int(x, y)
            , new MapVertex()
            {
                Position = new Vector3(x * CellSize, 0, y * CellSize)
            });
    }

    /// <summary>
    /// 获取顶点，如果找不到返回Null
    /// </summary>
    public MapVertex GetVertex(Vector2Int index)
    {
        MapVertex vertex = null;
        vertexDic.TryGetValue(index, out vertex);
        return vertex;
    }
    public MapVertex GetVertex(int x, int y)
    {
        return GetVertex(new Vector2Int(x, y));
    }

    /// <summary>
    /// 通过世界坐标获取顶点
    /// </summary>
    public MapVertex GetVertexByWorldPosition(Vector3 position)
    {
        int x = Mathf.Clamp(Mathf.RoundToInt(position.x / CellSize), 1, MapWidth);
        int y = Mathf.Clamp(Mathf.RoundToInt(position.z / CellSize), 1, MapHeight);
        return GetVertex(x, y);
    }


    /// <summary>
    /// 设置顶点类型
    /// </summary>
    private void SetVertexType(Vector2Int vertexIndex, MapVertexType mapVertexType)
    {
        MapVertex vertex = GetVertex(vertexIndex);
        if (vertex.VertexType != mapVertexType)
        {
            vertex.VertexType = mapVertexType;
            // 只有沼泽需要计算
            if (vertex.VertexType == MapVertexType.Marsh)
            {
                // 计算附近的贴图权重

                MapCell tempCell = GetLeftBottomMapCell(vertexIndex);
                if (tempCell != null) tempCell.TextureIndex += 1;

                tempCell = GetRightBottomMapCell(vertexIndex);
                if (tempCell != null) tempCell.TextureIndex += 2;

                 tempCell = GetLeftTopMapCell(vertexIndex);
                if (tempCell != null) tempCell.TextureIndex += 4;

                tempCell = GetRightTopMapCell(vertexIndex);
                if (tempCell != null) tempCell.TextureIndex += 8;
            }
        }
    }

    /// <summary>
    /// 设置顶点类型
    /// </summary>
    private void SetVertexType(int x,int y,MapVertexType mapVertexType)
    {
        SetVertexType(new Vector2Int(x, y), mapVertexType);
    }
    #endregion

    #region 格子
    private void AddCell(int x, int y)
    {
        float offset = CellSize / 2;
        cellDic.Add(new Vector2Int(x, y), 
            new MapCell()
            {
                Position = new Vector3(x * CellSize - offset, 0, y * CellSize - offset)
            }
        );
    }

    /// <summary>
    /// 获取格子，如果没有找到会返回Null
    /// </summary>
    public MapCell GetCell(Vector2Int index)
    {
        MapCell cell = null;
        cellDic.TryGetValue(index, out cell);
        return cell;
    }

    public MapCell GetCell(int x,int y)
    {
        return GetCell(new Vector2Int(x,y));
    }

    /// <summary>
    /// 获取左下角格子
    /// </summary>
    public MapCell GetLeftBottomMapCell(Vector2Int vertexIndex)
    {
        return GetCell(vertexIndex);
    }

    /// <summary>
    /// 获取右下角格子
    /// </summary>
    public MapCell GetRightBottomMapCell(Vector2Int vertexIndex)
    {
        return GetCell(vertexIndex.x+1,vertexIndex.y);
    }

    /// <summary>
    /// 获取左上角格子
    /// </summary>
    public MapCell GetLeftTopMapCell(Vector2Int vertexIndex)
    {
        return GetCell(vertexIndex.x, vertexIndex.y+1);
    }

    /// <summary>
    /// 获取右上角格子
    /// </summary>
    public MapCell GetRightTopMapCell(Vector2Int vertexIndex)
    {
        return GetCell(vertexIndex.x+1, vertexIndex.y + 1);
    }
    #endregion

    /// <summary>
    /// 计算格子贴图的索引数字
    /// </summary>
    public void CalculateMapVertexType(float[,] noiseMap,float limit)
    { 
        int width = noiseMap.GetLength(0);
        int height = noiseMap.GetLength(1);

        for (int x = 1; x < width; x++)
        {
            for (int z = 1; z < height; z++)
            {
                // 基于噪声中的值确定这个顶点的类型
                // 大于边界是沼泽，否则是森林
                if (noiseMap[x,z] >=limit)
                {
                    SetVertexType(x, z, MapVertexType.Marsh);
                }
                else
                {
                    SetVertexType(x, z, MapVertexType.Forest);
                }
            }
        }
    }
}

/// <summary>
/// 顶点类型
/// </summary>
public enum MapVertexType
{ 
    Forest, //森林
    Marsh,  //沼泽
}

/// <summary>
/// 地图顶点
/// </summary>
public class MapVertex
{
    public Vector3 Position;
    public MapVertexType VertexType;
}

/// <summary>
/// 地图格子
/// </summary>
public class MapCell
{
    public Vector3 Position;
    public int TextureIndex;
}
