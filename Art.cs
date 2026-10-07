using System.Numerics;
using Raylib_cs;

namespace JackassRun;

/// <summary>Uma folha de sprites: celulas de tamanho fixo e um ponto de ancoragem (pes ou centro).</summary>
public sealed class Sheet
{
    public Texture2D Tex, White;      // White = silhueta branca, para piscar / tingir
    public int CW, CH, AX, AY, PivotDY;
    public int Hero = -1;             // indice do heroi (velocidade das animacoes vem do HeroConfig); -1 = padrao
    /// <summary>Animacoes desenhadas a parte (pasta com um PNG por frame); substituem a linha da folha.</summary>
    public Dictionary<AState, Strip> Anims = new();
}

/// <summary>Uma animacao feita de frames soltos, juntados numa tira. Ancora = centro, logo abaixo dos pes.</summary>
public sealed class Strip
{
    public Texture2D Tex, White;
    public int W, H, AX, AY, N;
}

/// <summary>Arte carregada da pasta design/. Os PNGs que faltarem sao gerados com a arte padrao.</summary>
public static class Art
{
    public static string Root { get; private set; } = "design";
    public static Sheet[] Heroes = Array.Empty<Sheet>();
    public static Sheet[,] Grunts = new Sheet[0, 0];
    public static Sheet[] Flyers = Array.Empty<Sheet>(), Turrets = Array.Empty<Sheet>(), Dogs = Array.Empty<Sheet>(), DogBodies = Array.Empty<Sheet>();
    public static Sheet Barrel = null!, Glorb = null!, Cage = null!, Proj = null!, Hostage = null!, Parachute = null!, Car = null!;
    public static Texture2D[] Backs = Array.Empty<Texture2D>();     // paredes de fundo por era (tiles/fundo_<era>.png)
    public static Texture2D[] Tiles = Array.Empty<Texture2D>(), Sky = Array.Empty<Texture2D>(),
        Far = Array.Empty<Texture2D>(), Mid = Array.Empty<Texture2D>();
    public static Texture2D Bridge, Ladder, HouseWall, Concrete, Roof, Wagon;
    /// <summary>Sprite da arma principal de cada heroi (sprites/herois/NOME/armour/*.png), se existir.</summary>
    public static Texture2D?[] HeroWeapon = Array.Empty<Texture2D?>();
    static readonly List<Texture2D> loaded = new();

    public const int ProjBullet = 0, ProjPellet = 1, ProjLaser = 2, ProjRocket = 3, ProjERocket = 4,
        ProjGrenade = 5, ProjDynamite = 6, ProjEBullet = 7, ProjBomb = 8, ProjBatarang = 9, ProjWeb = 10, ProjArrow = 11;

