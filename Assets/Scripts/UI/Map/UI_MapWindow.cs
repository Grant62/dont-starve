using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using JKFrame;
using UnityEngine.Serialization;

[UIElement(true,"UI/UI_MapWindow",4)]
public class UI_MapWindow : UI_WindowBase
{
    [SerializeField] private RectTransform contentTransform; // 所有地图块、Icon显示的父物体
    private float contentSize;

    [SerializeField] private GameObject mapItemPrefab; // 单个地图块在UI中的预制体
    [SerializeField] private GameObject mapIconPrefab; // 单个Icon在UI中的预制体
    [SerializeField] private RectTransform playerIcon; // 玩家所在位置的Icon

    private Dictionary<Vector2Int, Image> mapImageDic = new Dictionary<Vector2Int, Image>();    // 地图图片字典 Key是坐标
    
    private float mapChunkImageSize;      // UI地图块的尺寸
    public int mapChunkAmount;   // 一个地图块有多少个格子
    private float mapSizeOnWorld;   // 3D地图在世界中的坐标
    private Sprite forestSprite;    // 森林地块精灵

    private float minScale; // 最小的放大倍数
    private float maxScale; // 最大的放大倍数

    /// <summary>
    /// 初始化地图
    /// </summary>
    /// <param name="chunkAmount">一个地图一行或一列有多少个Image/Chunk</param>
    /// <param name="mapSizeOnWorld">地图在世界中一行或一列有多大</param>
    /// <param name="forestTexture">森林的贴图</param>
    public void InitMap(float chunkAmount, int mapChunkAmount, float mapSizeOnWorld, Texture2D forestTexture)
    {
        this.mapSizeOnWorld = mapSizeOnWorld;
        this.forestSprite = CreateMapSprite(forestTexture);
        this.mapChunkAmount = mapChunkAmount;
        
        // 内容尺寸
        contentSize = mapSizeOnWorld * 10;
        contentTransform.sizeDelta = new Vector2(contentSize, contentSize);
        
        // 一个UI地图块的尺寸
        mapChunkImageSize = contentSize / chunkAmount;
        minScale = 1050f / contentSize;
    }
    
    /// <summary>
    /// 更新中心点，为了鼠标缩放的时候，中心点是玩家现在的坐标
    /// </summary>
    /// <param name="viewerPosition"></param>
    public void UpdatePivot(Vector3 viewerPosition)
    {
        float x = viewerPosition.x / mapSizeOnWorld;
        float y = viewerPosition.y / mapSizeOnWorld;
        contentTransform.pivot = new Vector2(x, y);
    }

    /// <summary>
    /// 添加单个地图块
    /// </summary>
    public void AddMapChunk(Vector2Int chunkIndex, List<MapChunkMapObjectModel> mapObjectList,
        Texture2D texture = null)
    {
        RectTransform mapChunkRect = Instantiate(mapItemPrefab, contentTransform).GetComponent<RectTransform>();
        // 确定地图块Image的坐标和宽高
        mapChunkRect.anchoredPosition = new Vector2(chunkIndex.x * mapChunkImageSize, chunkIndex.y * mapChunkImageSize);
        mapChunkRect.sizeDelta = new Vector2(mapChunkImageSize, mapChunkImageSize);

        Image mapChunkImage = mapChunkRect.GetComponent<Image>();
        // 森林的情况
        if (texture == null)
        {
            mapChunkImage.type = Image.Type.Tiled;
            // 要在一个image中显示 这个地图块包含的格子数量
            float ratio = forestSprite.texture.width / mapChunkImageSize;
            mapChunkImage.pixelsPerUnitMultiplier = mapChunkAmount * ratio;
        }
        else mapChunkImage.sprite = CreateMapSprite(texture);
    }
    /// <summary>
    /// 生成地图精灵
    /// </summary>
    private Sprite CreateMapSprite(Texture2D texture)
    {
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }
}