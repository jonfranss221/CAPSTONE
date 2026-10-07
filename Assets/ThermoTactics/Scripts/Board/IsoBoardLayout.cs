using UnityEngine;

/// <summary>
/// Isometric (2:1) math for the Baguio board art.
/// Art: 1248 x 576 px at 64 PPU, centered on (0,0). Board = 6 lanes x 6 cells.
///   a = position ALONG a lane (0 = ally end, lower-left; 6 = enemy end, upper-right)
///   b = which lane (0 = upper-left lane ... 6 = lower-right lane edge)
/// One cell is a 128 x 64 px diamond (2 x 1 world units).
/// </summary>
namespace ThermoTactics
{
public static class IsoBoardLayout
{
    public const int LaneCount = 6;
    public const int CellsPerLane = 6;

    // Slot positions along each lane (cell centers). Change these to move the slots.
    public const float AllySlotA = 1.5f;
    public const float EnemySlotA = 4.5f;

    // World position of board coordinate (0,0) = the board's left corner.
    private const float OriginX = -6f;
    private const float OriginY = -0.125f;

    /// <summary>Board coordinates (a, b) to a world position on the road surface.</summary>
    public static Vector3 BoardToWorld(float a, float b)
    {
        float x = OriginX + (a + b);          // each step in a or b moves 1 unit right
        float y = OriginY + (a - b) * 0.5f;   // a goes up, b goes down (2:1 iso)
        return new Vector3(x, y, 0f);
    }

    /// <summary>Center of a cell. Unit feet go here.</summary>
    public static Vector3 CellCenter(int lane, int cell)
    {
        return BoardToWorld(cell + 0.5f, lane + 0.5f);
    }

    /// <summary>The deploy slot for an ally or enemy in a lane.</summary>
    public static Vector3 SlotPosition(int lane, bool isEnemy)
    {
        return BoardToWorld(isEnemy ? EnemySlotA : AllySlotA, lane + 0.5f);
    }

    /// <summary>World point (e.g. a tap) to lane/cell. Returns false when off the road.</summary>
    public static bool TryWorldToCell(Vector3 world, out int lane, out int cell)
    {
        float s = world.x - OriginX;            // = a + b
        float t = (world.y - OriginY) * 2f;     // = a - b
        float a = (s + t) * 0.5f;
        float b = (s - t) * 0.5f;
        cell = Mathf.FloorToInt(a);
        lane = Mathf.FloorToInt(b);
        return lane >= 0 && lane < LaneCount && cell >= 0 && cell < CellsPerLane;
    }

    /// <summary>
    /// Sorting order for anything standing on the board: lower on screen = drawn in front.
    /// Call every frame for moving units, or once for static ones.
    /// </summary>
    public static int SortingOrderFor(Vector3 world)
    {
        return Mathf.RoundToInt(-world.y * 100f);
    }
}
}
