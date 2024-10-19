using System;
using System.Collections;
using JKFrame;
using Sirenix.OdinInspector;
using UnityEngine;
/// <summary>
/// 时间管理器
/// </summary>
[Serializable]
public class TimeStateData
{
    [LabelText("持续时间")] public float durationTime;
    [LabelText("阳光强度")] public float sunIntensity;
    [LabelText("阳光颜色")] public Color sunColor;
    [OnValueChanged(nameof(SetRotation)),LabelText("太阳的角度")] public Vector3 sunRotation;
    [HideInInspector] public Quaternion sunQuaternion;
     
    /// <summary>
    /// SunRotation值发生改变的回调函数
    /// </summary>
    private void SetRotation()
    {
        sunQuaternion = Quaternion.Euler(sunRotation);
    }
    /// <summary>
    /// 检查并且计算时间
    /// </summary>0
    /// <returns>是否还在当前状态</returns>
    public bool CheckAndCalTime(float currTime,TimeStateData nextState, out Quaternion rotation,out Color color,out float sunIntensity)
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
public class TimeManager : LogicManagerBase<TimeManager>
{
    [SerializeField,LabelText("太阳")] private Light mainLight;                   
    // [SerializeField,LabelText("当前太阳强度")] private float lightValue;                 
    [SerializeField,LabelText("时间配置")] private TimeStateData[] timeStateDatas;    
    private int currentStateIndex = 0;
    private float currTime = 0;
    private int dayNum;
    
    [SerializeField,Range(0,30),LabelText("时间缩放")] private float timeScale = 1;
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
        dayNum = 0;
        while (true)
        {
            yield return null;
            currTime -= Time.deltaTime * timeScale;
            if (!timeStateDatas[currentStateIndex].CheckAndCalTime(currTime, timeStateDatas[nextIndex], out Quaternion quaternion, out Color color, out float sunIntensity))
            {
                currentStateIndex = nextIndex;
                nextIndex = currentStateIndex + 1 >= timeStateDatas.Length ? 0 : currentStateIndex + 1;
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
        RenderSettings.ambientIntensity = intensity;
    }
}
