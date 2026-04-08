using System.Collections.Generic;
using Colony;
using SimplerTraps.GenPasses;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace SimplerTraps;

public class SimplerTrapsSystem : ModSystem
{
	public override void PostSetupContent()
	{
		TileID.Sets.IsAMechanism.RevertibleModify(TileID.BoulderStatue,true);
	}
	public override void ModifyWorldGenTasks(List<GenPass> tasks,ref double totalWeight)
	{
		int index;

		index=tasks.FindIndex((p)=>p.Name=="Traps"&&p.Enabled);
		if (index>=0) tasks.Insert(index+1,new ExtraTraps());

		index=tasks.FindIndex((p)=>p.Name=="Remove Broken Traps");
		if (index>=0) tasks.InsertRange(index+1,
		[
			new UpgradeTraps(),
			new StructureTraps()
		]);
	}
	internal static void ProgressMessage(string passName,GenerationProgress progress)
	{
		progress.Message=SimplerTraps.Instance.GetLocalization("WorldGenMessage."+(WorldGen.noTrapsWorldGen ? "NoTraps." : "")+passName).Value;
	}
}