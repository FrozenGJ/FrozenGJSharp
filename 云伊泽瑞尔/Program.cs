using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Data.Enumerations;
using LeagueSharp.SDK;
using LeagueSharp.SDK.Enumerations;
using LeagueSharp.SDK.UI;
using LeagueSharp.SDK.Utils;
using SharpDX;
using Keys = System.Windows.Forms.Keys;

namespace Ezreal {
	public static class Program
	{

		public static Menu EzMenu;
		public static Menu SpellMenu;
		public static Menu JungleStealerMenu;
		public static Menu DrawMenu;
		public static Menu AntiMeleeMenu;
		public static Orbwalker Orbwalker;
		public static Spell Q, W, E, R;

		private static float DragonDmg;
		private static float DragonTime;
		private static float OverKill = 0;
		public static int Muramana = 3042;
		public static int Tear = 3070;
		public static int Manamune = 3004;
		

		public static Obj_AI_Hero Player => GameObjects.Player;

		static void Main(string[] args)
		{
			Bootstrap.Init(args);
			Events.OnLoad += Events_OnLoad;
		}

		private static void Events_OnLoad(object sender, EventArgs e)
		{
			if (Player.ChampionName != "Ezreal")
			{
				return;
			}
			Orbwalker = Variables.Orbwalker;
			Variables.Orbwalker.Enabled = true;

			//Q = new Spell(SpellSlot.Q, 1170);
			//W = new Spell(SpellSlot.W, 950);
			//E = new Spell(SpellSlot.E, 475);
			//R = new Spell(SpellSlot.R, 3000f);

			Q = new Spell(SpellSlot.Q, 1160);
			W = new Spell(SpellSlot.W, 930);
			E = new Spell(SpellSlot.E, 475);
			R = new Spell(SpellSlot.R, 3000f);

			Q.SetSkillshot(0.25f, 60f, 2000f, true, SkillshotType.SkillshotLine);
			W.SetSkillshot(0.25f, 80f, 1600f, false, SkillshotType.SkillshotLine);
			R.SetSkillshot(1.1f, 160f, 2000f, false, SkillshotType.SkillshotLine);

			FrozenGJ.Info("云Ezreal [SDK]");

			EzMenu = new Menu("云Ezreal", "云Ezreal",true).Attach();

			SpellMenu = EzMenu.Add(new Menu("技能设置", "技能设置"));
			SpellMenu.Add(new MenuBool("EKs", "E抢人头", true));
			SpellMenu.Add(new MenuKeyBind("SemiCastR", "半手动R", Keys.Alt | Keys.R, KeyBindType.Press));
			SpellMenu.Add(new MenuSlider("minR", "R最小距离", 780, 0, 5000));
			SpellMenu.Add(new MenuSlider("maxR", "R最大距离", 3500, 0, 5000));

			JungleStealerMenu = EzMenu.Add(new Menu("抢野怪设置", "抢野怪设置"));
			JungleStealerMenu.Add(new MenuBool("Rjungle", "R抢野", true));
			JungleStealerMenu.Add(new MenuKeyBind("ForceJungle", "强制抢野键", Keys.J, KeyBindType.Press));
			JungleStealerMenu.Add(new MenuBool("Rdragon", "小龙", true));
			JungleStealerMenu.Add(new MenuBool("Rbaron", "大龙", true));
			JungleStealerMenu.Add(new MenuBool("Rred", "红Buff", true));
			JungleStealerMenu.Add(new MenuBool("Rblue", "蓝BUff", true));

			AntiMeleeMenu = EzMenu.Add(new Menu("防近身设置", "防近身设置"));
			AntiMeleeMenu.Add(new MenuBool("AntiMelee", "防止以下敌人近身", true));
			foreach (var enemyHero in GameObjects.EnemyHeroes)
			{
				AntiMeleeMenu.Add(new MenuBool(enemyHero.ChampionName, enemyHero.ChampionName));
			}

			DrawMenu = EzMenu.Add(new Menu("显示设置", "显示设置"));
			DrawMenu.Add(new MenuBool("Qrange", "显示Q范围", true));

			EzMenu.Add(new MenuButton("ResetMenu", "重置菜单", "重置") {Action = ResetMenu});

			Game.OnUpdate += Game_OnUpdate;
			Drawing.OnDraw += Drawing_OnDraw;
			Variables.Orbwalker.OnAction += Orbwalker_OnAction;
			Obj_AI_Base.OnBuffAdd += Obj_AI_Base_OnBuffAdd;
			Events.OnGapCloser += Events_OnGapCloser;
		}

