using System;
using System.Collections.Generic;
using System.Linq;
using Colony;
using Microsoft.Xna.Framework;
using SimplerTraps.Config;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.IO;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Terraria.WorldBuilding;

namespace SimplerTraps.GenPasses;

public class UpgradeTraps() : GenPass(nameof(SimplerTraps)+"/"+nameof(UpgradeTraps),100f)
{
	protected override void ApplyPass(GenerationProgress progress,GameConfiguration configuration)
	{
		SimplerTrapsSystem.ProgressMessage(nameof(UpgradeTraps),progress);

		var config=ModContent.GetInstance<SimplerTrapsConfig>();

		//For now it's that simple, but it's better to keep this as a function in case the IDs get modified
		static IEnumerable<int> UsedPaints()
		{
			for (int i=PaintID.DeepRedPaint;i<=PaintID.ShadowPaint;i++) yield return i;
		}
		static bool Paintable(int type)
		{
			return (TileID.Sets.Boulders[type]&&type!=TileID.RollingCactus)||((TileID.Sets.IsAMechanism[type]||TileID.Sets.IsATrigger[type])&&type!=TileID.ActiveStoneBlock&&type!=TileID.InactiveStoneBlock)||type==TileID.BoulderStatue;
		}
		bool ScaledChance(int chance,int extraRolls)
		{
			if (config.NoTrapsScaling&&WorldGen.noTrapsWorldGen)
			{
				for (int i=0;i<extraRolls;i++) if (WorldGen.genRand.NextBool(chance,100)) return true;
			}
			return WorldGen.genRand.NextBool(chance,100);
		}
		
		List<KeyValuePair<int,Color>> paintColorLookup=new();
		if (config.PaintChance!=0)
		{
			paintColorLookup.Add(default);
			foreach (var paintId in UsedPaints())
			{
				//I found no better way to check a paint's color than what it looks like on the map
				var mapTile=MapTile.Create(MapHelper.tileLookup[TileID.Stone],byte.MaxValue,(byte)paintId);
				paintColorLookup.Add(new(paintId,MapHelper.GetMapTileXnaColor(ref mapTile)));
			}
		}

		List<Point> adjacentTiles=new();
		var colorDifference=new int[paintColorLookup.Count];
		List<int> possibilities=new();

		for (int y=Main.maxTilesY-1;y>0;y--)
		{
			progress.Set(1-((double)y/Main.maxTilesY));

			for (int x=Main.maxTilesX-1;x>0;x--)
			{
				var tile=Main.tile[x,y];
				if (tile.HasTile&&tile.WallType!=WallID.LihzahrdBrickUnsafe&&TileObjectData.TopLeft(x,y)==new Point16(x,y))
				{
					int type=tile.TileType;

					if (type==TileID.Traps&&tile.TileFrameY==0&&ScaledChance(config.VenomDartTrapChance,5))
					{
						//Venom dart trap
						int variant=5;
						if (WorldGen.noTrapsWorldGen&&ScaledChance(config.VenomDartTrapChance,1))
						{
							//Super dart trap
							variant=1;
						}
						tile.TileFrameY=(short)(18*variant);
					}

					if (Paintable(type))
					{
						var objectData=TileObjectData.GetTileData(tile);
						int width=objectData?.Width??1;
						int height=objectData?.Height??1;

						byte? changedColor=null;
						bool turnInvisible=false;
						
						if (ScaledChance(config.PaintChance,3))
						{
							adjacentTiles.Clear();
							for (int i=0;i<width;i++)
							{
								int x2=x+i;
								if (WorldGen.SolidTileAllowTopSlope(x2,y-1)) adjacentTiles.Add(new(x2,y-1));
								if (WorldGen.SolidTileAllowBottomSlope(x2,y+height)) adjacentTiles.Add(new(x2,y+height));
							}
							for (int i=0;i<height;i++)
							{
								int y2=y+i;
								if (WorldGen.SolidTileAllowLeftSlope(x-1,y2)) adjacentTiles.Add(new(x-1,y2));
								if (WorldGen.SolidTileAllowRightSlope(x+width,y2)) adjacentTiles.Add(new(x+width,y2));
							}

							if (adjacentTiles.Count!=0)
							{
								var targetColor=ColonyUtils.GetMapColorTile(Main.tile[WorldGen.genRand.Next(adjacentTiles)].TileType);

								Array.Clear(colorDifference);
								paintColorLookup[0]=new(PaintID.None,ColonyUtils.GetMapColorTile(type));
								int colorDiffI=0;
								if (paintColorLookup[0].Value.A<byte.MaxValue)
								{
									colorDiffI++;
									colorDifference[0]=int.MaxValue;
								}
								for (;colorDiffI<colorDifference.Length;colorDiffI++)
								{
									Color paintColor=paintColorLookup[colorDiffI].Value;
									var dR=(targetColor.R-paintColor.R);
									var dG=(targetColor.G-paintColor.G);
									var dB=(targetColor.B-paintColor.B);
									colorDifference[colorDiffI]=(dR*dR)+(dG*dG)+(dB*dB);
								}

								int closest=colorDifference.Min();
								possibilities.Clear();
								for (int i=0;i<colorDifference.Length;i++)
								{
									if (colorDifference[i]==closest) possibilities.Add(i);
								}

								changedColor=(byte)paintColorLookup[WorldGen.genRand.Next(possibilities)].Key;
							}
						}
						if (ScaledChance(config.EchoCoatChance,4))
						{
							turnInvisible=true;
						}

						for (int yp=0;yp<height;yp++) for (int xp=0;xp<width;xp++)
						{
							var tile2=Main.tile[x+xp,y+yp];
							var paint=tile2.BlockColorAndCoating();

							if (changedColor.HasValue) paint.Color=changedColor.Value;
							if (turnInvisible) paint.Invisible=true;

							paint.ApplyToBlock(tile2);
						}
					}
				}
			}
		}
	}
}