using System.Collections.Generic;
using UnityEngine;

public class Ghost : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 5;
        this.power = 2;
        this.health = 4;
        this.extraText = "When you play this: Return an opposing Troop to its owner's hand.";
        this.artistText = "Eric J. Carter\nDominion: Nocturne\n(Ghost)";
    }

    public override void DonePlaying(Player player, Entity createdEntity, int logged)
    {
        Log.inst.RememberStep(this, StepType.UndoPoint, () => CardDecision(player, createdEntity, logged));
    }

    void CardDecision(Player player, Entity createdEntity, int logged)
    {
        Player otherPlayer = Manager.inst.OpposingPlayer(player);
        List<Row> withTroops = otherPlayer.FilterRows(true);

        if (player.myType == PlayerType.Bot)
        {
            if (player.chainTracker < player.currentChain.decisions.Count)
            {
                int next = player.currentChain.decisions[player.chainTracker];
                player.inReaction.Add(Bounce);
                player.DecisionMade(next);
            }
            else
            {
                player.NewChains(player.RowsToInts(withTroops));
            }
        }
        else
        {
            if (withTroops.Count == 0)
            {
                Log.inst.undoToThis = null;
                base.DonePlaying(player, createdEntity, logged);
                player.DecisionMade(-1);
            }
            else
            {
                player.ChooseRow(withTroops, "Choose an opposing Troop to return.", Bounce);
            }
        }

        void Bounce()
        {
            if (player.choice >= 0)
            {
                Row targetRow = Manager.inst.allRows[player.choice];
                MovingTroop targetTroop = targetRow.playerTroops[otherPlayer.playerPosition];
                otherPlayer.BounceCardRPC(targetTroop, logged);
            }
            else
            {
                Log.inst.AddTextRPC($"{this.name} can't target any Troops.", LogAdd.Remember, logged);
            }
            base.DonePlaying(player, createdEntity, logged);
        }
    }
}
