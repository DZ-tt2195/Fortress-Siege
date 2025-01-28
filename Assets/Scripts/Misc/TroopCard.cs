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
        canPlayInColumn = player.FilterRows(false);
        if (canPlayInColumn.Count >= 1)
            return base.CanPlayMe(player, pay);
        else
            return false;
    }

    public override void OnPlayEffect(Player player, int logged)
    {
        if (player.myType == PlayerType.Computer)
        {
            player.inReaction.Add(PlayTroop);
            if (player.currentChain.complete)
                player.DecisionMade(player.currentChain.GetNext());
            else
                player.NewChains(0, canPlayInColumn.Count, 1);
        }
        else if (player.myType == PlayerType.Human)
        {
            player.ChooseRow(canPlayInColumn, $"Where to play {this.name}?", PlayTroop);
        }

        void PlayTroop()
        {
            CreateTroop(player, Manager.instance.allRows.IndexOf(canPlayInColumn[player.choice]), logged);
            base.OnPlayEffect(player, logged);
        }
    }

    public MovingTroop CreateTroop(Player player, int startingColumn, int logged)
    {
        MovingTroop troop = player.availableTroops[0];

        player.RememberStep(this, StepType.Revert, () => RemoveFromAvailability(false, player.playerPosition, troop.pv.ViewID));
        player.RememberStep(troop, StepType.Revert, () => troop.AssignCardInfo(false, player.playerPosition, this.pv.ViewID));
        player.RememberStep(troop, StepType.Revert, () => troop.MoveTroop(false, -1, startingColumn, logged));

        return troop;
    }

    [PunRPC]
    void RemoveFromAvailability(bool undo, int playerPosition, int troopPV)
    {
        Player player = Manager.instance.playersInOrder[playerPosition];
        MovingTroop troop = PhotonView.Find(troopPV).GetComponent<MovingTroop>();
        if (undo)
            player.availableTroops.Add(troop);
        else
            player.availableTroops.Remove(troop);
    }

    #endregion

}
