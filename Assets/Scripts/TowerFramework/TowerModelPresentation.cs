using UnityEngine;

public class TowerModelPresentation : MonoBehaviour
{
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private Animator animator;

    private TowerCombatBehaviour towerCombatBehaviour;
    private bool hasLoggedMissingAnimator;
    private bool hasLoggedMissingTowerCombat;

    public Animator Animator => animator;
    public Transform AttackOrigin => attackOrigin;

    private void Awake()
    {
        CacheReferences();
    }

    public void Initialize(TowerCombatBehaviour towerCombatBehaviour)
    {
        this.towerCombatBehaviour = towerCombatBehaviour;
        CacheReferences();
    }

    public bool RequestAttackTrigger(string triggerName)
    {
        if (string.IsNullOrEmpty(triggerName))
        {
            return false;
        }

        if (!TryGetAnimator(out Animator targetAnimator))
        {
            return false;
        }

        targetAnimator.SetTrigger(triggerName);
        return true;
    }

    public bool RequestAttackingBool(string boolName, bool isAttacking)
    {
        if (string.IsNullOrEmpty(boolName))
        {
            return false;
        }

        if (!TryGetAnimator(out Animator targetAnimator))
        {
            return false;
        }

        targetAnimator.SetBool(boolName, isAttacking);
        return true;
    }

    public void OnAttackAnimationRelease()
    {
        if (towerCombatBehaviour == null)
        {
            towerCombatBehaviour = GetComponentInParent<TowerCombatBehaviour>();
        }

        if (towerCombatBehaviour != null)
        {
            towerCombatBehaviour.OnAttackAnimationRelease();
            return;
        }

        if (!hasLoggedMissingTowerCombat)
        {
            Debug.LogWarning("Tower model presentation cannot forward attack animation release: TowerCombatBehaviour is missing in parent hierarchy.", this);
            hasLoggedMissingTowerCombat = true;
        }
    }

    private bool TryGetAnimator(out Animator targetAnimator)
    {
        CacheReferences();
        targetAnimator = animator;

        if (targetAnimator != null)
        {
            return true;
        }

        if (!hasLoggedMissingAnimator)
        {
            Debug.LogWarning("Tower model presentation cannot play attack animation: Animator is missing.", this);
            hasLoggedMissingAnimator = true;
        }

        return false;
    }

    private void CacheReferences()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (attackOrigin == null)
        {
            attackOrigin = transform.Find("AttackOrigin");
        }
    }
}
