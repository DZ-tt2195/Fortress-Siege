using UnityEngine;
using UnityEngine.UI;
using MyBox;
using Photon.Pun;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class Card : PhotonCompatible
{

#region Variables

    public Button button { get; private set; }
    public Image border { get; private set; }
    public CardLayout layout { get; private set; }
    public int cardID { get; private set; }
    public CardData dataFile { get; private set; }

    #endregion

#region Setup

    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();

        border = this.transform.Find("Border").GetComponent<Image>();
        button = GetComponent<Button>();
        layout = GetComponent<CardLayout>();
        this.transform.localScale = Vector3.Lerp(Vector3.one, Manager.instance.canvas.transform.localScale, 0.5f);
    }
    
    #endregion

#region Animations

    public IEnumerator MoveCard(Vector3 newPos, float waitTime, Vector3 newScale)
    {
        float elapsedTime = 0;
        Vector2 originalPos = this.transform.localPosition;
        Vector2 originalScale = this.transform.localScale;

        while (elapsedTime < waitTime)
        {
            this.transform.localPosition = Vector3.Lerp(originalPos, newPos, elapsedTime / waitTime);
            this.transform.localScale = Vector3.Lerp(originalScale, newScale, elapsedTime / waitTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        this.transform.localPosition = newPos;
    }

    public IEnumerator RevealCard(float totalTime)
    {
        if (this.layout.cg.alpha == 1)
            yield break;

        transform.localEulerAngles = new Vector3(0, 0, 0);
        float elapsedTime = 0f;

        Vector3 originalRot = this.transform.localEulerAngles;
        Vector3 newRot = new(0, 90, 0);

        while (elapsedTime < totalTime)
        {
            this.transform.localEulerAngles = Vector3.Lerp(originalRot, newRot, elapsedTime / totalTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        this.layout.cg.alpha = 1;
        elapsedTime = 0f;

        while (elapsedTime < totalTime)
        {
            this.transform.localEulerAngles = Vector3.Lerp(newRot, originalRot, elapsedTime / totalTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        this.transform.localEulerAngles = originalRot;
    }

    private void FixedUpdate()
    {
        try { this.border.SetAlpha(Manager.instance.opacity); } catch { }
    }

    #endregion

#region Layout

    public virtual Color MyColor()
    {
        return Color.white;
    }

    public virtual string TextBox()
    {
        return "";
    }

#endregion

#region Gameplay

    public virtual bool CanPlayMe(Player player, bool pay)
    {
        if (!pay)
            return true;
        else
            return player.coins >= dataFile.cost;
    }

    public virtual void OnPlayEffect(Player player, int logged)
    {
        player.PopStack();
    }

    protected void DealDamage(Player player, MovingTroop defender, int logged, int totalDamage)
    {
        Log.instance.DoFunction(() => Log.instance.AddText($"{player.name}'s {this.name} does {totalDamage} damage to {defender.name}.", logged));
        //Manager.instance.DoFunction(() => Manager.instance.StoreDamage(defender.pv.ViewID, totalDamage, dataFile.multiTarget));
    }

    #endregion

}
