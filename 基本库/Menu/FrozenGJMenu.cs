using System;
using System.Drawing;
using System.Drawing.Imaging;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;
using Font = SharpDX.Direct3D9.Font;

namespace LeagueSharp.Common {
	internal class FrozenGJMenu {
		public static Menu Menu { get; private set; }
		public static Menu Draw { get; private set; }
		public static Menu PingBlocker { get; private set; }
		public static bool DisableDrawings { get; private set; }
		public static float EndTime { get; private set; }
		public static Menu Info { get; private set; }
		public static bool DrawWaterMark { get; private set; } = true;

		public static Font WaterMarkFont { get; private set; }

		public static void Initialize() {
			WaterMarkFont = new Font(Drawing.Direct3DDevice,
						new FontDescription
						{
							FaceName = "微软雅黑",
							Height = 28,
							OutputPrecision = FontPrecision.Default,
							Quality = FontQuality.Default
						});

			Menu = new Menu("FrozenGJ设置", "Common.FrozenGJ");
			CommonMenu.Instance.AddSubMenu(Menu);

			Info = Menu.AddSubMenu(new Menu("信息设置", "信息设置"));
			Info.AddItem(new MenuItem("水印", "载入屏显示FrozenGJ水印").SetValue(true));
			Info.AddItem(new MenuItem("logo类型", "选择显示的logo的类型").SetValue(new StringList(new[] { "logo1", "logo2" })));
			Info.AddItem(new MenuItem("新闻", "显示FrozenGJ新闻").SetValue(true));
			Info.AddItem(new MenuItem("加群", "点击复制FrozenGJ脚本群号").SetValue(false).DontSave()).ValueChanged += (sender, args) =>
			 {
				 //System.Diagnostics.Process.Start("http://jq.qq.com/?_wv=1027&k=2EAsP8x");
				 System.Windows.Forms.Clipboard.SetText("FrozenGJ脚本群:10642347");
				 Game.PrintChat("[FrozenGJ]：".ToHtml(Color.RoyalBlue) + "群号已经复制，记得出游戏后添加。FrozenGJ脚本群:10642347".ToUTF8());
			 };
			Info.AddItem(new MenuItem("Info0", ""));
			Info.AddItem(new MenuItem("Info1", "版权归属L#，部分代码归代码作者"));
			
			CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
			Drawing.OnDraw += Drawing_OnDraw;
		}

		private static void Drawing_OnDraw(EventArgs args) {
			if (Info.Item("水印").GetValue<bool>() && DrawWaterMark)
			{
				MenuDrawHelper.DrawBox(
				new Vector2(0, Drawing.Height * 48f / 100), 
				Drawing.Width,
				Drawing.Height * 5 / 100,
				Color.Black, 
				1,
				Color.Goldenrod);

				WaterMarkFont.DrawText(null, "FrozenGJ脚本  致力于做到最好！",
					Drawing.Width * 40 / 100,
					Drawing.Height * 49 / 100,
					new ColorBGRA(Color.Goldenrod.R, Color.Goldenrod.G, Color.Goldenrod.B, Color.Goldenrod.A));

				//WaterMarkFont.DrawText(null, "致力于做到最好！",
				//	Drawing.Width * 55 / 100,
				//	Drawing.Height * 49 / 100,
				//	new ColorBGRA(Color.Goldenrod.R, Color.Goldenrod.G, Color.Goldenrod.B, Color.Goldenrod.A));
			}
		}

