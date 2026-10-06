using Raylib_cs;
using static JackassRun.Col;

namespace JackassRun;

public sealed partial class Game
{
    enum Mode { Title, Play, Dying, Rewind, Select, Paused, GameOver }

    Mode mode = Mode.Title, pausedFrom;
    public WorldState S = new();
    public Terrain Ter = new();
    readonly List<WorldState> hist = new();
    public Player P = new();
    readonly List<Ghost> ghosts = new();
    Ghost? rec;                                   // gravacao da vida atual
    readonly List<Particle> parts = new();
    readonly List<FloatText> texts = new();
    readonly List<Corpse> corpses = new();
    readonly List<Trail> trails = new();
    readonly Random fx = new();                   // aleatoriedade so visual (fora do snapshot)
    readonly Sfx sfx;

    uint seed;
    int timeOuts, glorbCount, best, selIdx, rewindIdx, lastEra = -1, lastScore;
    float modeT, rewindAcc, shake, flash, eraBannerT, titleT, hitStop;
    string deathMsg = "", eraBanner = "", eraSub = "";
    bool quit, newBest;
    InputState pending;
    bool prevL, prevR;

    public Game()
    {
        Art.Load();
        sfx = new Sfx();
        best = LoadBest();
        StartWorld();
    }

    // ------------------------------------------------------------------ loop

    public void Run()
    {
        // 1 coluna extra para o deslocamento sub-pixel da camera
        var rt = Raylib.LoadRenderTexture(K.W + 1, K.H);
        Raylib.SetTextureFilter(rt.Texture, TextureFilter.Point);
        double acc = 0;
        while (!Raylib.WindowShouldClose() && !quit)
        {
            float ft = MathF.Min(Raylib.GetFrameTime(), 0.1f);
            var raw = Inp.Read();
            if (bot != null) raw = BotInput();
            pending.MergePressed(raw);
            if (Raylib.IsKeyPressed(KeyboardKey.F11) || Raylib.IsKeyPressed(KeyboardKey.F)) ToggleFull();
            if (Raylib.IsKeyPressed(KeyboardKey.M)) sfx.MusicOn = !sfx.MusicOn;
            if (Raylib.IsKeyPressed(KeyboardKey.F5)) Art.Load();      // recarrega a arte editada

            acc += ft;
            int steps = 0;
            while (acc >= K.DT && steps < 4)
            {
                var i = raw;
                i.ClearPressed(); i.MergePressed(pending);
                pending.ClearPressed();
                Tick(i);
                acc -= K.DT; steps++;
            }
            if (steps == 4) acc = 0;

            float mPitch = mode == Mode.Rewind ? 0.55f : mode == Mode.Dying ? 0.8f : 1f;
            float mVol = mode is Mode.Paused or Mode.GameOver ? 0.15f : mode == Mode.Select ? 0.25f : 0.45f;
            sfx.UpdateMusic(mPitch, mVol);

            Compose(rt);
            if (bot != null) BotCapture();
            Raylib.EndDrawing();
        }
        Raylib.UnloadRenderTexture(rt);
        sfx.Close();
    }

    static void ToggleFull()
    {
        if (OperatingSystem.IsMacOS()) Raylib.ToggleBorderlessWindowed();
        else Raylib.ToggleFullscreen();
    }

