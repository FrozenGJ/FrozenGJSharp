using System;
using Activator.Base;
using LeagueSharp.Common;

namespace Activator.Items.Defensives
{
    class _3143 : CoreItem
    {
        internal override int Id => 3143;
        internal override int Priority => 4;
        internal override string Name => "Randuins";
        internal override string DisplayName => "Randuin's Omen";
        internal override int Duration => 1000;
        internal override float Range => 500f;
        internal override MenuType[] Category => new[] { MenuType.SelfLowHP, MenuType.SelfCount };
        internal override MapType[] Maps => new[] { MapType.Common };
        internal override int DefaultHP => 55;
        internal override int DefaultMP => 0;

        public override void OnTick(EventArgs args)
        {
            if (!Menu.Item("use" + Name).GetValue<bool>() || !IsReady())
                return;

            if (Player.CountEnemiesInRange(Range) >= Menu.Item("selfcount" + Name).GetValue<Slider>().Value)
            {
                UseItem();
            }
        }
    }
}
