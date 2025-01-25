using UnityEngine;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class TroopCard : Card
{

#region Setup

    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    public override Color MyColor()
    {
        return Color.red;
    }

    public override string TextBox()
    {
        string answer = "";
        /*
        answer += $"Health: {dataFile.health}; Attack: {dataFile.damage}; Speed: {dataFile.movement}";

        if (dataFile.range > 1)
            answer += $"; Range: {dataFile.range}";

        if (dataFile.units > 1)
            answer += $"; Units: {dataFile.units}";
        */
        if (dataFile.extraText != "")
            answer += $"\n\n{dataFile.extraText}";

        return KeywordTooltip.instance.EditText(answer);
    }

    #endregion

#region Gameplay
    /*
    public override bool CanPlayMe(Player player, bool pay)
    {
        canPlay = new();
        foreach (MapSlot slot in Manager.instance.MySide(player))
        {
            if (slot.troopHere == null)
                canPlay.Add(slot);
        }

        if (canPlay.Count >= 1)
            return base.CanPlayMe(player, pay);
        else
            return false;
    }

    public override void OnPlayEffect(Player player, int logged)
    {
        player.ChooseMapSlot(canPlay, $"Where to play {this.name}?", PlayTroop);

        void PlayTroop()
        {
            CreateTroop(player, player.chosenSlot, logged);
            base.OnPlayEffect(player, logged);
        }
    }

    public MovingTroop CreateTroop(Player player, MapSlot startingPosition, int logged)
    {
        GameObject createdTroop = Manager.instance.MakeObject(CarryVariables.instance.movingTroopPrefab.gameObject);
        MovingTroop troopComponent = createdTroop.GetComponent<MovingTroop>();

        DoFunction(() => AddAbility(troopComponent.pv.ViewID, player.playerPosition ));
        troopComponent.DoFunction(() => troopComponent.AssignCardInfo( this.cardID ));
        troopComponent.DoFunction(() => troopComponent.ChangePlayer( player.playerPosition ));
        //troopComponent.DoFunction(() => troopComponent.MoveTroop( startingPosition.position, logged ));
        return troopComponent;
    }
    */
    [PunRPC]
    protected virtual void AddAbility(int PV, int playerPosition)
    {
    }

    internal virtual void DeathEffect(MovingTroop troop)
    {
    }

    #endregion

}
