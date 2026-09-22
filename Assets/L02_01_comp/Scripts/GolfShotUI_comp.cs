using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// みんゴル風 3タップ方式ゴルフショットUI制御クラス（RectMask2Dによるパワーゲージ表示対応版）
/// </summary>
public class GolfShotUI_comp : MonoBehaviour
{
    public enum ShotState
    {
        Ready,          // 発射準備中（1タップ目待ち）
        PowerSelecting, // パワー選択中（左移動: 右側 0.9 ➔ 左端 0.0）
        ImpactSelecting,// インパクト選択中（右移動: 左端 0.0 ➔ 右側 1.0方向）
        Executed        // ショット実行済み
    }

    [Header("UI References")]
    [SerializeField] private RectTransform gaugeBar;      // ゲージ背景のRectTransform
    [SerializeField] private RectTransform gaugeCursor;   // ゲージカーソルのRectTransform
    [SerializeField] private RectTransform impactZone;    // ジャストインパクトエリア表示用
    [SerializeField] private RectTransform powerBarMask;  // ★RectMask2Dがついた親オブジェクトのRectTransform
    [SerializeField] private RectTransform powerBarImage; // ★Maskの中に配置した子ImageのRectTransform
    [SerializeField] private TextMeshProUGUI statusText;  // 状態表示テキスト

    [Header("Shot Target Settings")]
    [SerializeField] private BallLauncher_comp ballLauncher;
    [SerializeField] private float maxLaunchPower = 150f; // 最大パワー
    [SerializeField] private float basePitchAngle = 30f;  // 基本打ち出し角

    [Header("Camera Settings")]
    [SerializeField] private CameraSwitcher_comp cameraSwitcher; // ★カメラ切り替えコンポーネント参照
    [SerializeField] private float cameraSwitchDelay = 0.5f;     // ★ショット後、サブカメラに切り替えるまでの遅延時間(秒)

    // ★ 角度ブレ設定（Zone内とZone外で分離）
    [Tooltip("ImpactZone内の端ギリギリで発生する最大ブレ角度（度）")]
    [SerializeField] private float maxZoneYawAngle = 10f; 

    [Tooltip("ImpactZoneを完全に外した（ミス）際の最大左右ズレ角度（度）")]
    [SerializeField] private float maxMissYawAngle = 25f; 

    // ★ パワーランダム減衰設定
    [Header("Power Variation Settings")]
    [Tooltip("ImpactZone内でのショット時のパワー下限倍率 (0.97 = 97%〜100%のランダム)")]
    [Range(0.5f, 1.0f)]
    [SerializeField] private float impactZoneMinPowerRatio = 0.97f;

    [Tooltip("ImpactZone外（ミス）でのショット時のパワー下限倍率 (0.85 = 85%〜100%のランダム)")]
    [Range(0.1f, 1.0f)]
    [SerializeField] private float missMinPowerRatio = 0.85f;

    [Header("Gauge Settings")]
    [SerializeField] private float gaugeSpeed = 2.0f;     // ゲージ移動速度

    [Header("Impact Zone Settings")]
    [Tooltip("ジャストインパクトおよび初期開始位置の比率 (0.9 = 右から10%の位置)")]
    [Range(0.7f, 0.95f)]
    [SerializeField] private float impactRatio = 0.9f;    // 0.9 の位置

    [Tooltip("インパクトゾーンの全体の幅比率 (0.1 の場合、0.85 〜 0.95 がゾーン内)")]
    [Range(0.01f, 0.3f)]
    [SerializeField] private float impactZoneWidth = 0.1f; // ★Zoneの幅設定を追加