		private static void ResetMenu()
		{
			SpellMenu["EKs"].GetValue<MenuBool>().Value = true;
			SpellMenu["SemiCastR"].GetValue<MenuKeyBind>().Key = Keys.Alt | Keys.R;
			SpellMenu["SemiCastR"].GetValue<MenuKeyBind>().Active = false;
			SpellMenu["minR"].GetValue<MenuSlider>().Value = 780;
			SpellMenu["maxR"].GetValue<MenuSlider>().Value = 3500;

			JungleStealerMenu["Rjungle"].GetValue<MenuBool>().Value = true;
			JungleStealerMenu["ForceJungle"].GetValue<MenuKeyBind>().Key = Keys.J;
			JungleStealerMenu["ForceJungle"].GetValue<MenuKeyBind>().Active = false;
			JungleStealerMenu["Rdragon"].GetValue<MenuBool>().Value = true;
			JungleStealerMenu["Rbaron"].GetValue<MenuBool>().Value = true;
			JungleStealerMenu["Rred"].GetValue<MenuBool>().Value = true;
			JungleStealerMenu["Rblue"].GetValue<MenuBool>().Value = true;

			AntiMeleeMenu["AntiMelee"].GetValue<MenuBool>().Value = true;
			foreach (var enemyHero in GameObjects.EnemyHeroes)
			{
				AntiMeleeMenu[enemyHero.ChampionName].GetValue<MenuBool>().Value = false;
			}

			DrawMenu["Qrange"].GetValue<MenuBool>().Value = true;
		}

		private static void Events_OnGapCloser(object sender, Events.GapCloserEventArgs e) {
			if (e.Sender.IsEnemy && (e.IsDirectedToPlayer || e.Target.IsMe) && E.IsReady())
			{
				var position = GetSafeDashPosition(false);
				if (position.IsZero)
				{
					position = GetCursorDashPosition(false);
				}
				if (!position.IsZero)
				{
					E.Cast(position);
				}
			}
		}

		private static void Obj_AI_Base_OnBuffAdd(Obj_AI_Base sender, Obj_AI_BaseBuffAddEventArgs args) {
			if (sender.IsMe && E.IsReady() && (args.Buff.Name == "ThreshQ" || args.Buff.Name == "rocketgrab2"))
			{
				CastDashSpell();
			}
		}

		private static void Orbwalker_OnAction(object sender, OrbwalkingActionArgs e) {
			if (e.Type == OrbwalkingType.AfterAttack 
				&& W.IsReady()
				&& Player.ManaPercent > 30
				&& e.Target.IsValidTarget()
				&& e.Target is Obj_AI_Turret)
			{
				var ally = GameObjects.AllyHeroes
					.Where(a => a.IsValid && !a.IsMe && !a.IsDead && a.DistanceToPlayer() < W.Range)
					.MaxOrDefault(a => a.GetAutoAttackDamage(e.Target as Obj_AI_Turret));
				W.Cast(ally);
			}

			if (e.Type == OrbwalkingType.NonKillableMinion 
				&& e.Target.Type == GameObjectType.obj_AI_Minion 
				&& Q.CanCast(e.Target as Obj_AI_Minion)
				&& Q.GetDamage(e.Target as Obj_AI_Minion) > e.Target.Health)
			{
				Q.Cast(e.Target as Obj_AI_Minion);
			}
		}

		private static void Drawing_OnDraw(EventArgs args) {
			if (DrawMenu["Qrange"].GetValue<MenuBool>().Value && Q.IsReady())
				Render.Circle.DrawCircle(Player.Position, Q.Range, System.Drawing.Color.Gold, 1);
		}

		private static void Game_OnUpdate(EventArgs args)
		{
			JungleStealer();
			QLogic();
			WLogic();
			ELogic();
			RLogic();
		}

