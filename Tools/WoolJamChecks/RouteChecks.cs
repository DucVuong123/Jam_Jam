using System;
using System.Collections.Generic;
using JamJam;

static class RouteChecks
{
    static int passed;
    public static int Assertions => passed;

    static void Check(bool value, string name)
    {
        if (!value) throw new InvalidOperationException(name);
        passed++;
    }

    static bool Near(float a, float b) { return Math.Abs(a - b) < 0.0002f; }

    static BlockDefinition B(int x, int y, int width = 1, int height = 1,
        ExitDirection direction = ExitDirection.Up)
    {
        return new BlockDefinition { X = x, Y = y, Width = width, Height = height, Direction = direction };
    }

    static void Setup(int columns, int rows, out LevelDefinition level,
        out IReadOnlyList<BlockState> states, params BlockDefinition[] blocks)
    {
        level = new LevelDefinition { Columns = columns, Rows = rows, Blocks = blocks };
        var list = new List<BlockState>();
        foreach (var block in blocks) list.Add(new BlockState { Definition = block });
        states = list;
    }

    static void Validate(LevelDefinition level, IReadOnlyList<BlockState> blocks,
        IReadOnlyList<RoutePoint> route, float exit, int slot)
    {
        var block = blocks[0].Definition;
        var start = WoolRouting.BlockCenter(block, level);
        var end = WoolRouting.SlotCenter(slot);
        Check(route[0].X == start.X && route[0].Z == start.Z, "Exact block center start");
        Check(route[route.Count - 1].X == end.X && route[route.Count - 1].Z == end.Z, "Exact slot center end");
        for (var i = 1; i < route.Count; i++)
        {
            var dx = Math.Abs(route[i].X - route[i - 1].X);
            var dz = Math.Abs(route[i].Z - route[i - 1].Z);
            Check((dx < 0.0001f) != (dz < 0.0001f), "Every segment cardinal and nonzero");
        }
        var arrowX=block.Direction==ExitDirection.Right?1:block.Direction==ExitDirection.Left?-1:0;
        var arrowZ=block.Direction==ExitDirection.Up?1:block.Direction==ExitDirection.Down?-1:0;
        Check(Near(exit,0.5f), "Initial arrow move stops after half a cell before safe routing");
        WoolRouting.Evaluate(route, exit, out var goal, out _);
        Check(Near(goal.X,start.X+arrowX*exit) && Near(goal.Z,start.Z+arrowZ*exit),
            "Initial nudge follows the arrow before a turn");
        for (var distance = 0f; distance <= exit + 0.001f; distance += 0.125f)
        {
            WoolRouting.Evaluate(route, Math.Min(distance, exit), out var point, out _);
            Check(Near(point.X,start.X+arrowX*Math.Min(distance,exit)) && Near(point.Z,start.Z+arrowZ*Math.Min(distance,exit)),
                "Every point before board exit follows the original arrow");
            var x = point.X - (block.Width - level.Columns) * 0.5f;
            var y = point.Z - WoolRouting.BoardCenterZ - (block.Height - level.Rows) * 0.5f;
            for (var i = 1; i < blocks.Count; i++)
            {
                if (blocks[i].Phase != 0) continue;
                var other = blocks[i].Definition;
                var overlap = x < other.X + other.Width - 0.0001f && x + block.Width > other.X + 0.0001f &&
                    y < other.Y + other.Height - 0.0001f && y + block.Height > other.Y + 0.0001f;
                Check(!overlap, "Moving full rectangle avoids static blocks through board exit");
            }
        }
        for(var distance=exit;distance<=WoolRouting.Length(route);distance+=0.0625f)
        {
            WoolRouting.Evaluate(route,distance,out var point,out _);
            for(var i=1;i<blocks.Count;i++)
            {
                if(blocks[i].Phase!=0) continue;
                var other=blocks[i].Definition;
                var center=WoolRouting.BlockCenter(other,level);
                var overlap=Math.Abs(point.X-center.X)<((block.Width+0.02f)*0.45f+other.Width+0.02f)*0.5f-0.0001f &&
                    Math.Abs(point.Z-center.Z)<((block.Height+0.02f)*0.45f+other.Height+0.02f)*0.5f-0.0001f;
                Check(!overlap,"Every moving sprite segment avoids stationary sprites after its initial nudge");
            }
        }
    }

