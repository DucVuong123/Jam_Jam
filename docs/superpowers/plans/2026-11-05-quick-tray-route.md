# Rút ngắn đường vào khay và tăng tốc di chuyển

Block nhích 0.5 ô theo mũi tên, thu gọn bằng cùng animation đang dùng rồi tìm đường ngắn trên lưới nửa ô. Chỉ rẽ khi footprint thu gọn có chỗ trống; vẫn giữ điều kiện làn mũi tên phải thông. Tăng Speed 5 lên 6 (+20%). Đồng bộ footprint của block đang di chuyển giữa model, view và planner. ExitDistance hiện lưu độ dài chặng bắt buộc theo mũi tên (0.5), không còn yêu cầu ra hết mép bàn.

## Tools/WoolJamChecks/RouteChecks.cs

**Trước**
```46:58:C:/Workspaces/UnityProjects/JamJam/Tools/WoolJamChecks/RouteChecks.cs
        }
        var arrowX=block.Direction==ExitDirection.Right?1:block.Direction==ExitDirection.Left?-1:0;
        var arrowZ=block.Direction==ExitDirection.Up?1:block.Direction==ExitDirection.Down?-1:0;
        var expectedExit=block.Direction==ExitDirection.Up?level.Rows-block.Y:
            block.Direction==ExitDirection.Down?block.Y+block.Height:
            block.Direction==ExitDirection.Right?level.Columns-block.X:block.X+block.Width;
        Check(Near(exit,expectedExit), "Exit distance includes whole block footprint in arrow direction");
        WoolRouting.Evaluate(route, exit, out var goal, out _);
        Check(Near(goal.X,start.X+arrowX*exit) && Near(goal.Z,start.Z+arrowZ*exit),
            "Whole rectangle leaves the board before turning");
        for (var distance = 0f; distance <= exit + 0.001f; distance += 0.125f)
        {
            WoolRouting.Evaluate(route, Math.Min(distance, exit), out var point, out _);
```

**Sau**
```csharp
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
```

**Trước**
```67:72:C:/Workspaces/UnityProjects/JamJam/Tools/WoolJamChecks/RouteChecks.cs
                var overlap = x < other.X + other.Width - 0.0001f && x + block.Width > other.X + 0.0001f &&
                    y < other.Y + other.Height - 0.0001f && y + block.Height > other.Y + 0.0001f;
                Check(!overlap, "Moving full rectangle avoids static blocks through board exit");
            }
        }
    }
```

**Sau**
```csharp
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
```

**Trước**
```85:100:C:/Workspaces/UnityProjects/JamJam/Tools/WoolJamChecks/RouteChecks.cs
                    Setup(8,6,out level,out blocks,B(3,2,size[0],size[1],direction));
                    Check(WoolRouting.TryFind(level,blocks,0,slot,out route,out exit), "Each arrow and size reaches each slot");
                    Validate(level,blocks,route,exit,slot);
                    for(var distance=exit;distance<WoolRouting.Length(route);distance+=0.125f)
                    {
                        WoolRouting.Evaluate(route,distance,out var point,out _);
                        var halfWidth=(size[0]+0.02f)*0.45f*0.5f;
                        var halfHeight=(size[1]+0.02f)*0.45f*0.5f;
                        var outside=point.X+halfWidth<=-4 || point.X-halfWidth>=4 ||
                            point.Z+halfHeight<=WoolRouting.BoardCenterZ-3 || point.Z-halfHeight>=WoolRouting.BoardCenterZ+3;
                        Check(outside,"After arrow exit, rendered block never re-enters the board");
                    }
                }

        Setup(5,5,out level,out blocks,B(2,1),B(1,2,3));
        Check(!WoolRouting.TryFind(level,blocks,0,2,out route,out exit), "Blocked arrow cannot detour around the wall");
```

**Sau**
```csharp
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
```

