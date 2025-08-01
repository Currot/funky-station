// SPDX-FileCopyrightText: 2024 Jake Huxell <JakeHuxell@pm.me>
// SPDX-FileCopyrightText: 2024 Tadeo <td12233a@gmail.com>
// SPDX-FileCopyrightText: 2024 eoineoineoin <github@eoinrul.es>
// SPDX-FileCopyrightText: 2024 exincore <me@exin.xyz>
// SPDX-FileCopyrightText: 2024 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Tay <td12233a@gmail.com>
// SPDX-FileCopyrightText: 2025 pa.pecherskij <pa.pecherskij@interfax.ru>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: MIT

using System.Numerics;
using Content.Client.UserInterface.Controls;
using Content.Shared.Shuttles.Components;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Threading;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Vector2 = System.Numerics.Vector2;

namespace Content.Client.Shuttles.UI;

/// <summary>
/// Provides common functionality for radar-like displays on shuttle consoles.
/// </summary>
[GenerateTypedNameReferences]
[Virtual]
public partial class BaseShuttleControl : MapGridControl
{
    [Dependency] private readonly IParallelManager _parallel = default!;
    protected readonly SharedMapSystem Maps;

    protected readonly Font Font;

    private GridDrawJob _drawJob;

    // Cache grid drawing data as it can be expensive to build
    public readonly Dictionary<EntityUid, GridDrawData> GridData = new();

    // Per-draw caching
    private readonly List<Vector2i> _gridTileList = new();
    private readonly HashSet<Vector2i> _gridNeighborSet = new();
    private readonly List<(Vector2 Start, Vector2 End)> _edges = new();

    private Vector2[] _allVertices = Array.Empty<Vector2>();

    private (DirectionFlag, Vector2i)[] _neighborDirections;

    public BaseShuttleControl() : this(32f, 32f, 32f)
    {
    }

    public BaseShuttleControl(float minRange, float maxRange, float range) : base(minRange, maxRange, range)
    {
        RobustXamlLoader.Load(this);
        Maps = EntManager.System<SharedMapSystem>();
        Font = new VectorFont(IoCManager.Resolve<IResourceCache>().GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 12);

        _drawJob = new GridDrawJob()
        {
            ScaledVertices = _allVertices,
        };

        _neighborDirections = new (DirectionFlag, Vector2i)[4];

        for (var i = 0; i < 4; i++)
        {
            var dir = (DirectionFlag) Math.Pow(2, i);
            var dirVec = dir.AsDir().ToIntVec();
            _neighborDirections[i] = (dir, dirVec);
        }
    }

    protected void DrawData(DrawingHandleScreen handle, string text)
    {
        var coordsDimensions = handle.GetDimensions(Font, text, 1f);
        const float coordsMargins = 5f;

        handle.DrawString(Font,
            new Vector2(coordsMargins, PixelHeight) - new Vector2(0f, coordsDimensions.Y + coordsMargins),
            text,
            Color.FromSrgb(IFFComponent.SelfColor));
    }

    protected void DrawCircles(DrawingHandleScreen handle)
    {
        // Equatorial lines
        var gridLines = Color.LightGray.WithAlpha(0.01f);

        // Each circle is this x distance of the last one.
        const float EquatorialMultiplier = 2f;

        var minDistance = MathF.Pow(EquatorialMultiplier, EquatorialMultiplier * 1.5f);
        var maxDistance = MathF.Pow(2f, EquatorialMultiplier * 6f);
        var cornerDistance = MathF.Sqrt(WorldRange * WorldRange + WorldRange * WorldRange);

        var origin = ScalePosition(-new Vector2(Offset.X, -Offset.Y));

        for (var radius = minDistance; radius <= maxDistance; radius *= EquatorialMultiplier)
        {
            if (radius > cornerDistance)
                continue;

            var color = Color.ToSrgb(gridLines).WithAlpha(0.05f);
            var scaledRadius = MinimapScale * radius;
            var text = $"{radius:0}m";
            var textDimensions = handle.GetDimensions(Font, text, UIScale);

            handle.DrawCircle(origin, scaledRadius, color, false);
            handle.DrawString(Font, ScalePosition(new Vector2(0f, -radius)) - new Vector2(0f, textDimensions.Y), text, UIScale, color);
        }

        const int gridLinesRadial = 8;

        for (var i = 0; i < gridLinesRadial; i++)
        {
            Angle angle = (Math.PI / gridLinesRadial) * i;
            // TODO: Handle distance properly.
            var aExtent = angle.ToVec() * ScaledMinimapRadius * 1.42f;
            var lineColor = Color.MediumSpringGreen.WithAlpha(0.02f);
            handle.DrawLine(origin - aExtent, origin + aExtent, lineColor);
        }
    }

