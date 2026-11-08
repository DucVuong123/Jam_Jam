using System;
using JamJam;

static class Program
{
    static int passed;

    static void Check(bool value, string name)
    {
        if (!value) throw new InvalidOperationException(name);
        passed++;
    }

    static void Fill(out LevelDefinition level, int slots = 2)
    {
        level = new LevelDefinition
        {
            Columns = 4, Rows = 2, Slots = slots, PathLength = 20, InitialHead = 4, Speed = 0.3f,
            Blocks = new[]
            {
                new BlockDefinition { X=0, Y=0, Width=2, Height=1, Direction=ExitDirection.Up, Color=0, Capacity=2 },
                new BlockDefinition { X=2, Y=0, Width=2, Height=1, Direction=ExitDirection.Up, Color=0, Capacity=2 }
            },
            Yarn = new[] { 0, 0, 0, 0 }
        };
    }

    static void Advance(WoolGame game, int frames)
    {
        for (var i = 0; i < frames; i++) game.Step(0.02f);
    }

    static void CheckArrowRules()
    {
        Fill(out var level);
        var first=level.Blocks[0]; first.Direction=ExitDirection.Right; level.Blocks[0]=first;
        var game=new WoolGame(level);
        Check(!WoolRouting.TryFind(level,game.Blocks,0,0,out _,out _), "Routing rejects a blocked arrow instead of finding an in-board detour");
        Check(!game.CanSelect(0) && !game.Select(0), "Block cannot detour around a blocker in its arrow direction");
        Check(game.Blocks[0].Phase==0 && game.Blocks[0].Slot==-1 && game.Blocks[0].Route.Count==0 && game.Spools[0].Block<0,
            "Rejected blocked tap leaves block and tray untouched");
        foreach(var direction in new[] { ExitDirection.Up, ExitDirection.Right, ExitDirection.Down, ExitDirection.Left })
        {
            var vertical=direction==ExitDirection.Up || direction==ExitDirection.Down;
            var forward=direction==ExitDirection.Up || direction==ExitDirection.Right;
            var a=new BlockDefinition { X=2,Y=2,Width=2,Height=2,Direction=direction,Color=0,Capacity=2 };
            var b=new BlockDefinition { X=vertical?3:(forward?5:0),Y=vertical?(forward?5:0):3,Width=1,Height=1,
                Direction=ExitDirection.Up,Color=0,Capacity=1 };
            level=new LevelDefinition { Columns=6,Rows=6,Blocks=new[] { a,b },Yarn=new[] { 0,0,0 } };
            game=new WoolGame(level);
            Check(!game.CanSelect(0) && !game.Select(0), "Partial footprint overlap blocks full ray across a gap: "+direction);
            if(vertical) b.X=0; else b.Y=0;
            level.Blocks[1]=b; game=new WoolGame(level);
            Check(game.CanSelect(0), "Adjacent lane does not block arrow: "+direction);
            if(vertical) { b.X=3; b.Y=forward?0:5; } else { b.Y=3; b.X=forward?0:5; }
            level.Blocks[1]=b; game=new WoolGame(level);
            Check(game.CanSelect(0), "Block behind arrow does not obstruct it: "+direction);
        }
        level=new LevelDefinition
        {
            Columns=5,Rows=4,Slots=3,
            Blocks=new[]
            {
                new BlockDefinition { X=2,Y=1,Width=2,Height=1,Direction=ExitDirection.Left,Color=0,Capacity=1 },
                new BlockDefinition { X=1,Y=0,Width=1,Height=2,Direction=ExitDirection.Up,Color=0,Capacity=1 },
                new BlockDefinition { X=4,Y=0,Width=1,Height=1,Direction=ExitDirection.Right,Color=0,Capacity=1 }
            },
            Yarn=new[] { 0,0,0 }
        };
        game=new WoolGame(level);
        Check(!game.CanSelect(0), "Green left arrow is blocked by tall purple block");
        Check(game.Select(2) && game.Select(1), "Outer block and purple blocker both depart");
        Check(game.Blocks[1].Travel==0 && !game.Select(0), "Moving blocker still at its start closes green arrow lane");
        for(var i=0;i<500 && !game.CanSelect(0);i++) game.Step(0.02f);
        Check(game.CanSelect(0) && game.Blocks[1].Phase==1 && game.Select(0),
            "Green arrow opens once moving purple footprint clears its lane before tray arrival");
    }

