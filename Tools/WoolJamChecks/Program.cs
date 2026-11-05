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
        foreach(ExitDirection direction in Enum.GetValues(typeof(ExitDirection)))
        {
            var vertical=direction==ExitDirection.Up || direction==ExitDirection.Down;
            var forward=direction==ExitDirection.Up || direction==ExitDirection.Right;
            var a=new BlockDefinition { X=3,Y=3,Width=2,Height=2,Direction=direction,Color=0,Capacity=2 };
            var b=new BlockDefinition { X=vertical?4:(forward?6:1),Y=vertical?(forward?6:1):4,Width=1,Height=1,
                Direction=ExitDirection.Up,Color=0,Capacity=1 };
            level=new LevelDefinition { Columns=7,Rows=7,Blocks=new[] { a,b },Yarn=new[] { 0,0,0 } };
            game=new WoolGame(level);
            Check(!game.CanSelect(0) && !game.Select(0), "Partial footprint overlap blocks full ray across a gap: "+direction);
            if(vertical) b.X=0; else b.Y=0;
            level.Blocks[1]=b; game=new WoolGame(level);
            Check(game.CanSelect(0), "Adjacent lane does not block arrow: "+direction);
            if(vertical) { b.X=4; b.Y=forward?0:6; } else { b.Y=4; b.X=forward?0:6; }
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
        Check(game.Select(2) && game.Select(1), "Outer block and purple blocker can queue in order");
        Check(game.Blocks[1].Travel==0 && !game.Select(0), "Queued unmoved blocker still closes green arrow lane");
        for(var i=0;i<500 && !game.CanSelect(0);i++) game.Step(0.02f);
        Check(game.CanSelect(0) && game.Blocks[1].Phase==1 && game.Select(0),
            "Green arrow opens once moving purple footprint clears its lane before tray arrival");
    }

    static int Main()
    {
        RouteChecks.Run();
        CheckArrowRules();
        Fill(out var level);
        var game = new WoolGame(level);
        Check(game.CanSelect(0), "Unblocked arrows allow selection");
        Check(game.Blocks[0].Phase == 0 && game.Blocks[0].Route.Count == 0 && game.Spools[0].Block < 0,
            "CanSelect does not reserve or mutate state");
        Check(game.Select(0) && game.Select(1), "Both blocks reserve FIFO departures immediately");
        Check(!game.Select(0), "Duplicate tap cannot reserve twice");
        var first = game.Blocks[0];
        var second = game.Blocks[1];
        Check(first.Slot == 0 && second.Slot == 1 && game.Spools[0].Block == 0 && game.Spools[1].Block == 1,
            "Queued blocks reserve distinct slots");
        Check(first.Route.Count > 1 && first.ExitDistance > 0 &&
            Math.Abs(first.Duration * WoolRouting.Speed - WoolRouting.Length(first.Route)) < 0.0001f,
            "Route, duration and exit distance are stored at selection");
        var queuedArrival = game.Spools[1].Arrival;
        game.Step(0.02f);
        Check(first.Travel > 0 && second.Travel == 0, "Only first departure moves");
        Check(game.Spools[0].Arrival < first.Duration && game.Spools[1].Arrival == queuedArrival,
            "Only active arrival advances");
        Check(game.Remaining == 4 && game.Spools[0].Target < 0 && game.Spools[1].Target < 0,
            "Reserved spools cannot pull while traveling");
        for (var i = 0; i < 500 && first.Phase == 1; i++)
        {
            game.Step(0.02f);
            Check(second.Travel == 0 && game.Spools[1].Arrival == queuedArrival && game.Spools[1].Target < 0,
                "Queued block and spool stay frozen until predecessor completes");
        }
        Check(first.Phase == 2 && first.Travel == first.Duration && game.Spools[0].Arrival == 0,
            "Completion clamps travel and makes spool ready");
        game.Step(0.02f);
        Check(second.Travel > 0, "Next departure starts after predecessor completes");
        Advance(game, 400);
        Check(game.Status == RoundStatus.Won && game.Remaining == 0, "Queued matching spools win without double consumption");
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
            game.Spools[0].Arrival == arrival && game.Remaining == 4, "Pause freezes FIFO travel, arrival, yarn and dragon");
        Check(!game.CanSelect(0) && !game.UnlockSlot(), "Pause rejects selection and unlock");
        game.SetPaused(false);
        Check(game.UnlockSlot() && game.Select(0), "Unlock creates a reservable slot");
        Advance(game, 400);
        Check(game.Status == RoundStatus.Won, "Resume and unlock complete round");
        var reset = new WoolGame(level);
        Check(reset.Remaining == 4 && reset.Blocks[1].Phase == 0 && reset.Spools[0].Block < 0 &&
            reset.Blocks[1].Route.Count == 0 && reset.Blocks[1].Travel == 0 && reset.Blocks[1].Duration == 0 &&
            reset.Blocks[1].ExitDistance == 0 && reset.Blocks[1].Slot == -1, "New round clears all route and queue state");
        Check(reset.Select(0), "Fresh round accepts first departure");
        reset.Step(0.02f);
        Check(reset.Blocks[0].Travel > 0, "Fresh queue starts without stale predecessor");

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
        foreach (ExitDirection direction in Enum.GetValues(typeof(ExitDirection)))
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
            Columns=5, Rows=5, Slots=2,
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
        Check(game.Select(0), "Follower can queue after gate clears the arrow lane");
        for (var i = 0; i < 500 && game.Blocks[2].Phase == 1; i++)
        {
            game.Step(0.02f);
            Check(game.Blocks[0].Travel == 0, "Enclosed follower waits until gate physically completes departure");
        }
        game.Step(0.02f);
        Check(game.Blocks[0].Travel > 0, "Follower moves only after gate completes");

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
        Console.WriteLine("PASS: " + passed + " gameplay assertions; " + routeAssertions + " route assertions");
        return 0;
    }
}
