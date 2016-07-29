#region Copyright © 2015 Kurisu Solutions
// All rights are reserved. Transmission or reproduction in part or whole,
// any form or by any means, mechanical, electronical or otherwise, is prohibited
// without the prior written consent of the copyright owner.
// 
// Document:	Handlers/Buffs.cs
// Date:		22/09/2015
// Author:		Robin Kurisu
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Activator.Base;
using Activator.Data;
using LeagueSharp;
using LeagueSharp.Common;

namespace Activator.Handlers
{
    public static class Buffs
    {
        public static void StartOnUpdate()
        {
            Game.OnUpdate += Game_OnUpdate;
        }

       internal static void Game_OnUpdate(EventArgs args)
       {
            foreach (var hero in Activator.Allies())
            {
                var aura = Buffdata.SomeAuras.Find(au => hero.Player.HasBuff(au.Name));
                if (aura == null)
                {
                    if (hero.DotTicks > 0)
                    {
                        hero.IncomeDamage -= 1;
                        hero.DotTicks -= 1;
                    }

                    if (hero.IncomeDamage < 0)
                        hero.IncomeDamage = 0;

                    continue;
                }

                if (aura.Cleanse)
                {
                    Utility.DelayAction.Add(aura.CleanseTimer,
                        () =>
                        {
                            // double check after delay incase we no longer have the buff
                            if (hero.Player.HasBuff(aura.Name))
                            {
                                hero.ForceQSS = true;
                                Utility.DelayAction.Add(100, () => hero.ForceQSS = false);
                            }
                        });
                }

                if (aura.Evade)
                {
                    Utility.DelayAction.Add(aura.EvadeTimer,
                        () =>
                        {                           
                            // double check after delay incase we no longer have the buff
                            if (hero.Player.HasBuff(aura.Name))
                            {
                                if (!hero.HitTypes.Contains(HitType.Ultimate))
                                {
                                    Utility.DelayAction.Add(500, () => hero.HitTypes.Remove(HitType.Ultimate));
                                    hero.HitTypes.Add(HitType.Ultimate);
                                }

                                if (Utils.GameTimeTickCount - aura.TickLimiter >= 100)
                                {
                                    hero.DotTicks += 1;
                                    hero.IncomeDamage += 1;
                                    aura.TickLimiter = Utils.GameTimeTickCount;
                                }
                            }
                        });
                }

                if (aura.DoT)
                {
                    if (Utils.GameTimeTickCount - aura.TickLimiter >= aura.Interval * 1000)
                    {
                        if (hero.Player.IsValidTarget(float.MaxValue, false))
                        {
                            if (!hero.Player.IsZombie && !hero.Immunity)
                            {
                                if (aura.Name == "velkozresearchstack" &&
                                    !hero.Player.HasBuffOfType(BuffType.Slow))
                                    continue;

                                hero.DotTicks += 1;
                                hero.IncomeDamage += 1; // todo: get actuall damage
                                aura.TickLimiter = Utils.GameTimeTickCount;
                            }
                        }
                    }
                }            
            }
        }

        #region Cleanse

        internal static void CheckCleanse(Obj_AI_Hero player)
        {
            foreach (var hero in Activator.Heroes.Where(x => x.Player.NetworkId == player.NetworkId))
            {
                hero.CleanseBuffCount = GetAuras(hero.Player, "summonerboost").Count();

                if (hero.CleanseBuffCount > 0)
                {
                    foreach (var buff in GetAuras(hero.Player, "summonerboost"))
                    {
                        var duration = (int) (buff.EndTime - buff.StartTime);
                        if (duration > hero.CleanseHighestBuffTime)
                        {
                            hero.CleanseHighestBuffTime = duration * 1000;
                        }
                    }

                    hero.LastDebuffTimestamp = Utils.GameTimeTickCount;
                }

                else
                {
                    if (hero.CleanseHighestBuffTime > 0)
                        hero.CleanseHighestBuffTime -= hero.QSSHighestBuffTime;
                    else
                        hero.CleanseHighestBuffTime = 0;
                }
            }
        }

        #endregion

        #region Dervish
        internal static void CheckDervish(Obj_AI_Hero player)
        {
            foreach (var hero in Activator.Heroes.Where(x => x.Player.NetworkId == player.NetworkId))
            {
                hero.DervishBuffCount = GetAuras(hero.Player, "Dervish").Count();

                if (hero.DervishBuffCount > 0)
                {
                    foreach (var buff in GetAuras(hero.Player, "Dervish"))
                    {
                        var duration = (int) (buff.EndTime - buff.StartTime);
                        if (duration > hero.DervishHighestBuffTime)
                        {
                            hero.DervishHighestBuffTime = duration * 1000;
                        }
                    }

                    hero.LastDebuffTimestamp = Utils.GameTimeTickCount;
                }

                else
                {
                    if (hero.DervishHighestBuffTime > 0)
                        hero.DervishHighestBuffTime -= hero.DervishHighestBuffTime;
                    else
                        hero.DervishHighestBuffTime = 0;
                }
            }
        }

        #endregion

        #region QSS
        internal static void CheckQSS(Obj_AI_Hero player)
        {
            foreach (var hero in Activator.Heroes.Where(x => x.Player.NetworkId == player.NetworkId))
            {
                hero.QSSBuffCount = GetAuras(hero.Player, "Quicksilver").Count();

                if (hero.QSSBuffCount > 0)
                {
                    foreach (var buff in GetAuras(hero.Player, "Quicksilver"))
                    {
                        var duration = (int) (buff.EndTime - buff.StartTime);
                        if (duration > hero.QSSHighestBuffTime)
                        {
                            hero.QSSHighestBuffTime = duration * 1000;
                        }
                    }

                    hero.LastDebuffTimestamp = Utils.GameTimeTickCount;
                }

                else
                {
                    if (hero.QSSHighestBuffTime > 0)
                        hero.QSSHighestBuffTime -= hero.QSSHighestBuffTime;
                    else
                        hero.QSSHighestBuffTime = 0;
                }
            }
        }