**Trước**
```140:146:C:/Workspaces/UnityProjects/JamJam/Tools/WoolJamChecks/RouteChecks.cs
        Check(Near(WoolRouting.Length(elbow), 7), "Length sums Manhattan arc distance");
        for (var tick = 0; tick <= 14; tick++)
        {
            var distance = tick * 0.1f * WoolRouting.Speed;
            WoolRouting.Evaluate(elbow, distance, out var point, out var direction);
            Check(Near(point.X, Math.Min(distance, 3)) && Near(point.Z, Math.Max(0, distance - 3)),
                "Equal time advances equal arc distance across corner");
```

**Sau**
```csharp
        Check(Near(WoolRouting.Length(elbow), 7), "Length sums Manhattan arc distance");
        for (var tick = 0; tick <= 14; tick++)
        {
            var distance = Math.Min(7,tick * 0.1f * WoolRouting.Speed);
            WoolRouting.Evaluate(elbow, distance, out var point, out var direction);
            Check(Near(point.X, Math.Min(distance, 3)) && Near(point.Z, Math.Max(0, distance - 3)),
                "Equal time advances equal arc distance across corner");
```

## Assets/JamJam/Runtime/WoolRouting.cs

**Trước**
```13:19:C:/Workspaces/UnityProjects/JamJam/Assets/JamJam/Runtime/WoolRouting.cs

    public static class WoolRouting
    {
        public const float Speed = 5f;
        public const float BoardCenterZ = -5.05f;
        public const float TrayZ = -0.75f;
        public const float SlotSpacing = 1.27f;
```

**Sau**
```csharp

    public static class WoolRouting
    {
        public const float Speed = 6f;
        public const float ArrowDistance = 0.5f;
        public const float TravelScale = 0.45f;
        public const float BoardCenterZ = -5.05f;
        public const float TrayZ = -0.75f;
        public const float SlotSpacing = 1.27f;
```

**Trước**
```46:87:C:/Workspaces/UnityProjects/JamJam/Assets/JamJam/Runtime/WoolRouting.cs
                block.Direction==ExitDirection.Right?level.Columns-block.X:block.X+block.Width;
            for(var step=1;step<=steps;step++)
                if(!IsClear(blocks,index,block.X+StepX[direction]*step,block.Y+StepY[direction]*step,block.Width,block.Height)) return false;
            var start=BlockCenter(block,level);
            var exit=new RoutePoint(start.X+StepX[direction]*steps,start.Z+StepY[direction]*steps);
            var route=new List<RoutePoint> { start };
            Add(route,exit);
            boardExitDistance=steps;
            var target=SlotCenter(slot);
            var top=BoardCenterZ+(level.Rows+block.Height)*0.5f;
            if(block.Direction==ExitDirection.Down)
            {
                var left=-(level.Columns+block.Width)*0.5f;
                var right=-left;
                var leftDistance=Math.Abs(exit.X-left)+Math.Abs(target.X-left);
                var rightDistance=Math.Abs(exit.X-right)+Math.Abs(target.X-right);
                var side=leftDistance<=rightDistance?left:right;
                Add(route,new RoutePoint(side,exit.Z));
                Add(route,new RoutePoint(side,top));
            }
            else if(block.Direction==ExitDirection.Left || block.Direction==ExitDirection.Right)
                Add(route,new RoutePoint(exit.X,top));
            Add(route,new RoutePoint(target.X,top));
            Add(route,new RoutePoint(target.X,TrayZ-0.8f));
            Add(route,target);
            points = route;
            return true;
        }

        static bool IsClear(IReadOnlyList<BlockState> blocks, int index, int x, int y, int width, int height)
        {
            for (var i = 0; i < blocks.Count; i++)
            {
                if (i == index || blocks[i].Phase != 0) continue;
                var other = blocks[i].Definition;
                if (x < other.X + other.Width && x + width > other.X &&
                    y < other.Y + other.Height && y + height > other.Y) return false;
            }
            return true;
        }

        static void Add(List<RoutePoint> points, RoutePoint point)
```

