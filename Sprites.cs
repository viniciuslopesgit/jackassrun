using Raylib_cs;
using static JackassRun.Col;

namespace JackassRun;

/// <summary>Sprites em pixel art no estilo Broforce (sem contorno preto, sombreamento por cor),
/// desenhados a mao como grades de caracteres. Todos olham para a direita; (0,0) = pes.
///
/// Legenda: H/h cabelo/capacete (claro/escuro) · S/s/m/d pele (luz, sombra, pescoco, boca) · E olho
///          R/r cor de destaque (bandana, viseira, chapeu) · V/v roupa · P/p calca · L/l cinto e municao
///          G/g/M arma (corpo, escuro, brilho) · f pelo · W branco · K preto · . vazio</summary>
public static class Sprites
{
    // Cabecas: 10 colunas (x = -4..5), 9 linhas (y = -19..-11). Indice = Look.Hat.
    public static readonly string[][] Heads =
    {
        new[] { // 0 bandana de comando + cabelo comprido
            "..........",
            "..HHHHHHH.",
            ".HHHHHHHHH",
            ".HHHrrHrrH",
            "rrHRRRRRR.",
            ".HHSSSESS.",
            "HHHsSSSSs.",
            "HHHssdms..",
            ".HHmssss..",
        },
        new[] { // 1 capacete militar
            "..........",
            "...hhhhh..",
            "..hHHHHHH.",
            ".hHHHHHHHH",
            "..hSSSSSS.",
            "..sSSSESS.",
            "..sSSSSSs.",
            "..ssSdms..",
            "...mssss..",
        },
        new[] { // 2 cientista: cabelo espetado + oculos
            ".H..H.H...",
            "HHHHHHHH..",
            "HHHHHHHHH.",
            "HHhSSSSSH.",
            "HHsRRWRRW.",
            "HHsSSSSSS.",
            "H.sSSSSSs.",
            "..ssShhs..",
            "...mssss..",
        },
        new[] { // 3 ninja
            "..........",
            "..HHHHHH..",
            ".HHHHHHHH.",
            ".HRRRRRRH.",
            "rHHHHHHHH.",
            ".HHHSSESH.",
            ".HHHHHHHH.",
            "..HHHHHH..",
            "...hhhh...",
        },
        new[] { // 4 capacete espacial
            "...hHHHh..",
            "..hHHHHHH.",
            ".hHHHHHHHH",
            ".hHHRRRRRH",
            ".hHHRWRRRH",
            ".hHHRRRRRH",
            ".hHHHHHHHH",
            "..hHHHHHH.",
            "...hhhhh..",
        },
        new[] { // 5 homem das cavernas: juba de pelo + osso
            ".R.....R..",
            ".RRRRRRR..",
            ".fffffff..",
            "fffffffff.",
            "fffSSSSSf.",
            "ffsSSSESS.",
            "ffsSSSSSs.",
            "f.ssSdms..",
            "...mssss..",
        },
        new[] { // 6 elmo de cavaleiro com pluma
            "..RR......",
            ".RRHHHH...",
            "..HHHHHHH.",
            ".hHHHHHHHH",
            ".hHHKKKKH.",
            ".hHHHHHHH.",
            ".hHHhHhHH.",
            "..hHHHHH..",
            "...hhhh...",
        },
        new[] { // 7 robo
            "....R.....",
            "....h.....",
            "..HHHHHHH.",
            ".hHHHHHHHH",
            ".hHRRRWRH.",
            ".hHHHHHHHH",
            ".hHhHhHhH.",
            ".hHHHHHHH.",
            "...hhhh...",
        },
        new[] { // 8 cowgirl: chapeu + cabelo comprido
            "..RRRRR...",
            "..rrrrr...",
            "RRRRRRRRRR",
            ".HHHHHHH..",
            "HHHSSSSSS.",
            "HHsSSSESS.",
            "HHsSSSSSs.",
            "HH.sSdms..",
            "HH.mssss..",
        },
        new[] { // 9 civil: cabelo curto penteado
            "..........",
            "..........",
            "..HHHHHH..",
            ".HHHHHHHH.",
            ".HHSSSSSS.",
            "..sSSSESS.",
            "..sSSSSSs.",
            "..ssSdms..",
            "...mssss..",
        },
        new[] { // 10 capuz de morcego (orelhas, olhos brancos, queixo a mostra)
            "..H....H..",
            "..HH..HH..",
            "..HHHHHH..",
            ".HHHHHHHH.",
            ".HHHHHWWH.",
            ".HHHHSSSS.",
            "..HHSSSSs.",
            "..HHSdms..",
            "...mssss..",
        },
        new[] { // 11 aventureira: cabelo preso (o rabo de cavalo balanca a parte)
            "..........",
            "..HHHHH...",
            ".HHHHHHH..",
            "HHHHSSSSS.",
            "HHHsSSESS.",
            "HHHsSSSSs.",
            ".H.ssSdms.",
            "...mssss..",
            "..........",
        },
        new[] { // 12 mascara de aranha (vermelha, olhos brancos grandes, linhas de teia)
            "..........",
            "...HHHH...",
            "..HHKHHH..",
            ".HHHKHHHH.",
            ".HKHHWWWH.",
            ".HHKHWWWH.",
            "..HHKHHH..",
            "..HHHKHH..",
            "...HHHH...",
        },
    };
    public const int HeadX = -4, HeadY = -19;

