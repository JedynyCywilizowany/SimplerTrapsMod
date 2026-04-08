using System.Collections.Generic;
using Terraria.ModLoader;

namespace SimplerTraps.TrapsForTheTrapGod;

public abstract class TrapForTheTrapGod : ModType
{
	internal static List<TrapForTheTrapGod> all=new();

	protected sealed override void Register()
	{
		all.Add(this);
	}
	public sealed override void SetupContent()
	{
		SetStaticDefaults();
	}
	protected sealed override void ValidateType()
	{
	}

	/// <summary>
	/// Return:<br/>
	/// false - fail, this trap will not be generated here<br/>
	/// true - success, automatically place wire and pressure plate (to be used in most cases)<br/>
	/// null - success, but don't automatically place anything (useful when placing pressure plates and wires manually)<br/>
	/// <br/>
	/// If the attempt fails, another trap will try to place itself on this position, so don't place anything until you're sure this trap will properly generate
	/// </summary>
	/// <param name="plateX">Horizontal position of the trap's origin, where the pressure plate will be generated when returning true</param>
	/// <param name="plateY">Vertical position of the trap's origin, where the pressure plate will be generated when returning true</param>
	/// <param name="trapX">Horizontal position of the trap, where the wire from the pressure plate will be connected when returning true</param>
	/// <param name="trapY">Vertical position of the trap, where the wire from the pressure plate will be connected when returning true</param>
	/// <param name="plateStyle">
	/// The style of the pressure plate when returning true<br/>
	/// If below 0, randomly 2 or 3 (Gray/Brown). defaults to -1<br/>
	/// 7 (Orange) is recommended for single-use traps, as it breaks when activated
	/// </param>
	public abstract bool? TryPlace(int plateX,int plateY,ref int trapX,ref int trapY,ref int plateStyle);
}