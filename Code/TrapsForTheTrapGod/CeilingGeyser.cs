using ColonyLib.Debug;
using Terraria;
using Terraria.ID;
using Terraria.WorldBuilding;

namespace SimplerTraps.TrapsForTheTrapGod.Default;

public class CeilingGeyser : TrapForTheTrapGod
{
	public override bool? TryPlace(int plateX,int plateY,ref int trapX,ref int trapY,ref int plateStyle)
	{
		if (trapY<GenVars.lavaLine) return false;
		while (!WorldGen.SolidOrSlopedTile(trapX,trapY))
		{
			trapY--;
			if (trapY<GenVars.lavaLine||plateY-trapY>20) return false;
		}
		if (plateY-trapY<5) return false;
		if (!WorldGen.SolidTileAllowTopSlope(trapX,trapY)) return false;
		trapY++;

		bool toLeft=!WorldGen.SolidOrSlopedTile(trapX-1,trapY)&&WorldGen.SolidTileAllowTopSlope(trapX-1,trapY-1);
		bool toRight=!WorldGen.SolidOrSlopedTile(trapX+1,trapY)&&WorldGen.SolidTileAllowTopSlope(trapX+1,trapY-1);

		if (!toLeft&&!toRight) return false;

		if (toLeft&&(!toRight||WorldGen.genRand.NextBool())) trapX--;

		for (int clearY=trapY;clearY<plateY;clearY++)
		{
			WorldGen.KillTile(trapX,clearY);
			WorldGen.KillTile(trapX+1,clearY);
		}

		int variant=WorldGen.genRand.Next(2,4);
		for (int placeX=0;placeX<2;placeX++)
		{
			var placedTile=Main.tile[trapX+placeX,trapY];
			placedTile.ClearTile();
			placedTile.HasTile=true;
			placedTile.TileType=TileID.GeyserTrap;
			placedTile.TileFrameX=(short)((variant*2+placeX)*18);
			placedTile.TileFrameY=0;
		}
		
		ColonyDebugSystem.AddWorldGenMarker(new(trapX,trapY),"Terraria/Images/Item_"+ItemID.GeyserTrap);

		return true;
	}
}