    // Tronco segurando a arma: 15 colunas (x = -7..7), y = -12..-5. Bracos fortes pendurados dos lados.
    public static readonly string[] TorsoHold =
    {
        ".SS........VS..",
        "SSSV.......VSS.",
        "sssVVmmmmmVVss.",
        "sssVVVVVVVVVss.",
        "sssvVVVVVVVvss.",
        "ssSvVVVVVVVvSs.",
        ".ss.vVVVVVv.ss.",
        "...pLLLLlllp...",
    };
    public const int TorsoX = -7, TorsoHoldY = -12;

    // Tronco com os bracos para cima (escalando, comemorando, voando morto). y = -17..-5.
    public static readonly string[] TorsoUp =
    {
        ".ss..........ss",
        ".SS..........SS",
        ".SS..........SS",
        ".SS..........SS",
        ".SS.........SS.",
        ".SSV.......VSS.",
        "..sVVmmmmmVVs..",
        "...VVVVVVVVV...",
        "...VVVVVVVVV...",
        "...vVVVVVVVv...",
        "...vVVVVVVVv...",
        "...vVVVVVVVv...",
        "...pLLLLlllp...",
    };
    public const int TorsoUpY = -17;

    /// <summary>Armas: grade, canto superior esquerdo (x,y) e ponta do cano (para tiros e clarao).</summary>
    public readonly record struct Gun(string[] Rows, int X, int Y, int MuzzleX, int MuzzleY);

