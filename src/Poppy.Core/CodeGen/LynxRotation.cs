namespace Poppy.Core.CodeGen;

/// <summary>
/// Atari Lynx screen rotation values for the LNX header.
/// </summary>
public enum LynxRotation : byte {
	/// <summary>No rotation (default landscape).</summary>
	None = 0,

	/// <summary>Rotated left 90 degrees.</summary>
	Left = 1,

	/// <summary>Rotated right 90 degrees.</summary>
	Right = 2
}
