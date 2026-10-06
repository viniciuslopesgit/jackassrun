using System.Numerics;
using Raylib_cs;

namespace JackassRun;

/// <summary>Uma folha de sprites: celulas de tamanho fixo e um ponto de ancoragem (pes ou centro).</summary>
public sealed class Sheet
{
    public Texture2D Tex, White;      // White = silhueta branca, para piscar / tingir
    public int CW, CH, AX, AY, PivotDY;
}

/// <summary>Arte carregada da pasta design/. Os PNGs que faltarem sao gerados com a arte padrao.</summary>
public static class Art
{
    public static string Root { get; private set; } = "design";
    public static Sheet[] Heroes = Array.Empty<Sheet>();
    public static Sheet[,] Grunts = new Sheet[0, 0];
    public static Sheet[] Flyers = Array.Empty<Sheet>(), Turrets = Array.Empty<Sheet>();
    public static Sheet Barrel = null!, Glorb = null!, Cage = null!, Proj = null!, Hostage = null!;
    public static Texture2D[] Tiles = Array.Empty<Texture2D>(), Sky = Array.Empty<Texture2D>(),
        Far = Array.Empty<Texture2D>(), Mid = Array.Empty<Texture2D>();
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
        for (int i = 0; i < Heroes.Length; i++)
            Heroes[i] = Make(DesignExport.HeroPath(Root, i), cw, ch, DesignExport.AX, DesignExport.AY, Gfx.Pivot);
        Grunts = new Sheet[eras, DesignExport.GruntSlug.Length];
        Flyers = new Sheet[eras]; Turrets = new Sheet[eras];
        Tiles = new Texture2D[eras]; Sky = new Texture2D[eras]; Far = new Texture2D[eras]; Mid = new Texture2D[eras];
        for (int e = 0; e < eras; e++)
        {
            for (int k = 0; k < DesignExport.GruntSlug.Length; k++)
                Grunts[e, k] = Make(DesignExport.GruntPath(Root, e, k), cw, ch, DesignExport.AX, DesignExport.AY, Gfx.Pivot);
            Flyers[e] = Make(DesignExport.FlyerPath(Root, e), 32, 24, 16, 12, 0);
            Turrets[e] = Make(DesignExport.TurretPath(Root, e), 32, 16, 12, 15, 0);
            Tiles[e] = Tex(DesignExport.TilesPath(Root, e));
            Sky[e] = Tex(DesignExport.BgPath(Root, e, "ceu"));
            Far[e] = Tex(DesignExport.BgPath(Root, e, "fundo"));
            Mid[e] = Tex(DesignExport.BgPath(Root, e, "meio"));
            Raylib.SetTextureWrap(Far[e], TextureWrap.Repeat);
            Raylib.SetTextureWrap(Mid[e], TextureWrap.Repeat);
        }
        Hostage = Make(DesignExport.PropPath(Root, "refem"), cw, ch, DesignExport.AX, DesignExport.AY, Gfx.Pivot);
        Barrel = Make(DesignExport.PropPath(Root, "barril"), 16, 16, 8, 15, 0);
        Glorb = Make(DesignExport.PropPath(Root, "glorb"), 16, 16, 8, 8, 0);
        Cage = Make(DesignExport.PropPath(Root, "jaula"), 32, 48, 16, 47, 0);
        Proj = Make(DesignExport.PropPath(Root, "projeteis"), 32, 16, 16, 8, 0);
        return generated;
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
    public static (int col, int row) Frame(in Anim a)
    {
        float t = a.T;
        switch (a.S)
        {
            case AState.Run:
                float rate = 14f * Math.Clamp(a.Speed / 88f, 0.55f, 1.4f);
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

    /// <summary>Personagem animado. over != null desenha a silhueta naquela cor (dano, fantasmas, rastros).</summary>
    public static void Human(Sheet s, int fx, int fy, int facing, in Anim a, float alpha = 1, Color? over = null, Color? tint = null)
    {
        var (col, row) = Frame(a);
        int x = fx - (int)a.Recoil * facing;
        var c = over ?? tint ?? Color.White;
        Cell(s, col, row, x, fy, facing, a.Rot, Col.A(c, alpha), over.HasValue);
    }
}
