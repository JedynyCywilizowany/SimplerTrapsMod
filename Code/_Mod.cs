using Colony;
using Terraria.ModLoader;

namespace SimplerTraps;

public partial class SimplerTraps : Mod
{
	public override string Name=>nameof(SimplerTraps);
	public static SimplerTraps Instance=>ModContent.GetInstance<SimplerTraps>();
	public override void Load()
	{
		LoadILEditsAndDetours();
	}
	public override void Unload()
	{
		this.AutoUnload();
	}
}