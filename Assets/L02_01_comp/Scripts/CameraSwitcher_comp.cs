using UnityEngine;

/// <summary>
/// メインカメラとサブカメラの表示切り替えを管理するクラス
/// </summary>
public class CameraSwitcher_comp : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera subCamera01;

    private void Start()
    {
        InitializeCameras();
    }

    /// <summary>
    /// 初期状態のセットアップ（MainCameraを有効化、SubCamera01を無効化）
    /// </summary>
    private void InitializeCameras()
    {
        if (mainCamera != null)
        {
            mainCamera.gameObject.SetActive(true);
        }

        if (subCamera01 != null)
        {
            subCamera01.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// MainCamera から SubCamera01 へ表示を切り替える（外部呼び出し用）
    /// </summary>
    public void SwitchToSubCamera01()
    {
        if (mainCamera != null)
        {
            mainCamera.gameObject.SetActive(false);
        }

        if (subCamera01 != null)
        {
            subCamera01.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// SubCamera01 から MainCamera へ戻す（必要に応じて呼び出し可能）
    /// </summary>
    public void SwitchToMainCamera()
    {
        if (mainCamera != null)
        {
            mainCamera.gameObject.SetActive(true);
        }

        if (subCamera01 != null)
        {
            subCamera01.gameObject.SetActive(false);
        }
    }
}