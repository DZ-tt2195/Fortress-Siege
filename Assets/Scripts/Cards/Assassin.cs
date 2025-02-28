using System;
using UnityEngine;

public class Assassin : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 3;
        this.power = 2;
        this.health = 2;
        this.extraText = "If the other player has a Troop here, this gets +4 Power.";
    }

    public override (int, int) PassiveStats(MovingTroop troop, Environment enviro = null)
    {
        MovingTroop otherTroop = Manager.inst.FindOpposingTroop(troop.player, troop.currentRow);
        if (otherTroop == null)
            return (0, 0);
        else
            return (4, 0);
    }
}
