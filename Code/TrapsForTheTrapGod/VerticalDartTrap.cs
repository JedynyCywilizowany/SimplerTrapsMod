using System;
using Colony.Debug;
using Terraria;
using Terraria.ID;

namespace SimplerTraps.TrapsForTheTrapGod.Default;

public class VerticalDartTrap : TrapForTheTrapGod
{
	public override bool? TryPlace(int plateX,int plateY,ref int trapX,ref int trapY,ref int plateStyle)
	{
		trapX+=WorldGen.genRand.Next(-1,2);
		if (WorldGen.SolidOrSlopedTile(trapX,trapY)) return false;
		
		//-1 for above the pressure plate, 1 for below
		int direction=(WorldGen.genRand.NextBool() ? -1 : 1);

		while (!WorldGen.SolidOrSlopedTile(trapX,trapY))
		{
			trapY+=direction;
			if (trapY>=Main.maxTilesY-300||trapY<Main.worldSurface) return false;
		}

		if ((trapY<=plateY&&plateY-trapY<=3)) return false;
		if (Math.Abs(trapY-plateY)>10) return false;

		WorldGen.KillTile(trapX,trapY);
		WorldGen.PlaceTile(trapX,trapY,TileID.Traps,style:0); //Dart Trap
		bool rightOrientation;
		if (trapX<plateX) rightOrientation=true;
		else if (trapX>plateX) rightOrientation=false;
		else rightOrientation=WorldGen.genRand.NextBool();
		Main.tile[trapX,trapY].TileFrameX=(direction==1 ?
			(rightOrientation ? (short)54 : (short)36) :
			(rightOrientation ? (short)72 : (short)90)
		);

		ColonyDebugSystem.AddWorldGenMarker(new(trapX,trapY),"Terraria/Images/Item_"+ItemID.DartTrap);
		
		return true;
	}
}