**Sau**
```csharp
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
```

## Assets/JamJam/Runtime/WoolScene.cs

**Trước**
```205:212:C:/Workspaces/UnityProjects/JamJam/Assets/JamJam/Runtime/WoolScene.cs
                    var distance=block.Travel*WoolRouting.Speed;
                    WoolRouting.Evaluate(block.Route,distance,out var point,out var movement);
                    view.transform.position=new Vector3(point.X,0.12f,point.Z);
                    var shrink=Mathf.SmoothStep(0,1,Mathf.InverseLerp(Mathf.Max(0,block.ExitDistance-0.7f),block.ExitDistance,distance));
                    view.transform.localScale=Vector3.one*Mathf.Lerp(1,0.45f,shrink);
                    if(block.Travel>0)
                    {
                        var angle=Mathf.Atan2(movement.X,movement.Z)*Mathf.Rad2Deg;
```

**Sau**
```csharp
                    var distance=block.Travel*WoolRouting.Speed;
                    WoolRouting.Evaluate(block.Route,distance,out var point,out var movement);
                    view.transform.position=new Vector3(point.X,0.12f,point.Z);
                    view.transform.localScale=Vector3.one*WoolRouting.ScaleAt(distance);
                    if(block.Travel>0)
                    {
                        var angle=Mathf.Atan2(movement.X,movement.Z)*Mathf.Rad2Deg;
```

## Assets/JamJam/Runtime/WoolGame.cs

**Trước**
```154:167:C:/Workspaces/UnityProjects/JamJam/Assets/JamJam/Runtime/WoolGame.cs
                if(i==index || other.Phase==2) continue;
                var b=other.Definition;
                var x=(float)b.X; var y=(float)b.Y;
                if(other.Phase==1)
                {
                    WoolRouting.Evaluate(other.Route,other.Travel*WoolRouting.Speed,out var point,out _);
                    x=point.X-(b.Width-level.Columns)*0.5f;
                    y=point.Z-WoolRouting.BoardCenterZ-(b.Height-level.Rows)*0.5f;
                }
                var left=Math.Max(0,x); var right=Math.Min(level.Columns,x+b.Width);
                var bottom=Math.Max(0,y); var top=Math.Min(level.Rows,y+b.Height);
                if(right-left<=epsilon || top-bottom<=epsilon) continue;
                var overlapX=a.X<right-epsilon && a.X+a.Width>left+epsilon;
                var overlapY=a.Y<top-epsilon && a.Y+a.Height>bottom+epsilon;
```

**Sau**
```csharp
                if(i==index || other.Phase==2) continue;
                var b=other.Definition;
                var x=(float)b.X; var y=(float)b.Y;
                var width=(float)b.Width; var height=(float)b.Height;
                if(other.Phase==1)
                {
                    WoolRouting.Evaluate(other.Route,other.Travel*WoolRouting.Speed,out var point,out _);
                    x=point.X-(b.Width-level.Columns)*0.5f;
                    y=point.Z-WoolRouting.BoardCenterZ-(b.Height-level.Rows)*0.5f;
                    var scale=WoolRouting.ScaleAt(other.Travel*WoolRouting.Speed);
                    x+=width*(1-scale)*0.5f; y+=height*(1-scale)*0.5f;
                    width*=scale; height*=scale;
                }
                var left=Math.Max(0,x); var right=Math.Min(level.Columns,x+width);
                var bottom=Math.Max(0,y); var top=Math.Min(level.Rows,y+height);
                if(right-left<=epsilon || top-bottom<=epsilon) continue;
                var overlapX=a.X<right-epsilon && a.X+a.Width>left+epsilon;
                var overlapY=a.Y<top-epsilon && a.Y+a.Height>bottom+epsilon;
```

## Assets/JamJam/Editor/WoolEditorChecks.cs

