using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// ボール（ターゲット）を一定のオフセットで追随するサブカメラ制御クラス
/// （完全停止後のカメラ回転・移動演出機能を追加拡張）
/// </summary>
public class BallCameraFollow : MonoBehaviour
{
    [Header("Follow Settings")]
    [Tooltip("追随対象のオブジェクト（ボールなど）")]
    [SerializeField] private GameObject target;

    [Tooltip("ターゲットからの相対位置（カメラの位置調整）")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 3f, -5f);

    // --- 【追加】移動演出設定項目 ---
    [Header("Camera Transition Settings")]
    [Tooltip("ボール完全停止後、カメラが移動を開始するまでの待機時間(秒)")]
    [SerializeField] private float delayBeforeCameraMove = 1.0f;

    [Tooltip("カメラが目標位置へ到達するまでの移動時間(秒)")]
    [SerializeField] private float cameraMoveDuration = 2.0f;

    [Tooltip("移動完了後のボールからの距離")]
    [SerializeField] private float distanceFromBall = 5.0f;

    [Tooltip("移動完了後のカメラの高さ（Y軸オフセット）")]
    [SerializeField] private float cameraHeightOffset = 2.0f;

    // --- 【内部状態管理変数】 ---
    private bool isAnimating = false; // アニメーション移動中かどうか
    private Coroutine transitionCoroutine;

    // 初期状態の保持変数
    private Vector3 initialOffset;
    private Quaternion initialRotation;

    private void Awake()
    {
        // インスペクターで設定された初期のoffsetおよび配置時の回転角を保持
        initialOffset = offset;
        initialRotation = transform.rotation;
    }

    private void LateUpdate()
    {
        // アニメーション移動中以外のみ、追随処理を実行
        //L02_01_03 if文と、追随処理。
    }

    /// <summary>
    /// ボール完全停止後に呼ばれる演出移動シーケンスを開始する
    /// </summary>
    /// <param name="ballPos">ボールの位置</param>
    /// <param name="targetCupPos">カップ（ターゲット）の位置</param>
    /// <param name="onComplete">カメラ移動完了時に実行されるアクション（UI表示など）</param>
    public void StartCameraMoveSequence(Vector3 ballPos, Vector3 targetCupPos, Action onComplete)
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
        transitionCoroutine = StartCoroutine(AnimateCameraSequence(ballPos, targetCupPos, onComplete));
    }

    /// <summary>
    /// カメラ移動演出のアニメーションコルーチン
    /// </summary>
    private IEnumerator AnimateCameraSequence(Vector3 ballPos, Vector3 targetCupPos, Action onComplete)
    {
        isAnimating = true;

        // 1. 指定時間の待機
        yield return new WaitForSeconds(delayBeforeCameraMove);

        // 2. 移動先の目標座標を計算
        Vector3 dirFromCupToBall = (ballPos - targetCupPos);
        dirFromCupToBall.y = 0f; // 水平方向のみ抽出

        if (dirFromCupToBall.sqrMagnitude < 0.001f)
        {
            dirFromCupToBall = Vector3.forward;
        }
        else
        {
            dirFromCupToBall.Normalize();
        }

        // ボールを挟んでカップと反対側の位置 = ボール位置 + 方向ベクトル * 距離
        Vector3 targetCameraPos = ballPos + dirFromCupToBall * distanceFromBall;
        targetCameraPos.y = ballPos.y + cameraHeightOffset;

        // 初期移動開始時点のカメラ位置と回転
        Vector3 startCameraPos = transform.position;
        Quaternion startCameraRot = transform.rotation;

        float elapsedTime = 0f;

        // 3. 移動＆回転アニメーション
        while (elapsedTime < cameraMoveDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / cameraMoveDuration);

            // スムーズなイージング（SmootherStep）を適用
            float smoothT = t * t * (3f - 2f * t);

            // 位置補間
            transform.position = Vector3.Lerp(startCameraPos, targetCameraPos, smoothT);

            // カメラがターゲット（カップ方向）を向く回転計算
            Vector3 lookTarget = targetCupPos;
            lookTarget.y = ballPos.y; // 視線が高くなりすぎないよう調整
            Quaternion targetRotation = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);

            // 回転補間
            transform.rotation = Quaternion.Slerp(startCameraRot, targetRotation, smoothT);

            yield return null;
        }

        // 最終位置・回転の確定
        transform.position = targetCameraPos;
        Vector3 finalLookTarget = targetCupPos;
        finalLookTarget.y = ballPos.y;
        transform.rotation = Quaternion.LookRotation(finalLookTarget - transform.position, Vector3.up);

        // --- 【重要修正】追随再開時にカメラ位置が維持されるよう、現在の位置からoffsetを再計算 ---
        if (target != null)
        {
            offset = transform.position - target.transform.position;
        }

        isAnimating = false;

        // 4. 移動完了コールバックの実行（距離UI表示などを起動）
        onComplete?.Invoke();
    }

    /// <summary>
    /// カメラの状態を初期位置・回転にリセットする（リセットボタン押下時など）
    /// </summary>
    public void ResetCameraState()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }
        isAnimating = false;

        // --- 【重要修正】Quaternion.identityではなく、保持した初期回転・offsetにリセット ---
        offset = initialOffset;
        transform.rotation = initialRotation;
    }
}