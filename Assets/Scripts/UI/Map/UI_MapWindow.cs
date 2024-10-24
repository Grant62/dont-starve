using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using JKFrame;

/// <summary>
/// UI地图窗口
/// </summary>
[UIElement(true, "UI/UI_MapWindow",4)]
public class UI_MapWindow : UI_WindowBase
{
    [SerializeField] private RectTransform contentTrans; // 所有地图块、Icon显示的父物体
    private float contentSize;

    [SerializeField] private GameObject mapItemPrefab; // 单个地图块在UI中的预制体
    [SerializeField] private GameObject mapIconPrefab; // 单个Icon在UI中的预制体
    [SerializeField] private RectTransform playerIcon; // 玩家所在位置的Icon

    private Dictionary<Vector2Int, Image> mapImageDic = new Dictionary<Vector2Int, Image>();    // 地图图片字典 Key是坐标

    private float mapChunkImageSize;  // UI地图块的尺寸
    private int mapChunkAmount;   // 一个地图块有多少个格子   
    private float mapSizeOnWorld;// 3D地图在世界中的坐标
    private Sprite forestSprite;// 森林地块的精灵

    private float minScale; // 最小的放大倍数
    private float maxScale = 10; // 最大的放大倍数

    public override void Init()
    {
        transform.Find("Scroll View").GetComponent<ScrollRect>().onValueChanged.AddListener(UpdatePlayerIconPos);
    }

    private void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            float newScale = Mathf.Clamp(contentTrans.localScale.x + scroll, minScale, maxScale);
            contentTrans.localScale = new Vector3(newScale, newScale, 0);
        }
    }

    /// <summary>
    /// 初始化地图
    /// </summary>
    /// <param name="mapAmount">一个地图一行或一列有多少个Image/Chunk</param>
    /// <param name="mapSizeOnWord">地图在世界中一行或一列有多大</param>
    /// <param name="forestTexture">森林的贴图</param>
    public void InitMap(float mapAmount,int mapChunkSize, float mapSizeOnWord,Texture2D forestTexture)
    { 
        this.mapSizeOnWorld = mapSizeOnWord;
        forestSprite = CreateMapSprite(forestTexture);
        this.mapChunkAmount = mapChunkSize;
        // 内容尺寸
        contentSize = mapSizeOnWord * 10;
        contentTrans.sizeDelta = new Vector2(contentSize, contentSize);

        // 一个UI地图块的尺寸
        mapChunkImageSize = contentSize / mapAmount;
        minScale = 1050f / contentSize;
    }

    /// <summary>
    /// 更新中心点，为了鼠标缩放的时候，中心点是玩家现在的坐标
    /// </summary>
    /// <param name="viewerPosition"></param>
    public void UpdatePivot(Vector3 viewerPosition)
    {
        float x = viewerPosition.x / mapSizeOnWorld;
        float y = viewerPosition.z / mapSizeOnWorld;
        // 修改Content后会导致Scroll Rect 组件的 当值修改事件=》UpdatePlayerIconPos
        contentTrans.pivot = new Vector2(x, y);
    }

    public void UpdatePlayerIconPos(Vector2 value)
    {
        // 玩家的Icon完全放在Content的中心点
        playerIcon.anchoredPosition3D = contentTrans.anchoredPosition3D;
    }

    /// <summary>
    /// 添加一个地图块
    /// </summary>
    public void AddMapChunk(Vector2Int chunkIndex,List<MapChunkMapObjectModel> mapObjectList,Texture2D texture = null)
    { 
        RectTransform mapChunkRect = Instantiate(mapItemPrefab,contentTrans).GetComponent<RectTransform>();
        // 确定地图块的Image的坐标和宽高
        mapChunkRect.anchoredPosition = new Vector2(chunkIndex.x * mapChunkImageSize, chunkIndex.y * mapChunkImageSize);
        mapChunkRect.sizeDelta = new Vector2(mapChunkImageSize, mapChunkImageSize);

        Image mapChunkImage = mapChunkRect.GetComponent<Image>();
        // 森林的情况
        if (texture == null)
        {
            mapChunkImage.type = Image.Type.Tiled;
            // 设置贴瓷砖的比例，要在一个Image中显示 这个地图块所包含的格子数量
            // 贴图和Image的比列
            float ratio = forestSprite.texture.width / mapChunkImageSize;
            // 一个地图块上有多少个格子
            mapChunkImage.pixelsPerUnitMultiplier = mapChunkAmount * ratio;
            mapChunkImage.sprite = forestSprite;
        }
        else mapChunkImage.sprite = CreateMapSprite(texture);

        // TODO:添加物体的ICON

        // TODO:待重构，因为肯定还需要保存ICON的信息用来后续移除（因为ICON代表的花草树木有可能会消失）
        mapImageDic.Add(chunkIndex, mapChunkImage);
    }


    /// <summary>
    /// 生成地图精灵
    /// </summary>
    private Sprite CreateMapSprite(Texture2D texture)
    {
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

}
