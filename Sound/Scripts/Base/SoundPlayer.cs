using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//执行音频播放类
public partial class SoundPlayer : MonoBehaviour
{
    public void Click()
    {
        SoundCtrl.Instance.Play(SoundType.Click);
    }
}
