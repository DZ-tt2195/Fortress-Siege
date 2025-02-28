using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Excavator : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 4;
        this.power = 4;
        this.health = 1;
        this.extraText = "When you play this: You may destroy an Environment.";
    }

    public override void DonePlaying(Player player, Entity createdEntity, int logged)
    {
        Log.inst.RememberStep(this, StepType.UndoPoint, () => CardDecision(player, createdEntity, logged));
    }

    void CardDecision(Player player, Entity createdEntity, int logged)
    { 
        List<Row> withEnviros = Manager.inst.allRows.Where(row => row.environment != null).ToList();
        List<string> actions = new() { $"Decline" };

        if (player.myType == PlayerType.Computer)
        {
            if (player.chainTracker < player.currentChain.decisions.Count)
            {
                int next = player.currentChain.decisions[player.chainTracker];
                //Debug.Log($"resolved continue turn with choice {next}");
                player.inReaction.Add(Excavation);
                player.DecisionMade(next);
            }
            else
            {
                //Debug.Log($"{chainTracker}, {currentChain.decisions.Count}");
                player.NewChains(-1, withEnviros.Count, 1);
            }
        }
        else
        {
            if (withEnviros.Count == 0)
            {
                Log.inst.undoToThis = null;
                base.DonePlaying(player, createdEntity, logged);
                player.DecisionMade(-1);
            }
            else
            {
                player.ChooseButton(actions, Vector3.zero, "Destroy an Environment?", Excavation);
                player.ChooseRow(withEnviros, "Destroy an Environment?", null);
            }
        }

        void Excavation()
        {
            if (player.choice < withEnviros.Count)
            {
                Row toRemove = withEnviros[player.choice];
                Environment enviro = toRemove.environment;
                enviro.MoveEnviroRPC(-1, logged);
            }
            else
            {
                Log.inst.PreserveTextRPC($"{this.name} doesn't destroy an Environment.", logged);
            }
            base.DonePlaying(player, createdEntity, logged);
        }
    }
}
