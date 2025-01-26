using UnityEngine;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class TroopCard : Card
{

#region Setup

    public int damage { get; protected set; }
    public int health { get; protected set; }
    List<Row> canPlayInColumn = new();

    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    public override Color MyColor()
    {
        return Color.red;
    }

    #endregion

#region Gameplay

    public override bool CanPlayMe(Player player, bool pay)
    {
        foreach (Row column in Manager.instance.allRows)
        {
            if (column.playerTroops[player.playerPosition] == null)
                canPlayInColumn.Add(column);
        }

        if (canPlayInColumn.Count >= 1)
            return base.CanPlayMe(player, pay);
        else
            return false;
    }

    public override void OnPlayEffect(Player player, int logged)
    {
        player.ChooseRow(canPlayInColumn, $"Where to play {this.name}?", PlayTroop);

        void PlayTroop()
        {
            CreateTroop(player, player.choice, logged);
            base.OnPlayEffect(player, logged);
        }
    }

    public MovingTroop CreateTroop(Player player, int startingColumn, int logged)
    {
        GameObject createdTroop = Manager.instance.MakeObject(CarryVariables.instance.movingTroopPrefab.gameObject);
        MovingTroop troopComponent = createdTroop.GetComponent<MovingTroop>();

        DoFunction(() => AddAbility(troopComponent.pv.ViewID, player.playerPosition));
        troopComponent.DoFunction(() => troopComponent.AssignCardInfo(this.pv.ViewID));
        troopComponent.DoFunction(() => troopComponent.ChangePlayer(player.playerPosition));
        troopComponent.DoFunction(() => troopComponent.MoveTroop(startingColumn, logged));
        return troopComponent;
    }

    [PunRPC]
    protected virtual void AddAbility(int PV, int playerPosition)
    {
    }

    internal virtual void DeathEffect(MovingTroop troop)
    {
    }

    #endregion

}
