using System;
using System.Collections.Generic;

namespace JamJam
{
    public readonly struct RoutePoint
    {
        public readonly float X;
        public readonly float Z;

        public RoutePoint(float x, float z) { X = x; Z = z; }
    }

    public static class WoolRouting
    {
        public const float Speed = 6f;
        public const float ArrowDistance = 0.5f;
        public const float TravelScale = 0.45f;
        public const float BoardCenterZ = -5.05f;
        public const float TrayZ = -0.75f;
        public const float SlotSpacing = 1.27f;
        static readonly int[] StepX = { 0, 1, 0, -1 };
        static readonly int[] StepY = { 1, 0, -1, 0 };

        public static RoutePoint BlockCenter(BlockDefinition block, LevelDefinition level)
        {
            return new RoutePoint(block.X + (block.Width - level.Columns) * 0.5f,
                BoardCenterZ + block.Y + (block.Height - level.Rows) * 0.5f);
        }

        public static RoutePoint SlotCenter(int slot)
        {
            return new RoutePoint((slot - 2.5f) * SlotSpacing, TrayZ);
        }

        public static bool TryFind(LevelDefinition level, IReadOnlyList<BlockState> blocks, int index, int slot,
            out IReadOnlyList<RoutePoint> points, out float boardExitDistance)
        {
            points = Array.Empty<RoutePoint>();
            boardExitDistance = 0;
            if (level == null || blocks == null || index < 0 || index >= blocks.Count) return false;
            var block = blocks[index].Definition;
            if (level.Columns < 1 || level.Rows < 1 || block.Width < 1 || block.Height < 1) return false;
            var direction=(int)block.Direction;
            if(direction<0 || direction>3) return false;
            var steps=block.Direction==ExitDirection.Up?level.Rows-block.Y:
                block.Direction==ExitDirection.Down?block.Y+block.Height:
                block.Direction==ExitDirection.Right?level.Columns-block.X:block.X+block.Width;
            for(var step=1;step<=steps;step++)
                if(!IsClear(blocks,index,block.X+StepX[direction]*step,block.Y+StepY[direction]*step,block.Width,block.Height)) return false;
            const int resolution=2;
            var target=SlotCenter(slot);
            var goalX=(int)Math.Round((target.X-(block.Width-level.Columns)*0.5f)*resolution);
            var goalY=(int)Math.Ceiling((level.Rows-block.Height*(1-TravelScale)*0.5f)*resolution);
            var padding=(Math.Max(block.Width,block.Height)+1)*resolution;
            var minX=Math.Min(-block.Width*resolution-padding,goalX-padding);
            var maxX=Math.Max(level.Columns*resolution+padding,goalX+padding);
            var minY=-block.Height*resolution-padding;
            var maxY=level.Rows*resolution+padding;
            var columns=maxX-minX+1;
            var previous=new int[columns*(maxY-minY+1)];
            for(var i=0;i<previous.Length;i++) previous[i]=-1;
            var startX=block.X*resolution+StepX[direction];
            var startY=block.Y*resolution+StepY[direction];
            var start=(startY-minY)*columns+startX-minX;
            var goal=(goalY-minY)*columns+goalX-minX;
            var queue=new Queue<int>(); previous[start]=start; queue.Enqueue(start);
            while(queue.Count>0 && previous[goal]<0)
            {
                var current=queue.Dequeue();
                var x=current%columns+minX; var y=current/columns+minY;
                for(var turn=0;turn<4;turn++)
                {
                    if(current==start && turn==(direction+2)%4) continue;
                    var nextX=x+StepX[turn]; var nextY=y+StepY[turn];
                    if(nextX<minX || nextX>maxX || nextY<minY || nextY>maxY) continue;
                    var next=(nextY-minY)*columns+nextX-minX;
                    if(previous[next]>=0 || !IsClear(blocks,index,nextX/(float)resolution,nextY/(float)resolution,block.Width,block.Height,TravelScale)) continue;
                    previous[next]=current; queue.Enqueue(next);
                }
            }
            if(previous[goal]<0) return false;
            var route=new List<RoutePoint>();
            for(var current=goal;;current=previous[current])
            {
                route.Add(new RoutePoint((current%columns+minX)/(float)resolution+(block.Width-level.Columns)*0.5f,
                    BoardCenterZ+(current/columns+minY)/(float)resolution+(block.Height-level.Rows)*0.5f));
                if(current==start) break;
            }
            route.Add(BlockCenter(block,level)); route.Reverse();
            boardExitDistance=ArrowDistance;
            Add(route,new RoutePoint(route[route.Count-1].X,TrayZ-0.8f));
            Add(route,new RoutePoint(target.X,TrayZ-0.8f));
            Add(route,target);
            points = route;
            return true;
        }

        static bool IsClear(IReadOnlyList<BlockState> blocks, int index, float x, float y, int width, int height, float scale=1)
        {
            x+=width*(1-scale)*0.5f; y+=height*(1-scale)*0.5f;
            var scaledWidth=width*scale; var scaledHeight=height*scale;
            for (var i = 0; i < blocks.Count; i++)
            {
                if (i == index || blocks[i].Phase != 0) continue;
                var other = blocks[i].Definition;
                if (x < other.X + other.Width && x + scaledWidth > other.X &&
                    y < other.Y + other.Height && y + scaledHeight > other.Y) return false;
            }
            return true;
        }

        public static float ScaleAt(float distance)
        {
            var t=Math.Max(0,Math.Min(1,distance/ArrowDistance));
            return 1+(TravelScale-1)*t*t*(3-2*t);
        }

        static void Add(List<RoutePoint> points, RoutePoint point)
        {
            var last = points[points.Count - 1];
            if (Math.Abs(last.X - point.X) + Math.Abs(last.Z - point.Z) > 0.0001f) points.Add(point);
        }

        public static float Length(IReadOnlyList<RoutePoint> points)
        {
            var length = 0f;
            if (points != null)
                for (var i = 1; i < points.Count; i++)
                    length += Math.Abs(points[i].X - points[i - 1].X) + Math.Abs(points[i].Z - points[i - 1].Z);
            return length;
        }

        public static void Evaluate(IReadOnlyList<RoutePoint> points, float distance,
            out RoutePoint position, out RoutePoint direction)
        {
            position = default;
            direction = default;
            if (points == null || points.Count == 0) return;
            position = points[0];
            distance = float.IsNaN(distance) ? 0 : Math.Max(0, distance);
            for (var i = 1; i < points.Count; i++)
            {
                var start = points[i - 1];
                var end = points[i];
                var dx = end.X - start.X;
                var dz = end.Z - start.Z;
                var length = Math.Abs(dx) + Math.Abs(dz);
                if (length <= 0.0001f) continue;
                direction = new RoutePoint(dx / length, dz / length);
                if (distance <= length)
                {
                    position = new RoutePoint(start.X + direction.X * distance, start.Z + direction.Z * distance);
                    return;
                }
                distance -= length;
                position = end;
            }
        }
    }
}