		private static void RLogic() {
			R.Range = (500 * R.Level) + 1500;

			if (!R.IsReady() || Player.IsUnderEnemyTurret()) return;

			if (SpellMenu["SemiCastR"].GetValue<MenuKeyBind>().Active)
			{
				var t = Variables.TargetSelector.GetTarget(R);
				if (t.IsValidTarget())
					R.Cast(t);

				//R.CastOnBestTarget(0, true);
			}

			if (Player.CountEnemyHeroesInRange(800)== 0 && Game.Time - OverKill > 0.6)
			{
				R.Range = SpellMenu["maxR"].GetValue<MenuSlider>().Value;
				foreach (var enemy in GameObjects.EnemyHeroes.Where(e => e.IsValidTarget(R.Range)))
				{
					if (GetRdmg(enemy) > enemy.Health)
					{
						R.Cast(enemy);
					}
				}
			}
		}

		private static double GetRdmg(Obj_AI_Base target)
		{
			//return R.GetDamage(target) * 0.9;

			var rDmg = R.GetDamage(target);
			var dmg = 0;

			PredictionOutput output = R.GetPrediction(target,true,-1f, CollisionableObjects.Walls);

			if (output.Hitchance == HitChance.Collision)
			{
				return 0;
			}

			Vector2 direction = output.CastPosition.ToVector2() - Player.Position.ToVector2();
			direction.Normalize();
			foreach (var enemy in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget()))
			{
				PredictionOutput prediction = R.GetPrediction(enemy);
				Vector3 predictedPosition = prediction.CastPosition;
				Vector3 v = output.CastPosition - Player.ServerPosition;
				Vector3 w = predictedPosition - Player.ServerPosition;
				double c1 = Vector3.Dot(w, v);
				double c2 = Vector3.Dot(v, v);
				double b = c1 / c2;
				Vector3 pb = Player.ServerPosition + ((float)b * v);
				float length = Vector3.Distance(predictedPosition, pb);
				if (length < (R.Width + 100 + enemy.BoundingRadius / 2) && Player.Distance(predictedPosition) < Player.Distance(target.ServerPosition))
					dmg++;
			}

			foreach (var minion in GameObjects.EnemyMinions.Where(m => m.IsValidTarget(R.Range)))
			{
				PredictionOutput prediction = R.GetPrediction(minion);
				Vector3 predictedPosition = prediction.CastPosition;
				Vector3 v = output.CastPosition - Player.ServerPosition;
				Vector3 w = predictedPosition - Player.ServerPosition;
				double c1 = Vector3.Dot(w, v);
				double c2 = Vector3.Dot(v, v);
				double b = c1 / c2;
				Vector3 pb = Player.ServerPosition + ((float)b * v);
				float length = Vector3.Distance(predictedPosition, pb);
				if (length < (R.Width + 100 + minion.BoundingRadius / 2) && Player.Distance(predictedPosition) < Player.Distance(target.ServerPosition))
					dmg++;
			}
			//if (Config.Item("debug", true).GetValue<bool>())
			//Game.PrintChat("R collision" + dmg);
			if (dmg == 0)
				return rDmg;
			else if (dmg > 7)
				return rDmg * 0.7;
			else
				return rDmg - (rDmg * 0.1 * dmg);

		}

		private static bool isBigMinion(Obj_AI_Minion target)
		{
			return (target.GetMinionType() & MinionTypes.Super) != 0
			       || (target.GetMinionType() & MinionTypes.Siege) != 0
			       || target.GetJungleType() == JungleType.Large
			       || target.GetJungleType() == JungleType.Legendary;
		}

