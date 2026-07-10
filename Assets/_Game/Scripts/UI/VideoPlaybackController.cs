using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 全屏视频播放：通关结尾与主菜单致谢。挂于 SceneFlowManager 节点，跨场景常驻。
/// </summary>
[RequireComponent(typeof(VideoPlayer))]
[RequireComponent(typeof(AudioSource))]
public class VideoPlaybackController : MonoBehaviour
{
    private const string DefaultEndingVideoPath = "Assets/_Game/Art/Viedo/结尾.mp4";
    private const string DefaultCreditsVideoPath = "Assets/_Game/Art/Viedo/单独致谢.mp4";

    [Header("视频资源（Build 需在 Inspector 指定，Editor 可自动补引用）")]
    [SerializeField] private VideoClip endingVideo;
    [SerializeField] private VideoClip creditsVideo;

    [Header("UI")]
    [SerializeField] private int canvasSortOrder = 190;

    private VideoPlayer videoPlayer;
    private AudioSource audioSource;
    private SceneTransitionUI transitionUI;

    private GameObject canvasRoot;
    private RawImage videoImage;
    private RenderTexture renderTexture;
    private bool isPlaying;

    public bool IsPlaying => isPlaying;

    public void Configure(VideoClip ending, VideoClip credits)
    {
        if (ending != null)
        {
            endingVideo = ending;
        }

        if (credits != null)
        {
            creditsVideo = credits;
        }
    }

    private void Awake()
    {
#if UNITY_EDITOR
        EnsureVideoReferences();
#endif
        transitionUI = GetComponent<SceneTransitionUI>();
        EnsureVideoComponents();
        EnsureUiBuilt();
        HideImmediate();
    }

    private void OnDestroy()
    {
        ReleaseRenderTexture();
    }

    public IEnumerator PlayEndingAndWait()
    {
        yield return PlayVideoAndWait(endingVideo, "结尾", holdBlackBeforeHide: true);
    }

    public IEnumerator PlayCreditsAndWait()
    {
        yield return PlayVideoAndWait(creditsVideo, "致谢", holdBlackBeforeHide: false);
    }

    private IEnumerator PlayVideoAndWait(VideoClip clip, string label, bool holdBlackBeforeHide)
    {
        if (clip == null)
        {
            Debug.LogWarning($"[VideoPlaybackController] 未配置{label}视频，已跳过播放。");
            yield break;
        }

        if (isPlaying)
        {
            Debug.LogWarning("[VideoPlaybackController] 已有视频正在播放，忽略重复请求。");
            yield break;
        }

        isPlaying = true;
        AudioManager.Instance?.StopMusic();
        transitionUI?.SuppressOverlayForContent();

        EnsureVideoComponents();
        EnsureUiBuilt();
        PrepareRenderTexture(clip);
        ShowOverlay();

        videoPlayer.clip = clip;
        videoPlayer.Prepare();

        while (!videoPlayer.isPrepared)
        {
            yield return null;
        }

        videoPlayer.Play();

        bool finished = false;
        void OnVideoFinished(VideoPlayer source) => finished = true;
        videoPlayer.loopPointReached += OnVideoFinished;

        while (!finished)
        {
            yield return null;
        }

        videoPlayer.loopPointReached -= OnVideoFinished;
        videoPlayer.Stop();
        if (holdBlackBeforeHide)
        {
            transitionUI?.HoldBlack();
            yield return null;
            yield return new WaitForEndOfFrame();
        }

        HideImmediate();
        isPlaying = false;
    }

    private void EnsureVideoComponents()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.SetTargetAudioSource(0, audioSource);
    }

    private void PrepareRenderTexture(VideoClip clip)
    {
        int width = clip.width > 0 ? (int)clip.width : 1920;
        int height = clip.height > 0 ? (int)clip.height : 1080;

        if (renderTexture == null ||
            renderTexture.width != width ||
            renderTexture.height != height)
        {
            ReleaseRenderTexture();
            renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        }

        videoPlayer.targetTexture = renderTexture;

        if (videoImage != null)
        {
            videoImage.texture = renderTexture;
        }
    }

    private void ShowOverlay()
    {
        if (videoImage != null)
        {
            videoImage.color = Color.white;
        }

        if (canvasRoot != null)
        {
            canvasRoot.SetActive(true);
        }
    }

    private void HideImmediate()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        if (canvasRoot != null)
        {
            canvasRoot.SetActive(false);
        }
    }

    private void ReleaseRenderTexture()
    {
        if (renderTexture == null)
        {
            return;
        }

        if (videoPlayer != null)
        {
            videoPlayer.targetTexture = null;
        }

        renderTexture.Release();
        Destroy(renderTexture);
        renderTexture = null;
    }

    private void EnsureUiBuilt()
    {
        if (canvasRoot != null)
        {
            return;
        }

        canvasRoot = new GameObject("VideoPlaybackCanvas");
        canvasRoot.transform.SetParent(transform, false);

        Canvas canvas = canvasRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = canvasSortOrder;

        CanvasScaler scaler = canvasRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasRoot.AddComponent<GraphicRaycaster>();

        CreateStretchPanel(canvasRoot.transform, "Background", Color.black);

        GameObject videoObject = new GameObject("VideoDisplay");
        videoObject.transform.SetParent(canvasRoot.transform, false);
        videoImage = videoObject.AddComponent<RawImage>();
        videoImage.color = Color.white;
        videoImage.raycastTarget = false;

        RectTransform videoRect = videoObject.GetComponent<RectTransform>();
        videoRect.anchorMin = Vector2.zero;
        videoRect.anchorMax = Vector2.one;
        videoRect.offsetMin = Vector2.zero;
        videoRect.offsetMax = Vector2.zero;
    }

    private static GameObject CreateStretchPanel(Transform parent, string name, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        Image image = panel.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return panel;
    }

#if UNITY_EDITOR
    private void EnsureVideoReferences()
    {
        if (endingVideo == null)
        {
            endingVideo = AssetDatabase.LoadAssetAtPath<VideoClip>(DefaultEndingVideoPath);
        }

        if (creditsVideo == null)
        {
            creditsVideo = AssetDatabase.LoadAssetAtPath<VideoClip>(DefaultCreditsVideoPath);
        }
    }

    [ContextMenu("Preview Ending Video")]
    private void PreviewEndingVideo()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[VideoPlaybackController] 请在 Play 模式下预览视频。");
            return;
        }

        StartCoroutine(PlayEndingAndWait());
    }

    [ContextMenu("Preview Credits Video")]
    private void PreviewCreditsVideo()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[VideoPlaybackController] 请在 Play 模式下预览视频。");
            return;
        }

        StartCoroutine(PlayCreditsAndWait());
    }
#endif
}
