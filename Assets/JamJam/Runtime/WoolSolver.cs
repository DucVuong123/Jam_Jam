using System;

namespace JamJam
{
    public static class WoolSolver
    {
        public const float Delta = 0.02f;
        public const int MaxFrames = 30000;
        public const float TapInterval = 1.5f;

        public static void Run(LevelDefinition data, out bool won, out float peak, out float elapsed)
        {
            var game = new WoolGame(data);
            peak = game.Head / game.PathLength;
            var wait = 0f;
            for (var frame = 0; frame < MaxFrames && game.Status == RoundStatus.Playing; frame++)
            {
                wait -= Delta;
                if (wait <= 0)
                    for (var b = 0; b < game.Blocks.Count; b++)
                        if (game.Blocks[b].Phase == 0 && game.HasVisibleColor(game.Blocks[b].Definition.Color) && game.CanSelect(b))
                        {
                            game.Select(b);
                            wait = TapInterval;
                            break;
                        }
                game.Step(Delta);
                peak = Math.Max(peak, game.Head / game.PathLength);
            }
            won = game.Status == RoundStatus.Won;
            elapsed = game.Elapsed;
        }
    }
}