**Trước**
```272:278:C:/Workspaces/UnityProjects/JamJam/Assets/JamJam/Editor/WoolEditorChecks.cs
                {
                    solver?.Dispose(); solver=null; motionProbe?.Dispose(); motionProbe=null;
                    if(arrowExitSamples==0 || arrowExitDirections!=15) { File.AppendAllText(Report,"ARROW EXIT COVERAGE FAILED\n"); EditorApplication.Exit(1); return; }
                    File.AppendAllText(Report,"ARROW FIRST EXIT PASS / "+arrowExitSamples+" samples / all four directions\n");
                    File.AppendAllText(Report,"RUNTIME MOTION PASS / maximum head turn "+maximumTurnSpeed.ToString("F2")+" deg/s / flat cardinal blocks\n");
                    File.AppendAllText(Report,"RUNTIME ALL LEVELS PASS\n");
                    if(Application.isBatchMode)
```

**Sau**
```csharp
                {
                    solver?.Dispose(); solver=null; motionProbe?.Dispose(); motionProbe=null;
                    if(arrowExitSamples==0 || arrowExitDirections!=15) { File.AppendAllText(Report,"ARROW EXIT COVERAGE FAILED\n"); EditorApplication.Exit(1); return; }
                    File.AppendAllText(Report,"INITIAL ARROW MOVE PASS / "+arrowExitSamples+" samples / all four directions\n");
                    File.AppendAllText(Report,"RUNTIME MOTION PASS / maximum head turn "+maximumTurnSpeed.ToString("F2")+" deg/s / flat cardinal blocks\n");
                    File.AppendAllText(Report,"RUNTIME ALL LEVELS PASS\n");
                    if(Application.isBatchMode)
```

**Trước**
```344:350:C:/Workspaces/UnityProjects/JamJam/Assets/JamJam/Editor/WoolEditorChecks.cs
                    var expected=direction==ExitDirection.Up?Vector3.forward:direction==ExitDirection.Down?Vector3.back:direction==ExitDirection.Left?Vector3.left:Vector3.right;
                    var start=WoolScene.BlockPosition(block.Definition,scene.Levels[scene.CurrentLevel].Data);
                    if(Vector3.Distance(view.transform.position,start+expected*distance)>0.001f || Vector3.Dot(view.Arrow.up,expected)<0.99f)
                        throw new InvalidOperationException("Block turned before fully exiting in its arrow direction");
                    arrowExitSamples++;
                    arrowExitDirections|=1<<(int)direction;
                }
```

**Sau**
```csharp
                    var expected=direction==ExitDirection.Up?Vector3.forward:direction==ExitDirection.Down?Vector3.back:direction==ExitDirection.Left?Vector3.left:Vector3.right;
                    var start=WoolScene.BlockPosition(block.Definition,scene.Levels[scene.CurrentLevel].Data);
                    if(Vector3.Distance(view.transform.position,start+expected*distance)>0.001f || Vector3.Dot(view.Arrow.up,expected)<0.99f)
                        throw new InvalidOperationException("Block turned before its initial arrow movement finished");
                    arrowExitSamples++;
                    arrowExitDirections|=1<<(int)direction;
                }
```

## Tóm tắt

- WoolRouting.cs | trước/sau | Nhích nửa ô, BFS đường ngắn có xét footprint thu gọn, tốc độ 6, dùng ScaleAt thống nhất.
- WoolScene.cs | trước/sau | Thu gọn theo cùng ScaleAt với model/planner.
- WoolGame.cs | trước/sau | Kiểm blocker đang di chuyển theo kích thước hiển thị hiện tại.
- RouteChecks.cs | trước/sau | Kiểm rẽ sớm khi trống, tiếp tục thẳng khi chưa đủ chỗ, va chạm toàn đường và speed sampling.
- WoolEditorChecks.cs | trước/sau | Cập nhật tên kiểm chứng chặng nhích theo mũi tên.