    /// <summary>Procura a pasta design/: variavel JACKASS_DESIGN, depois a pasta do projeto (subindo a partir
    /// do executavel), por fim ao lado do executavel.</summary>
    static string FindRoot()
    {
        var env = Environment.GetEnvironmentVariable("JACKASS_DESIGN");
        if (!string.IsNullOrEmpty(env)) return env;
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (int i = 0; i < 7 && dir != null; i++, dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "JackassRun.csproj")))
                return Path.Combine(dir.FullName, "design");
        return Path.Combine(AppContext.BaseDirectory, "design");
    }

    public static int Load()
    {
        Root = FindRoot();
        HeroConfig.Apply();
        int generated = DesignExport.EnsureAll(Root);
        Unload();
        int eras = Eras.All.Length;
        const int cw = DesignExport.CW, ch = DesignExport.CH;
        Heroes = new Sheet[Chars.All.Length];
        HeroWeapon = new Texture2D?[Chars.All.Length];
        for (int i = 0; i < Heroes.Length; i++)
        {
            var hp = DesignExport.HeroPath(Root, i);
            Heroes[i] = Make(hp, cw, ch, DesignExport.AX, DesignExport.AY, Gfx.Pivot);
            Heroes[i].Hero = i;
            // arma principal desenhada a parte: o primeiro PNG da pasta armour/
            var armour = Path.Combine(Path.ChangeExtension(hp, null)!, "armour");
            var wpn = Directory.Exists(armour) ? Directory.GetFiles(armour, "*.png").OrderBy(f => f, StringComparer.OrdinalIgnoreCase).FirstOrDefault() : null;
            if (wpn != null) HeroWeapon[i] = Tex(wpn);
            LoadAnims(Heroes[i], Path.ChangeExtension(hp, null));     // ex.: sprites/herois/batman/running/
        }
        Grunts = new Sheet[eras, DesignExport.GruntSlug.Length];
        Flyers = new Sheet[eras]; Turrets = new Sheet[eras]; Dogs = new Sheet[eras]; DogBodies = new Sheet[eras];
        Tiles = new Texture2D[eras]; Backs = new Texture2D[eras]; Sky = new Texture2D[eras]; Far = new Texture2D[eras]; Mid = new Texture2D[eras];
        for (int e = 0; e < eras; e++)
        {
            for (int k = 0; k < DesignExport.GruntSlug.Length; k++)
                Grunts[e, k] = Make(DesignExport.GruntPath(Root, e, k), cw, ch, DesignExport.AX, DesignExport.AY, Gfx.Pivot);
            Flyers[e] = Make(DesignExport.FlyerPath(Root, e), 32, 24, 16, 12, 0);
            Turrets[e] = Make(DesignExport.TurretPath(Root, e), 32, 16, 12, 15, 0);
            Dogs[e] = Make(DesignExport.DogPath(Root, e), DesignExport.DogW, DesignExport.DogH, DesignExport.DogW / 2, DesignExport.DogH - 1, 0);
            DogBodies[e] = Make(DesignExport.DogPath(Root, e), DesignExport.DogW, DesignExport.DogH, DesignExport.DogW / 2, DesignExport.DogH / 2, 0);   // corpo (gira no centro)
            Tiles[e] = Tex(DesignExport.TilesPath(Root, e));
            Backs[e] = Tex(DesignExport.TilePath(Root, "fundo_" + DesignExport.EraSlug[e]));
            Sky[e] = Tex(DesignExport.BgPath(Root, e, "ceu"));
            Far[e] = Tex(DesignExport.BgPath(Root, e, "fundo"));
            Mid[e] = Tex(DesignExport.BgPath(Root, e, "meio"));
            Raylib.SetTextureWrap(Far[e], TextureWrap.Repeat);
            Raylib.SetTextureWrap(Mid[e], TextureWrap.Repeat);
        }
        Bridge = Tex(DesignExport.TilePath(Root, "ponte"));
        Ladder = Tex(DesignExport.TilePath(Root, "escada"));
        HouseWall = Tex(DesignExport.TilePath(Root, "parede"));
        Concrete = Tex(DesignExport.TilePath(Root, "concreto"));
        Roof = Tex(DesignExport.TilePath(Root, "telhado"));
        Wagon = Tex(DesignExport.TilePath(Root, "vagao"));
        Hostage = Make(DesignExport.PropPath(Root, "refem"), cw, ch, DesignExport.AX, DesignExport.AY, Gfx.Pivot);
        Barrel = Make(DesignExport.PropPath(Root, "barril"), 16, 16, 8, 15, 0);
        Glorb = Make(DesignExport.PropPath(Root, "glorb"), 16, 16, 8, 8, 0);
        Cage = Make(DesignExport.PropPath(Root, "jaula"), 32, 48, 16, 47, 0);
        Proj = Make(DesignExport.PropPath(Root, "projeteis"), 32, 16, 16, 8, 0);
        Car = Make(DesignExport.PropPath(Root, "carro"), 32, 16, 16, 15, 0);
        Parachute = Make(DesignExport.PropPath(Root, "paraquedas"), 24, 18, 12, 17, 0);
        return generated;
    }

    /// <summary>Nome da pasta de cada animacao (sprites/herois/NOME_DO_HEROI/PASTA/*.png). Aceita os dois nomes
    /// (ex.: "fall" ou "falling"); se existirem os dois, vale o primeiro da lista.</summary>
    public static readonly (string dir, AState st)[] AnimDirs =
    {
        ("stop", AState.Idle), ("idle", AState.Idle), ("running", AState.Run), ("run", AState.Run),
        ("jumping", AState.Jump), ("jump", AState.Jump), ("falling", AState.Fall), ("fall", AState.Fall),
        ("climbing", AState.Climb), ("climb", AState.Climb), ("dash", AState.Dash), ("dashing", AState.Dash),
        ("hurt", AState.Hurt), ("cheer", AState.Cheer), ("tumble", AState.Tumble),
    };

    static void LoadAnims(Sheet s, string dir)
    {
        if (!Directory.Exists(dir)) return;
        foreach (var (name, st) in AnimDirs)
        {
            var d = Path.Combine(dir, name);
            if (s.Anims.ContainsKey(st) || !Directory.Exists(d)) continue;
            var files = Directory.GetFiles(d, "*.png").OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToArray();
            if (files.Length == 0) continue;
            try { s.Anims[st] = MakeStrip(files); }
            catch (Exception ex) { Console.WriteLine($"[animacao {d}] erro: {ex.Message}"); }
        }
    }

    /// <summary>Junta os frames numa tira. Se o PNG nao tiver transparencia, a cor do canto (ex.: branco) vira fundo.</summary>
    static unsafe Strip MakeStrip(string[] files)
    {
        var imgs = files.Select(f => { var im = Raylib.LoadImage(f); Raylib.ImageFormat(ref im, PixelFormat.UncompressedR8G8B8A8); return im; }).ToArray();
        int w = imgs.Max(i => i.Width), h = imgs.Max(i => i.Height), n = imgs.Length;
        var strip = Raylib.GenImageColor(w * n, h, Color.Blank);
        Raylib.ImageFormat(ref strip, PixelFormat.UncompressedR8G8B8A8);
        int bottom = 0;
        for (int k = 0; k < n; k++)
        {
            var im = imgs[k];
            byte* d = (byte*)im.Data;
            byte a0 = d[3];
            byte* o = (byte*)strip.Data;
            var bg = a0 == 255 ? Background(d, im.Width, im.Height) : null;
            for (int y = 0; y < im.Height; y++)
                for (int x = 0; x < im.Width; x++)
                {
                    byte* p = d + (y * im.Width + x) * 4;
                    if (p[3] == 0) continue;
                    if (bg != null && bg[y * im.Width + x]) continue;                    // fundo
                    byte* q = o + (y * w * n + k * w + x) * 4;
                    q[0] = p[0]; q[1] = p[1]; q[2] = p[2]; q[3] = p[3];
                    bottom = Math.Max(bottom, y);
                }
            Raylib.UnloadImage(im);
        }
        var tex = Raylib.LoadTextureFromImage(strip);
        byte* sd = (byte*)strip.Data;
        for (int i = 0; i < w * n * h; i++) { sd[i * 4] = 255; sd[i * 4 + 1] = 255; sd[i * 4 + 2] = 255; }
        var white = Raylib.LoadTextureFromImage(strip);
        Raylib.UnloadImage(strip);
        Raylib.SetTextureFilter(tex, TextureFilter.Point);
        Raylib.SetTextureFilter(white, TextureFilter.Point);
        loaded.Add(tex); loaded.Add(white);
        return new Strip { Tex = tex, White = white, W = w, H = h, N = n, AX = w / 2, AY = bottom + 1 };
    }

    /// <summary>Fundo de um PNG sem transparencia: a cor do canto e as cores quase iguais a ela que estao ligadas a
    /// borda da imagem (tira o "halo" claro que fica em volta do desenho; brancos de dentro, como olhos, ficam).</summary>
    static unsafe bool[] Background(byte* d, int w, int h)
    {
        const int Tol = 24;
        byte r0 = d[0], g0 = d[1], b0 = d[2];
        var bg = new bool[w * h];
        var q = new Stack<int>();
        bool Near(int i)
        {
            byte* p = d + i * 4;
            return p[3] == 255 && Math.Abs(p[0] - r0) <= Tol && Math.Abs(p[1] - g0) <= Tol && Math.Abs(p[2] - b0) <= Tol;
        }
        for (int x = 0; x < w; x++) { q.Push(x); q.Push((h - 1) * w + x); }
        for (int y = 0; y < h; y++) { q.Push(y * w); q.Push(y * w + w - 1); }
        while (q.Count > 0)
        {
            int i = q.Pop();
            if (bg[i] || !Near(i)) continue;
            bg[i] = true;
            int x = i % w, y = i / w;
            if (x > 0) q.Push(i - 1);
            if (x < w - 1) q.Push(i + 1);
            if (y > 0) q.Push(i - w);
            if (y < h - 1) q.Push(i + w);
        }
        // pixel exatamente da cor do canto e sempre fundo (como antes), mesmo preso dentro do desenho
        for (int i = 0; i < w * h; i++)
        {
            byte* p = d + i * 4;
            if (p[0] == r0 && p[1] == g0 && p[2] == b0) bg[i] = true;
        }
        return bg;
    }

    static void Unload()
    {
        foreach (var t in loaded) Raylib.UnloadTexture(t);
        loaded.Clear();
    }

    static Texture2D Tex(string path)
    {
        var t = Raylib.LoadTexture(path);
        Raylib.SetTextureFilter(t, TextureFilter.Point);
        loaded.Add(t);
        return t;
    }

    static unsafe Sheet Make(string path, int cw, int ch, int ax, int ay, int pivotDy)
    {
        var img = Raylib.LoadImage(path);
        Raylib.ImageFormat(ref img, PixelFormat.UncompressedR8G8B8A8);
        var tex = Raylib.LoadTextureFromImage(img);
        // silhueta branca mantendo o alpha
        byte* d = (byte*)img.Data;
        int n = img.Width * img.Height;
        for (int i = 0; i < n; i++) { d[i * 4] = 255; d[i * 4 + 1] = 255; d[i * 4 + 2] = 255; }
        var white = Raylib.LoadTextureFromImage(img);
        Raylib.UnloadImage(img);
        Raylib.SetTextureFilter(tex, TextureFilter.Point);
        Raylib.SetTextureFilter(white, TextureFilter.Point);
        loaded.Add(tex); loaded.Add(white);
        return new Sheet { Tex = tex, White = white, CW = cw, CH = ch, AX = ax, AY = ay, PivotDY = pivotDy };
    }

    /// <summary>Desenha uma celula com a ancora em (x,y). facing -1 espelha; rot gira em passos de 90 graus
    /// em torno do pivo (ancora + PivotDY). silhouette usa a versao branca tingida por tint.</summary>
    public static void Cell(Sheet s, int col, int row, float x, float y, int facing = 1, int rot = 0, Color? tint = null, bool silhouette = false)
    {
        var tex = silhouette ? s.White : s.Tex;
        var src = new Rectangle(col * s.CW, row * s.CH, facing >= 0 ? s.CW : -s.CW, s.CH);
        float ox = facing >= 0 ? s.AX : s.CW - s.AX;
        float oy = s.AY + s.PivotDY;
        var dest = new Rectangle(x, y + s.PivotDY, s.CW, s.CH);
        Raylib.DrawTexturePro(tex, src, dest, new Vector2(ox, oy), (rot & 3) * 90, tint ?? Color.White);
    }

    /// <summary>Linha e coluna da folha de personagem para um estado de animacao.</summary>
    /// <summary>Frames por segundo da corrida (HeroConfig.FpsCorrida para herois; 14 para os outros bonecos).</summary>
    static float RunFps(Sheet s, in Anim a) =>
        (s.Hero >= 0 ? HeroConfig.Of(s.Hero).FpsCorrida : 14f) * Math.Clamp(a.Speed / 88f, 0.55f, 1.4f);

    public static (int col, int row) Frame(Sheet s, in Anim a)
    {
        float t = a.T;
        switch (a.S)
        {
            case AState.Run:
                float rate = RunFps(s, a);
                return ((int)(t * rate) % 6, 1);
            case AState.Jump: return (0, 2);
            case AState.Fall: return (1 + (int)(t * 10) % 2, 2);
            case AState.Climb: return ((int)(t * 9) & 1, 3);
            case AState.Dash: return (2, 3);
            case AState.Hurt: return (3, 3);
            case AState.Cheer: return ((int)(t * 6) & 1, 4);
            case AState.Tumble: return (2 + ((int)(t * 12) & 1), 4);
            default:
                if (a.Land > 0) return (3, 2);
                if (t % 3.4f < 0.12f) return (3, 0);
                if (MathF.Sin(t * 3f) > 0.6f) return (1, 0);
                return ((int)(t * 0.5f) % 2 == 0 ? 0 : 2, 0);
        }
    }

    /// <summary>Frame atual de uma animacao solta. Correr segue a velocidade do heroi.</summary>
    static int StripIndex(Sheet s, Strip st, in Anim a)
    {
        float fps = a.S == AState.Run ? RunFps(s, a) : s.Hero >= 0 ? HeroConfig.Of(s.Hero).FpsAnimacoes : 10f;
        return (int)(a.T * fps) % st.N;
    }

    static void StripFrame(Sheet s, Strip st, int i, float x, float y, int facing, int rot, Color tint, bool silhouette)
    {
        var src = new Rectangle(i * st.W, 0, facing >= 0 ? st.W : -st.W, st.H);
        float ox = facing >= 0 ? st.AX : st.W - st.AX;
        var dest = new Rectangle(x, y + s.PivotDY, st.W, st.H);
        Raylib.DrawTexturePro(silhouette ? st.White : st.Tex, src, dest, new Vector2(ox, st.AY + s.PivotDY), (rot & 3) * 90, tint);
    }

    /// <summary>Personagem animado. over != null desenha a silhueta naquela cor (dano, fantasmas, rastros).</summary>
    public static void Human(Sheet s, int fx, int fy, int facing, in Anim a, float alpha = 1, Color? over = null, Color? tint = null)
    {
        int x = fx - (int)a.Recoil * facing;
        var c = over ?? tint ?? Color.White;
        if (s.Anims.TryGetValue(a.S, out var st))
        {
            StripFrame(s, st, StripIndex(s, st, a), x, fy, facing, a.Rot, Col.A(c, alpha), over.HasValue);
            return;
        }
        var (col, row) = Frame(s, a);
        Cell(s, col, row, x, fy, facing, a.Rot, Col.A(c, alpha), over.HasValue);
    }
}
