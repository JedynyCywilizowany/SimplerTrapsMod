using System;
using System.Collections.Generic;
using System.Linq;
using CywilizowanysMod.Common;
using Microsoft.Xna.Framework;
using ReLogic.Utilities;
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
			if (!WorldGen.SolidTile(x,y)) return false;
			y--;
			int x2;
			int y2;

			var plateTile=Main.tile[x,y];
			if (plateTile.LiquidAmount!=0&&plateTile.LiquidType==LiquidID.Lava)
			{
				if (y<=Main.rockLayer) return false;
				//Lava pit trap
				x2=x;
				y2=y;

				while (Main.tile[x2,y2].LiquidAmount!=0)
				{
					y2--;
					if (y2<=Main.rockLayer) return false;
				}
				y2++;
				while (Main.tile[x2,y2].LiquidAmount!=0)
				{
					x2--;
					if (x2<10) return false;
				}
				x2++;

				int width=0;
				while (Main.tile[x2+width,y2].LiquidAmount!=0)
				{
					width++;
					if (x2>=Main.maxTilesX-10) return false;
				}
				width--;
				const int MinWidth=5;
				const int MaxWidth=25;
				if (width<MinWidth) return false;
				if (width>MaxWidth)
				{
					x2+=WorldGen.genRand.Next(width-MaxWidth+1);
					width=MaxWidth;
				}

				Span<int> heights=stackalloc int[width];
				heights.Clear();
				for (int checkX=0;checkX<width;checkX++)
				{
					bool reached=false;
					for (int checkY=0;checkY<=30;checkY++)
					{
						int checkX2=x2+checkX;
						int checkY2=y2-checkY;
						if (reached)
						{
							if (!WorldGen.SolidOrSlopedTile(checkX2,checkY2))
							{
								if (Main.tile[checkX2,checkY2].LiquidAmount==0)
								{
									for (int freeSpaceCheck=0;freeSpaceCheck<3;freeSpaceCheck++)
									{
										if (WorldGen.SolidOrSlopedTile(checkX2,checkY2-freeSpaceCheck)) goto heightCheckNoSpace;
									}

									heights[checkX]=checkY;
								}
								break;
								heightCheckNoSpace:;
							}
						}
						else if (WorldGen.SolidOrSlopedTile(checkX2,checkY2)) reached=true;
					}
				}

				int leftExcess=0;
				int rightExcess=0;
				while (leftExcess<heights.Length&&heights[leftExcess]==0) leftExcess++;
				while (rightExcess<heights.Length&&heights[^(rightExcess+1)]==0) rightExcess++;
				width-=leftExcess+rightExcess;
				x2+=leftExcess;
				if (width<MinWidth) return false;
				heights=heights.Slice(leftExcess,width);

				int topHeight=0;
				foreach (var h in heights) if (h>topHeight) topHeight=h;

				int currentChunk=0;
				Dictionary<Point,int> terrainChunks=new();
				for (int checkX=x2;checkX<x2+width;checkX++) for (int checkY=y2;checkY>y2-topHeight;checkY--)
				{
					bool RegisterChunk(int cx,int cy)
					{
						if (cx>=x2&&cx<x2+width&&cy<y2&&cy>y2-topHeight&&WorldGen.SolidOrSlopedTile(cx,cy))
						{
							if (terrainChunks.TryAdd(new(cx,cy),currentChunk))
							{
								RegisterChunk(cx-1,cy);
								RegisterChunk(cx,cy-1);
								RegisterChunk(cx+1,cy);
								RegisterChunk(cx,cy+1);
								return true;
							}
						}
						return false;
					}
					if (WorldGen.SolidOrSlopedTile(checkX,checkY))
					{
						if (RegisterChunk(checkX,checkY)) currentChunk++;
					}
				}

				int plateCount;
				int plateSpace;
				do
				{
					plateCount=0;
					plateSpace=0;
					foreach (var entry in terrainChunks.Keys)
					{
						int plateX=entry.X;
						int plateY=entry.Y;
						int plateY2=entry.Y-1;
						if (WorldGen.SolidOrSlopedTile(plateX,plateY)&&!WorldGen.SolidOrSlopedTile(plateX,plateY2))
						{
							plateSpace++;
							var plateTile2=Main.tile[plateX,plateY2];
							if (plateTile2.HasTile&&plateTile2.TileType==TileID.PressurePlates)
							{
								plateTile2.YellowWire=true;
								plateCount++;
							}
							else if (WorldGen.genRand.NextBool(5))
							{
								if (!WorldGen.SolidTileAllowBottomSlope(plateX,plateY)) WorldGen.SlopeTile(plateX,plateY);
								WorldGen.KillTile(plateX,plateY2);
								WorldGen.PlaceTile(plateX,plateY2,TileID.PressurePlates,style:7);
								plateTile2.YellowWire=true;
								plateCount++;
							}
						}
					}
				}
				while (plateCount<Math.Max(1,plateSpace/5));

				foreach (var entry in terrainChunks.Keys)
				{
					var wireTile=Main.tile[entry];
					wireTile.RedWire=false;
					wireTile.BlueWire=false;
					wireTile.GreenWire=false;
					wireTile.YellowWire=true;
					wireTile.HasActuator=true;
				}
				var orderedChunks=terrainChunks.OrderBy((p)=>p.Key.Y).OrderBy((p)=>p.Key.X);
				var connectedChunks=new bool[currentChunk,currentChunk];
				for (int i=0;i<currentChunk;i++) for (int j=0;j<currentChunk;j++)
				{
					Point from=Point.Zero;
					Point to=new(ushort.MaxValue,ushort.MaxValue);
					foreach (var fromEntry in orderedChunks) foreach (var toEntry in orderedChunks)
					{
						if (!connectedChunks[fromEntry.Value,toEntry.Value])
						{
							if (fromEntry.Key.ManhattanDistance(toEntry.Key)<from.ManhattanDistance(to))
							{
								from=fromEntry.Key;
								to=toEntry.Key;
							}
						}
					}
					if (from!=Point.Zero)
					{
						for (int wireX=Math.Min(from.X,to.X);wireX<=Math.Max(from.X,to.X);wireX++)
						{
							var wiredTile=Main.tile[wireX,from.Y];
							wiredTile.YellowWire=true;
						}
						for (int wireY=Math.Min(from.Y,to.Y);wireY<=Math.Max(from.Y,to.Y);wireY++)
						{
							var wiredTile=Main.tile[to.X,wireY];
							wiredTile.YellowWire=true;
						}

						int connectedFrom=terrainChunks[from];
						int connectedTo=terrainChunks[to];
						connectedChunks[connectedFrom,connectedFrom]=true;
						connectedChunks[connectedTo,connectedTo]=true;
						for (int c=0;c<currentChunk;c++)
						{
							connectedChunks[c,connectedTo]=(connectedChunks[connectedTo,c]|=connectedChunks[connectedFrom,c]);
							connectedChunks[c,connectedFrom]=(connectedChunks[connectedFrom,c]|=connectedChunks[c,connectedTo]);
						}
					}
				}
				
				goto success;
			}

			for (int checkY=y;checkY>y-3;checkY--)
			{
				if (WorldGen.SolidOrSlopedTile(x,checkY)) return false;
			}
			x2=x+WorldGen.genRand.Next(-1,2);
			y2=y;
			if (WorldGen.SolidOrSlopedTile(x2,y2)) return false;
			//-1 for above the pressure plate, 1 for below
			int direction=(WorldGen.genRand.NextBool() ? -1 : 1);

			while (!WorldGen.SolidOrSlopedTile(x2,y2))
			{
				y2+=direction;
				if (y2>=Main.maxTilesY-300||y<Main.worldSurface) return false;
			}

			if ((y2<y&&y-y2<3)) return false;
			if (direction==-1&&y-y2<=20&&y2>GenVars.lavaLine&&WorldGen.genRand.NextBool())
			{
				//Upside-down geyser trap
				int startX;
				int startY=y2+1;
				if (x2>x)
				{
					startX=x;
					if (WorldGen.genRand.NextBool()) startX--;
				}
				else startX=x2;

				if (!(WorldGen.noTrapsWorldGen&&(WorldGen.tenthAnniversaryWorldGen||WorldGen.notTheBees)&&(WorldGen.genRand.NextBool(3)||WorldGen.IsTileNearby(x2,y2,TileID.GeyserTrap,30))))
				{
					if (WorldGen.SolidTile(startX,y2)&&WorldGen.SolidTile(startX+1,y2))
					{
						for (int checkY=startY;checkY<y-1;checkY++)
						{
							WorldGen.KillTile(startX,checkY);
							WorldGen.KillTile(startX+1,checkY);
						}
						int variant=WorldGen.genRand.Next(2,4);
						for (int placeX=0;placeX<2;placeX++)
						{
							var placedTile=Main.tile[startX+placeX,startY];
							placedTile.ClearTile();
							placedTile.HasTile=true;
							placedTile.TileType=TileID.GeyserTrap;
							placedTile.TileFrameX=(short)((variant*2+placeX)*18);
							placedTile.TileFrameY=0;
						}
						x2=x;
						y2=startY;

						goto pressurePlate;
					}
				}
			}
			if (direction==-1&&y-y2>20&&y-y2<=50)
			{
				//Boulder statue trap
				int startX;
				int startY=y2+1;
				if (x2>x)
				{
					startX=x;
					if (WorldGen.genRand.NextBool()) startX--;
				}
				else startX=x2;

				if (WorldGen.SolidTile(startX,y2)&&WorldGen.SolidTile(startX+1,y2))
				{
					for (int checkY=startY;checkY<y-1;checkY++)
					{
						WorldGen.KillTile(startX,checkY);
						WorldGen.KillTile(startX+1,checkY);
					}
					for (int placeX=0;placeX<2;placeX++) for (int placeY=0;placeY<3;placeY++)
					{
						var placedTile=Main.tile[startX+placeX,startY+placeY];
						placedTile.ClearTile();
						placedTile.HasTile=true;
						placedTile.TileType=TileID.BoulderStatue;
						placedTile.TileFrameX=(short)(placeX*18);
						placedTile.TileFrameY=(short)(placeY*18);
					}
					x2=x;
					y2=startY+1;
					//The actuator is needed here, so that WorldGen sees this as a trap (and not an abandoned trigger) and doesn't disarm it
					//For some reason, a Boulder Statue is not seen as a mechanism, but an actuator, even without a solid block, is
					var wireTopTile=Main.tile[x2,y2];
					wireTopTile.HasActuator=true;

					goto pressurePlate;
				}
			}
			//Vertical dart trap
			if ((y2<y&&y-y2<3)||Math.Abs(y2-y)>10) return false;
			WorldGen.KillTile(x2,y2);
			WorldGen.PlaceTile(x2,y2,TileID.Traps,style:0);
			bool rightOrientation;
			if (x2<x) rightOrientation=true;
			else if (x2>x) rightOrientation=false;
			else rightOrientation=WorldGen.genRand.NextBool();
			Main.tile[x2,y2].TileFrameX=(direction==1 ?
				(rightOrientation ? (short)54 : (short)36) :
				(rightOrientation ? (short)72 : (short)90)
			);

			pressurePlate:
			WorldGen.KillTile(x,y);
			WorldGen.PlaceTile(x,y,TileID.PressurePlates,style:WorldGen.genRand.Next(2,4));

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