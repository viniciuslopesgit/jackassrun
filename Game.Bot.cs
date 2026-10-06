using Raylib_cs;

namespace JackassRun;

/// <summary>Autoteste opcional: com JACKASS_AUTOTEST=&lt;pasta&gt; um bot joga sozinho
/// e salva screenshots da resolucao interna nessa pasta, depois fecha o jogo.</summary>
public sealed partial class Game
{
    readonly string? bot = Environment.GetEnvironmentVariable("JACKASS_AUTOTEST");
    int botFrame, shots;
    readonly int shotEvery = int.TryParse(Environment.GetEnvironmentVariable("JACKASS_SHOT_EVERY"), out var se) ? se : 75;

    InputState BotInput()
    {
        botFrame++;
        var i = new InputState();
        switch (mode)
        {
            case Mode.Title:
            case Mode.GameOver:
                i.Confirm = botFrame % 90 == 0; break;
            case Mode.Rewind:
                i.Confirm = modeT > 1.2f; break;
            case Mode.Select:
                i.Right = botFrame % 20 < 2; i.Confirm = modeT > 1f; break;
            case Mode.Play:
                if (Environment.GetEnvironmentVariable("JACKASS_GOD") != null) P.InvulnT = MathF.Max(P.InvulnT, 0.1f);
                i.Right = botFrame % 300 < 250;
                i.Left = !i.Right && botFrame % 300 > 280;
                i.FireHeld = true;
                bool wall = Overlaps(P.X + 8, P.Y - 2, 3, 10);
                bool gap = Ter.Surface((int)((P.X + 14) / K.T)) < 0;
                i.Jump = (wall || gap) && P.OnGround || botFrame % 70 == 0;
                i.JumpHeld = true;
                i.Special = botFrame % 400 == 0;
                break;
        }
        return i;
    }

    void BotCapture(RenderTexture2D rt)
    {
        if (botFrame % shotEvery == 0 && shots < 40)
        {
            var img = Raylib.LoadImageFromTexture(rt.Texture);
            Raylib.ImageFlipVertical(ref img);
            Directory.CreateDirectory(bot!);
            Raylib.ExportImage(img, Path.Combine(bot!, $"shot_{shots:D2}_{mode}.png"));
            Raylib.UnloadImage(img);
            shots++;
        }
        if (botFrame > shotEvery * 40) quit = true;
    }
}
