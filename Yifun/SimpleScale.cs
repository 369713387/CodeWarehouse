using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SimpleScale : MonoBehaviour
{
    public float time = 1f;

    public float tar_Scale = 0.5f;

    private void Awake()
    {
        time = GlobalData.perfrectTime;
    }

    private void Start()
    {
        transform.DOScale(tar_Scale, time);
    }
}
