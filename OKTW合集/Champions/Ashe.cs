﻿using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SebbyLib;

namespace OneKeyToWin_AIO_Sebby
{
    class Ashe
    {
        private Menu Config = Program.Config;
        public static SebbyLib.Orbwalking.Orbwalker Orbwalker = Program.Orbwalker;
        private bool CastR = false;
        public Spell Q, W, E, R;
        public float QMANA = 0, WMANA = 0, EMANA = 0, RMANA = 0;
        public Obj_AI_Hero Player { get { return ObjectManager.Player; }}
        
        private void LoadMenuOKTW()
        {
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw only ready spells", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("wRange", "W range", true).SetValue(false));


            Config.SubMenu(Player.ChampionName).SubMenu("Q Config").AddItem(new MenuItem("harasQ", "Harass Q", true).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("autoE", "Auto E", true).SetValue(true));
            
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("autoR", "Auto R", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("Rkscombo", "R KS combo R + W + AA", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("autoRaoe", "Auto R aoe", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("autoRinter", "Auto R OnPossibleToInterrupt", true).SetValue(true));

            foreach (var enemy in HeroManager.Enemies)
            {
                for (int i = 0; i < 4; i++)
                {
                    var spell = enemy.Spellbook.Spells[i];
                    if (spell.SData.TargettingType != SpellDataTargetType.Self && spell.SData.TargettingType != SpellDataTargetType.SelfAndUnit)
                    {
                        Config.SubMenu(Player.ChampionName).SubMenu("R Config").SubMenu("Spell Manager").SubMenu(enemy.ChampionName).AddItem(new MenuItem("spell" + spell.SData.Name, spell.Name, true).SetValue(false));
                    }
                }
            }

            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("useR", "Semi-manual cast R key", true).SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press))); //32 == space

            List<string> modes = new List<string>();

            modes.Add("LOW HP");
            modes.Add("CLOSEST");

            foreach (var enemy in HeroManager.Enemies)
            {
                modes.Add(enemy.ChampionName);
            }
            
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("Semi-manual", "Semi-manual MODE", true).SetValue(new StringList(modes.ToArray(), 0)));

            foreach (var enemy in HeroManager.Enemies)
                Config.SubMenu(Player.ChampionName).SubMenu("R Config").SubMenu("GapCloser R").AddItem(new MenuItem("GapCloser" + enemy.ChampionName, enemy.ChampionName).SetValue(false));