    [Header("Input System Settings")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private string tapActionName = "Launch"; // PlayerInput上のショットアクション名
    [SerializeField] private string resetActionName = "Reset"; // PlayerInput上のリセットアクション名（Rキー等）

    private ShotState currentState = ShotState.Ready;
    private float cursorValue = 0.9f;   // 0.0 (左端: MAX) 〜 1.0 (右端)
    private float selectedPower = 0f;  // 決定されたパワー (0.0 〜 1.0)
    private float selectedImpact = 0f; // 決定されたインパクトのズレ
    private float barWidth = 0f;
    private float barHeight = 0f;

    private InputAction tapAction;
    private InputAction resetAction;

    // 連続タップ誤動作防止用の変数
    private float lastTapTime = 0f;
    private const float tapCooldown = 0.15f; // 150ミリ秒以内の連打・連続イベントをガード

    private Coroutine cameraSwitchCoroutine; // ★コルーチンの二重実行を防ぐためのキャッシュ変数

    private void Awake()
    {
        if (playerInput == null) playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        if (playerInput != null && playerInput.actions != null)
        {
            tapAction = playerInput.actions.FindAction(tapActionName);
            if (tapAction != null)
            {
                tapAction.performed += OnTapInput;
                tapAction.Enable();
            }

            resetAction = playerInput.actions.FindAction(resetActionName);
            if (resetAction != null)
            {
                resetAction.performed += OnResetInput;
                resetAction.Enable();
            }
        }
    }

    private void OnDisable()
    {
        if (tapAction != null)
        {
            tapAction.performed -= OnTapInput;
        }

        if (resetAction != null)
        {
            resetAction.performed -= OnResetInput;
        }
    }

    private void Start()
    {
        InitializeGaugeDimensions();
        ResetAll();
    }

    private void InitializeGaugeDimensions()
    {
        if (gaugeBar != null)
        {
            barWidth = gaugeBar.rect.width;
            barHeight = gaugeBar.rect.height;
            SetupPowerBarTransform();
        }
    }

    /// <summary>
    /// powerBarMask と powerBarImage のサイズ・位置関係を初期セットアップ
    /// </summary>
    private void SetupPowerBarTransform()
    {
        if (powerBarMask == null || gaugeBar == null) return;

        // --- 1. 親（powerBarMask）の設定 ---
        powerBarMask.pivot = new Vector2(1.0f, 0.5f);
        powerBarMask.anchorMin = new Vector2(0.5f, 0.5f);
        powerBarMask.anchorMax = new Vector2(0.5f, 0.5f);

        float leftX = -barWidth * gaugeBar.pivot.x;
        float rightX = barWidth * (1f - gaugeBar.pivot.x);
        float impactXPos = Mathf.Lerp(leftX, rightX, impactRatio);

        powerBarMask.anchoredPosition = new Vector2(impactXPos, 0f);

        // --- 2. 子（powerBarImage）の設定 ---
        if (powerBarImage != null)
        {
            float maxPowerBarWidth = barWidth * impactRatio;
            powerBarImage.sizeDelta = new Vector2(maxPowerBarWidth, barHeight);

            powerBarImage.pivot = new Vector2(1.0f, 0.5f);
            powerBarImage.anchorMin = new Vector2(1.0f, 0.5f);
            powerBarImage.anchorMax = new Vector2(1.0f, 0.5f);
            powerBarImage.anchoredPosition = Vector2.zero;
        }
    }

    private void Update()
    {
        switch (currentState)
        {
            case ShotState.PowerSelecting:
                cursorValue -= Time.deltaTime * gaugeSpeed;
                if (cursorValue <= 0.0f)
                {
                    cursorValue = 0.0f;
                    selectedPower = 1.0f;
                    currentState = ShotState.ImpactSelecting;
                    lastTapTime = Time.time;
                }
                UpdateCursorPosition();
                UpdatePowerBarFill();
                break;

            case ShotState.ImpactSelecting:
                cursorValue += Time.deltaTime * gaugeSpeed;
                if (cursorValue >= 1.0f)
                {
                    cursorValue = 1.0f;
                    ExecuteShotWithMiss();
                }
                UpdateCursorPosition();
                break;
        }
    }

    public void OnTapInput(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (Time.time - lastTapTime < tapCooldown) return;
        lastTapTime = Time.time;

        switch (currentState)
        {
            case ShotState.Ready:
                currentState = ShotState.PowerSelecting;
                cursorValue = impactRatio;
                UpdateStatusText("Power Selecting... Press Space to Lock Power!");
                break;

            case ShotState.PowerSelecting:
                selectedPower = Mathf.Clamp01((impactRatio - cursorValue) / impactRatio);
                currentState = ShotState.ImpactSelecting;
                UpdatePowerBarFill();
                UpdateStatusText($"Power: {Mathf.RoundToInt(selectedPower * 100)}% | Select Impact!");
                break;

            case ShotState.ImpactSelecting:
                ExecuteShot();
                break;
        }
    }

    private void OnResetInput(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        ResetAll();
    }

    private void ExecuteShot()
    {
        currentState = ShotState.Executed;

        float yawOffset = 0f;
        float powerMultiplier = 1.0f;

        float halfZoneWidth = impactZoneWidth / 2.0f;
        float minZone = impactRatio - halfZoneWidth;
        float maxZone = impactRatio + halfZoneWidth;

        if (cursorValue >= minZone && cursorValue <= maxZone)
        {
            float normalizedDistance = (cursorValue - impactRatio) / halfZoneWidth;
            yawOffset = normalizedDistance * maxZoneYawAngle;
            powerMultiplier = Random.Range(impactZoneMinPowerRatio, 1.0f);
        }
        else
        {
            if (cursorValue < minZone)
            {
                yawOffset = -maxMissYawAngle;
            }
            else
            {
                yawOffset = maxMissYawAngle;
            }

            powerMultiplier = Random.Range(missMinPowerRatio, 1.0f);
        }

        float finalPower = selectedPower * powerMultiplier * maxLaunchPower;

        string impactMsg = "NICE SHOT!!";
        if (Mathf.Abs(yawOffset) >= maxMissYawAngle) impactMsg = "BAD SHOT!";
        else if (Mathf.Abs(yawOffset) > 0f) impactMsg = "GOOD SHOT";

        UpdateStatusText($"{impactMsg} (Power: {Mathf.RoundToInt(selectedPower * powerMultiplier * 100)}%, Yaw: {yawOffset:F1}°) - Press R to Reset");

        if (ballLauncher != null)
        {
            ballLauncher.SetShotParameters(finalPower, basePitchAngle, yawOffset);
            ballLauncher.LaunchBall();

            // ★ 一定時間後にサブカメラへ切り替えるコルーチンを開始
            if (cameraSwitchCoroutine != null) StopCoroutine(cameraSwitchCoroutine);
            cameraSwitchCoroutine = StartCoroutine(SwitchCameraDelayed());
        }
    }

    /// <summary>
    /// ★ 指定された時間（cameraSwitchDelay秒）待ってからSubCamera01へ切り替えるコルーチン
    /// </summary>
    private IEnumerator SwitchCameraDelayed()
    {
        yield return new WaitForSeconds(cameraSwitchDelay);

        if (cameraSwitcher != null)
        {
            cameraSwitcher.SwitchToSubCamera01();
        }
    }

    private void ExecuteShotWithMiss()
    {
        ExecuteShot();
    }

    /// <summary>
    /// UIおよびボールの双方を完全初期化（Rキー押下時・スタート時共通）
    /// </summary>
    [ContextMenu("Reset All")]
    public void ResetAll()
    {
        // 1. カメラタイマーの停止＆メインカメラへの復帰
        if (cameraSwitchCoroutine != null)
        {
            StopCoroutine(cameraSwitchCoroutine);
            cameraSwitchCoroutine = null;
        }

        if (cameraSwitcher != null)
        {
            cameraSwitcher.SwitchToMainCamera();
        }

        // 2. UIの初期化
        ResetUI();

        // 3. ボール位置・物理・距離計測の初期化
        if (ballLauncher != null)
        {
            ballLauncher.ResetBall();
        }
    }

    public void ResetUI()
    {
        currentState = ShotState.Ready;
        cursorValue = impactRatio;
        selectedPower = 0f;
        selectedImpact = 0f;
        lastTapTime = 0f;

        InitializeGaugeDimensions();

        UpdateCursorPosition();
        UpdateImpactZoneGraphic();
        UpdatePowerBarFill();
        UpdateStatusText("Press Space to Start Shot");
    }

    private void UpdateCursorPosition()
    {
        if (gaugeCursor != null && gaugeBar != null)
        {
            float leftX = -barWidth * gaugeBar.pivot.x;
            float rightX = barWidth * (1f - gaugeBar.pivot.x);

            float xPos = Mathf.Lerp(leftX, rightX, cursorValue);
            gaugeCursor.anchoredPosition = new Vector2(xPos, gaugeCursor.anchoredPosition.y);
        }
    }

    private void UpdatePowerBarFill()
    {
        if (powerBarMask == null) return;

        float powerRatio = 0f;

        if (currentState == ShotState.PowerSelecting)
        {
            powerRatio = Mathf.Clamp01((impactRatio - cursorValue) / impactRatio);
        }
        else if (currentState == ShotState.ImpactSelecting || currentState == ShotState.Executed)
        {
            powerRatio = selectedPower;
        }

        float maxPowerBarWidth = barWidth * impactRatio;
        powerBarMask.sizeDelta = new Vector2(maxPowerBarWidth * powerRatio, barHeight);
    }

    private void UpdateImpactZoneGraphic()
    {
        if (impactZone != null && gaugeBar != null)
        {
            float leftX = -barWidth * gaugeBar.pivot.x;
            float rightX = barWidth * (1f - gaugeBar.pivot.x);

            float xPos = Mathf.Lerp(leftX, rightX, impactRatio);
            impactZone.anchoredPosition = new Vector2(xPos, impactZone.anchoredPosition.y);
        }
    }

    private void UpdateStatusText(string msg)
    {
        if (statusText != null)
        {
            statusText.text = msg;
        }
    }
}