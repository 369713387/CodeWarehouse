using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// 音效播放器
/// </summary>
public class SoundCtrl : SoundCtrlBase<SoundCtrl>
{
    private PlayerData user_data;

    public override void Init()
    {
        user_data = PlayerSaveTool.data;

        base.Init();
    }

    //开关音效
    public override void OpenOrStopSound(bool tag)
    {
        user_data.tag_Sound = tag;
        base.OpenOrStopSound(tag);
    }

    //开关音效
    public override void OpenOrStopBgSound(bool tag)
    {
        user_data.tag_BgSound = tag;
        base.OpenOrStopBgSound(tag);
    }
}