        #endregion

        #region Mikaels
        internal static void CheckMikaels(Obj_AI_Hero player)
        {
            foreach (var hero in Activator.Heroes.Where(x => x.Player.NetworkId == player.NetworkId))
            {
                hero.MikaelsBuffCount = GetAuras(hero.Player, "Mikaels").Count();

                if (hero.MikaelsBuffCount > 0)
                {
                    foreach (var buff in GetAuras(hero.Player, "Mikaels"))
                    {
                        var duration = (int) (buff.EndTime - buff.StartTime);
                        if (duration > hero.MikaelsHighestBuffTime)
                        {
                            hero.MikaelsHighestBuffTime = duration * 1000;
                        }
                    }

                    hero.LastDebuffTimestamp = Utils.GameTimeTickCount;
                }

                else
                {
                    if (hero.MikaelsHighestBuffTime > 0)
                        hero.MikaelsHighestBuffTime -= hero.MikaelsHighestBuffTime;
                    else
                        hero.MikaelsHighestBuffTime = 0;
                }

                foreach (var aura in Buffdata.BuffList.Where(au => hero.Player.HasBuff(au.Name)))
                {
                    if (aura.DoT && hero.Player.Health / hero.Player.MaxHealth * 100 <=
                        Activator.Origin.Item("useMikaelsdot").GetValue<Slider>().Value)
                    {
                        hero.ForceQSS = true;
                        Utility.DelayAction.Add(100, () => hero.ForceQSS = false);
                    }
                }
            }
        }

        #endregion

        #region Mercurial
        internal static void CheckMercurial(Obj_AI_Hero player)
        {
            foreach (var hero in Activator.Heroes.Where(x => x.Player.NetworkId == player.NetworkId))
            {
                hero.MercurialBuffCount = GetAuras(hero.Player, "Mercurial").Count();

                if (hero.MercurialBuffCount > 0)
                {
                    foreach (var buff in GetAuras(hero.Player, "Mercurial"))
                    {
                        var duration = (int) (buff.EndTime - buff.StartTime);
                        if (duration > hero.MercurialHighestBuffTime)
                        {
                            hero.MercurialHighestBuffTime = duration * 1000;
                        }
                    }

                    hero.LastDebuffTimestamp = Utils.GameTimeTickCount;
                }

                else
                {
                    if (hero.MercurialHighestBuffTime > 0)
                        hero.MercurialHighestBuffTime -= hero.MercurialHighestBuffTime;
                    else
                        hero.MercurialHighestBuffTime = 0;
                }
            }
        }

        #endregion

        internal static IEnumerable<BuffInstance> GetAuras(Obj_AI_Hero player, string itemname)
        {
            if (player.HasBuffOfType(BuffType.Knockback) || player.HasBuffOfType(BuffType.Knockup))
                return Enumerable.Empty<BuffInstance>();

            return player.Buffs.Where(buff => 
                !Buffdata.BuffList.Any(b => buff.Name.ToLower() == b.Name && b.QssIgnore) &&
                   (buff.Type == BuffType.Snare &&
                    Activator.Origin.Item(itemname + "csnare").GetValue<bool>() ||
                    buff.Type == BuffType.Silence &&
                    Activator.Origin.Item(itemname + "csilence").GetValue<bool>() ||
                    buff.Type == BuffType.Charm &&
                    Activator.Origin.Item(itemname + "ccharm").GetValue<bool>() ||
                    buff.Type == BuffType.Taunt &&
                    Activator.Origin.Item(itemname + "ctaunt").GetValue<bool>() ||
                    buff.Type == BuffType.Stun &&
                    Activator.Origin.Item(itemname + "cstun").GetValue<bool>() ||
                    buff.Type == BuffType.Flee &&
                    Activator.Origin.Item(itemname + "cflee").GetValue<bool>() ||
                    buff.Type == BuffType.Polymorph &&
                    Activator.Origin.Item(itemname + "cpolymorph").GetValue<bool>() ||
                    buff.Type == BuffType.Blind &&
                    Activator.Origin.Item(itemname + "cblind").GetValue<bool>() ||
                    buff.Type == BuffType.Suppression &&
                    Activator.Origin.Item(itemname + "csupp").GetValue<bool>() ||
                    buff.Type == BuffType.Poison &&
                    Activator.Origin.Item(itemname + "cpoison").GetValue<bool>() ||
                    buff.Type == BuffType.Slow &&
                    Activator.Origin.Item(itemname + "cslow").GetValue<bool>() || 
                    buff.Name.ToLower() == "summonerexhaust") &&
                    Activator.Origin.Item(itemname + "cexh").GetValue<bool>());
        }

        public static int GetCustomDamage(this Obj_AI_Hero source, string auraname, Obj_AI_Hero target)
        {
            if (auraname == "sheen")
            {
                return
                    (int)
                        source.CalcDamage(target, Damage.DamageType.Physical,
                            1.0 * source.FlatPhysicalDamageMod + source.BaseAttackDamage);
            }

            if (auraname == "lichbane")
            {
                return
                    (int)
                        source.CalcDamage(target, Damage.DamageType.Magical,
                            (0.75 * source.FlatPhysicalDamageMod + source.BaseAttackDamage) +
                            (0.50 * source.FlatMagicDamageMod));
            }

            return 0;
        }
    }

}