    void Tick(InputState i)
    {
        modeT += K.DT;
        switch (mode)
        {
            case Mode.Title:
                titleT += K.DT;
                S.CamX += 40 * K.DT;
                EnsureChunks();
                foreach (var p in S.Props) p.T += K.DT;
                foreach (var e in S.Enemies) e.AnimT += K.DT;
                if (i.Confirm && modeT > 0.3f) { sfx.Play("spawn"); NewRun(); }
                if (i.Pause && modeT > 0.3f) quit = true;
                break;

            case Mode.Play:
                if (i.Pause) { pausedFrom = mode; mode = Mode.Paused; modeT = 0; break; }
                if (hitStop > 0) { hitStop -= K.DT; break; }
                if (i.Rewind && timeOuts > 0 && !P.Dead) { VoluntaryTimeOut(); break; }
                Step(i);
                UpdateFx();
                break;

            case Mode.Dying:
                UpdateFx();
                if (modeT > 1.3f)
                {
                    if (timeOuts > 0) BeginRewind();
                    else GameOver();
                }
                break;

            case Mode.Rewind: UpdateRewind(i); break;

            case Mode.Select:
                if (i.Left && !prevL) { selIdx = (selIdx + Chars.All.Length - 1) % Chars.All.Length; sfx.Play("select"); }
                if (i.Right && !prevR) { selIdx = (selIdx + 1) % Chars.All.Length; sfx.Play("select"); }
                if (i.Confirm && modeT > 0.25f)
                {
                    timeOuts--;
                    SpawnPlayer(selIdx);
                    mode = Mode.Play; modeT = 0;
                }
                break;

            case Mode.Paused:
                if (i.Pause || i.Confirm) { mode = pausedFrom; modeT = 0; }
                else if (i.Back) { StartWorld(); }
                break;

            case Mode.GameOver:
                UpdateFx();
                if (i.Confirm && modeT > 1f) { sfx.Play("spawn"); NewRun(); }
                else if ((i.Back || i.Pause) && modeT > 0.5f) StartWorld();
                break;
        }
        prevL = i.Left; prevR = i.Right;
        if (shake > 0) shake = MathF.Max(0, shake - K.DT * 18);
        if (flash > 0) flash -= K.DT * 3;
        if (eraBannerT > 0) eraBannerT -= K.DT;
    }

    // ------------------------------------------------------------------ fluxo da partida

    /// <summary>Mundo de demonstracao para a tela de titulo.</summary>
    void StartWorld()
    {
        seed = (uint)Environment.TickCount | 1u;
        S = new WorldState { Rng = new Rng(seed * 2654435761u) };
        Ter = new Terrain();
        hist.Clear(); ghosts.Clear(); parts.Clear(); texts.Clear(); corpses.Clear(); trails.Clear(); decals.Clear();
        rec = null; P = new Player { Dead = true };
        lastEra = -1;
        EnsureChunks();
        mode = Mode.Title; modeT = 0; titleT = 0;
    }

    void NewRun()
    {
        seed = (uint)Environment.TickCount | 1u;
        S = new WorldState { Rng = new Rng(seed * 2654435761u) };
        Ter = new Terrain();
        hist.Clear(); ghosts.Clear(); parts.Clear(); texts.Clear(); corpses.Clear(); trails.Clear(); decals.Clear();
        timeOuts = 3; glorbCount = 0; lastEra = -1; newBest = false;
        EnsureChunks();
        SpawnPlayer(0);
        hist.Add(S.Clone());
        mode = Mode.Play; modeT = 0;
    }

    void SpawnPlayer(int ch)
    {
        // procura chao firme alguns tiles a frente da borda esquerda
        float x = S.CamX + 70, y = 30;
        int start = (int)(x / K.T);
        for (int k = 0; k < 30; k++)
        {
            int tx = start + k;
            int sy = Ter.Surface(tx);
            if (sy > 2 && !Ter.Solid(tx, sy - 1) && !Ter.Solid(tx, sy - 2))
            { x = tx * K.T + K.T / 2f; y = sy * K.T; break; }
        }
        P = new Player { Char = ch, X = x, Y = y, InvulnT = 2f, Specials = 3 };
        rec = new Ghost { Char = ch, StartFrame = S.Frame + 1 };
        sfx.Play("spawn");
        // feixe de teletransporte estilo STF
        for (int k = 0; k < 24; k++)
            AddPart(PKind.Pixel, x + fx.Next(-4, 5), y - fx.Next(0, 60), 0, fx.Next(-20, 60), 0.5f, 2, Cyan);
        AddPart(PKind.Ring, x, y - 8, 0, 0, 0.4f, 20, White);
        flash = 0.4f;
    }