            foreach (var enemy in HeroManager.Enemies)
                Config.SubMenu(Player.ChampionName).SubMenu("Harras").AddItem(new MenuItem("haras" + enemy.ChampionName, enemy.ChampionName).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmQ", "Lane clear Q", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmW", "Lane clear W", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("Mana", "LaneClear Mana", true).SetValue(new Slider(80, 100, 30)));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("LCminions", "LaneClear minimum minions", true).SetValue(new Slider(3, 10, 0)));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("jungleQ", "Jungle clear Q", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("jungleW", "Jungle clear W", true).SetValue(true));
        }

        public void LoadOKTW()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 1240);
            E = new Spell(SpellSlot.E, 2500);
            R = new Spell(SpellSlot.R, float.MaxValue);

            W.SetSkillshot(0.25f, 20f , 1500f, true, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.25f, 299f, 1400f, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(0.25f, 130f, 1600f, false, SkillshotType.SkillshotLine);
            LoadMenuOKTW();

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            SebbyLib.Orbwalking.BeforeAttack += BeforeAttack;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget +=Interrupter2_OnInterruptableTarget;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {

            if (!R.IsReady() || sender.IsMinion || !sender.IsEnemy || args.SData.IsAutoAttack()
                || !sender.IsValid<Obj_AI_Hero>() || !sender.IsValidTarget() || args.SData.Name.ToLower() == "tormentedsoil")
                return;

            if (Config.Item("spell" + args.SData.Name, true) != null && Config.Item("spell" + args.SData.Name, true).GetValue<bool>())
            {
                R.Cast(sender);
                Program.debug("R 2");
            }
        }

        private void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (Config.Item("autoRinter", true).GetValue<bool>() && R.IsReady() && sender.IsValidTarget(2500))
                R.Cast(sender);
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (R.IsReady() )
            {
                var Target = gapcloser.Sender;
                if (Target.IsValidTarget(800) && Config.Item("GapCloser" + Target.ChampionName).GetValue<bool>())
                {
                    R.Cast(Target.ServerPosition, true);
                    Program.debug("AGC " + Target.ChampionName);
                }
            }
        }

        private void BeforeAttack(SebbyLib.Orbwalking.BeforeAttackEventArgs args)
        {
            LogicQ();
        }

        private void Game_OnUpdate(EventArgs args)
        {
            if (R.IsReady())
            {
                if (Config.Item("useR", true).GetValue<KeyBind>().Active)
                {
                    CastR = true;
                }

                if (CastR)
                {
                    Program.debug("R semi");
                    if (Config.Item("Semi-manual", true).GetValue<StringList>().SelectedIndex == 0)
                    { 
                        var t = TargetSelector.GetTarget(1800, TargetSelector.DamageType.Physical);
                        if (t.IsValidTarget())
                            Program.CastSpell(R, t);
                    }
                    else if(Config.Item("Semi-manual", true).GetValue<StringList>().SelectedIndex == 1)
                    {
                        var t = HeroManager.Enemies.OrderBy(x => x.Distance(Player)).FirstOrDefault();
                        if (t.IsValidTarget())
                            Program.CastSpell(R, t);
                    }
                    else
                    {
                        var t = HeroManager.Enemies[Config.Item("Semi-manual", true).GetValue<StringList>().SelectedIndex - 2];
                        if (t.IsValidTarget())
                            Program.CastSpell(R, t);
                    }
                }
            }
            else
            {
                CastR = false;
            }

            if (Program.LagFree(1))
            {
                SetMana();
                Jungle();
            }

            if (Program.LagFree(3) && W.IsReady() && !Player.IsWindingUp)
                LogicW();

            if (Program.LagFree(4) && R.IsReady())
                LogicR();
        }

        private void Jungle()
        {
            if (Program.LaneClear)
            {
                var mobs = Cache.GetMinions(Player.ServerPosition, 600, MinionTeam.Neutral);
                if (mobs.Count > 0)
                {
                    var mob = mobs[0];

                    if (W.IsReady() && Config.Item("jungleW", true).GetValue<bool>())
                    {
                        W.Cast(mob.ServerPosition);
                        return;
                    }
                    if (Q.IsReady() && Config.Item("jungleQ", true).GetValue<bool>())
                    {
                        Q.Cast();
                        return;
                    }
                }
            }
        }

        private void LogicR()
        {
            if (Config.Item("autoR", true).GetValue<bool>())
            {
                foreach (var target in HeroManager.Enemies.Where(target => target.IsValidTarget(2000) && OktwCommon.ValidUlt(target)))
                {
                    var rDmg = OktwCommon.GetKsDamage(target, R);
                    if (Program.Combo && target.CountEnemiesInRange(250) > 2 && Config.Item("autoRaoe", true).GetValue<bool>())
                        Program.CastSpell(R, target);
                    if(Program.Combo && target.IsValidTarget(W.Range)  && Config.Item("Rkscombo", true).GetValue<bool>() &&  Player.GetAutoAttackDamage(target) * 5 + rDmg + W.GetDamage(target) > target.Health && target.HasBuffOfType(BuffType.Slow) && !OktwCommon.IsSpellHeroCollision(target, R))
                        Program.CastSpell(R, target);
                    if (rDmg > target.Health && target.CountAlliesInRange(600) == 0 && target.Distance(Player.Position) > 1000)
                    {
                        if (!OktwCommon.IsSpellHeroCollision(target, R))
                            Program.CastSpell(R, target);
                    }
                }
            }

            if (Player.HealthPercent < 50)
            {
                foreach (var enemy in HeroManager.Enemies.Where(enemy => enemy.IsValidTarget(300) && enemy.IsMelee && Config.Item("GapCloser" + enemy.ChampionName).GetValue<bool>() && !OktwCommon.ValidUlt(enemy)))
                {
                    R.Cast(enemy);
                    Program.debug("R Meele");
                }
            }
        }

        private void LogicQ()
        {
            var t = Orbwalker.GetTarget() as Obj_AI_Hero;
            if (t != null && t.IsValidTarget())
            {
                if (Program.Combo && (Player.Mana > RMANA + QMANA || t.Health < 5 * Player.GetAutoAttackDamage(Player)))
                    Q.Cast();
                else if (Program.Farm && Player.Mana > RMANA + QMANA + WMANA && Config.Item("harasQ", true).GetValue<bool>() && Config.Item("haras" + t.ChampionName).GetValue<bool>())
                    Q.Cast();
            }
            else if (Program.LaneClear)
            {
                var minion = Orbwalker.GetTarget() as Obj_AI_Minion;
                if(minion != null && Player.ManaPercent > Config.Item("Mana", true).GetValue<Slider>().Value && Config.Item("farmQ", true).GetValue<bool>() && Player.Mana > RMANA + QMANA)
                {
                    if (Cache.GetMinions(Player.ServerPosition, 600).Count >= Config.Item("LCminions", true).GetValue<Slider>().Value)
                        Q.Cast();
                }
            }
        }

        private void LogicW()
        {
            var t = Orbwalker.GetTarget() as Obj_AI_Hero;

            if (t == null)
                t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);

            if (t.IsValidTarget())
            {
                if (Program.Combo && Player.Mana > RMANA + WMANA)
                    CastW(t);
                else if (Program.Farm  && Player.Mana > RMANA + WMANA + QMANA + WMANA && OktwCommon.CanHarras())
                {
                    foreach (var enemy in HeroManager.Enemies.Where(enemy => enemy.IsValidTarget(W.Range) && Config.Item("haras" + t.ChampionName).GetValue<bool>()))
                        CastW(t);
                }
                else if (OktwCommon.GetKsDamage(t, W) > t.Health)
                {
                    CastW(t);
                }

                if (!Program.None && Player.Mana > RMANA + WMANA)
                {
                    foreach (var enemy in HeroManager.Enemies.Where(enemy => enemy.IsValidTarget(W.Range) && !OktwCommon.CanMove(enemy)))
                        W.Cast(t);
                }
            }
            else if (Program.LaneClear && Player.ManaPercent > Config.Item("Mana", true).GetValue<Slider>().Value && Config.Item("farmW", true).GetValue<bool>() && Player.Mana > RMANA + WMANA)
            {
                var minionList = Cache.GetMinions(Player.ServerPosition, W.Range);
                var farmPosition = W.GetCircularFarmLocation(minionList, 300);

                if (farmPosition.MinionsHit >= Config.Item("LCminions", true).GetValue<Slider>().Value)
                    W.Cast(farmPosition.Position);
            }
        }

        private void CastW(Obj_AI_Base t)
        {
            SebbyLib.Prediction.SkillshotType CoreType2 = SebbyLib.Prediction.SkillshotType.SkillshotLine;

            var predInput2 = new SebbyLib.Prediction.PredictionInput
            {
                Aoe = false,
                Collision = W.Collision,
                Speed = W.Speed,
                Delay = W.Delay,
                Range = W.Range,
                From = Player.ServerPosition,
                Radius = W.Width,
                Unit = t,
                Type = CoreType2
            };

            var poutput2 = SebbyLib.Prediction.Prediction.GetPrediction(predInput2);

            if (poutput2.Hitchance >= SebbyLib.Prediction.HitChance.High)
            {
                W.Cast(poutput2.CastPosition);
            }
        }

        private void SetMana()
        {
            if ((Config.Item("manaDisable", true).GetValue<bool>() && Program.Combo) || Player.HealthPercent < 20)
            {
                QMANA = 0;
                WMANA = 0;
                EMANA = 0;
                RMANA = 0;
                return;
            }

            QMANA = Q.Instance.ManaCost;
            WMANA = W.Instance.ManaCost;
            EMANA = E.Instance.ManaCost;

            if (!R.IsReady())
                RMANA = WMANA - Player.PARRegenRate * W.Instance.Cooldown;
            else
                RMANA = R.Instance.ManaCost;
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (Config.Item("wRange", true).GetValue<bool>())
            {
                if (Config.Item("onlyRdy", true).GetValue<bool>())
                {
                    if (W.IsReady())
                        Utility.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Orange, 1, 1);
                }
                else
                    Utility.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Orange, 1, 1);
            }
        }
    }
}
