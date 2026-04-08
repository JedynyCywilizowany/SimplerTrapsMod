using System;
using System.Reflection;
using ColonyLib;
using MonoMod.Cil;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SimplerTraps;

partial class SimplerTraps : Mod
{
	private static void LoadILEditsAndDetours()
	{
		try
		{
			IL_WorldGen.placeTrap+=(il)=>
			{
				static void TileRemoveReplacement(int x,int y)
				{
					var tile=Main.tile[x,y];
					if (tile.HasTile&&Main.tileSolid[tile.TileType])
					{
						tile.RedWire=true;
						tile.HasActuator=true;
					}
				}
				ILCursor c=new(il);
				c.GotoNext((ins)=>ins.MatchSwitch(out _));
				c.Next!.MatchSwitch(out ILLabel[]? switchOpts);

				c.Next=switchOpts![1].Target;
				if (c.TryGotoNext(MoveType.After,
					(ins)=>ins.MatchCall(typeof(Tile).GetMethod("get_type",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)!),
					(ins)=>ins.MatchLdcI4(TileID.Stone),
					(ins)=>ins.MatchStindI2()
				))
				{
					c.Index--;
					c.Remove();
					c.EmitPop();
					c.EmitPop();
				}
				if (c.TryGotoPrev(MoveType.Before,
					(ins)=>ins.MatchLdcI4(out _),
					(ins)=>ins.MatchLdcI4(out _),
					(ins)=>ins.MatchLdcI4(out _),
					(ins)=>ins.MatchCall(typeof(WorldGen).GetMethod(nameof(WorldGen.KillTile))!)
				))
				{
					c.Prev.MatchLdloc(out int yVarIndex);

					var c2=c.Clone();
					c2.GotoPrev(MoveType.Before,
						(ins)=>ins.MatchLdloc(out _),
						(ins)=>ins.MatchStloc(yVarIndex)
					);
					c2.Next!.MatchLdloc(out int startYVarIndex);

					c.RemoveRange(4);
					c.EmitLdloc(startYVarIndex);
					static void Insertion(int x,int y,int startY)
					{
						if (y<startY+2) WorldGen.KillTile(x,y);
						else TileRemoveReplacement(x,y);
					}
					c.EmitCallFromDelegate(Insertion);
				}

				c.Next=switchOpts[1].Target;
				while (c.TryGotoNext(
					(ins)=>ins.MatchLdcI4(TileID.ActiveStoneBlock),
					(ins)=>ins.MatchLdcI4(out _),
					(ins)=>ins.MatchLdcI4(out _),
					(ins)=>ins.MatchLdcI4(out _),
					(ins)=>ins.MatchLdcI4(out _),
					(ins)=>ins.MatchCall(typeof(WorldGen).GetMethod(nameof(WorldGen.PlaceTile))!),
					(ins)=>ins.MatchPop()
				))
				{
					c.RemoveRange(7);
					c.EmitCallFromDelegate(TileRemoveReplacement);
				}
			};
		}
		catch (Exception e)
		{
			Instance.Logger.Error(e.Message);
		}
		/*
		for (int i=0;i<il.Instrs.Count;i++) il.Instrs[i].Offset=i;
		MonoModHooks.DumpIL(Instance,c.Context);
		*/
	}
}