    void KillPlayer(int killerId, string msg, bool boom = false)
    {
        if (P.Dead || mode != Mode.Play) return;
        if (P.InvulnT > 0 || P.DashT > 0) return;
        if (P.Shield > 0)
        {
            P.Shield--; P.InvulnT = 1.2f; sfx.Play("shield");
            AddPart(PKind.Ring, P.X, P.Y - 11, 0, 0, 0.4f, 16, Cyan);
            AddText(P.X, P.Y - 24, "ESCUDO!", Cyan);
            return;
        }
        P.Dead = true;
        deathMsg = msg;
        sfx.Play("pdie"); sfx.Play("boom", 0.6f);
        shake = 5; flash = 0.6f; hitStop = 0;
        var lk = Chars.All[P.Char].Look;
        if (boom) Gibs(P.X, P.Y - 10, lk.Skin, lk.Shirt, lk.Pants, BloodRed, 40);
        else
        {
            AddCorpse(P.X, P.Y + Gfx.Pivot, Art.Heroes[P.Char], P.Facing, -P.Facing, false, null);
            BloodSpray(P.X, P.Y - 10, -P.Facing, 22, BloodRed, 1.3f);
        }
        if (rec != null) { rec.Died = true; rec.KillerId = killerId; ghosts.Add(rec); rec = null; }
        mode = Mode.Dying; modeT = 0;
    }

    /// <summary>"Time Out" voluntario: a vida atual vira um fantasma e voce volta no tempo.</summary>
    void VoluntaryTimeOut()
    {
        if (rec != null) { rec.Died = false; ghosts.Add(rec); rec = null; }
        P.Dead = true;
        deathMsg = "TIME OUT!";
        BeginRewind();
    }

    void BeginRewind()
    {
        mode = Mode.Rewind; modeT = 0;
        rewindIdx = hist.Count - 1; rewindAcc = 0;
        parts.Clear(); texts.Clear(); corpses.Clear(); trails.Clear(); decals.Clear();
        sfx.Play("rewind");
    }

    void UpdateRewind(InputState i)
    {
        // acelera conforme segura; volta no historico frame a frame
        float speed = MathF.Min(1.5f + modeT * 2.2f, 6f);
        rewindAcc += speed;
        while (rewindAcc >= 1 && rewindIdx > 0) { rewindIdx--; rewindAcc -= 1; }
        S = hist[rewindIdx];
        Ter.RewindTo(S.Frame);
        if ((i.Confirm || i.Fire || i.Jump) && modeT > 0.35f || rewindIdx == 0)
        {
            // trava o ponto escolhido como o novo presente
            S = hist[rewindIdx].Clone();
            hist.RemoveRange(rewindIdx + 1, hist.Count - rewindIdx - 1);
            foreach (var g in ghosts) if (g.EndFrame >= S.Frame) g.Resolved = false;
            mode = Mode.Select; modeT = 0;
            selIdx = P.Char;
            sfx.Play("select");
        }
    }

    void GameOver()
    {
        int final = FinalScore();
        lastScore = final;
        if (final > best) { best = final; newBest = true; SaveBest(best); }
        mode = Mode.GameOver; modeT = 0;
    }

    int Meters => (int)(S.CamX / K.PX_PER_M);
    int FinalScore() => S.Score + Meters * 10;

    static string BestPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JackassRun", "best.txt");
    static int LoadBest()
    {
        try { return int.Parse(File.ReadAllText(BestPath).Trim()); } catch { return 0; }
    }
    static void SaveBest(int v)
    {
        try { Directory.CreateDirectory(Path.GetDirectoryName(BestPath)!); File.WriteAllText(BestPath, v.ToString()); } catch { }
    }
}