    public static readonly Gun[] Guns =
    {
        // 0 metralhadora pesada (Jean Rockfire)
        new(new[] {
            ".........g.......",
            "......gGGGGg.....",
            "gGGGGGGGGGGGGGGMM",
            ".S..gg.gg..S.....",
            "....gg...........",
        }, -6, -9, 11, -7),
        // 1 fuzil (soldados)
        new(new[] {
            ".......g.......",
            "gGGGGGGGGGGGGM.",
            ".S..gg.....S...",
        }, -6, -8, 9, -7),
        // 2 escopeta de cano longo com coronha de madeira (Sheila)
        new(new[] {
            "LLLLgGGGGGGGGGGM",
            ".S..LLLLL..S....",
        }, -6, -7, 10, -7),
        // 3 pistola laser (Doc Chrono)
        new(new[] {
            ".....GGGGG.......",
            "..gGGGGGGGGGRRRW.",
            ".S..gg.....S.....",
        }, -6, -8, 11, -7),
        // 4 bazuca no ombro
        new(new[] {
            "gGGGGGGGGGGGGGGGGg",
            "gMMMMMMMMMMMMMMMMg",
            "gGGGGGGGGGGGGGGGGg",
            ".......gg..S......",
        }, -7, -12, 11, -11),
        // 5 katana erguida
        new(new[] {
            "........M",
            ".......G.",
            "......G..",
            ".....G...",
            "....G....",
            "...M.....",
            "..RR.....",
            ".SS......",
        }, 3, -14, 9, -10),
        // 6 metralhadora giratoria (brutamontes)
        new(new[] {
            "....gGGGGGGGGGGGG.",
            "gGGGGGGMMMMMMMMMMM",
            ".S..gGGGGGGGGGGGG.",
            "....gg............",
        }, -6, -8, 12, -7),
        // 7 nenhuma arma (refens)
        new(Array.Empty<string>(), 0, 0, 0, 0),
        // 8 faca
        new(new[] {
            "...WW",
            ".SSM.",
        }, 3, -8, 8, -8),
        // 9 detonador do homem-bomba
        new(new[] {
            ".R",
            "SG",
        }, 3, -8, 5, -8),
        // 10 pistolas duplas (uma em cada mao)
        new(new[] {
            "...gGGGG",
            "..SSg...",
            ".gGGGG..",
            "SSg.....",
        }, 2, -9, 9, -9),
        // 11 escudo alto com visor (escudeiro): cobre o corpo inteiro pela frente
        new(new[] {
            ".gG.",
            ".gGM",
            ".gGG",
            ".gWG",
            ".gWG",
            ".gGG",
            ".gGG",
            "SgRG",
            ".gRG",
            ".gGG",
            ".gGG",
            ".gGG",
            ".gGg",
            "..g.",
        }, 2, -17, 6, -9),
        // 12 granada na mao (granadeiro)
        new(new[] {
            "..gG",
            ".SGM",
            ".Sg.",
        }, 3, -10, 6, -10),
        // 13 fuzil de precisao com luneta (atirador de elite)
        new(new[] {
            "......gGGGg.......",
            ".......g.g........",
            "gGGGGGGGGGGGGGGGGM",
            ".S..gg.....S......",
        }, -6, -10, 11, -8),
        // 14 lanca-chamas (bico com chama piloto na ponta)
        new(new[] {
            ".....gGGGGGg..",
            "gGGGGGGGGGGGMR",
            ".S..gg.....S..",
        }, -6, -9, 8, -8),
    };

    public static Gun GunOf(in Look l) => Guns[Math.Min(l.Wpn, (byte)(Guns.Length - 1))];
}

public static partial class Gfx
{
    static readonly Color BeltBrown = Hex(0x734122), AmmoGrey = Hex(0x717571), EyeDark = Hex(0x2a1410);

    static bool bodyPass;      // desenhando tronco/maos: 'S' vira a cor do uniforme quando SuitArms

    static Color PixColor(char ch, in Look L, bool blink) => ch switch
    {
        'H' => L.Hair, 'h' => Mul(L.Hair, 0.68f),
        'S' => bodyPass && L.SuitArms ? L.Arms : L.Skin, 's' => Mul(bodyPass && L.SuitArms ? L.Arms : L.Skin, 0.86f), 'm' => Mul(L.Skin, 0.58f), 'd' => Mul(L.Skin, 0.42f),
        'E' => blink ? Mul(L.Skin, 0.86f) : EyeDark,
        'R' => L.Accent, 'r' => Mul(L.Accent, 0.62f),
        'V' => L.Shirt, 'v' => Mul(L.Shirt, 0.72f),
        'P' => L.Pants, 'p' => Mul(L.Pants, 0.72f),
        'B' => L.Boots,
        'L' => L.CustomBelt ? L.Belt : BeltBrown, 'l' => L.CustomBelt ? Mul(L.Belt, 0.8f) : AmmoGrey,
        'G' => L.Gun, 'g' => Mul(L.Gun, 0.65f), 'M' => Col.Lerp(L.Gun, White, 0.35f),
        'f' => Mul(L.Shirt, 0.6f),
        'W' => White, 'K' => Ink,
        _ => Magenta,
    };

