using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 【授業解説用】3タップ方式ゴルフショットの核心ロジック抽出クラス
/// </summary>
public class GolfShotUI_test_comp : MonoBehaviour
{
    // --- 1. ステート定義 ---
    public enum ShotState
    {
        Ready,          // 1タップ目待ち（初期状態）
        PowerSelecting, // 2タップ目待ち（パワー選択中：右→左へ移動）
        ImpactSelecting,// 3タップ目待ち（インパクト選択中：左→右へ移動）
        Executed        // ショット実行完了
    }

    [Header("UI Reference")]
    [SerializeField] private RectTransform gaugeBar;    // ゲージ背景のRectTransform
    [SerializeField] private RectTransform gaugeCursor; // 移動するカーソルのRectTransform

    [Header("Gauge Settings")]
    [SerializeField] private float gaugeSpeed = 2.0f;   // カーソルの移動速度
    [Range(0.7f, 0.95f)]
    [SerializeField] private float impactRatio = 0.9f;  // 初期位置およびジャストインパクトの位置（右から10%付近）
    [SerializeField] private float impactZoneWidth = 0.1f; // インパクトゾーンの幅

    [Header("Input Settings")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private string tapActionName = "Launch"; // スペースキーなどのアクション名

    // 内部状態管理変数
    private ShotState currentState = ShotState.Ready;
    private float cursorValue = 0.9f;  // カーソル位置（0.0:左端 MAX ～ 1.0:右端）
    private float selectedPower = 0f;  // 決定されたパワー（0.0 ～ 1.0）
    private float barWidth = 0f;
    private InputAction tapAction;

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
                // スペースキー押下時にOnTapInputを呼び出すイベント登録
                tapAction.performed += OnTapInput;
                tapAction.Enable();
            }
        }
    }

    private void OnDisable()
    {
        if (tapAction != null)
        {
            tapAction.performed -= OnTapInput;
        }
    }

    private void Start()
    {
        if (gaugeBar != null)
        {
            barWidth = gaugeBar.rect.width;
        }
        ResetUI();
    }

    // =========================================================================
    // 【解説ポイント1】スペースキーを押すごとにステートが切り替わる処理
    // =========================================================================
    public void OnTapInput(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        switch (currentState)
        {
            case ShotState.Ready:
                // 【1タップ目】パワー選択開始（カーソルを初期位置にセットし移動開始）
                currentState = ShotState.PowerSelecting;
                cursorValue = impactRatio;
                Debug.Log("【1タップ目】パワー選択開始");
                break;

            case ShotState.PowerSelecting:
                // 【2タップ目】パワー決定 ＆ インパクト選択へ移行
                // cursorValue（1.0〜0.0）から決定したパワーの割合（0.0〜1.0）を算出
                selectedPower = Mathf.Clamp01((impactRatio - cursorValue) / impactRatio);
                currentState = ShotState.ImpactSelecting;
                Debug.Log($"【2タップ目】パワー決定: {Mathf.RoundToInt(selectedPower * 100)}%");
                break;

            case ShotState.ImpactSelecting:
                // 【3タップ目】インパクト決定 ＆ ショット実行
                ExecuteShot();
                break;
        }
    }

    // =========================================================================
    // 【解説ポイント2】ステートに応じたcursorValueの更新とカーソルUI移動処理
    // =========================================================================
    private void Update()
    {
        switch (currentState)
        {
            case ShotState.PowerSelecting:
                // パワー選択中：左方向へ移動（1.0 ➔ 0.0）
                cursorValue -= Time.deltaTime * gaugeSpeed;
                if (cursorValue <= 0.0f)
                {
                    // 左端に到達したら自動的に最大パワー(100%)でインパクト選択へ強制移行
                    cursorValue = 0.0f;
                    selectedPower = 1.0f;
                    currentState = ShotState.ImpactSelecting;
                }
                UpdateCursorPosition();
                break;

            case ShotState.ImpactSelecting:
                // インパクト選択中：右方向へ移動（0.0 ➔ 1.0）
                cursorValue += Time.deltaTime * gaugeSpeed;
                if (cursorValue >= 1.0f)
                {
                    // 右端まで振り切ったら振り空振り（ミスショット）として自動実行
                    cursorValue = 1.0f;
                    ExecuteShot();
                }
                UpdateCursorPosition();
                break;
        }
    }

    /// <summary>
    /// cursorValue (0.0〜1.0) を RectTransform の Local Position X 座標に変換して反映する
    /// </summary>
    private void UpdateCursorPosition()
    {
        if (gaugeCursor != null && gaugeBar != null)
        {
            // ゲージの左端と右端のX座標をPivot考慮で算出
            float leftX = -barWidth * gaugeBar.pivot.x;
            float rightX = barWidth * (1f - gaugeBar.pivot.x);

            // 0.0〜1.0の割合（cursorValue）を実際の座標（X値）に線形補間（Lerp）で変換
            float xPos = Mathf.Lerp(leftX, rightX, cursorValue);
            gaugeCursor.anchoredPosition = new Vector2(xPos, gaugeCursor.anchoredPosition.y);
        }
    }

    // =========================================================================
    // 【解説ポイント3】打球の強さ（Power）と左右の角度変化量（YawRatio）を取得する処理
    // =========================================================================
    private void ExecuteShot()
    {
        currentState = ShotState.Executed;

        // 1. 決定された「打球の強さ(割合)」
        float finalPowerRatio = selectedPower;

        // 2. 狙い目（impactRatio）からのズレに基づいて「左右の角度変化量」を計算（-1.0 ～ +1.0）
        float halfImpactZoneWidth = impactZoneWidth / 2.0f;
        float minImpactZone = impactRatio - halfImpactZoneWidth;
        float maxImpactZone = impactRatio + halfImpactZoneWidth;

        float yawRatio = 0f;
        bool isSuccess = false;

        if (cursorValue >= minImpactZone && cursorValue <= maxImpactZone)
        {
            // ゾーン内（成功）：中央からの距離に応じた精密な横ブレ（-1.0:左ズレ, 0:ジャスト, +1.0:右ズレ）
            isSuccess = true;
            yawRatio = (cursorValue - impactRatio) / halfImpactZoneWidth;
        }
        else
        {
            // ゾーン外（ミス）：大きく左または右に大きくブレる
            isSuccess = false;
            yawRatio = (cursorValue < minImpactZone) ? -1.0f : 1.0f;
        }

        // --- 取得した値の出力確認 ---
        Debug.Log($"【ショット実行】\n" +
                  $"・最終パワー: {Mathf.RoundToInt(finalPowerRatio * 100)}%\n" +
                  $"・左右ブレ率(YawRatio): {yawRatio:F2} (範囲: -1.0〜+1.0)\n" +
                  $"・ジャスト成功か: {isSuccess}");
    }

    /// <summary>
    /// ステートとUIを初期状態に戻す
    /// </summary>
    public void ResetUI()
    {
        currentState = ShotState.Ready;
        cursorValue = impactRatio;
        selectedPower = 0f;
        UpdateCursorPosition();
    }
}