using UnityEngine;

/// <summary>
/// メインカメラ、サブカメラ、およびマーカー確認用カメラの表示切り替えを管理するクラス
/// </summary>
public class CameraSwitcher : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera subCamera01;
    [SerializeField] private Camera markerCamera; // 追加：ティーショット前のマーカー配下カメラ

    public Camera MainCamera => mainCamera;
    public Camera SubCamera01 => subCamera01;
    public Camera MarkerCamera => markerCamera;

    private void Start()
    {
        InitializeCameras();
    }

    /// <summary>
    /// 初期状態のセットアップ（MainCameraを有効化、他を無効化）
    /// </summary>
    public void InitializeCameras()
    {
        if (mainCamera != null) mainCamera.gameObject.SetActive(true);
        if (subCamera01 != null) subCamera01.gameObject.SetActive(false);
        if (markerCamera != null) markerCamera.gameObject.SetActive(false);
    }

    /// <summary>
    /// ティーショット前のメインカメラとマーカーカメラのトグル切り替え
    /// </summary>
    public void ToggleTeeShotCamera()
    {
        if (mainCamera == null || markerCamera == null) return;

        bool isMainActive = mainCamera.gameObject.activeSelf;

        mainCamera.gameObject.SetActive(!isMainActive);
        markerCamera.gameObject.SetActive(isMainActive);

        if (subCamera01 != null) subCamera01.gameObject.SetActive(false);
    }

    /// <summary>
    /// 強制的にメインカメラ表示に戻す
    /// </summary>
    public void SwitchToMainCamera()
    {
        if (mainCamera != null) mainCamera.gameObject.SetActive(true);
        if (subCamera01 != null) subCamera01.gameObject.SetActive(false);
        if (markerCamera != null) markerCamera.gameObject.SetActive(false);
    }

    /// <summary>
    /// MainCamera / MarkerCamera から SubCamera01 へ表示を切り替える（ショット後用）
    /// </summary>
    public void SwitchToSubCamera01()
    {
        if (mainCamera != null) mainCamera.gameObject.SetActive(false);
        if (markerCamera != null) markerCamera.gameObject.SetActive(false);
        if (subCamera01 != null) subCamera01.gameObject.SetActive(true);
    }

    /// <summary>
    /// 強制的にマーカーカメラ表示にする
    /// </summary>
    public void SwitchToMarkerCamera()
    {
        if (mainCamera != null) mainCamera.gameObject.SetActive(false);
        if (subCamera01 != null) subCamera01.gameObject.SetActive(false);
        if (markerCamera != null) markerCamera.gameObject.SetActive(true);
    }
}