    /// <summary>Copia uma grade de pixels para a lista de partes, juntando pixels vizinhos da mesma cor.
    /// bulk alarga a coluna central e estica a linha da barriga (brutamontes).</summary>
    static void Grid(string[] rows, int x0, int y0, in Look L, bool blink, int bulk = 0, int stretchRow = int.MinValue)
    {
        for (int r = 0; r < rows.Length; r++)
        {
            string row = rows[r];
            int y = y0 + r;
            int yy = y < stretchRow ? y - bulk : y;
            int h = y == stretchRow ? 1 + bulk : 1;
            int c = 0;
            while (c < row.Length)
            {
                char ch = row[c];
                if (ch == '.') { c++; continue; }
                int start = c;
                while (c < row.Length && row[c] == ch) c++;
                int xa = x0 + start, xb = x0 + c;              // [xa, xb)
                // alarga: colunas a direita do centro andam "bulk" pixels
                int sxa = xa > 0 ? xa + bulk : xa, sxb = xb > 0 ? xb + bulk : xb;
                P(sxa, yy, sxb - sxa, h, PixColor(ch, L, blink));
            }
        }
    }

    // Corrida em 6 quadros: deslocamento do pe da frente (x, altura), pe de tras (x, altura), balanco
    static readonly sbyte[,] RunTab =
    {
        {  3,  0, -3,  0,  0 },
        {  1,  0, -1, -2, -1 },
        { -1,  0,  2, -2, -1 },
        { -3,  0,  3,  0,  0 },
        { -1, -2,  1,  0, -1 },
        {  2, -2, -1,  0, -1 },
    };

    /// <summary>Perna: coluna de 2px da calca ate a bota (2px em cima, 3px embaixo com a ponta para frente).</summary>
    static void PixLeg(int hipX, int off, int lift, int hipY, Color pants, Color boots)
    {
        int footX = hipX + off;
        int bootTop = -2 + lift;
        for (int y = hipY + 1; y < bootTop; y++)
        {
            float k = (y - hipY) / (float)Math.Max(1, bootTop - hipY);
            int x = (int)MathF.Round(hipX + (footX - hipX) * k);
            P(x, y, 2, 1, pants);
        }
        P(footX, bootTop, 2, 1, boots);
        P(footX, bootTop + 1, 3, 1, boots);
    }

