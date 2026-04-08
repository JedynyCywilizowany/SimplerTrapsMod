using Colony.Debug;
using Terraria;
using Terraria.ID;
using Terraria.WorldBuilding;

namespace SimplerTraps.TrapsForTheTrapGod.Default;

public class BoulderStatue : TrapForTheTrapGod
{
	public override bool? TryPlace(int plateX,int plateY,ref int trapX,ref int trapY,ref int plateStyle)
	{
		if (trapY<GenVars.rockLayer) return false;
		while (!WorldGen.SolidOrSlopedTile(trapX,trapY))
		{
			trapY--;
			if (trapY<GenVars.rockLayer||plateY-trapY>50) return false;
		}
		if (plateY-trapY<15) return false;
		if (!WorldGen.SolidTileAllowTopSlope(trapX,trapY)) return false;
		trapY++;

		bool toLeft=WorldGen.SolidTileAllowTopSlope(trapX-1,trapY-1);
		bool toRight=WorldGen.SolidTileAllowTopSlope(trapX+1,trapY-1);

		for (int checkY=0;checkY<3;checkY++)
		{
			if (WorldGen.SolidOrSlopedTile(trapX,trapY-checkY)) return false;
			if (WorldGen.SolidOrSlopedTile(trapX-1,trapY-checkY)) toLeft=false;
			if (WorldGen.SolidOrSlopedTile(trapX+1,trapY-checkY)) toRight=false;
		}

		if (!toLeft&&!toRight) return false;

		if (toLeft&&(!toRight||WorldGen.genRand.NextBool())) trapX--;

		for (int clearY=trapY;clearY<plateY;clearY++)
		{
			WorldGen.KillTile(trapX,clearY);
			WorldGen.KillTile(trapX+1,clearY);
		}
		
		for (int placeX=0;placeX<2;placeX++)
		{
			var placedTile=Main.tile[trapX+placeX,trapY];
			placedTile.ClearTile();
			placedTile.HasTile=true;
			placedTile.TileType=TileID.GeyserTrap;
			placedTile.TileFrameX=(short)(placeX*18);
			placedTile.TileFrameY=0;
		}
		//The actuator is needed here, so that WorldGen sees this as a trap (and not an abandoned trigger) and doesn't disarm it
		//For some reason, a Boulder Statue is not seen as a mechanism, but an actuator, even without a solid block, is
		var wireTopTile=Main.tile[trapX,trapY];
		wireTopTile.HasActuator=true;

		ColonyDebugSystem.AddWorldGenMarker(new(trapX,trapY),"Terraria/Images/Item_"+ItemID.BoulderStatue);

		return true;
	}
}