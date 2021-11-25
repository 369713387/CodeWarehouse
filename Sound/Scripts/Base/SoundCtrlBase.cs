using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 音效播放器基类
/// </summary>
/// <typeparam name="T"></typeparam>
public class SoundCtrlBase<T> : SingletonMono<T>
    where T : MonoBehaviour
{
    protected bool tag_init = false;//初始化标志

    private PlayerData user_data;

    private SoundType cur_BgSound = SoundType.None;//背景音乐播放场景
    private AudioSource cur_BgSource = null;
    private bool isPlayingBG = false;
    private int cur_BgSound_id=-1;
    private int next_BgSound_id=-1;
    private float timer_music;
    /// <summary>
    /// 音效开关
    /// </summary>
    protected bool tag_Sound = true;

    /// <summary>
    /// 音频开关
    /// </summary>
    protected bool tag_BgSound = true;


    #region 声音对象
    /// <summary>
    /// 所有音效Path Dic
    /// </summary>
    private Dictionary<SoundType, Sound> dicSoundData;

    /// <summary>
    /// 音效dic
    /// </summary>
    protected Dictionary<SoundType, AudioSource> dicSound = new Dictionary<SoundType, AudioSource>();

    /// <summary>
    /// 背景音乐dic
    /// </summary>
    protected Dictionary<SoundType, AudioSource> dicBgSound = new Dictionary<SoundType, AudioSource>();
    /// <summary>
    /// 背景音乐list
    /// </summary>
    public List<SoundType> listBgSound = new List<SoundType>();
    #endregion

    public virtual void Init()
    {
        dicSoundData = SoundDBModel.Instance.dicSoundPath;
        user_data = PlayerSaveTool.data;
        tag_Sound = user_data.tag_Sound;
        tag_BgSound = user_data.tag_BgSound;
        tag_init = true;
    }

    void Update()
    {
        
        if (cur_BgSource != null)
        {
            if (!cur_BgSource.isPlaying && isPlayingBG && timer_music <= 0.3f)
            {

                    if (next_BgSound_id == cur_BgSound_id)
                    {
                        next_BgSound_id = CaluNextbg();
                        PlayBg(listBgSound[next_BgSound_id]);
                        //Debug.Log("playbg" + next_BgSound_id + " timer" + timer_music);
                    }
                
            }
            else if(cur_BgSource.isPlaying && isPlayingBG)
            {
                timer_music = timer_music - Time.deltaTime;
                //Debug.Log(timer_music);
            }
        }
    }


    #region 播放音效
    /// <summary>
    /// 直接播放音效
    /// </summary>
    /// <param name="type"></param>
    public virtual void Play(SoundType type)
    {
        if (!tag_Sound)
        {
            return;
        }

        LoadSound(type);
        AudioSource sourceItem = GetAudioSource(type);
        if (sourceItem != null)
        {
            sourceItem.Play();
        }
    }

    /// <summary>
    /// 延迟播放
    /// </summary>
    /// <param name="type"></param>
    /// <param name="delayTime"></param>
    public virtual void Play(SoundType type, float delayTime)
    {
        if (!tag_Sound)
        {
            return;
        }

        StartCoroutine(DelayPlay(type, delayTime));
    }

    IEnumerator DelayPlay(SoundType type, float delayTime)
    {
        yield return new WaitForSeconds(delayTime);
        LoadSound(type);
        AudioSource sourceItem = GetAudioSource(type);

        if (sourceItem != null)
        {
            sourceItem.Play();
        }
    }

    /// <summary>
    /// 播放新音频并且停止掉其他指定音频
    /// </summary>
    /// <param name="type"></param>
    /// <param name="listStopType"></param>
    public virtual void Play(SoundType type, List<SoundType> listStopType)
    {
        if (!tag_Sound)
        {
            return;
        }

        foreach (SoundType item in listStopType)
        {
            Stop(item);
        }

        LoadSound(type);
        AudioSource sourceItem = GetAudioSource(type);

        if (sourceItem != null)
        {
            sourceItem.Play();
        }
    }

    /// <summary>
    /// 播放新音频并且停止掉其他指定音频
    /// </summary>
    /// <param name="type"></param>
    /// <param name="stopType"></param>
    public virtual void Play(SoundType type, SoundType stopType)
    {
        if (!tag_Sound)
        {
            return;
        }

        LoadSound(type);
        Stop(stopType);
        AudioSource sourceItem = GetAudioSource(type);

        if (sourceItem != null)
        {
            sourceItem.Play();
        }
    }
    #endregion

    #region 控制背景音乐
    /// <summary>
    /// 播放背景音乐
    /// </summary>
    /// <param name="type"></param>
    public virtual void PlayBg(SoundType type)
    {
        if(ChangeBgSound(type))
        {
            if (!tag_BgSound)
            {
                
                return;
            }
            
            LoadSound(type);

            if (!dicBgSound.ContainsKey(type))
            {
                Debug.LogError("无此背景音乐 " + type.ToString());
                return;
            }

            //遍历bgSound列表，播放bg的同时，关闭其他bg
            foreach (var item in dicBgSound)
            {
                AudioSource sourceItem = item.Value;
                if (item.Key == type)
                {
                    sourceItem.loop = false;
                    if (!sourceItem.isPlaying)
                    {
                        
                        //从changesong移到这里
                        isPlayingBG = true;
                        cur_BgSound = type;
                        sourceItem.Play();
                        //更改cur_BgSource
                        cur_BgSource = sourceItem;
                        cur_BgSound_id = next_BgSound_id;
                        timer_music = sourceItem.clip.length;
                    }
                }
                else
                {
                   
                    if (sourceItem.isPlaying)
                    {
                        sourceItem.Stop();
                    }
                }
            }
        }
    }
    /// <summary>
    /// 延迟播放背景音乐
    /// </summary>
    /// <param name="type"></param>
    IEnumerator PlayBgDelay(SoundType type,float delayTime)
    {
        if (ChangeBgSound(type))
        {
            if (!tag_BgSound)
            {

                yield break;
            }

            yield return new WaitForSeconds(delayTime);

            LoadSound(type);

            if (!dicBgSound.ContainsKey(type))
            {
                Debug.LogError("无此背景音乐 " + type.ToString());
                yield break;
            }

            //遍历bgSound列表，播放bg的同时，关闭其他bg
            foreach (var item in dicBgSound)
            {
                AudioSource sourceItem = item.Value;
                if (item.Key == type)
                {
                    sourceItem.loop = false;
                    if (!sourceItem.isPlaying)
                    {

                        //从changesong移到这里
                        isPlayingBG = true;
                        cur_BgSound = type;
                        sourceItem.Play();
                        //更改cur_BgSource
                        cur_BgSource = sourceItem;
                        cur_BgSound_id = next_BgSound_id;
                        timer_music = sourceItem.clip.length;
                    }
                }
                else
                {

                    if (sourceItem.isPlaying)
                    {
                        sourceItem.Stop();
                    }
                }
            }
        }
    }
    /// <summary>
    /// 随机播放背景音乐
    /// </summary>
    public virtual void InitRandomPlayInAllBg()
    {
        if (!isPlayingBG)
        {
            if (tag_BgSound)
            {
                Debug.Log("RandomPlay");
                int BGNum = listBgSound.Count;

                next_BgSound_id = Random.Range(0, listBgSound.Count);

                PlayBg(listBgSound[next_BgSound_id]);
            }
        }
    }
    /// <summary>
    /// 延迟随机播放背景音乐
    /// </summary>
    public virtual void InitRandomPlayInAllBgDelay(float delayTime)
    {
        if (!isPlayingBG)
        {
            if (tag_BgSound)
            {
                Debug.Log("RandomPlay");
                int BGNum = listBgSound.Count;

                next_BgSound_id = Random.Range(0, listBgSound.Count);

                StartCoroutine(PlayBgDelay(listBgSound[next_BgSound_id], delayTime));
            }
        }
    }

    /// <summary>
    /// 更改当前需要播放的背景音乐
    /// </summary>
    /// <param name="type"></param>
    private bool ChangeBgSound(SoundType type)
    {
        bool result = false;
        if(dicSoundData.ContainsKey(type))
        {
            Sound datd = dicSoundData[type];
            if(datd.isBg)
            {
                result = true;
            }
            else
            {
                Debug.LogError("要切换的音频不是背景音乐  " + type.ToString());
            }
        }
        else
        {
            Debug.LogError("无此音频  " + type.ToString());
        }
        return result;
    }
    /// <summary>
    /// 停止播放当前背景音乐
    /// </summary>
    public virtual void StopBg()
    {
        isPlayingBG = false;
        Stop(cur_BgSound);
        cur_BgSound = SoundType.None;
        cur_BgSource = null;
        timer_music = 0;


    }

    private int CaluNextbg()
    {
        int BGNumMax = listBgSound.Count-1;
        int temp = cur_BgSound_id;
        if (temp != BGNumMax)
            temp = cur_BgSound_id + 1;
        else
            temp = 0;

        return temp;
    }



    #endregion

    #region 停止播放音效
    /// <summary>
    /// 直接停止所有延迟播放的音频
    /// </summary>
    public void StopAllDelay()
    {
        StopCoroutine("DelayPlay");
    }

    /// <summary>
    /// 停止播放单个音效
    /// </summary>
    /// <param name="type"></param>
    public void Stop(SoundType type)
    {
        AudioSource sourceItem = GetAudioSource(type);
        if (sourceItem != null)
            sourceItem.Stop();
    }



    #endregion

    #region 控制音频开关
    /// <summary>
    /// 开关音效
    /// </summary>
    /// <param name="tag"></param>
    public virtual void OpenOrStopSound(bool tag)
    {
        tag_Sound = tag;
    }

    /// <summary>
    /// 开关背景音乐
    /// </summary>
    /// <param name="tag"></param>
    public virtual void OpenOrStopBgSound(bool tag)
    {
        tag_BgSound = tag;
        
        if(tag_BgSound)
        {
            if (SceneMgr.Instance.CurrentSceneType == SceneType.GA_Menu)
            {
                //局外开启背景音效开关时播放音效
                if (listBgSound.Contains(SoundType.Bj3))
                {
                    PlayBg(SoundType.Bj3);
                }
            }
            else
            {
                //局内开启背景音效开关时播放音效
                if (listBgSound.Contains(cur_BgSound))
                {
                    PlayBg(cur_BgSound);
                }
                else
                {   
                    InitRandomPlayInAllBg();
                }
            }              
        }
        else
        {
            if (listBgSound.Contains(cur_BgSound))
                StopBg();
            else
                PlayBg(SoundType.None);
        }
    }
    #endregion

    #region 暂停播放背景音乐

    public void PauseCurrentBg()
    {
        if (cur_BgSource != null)
            cur_BgSource.Pause();
    }

    public void RecoverPauseCurrentBg()
    {
        if (cur_BgSource != null)
            cur_BgSource.UnPause();
    }

    #endregion

    #region 音效控制

    /// <summary>
    /// 关闭所有音效
    /// </summary>
    public void StopAll()
    {
        foreach (var item in dicSound)
        {
            if (item.Value.isPlaying)
            {
                item.Value.Stop();
            }           
        }
    }

    #endregion

    #region 基础方法
    /// <summary>
    /// 根据枚举获取对应音频对象
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    protected AudioSource GetAudioSource(SoundType type)
    {
        AudioSource soundItem = null;
        if (dicSound.ContainsKey(type))
        {
            soundItem = dicSound[type];
        }
        else if(dicBgSound.ContainsKey(type))
        {
            soundItem = dicBgSound[type];
        }

        return soundItem;
    }

    /// <summary>
    /// 生成音频对象
    /// </summary>
    protected void LoadSound(SoundType type)
    {
        //先判断是否已经加载了音效
        if(dicSound.ContainsKey(type) || dicBgSound.ContainsKey(type))
        {
            return;
        }
        else
        {
            //将音频加载进来
            if(dicSoundData.ContainsKey(type))
            {
                Sound item = dicSoundData[type];
                GameObject obj = new GameObject(item.SoundName);
                obj.transform.parent = transform;
                AudioSource temp = obj.GetOrAddComponent<AudioSource>();
                temp.clip = (AudioClip)Resources.Load(SoundDBModel.Instance.GetSoundPath(item.SoundName)
                                                        , typeof(AudioClip));//调用Resources方法加载AudioClip资源

                if (item.isBg)
                {
                    dicBgSound.Add(type, temp);
                }
                else
                {
                    dicSound.Add(type, temp);
                }
            }
            else
            {
                Debug.LogError("无此音频：" + type);
            }
        }
    }
    #endregion
}
