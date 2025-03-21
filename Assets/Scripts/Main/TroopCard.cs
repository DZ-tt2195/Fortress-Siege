using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

public class TroopCard : Card
{

#region Setup

    public int power { get; protected set; }
    public int health { get; protected set; }
    List<Row> canPlayInColumn = new();

    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    public override Color MyColor()
    {
        return Color.gray;
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
        Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseRow(player, logged));
    }

    void ChooseRow(Player player, int logged)
    {
        if (player.myType == PlayerType.Bot)
        {
            if (player.chainTracker < player.currentChain.decisions.Count)
            {
                int next = player.currentChain.decisions[player.chainTracker];
                //Debug.Log($"resolved choose row with choice {next}");
                player.inReaction.Add(PlayTroop);
                player.DecisionMade(next);
            }
            else
            {
                //Debug.Log($"add rows: {player.chainTracker}, {player.currentChain.decisions.Count}");
                player.NewChains(player.RowsToInts(canPlayInColumn));
            }
        }
        else if (player.myType == PlayerType.Human)
        {
            player.ChooseRow(canPlayInColumn, $"Where to play {this.name}?", PlayTroop);
        }

        void PlayTroop()
        {
            int rememberChoice = Manager.inst.allRows[player.choice].position;
            MovingTroop newTroop = player.availableTroops[0];
            newTroop.AssignCardRPC(player, this);

            newTroop.MoveEntityRPC(rememberChoice, logged + 1);
            DonePlaying(player, newTroop, logged + 1);
        }
    }

    #endregion

}
