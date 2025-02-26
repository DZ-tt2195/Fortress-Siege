using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class EnviroCard : Card
{

#region Setup

    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    protected float Math()
    {
        float math = (-3 - this.coinCost) + ((6f/2) + abilityValue);
        if (Mathf.Abs(math) >= 2f)
            Debug.Log($"{this.name}'s math: {math}");
        return math;
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
            int rememberChoice = player.choice;

            Environment existingEnviro = Manager.inst.allRows[rememberChoice].environment;
            if (existingEnviro != null)
                existingEnviro.MoveEnviroRPC(-1, logged + 1);

            Environment newEnviro = player.availableEnviros[0];
            Log.inst.RememberStep(newEnviro, StepType.Revert, () => newEnviro.AssignCardInfo(false, player.playerPosition, this.pv.ViewID));
            newEnviro.MoveEnviroRPC(rememberChoice, logged + 1);
            DonePlaying(player, newEnviro, logged+1);
        }
    }

    #endregion

}