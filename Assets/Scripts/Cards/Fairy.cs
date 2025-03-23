using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Fairy : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 2;
        this.power = 2;
        this.health = 3;
        this.extraText = "When you play this: Move an opposing troop to another row.";
        this.artistText = "Claus Stephan\nDominion: Nocturne\n(Pixie)";
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
                player.inReaction.Add(ChosenTroop);
                player.DecisionMade(next);
            }
            else
            {

                if (withTroops.Count == 0 || withTroops.Count == 5)
                    player.NewChains(new List<int>() { -1 });
                else
                    player.NewChains(player.RowsToInts(withTroops));
            }
        }
        else
        {
            if (withTroops.Count == 0 || withTroops.Count == 5)
            {
                Log.inst.undoToThis = null;
                base.DonePlaying(player, createdEntity, logged);
                player.DecisionMade(-1);
            }
            else
            {
                player.ChooseRow(withTroops, "Move an opposing troop.", ChosenTroop);
            }
        }

        void ChosenTroop()
        {
            if (player.choice >= 0)
            {
                Row targetRow = Manager.inst.allRows[player.choice];
                MovingTroop targetTroop = targetRow.playerTroops[otherPlayer.playerPosition];
                Log.inst.AddTextRPC($"{this.name} chooses {targetTroop.name}.", LogAdd.Personal, logged);
                Log.inst.RememberStep(this, StepType.UndoPoint, () => MoveChosenTroop(player, createdEntity, targetTroop, logged));
            }
            else
            {
                Log.inst.AddTextRPC($"{this.name} has no Troops to move.", LogAdd.Remember, logged);
                base.DonePlaying(player, createdEntity, logged);
            }
        }
    }

    void MoveChosenTroop(Player player, Entity createdEntity, MovingTroop targetTroop, int logged)
    {
        Player otherPlayer = Manager.inst.OpposingPlayer(player);
        List<Row> blankSpots = otherPlayer.FilterRows(false);

        if (player.myType == PlayerType.Bot)
        {
            if (player.chainTracker < player.currentChain.decisions.Count)
            {
                int next = player.currentChain.decisions[player.chainTracker];
                player.inReaction.Add(MoveTarget);
                player.DecisionMade(next);
            }
            else
            {
                player.NewChains(player.RowsToInts(blankSpots));
            }
        }
        else
        {
            player.ChooseRow(blankSpots, $"Move {targetTroop.name} to a different row.", MoveTarget);
        }

        void MoveTarget()
        {
            int rememberChoice = Manager.inst.allRows[player.choice].position;
            targetTroop.MoveEntityRPC(rememberChoice, logged);
            base.DonePlaying(player, createdEntity, logged);
        }
    }
}
