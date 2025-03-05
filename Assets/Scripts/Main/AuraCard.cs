using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class AuraCard : Card
{

#region Setup

    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    public override Color MyColor()
    {
        return Color.blue;
    }

    #endregion

#region Gameplay

    public override void OnPlayEffect(Player player, int logged)
    {
        Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseRow(player, logged));
    }

    void ChooseRow(Player player, int logged)
    {
        if (player.myType == PlayerType.Computer)
        {
            if (player.chainTracker < player.currentChain.decisions.Count)
            {
                int next = player.currentChain.decisions[player.chainTracker];
                //Debug.Log($"resolved choose row with choice {next}");
                player.inReaction.Add(PlayEnviro);
                player.DecisionMade(next);
            }
            else
            {
                //Debug.Log($"add rows: {player.chainTracker}, {player.currentChain.decisions.Count}");
                player.NewChains(0, 5, 0);
            }
        }
        else if (player.myType == PlayerType.Human)
        {
            player.ChooseRow(Manager.inst.allRows, $"Where to play {this.name}?", PlayEnviro);
        }

        void PlayEnviro()
        {
            Row row = Manager.inst.allRows[player.choice];
            int rememberChoice = row.position;
            MovingAura existingEnviro = row.auraHere;
            if (existingEnviro != null)
                existingEnviro.MoveEntityRPC(-1, logged + 1);

            MovingAura newEnviro = player.availableEnviros[0];
            Log.inst.RememberStep(newEnviro, StepType.Revert, () => newEnviro.AssignCardInfo(false, player.playerPosition, this.pv.ViewID));
            newEnviro.MoveEntityRPC(rememberChoice, logged + 1);
            DonePlaying(player, newEnviro, logged+1);
        }
    }

    #endregion

}