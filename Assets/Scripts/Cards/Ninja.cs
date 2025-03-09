using System;
using UnityEngine;

public class Ninja : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 3;
        this.power = 2;
        this.health = 2;
        this.extraText = "If the other player has a Troop here, this gets +4 Power.";
        this.artistText = "Elisa Cella\nDominion: Rising Sun\n(Ninja)";
    }

    public override (int, int) PassiveStats(MovingTroop troop, MovingAura enviro = null)
    {
        MovingTroop otherTroop = Manager.inst.FindOpposingTroop(troop.player, troop.currentRow);
        if (otherTroop == null)
            return (0, 0);
        else
            return (4, 0);
    }
}