    /// <summary>Boneco no estilo Broforce. (fx,fy) = pes, centro.
    /// Usado para herois, inimigos, prisioneiros, fantasmas e corpos.</summary>
    public static void Humanoid(int fx, int fy, int facing, in Look L, in Anim a, float alpha = 1, Color? over = null)
    {
        np = 0;
        int bob = 0, lean = 0, headDY = 0, headDX = 0;
        int fOff = 1, fLift = 0, bOff = -1, bLift = 0;
        bool up = false, noGun = false, blink = false;
        float flow = 0.3f, t = a.T;

        switch (a.S)
        {
            case AState.Idle:
                headDY = MathF.Sin(t * 3f) > 0.6f ? 1 : 0;
                blink = (t % 3.4f) < 0.12f;
                break;
            case AState.Run:
            {
                float rate = 14f * Math.Clamp(a.Speed / 88f, 0.55f, 1.4f);
                int f = (int)(t * rate) % 6;
                fOff = RunTab[f, 0]; fLift = RunTab[f, 1]; bOff = RunTab[f, 2]; bLift = RunTab[f, 3]; bob = RunTab[f, 4];
                headDY = RunTab[(f + 5) % 6, 4] - bob;      // cabeca chega um quadro atrasada
                lean = a.Speed > 60 ? 1 : 0;
                flow = 1f;
                break;
            }
            case AState.Jump:
                fOff = 2; fLift = -2; bOff = -2; bLift = -1; bob = -1; flow = 0.9f;
                break;
            case AState.Fall:
                fOff = 1; bOff = -2; bLift = -1; flow = 1f;
                headDY = (int)(t * 10) % 2;
                break;
            case AState.Climb:
            {
                int c = (int)(t * 9) & 1;
                fOff = 1; fLift = c == 0 ? -2 : 0; bOff = -1; bLift = c == 0 ? 0 : -2;
                up = true; noGun = true; flow = 0.4f; bob = -c;
                break;
            }
            case AState.Dash:
                lean = 1; fOff = 4; fLift = -1; bOff = -4; bLift = -1; bob = 1; flow = 1.5f;
                break;
            case AState.Cheer:
            {
                int c = (int)(t * 6) & 1;
                up = true; noGun = true; bob = -c;
                break;
            }
            case AState.Tumble:
                fOff = 2; fLift = -2; bOff = -3; bLift = -1; up = true; noGun = true; flow = 1f;
                break;
            case AState.Hurt:
                lean = -1; headDX = -1; fOff = 2; bOff = -2;
                break;
        }
        if (a.Land > 0) { bob += 1; fOff += 1; bOff -= 1; }

        int B = L.Bulk;
        int hipY = -4 + bob;

        // --- capa (atras de tudo), abre com o movimento
        if (L.Cape)
        {
            var cc = Mul(L.Hair, 0.9f); var cl = Mul(L.Hair, 1.35f);
            int topY = -13 + bob;
            for (int y = topY; y <= -4; y++)
            {
                int k = y - topY;
                int w = 1 + k / 3 + (int)(flow * k / 5) + (int)MathF.Round(MathF.Sin(t * 12 + k * 0.6f) * flow);
                P(-3 - w + lean, y, w + 1, 1, k % 4 == 1 ? cl : cc);
            }
        }

        // --- tanque de combustivel nas costas (lanca-chamas)
        if (L.Tank)
        {
            var tk = L.Accent;
            int ty0 = -14 + bob;
            P(-9 - B + lean, ty0, 4, 10, Mul(tk, 0.78f));
            P(-9 - B + lean, ty0 + 1, 1, 8, Mul(tk, 1.15f));
            P(-6 - B + lean, ty0 + 1, 1, 8, Mul(tk, 0.55f));
            P(-8 - B + lean, ty0 - 1, 2, 1, Hex(0x5a5a62));
            P(-9 - B + lean, ty0 + 4, 4, 1, Hex(0x3a3a40));
        }

        // --- pernas
        PixLeg(-3 - B, bOff, bLift, hipY, Mul(L.Pants, 0.72f), Mul(L.Boots, 0.9f));
        PixLeg(1 + B, fOff, fLift, hipY, L.Pants, L.Boots);
        P(-4 - B, hipY, 9 + B * 2, 1, L.Pants);
        P(-4 - B, hipY, 1, 1, Mul(L.Pants, 0.72f));

        // --- tronco (com bracos)
        offX = lean; offY = bob;
        bodyPass = true;
        if (up) Grid(Sprites.TorsoUp, Sprites.TorsoX, Sprites.TorsoUpY, L, blink, B, -8);
        else Grid(Sprites.TorsoHold, Sprites.TorsoX, Sprites.TorsoHoldY, L, blink, B, -8);

        bodyPass = false;
        // --- cabeca
        offX = lean + headDX; offY = bob + headDY - B;
        var head = Sprites.Heads[Math.Min(L.Hat, (byte)(Sprites.Heads.Length - 1))];
        Grid(head, Sprites.HeadX, Sprites.HeadY, L, blink);

        // pontas da bandana / cachecol / cabelo balancando com o movimento
        int w0 = (int)MathF.Round(MathF.Sin(t * 18f) * flow * 1.2f);
        int w1 = (int)MathF.Round(MathF.Sin(t * 18f - 1.4f) * flow * 1.4f);
        int len = 1 + (int)(flow * 2.6f);
        if (L.Hat is 0 or 3)
        {
            var acc = L.Accent; var accD = Mul(L.Accent, 0.62f);
            P(-4 - len, -15 + w0, len, 1, acc);
            P(-4 - len + 1, -14 + w1, Math.Max(1, len - 1), 1, accD);
        }
        else if (L.Hat == 8 && flow > 0.5f)
            P(-5, -15 + (w0 > 0 ? 1 : 0), 1, 4 + w1, L.Hair);
        else if (L.Hat == 11)
        {
            // rabo de cavalo balancando
            P(-5, -17, 2, 2, L.Hair);
            P(-6 - (flow > 0.5f ? 1 : 0), -16 + w0, 2, 5 + w1, Mul(L.Hair, 0.85f));
        }

        // --- arma (inclui as maos)
        if (!noGun)
        {
            var g = Sprites.GunOf(L);
            offX = lean - (int)a.Recoil; offY = bob;
            bodyPass = true;
            Grid(g.Rows, g.X, g.Y, L, blink, B);
            bodyPass = false;
        }
        offX = offY = 0;

        Flush(fx, fy, facing, alpha, over, a.Rot, outline: false);
    }
}
