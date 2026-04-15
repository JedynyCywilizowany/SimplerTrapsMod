using System;
using System.Collections.Generic;
using System.Linq;
using ColonyLib;
using ColonyLib.Debug;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace SimplerTraps.TrapsForTheTrapGod.Default;

public class LavaPit : TrapForTheTrapGod
{
	public override bool? TryPlace(int origX,int origY,ref int trapX,ref int trapY,ref int plateStyle)
	{
		if (origY<=Main.rockLayer) return false;

		var origTile=Main.tile[origX,origY];
		if (origTile.LiquidAmount==0||origTile.LiquidType!=LiquidID.Lava) return false;

		int x2=origX;
		int y2=origY;

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
			if (x2+width>=Main.maxTilesX-10) return false;
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
					var plateTile=Main.tile[plateX,plateY2];
					if (plateTile.HasTile&&plateTile.TileType==TileID.PressurePlates)
					{
						plateTile.YellowWire=true;
						plateCount++;
					}
					else if (WorldGen.genRand.NextBool(5))
					{
						if (!WorldGen.SolidTileAllowBottomSlope(plateX,plateY)) WorldGen.SlopeTile(plateX,plateY);
						WorldGen.KillTile(plateX,plateY2);
						WorldGen.PlaceTile(plateX,plateY2,TileID.PressurePlates,style:7);
						plateTile.YellowWire=true;
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

		ColonyDebug.AddWorldGenMarker(new(x2+(width/2),y2),"Terraria/Images/Item_"+ItemID.LavaBucket);
		
		return null;
	}
}