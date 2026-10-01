using UnityEngine;

/// <summary>
/// ショット前の着地予想マーカー付近に配置するカメラ（cameraWrapper）の位置と回転を制御するクラス
/// </summary>
public class TargetMarkerCamera_comp : MonoBehaviour
{
    [Header("References")]
    [Tooltip("参照するBallLauncher_comp")]
    [SerializeField] private BallLauncher_comp ballLauncher;

    [Tooltip("参照する到達地点マーカー")]
    [SerializeField] private GameObject targetMarker;

    [Header("Camera Wrapper Settings")]
    [Tooltip("追加するカメラを子要素とする空オブジェクト")]
    [SerializeField] private GameObject cameraWrapper;

    [Tooltip("targetGroundPosition からの手前方向へのオフセット距離")]
    [SerializeField] private float cameraOffsetFromTarget = 5f;

    [Tooltip("cameraWrapper の高さ（Y座標）オフセット")]
    [SerializeField] private float cameraWrapperYOffset = 2f;

    // --- 【指定された計算用変数】 ---
    private Vector3 cameraGroundPosition;
    private Vector3 targetGroundPosition;
    private Vector3 initShotGroundPosition = Vector3.zero;

    private void Update()
    {
        UpdateWrapperTransform();
    }

    /// <summary>
    /// cameraWrapper の位置と回転を計算して更新します
    /// </summary>
    public void UpdateWrapperTransform()
    {
        if (cameraWrapper == null || targetMarker == null || ballLauncher == null) return;

        // 1. ティーショット時のボールの初期位置から initShotGroundPosition を算出（Y=0）
        Vector3 initialBallPos = ballLauncher.InitialPosition;
        initShotGroundPosition = new Vector3(initialBallPos.x, 0f, initialBallPos.z);

        // 2. マーカーの現在位置から targetGroundPosition を算出（Y=0）
        Vector3 targetPos = targetMarker.transform.position;
        targetGroundPosition = new Vector3(targetPos.x, 0f, targetPos.z);

        // 3. 打球方向（initShotGroundPosition から targetGroundPosition への方向ベクトル）を算出
        Vector3 shotDirection = (targetGroundPosition - initShotGroundPosition).normalized;

        // 方向がゼロベクトルの場合はデフォルト（Z軸正方向）を設定
        if (shotDirection.sqrMagnitude < 0.001f)
        {
            shotDirection = Vector3.forward;
        }

        // 4. targetGroundPosition から打球方向の手前（ボール側）に cameraOffsetFromTarget だけ離れた位置を計算
        cameraGroundPosition = targetGroundPosition - (shotDirection * cameraOffsetFromTarget);

        // 5. Y座標に cameraWrapperYOffset を加算して実際の cameraWrapper の位置を決定
        Vector3 finalWrapperPosition = cameraGroundPosition;
        finalWrapperPosition.y += cameraWrapperYOffset;

        cameraWrapper.transform.position = finalWrapperPosition;

        // 6. cameraWrapper の正面（+Z軸）が打球方向（initShotGroundPosition -> targetGroundPosition）を向くように設定
        cameraWrapper.transform.rotation = Quaternion.LookRotation(shotDirection, Vector3.up);
    }
}