using System.Collections.Generic;
using UnityEngine;
using JKFrame;
/// <summary>
/// 地图块数据
/// </summary>
public class MapChunkData
{
    public List<MapChunkMapObjectModel> MapObjectList = new List<MapChunkMapObjectModel>();
}
public class MapChunkMapObjectModel
{
    public int ConfigID;
    public Vector3 Position;
}

public class MapChunkController : MonoBehaviour
{
    public Vector2Int ChunkIndex { get; private set; }
    public Vector3 CenterPosition { get; private set; }
    public bool IsAllForest { get; private set; }
    public MapChunkData mapChunkData { get; private set; }
    private List<GameObject> mapObjectList;
    public bool IsInitialized { get; private set; } = false;
    private bool isActive = false;
    public void Init(Vector2Int chunkIndex, Vector3 centrePosition, bool isAllForest, List<MapChunkMapObjectModel> MapObjectList)
    {
        ChunkIndex = chunkIndex;
        CenterPosition = centrePosition;
        mapChunkData = new MapChunkData();
        mapChunkData.MapObjectList = MapObjectList;
        mapObjectList = new List<GameObject>(MapObjectList.Count);
        IsAllForest = isAllForest;
        IsInitialized = true;
    }
    
    public void SetActive(bool active)
    {
        if (isActive!=active)
        {
            isActive = active;
            gameObject.SetActive(isActive);
            List<MapChunkMapObjectModel> ObjectList = mapChunkData.MapObjectList;
            // 从对象池中获取所有物体
            if (isActive)
            {
                for (int i = 0; i < ObjectList.Count; i++)
                {
                    MapObjectConfig config = ConfigManager.Instance.GetConfig<MapObjectConfig>(ConfigName.MapObject, ObjectList[i].ConfigID);
                    GameObject go = PoolManager.Instance.GetGameObject(config.Prefab, transform);
                    go.transform.position = ObjectList[i].Position;
                    mapObjectList.Add(go);
                }
            }
            // 把所有物体放回对象池
            else
            {
                for (int i = 0; i < ObjectList.Count; i++)
                {
                    PoolManager.Instance.PushGameObject(mapObjectList[i]);
                }
                mapObjectList.Clear();
            }
        }
    }
}