    static void CheckMovingBlocker()
    {
        var level=new LevelDefinition
        {
            Columns=5,Rows=3,Slots=2,
            Blocks=new[]
            {
                new BlockDefinition { X=4,Y=1,Width=1,Height=1,Direction=ExitDirection.Left,Color=0,Capacity=1 },
                new BlockDefinition { X=1,Y=0,Width=1,Height=1,Direction=ExitDirection.Up,Color=0,Capacity=1 }
            },
            Yarn=new[] { 0,0 }
        };
        var game=new WoolGame(level);
        Check(game.CanSelect(1) && game.Select(0), "Crossing lane is open before the mover departs");
        Check(!game.CanSelect(1), "Mover blocks the lane its remaining path will cross from the first step");
        var half=WoolRouting.CellSize*level.Columns*0.5f;
        var shortOfColumn=false;
        var reopened=false;
        var offBoard=false;
        for(var i=0;i<500 && game.Blocks[0].Phase==1 && !offBoard;i++)
        {
            game.Step(0.02f);
            WoolRouting.Evaluate(game.Blocks[0].Route,game.Blocks[0].Travel*WoolRouting.Speed,out var point,out _);
            var rightEdge=point.X/WoolRouting.CellSize+level.Columns*0.5f+0.5f;
            if(rightEdge>1.0001f)
            {
                if(rightEdge>2.7f) shortOfColumn=true;
                Check(!game.CanSelect(1),"Lane stays closed while the mover's right edge is past the column");
            }
            else if(rightEdge<0.9999f)
            {
                reopened=true;
                Check(game.CanSelect(1),"Lane is open once the mover's right edge has cleared the column");
            }
            offBoard=point.X<-half-WoolRouting.CellSize*0.5f;
        }
        Check(shortOfColumn && reopened,"Mover was sampled both short of the column and past it");
        Check(offBoard && game.Blocks[0].Phase==1 && game.CanSelect(1) && game.Select(1), "Lane is open once the mover is off the board, before it reaches the tray");
    }

    static void CheckArrivalOrder()
    {
        Fill(out var level);
        var gap=(WoolRouting.Length(new[] { WoolRouting.BlockCenter(level.Blocks[1],level),WoolRouting.SlotCenter(0) })-
            WoolRouting.Length(new[] { WoolRouting.BlockCenter(level.Blocks[0],level),WoolRouting.SlotCenter(1) }))/WoolRouting.Speed;
        var capacity=(int)Math.Ceiling(gap/WoolGame.PullDuration)+1;
        level.Blocks[0].Capacity=capacity;
        level.Blocks[1].Capacity=capacity;
        level.Yarn=new int[capacity*2];
        var game=new WoolGame(level);
        Check(game.Select(1) && game.Select(0), "Far block departs first, near block second");
        var far=game.Blocks[1];
        var near=game.Blocks[0];
        Check(far.Slot==0 && near.Slot==1 && near.Duration<far.Duration, "Later-selected block has the shorter route");
        var sawEarlyPull=false;
        var sawLatePull=false;
        for(var i=0;i<500 && game.Status==RoundStatus.Playing;i++)
        {
            game.Step(0.02f);
            var farSpool=game.Spools[0];
            var nearSpool=game.Spools[1];
            Check(farSpool.Arrival<=0 || farSpool.Target<0,"Spool with a block still traveling never holds a pull target");
            Check(nearSpool.Arrival<=0 || nearSpool.Target<0,"Second spool with a block still traveling never holds a pull target");
            if(nearSpool.Arrival<=0 && farSpool.Arrival>0 && nearSpool.Target>=0) sawEarlyPull=true;
            if(nearSpool.Arrival<=0 && farSpool.Arrival<=0 && farSpool.Target>=0 && nearSpool.Target>=0)
            {
                sawLatePull=true;
                Check(farSpool.Target!=nearSpool.Target,"Two spools never share a yarn target");
            }
        }
        Check(sawEarlyPull,"Spool whose block arrives first pulls before the earlier-selected one is ready");
        Check(sawLatePull,"Both spools pull once both blocks have arrived");
        Check(game.Status==RoundStatus.Won && game.Remaining==0,"Same-colour blocks arriving out of order still win");
    }

