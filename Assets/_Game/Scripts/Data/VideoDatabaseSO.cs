using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// 视频资源总表：通关结尾与主菜单致谢。
/// </summary>
[CreateAssetMenu(fileName = "VideoDatabase", menuName = "SO/VideoDatabase")]
public class VideoDatabaseSO : ScriptableObject
{
    [SerializeField] private VideoClip endingVideo;
    [SerializeField] private VideoClip creditsVideo;

    public VideoClip EndingVideo => endingVideo;

    public VideoClip CreditsVideo => creditsVideo;
}
