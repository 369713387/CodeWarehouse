using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class SoundDBModel : AbstractDBModel<SoundDBModel, Sound>
{
    /// <summary>
    /// 所有音效Path Dic
    /// </summary>
    public Dictionary<SoundType, Sound> dicSoundPath = new Dictionary<SoundType, Sound>();

    /// <summary>
    /// 地址后缀
    /// </summary>
    private string path = "Sound/";

   public void Init()
    {
        foreach (Sound item in GetList())
        {
            if (SoundType.IsDefined(typeof(SoundType), item.SoundName))
            {
                SoundType key = (SoundType)System.Enum.Parse(typeof(SoundType), item.SoundName);

                dicSoundPath.Add(key, item);
            }
            else
            {
                Debug.Log("enum无此名称：" + item.SoundName);
            }
        }
        
        SoundCtrl.Instance.Init();
        InitBglist();
    }

    /// <summary>
    /// 初始化SoundCtrl的背景音乐列表
    /// </summary>
    public void InitBglist()
    {
        foreach (Sound item in GetList())
        {
            if (item.isBg)
            {
                if (SoundType.IsDefined(typeof(SoundType), item.SoundName))
                {
                    SoundType key = (SoundType)System.Enum.Parse(typeof(SoundType), item.SoundName);

                    SoundCtrl.Instance.listBgSound.Add(key);
                }
            }
        }
    }

    /// <summary>
    /// 获取声音播放路径
    /// </summary>
    public string GetSoundPath(string soundName)
    {
        return path + soundName;
    }
}