    static float Overlap(BlockState a, BlockState b)
    {
        WoolRouting.Evaluate(a.Route,a.Travel*WoolRouting.Speed,out var pa,out _);
        WoolRouting.Evaluate(b.Route,b.Travel*WoolRouting.Speed,out var pb,out _);
        var reach=WoolRouting.MaxHalf(a.Definition)+WoolRouting.MaxHalf(b.Definition);
        return Math.Min(reach-Math.Abs(pa.X-pb.X),reach-Math.Abs(pa.Z-pb.Z));
    }

    static void CheckYielding()
    {
        var level=new LevelDefinition
        {
            Columns=4,Rows=3,Slots=2,
            Blocks=new[]
            {
                new BlockDefinition { X=0,Y=0,Width=1,Height=1,Direction=ExitDirection.Right,Color=0,Capacity=1 },
                new BlockDefinition { X=0,Y=1,Width=1,Height=1,Direction=ExitDirection.Right,Color=0,Capacity=1 }
            },
            Yarn=new[] { 0,0 }
        };
        var game=new WoolGame(level);
        var first=game.Blocks[0];
        var second=game.Blocks[1];
        Check(game.Select(0),"First same-column block departs");
        Advance(game,(int)Math.Floor(WoolRouting.CellSize/(WoolRouting.Speed*0.02f))+2);
        Check(game.Select(1),"Second same-column block departs one cell plus one frame of travel later");
        var worst=0f;
        var yielded=false;
        for(var i=0;i<1000 && (first.Phase==1 || second.Phase==1);i++)
        {
            var before=second.Travel;
            game.Step(0.02f);
            if(second.Phase==1 && second.Travel==before) yielded=true;
            if(first.Phase==1 && second.Phase==1 &&
                (first.Travel*WoolRouting.Speed>first.ExitDistance || second.Travel*WoolRouting.Speed>second.ExitDistance))
                worst=Math.Max(worst,Overlap(first,second));
        }
        Check(worst<=0.02f,"Merging blocks never overlap by more than 0.02 once either is off the board");
        Check(yielded,"The later-selected block yielded while merging");
        Check(first.Phase==2 && second.Phase==2 && first.Travel==first.Duration && second.Travel==second.Duration,"Both merging blocks arrive");
        Advance(game,400);
        Check(game.Status==RoundStatus.Won && game.Remaining==0,"Round still wins after yielding");
    }

    static void CheckOppositeSides()
    {
        var level=new LevelDefinition
        {
            Columns=4,Rows=3,Slots=2,
            Blocks=new[]
            {
                new BlockDefinition { X=0,Y=1,Width=1,Height=1,Direction=ExitDirection.Left,Color=0,Capacity=1 },
                new BlockDefinition { X=3,Y=1,Width=1,Height=1,Direction=ExitDirection.Right,Color=0,Capacity=1 }
            },
            Yarn=new[] { 0,0 }
        };
        var game=new WoolGame(level);
        Check(game.Select(0) && game.Select(1),"Blocks on opposite sides both depart");
        var a=game.Blocks[0];
        var b=game.Blocks[1];
        for(var i=0;i<1000 && (a.Phase==1 || b.Phase==1);i++)
        {
            var ta=a.Travel; var tb=b.Travel;
            var both=a.Phase==1 && b.Phase==1;
            game.Step(0.02f);
            if(both) Check(a.Travel>ta && b.Travel>tb,"Blocks on opposite sides never yield to each other");
        }
        Check(a.Phase==2 && b.Phase==2,"Opposite-side blocks both arrive");
    }

