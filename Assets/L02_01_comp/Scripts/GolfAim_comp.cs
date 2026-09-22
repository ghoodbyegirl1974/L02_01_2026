using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ゴルフのショット方向（照準）操作、カメラ回転追随、着地予測マーカーの表示を管理するコンポーネント
/// </summary>
public class GolfAim_comp : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform ballTransform;       // ボール（または発射起点）のTransform
    [SerializeField] private Transform aimPivot;           // 照準の基準となるピボット（カメラやマーカーの親）
    [SerializeField] private GameObject targetMarker;       // 地面に表示するマーカーオブジェクト

    [Header("Aiming Settings")]
    [SerializeField] private float rotateSpeed = 60f;       // 左右キーでの回転速度（度/秒）
    [SerializeField] private LayerMask groundLayer;         // 地面レイヤー（Raycast用）

    [Header("Input System")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private string aimActionName = "Aim";  // Vector2型のアクション（LookまたはMove等）

    private InputAction aimAction;
    private float currentYawAngle = 0f;                     // 現在の照準角度（Y軸回転）
    private bool isAimingEnabled = true;

    public Vector3 CurrentAimDirection => aimPivot != null ? aimPivot.forward : Vector3.forward;

    private void Awake()
    {
        if (playerInput == null) playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        if (playerInput != null && playerInput.actions != null)
        {
            aimAction = playerInput.actions.FindAction(aimActionName);
            if (aimAction != null) aimAction.Enable();
        }
    }

    private void Update()
    {
        if (!isAimingEnabled || ballTransform == null || aimPivot == null) return;

        // ピボットの位置を常にボールに同期
        aimPivot.position = ballTransform.position;

        // 左右入力の取得（PlayerInputのVector2/Axis入力）
        float inputX = 0f;
        if (aimAction != null)
        {
            inputX = aimAction.ReadValue<Vector2>().x;
        }
        else
        {
            // 旧InputFallback（矢印キー / A・Dキー）
            if (Keyboard.current != null)
            {
                if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed) inputX = -1f;
                if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed) inputX = 1f;
            }
        }

        // 照準回転の処理
        if (Mathf.Abs(inputX) > 0.01f)
        {
            currentYawAngle += inputX * rotateSpeed * Time.deltaTime;
            aimPivot.rotation = Quaternion.Euler(0f, currentYawAngle, 0f);
        }
    }

    /// <summary>
    /// 設定された最大パワー・角度に基づき、予測着地地点を計算してマーカーを移動させる
    /// </summary>
    public void UpdateLandingMarker(float maxPower, float pitchAngle)
    {
        if (targetMarker == null || ballTransform == null) return;

        if (!isAimingEnabled)
        {
            targetMarker.SetActive(false);
            return;
        }

        targetMarker.SetActive(true);

        // 物理計算 (初速, 角度, 重力)
        float rad = pitchAngle * Mathf.Deg2Rad;
        float v0 = maxPower;
        float g = Mathf.Abs(Physics.gravity.y);

        // 平地での飛行時間と到達距離の基本計算
        float flyTime = (2f * v0 * Mathf.Sin(rad)) / g;
        float horizontalDistance = v0 * Mathf.Cos(rad) * flyTime;

        // 予測の着地位置（水平移動先）
        Vector3 predictedPos = ballTransform.position + CurrentAimDirection * horizontalDistance;

        // 地形（高低差）に合わせてRaycastで地面にスナップさせる
        Vector3 rayStart = predictedPos + Vector3.up * 50f;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 100f, groundLayer))
        {
            targetMarker.transform.position = hit.point + Vector3.up * 0.02f; // 地面とのちらつき（Z-Fighting）防止
            targetMarker.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * aimPivot.rotation;
        }
        else
        {
            targetMarker.transform.position = predictedPos;
        }
    }

    public void SetAimingEnabled(bool enabled)
    {
        isAimingEnabled = enabled;
        if (targetMarker != null)
        {
            targetMarker.SetActive(enabled);
        }
    }

    public void ResetAim()
    {
        currentYawAngle = 0f;
        if (aimPivot != null)
        {
            aimPivot.rotation = Quaternion.identity;
        }
        SetAimingEnabled(true);
    }
}