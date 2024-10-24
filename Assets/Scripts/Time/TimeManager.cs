using System.Collections;
using UnityEngine;
using JKFrame;
using System;
using Sirenix.OdinInspector;

/// <summary>
/// 时间状态数据
/// </summary>
[Serializable]
public class TimeStateData
{
    // 持续时间
    public float durationTime;
    // 阳光强度
    public float sunIntensity;
    // 阳光颜色
    public Color sunColor;
    // 太阳的角度
    [OnValueChanged(nameof(SetRotation))]
    public Vector3 sunRotation;
    [HideInInspector]
    public Quaternion sunQuaternion;

    private void SetRotation()
    {
        sunQuaternion = Quaternion.Euler(sunRotation);
    }

    /// <summary>
    /// 检查并且计算时间
    /// </summary>
    /// <returns>是否还在当前状态</returns>
    public bool CheckAndCalTime(float currTime, TimeStateData nextState, out Quaternion rotation, out Color color, out float sunIntensity)
    {
        // 0~1之间
        float ratio = 1f - (currTime / durationTime);
        rotation = Quaternion.Slerp(this.sunQuaternion, nextState.sunQuaternion, ratio);
        color = Color.Lerp(this.sunColor, nextState.sunColor, ratio);
        sunIntensity = UnityEngine.Mathf.Lerp(this.sunIntensity, nextState.sunIntensity, ratio);
        // 如果时间大于0所以还在本状态
        return currTime > 0;
    }
}

/// <summary>
/// 时间管理器
/// </summary>
public class TimeManager : LogicManagerBase<TimeManager>
{
    [SerializeField] private Light mainLight;                   // 太阳
    [SerializeField] private TimeStateData[] timeStateDatas;    // 时间配置
    private int currentStateIndex = 0;
    private float currTime = 0;
    private int dayNum;

    [SerializeField,Range(0,30)] private float timeScale = 1;
    protected override void RegisterEventListener()
    {

    }
    protected override void CancelEventListener()
    {
    }
    private void Start()
    {
        StartCoroutine(UpdateTime());
    }

    private IEnumerator UpdateTime()
    {
        currentStateIndex = 0;   // 默认是早上
        int nextIndex = currentStateIndex + 1;
        currTime = timeStateDatas[currentStateIndex].durationTime;
        dayNum = 0; // 天数
        while (true)
        {
            yield return null;
            currTime -= Time.deltaTime * timeScale;
            // 计算并且得到阳光相关的设置
            if (!timeStateDatas[currentStateIndex].CheckAndCalTime(currTime, timeStateDatas[nextIndex], out Quaternion quaternion, out Color color, out float sunIntensity))
            {
                // 切换到下一个状态
                currentStateIndex = nextIndex;
                // 检查边界，超过就从0开始
                nextIndex = currentStateIndex + 1 >= timeStateDatas.Length ? 0 : currentStateIndex + 1;
                // 如果现在是早上，也就是currentStateIndex==0，那么意味着，天数加1
                if (currentStateIndex == 0) dayNum++;
                currTime = timeStateDatas[currentStateIndex].durationTime;
            }
            mainLight.transform.rotation = quaternion;
            mainLight.color = color;
            SetLight(sunIntensity);
        }
    }

    private void SetLight(float intensity)
    { 
        mainLight.intensity = intensity;
        // 设置环境光的亮度
        RenderSettings.ambientIntensity = intensity;
    }
}