		private static async void Game_OnGameLoad(EventArgs eventArgs) {
			if (Info.Item("新闻").GetValue<bool>())
			{
				var news = await FrozenGJ.FetchNews();
				if (!string.IsNullOrEmpty(news))
				{
					FrozenGJ.News(news);
				}
			}

			DrawWaterMark = false;
			ShowLogo();

			DisableDrawings = LeagueSharp.Hacks.DisableDrawings;
			EndTime = 0f;

			Draw = Menu.AddSubMenu(new Menu("屏蔽显示", "AsStreamMode"));
			Draw.AddItem(new MenuItem("屏蔽显示", "屏蔽显示").SetValue(new KeyBind('I', KeyBindType.Toggle, LeagueSharp.Hacks.DisableDrawings))).ValueChanged += delegate (object sender, OnValueChangeEventArgs args) {
				LeagueSharp.Hacks.DisableDrawings = args.GetNewValue<KeyBind>().Active;
				DisableDrawings = args.GetNewValue<KeyBind>().Active;
			};
			Draw.AddItem(new MenuItem("商店屏蔽", "购买东西时屏蔽显示").SetValue(true));
			Draw.AddItem(new MenuItem("比分屏蔽", "查看比分时屏蔽显示").SetValue(true));
			Draw.AddItem(new MenuItem("超神屏蔽", "超神屏蔽显示").SetValue(true));
			Draw.AddItem(new MenuItem("连杀人数", "已连杀人数").SetValue(new Slider(0, 0, 8)).SetTooltip("不要轻易改动这个，除非这个已经不准了"));
			Draw.AddItem(new MenuItem("多杀屏蔽", "多杀屏蔽显示").SetValue(true));
			Draw.AddItem(new MenuItem("屏蔽时长", "屏蔽时长(单位：秒)").SetValue(new Slider(4, 0, 9)));
			Draw.AddItem(new MenuItem("32", "屏蔽显示作者：晴依"));

			PingBlocker = Menu.AddSubMenu(new Menu("屏蔽信号", "PingBlocker"));
			PingBlocker.AddItem(new MenuItem("pb0", "允许以下人员发送信号"));
			foreach (var hero in HeroManager.Allies)
			{
				PingBlocker.AddItem(new MenuItem(hero.Name, $"{hero.CnName()} ( {hero.Name.ToGBK()} )").SetValue(true));
			}
			PingBlocker.AddItem(new MenuItem("pb3", ""));
			PingBlocker.AddItem(new MenuItem("pb1", "屏蔽信号不能分辨是脚本打的").SetFontStyle(FontStyle.Regular, SharpDX.Color.Gold));
			PingBlocker.AddItem(new MenuItem("pb2", "还是手动打的，会统一处理").SetFontStyle(FontStyle.Regular, SharpDX.Color.Gold));

			Game.OnPing += Game_OnPing;
			Game.OnNotify += Game_OnNotify;
			Game.OnUpdate += Game_OnUpdate;
			Game.OnStart += Game_OnStart;
		}

		private static void Game_OnUpdate(EventArgs args) {
			LeagueSharp.Hacks.DisableDrawings = DisableDrawings 
				|| Game.Time < EndTime
				|| Draw.Item("商店屏蔽").GetValue<bool>() && MenuGUI.IsShopOpen
				|| Draw.Item("比分屏蔽").GetValue<bool>() && MenuGUI.IsScoreboardOpen;
		}

		private static void Game_OnPing(GamePingEventArgs args) {
			args.Process = PingBlocker.Item(args.Source.Name).GetValue<bool>();
		}

		private static void Game_OnNotify(GameNotifyEventArgs args) {
			if (args.NetworkId == ObjectManager.Player.NetworkId)
			{
				if (args.EventId == GameEventId.OnChampionKill || args.EventId == GameEventId.OnChampionKillPre)
				{
					Draw.Item("连杀人数").SetValue(new Slider(0, 0, 8));
				}
				else if (args.EventId == GameEventId.OnChampionDie)
				{
					int kills = Draw.Item("连杀人数").GetValue<Slider>().Value + 1;
					if (kills >= 8 && Draw.Item("超神屏蔽").GetValue<bool>())
					{
						Draw.Item("连杀人数").SetValue(new Slider(8, 0, 8));
						EndTime = Math.Max(Game.Time + Draw.Item("屏蔽时长").GetValue<Slider>().Value, EndTime);
					}
					else
					{
						Draw.Item("连杀人数").SetValue(new Slider(kills, 0, 8));
					}
				}
				else if (args.EventId == GameEventId.OnChampionTripleKill
					|| args.EventId == GameEventId.OnChampionQuadraKill
					|| args.EventId == GameEventId.OnChampionPentaKill
					|| args.EventId == GameEventId.OnChampionUnrealKill)
				{
					EndTime =  Math.Max(Game.Time + Draw.Item("屏蔽时长").GetValue<Slider>().Value, EndTime);
				}

			}
		}

		private static void Game_OnStart(EventArgs args) {
			Draw.Item("连杀人数").SetValue(new Slider(0, 0, 8));
		}

		private static void ShowLogo() {
			Game.PrintChat("[FrozenGJ]".ToHtml(32) + "-" + " FrozenGJ 脚本群@(10642347)致力于提供最好用，最实用的脚本供中国玩家使用".ToHtml(Color.MediumOrchid));

			var sprite = Info.Item("logo类型").GetValue<StringList>().SelectedIndex == 0
				? new Render.Sprite(Properties.Resources.logo1, new Vector2((Drawing.Width - 560) / 2f, (Drawing.Height - 560) / 2f) - 100)
				: new Render.Sprite(Properties.Resources.logo2, new Vector2((Drawing.Width - 400) / 2f, (Drawing.Height - 400) / 2f) - 100);
			sprite.Add();
			sprite.OnDraw();

			Utility.DelayAction.Add(9000, () => sprite.Remove());
		}

		public static void Shutdown() {
			CustomEvents.Game.OnGameLoad -= Game_OnGameLoad;
			Game.OnPing -= Game_OnPing;
			Game.OnStart -= Game_OnStart;
			Game.OnNotify -= Game_OnNotify;
			Game.OnUpdate -= Game_OnUpdate;
			Drawing.OnDraw -= Drawing_OnDraw;
			Menu.Remove(Menu);
		}
	}
}