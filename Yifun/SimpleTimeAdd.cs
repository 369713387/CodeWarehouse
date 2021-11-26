using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleTimeAdd : MonoBehaviour
{
    public Material mat;

    public float offset;

    private void Awake()
    {
        offset = 0f;
    }

    void Update()
    {
        offset += 5f * Time.deltaTime;
        mat.SetTextureOffset("_MainTex", new Vector2(offset, 0));
    }
}