		private static void QLogic()
		{
			if (!Q.IsReady()) return;
			#region 清线
			if (FarmActive() && Player.ManaPercent > 20)
			{
				var minions = GameObjects.EnemyMinions.Where(m => Q.IsInRange(m)).OrderByDescending(m => m.MaxHealth);
				int orbTargetId = Orbwalker.GetTarget() != null ? Orbwalker.GetTarget().NetworkId : 0;
				var minion = minions
					.Find(m => m.IsValidTarget(Q.Range)
								&& (m.DistanceToPlayer() > Player.GetRealAutoAttackRange(m) || isBigMinion(m))
								&& Q.GetDamage(m) > m.Health);
				if (minion!=null && Q.Cast(minion) == CastStates.SuccessfullyCasted)
				{
					return;
				}
				//  && 
				if ((!Orbwalker.CanAttack || !Player.CanAttack) && Orbwalker.ActiveMode == OrbwalkingMode.LaneClear && Player.ManaPercent>30)
				{
					foreach (var aiMinion in minions.Where(m => m.IsValidTarget(Player.GetRealAutoAttackRange(m))))
					{
						//或者230
						var hpPred = Health.GetPrediction(aiMinion, 250);
						if (hpPred < 20)
							continue;
						var qDmg = Q.GetDamage(aiMinion);
						if (aiMinion != null && hpPred < qDmg 
							&& (orbTargetId != aiMinion.NetworkId || isBigMinion(aiMinion))
							&& Q.CastOn(aiMinion))
						{
								return;
						}
						//ezrealrisingspellforce  Game.Time - GetPassiveTime() > -1.5
						//GetBuffLive("ezrealrisingspellforce") < 1.5
					
						//aiMinion != null
						//	&& (orbTargetId != aiMinion.NetworkId || isBigMinion(aiMinion)
						//	)
						else if (GetBuffLive("ezrealrisingspellforce") < 1.5 || !E.IsReady())
						{
							if (aiMinion.HealthPercent > 80 && Q.CastOn(aiMinion))
							{
									return;
							}
						}
					}
				}

			}
			#endregion

			#region 野怪
			if ((Orbwalker.ActiveMode == OrbwalkingMode.LaneClear
				|| Orbwalker.ActiveMode == OrbwalkingMode.LastHit
				|| Orbwalker.ActiveMode == OrbwalkingMode.Hybrid)
				&& Player.ManaPercent > 30
				)
			{
				var minion = GameObjects.Jungle.Where(j => Q.IsInRange(j)).MaxOrDefault(j => j.MaxHealth);
				if (Q.CastOn(minion))
					return;
			}

			#endregion

			#region 连招

			if (Orbwalker.ActiveMode == OrbwalkingMode.Combo)
			{
				var target = Variables.TargetSelector.GetTarget(Q);
				if (target != null && !hasSpellShild(target) && Q.CastOn(target))
				{
					return;
				}
			}

			#endregion

			foreach (var enemy in GameObjects.EnemyHeroes.Where(e =>  e.IsValidTarget(Q.Range) && !hasSpellShild(e) && !e.IsDead && !e.IsZombie).OrderBy(e => e.Health))
			{
				if (Q.GetDamage(enemy) + W.GetDamage(enemy) > enemy.Health
					&& Q.CastOn(enemy))
				{
					OverKill = Game.Time;
					return;
				}

				if (!enemy.CanMove() && Q.CastOn(enemy))
				{
					return;
				}

				if (CanHarras() && Player.ManaPercent > 35 && Q.CastOn(enemy))
				{
					return;
				}
			}

			if (Variables.TickCount - Q.LastCastAttemptT > 4000 
				&& !Player.HasBuff("Recall") 
				&& (Player.Mana > Player.MaxMana * 0.9 || Player.InShop() && !MenuGUI.IsShopOpen)
				&& Variables.Orbwalker.ActiveMode == OrbwalkingMode.None 
				&& (Items.HasItem(Tear) || Items.HasItem(Manamune)))
			{
				if (Player.HealthPercent>10)
				{
					var target = Variables.TargetSelector.GetTarget(Q);
					if (target != null && !hasSpellShild(target) && Q.CastOn(target))
					{
						return;
					}
					else if(Q.CastOn(GameObjects.EnemyMinions.Find(m=>Q.GetDamage(m)<m.Health || Q.GetDamage(m) + 2 * Player.GetAutoAttackDamage(m) > m.Health)))
					{
						return;
					}
					else if (Q.CastOn(GameObjects.Jungle.Find(m => Q.GetDamage(m) +  Player.GetAutoAttackDamage(m) > m.Health)))
					{
						return;
					}
					else if(Q.Cast(Player.Position.Extend(Game.CursorPos, 500)))
					{
						return;
					}
				}
				else
				{
					Q.Cast(Player.Position.Extend(Game.CursorPos, 500));
				}
				
			}
		}

		public static bool CanHarras() {
			if (!Player.IsWindingUp && !Player.IsUnderAllyTurret() && Orbwalker.CanMove)
				return true;
			else
				return false;
		}

