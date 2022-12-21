using UnityEngine;

public class RenderReplacementShaderToTexture : MonoBehaviour
{
    [SerializeField] private GameObject cameraObject;
    [SerializeField] private Shader replacementShader;
    [SerializeField] private RenderTextureFormat renderTextureFormat = RenderTextureFormat.ARGB32;
    [SerializeField] private FilterMode filterMode = FilterMode.Point;
    [SerializeField] private int renderTextureDepth = 24;
    [SerializeField] private CameraClearFlags cameraClearFlags = CameraClearFlags.Color;
    [SerializeField] private Color background = Color.black;
    [SerializeField] private string[] targetTextures;
    [SerializeField] private string targetTag = "RenderToNormal";

    private RenderTexture[] renderTextures;

    internal Camera parentCamera;
    internal Camera childCamera;
    private PostProcessingEffect postProcessingEffect;

    private Material edgeMaterial;
    private Material mergeMaterial;

    internal void Initialize()
    {
        parentCamera = GetComponent<Camera>();
        parentCamera.depthTextureMode = DepthTextureMode.Depth;

        // create a render textures matching the camera's current dimensions
        CreateRenderTextures();

        // set our materials
        postProcessingEffect = parentCamera.GetComponent<PostProcessingEffect>();
        if(postProcessingEffect != null)
        {
            edgeMaterial = postProcessingEffect.lineMaterial;
            mergeMaterial = postProcessingEffect.mergeMaterial;
        }

        // setup a copy of the camera to render the scene using the replacement shader
        childCamera = cameraObject.GetComponent<Camera>();
        childCamera.CopyFrom(parentCamera);
        childCamera.fieldOfView = parentCamera.fieldOfView;
        childCamera.transform.SetParent(transform);
        childCamera.SetReplacementShader(replacementShader, targetTag);
        childCamera.depth = parentCamera.depth - 1;
        childCamera.clearFlags = cameraClearFlags;
        childCamera.backgroundColor = background;
        childCamera.depthTextureMode = DepthTextureMode.Depth;
        childCamera.enabled = false; // disable camera so that we may only manually render it at the right time

        // create and set buffers 
        SetCameraTargetBuffers();

        // update the materials
        SetTexturesForMaterials();
    }

    private void Update()
    {
        // update the materials
        SetTexturesForMaterials();

        // see whether we need to update the texture, do so on change of screen size
        if (renderTextures.Length > 0 && 
            (renderTextures[0].width != parentCamera.pixelWidth || renderTextures[0].height != parentCamera.pixelHeight))
        {
            // create new texture, re-set buffers
            CreateRenderTextures();
            SetCameraTargetBuffers();
        }
    }

    private void CreateRenderTextures()
    {
        renderTextures = new RenderTexture[targetTextures.Length];
        for (int i = 0; i < targetTextures.Length; i++)
        {
            renderTextures[i] = new RenderTexture(parentCamera.pixelWidth, 
                                                  parentCamera.pixelHeight, 
                                                  renderTextureDepth, 
                                                  renderTextureFormat);
            renderTextures[i].filterMode = filterMode;
        }
    }

    private void SetCameraTargetBuffers()
    {
        RenderBuffer[] renderBuffers = new RenderBuffer[renderTextures.Length];
        for (int i = 0; i < renderBuffers.Length; i++)
            renderBuffers[i] = renderTextures[i].colorBuffer;
        if (renderBuffers.Length > 0)
            childCamera.SetTargetBuffers(renderBuffers, renderTextures[0].depthBuffer);
    }

    private void SetTexturesForMaterials()
    {
        for (int i = 0; i < renderTextures.Length; i++)
        {
            edgeMaterial.SetTexture(targetTextures[i], renderTextures[i]);

            if (mergeMaterial != null)
                mergeMaterial.SetTexture(targetTextures[i], renderTextures[i]);
        }
    }
}

