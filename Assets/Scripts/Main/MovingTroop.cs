using UnityEngine;
using Photon.Pun;
using TMPro;

public class MovingTroop : Entity
{

#region Setup

    TMP_Text powerText;
    TMP_Text heartText;

    int myHealth;
    int myPower;

    public int calcHealth { get; private set; }
    public int calcPower { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        heartText = this.transform.Find("Heart Text").GetComponent<TMP_Text>();
        powerText = this.transform.Find("Power Text").GetComponent<TMP_Text>();
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
                this.myHealth = intoTroop.health;
                this.myPower = intoTroop.power;
            }
        }
    }

    #endregion

#region Gameplay

    public void MoveTroopRPC(int newPosition, int logged)
    {
        Log.inst.RememberStep(this, StepType.Revert, () => MoveTroop(false, this.currentRow, newPosition, logged));
    }

    [PunRPC]
    void MoveTroop(bool undo, int oldPosition, int newPosition, int logged)
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
            RecalculateStats();
        }
        else
        {
            player.availableTroops.Add(this);
            this.transform.SetParent(null);
        }
    }

    public void ChangeStatsRPC(int power, int health, int logged)
    {
        Log.inst.RememberStep(this, StepType.Revert, () => ChangeStats(false, power, health, logged));
    }

    [PunRPC]
    void ChangeStats(bool undo, int power, int health, int logged)
    {
        if (undo)
        {
            myPower -= power;
            myHealth -= health;
        }
        else
        {
            myPower += power;
            if (power > 0)
                Log.inst.AddText($"{player.name}'s {this.name} gains {power} power.", logged);
            else if (power < 0)
                Log.inst.AddText($"{player.name}'s {this.name} loses {Mathf.Abs(power)} power.", logged);

            myHealth += health;
            if (health > 0)
                Log.inst.AddText($"{player.name}'s {this.name} gains {health} health.", logged);
            else if (health < 0)
                Log.inst.AddText($"{player.name}'s {this.name} loses {Mathf.Abs(health)} health.", logged);
        }
        RecalculateStats();
    }

    public void RecalculateStats()
    {
        (int troopPower, int troopHealth) = myCard.PassiveStats(this);
        calcPower = myPower + troopPower;
        calcHealth = myHealth + troopHealth;

        Environment enviro = Manager.inst.allRows[currentRow].environment;
        if (enviro != null)
        {
            (int enviroPower, int enviroHealth) = enviro.myCard.PassiveStats(this, enviro);
            calcPower += enviroPower;
            calcHealth += enviroHealth;
        }

        powerText.text = calcPower.ToString();
        heartText.text = calcHealth.ToString();
    }

    public void Attack(int logged)
    {
        Player opposingPlayer = Manager.inst.OtherPlayer(this.player);
        MovingTroop opposingTroop = Manager.inst.FindOpposingTroop(this.player, this.currentRow);

        if (opposingTroop != null)
        {
            Log.inst.PreserveTextRPC($"{this.player.name}'s {this.name} attacks {opposingPlayer.name}'s {opposingTroop.name}.", logged);
            opposingTroop.ChangeStatsRPC(0, -this.calcPower, logged+1);
            foreach ((Card card, Entity entity) in Manager.inst.GatherAbilities())
                card.CardAttacked(this, opposingTroop, logged);
        }
        else
        {
            Log.inst.PreserveTextRPC($"{this.player.name}'s {this.name} attacks {opposingPlayer.name}.", logged);
            opposingPlayer.myBase.ChangeHealthRPC(-this.calcPower, logged+1);
            foreach ((Card card, Entity entity) in Manager.inst.GatherAbilities())
                card.CardAttacked(this, opposingPlayer.myBase, logged);
        }
    }

    #endregion

}