		public static bool CanMove(this Obj_AI_Hero target) {
			if (!target.CanMove || target.MoveSpeed < 50 || target.IsStunned || target.HasBuffOfType(BuffType.Stun) || target.HasBuffOfType(BuffType.Fear) || target.HasBuffOfType(BuffType.Snare) || target.HasBuffOfType(BuffType.Knockup) || target.HasBuff("Recall") ||
				target.HasBuffOfType(BuffType.Knockback) || target.HasBuffOfType(BuffType.Charm) || target.HasBuffOfType(BuffType.Taunt) || target.HasBuffOfType(BuffType.Suppression) || (target.IsCastingInterruptableSpell() && !target.IsMoving))
			{
				return false;
			}
			return true;
		}

		private static float GetBuffLive(string name)
		{
			return
				Player.Buffs
					.Where(buff => buff.Name == name)
					.OrderByDescending(buff => buff.EndTime - Game.Time)
					.Select(buff => buff.EndTime - Game.Time)
					.FirstOrDefault();
		}

		private static float GetPassiveTime() {
			return
				Player.Buffs
					.OrderByDescending(buff => buff.EndTime - Game.Time)
					.Where(buff => buff.Name == "ezrealrisingspellforce")
					.Select(buff => buff.EndTime)
					.FirstOrDefault();
		}

		private static bool FarmActive()
		{
			return Orbwalker.ActiveMode == OrbwalkingMode.LastHit
			       || Orbwalker.ActiveMode == OrbwalkingMode.Hybrid
			       || Orbwalker.ActiveMode == OrbwalkingMode.LaneClear;
		}

