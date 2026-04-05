using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace SimplerTraps.Config;

public class SimplerTrapsConfig : ModConfig
{
	public override ConfigScope Mode=>ConfigScope.ClientSide;

	[DefaultValue(true)]
	public bool NoTrapsScaling{get;set;}

	[Range(0,100)]
	[DefaultValue(50)]
	public int PaintChance{get;set;}

	[Range(0,100)]
	[DefaultValue(4)]
	public int EchoCoatChance{get;set;}

	[Range(0,100)]
	[DefaultValue(10)]
	public int VenomDartTrapChance{get;set;}
}