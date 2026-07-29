using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MokouMod.MokouModCode.Potions;
using MokouMod.MokouModCode.Powers;

namespace MokouMod.MokouModCode.Cards.Rare;

public class HonestMansDeath : MokouModCard
{
    public HonestMansDeath() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(10, 4);
        WithPower<MarkOfSinPower>(2);
        WithKeyword(CardKeyword.Retain, UpgradeType.Add);
        WithKeyword(CardKeyword.Exhaust);
        WithTip(typeof(HouraiElixir));
    }

    protected override async Task OnPlayMokou(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, cardPlay).Targeting(cardPlay.Target).WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3").Execute(choiceContext);
        await PowerCmd.Apply<MarkOfSinPower>(choiceContext, cardPlay.Target, DynamicVars["MarkOfSinPower"].BaseValue, Owner.Creature, this);
        
    }
}