    static void CheckTallBoardRejected()
    {
        Fill(out var level);
        level.Rows=(int)Math.Floor((WoolRouting.TrayZ-WoolRouting.PadHalfDepth-WoolRouting.LaneClearance-WoolRouting.BoardCenterZ-WoolRouting.ExitMargin-2*WoolRouting.CellSize)/(WoolRouting.CellSize*0.5f))+1;
        var errors=WoolGame.Validate(level);
        Check(new System.Collections.Generic.List<string>(errors).Contains("Board too tall for the tray lane"),
            "Level validation rejects a board whose lane would cross the tray");
        Fill(out level);
        Check(WoolGame.Validate(level).Count==0,"Small board passes level validation");
    }

    static void CheckStaggered()
    {
        Fill(out var level);
        var game=new WoolGame(level);
        Check(game.Select(0), "First block departs");
        Advance(game,10);
        var first=game.Blocks[0];
        var travel=first.Travel;
        Check(game.Select(1), "Second block departs while the first is moving");
        game.Step(0.02f);
        var second=game.Blocks[1];
        Check(Math.Abs(first.Travel-travel-0.02f)<0.0001f && Math.Abs(second.Travel-0.02f)<0.0001f,
            "Later departure neither pauses nor restarts the earlier one");
        Check(game.Spools[0].Arrival<game.Spools[1].Arrival && game.Spools[0].Arrival==first.Duration-first.Travel &&
            game.Spools[1].Arrival==second.Duration-second.Travel, "Staggered spools keep separate arrival clocks");
        var firstHeld=first.Travel;
        var secondHeld=second.Travel;
        game.SetPaused(true);
        Advance(game,20);
        Check(first.Travel==firstHeld && second.Travel==secondHeld && first.Phase==1 && second.Phase==1, "Pause freezes every moving block");
        game.SetPaused(false);
        game.Step(0.02f);
        Check(first.Travel>firstHeld && second.Travel>secondHeld, "Resume moves every block again");
    }

