using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class Raymarcher : MonoBehaviour
{
    private Material raymarchMat;

    void Start() {
        raymarchMat = new Material(Shader.Find("Hidden/Raymarch"));
        Debug.Log(this.transform.position.ToString());
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination) {
        Graphics.Blit(source, destination, raymarchMat);
    }
}