    public static int Run()
    {
        Setup(8, 6, out var level, out var blocks, B(6, 0, 2, 1, ExitDirection.Right));
        Check(WoolRouting.TryFind(level,blocks,0,0,out var route,out var exit), "Purple right arrow has an exit route");
        Check(route[1].X>route[0].X && Near(route[1].Z,route[0].Z),
            "Purple right arrow must move right before turning toward the tray");
        Validate(level,blocks,route,exit,0);
        foreach(ExitDirection direction in Enum.GetValues(typeof(ExitDirection)))
            foreach(var size in new[] { new[] {1,1},new[] {2,1},new[] {1,2} })
                for(var slot=0;slot<6;slot++)
                {
                    Setup(8,6,out level,out blocks,B(3,2,size[0],size[1],direction));
                    Check(WoolRouting.TryFind(level,blocks,0,slot,out route,out exit), "Each arrow and size reaches each slot");
                    Validate(level,blocks,route,exit,slot);
                }

        Setup(8,6,out level,out blocks,B(3,2,2,1,ExitDirection.Right));
        Check(WoolRouting.TryFind(level,blocks,0,2,out route,out exit), "Open board has a short route");
        Check(Near(exit,0.5f) && Math.Abs(route[2].Z-route[1].Z)>0.1f && WoolRouting.Length(route)<12,
            "Right arrow can turn upward after a short nudge without reaching the far edge");
        Validate(level,blocks,route,exit,2);
        Setup(8,6,out level,out blocks,B(3,2,2,1,ExitDirection.Right),B(3,3,2));
        Check(WoolRouting.TryFind(level,blocks,0,2,out route,out exit), "Blocked early bend still finds a clear route");
        var firstTurn=0f;
        for(var i=1;i<route.Count;i++)
        {
            if(Math.Abs(route[i].Z-route[i-1].Z)>0.001f) break;
            firstTurn+=Math.Abs(route[i].X-route[i-1].X);
        }
        Check(firstTurn>=1.5f,"Block continues along arrow until its reduced footprint has room to turn");
        Validate(level,blocks,route,exit,2);

        Setup(5,5,out level,out blocks,B(2,1),B(1,2,3));
        Check(!WoolRouting.TryFind(level,blocks,0,2,out route,out exit), "Blocked arrow cannot detour around the wall");
        Check(route.Count==0 && exit==0,"Failed route has empty outputs");
        blocks[1].Phase=1;
        Check(WoolRouting.TryFind(level,blocks,0,2,out route,out exit), "Earlier departure vacates its lane for planned FIFO route");
        Validate(level,blocks,route,exit,2);

        Setup(5,5,out level,out blocks,B(2,2,1,1,ExitDirection.Down),
            B(1,1,3),B(1,3,3),B(1,2),B(3,2));
        Check(!WoolRouting.TryFind(level,blocks,0,0,out route,out exit), "Enclosed rectangle cannot leave");
        blocks[2].Phase=2;
        Check(!WoolRouting.TryFind(level,blocks,0,0,out route,out exit), "Opening opposite arrow direction does not authorize a turn");
        blocks[1].Phase=2;
        Check(WoolRouting.TryFind(level,blocks,0,5,out route,out exit), "Opening arrow lane allows departure");
        Validate(level,blocks,route,exit,5);

        Setup(6,6,out level,out blocks,B(2,2,2),B(0,0,6),B(0,1,1,4),B(5,1,1,4),B(0,5,2),B(3,5,3));
        Check(!WoolRouting.TryFind(level,blocks,0,2,out route,out exit), "Width2 cannot fit width1 opening");
        blocks[0].Definition=B(2,2);
        Check(WoolRouting.TryFind(level,blocks,0,2,out route,out exit), "Width1 passes arrow-aligned opening");
        Validate(level,blocks,route,exit,2);

        Setup(6,6,out level,out blocks,B(2,2,1,2,ExitDirection.Right),B(0,0,6),B(0,5,6),B(0,1,1,4),B(5,1),B(5,3,1,2));
        Check(!WoolRouting.TryFind(level,blocks,0,3,out route,out exit), "Height2 cannot fit height1 opening");
        blocks[0].Definition=B(2,2,1,1,ExitDirection.Right);
        Check(WoolRouting.TryFind(level,blocks,0,3,out route,out exit), "Height1 passes right-aligned opening");
        Validate(level,blocks,route,exit,3);

        Setup(8,6,out level,out blocks,B(0,0,1,2,ExitDirection.Down),B(0,5,2));
        Check(WoolRouting.TryFind(level,blocks,0,0,out route,out exit), "Tall down arrow circles outside to first slot");
        Validate(level,blocks,route,exit,0);
        var obstacle=WoolRouting.BlockCenter(blocks[1].Definition,level);
        for(var distance=exit;distance<WoolRouting.Length(route);distance+=0.05f)
        {
            WoolRouting.Evaluate(route,distance,out var point,out _);
            var overlap=Math.Abs(point.X-obstacle.X)<(1.02f*0.45f+2.02f)*0.5f &&
                Math.Abs(point.Z-obstacle.Z)<(2.02f*0.45f+1.02f)*0.5f;
            Check(!overlap,"Shrunk tall sprite avoids top row throughout outside route");
        }

        IReadOnlyList<RoutePoint> elbow = new[] { new RoutePoint(0, 0), new RoutePoint(3, 0), new RoutePoint(3, 4) };
        Check(Near(WoolRouting.Length(elbow), 7), "Length sums Manhattan arc distance");
        for (var tick = 0; tick <= 14; tick++)
        {
            var distance = Math.Min(7,tick * 0.1f * WoolRouting.Speed);
            WoolRouting.Evaluate(elbow, distance, out var point, out var direction);
            Check(Near(point.X, Math.Min(distance, 3)) && Near(point.Z, Math.Max(0, distance - 3)),
                "Equal time advances equal arc distance across corner");
            Check(Near(Math.Abs(direction.X) + Math.Abs(direction.Z), 1) && (Near(direction.X, 0) || Near(direction.Z, 0)),
                "Evaluation returns cardinal unit direction");
        }
        WoolRouting.Evaluate(elbow, -10, out var before, out _);
        WoolRouting.Evaluate(elbow, 100, out var after, out _);
        Check(Near(before.X, 0) && Near(before.Z, 0) && Near(after.X, 3) && Near(after.Z, 4), "Evaluation clamps endpoints");
        return passed;
    }
}
