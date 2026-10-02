using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// みんゴル風 3タップ方式ゴルフショットUI制御クラス（RectMask2Dによるパワーゲージ表示対応版）
/// </summary>
public class GolfShotUI : MonoBehaviour
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
    [SerializeField] private RectTransform impactZone;    // インパクトエリア表示用
    [SerializeField] private RectTransform niceShotZone;  // ジャストインパクト（ナイスショット）エリア表示用
    [SerializeField] private RectTransform powerBarMask;  // RectMask2Dがついた親オブジェクトのRectTransform
    [SerializeField] private RectTransform powerBarImage; // Maskの中に配置した子ImageのRectTransform
    [SerializeField] private TextMeshProUGUI statusText;  // 状態表示テキスト

    [Header("Impact Message UI Settings")]
    [Tooltip("NICE SHOT時に表示するGameObject")]
    [SerializeField] private GameObject niceShotMessageObject;

    [Tooltip("GOOD SHOT時に表示するGameObject")]
    [SerializeField] private GameObject goodShotMessageObject;

    [Tooltip("BAD SHOT時に表示するGameObject")]
    [SerializeField] private GameObject badShotMessageObject;

    // --- 【追加】メッセージ自動消去設定 ---
    [Tooltip("インパクトメッセージが表示されてから自動で消えるまでの時間（秒）")]
    [SerializeField] private float messageDisplayDuration = 2.0f;

    [Header("Shot Target Settings")]
    [SerializeField] private BallLauncher ballLauncher;

    [Header("Camera Settings")]
    [SerializeField] private CameraSwitcher cameraSwitcher; // カメラ切り替えコンポーネント参照
    [SerializeField] private float cameraSwitchDelay = 0.5f;     // ショット後、サブカメラに切り替えるまでの遅延時間(秒)

    // パワーランダム減衰設定
    [Header("Power Variation Settings")]
    [Tooltip("NiceShotZone内でのショット時のパワー下限倍率 (0.99 = 99%〜100%のランダム)")]
    [Range(0.5f, 1.0f)]
    [SerializeField] private float niceShotZoneMinPowerRatio = 0.99f;

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
    [SerializeField] private float impactZoneWidth = 0.1f; // Zoneの幅設定

    [Tooltip("ナイスショットゾーンの全体の幅比率 (0.02 の場合、0.89 〜 0.91 がゾーン内)")]
    [Range(0.005f, 0.1f)]
    [SerializeField] private float niceShotZoneWidth = 0.02f; // NiceShotZoneの幅設定

    [Header("Input System Settings")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private string tapActionName = "Launch"; // PlayerInput上のショットアクション名
    [SerializeField] private string resetActionName = "Reset"; // PlayerInput上のリセットアクション名（Rキー等）
    [SerializeField] private string rotateActionName = "Rotate"; // 向き変更アクション名
    [SerializeField] private string switchTeeShotCameraActionName = "SwitchTeeShotCamera"; // カメラ切り替えアクション名（Cキー等）

    [Header("Aim Settings")]
    [Tooltip("左右キー長押し時の向き変更速度（度/秒）")]
    [SerializeField] private float rotateSpeed = 45f;

    private ShotState currentState = ShotState.Ready;
    private float cursorValue = 0.9f;   // 0.0 (左端: MAX) 〜 1.0 (右端)
    private float selectedPower = 0f;  // 決定されたパワー (0.0 〜 1.0)
    private float selectedImpact = 0f; // 決定されたインパクトのズレ
    private float barWidth = 0f;
    private float barHeight = 0f;

    private InputAction tapAction;
    private InputAction resetAction;
    private InputAction rotateAction;
    private InputAction switchTeeShotCameraAction;

    // 連続タップ誤動作防止用の変数
    private float lastTapTime = 0f;
    private const float tapCooldown = 0.15f; // 150ミリ秒以内の連打・連続イベントをガード

    private Coroutine cameraSwitchCoroutine; // コルーチンの二重実行を防ぐためのキャッシュ変数
    private Coroutine hideMessageCoroutine;  // 【追加】メッセージ消去用コルーチン

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

            rotateAction = playerInput.actions.FindAction(rotateActionName);
            if (rotateAction != null)
            {
                rotateAction.Enable();
            }

            switchTeeShotCameraAction = playerInput.actions.FindAction(switchTeeShotCameraActionName);
            if (switchTeeShotCameraAction != null)
            {
                switchTeeShotCameraAction.performed += OnSwitchTeeShotCameraInput;
                switchTeeShotCameraAction.Enable();
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

        if (rotateAction != null)
        {
            rotateAction.Disable();
        }

        if (switchTeeShotCameraAction != null)
        {
            switchTeeShotCameraAction.performed -= OnSwitchTeeShotCameraInput;
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
        // Ready状態の時のみ左右キー操作によるMoveDirectionの変更を受け付ける
        HandleRotationInput();

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

    /// <summary>
    /// 左右キー入力によるBallLauncherのMoveDirection更新処理
    /// </summary>
    private void HandleRotationInput()
    {
        if (currentState != ShotState.Ready) return;
        if (rotateAction == null || ballLauncher == null) return;

        float rotateInput = rotateAction.ReadValue<float>();
        if (Mathf.Abs(rotateInput) > 0.01f)
        {
            ballLauncher.MoveDirection += rotateInput * rotateSpeed * Time.deltaTime;
        }
    }

    /// <summary>
    /// Cキー入力によるティーショット前のカメラ切替処理
    /// </summary>
    private void OnSwitchTeeShotCameraInput(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (currentState == ShotState.Ready && cameraSwitcher != null)
        {
            cameraSwitcher.ToggleTeeShotCamera();
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
                if (cameraSwitcher != null)
                {
                    cameraSwitcher.SwitchToMainCamera();
                }

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

        bool isImpactZone = false;
        float yawRatio = 0f; // -1.0 〜 +1.0
        float powerMultiplier = 1.0f;
        string impactMsg = "";

        // ImpactZoneの範囲計算
        float halfImpactZoneWidth = impactZoneWidth / 2.0f;
        float minImpactZone = impactRatio - halfImpactZoneWidth;
        float maxImpactZone = impactRatio + halfImpactZoneWidth;

        // NiceShotZoneの範囲計算
        float halfNiceZoneWidth = niceShotZoneWidth / 2.0f;
        float minNiceZone = impactRatio - halfNiceZoneWidth;
        float maxNiceZone = impactRatio + halfNiceZoneWidth;

        // --- ショット判定の3分岐処理 ---
        if (cursorValue >= minNiceZone && cursorValue <= maxNiceZone)
        {
            // 1. NiceShotZone内の判定
            isImpactZone = true;
            yawRatio = 0f;
            powerMultiplier = Random.Range(niceShotZoneMinPowerRatio, 1.0f);
            impactMsg = "NICE SHOT!!";
        }
        else if (cursorValue >= minImpactZone && cursorValue <= maxImpactZone)
        {
            // 2. NiceShotZone外だが、ImpactZone内の判定
            isImpactZone = true;
            yawRatio = (cursorValue - impactRatio) / halfImpactZoneWidth;
            powerMultiplier = Random.Range(impactZoneMinPowerRatio, 1.0f);
            impactMsg = "GOOD SHOT";
        }
        else
        {
            // 3. ImpactZone外（ミスショット）の判定
            isImpactZone = false;
            yawRatio = (cursorValue < minImpactZone) ? -1.0f : 1.0f;
            powerMultiplier = Random.Range(missMinPowerRatio, 1.0f);
            impactMsg = "BAD SHOT!";
        }

        // ショット結果に応じたメッセージオブジェクトの表示切り替え（タイマー消去付き）
        ShowImpactMessage(impactMsg);

        float finalPowerRatio = selectedPower * powerMultiplier;

        UpdateStatusText($"{impactMsg} (Power: {Mathf.RoundToInt(finalPowerRatio * 100)}%, YawRatio: {yawRatio:F2}) - Press R to Reset");

        if (ballLauncher != null)
        {
            ballLauncher.LaunchBall(finalPowerRatio, yawRatio, isImpactZone);

            if (cameraSwitchCoroutine != null) StopCoroutine(cameraSwitchCoroutine);
            cameraSwitchCoroutine = StartCoroutine(SwitchCameraDelayed());
        }
    }

    /// <summary>
    /// インパクト判定メッセージオブジェクトを表示し、一定時間後に自動で非表示にする（修正）
    /// </summary>
    private void ShowImpactMessage(string message)
    {
        // 既存の自動消去タイマーがあれば停止
        if (hideMessageCoroutine != null)
        {
            StopCoroutine(hideMessageCoroutine);
        }

        if (niceShotMessageObject != null) niceShotMessageObject.SetActive(message == "NICE SHOT!!");
        if (goodShotMessageObject != null) goodShotMessageObject.SetActive(message == "GOOD SHOT");
        if (badShotMessageObject != null) badShotMessageObject.SetActive(message == "BAD SHOT!");

        // 自動消去コルーチンを開始
        hideMessageCoroutine = StartCoroutine(HideImpactMessageAfterDelay());
    }

    /// <summary>
    /// 【追加】指定秒数経過後にメッセージを自動非表示にするコルーチン
    /// </summary>
    private IEnumerator HideImpactMessageAfterDelay()
    {
        yield return new WaitForSeconds(messageDisplayDuration);
        HideAllImpactMessages();
        hideMessageCoroutine = null;
    }

    /// <summary>
    /// 全てのインパクトメッセージオブジェクトを非表示（修正）
    /// </summary>
    private void HideAllImpactMessages()
    {
        if (hideMessageCoroutine != null)
        {
            StopCoroutine(hideMessageCoroutine);
            hideMessageCoroutine = null;
        }

        if (niceShotMessageObject != null) niceShotMessageObject.SetActive(false);
        if (goodShotMessageObject != null) goodShotMessageObject.SetActive(false);
        if (badShotMessageObject != null) badShotMessageObject.SetActive(false);
    }

    private IEnumerator SwitchCameraDelayed()
    {
        yield return new WaitForSeconds(cameraSwitchDelay);

        //L02_01_02 if文と、外部の関数の実行
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
        if (cameraSwitchCoroutine != null)
        {
            StopCoroutine(cameraSwitchCoroutine);
            cameraSwitchCoroutine = null;
        }

        if (cameraSwitcher != null)
        {
            cameraSwitcher.SwitchToMainCamera();
        }

        ResetUI();

        if (ballLauncher != null)
        {
            ballLauncher.ResetBall();
        }

        // SubCameraのアニメーション状態と回転を初期化
        if (cameraSwitcher != null && cameraSwitcher.SubCamera01 != null)
        {
            var ballFollow = cameraSwitcher.SubCamera01.GetComponent<BallCameraFollow>();
            if (ballFollow != null)
            {
                ballFollow.ResetCameraState();
            }
        }
    }

    public void ResetUI()
    {
        currentState = ShotState.Ready;
        cursorValue = impactRatio;
        selectedPower = 0f;
        selectedImpact = 0f;
        lastTapTime = 0f;

        // インパクトメッセージをすべて非表示化
        HideAllImpactMessages();

        InitializeGaugeDimensions();

        UpdateCursorPosition();
        UpdateImpactZoneGraphic();
        UpdatePowerBarFill();
        UpdateStatusText("Press Space to Start Shot (Press C to Toggle Camera)");
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
        if (gaugeBar == null) return;

        float leftX = -barWidth * gaugeBar.pivot.x;
        float rightX = barWidth * (1f - gaugeBar.pivot.x);
        float xPos = Mathf.Lerp(leftX, rightX, impactRatio);

        if (impactZone != null)
        {
            impactZone.anchoredPosition = new Vector2(xPos, impactZone.anchoredPosition.y);
        }

        if (niceShotZone != null)
        {
            niceShotZone.anchoredPosition = new Vector2(xPos, niceShotZone.anchoredPosition.y);
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