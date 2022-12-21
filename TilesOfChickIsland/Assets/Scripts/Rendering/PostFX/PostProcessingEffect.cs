using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class PostProcessingEffect : MonoBehaviour
{
    public Material lineMaterial;
    public Material mergeMaterial;

    private new Camera camera;

    private RenderReplacementShaderToTexture replacementCamera;
    private RenderTexture edgeRT;

    private bool initialized = false;

    internal void Initialize()
    {
        camera = GetComponent<Camera>();

        //lineMaterial = new Material(lineMaterial);
        //mergeMaterial = new Material(mergeMaterial);

        replacementCamera = GetComponent<RenderReplacementShaderToTexture>();
        replacementCamera.Initialize();

        initialized = true;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!initialized)
            return;

        // get info from source render texture
        int width = source.width;
        int height = source.height;
        RenderTextureFormat format = source.format;

        // set shader variables edge material
        Matrix4x4 clipToView = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true).inverse;
        lineMaterial.SetMatrix("_ClipToView", clipToView);
        lineMaterial.SetFloat("_Width", width);
        lineMaterial.SetFloat("_Height", height);
        lineMaterial.SetFloat("_SampleDistance", (float)Math.Round((double)(height / 1080) * 2, MidpointRounding.AwayFromZero) * 0.5f);

        // force update replacement cam at the right time
        ShadowQuality shadowQuality = QualitySettings.shadows;
        QualitySettings.shadows = ShadowQuality.Disable;
        replacementCamera.childCamera.Render();
        QualitySettings.shadows = shadowQuality;

        // get edges in a RT
        edgeRT = RenderTexture.GetTemporary(width, height, 0, format);
        Graphics.Blit(source, edgeRT, lineMaterial);

        // set shader variables merge material
        mergeMaterial.SetTexture("_EdgeTex", edgeRT);
        mergeMaterial.SetFloat("_Width", width);
        mergeMaterial.SetFloat("_Height", height);

        // merge the origin and the edges
        Graphics.Blit(source, destination, mergeMaterial);

        // release the edge RT
        RenderTexture.ReleaseTemporary(edgeRT);
    }
}
