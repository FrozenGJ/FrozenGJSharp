using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;
using Color = System.Drawing.Color;
using Font = SharpDX.Direct3D9.Font;
using global::SharpDX;
using global::SharpDX.Direct3D9;

namespace LeagueSharp.Common {
	public static class FrozenGJ {
		

		public static Font DrawFont { get; set; } = new Font(Drawing.Direct3DDevice, new FontDescription
		{
			FaceName = "微软雅黑",
			Height = 28,
			OutputPrecision = FontPrecision.Default,
			Quality = FontQuality.Default
		});

		public static void DrawText(string content, float x, float y, Color color = default(Color), bool percent = false) {
			if (percent)
			{
				DrawFont.DrawText(null, content,
					(int)(Drawing.Width * x / 100),
					(int)(Drawing.Height * y / 100),
					new ColorBGRA(color.B, color.G, color.R, color.A));
			}
			else
			{
				DrawFont.DrawText(null, content, (int)x, (int)y, new ColorBGRA(color.B, color.G, color.R, color.A));
			}
		}

		public static void DrawText(this Font font, Sprite sprite, string text, int x, int y, ColorBGRA color) {
			font.DrawText(sprite, MultiLanguage._(text), x, y, color);
		}

		public static void News(string news,Color color = default(Color), FontStlye fontStlye = FontStlye.Bold)
		{
			if (color == default(Color))
			{
				color = Color.Goldenrod;
			}
			string msg = news.AddBlank().ToHtml(color, fontStlye);
			
			Game.PrintChat("====[FrozenGJ新闻]：=============".ToHtml(Color.Goldenrod) + msg);
			Game.PrintChat(msg);
			Game.PrintChat("=================================".ToHtml(Color.Goldenrod) + msg);
		}

		public static void Info(string assemblyName, string info = "已加载。。。　", int delay = 0) {
			var start = "FrozenGJ".ToHtml(Color.RoyalBlue, FontStlye.Bold);
			var end = info.Equals("已加载。。。　")
				? (assemblyName + "已加载。。。　").ToHtml(Color.Goldenrod, FontStlye.Cite)
				: ("[".ToHtml(Color.RoyalBlue) + assemblyName.ToHtml(Color.Goldenrod, FontStlye.Cite) + "]".ToHtml(Color.RoyalBlue)) + " " + info.AddBlank().ToUTF8();

			if (delay == 0)
			{
				Game.PrintChat($"{start} - {end}");
			}
			else
			{
				Utility.DelayAction.Add(delay, () =>
				{
					Game.PrintChat($"{start} - {end}");
				});
			}
		}

		public static async Task<string> FetchNews(string url = "https://raw.githubusercontent.com/FrozenGJ/FrozenGJSharp/master/A%E6%96%B0%E9%97%BB/NEWS.txt") {
			var News = await Task.Factory.StartNew(
				() =>
				{
					try
					{
						using (var c = new WebClient())
						{
							var news = new WebClient().DownloadString(url);
							return news;
						}
					}

					catch (Exception e)
					{
						Console.WriteLine("抓取新闻失败");
						return "";
					}
				});
			return News;
		}
	}
}
