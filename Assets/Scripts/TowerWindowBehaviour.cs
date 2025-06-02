using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class TowerWindowBehaviour : WindowBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI levelText;
    [FormerlySerializedAs("damageText")]
    [SerializeField] TextMeshProUGUI damageCoefficientText;
    [SerializeField] Image iconImage;
    [SerializeField] Button upgradeButton;
    [SerializeField] TextMeshProUGUI upgradeButtonText;
    public class Data
    {
        public readonly string NameString;
        public readonly string LevelString;
        public readonly string Damage;
        public readonly Sprite IconSprite;
        public readonly bool CanUpgrade;
        public readonly string UpgradeButtonString;
        event Action OnUpgrade;
        /// <summary>
        /// The data TowerWindowBehaviour need when it is initialized.
        /// </summary>
        /// <param name="nameString">The name of the tower.</param>
        /// <param name="levelString">The level of the tower.</param>
        /// <param name="iconSprite">The icon of the tower. </param>
        /// <param name="damage">The damage value of the tower.</param>
        /// <param name="canUpgrade">If the tower is upgradable? </param>
        /// <param name="upgradeButtonString">The content of UpgradeButton text.</param>
        /// <param name="onClickUpgradeButton">The click event of upgrade button.</param>
        public Data(
            string nameString, string levelString, Sprite iconSprite, 
            string damage, bool canUpgrade, string upgradeButtonString, Action onClickUpgradeButton)
        {
            NameString = nameString;
            LevelString = levelString;
            IconSprite = iconSprite;
            Damage = damage;
            CanUpgrade = canUpgrade;
            UpgradeButtonString = upgradeButtonString;
            OnUpgrade += onClickUpgradeButton;
        }
        public void InvokeOnUpgrade() => OnUpgrade?.Invoke();
    }
    public override void Initialize(object data = null)
    {
        if (data is Data towerWindowData)
        {
            nameText.text = towerWindowData.NameString;
            levelText.text = towerWindowData.LevelString;
            damageCoefficientText.text = towerWindowData.Damage;
            iconImage.sprite = towerWindowData.IconSprite;
            upgradeButtonText.text = towerWindowData.UpgradeButtonString;
            upgradeButtonText.color = towerWindowData.CanUpgrade ? Color.green : Color.red;
            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(() => { towerWindowData.InvokeOnUpgrade(); });
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }
    }
    void OnDisable()
    {
        closeButton.onClick.RemoveAllListeners();
        upgradeButton.onClick.RemoveAllListeners();
    }
}
