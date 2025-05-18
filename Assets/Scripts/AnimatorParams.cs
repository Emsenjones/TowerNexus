using UnityEngine;

public static class AnimatorParams
{
    // === Trigger 参数 ===
    public static readonly int IsMoving = Animator.StringToHash("IsMoving");
    public static readonly int Die = Animator.StringToHash("Die");
    public static readonly int Attack = Animator.StringToHash("Attack");
    public static readonly int Idle = Animator.StringToHash("Idle");
    public static readonly int IsAttacking = Animator.StringToHash("IsAttacking");

}
