using UnityEngine;
using Photon.Pun;
using TMPro;

public class MovingTroop : Entity
{

#region Variables

    TMP_Text damageText;
    TMP_Text heartText;

    int _currentHealth;
    public int currentHealth
    {
        get { return _currentHealth; }
        protected set { _currentHealth = value; try { heartText.text = _currentHealth.ToString(); } catch { } }
    }
    int _currentDamage;
    public int currentDamage
    {
        get { return _currentDamage; }
        protected set { _currentDamage = value; try { damageText.text = _currentDamage.ToString(); } catch { } }
    }

    #endregion

#region Setup

    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        heartText = this.transform.Find("Heart Text").GetComponent<TMP_Text>();
        damageText = this.transform.Find("Damage Text").GetComponent<TMP_Text>();
    }

    [PunRPC]
    internal void AssignCardInfo(bool undo, int playerPosition, int cardID)
    {
        if (!undo)
        {
            this.player = Manager.inst.playersInOrder[playerPosition];
            this.image.transform.localScale = new(playerPosition == 0 ? 1 : -1, 1, 1);
            if (border != null)
                border.color = this.player.playerPosition == 0 ? Color.white : Color.black;

            if (cardID >= 0)
            {
                myCard = PhotonView.Find(cardID).GetComponent<TroopCard>();
                this.name = myCard.name;
                this.image.sprite = Resources.Load<Sprite>($"Card Art/{this.name}");

                TroopCard intoTroop = (TroopCard)myCard;
                this.currentHealth = intoTroop.health;
                this.currentDamage = intoTroop.damage;
            }
        }
    }

    #endregion

#region Gameplay

    [PunRPC]
    internal void MoveTroop(bool undo, int oldPosition, int newPosition, int logged)
    {
        if (undo)
        {
            if (newPosition > -1)
                Manager.inst.allRows[newPosition].playerTroops[player.playerPosition] = null;

            this.currentRow = oldPosition;
        }
        else
        {
            if (oldPosition > -1)
                Manager.inst.allRows[oldPosition].playerTroops[player.playerPosition] = null;

            this.currentRow = newPosition;
            Log.inst.AddText($"{player.name} moves {this.name} to row {newPosition+1}.", logged);
        }

        if (currentRow > -1)
        {
            player.availableTroops.Remove(this);
            Row spawnPoint = Manager.inst.allRows[currentRow];
            spawnPoint.playerTroops[player.playerPosition] = this;

            this.transform.SetParent(spawnPoint.button.transform);
            this.transform.localPosition = new((player.playerPosition) == 0 ? -575 : 575, 0);
            this.transform.localScale = Vector3.one;
        }
        else
        {
            player.availableTroops.Add(this);
            this.transform.SetParent(null);
        }
    }

    public void ChangeHealthRPC(int healthChange, int logged)
    {
        Log.inst.RememberStep(this, StepType.Revert, () => ChangeHealth(false, healthChange, logged));
        if (currentHealth <= 0)
        {
            Log.inst.PreserveTextRPC($"{player.name}'s {this.name} is destroyed.", logged);
            Log.inst.RememberStep(this, StepType.Revert, () => MoveTroop(false, currentRow, -1, -1));
        }
    }

    [PunRPC]
    void ChangeHealth(bool undo, int healthChange, int logged)
    {
        if (undo)
        {
            currentHealth -= healthChange;
        }
        else
        {
            currentHealth += healthChange;
            LogHealth(healthChange, logged);
        }
    }

    protected virtual void LogHealth(int healthChange, int logged)
    {
        if (healthChange > 0)
            Log.inst.AddText($"{player.name}'s {this.name} gains {healthChange} health.", logged);
        else if (healthChange < 0)
            Log.inst.AddText($"{player.name}'s {this.name} loses {Mathf.Abs(healthChange)} health.", logged);
    }

    [PunRPC]
    internal void ChangeDamage(bool undo, int damageChange, int logged)
    {
        if (undo)
        {
            currentDamage -= damageChange;
        }
        else
        {
            currentDamage += damageChange;
            if (damageChange > 0)
                Log.inst.AddText($"{player.name}'s {this.name} gains {damageChange} power.", logged);
            else if (damageChange < 0)
                Log.inst.AddText($"{player.name}'s {this.name} loses {Mathf.Abs(damageChange)} power.", logged);
        }
    }

    #endregion

}
