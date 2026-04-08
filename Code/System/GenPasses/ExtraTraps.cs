using System;
using ReLogic.Utilities;
using SimplerTraps.TrapsForTheTrapGod;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace SimplerTraps.GenPasses;

public class ExtraTraps() : GenPass(nameof(SimplerTraps)+"/"+nameof(ExtraTraps),50f)
{
	protected override void ApplyPass(GenerationProgress progress,GameConfiguration configuration)
	{
		static bool PlaceExtraTrap(int x,int y)
		{
			if (Vector2D.Distance(new Vector2D(x,y),GenVars.shimmerPosition)<100) return false;

			while (!WorldGen.SolidOrSlopedTile(x,y))
			{
				y++;
				if (y>=Main.maxTilesY-300) return false;
			}
			if (!WorldGen.SolidTileAllowBottomSlope(x,y)) return false;
			y--;
			for (int checkY=0;checkY<3;checkY++)
			{
				if (WorldGen.SolidOrSlopedTile(x,y-checkY)) return false;
			}

			int x2;
			int y2;
			int plateStyle;

			var trapTypes=TrapForTheTrapGod.all;
			Span<bool> triedTraps=stackalloc bool[trapTypes.Count];
			triedTraps.Clear();
			for (int i=0;i<triedTraps.Length;i++)
			{
				int shuffleIndex;
				do
				{
					shuffleIndex=WorldGen.genRand.Next(triedTraps.Length);
				}
				while (triedTraps[shuffleIndex]);

				x2=x;
				y2=y;
				plateStyle=-1;
				var trap=trapTypes[shuffleIndex];
				try
				{
					var attemptResult=trap.TryPlace(x,y,ref x2,ref y2,ref plateStyle);
					if (!attemptResult.HasValue) goto success; //null
					else if (!attemptResult.Value) goto fail; //false
					//true

					WorldGen.KillTile(x,y);
					if (plateStyle<0) plateStyle=WorldGen.genRand.Next(2,4);
					WorldGen.PlaceTile(x,y,TileID.PressurePlates,style:plateStyle);

					for (int wireY=Math.Min(y,y2);wireY<=Math.Max(y,y2);wireY++)
					{
						var wiredTile=Main.tile[x,wireY];
						wiredTile.RedWire=true;
					}
					for (int wireX=Math.Min(x,x2);wireX<=Math.Max(x,x2);wireX++)
					{
						var wiredTile=Main.tile[wireX,y2];
						wiredTile.RedWire=true;
					}

					//For debug, makes those traps easy to find
					success:
					/*
					int radius=Math.Max(Math.Abs(x-x2),Math.Abs(y-y2))+5;
					for (int ry=y-radius;ry<=y+radius;ry++) for (int rx=x-radius;rx<=x+radius;rx++)
					{
						if (!WorldGen.InWorld(rx,ry)) continue;
						var dist=Vector2D.Distance(new(rx,ry),new(x,y));
						if (dist>radius-2&&dist<radius)
						{
							WorldGen.KillTile(rx,ry);
							WorldGen.PlaceTile(rx,ry,TileID.SapphireGemspark);
						}
					}
					*/
					return true;
				}
				catch (Exception e)
				{
					SimplerTraps.Instance.Logger.Error($"Generating trap {trap.FullName} at {x} : {y} threw an exception:\n{e}");
				}
				fail:

				triedTraps[shuffleIndex]=true;
			}
			return false;
		}

		SimplerTrapsSystem.ProgressMessage(nameof(ExtraTraps),progress);

		//original is 0.05
		double num377=Main.maxTilesX*0.02;
		if (WorldGen.noTrapsWorldGen)
		{
			num377=((!WorldGen.tenthAnniversaryWorldGen&&!WorldGen.notTheBees) ? (num377*100.0) : (num377*5.0));
		}
		else if (WorldGen.getGoodWorldGen) num377*=1.5;
		
		//Don't know what this is, but it was in the original
		if (Main.starGame) num377*=Main.starGameMath(0.2);

		int num377Int=(int)num377;
		for (int num378=0;num378<num377Int;num378++)
		{
			progress.Set(num378/num377);
			for (int num379=0;num379<1150;num379++)
			{
				if (WorldGen.noTrapsWorldGen)
				{
					int x=WorldGen.genRand.Next(50,Main.maxTilesX-50);
					int y=WorldGen.genRand.Next(50,Main.maxTilesY-50);
					if (WorldGen.remixWorldGen)
					{
						y=WorldGen.genRand.Next(50,Main.maxTilesY-210);
					}
					if ((y>Main.worldSurface||Main.tile[x,y].WallType!=WallID.None)&&PlaceExtraTrap(x,y)) break;
				}
				else
				{
					int x=WorldGen.genRand.Next(200,Main.maxTilesX-200);
					int y=WorldGen.genRand.Next((int)Main.worldSurface,Main.maxTilesY-210);
					while (WorldGen.oceanDepths(x,y))
					{
						x=WorldGen.genRand.Next(200,Main.maxTilesX-200);
						y=WorldGen.genRand.Next((int)Main.worldSurface,Main.maxTilesY-210);
					}
					if (Main.tile[x,y].WallType==WallID.None&&PlaceExtraTrap(x,y)) break;
				}
			}
		}
	}
}