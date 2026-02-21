using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SurvivalShopUI : MonoBehaviour
{
    [System.Serializable]
    public class WeaponRow
    {
        public string weaponName;
        public TMP_Text nameText;
        public TMP_Text categoryText;
        public TMP_Text costText;
        public Button buySellButton;
        public Button buyAmmoButton;
    }

    public SurvivalController controller;
    public SurvivalPlayerWeaponSystem playerWeaponSystem;
    public List<WeaponRow> rows = new List<WeaponRow>();
    public TMP_Text healCostText;
    public int healCostPerHealth = 2;

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (controller == null)
            controller = FindObjectOfType<SurvivalController>();

        if (playerWeaponSystem == null)
            playerWeaponSystem = FindObjectOfType<SurvivalPlayerWeaponSystem>();

        if (controller == null)
            return;

        IReadOnlyList<SurvivalController.WeaponOffer> offers = controller.GetWeaponOffers();

        foreach (WeaponRow row in rows)
        {
            SurvivalController.WeaponOffer offer = null;
            for (int i = 0; i < offers.Count; i++)
            {
                if (offers[i].weaponName == row.weaponName)
                {
                    offer = offers[i];
                    break;
                }
            }

            if (offer == null)
                continue;

            if (row.nameText != null)
                row.nameText.text = offer.weaponName;

            if (row.categoryText != null)
                row.categoryText.text = offer.category.ToString();

            if (row.costText != null)
                row.costText.text = $"Cost: {offer.cost}";
        }

        if (healCostText != null)
        {
            int missing = Mathf.RoundToInt(controller.MaxHealth - controller.CurrentHealth);
            healCostText.text = $"Heal: {missing * healCostPerHealth}";
        }
    }

    public void BuyWeapon(string weaponName)
    {
        if (controller != null && controller.BuyWeapon(weaponName))
            Refresh();
    }

    public void SellWeapon(string weaponName)
    {
        if (controller != null && controller.SellWeapon(weaponName))
            Refresh();
    }

    public void BuyAmmoForEquippedWeapon()
    {
        if (playerWeaponSystem == null)
            playerWeaponSystem = FindObjectOfType<SurvivalPlayerWeaponSystem>();

        playerWeaponSystem?.BuyAmmoForEquippedWeapon();
        Refresh();
    }

    public void HealPlayer()
    {
        if (controller == null)
            return;

        controller.HealMissingHealth(healCostPerHealth);
        Refresh();
    }

    public void CloseShop()
    {
        controller?.CloseShop();
    }
}
