﻿using System;
using Activator.Base;
using LeagueSharp.Common;

namespace Activator.Spells.Heals
{
    class namiw : CoreSpell
    {
        internal override string Name => "namiw";
        internal override string DisplayName => "Ebb and Flow | W";
        internal override float Range => 725f;
        internal override MenuType[] Category => new[] { MenuType.SelfLowHP, MenuType.SelfMuchHP, MenuType.SelfMinMP };
        internal override int DefaultHP => 90;
        internal override int DefaultMP => 55;

        public override void OnTick(EventArgs args)
        {
            if (!Menu.Item("use" + Name).GetValue<bool>() || !IsReady())
                return;

            if (Player.Mana/Player.MaxMana * 100 <
                Menu.Item("selfminmp" + Name + "pct").GetValue<Slider>().Value)
                return;

            foreach (var hero in Activator.Allies())
            {
                if (!Parent.Item(Parent.Name + "useon" + hero.Player.NetworkId).GetValue<bool>())
                    continue; 

                if (hero.Player.Distance(Player.ServerPosition) <= Range)
                {
                    if (hero.Player.Health/hero.Player.MaxHealth*100 <=
                        Menu.Item("selflowhp" + Name + "pct").GetValue<Slider>().Value)
                        UseSpellOn(hero.Player);

                    if (hero.IncomeDamage/hero.Player.MaxHealth*100 >=
                        Menu.Item("selfmuchhp" + Name + "pct").GetValue<Slider>().Value)
                        UseSpellOn(hero.Player);
                }
            }
        }
    }
}
