using UnityEngine;

/// <summary>
/// 100%パワー・ブレなし時のボール到達予想位置を計算し、マーカーを表示・追従させるクラス
/// </summary>
public class ShotTrajectoryMarker_comp : MonoBehaviour
{
    [Header("References")]
    [Tooltip("参照するBallLauncher_comp")]
    [SerializeField] private BallLauncher_comp ballLauncher;

    [Tooltip("到達地点に表示するマーカーオブジェクト")]
    [SerializeField] private GameObject targetMarker;

    [Tooltip("追従させるメインカメラ（未設定の場合は Camera.main を自動使用します）")]
    [SerializeField] private Camera mainCamera; // 追加

    [Header("Simulation Settings")]
    [Tooltip("予想軌道を計算する際の最大シミュレーション時間(秒)")]
    [SerializeField] private float maxSimulationTime = 10f;

    [Tooltip("レイキャストによる軌道検証の刻み時間(秒)。小さいほど精度が上がります")]
    [SerializeField] private float timeStep = 0.05f;

    [Tooltip("地面として判定するレイヤー")]
    [SerializeField] private LayerMask groundLayer = ~0;

    [Header("Display Settings")]
    [Tooltip("マーカーを地面から少し浮かせるオフセット(チラつき防止)")]
    [SerializeField] private float surfaceOffset = 0.05f;

    private void Awake()
    {
        // メインカメラがInspectorでセットされていない場合は自動取得（追加）
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        UpdateMarkerPosition();
    }

    /// <summary>
    /// 放物線軌道と地面の交点を計算し、マーカーの位置と回転を更新する
    /// </summary>
    public void UpdateMarkerPosition()
    {
        if (ballLauncher == null || targetMarker == null) return;
        if (ballLauncher.TargetRigidbody == null) return;

        // BallLauncher_comp の公開プロパティからパラメータを取得
        Vector3 startPos = ballLauncher.InitialPosition;
        float pitchAngle = ballLauncher.BasePitchAngle;
        float moveDir = ballLauncher.MoveDirection;
        float maxPower = ballLauncher.MaxLaunchPower;
        float mass = ballLauncher.TargetRigidbody.mass;

        if (mass <= 0f) return;

        // 100%パワー時の初速ベクトルを計算 (ForceMode.Impulse: v0 = Force / Mass)
        float initialSpeed = maxPower / mass;
        Quaternion baseRotation = Quaternion.Euler(0f, moveDir, 0f);
        Quaternion shotOffsetRotation = Quaternion.Euler(-pitchAngle, 0f, 0f);
        Vector3 initialVelocity = (baseRotation * shotOffsetRotation) * Vector3.forward * initialSpeed;

        // 軌道を小刻みにレイキャストして地面との交点を算出
        Vector3 currentPos = startPos;
        Vector3 gravity = Physics.gravity;
        bool hitGround = false;
        RaycastHit groundHit = default;

        for (float t = 0f; t < maxSimulationTime; t += timeStep)
        {
            float nextT = t + timeStep;
            // P(t) = P0 + v0*t + 0.5*g*t^2
            Vector3 nextPos = startPos + initialVelocity * nextT + 0.5f * gravity * (nextT * nextT);

            Vector3 segment = nextPos - currentPos;
            float distance = segment.magnitude;

            if (distance > 0f)
            {
                if (Physics.Raycast(currentPos, segment.normalized, out RaycastHit hit, distance, groundLayer))
                {
                    hitGround = true;
                    groundHit = hit;
                    break;
                }
            }

            currentPos = nextPos;
        }

        // マーカーの表示と位置更新
        if (hitGround)
        {
            if (!targetMarker.activeSelf) targetMarker.SetActive(true);

            // 地面の上に少し浮かせ、傾斜に合わせて向きを調整
            targetMarker.transform.position = groundHit.point + groundHit.normal * surfaceOffset;
            targetMarker.transform.rotation = Quaternion.FromToRotation(Vector3.up, groundHit.normal);

            // メインカメラのTransformをマーカーの方向へ向けさせる（追加）
            if (mainCamera != null)
            {
                mainCamera.transform.LookAt(targetMarker.transform.position);
            }
        }
        else
        {
            // 地面に届かない場合は非表示
            if (targetMarker.activeSelf) targetMarker.SetActive(false);
        }
    }
}