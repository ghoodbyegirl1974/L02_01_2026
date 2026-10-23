using UnityEngine;

/// <summary>
/// ゴルフスイングキャラクターのAnimator制御クラス
/// </summary>
public class CharacterAnimation : MonoBehaviour
{
    [Header("Animator Reference")]
    [SerializeField] private Animator characterAnimator;

    // AnimatorのBoolパラメータ名
    private static readonly int IsSwingHash = Animator.StringToHash("IsSwing");

    // 子要素（FBXモデル）の初期ローカルTransform保持用
    private Transform modelTransform;
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;

    private void Awake()
    {
        if (characterAnimator != null)
        {
            modelTransform = characterAnimator.transform;
            initialLocalPosition = modelTransform.localPosition;
            initialLocalRotation = modelTransform.localRotation;
        }
    }

    /// <summary>
    /// Swingアニメーションを再生（IsSwing -> true）
    /// </summary>
    public void PlaySwing()
    {
        if (characterAnimator != null)
        {
            
        }
    }

    /// <summary>
    /// Idleアニメーションへ移行（IsSwing -> false）し、モデルのローカル位置・回転を初期値にリセット
    /// </summary>
    public void PlayIdle()
    {
        if (characterAnimator != null)
        {
            
        }
    }
}