    protected void DrawGrid(DrawingHandleScreen handle, Matrix3x2 gridToView, Entity<MapGridComponent> grid, Color color, float alpha = 0.01f)
    {
        var rator = Maps.GetAllTilesEnumerator(grid.Owner, grid.Comp);
        var tileSize = grid.Comp.TileSize;

        // Check if we even have data
        // TODO: Need to prune old grid-data if we don't draw it.
        var gridData = GridData.GetOrNew(grid.Owner);

        if (gridData.LastBuild < grid.Comp.LastTileModifiedTick)
        {
            gridData.Vertices.Clear();
            _gridTileList.Clear();
            _gridNeighborSet.Clear();

            // Okay so there's 2 steps to this
            // 1. Is that get we get a set of all tiles. This is used to decompose into triangle-strips
            // 2. Is that we get a list of all tiles. This is used for edge data to decompose into line-strips.
            while (rator.MoveNext(out var tileRef))
            {
                var index = tileRef.Value.GridIndices;
                _gridNeighborSet.Add(index);
                _gridTileList.Add(index);

                var bl = Maps.TileToVector(grid, index);
                var br = bl + new Vector2(tileSize, 0f);
                var tr = bl + new Vector2(tileSize, tileSize);
                var tl = bl + new Vector2(0f, tileSize);

                gridData.Vertices.Add(bl);
                gridData.Vertices.Add(br);
                gridData.Vertices.Add(tl);

                gridData.Vertices.Add(br);
                gridData.Vertices.Add(tl);
                gridData.Vertices.Add(tr);
            }

            gridData.EdgeIndex = gridData.Vertices.Count;
            _edges.Clear();

            foreach (var index in _gridTileList)
            {
                // We get all of the raw lines up front
                // then we decompose them into longer lines in a separate step.
                foreach (var (dir, dirVec) in _neighborDirections)
                {
                    var neighbor = index + dirVec;

                    if (_gridNeighborSet.Contains(neighbor))
                        continue;

                    var bl = Maps.TileToVector(grid, index);
                    var br = bl + new Vector2(tileSize, 0f);
                    var tr = bl + new Vector2(tileSize, tileSize);
                    var tl = bl + new Vector2(0f, tileSize);

                    // Could probably rotate this but this might be faster?
                    Vector2 actualStart;
                    Vector2 actualEnd;

                    switch (dir)
                    {
                        case DirectionFlag.South:
                            actualStart = bl;
                            actualEnd = br;
                            break;
                        case DirectionFlag.East:
                            actualStart = br;
                            actualEnd = tr;
                            break;
                        case DirectionFlag.North:
                            actualStart = tr;
                            actualEnd = tl;
                            break;
                        case DirectionFlag.West:
                            actualStart = tl;
                            actualEnd = bl;
                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    _edges.Add((actualStart, actualEnd));
                }
            }

            // Decompose the edges into longer lines to save data.
            // Now we decompose the lines into longer lines (less data to send to the GPU)
            var decomposed = true;

            while (decomposed)
            {
                decomposed = false;

                for (var i = 0; i < _edges.Count; i++)
                {
                    var (start, end) = _edges[i];
                    var neighborFound = false;
                    var neighborIndex = 0;
                    Vector2 neighborStart;
                    Vector2 neighborEnd = Vector2.Zero;

                    // Does our end correspond with another start?
                    for (var j = i + 1; j < _edges.Count; j++)
                    {
                        (neighborStart, neighborEnd) = _edges[j];

                        if (!end.Equals(neighborStart))
                            continue;

                        neighborFound = true;
                        neighborIndex = j;
                        break;
                    }

                    if (!neighborFound)
                        continue;

                    // Check if our start and the neighbor's end are collinear
                    if (!CollinearSimplifier.IsCollinear(start, end, neighborEnd, 10f * float.Epsilon))
                        continue;

                    decomposed = true;
                    _edges[i] = (start, neighborEnd);
                    _edges.RemoveAt(neighborIndex);
                }
            }

            gridData.Vertices.EnsureCapacity(_edges.Count * 2);

            foreach (var edge in _edges)
            {
                gridData.Vertices.Add(edge.Start);
                gridData.Vertices.Add(edge.End);
            }

            gridData.LastBuild = grid.Comp.LastTileModifiedTick;
        }

        var totalData = gridData.Vertices.Count;
        var triCount = gridData.EdgeIndex;
        var edgeCount = totalData - gridData.EdgeIndex;
        Extensions.EnsureLength(ref _allVertices, totalData);

        _drawJob.Matrix = gridToView;
        _drawJob.Vertices = gridData.Vertices;
        _drawJob.ScaledVertices = _allVertices;

        _parallel.ProcessNow(_drawJob, totalData);

        const float BatchSize = 3f * 4096;

        for (var i = 0; i < Math.Ceiling(triCount / BatchSize); i++)
        {
            var start = (int) (i * BatchSize);
            var end = (int) Math.Min(triCount, start + BatchSize);
            var count = end - start;
            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, new Span<Vector2>(_allVertices, start, count), color.WithAlpha(alpha));
        }

        handle.DrawPrimitives(DrawPrimitiveTopology.LineList, new Span<Vector2>(_allVertices, gridData.EdgeIndex, edgeCount), color);
    }

    private record struct GridDrawJob : IParallelRobustJob
    {
        public int BatchSize => 64;

        public Matrix3x2 Matrix;

        public List<Vector2> Vertices;
        public Vector2[] ScaledVertices;

        public void Execute(int index)
        {
            ScaledVertices[index] = Vector2.Transform(Vertices[index], Matrix);
        }
    }
}

public sealed class GridDrawData
{
    /*
     * List of lists because we use LineStrip and TriangleStrip respectively (less data to pass to the GPU).
     */

    public List<Vector2> Vertices = new();

    /// <summary>
    /// Vertices index from when edges start.
    /// </summary>
    public int EdgeIndex;

    public GameTick LastBuild;
}
