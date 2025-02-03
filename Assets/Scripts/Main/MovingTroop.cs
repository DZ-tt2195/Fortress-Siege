using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class MovingTroop : PhotonCompatible, IPointerClickHandler
{

#region Variables

    protected Image image;
    Image border;
    TMP_Text damageText;
    TMP_Text heartText;

    public TroopCard myCard { get; private set; }
    public Player player { get; protected set; }
    public int currentRow { get; private set; }

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
        currentRow = -1;
        try
        {
            heartText = this.transform.Find("Heart Text").GetComponent<TMP_Text>();
            image = this.transform.Find("Art Box").GetComponent<Image>();
            damageText = this.transform.Find("Damage Text").GetComponent<TMP_Text>();
            border = this.transform.Find("border").GetComponent<Image>();
        }
        catch { }
    }

    [PunRPC]
    internal void AssignCardInfo(bool undo, int playerPosition, int cardID)
    {
        this.currentHealth = 0;
        this.currentDamage = 0;

        if (!undo)
        {
            //Debug.Log("assigned card info");
            this.player = Manager.instance.playersInOrder[playerPosition];
            this.image.transform.localScale = new(playerPosition == 0 ? 1 : -1, 1, 1);
            if (border != null)
                border.color = this.player.playerPosition == 0 ? Color.white : Color.black;

            if (cardID >= 0)
            {
                myCard = PhotonView.Find(cardID).GetComponent<TroopCard>();
                this.name = myCard.name;
                this.image.sprite = Resources.Load<Sprite>($"Card Art/{this.name}");
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && myCard != null)
        {
            CarryVariables.instance.RightClickDisplay(this.myCard, 1);
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
                Manager.instance.allRows[newPosition].playerTroops[player.playerPosition] = null;
            this.currentRow = oldPosition;
        }
        else
        {
            if (oldPosition > -1)
                Manager.instance.allRows[oldPosition].playerTroops[player.playerPosition] = null;
            this.currentRow = newPosition;
            Log.instance.AddText($"{player.name} moves {this.name} to row {newPosition+1}.", logged);
        }

        //Debug.Log($"move troop to {currentRow}");
        if (currentRow > -1)
        {
            Row spawnPoint = Manager.instance.allRows[currentRow];
            spawnPoint.playerTroops[player.playerPosition] = this;

            this.transform.SetParent(spawnPoint.button.transform);
            this.transform.localPosition = new((player.playerPosition) == 0 ? -575 : 575, 0);
            this.transform.localScale = Vector3.one;
        }
        else
        {
            this.transform.SetParent(null);
        }
    }

    [PunRPC]
    internal protected virtual void ChangeHealth(bool undo, int healthChange, int logged)
    {
        if (undo)
        {
            currentHealth -= healthChange;
        }
        else
        {
            currentHealth += healthChange;
            if (healthChange > 0)
                Log.instance.AddText($"{player.name}'s {this.name} gains {healthChange} health.", logged);
            else if (healthChange < 0)
                Log.instance.AddText($"{player.name}'s {this.name} loses {Mathf.Abs(healthChange)} health.", logged);
        }
    }

    [PunRPC]
    internal protected virtual void ChangeDamage(bool undo, int damageChange, int logged)
    {
        if (undo)
        {
            currentDamage -= damageChange;
        }
        else
        {
            currentDamage += damageChange;
            if (damageChange > 0)
                Log.instance.AddText($"{player.name}'s {this.name} gains {damageChange} power.", logged);
            else if (damageChange < 0)
                Log.instance.AddText($"{player.name}'s {this.name} loses {Mathf.Abs(damageChange)} power.", logged);
        }
    }

    #endregion

}