    static int Main()
    {
        ProgressChecks.Run();
        FeelChecks.Run();
        RouteChecks.Run();
        StageChecks.Run();
        ShapeChecks.Run();
        GeneratorChecks.Run();
        CheckArrowRules();
        CheckMovingBlocker();
        CheckStaggered();
        CheckArrivalOrder();
        CheckYielding();
        CheckOppositeSides();
        CheckTallBoardRejected();
        Fill(out var level);
        var game = new WoolGame(level);
        Check(game.CanSelect(0), "Unblocked arrows allow selection");
        Check(game.Blocks[0].Phase == 0 && game.Blocks[0].Route.Count == 0 && game.Spools[0].Block < 0,
            "CanSelect does not reserve or mutate state");
        Check(game.Select(0) && game.Select(1), "Both blocks depart immediately");
        Check(!game.Select(0), "Duplicate tap cannot reserve twice");
        var first = game.Blocks[0];
        var second = game.Blocks[1];
        Check(first.Slot == 0 && second.Slot == 1 && game.Spools[0].Block == 0 && game.Spools[1].Block == 1,
            "Concurrent blocks reserve distinct slots");
        Check(first.Route.Count > 1 && first.ExitDistance > 0 &&
            Math.Abs(first.Duration * WoolRouting.Speed - WoolRouting.Length(first.Route)) < 0.0001f,
            "Route, duration and exit distance are stored at selection");
        game.Step(0.02f);
        Check(first.Travel > 0 && second.Travel > 0 && first.Phase == 1 && second.Phase == 1, "Both departures move in the same Step");
        Check(game.Spools[0].Arrival == first.Duration - first.Travel && game.Spools[1].Arrival == second.Duration - second.Travel,
            "Each spool Arrival counts down its own block");
        Check(game.Remaining == 4 && game.Spools[0].Target < 0 && game.Spools[1].Target < 0,
            "Reserved spools cannot pull while traveling");
        for (var i = 0; i < 500 && (first.Phase == 1 || second.Phase == 1); i++)
        {
            game.Step(0.02f);
            Check(game.Spools[0].Arrival <= 0 || game.Spools[0].Target < 0, "Spool 0 cannot pull while its block travels");
            Check(game.Spools[1].Arrival <= 0 || game.Spools[1].Target < 0, "Spool 1 cannot pull while its block travels");
            Check(game.Spools[0].Arrival == Math.Max(0, first.Duration - first.Travel) &&
                game.Spools[1].Arrival == Math.Max(0, second.Duration - second.Travel),
                "Spool arrivals advance independently every Step");
        }
        Check(first.Phase == 2 && second.Phase == 2 && first.Travel == first.Duration && second.Travel == second.Duration &&
            game.Spools[0].Arrival == 0 && game.Spools[1].Arrival == 0, "Completion clamps travel and makes both spools ready");
        Advance(game, 400);
        Check(game.Status == RoundStatus.Won && game.Remaining == 0, "Concurrent matching spools win without double consumption");
        Advance(game, 30);
        Check(game.Spools[0].Block < 0 && game.Spools[1].Block < 0, "Completed spools release reserved slots");

        Fill(out level, 1);
        game = new WoolGame(level);
        Check(game.Select(1), "Reserve last slot");
        Advance(game, 16);
        Check(!game.Select(0) && game.Status == RoundStatus.Playing, "Full tray blocks selection without premature loss");
        var head = game.Head;
        var elapsed = game.Elapsed;
        var travel = game.Blocks[1].Travel;
        var arrival = game.Spools[0].Arrival;
        game.SetPaused(true);
        Advance(game, 100);
        Check(game.Head == head && game.Elapsed == elapsed && game.Blocks[1].Travel == travel &&
            game.Spools[0].Arrival == arrival && game.Remaining == 4, "Pause freezes travel, arrival, yarn and dragon");
        Check(!game.CanSelect(0) && !game.UnlockSlot(), "Pause rejects selection and unlock");
        game.SetPaused(false);
        Check(game.UnlockSlot() && game.Select(0), "Unlock creates a reservable slot");
        Advance(game, 400);
        Check(game.Status == RoundStatus.Won, "Resume and unlock complete round");
        var reset = new WoolGame(level);
        Check(reset.Remaining == 4 && reset.Blocks[1].Phase == 0 && reset.Spools[0].Block < 0 &&
            reset.Blocks[1].Route.Count == 0 && reset.Blocks[1].Travel == 0 && reset.Blocks[1].Duration == 0 &&
            reset.Blocks[1].ExitDistance == 0 && reset.Blocks[1].Slot == -1, "New round clears all route and travel state");
        Check(reset.Select(0), "Fresh round accepts first departure");
        reset.Step(0.02f);
        Check(reset.Blocks[0].Travel > 0, "Fresh round departs without stale travel");

        Fill(out level);
        level.InitialHead = 0.2f;
        level.Spacing = 1;
        level.Speed = 0.01f;
        game = new WoolGame(level);
        Check(!game.HasVisibleColor(0), "Unspawned yarn must not be targetable");
        game.Select(1);
        for (var i = 0; i < 500 && game.Blocks[1].Phase != 2; i++) game.Step(0.02f);
        Advance(game, 20);
        Check(game.Blocks[1].Phase == 2 && game.Remaining == 4 && game.Spools[0].Target < 0,
            "Arrived spool waits for hidden yarn");
        Fill(out level);
        level.PathLength = 4.01f;
        game = new WoolGame(level);
        Advance(game, 10);
        Check(game.Status == RoundStatus.Lost && !game.Select(0), "Dragon reaching kitten loses and rejects selection");
        Fill(out level);
        level.Blocks[1].X = 1;
        Check(WoolGame.Validate(level).Count > 0, "Overlapping blocks rejected");
        Fill(out level);
        level.Yarn = new[] { 0 };
        Check(WoolGame.Validate(level).Count > 0, "Unbalanced color capacity rejected");
        foreach (var direction in new[] { ExitDirection.Up, ExitDirection.Right, ExitDirection.Down, ExitDirection.Left })
        {
            Fill(out level);
            var a = level.Blocks[0];
            a.Direction = direction;
            a.Width = a.Height = 1;
            var b = level.Blocks[1];
            b.Width = b.Height = 1;
            a.X = a.Y = 1;
            b.X = 1 + (direction == ExitDirection.Right ? 1 : direction == ExitDirection.Left ? -1 : 0);
            b.Y = 1 + (direction == ExitDirection.Up ? 1 : direction == ExitDirection.Down ? -1 : 0);
            level.Rows = level.Columns = 4;
            level.Blocks = new[] { a, b };
            game = new WoolGame(level);
            Check(!game.CanSelect(0) && !game.Select(0), "Blocked arrow rejects alternate routes: " + direction);
        }

        level = new LevelDefinition
        {
            Columns=5, Rows=4, Slots=2,
            Blocks=new[]
            {
                new BlockDefinition { X=2,Y=2,Width=1,Height=1,Color=0,Capacity=1 },
                new BlockDefinition { X=1,Y=1,Width=3,Height=1,Color=0,Capacity=1 },
                new BlockDefinition { X=1,Y=3,Width=3,Height=1,Color=0,Capacity=1 },
                new BlockDefinition { X=1,Y=2,Width=1,Height=1,Color=0,Capacity=1 },
                new BlockDefinition { X=3,Y=2,Width=1,Height=1,Color=0,Capacity=1 }
            },
            Yarn=new[] { 0,0,0,0,0 }
        };
        game = new WoolGame(level);
        Check(!game.Select(0) && game.Spools[0].Block < 0 && game.Blocks[0].Route.Count == 0,
            "No route leaves block and slot unchanged");
        Check(game.Select(2) && !game.Select(0), "Moving gate still blocks its original arrow lane");
        for(var i=0;i<500 && !game.CanSelect(0);i++) game.Step(0.02f);
        Check(game.Blocks[2].Phase == 1 && game.Select(0), "Follower departs once the moving gate clears its arrow lane");
        game.Step(0.02f);
        Check(game.Blocks[0].Travel > 0 && game.Blocks[2].Phase == 1, "Follower moves while the gate is still on its route");

        level = new LevelDefinition
        {
            Columns=3, Rows=1, Slots=2, PathLength=30, InitialHead=1.1f, Speed=0.02f, Spacing=1,
            Blocks=new[]
            {
                new BlockDefinition { X=0,Y=0,Width=1,Height=1,Direction=ExitDirection.Up,Color=0,Capacity=1 },
                new BlockDefinition { X=1,Y=0,Width=1,Height=1,Direction=ExitDirection.Up,Color=1,Capacity=2 },
                new BlockDefinition { X=2,Y=0,Width=1,Height=1,Direction=ExitDirection.Up,Color=1,Capacity=1 }
            },
            Yarn=new[] { 0,1,1,1 }
        };
        game = new WoolGame(level);
        game.Select(0);
        game.Select(1);
        for (var i = 0; i < 500 && game.Spools[0].Block >= 0; i++) game.Step(0.02f);
        Check(game.Spools[0].Block < 0 && game.Select(2), "New spool reuses lower free slot");
        for (var i = 0; i < 2000 && game.Yarn[1].ReservedBy < 0; i++) game.Step(0.02f);
        Check(game.Blocks[2].Phase == 2 && game.Yarn[1].ReservedBy == 1,
            "Older arrived spool keeps priority after lower slot is reused");
        var routeAssertions = RouteChecks.Assertions;
        Console.WriteLine("PASS: " + passed + " gameplay assertions; " + routeAssertions + " route assertions; " + ProgressChecks.Assertions + " progress assertions; " + FeelChecks.Assertions + " feel assertions; " + StageChecks.Assertions + " stage assertions; " + ShapeChecks.Assertions + " shape assertions; " + ShapeChecks.Comparisons + " grid comparisons on " + ShapeChecks.Boards + " boards" + "; " + GeneratorChecks.Assertions + " generator assertions; fingerprint " + GeneratorChecks.Fingerprint.ToString("X8"));
        return 0;
    }
}
