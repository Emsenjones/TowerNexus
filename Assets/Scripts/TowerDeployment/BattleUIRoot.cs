using UnityEngine;

public class BattleUIRoot : MonoBehaviour
{
    [SerializeField] private BattleHUDUI battleHUDUI;
    [SerializeField] private MonsterStatusUIManager monsterStatusUIManager;
    [SerializeField] private DamageNumberManager damageNumberManager;

    private void Awake()
    {
        ValidateComposition();
    }

    private void ValidateComposition()
    {
        if (battleHUDUI == null)
        {
            Debug.LogError("Battle UI Root is missing its BattleHUDUI reference.", this);
        }

        if (monsterStatusUIManager == null)
        {
            Debug.LogError("Battle UI Root is missing its MonsterStatusUIManager reference.", this);
        }

        if (damageNumberManager == null)
        {
            Debug.LogError("Battle UI Root is missing its DamageNumberManager reference.", this);
        }
    }
}
