using UnityEngine;

/// <summary>
/// アタッチされたオブジェクト（MainCamera等）が、インスペクタで指定したターゲットを注視し続けるコンポーネント
/// </summary>
public class LookAtTarget_comp : MonoBehaviour
{
    [Header("注視対象のオブジェクト")]
    [SerializeField] private GameObject targetObject;

    void Update()
    {
        // ターゲットが設定されていない場合は処理を行わない
        if (targetObject == null) return;

        // ターゲットの方向を向く
        transform.LookAt(targetObject.transform);
    }
}