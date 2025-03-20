using System.Collections.Generic;
using UnityEngine;

public class Puppeteer : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 3;
        this.power = 4;
        this.health = 2;
        this.extraText = "When you play this: Switch the positions of 2 of your Troops.";
        this.artistText = "James Ryman\nMTG: Kaladesh\n(Marionette Master)";
    }

    public override void DonePlaying(Player player, Entity createdEntity, int logged)
    {
        Log.inst.RememberStep(this, StepType.UndoPoint, () => FirstDecision(player, createdEntity, logged));
    }

    void FirstDecision(Player player, Entity createdEntity, int logged)
    {
        List<Row> withTroops = player.FilterRows(true);

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
                if (withTroops.Count < 2)
                    player.NewChains(new List<int>() { -1 });
                else
                    player.NewChains(player.RowsToInts(withTroops));
            }
        }
        else
        {
            if (withTroops.Count < 2)
            {
                Log.inst.undoToThis = null;
                base.DonePlaying(player, createdEntity, logged);
                player.DecisionMade(-1);
            }
            else
            {
                player.ChooseRow(withTroops, "Choose your 1st troop to move.", ChosenTroop);
            }
        }

        void ChosenTroop()
        {
            if (player.choice >= 0)
            {
                Row targetRow = Manager.inst.allRows[player.choice];
                MovingTroop firstTroop = targetRow.playerTroops[player.playerPosition];
                Log.inst.PreserveTextRPC($"{this.name} chooses {firstTroop.name}.", logged);
                Log.inst.RememberStep(this, StepType.UndoPoint, () => SecondDecision(player, createdEntity, firstTroop, logged));
            }
            else
            {
                Log.inst.PreserveTextRPC($"{this.name} doesn't have enough Troops to switch.", logged);
                base.DonePlaying(player, createdEntity, logged);
            }
        }
    }

    void SecondDecision(Player player, Entity createdEntity, MovingTroop firstTroop, int logged)
    {
        List<Row> blankSpots = player.FilterRows(true);
        blankSpots.Remove(Manager.inst.allRows[firstTroop.currentRow]);

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
            player.ChooseRow(blankSpots, $"Choose your 2nd Troop to move.", MoveTarget);
        }

        void MoveTarget()
        {
            Row targetRow = Manager.inst.allRows[player.choice];
            MovingTroop secondTroop = targetRow.playerTroops[player.playerPosition];
            int firstLocation = firstTroop.currentRow;
            int secondLocation = secondTroop.currentRow;

            firstTroop.MoveEntityRPC(secondLocation, logged);
            secondTroop.MoveEntityRPC(firstLocation, logged);

            base.DonePlaying(player, createdEntity, logged);
        }
    }
}