		private static void WLogic()
		{
			if(!W.IsReady())return;

			var t = Variables.TargetSelector.GetTarget(W);
			if (t!=null && t.IsValidTarget() && !hasSpellShild(t))
			{
				if (Orbwalker.ActiveMode == OrbwalkingMode.Combo
				    && Player.Mana > R.Instance.ManaCost + W.Instance.ManaCost + E.Instance.ManaCost)
					W.Cast(t);
				else if (FarmActive() && Player.ManaPercent > 80 && CanHarras())
					W.Cast(t);
				else
				{
					var qDmg = Q.GetDamage(t);
					var wDmg = W.GetDamage(t);
					if (wDmg > t.Health)
					{
						W.Cast(t);
						OverKill = Game.Time;
					}
					else if (wDmg + qDmg > t.Health && Q.IsReady())
					{
						W.Cast(t);
					}
				}
			}

			if (Orbwalker.ActiveMode != OrbwalkingMode.None
					&& Player.Mana > R.Instance.ManaCost + W.Instance.ManaCost + E.Instance.ManaCost)
			{
				foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(W.Range) && !CanMove(enemy)))
					W.Cast(enemy);
			}
		}

		private static void ELogic()
		{
			if(!E.IsReady())return;

			if (AntiMeleeMenu["AntiMelee"].GetValue<MenuBool>().Value)
			{
				if (GameObjects.EnemyHeroes.Any(e => AntiMeleeMenu[e.ChampionName].GetValue<MenuBool>().Value && e.IsValidTarget(1000) && e.IsMelee && Player.Distance(Movement.GetPrediction(e, 0.2f).CastPosition) < 250))
				{
					bool b = Orbwalker.ActiveMode == OrbwalkingMode.Combo;

					var safeDashPosition = GetSafeDashPosition(!b);
					if (!safeDashPosition.IsZero)
					{
						E.Cast(safeDashPosition);
					}
				}
			}

			var target = Variables.TargetSelector.GetTarget(1300, DamageType.Physical);

			if (target!=null
				&&Variables.Orbwalker.ActiveMode == OrbwalkingMode.Combo 
				&& SpellMenu["EKs"].GetValue<MenuBool>().Value
				&& Player.HealthPercent > 40
				&& target.DistanceToPlayer() > target.Distance(Game.CursorPos) + 300
				&& Player.GetRealAutoAttackRange(target) < target.DistanceToPlayer()
				&& !Player.IsUnderAllyTurret()
				&& Game.Time - OverKill > 0.3)
			{
				var dashPosition = Player.Position.Extend(Game.CursorPos, E.Range);
				if (dashPosition.CountEnemyHeroesInRange(900) < 3)
				{
					var dmgCombo = 0f;

					if (target.IsValidTarget(950))
					{
						dmgCombo = (float)Player.GetAutoAttackDamage(target) + E.GetDamage(target);
					}

					if (Q.IsReady() && Player.Mana > Q.Instance.ManaCost + E.Instance.ManaCost 
						&& Q.WillHit(dashPosition, Q.GetPrediction(target).UnitPosition))
						dmgCombo = Q.GetDamage(target);

					if (W.IsReady() && Player.Mana > Q.Instance.ManaCost + E.Instance.ManaCost + W.Instance.ManaCost)
					{
						dmgCombo += W.GetDamage(target);
					}

					if (dmgCombo > target.Health && ValidUlt(target))
					{
						E.Cast(dashPosition);
						OverKill = Game.Time;
					}
				}
			}
		}

		public static bool hasSpellShild(Obj_AI_Hero target)
		{
			return target.HasBuffOfType(BuffType.PhysicalImmunity)
				|| target.HasBuffOfType(BuffType.SpellImmunity)
				|| target.IsInvulnerable
				|| target.HasBuffOfType(BuffType.Invulnerability)
				|| target.HasBuffOfType(BuffType.SpellShield)
			;
		}

		public static bool ValidUlt(Obj_AI_Hero target) {
			if (target.HasBuffOfType(BuffType.PhysicalImmunity) 
				|| target.HasBuffOfType(BuffType.SpellImmunity)
				|| target.IsZombie || target.IsInvulnerable 
				|| target.HasBuffOfType(BuffType.Invulnerability) 
				|| target.HasBuff("kindredrnodeathbuff")
				|| target.HasBuffOfType(BuffType.SpellShield))
				return false;
			else
				return true;
		}

		public static void JungleStealer()
		{
			if (!JungleStealerMenu["Rjungle"].GetValue<MenuBool>().Value) return;

			var jungles = GameObjects.Jungle.Where(j => (int)j.GetJungleType() >=2);
			foreach (var jungle in jungles.Where(j => j.Health < j.MaxHealth))
			{
				if (((jungle.SkinName == "SRU_Dragon" && JungleStealerMenu["Rdragon"].GetValue<MenuBool>().Value)
					|| (jungle.SkinName == "SRU_Baron" && JungleStealerMenu["Rbaron"].GetValue<MenuBool>().Value)
					|| (jungle.SkinName == "SRU_Red" && JungleStealerMenu["Rred"].GetValue<MenuBool>().Value)
					|| (jungle.SkinName == "SRU_Blue" && JungleStealerMenu["Rblue"].GetValue<MenuBool>().Value))
					&& 
					(jungle.DistanceToPlayer() > Q.Range && jungle.CountAllyHeroesInRange(1000) == 0
						|| JungleStealerMenu["ForceJungle"].GetValue<MenuKeyBind>().Active
					))
				{
					if (DragonDmg == 0) DragonDmg = jungle.Health;

					if (Game.Time - DragonTime > 3)
					{
						if (DragonDmg - jungle.Health > 0)
						{
							DragonDmg = jungle.Health;
						}
						DragonTime = Game.Time;
					}
					else
					{
						var DmgSec = (DragonDmg - jungle.Health) * (Math.Abs(DragonTime - Game.Time) / 3);
						if (DragonDmg - jungle.Health > 0)
						{

							var timeTravel = GetUltTravelTime(Player, R.Speed, R.Delay, jungle.Position);
							var timeR = (jungle.Health - R.GetDamage(jungle)) / (DmgSec / 3);
							if (timeTravel > timeR)
								R.Cast(jungle.Position);
						}
						else
							DragonDmg = jungle.Health;
					}
				}
			}
		}

		public static bool CastOn(this Spell spell,Obj_AI_Base target)
		{
			return target!=null && target.IsValidTarget(Q.Range) && spell.Cast(target) == CastStates.SuccessfullyCasted;
		}

		private static float GetUltTravelTime(Obj_AI_Hero source, float speed, float delay, Vector3 targetpos) {
			float distance = Vector3.Distance(source.ServerPosition, targetpos);
			float missilespeed = speed;

			return (distance / missilespeed + delay);
		}

		public static void CastDashSpell()
		{
			var position = GetSafeDashPosition();
			if (position.IsZero)
			{
				position = GetSideDashPosition();
				if (position.IsZero)
				{
					position = GetCursorDashPosition();
					if (position.IsZero)
					{
						position = GetCursorDashPosition();
						if (position.IsZero)
						{
							position = Game.CursorPos;
						}
					}
				}
			}
			E.Cast(position);
		}

		public static Vector3 GetCursorDashPosition(bool checkWall = true) {
			var bestpoint = Player.Position.Extend(Game.CursorPos, E.Range);
			return PositionCheck(bestpoint);
		}

		public static Vector3 GetSideDashPosition(bool checkWall = true) {
			Vector3 bestpoint = new Vector3();
			var orbT = Orbwalker.GetTarget();
			if (orbT is Obj_AI_Hero)
			{
				Vector2 start = Player.Position.ToVector2();
				Vector2 end = orbT.Position.ToVector2();
				var dir = (end - start).Normalized();
				var pDir = dir.Perpendicular();

				var rightEndPos = end + pDir * Player.Distance(orbT);
				var leftEndPos = end - pDir * Player.Distance(orbT);

				var rEndPos = new Vector3(rightEndPos.X, rightEndPos.Y, Player.Position.Z);
				var lEndPos = new Vector3(leftEndPos.X, leftEndPos.Y, Player.Position.Z);

				if (Game.CursorPos.Distance(rEndPos) < Game.CursorPos.Distance(lEndPos))
				{
					bestpoint = Player.Position.Extend(rEndPos, E.Range);
				}
				else
				{
					bestpoint = Player.Position.Extend(lEndPos, E.Range);
				}
			}
			return PositionCheck(bestpoint);
		}

		public static Vector3 GetSafeDashPosition(bool checkWall = true)
		{
			var points = CirclePoints(Player.Position,E.Range);
			var bestpoint = Player.Position.Extend(Game.CursorPos, E.Range);
			int enemies = bestpoint.CountEnemyHeroesInRange(350);
			foreach (var point in points)
			{
				int count = point.CountEnemyHeroesInRange(350);
				if (!InAARange(point))
					continue;
				if (point.IsUnderAllyTurret())
				{
					bestpoint = point;
					enemies = count - 1;
				}
				else if (count < enemies)
				{
					enemies = count;
					bestpoint = point;
				}
				else if (count == enemies && Game.CursorPos.Distance(point) < Game.CursorPos.Distance(bestpoint))
				{
					enemies = count;
					bestpoint = point;
				}
			}
			return PositionCheck(bestpoint);
		}

		public static Vector3 PositionCheck(Vector3 bestPosition,bool checkWall = true)
		{
			if (bestPosition.IsZero)
				return Vector3.Zero;

			if ((!checkWall || !HasWall(Player, bestPosition))
				&& !bestPosition.IsUnderEnemyTurret()
				&& bestPosition.CountEnemyHeroesInRange(600) < Math.Min(3, Player.CountEnemyHeroesInRange(400)))
			{
				return bestPosition;
			}
			return new Vector3();
		}

		private static bool InAARange(Vector3 point)
		{
			return Orbwalker.GetTarget() != null && Orbwalker.GetTarget().Type == GameObjectType.obj_AI_Hero
				? point.Distance(Orbwalker.GetTarget().Position) < Player.AttackRange
				: point.CountEnemyHeroesInRange(Player.AttackRange) > 0;
		}

		public static bool HasWall(Obj_AI_Base from, Vector3 to)
		{
			return GetFirstWallPoint(from.Position, to) != null;
		}

		public static Vector2? GetFirstWallPoint(Vector3 from, Vector3 to, float step = 25) {
			return GetFirstWallPoint(from.ToVector2(), to.ToVector2(), step);
		}

		public static Vector2? GetFirstWallPoint(Vector2 from, Vector2 to, float step = 25) {
			var direction = (to - from).Normalized();

			for (float d = 0; d < from.Distance(to); d = d + step)
			{
				var testPoint = from + d * direction;
				var flags = NavMesh.GetCollisionFlags(testPoint.X, testPoint.Y);
				if (flags.HasFlag(CollisionFlags.Wall) || flags.HasFlag(CollisionFlags.Building))
				{
					return from + (d - step) * direction;
				}
			}
			return null;
		}

		public static List<Vector3> CirclePoints(Vector3 position, float radius, float CircleLineSegmentN = 15) {
			var points = new List<Vector3>();
			for (var i = 1; i <= CircleLineSegmentN; i++)
			{
				var angle = i * 2 * Math.PI / CircleLineSegmentN;
				var point = new Vector3(position.X + radius * (float)Math.Cos(angle), position.Y + radius * (float)Math.Sin(angle), position.Z);
				points.Add(point);
			}
			return points;
		}
	}
}
