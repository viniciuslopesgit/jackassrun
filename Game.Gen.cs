namespace JackassRun;

/// <summary>Geracao do mapa, deterministica por chunk (mesma seed = mesmo chunk), o que permite regenerar chunks
/// com seguranca depois de rebobinar o tempo.
///
/// O mapa NAO e sorteado bloco a bloco: cada trecho de 40 m (20 colunas) e montado com MODULOS desenhados a mao
/// (Modulos.cs) — um de 20 colunas ou dois de 10 — e o jogo sorteia as VARIACOES em cima dessa base:
///  - qual modulo entra (por pesos, conforme a parte do level, sem repetir o anterior);
///  - espelhado ou nao, e em que altura (os modulos se encaixam pela altura do chao das pontas);
///  - caixas e tijolos opcionais, grupos de blocos que aparecem ou somem juntos;
///  - quais inimigos aparecem (ORCAMENTO do level) e de que tipo; emboscadas e paraquedistas.
///
/// Estrutura de cada LEVEL (Broforce): chegada calma -> arredores -> subsolo -> posto avancado -> FORTALEZA
/// (o ultimo pedaco, com o prisioneiro). Depois de um modulo intenso vem, de preferencia, um respiro.</summary>
public sealed partial class Game
{
    /// <summary>Rastro opcional da geracao (chunk, ato, modulos) para testes; null = desligado.</summary>
    public List<string>? GenTrace;

    /// <summary>Como terminou cada chunk ja gerado: altura do chao na saida, ultimo modulo e a intensidade dele.
    /// Fica fora do snapshot, mas e deterministico: um chunk regenerado depois de rebobinar sai igual.</summary>
    readonly Dictionary<int, (int exit, int mod, int intensity)> chunkEnd = new();

    void EnsureChunks()
    {
        while (S.NextChunk * K.CHUNK * K.T < S.CamX + K.MaxViewW + 96)
            GenChunk(S.NextChunk++);
    }

    public const int Ground0 = 14;                 // linha tipica da superficie
    const int GroundMin = 11, GroundMax = 17;      // o chao sobe e desce dentro desta faixa

    /// <summary>Ato do chunk dentro do level.</summary>
    enum Act { Arrival, Outskirts, Underground, Outpost, Fortress }

    static Ato AtoOf(Act a) => (Ato)(1 << (int)a);

    /// <summary>Orcamento de inimigos de cada ato (antes de multiplicar pela dificuldade).</summary>
    static float Budget(Act a) => a switch
    {
        Act.Arrival => 3f, Act.Outskirts => 5f, Act.Underground => 5f, Act.Outpost => 7f, _ => 10f,
    };

    static float Cost(EnemyKind k) => k switch
    {
        EnemyKind.Brute => 2f, EnemyKind.Turret => 2f, EnemyKind.Rocketeer => 1.3f, EnemyKind.Bomber => 1.3f,
        EnemyKind.Shield => 1.5f, EnemyKind.Grenadier => 1.4f, EnemyKind.Sniper => 1.5f, EnemyKind.Flamer => 1.5f,
        EnemyKind.Dog => 0.8f, _ => 1f,
    };

    /// <summary>Marca de inimigo, objeto ou evento de um modulo (coluna e linha no mundo).</summary>
    readonly record struct Mark(char Ch, int Tx, int Row);

    void GenChunk(int c)
    {
        if (c == 0) chunkEnd.Clear();
        var r = new Rng(seed ^ ((uint)(c + 1) * 2246822519u));
        r.Next();
        int baseCol = c * K.CHUNK;
        float diff = MathF.Min(1f, c / 26f);   // dificuldade sobe devagar ao longo da corrida
        // ato do pedaco dentro do level: o ultimo e sempre a fortaleza; os outros se espalham pelos 4 primeiros atos
        int n = GameConfig.PedacosPorLevel, pos = c % n;
        var act = pos == n - 1 ? Act.Fortress : (Act)Math.Min(3, pos * 4 / Math.Max(1, n - 1));
        float budget = Budget(act) * (0.8f + diff * 0.8f) * GameConfig.QuantidadeInimigos;
        var style = Eras.ForCol(baseCol).Style;
        var marks = new List<Mark>();

        // ---------------------------------------------------------------- montagem do chunk
        int ground;
        if (c == 0)
        {
            ground = PlaceModule(Modulos.Inicio, false, baseCol, Ground0, ref r, marks);
            chunkEnd[0] = (ground, -1, 0);
        }
        else
        {
            var (h, prevMod, prevInt) = chunkEnd.TryGetValue(c - 1, out var pe) ? pe : (Ground0, -1, 0);
            int col = 0;
            while (col < K.CHUNK)
            {
                var (mi, mirror) = PickModule(act, K.CHUNK - col, h, prevMod, prevInt, style, ref r);
                if (mi < 0)
                {
                    // nenhum modulo cabe (nao deve acontecer): chao plano ate o fim
                    for (int x = col; x < K.CHUNK; x++) FlatColumn(baseCol + x, h);
                    break;
                }
                var m = Modulos.Lista[mi];
                GenTrace?.Add($"{c}|{act}|{m.Nome}{(mirror ? " (espelhado)" : "")}|{col}|{h}");
                h = PlaceModule(m, mirror, baseCol + col, h, ref r, marks);
                col += m.Largura; prevMod = mi; prevInt = m.Intensidade;
            }
            chunkEnd[c] = (h, prevMod, prevInt);
            ground = h;
        }

        // ---------------------------------------------------------------- objetos, inimigos e eventos
        int k = 0;
        int Id() => c * 1000 + (k++);
        float X(int tx) => tx * K.T + K.T / 2f;

        // inimigo so entra se couber no orcamento (os obrigatorios entram sempre, mas gastam o orcamento)
        bool Enemy(EnemyKind kind, int tx, float y, bool must, ref Rng rr)
        {
            float cost = Cost(kind);
            if (!must && cost > budget + 0.01f) return false;
            budget -= cost;
            S.Enemies.Add(new Enemy
            {
                Id = Id(), Kind = kind, X = X(tx), Y = y, BaseY = y, Hp = EnemyHp(kind), Facing = -1,
                StateT = rr.Range(0, 2), FireT = rr.Range(0.5f, 1.5f), Phase = rr.Range(0, 6),
            });
            return true;
        }
        // EMBOSCADA (Metal Slug): ao passar da coluna, entram tropas correndo pela direita (type 0)
        // ou caindo de paraquedas (type 1). Paga do mesmo orcamento de inimigos.
        void Ambush(int tx, int count, int type, EnemyKind kind)
        {
            if (c < 2) return;
            int m = 0;
            while (m < count && Cost(kind) <= budget + 0.01f) { budget -= Cost(kind); m++; }
            if (m > 0) S.Ambushes.Add(new Ambush { Col = tx, Count = m, Type = type, Enemy = kind });
        }
        // tipo do inimigo comum: os novos entram aos poucos ao longo da corrida
        EnemyKind Grunt(ref Rng rr)
        {
            float v = rr.F(), acc = 0;
            if (c >= 5 && v < (acc += 0.07f + diff * 0.05f)) return EnemyKind.Brute;
            if (c >= 2 && v < (acc += 0.07f + diff * 0.03f)) return EnemyKind.Knife;
            if (c >= 2 && v < (acc += 0.07f + diff * 0.03f)) return EnemyKind.Dog;
            if (c >= 4 && v < (acc += 0.06f + diff * 0.03f)) return EnemyKind.Grenadier;
            if (c >= 6 && v < (acc += 0.06f + diff * 0.03f)) return EnemyKind.Shield;
            if (c >= 8 && v < (acc += 0.05f + diff * 0.03f)) return EnemyKind.Flamer;
            if (c >= 4 && v < (acc += 0.04f + diff * 0.04f)) return EnemyKind.Bomber;
            if (c >= 2 && v < (acc += 0.10f)) return EnemyKind.Rocketeer;
            return EnemyKind.Soldier;
        }
        // atirador de posto (torres, telhados): nunca faca, cao nem homem-bomba
        EnemyKind Shooter(ref Rng rr)
        {
            float v = rr.F();
            if (c >= 7 && v < 0.3f) return EnemyKind.Sniper;
            if (c >= 3 && v < 0.45f) return EnemyKind.Grenadier;
            if (c >= 2 && v < 0.65f) return EnemyKind.Rocketeer;
            return EnemyKind.Soldier;
        }
        void Prop(PropKind kind, float x, float y, ref Rng rr) =>
            S.Props.Add(new Prop { Id = Id(), Kind = kind, X = x, Y = y, Char = rr.Int(0, Chars.All.Length), T = rr.Range(0, 5) });

        // objetos entram sempre; inimigos: primeiro os obrigatorios (maiusculos), depois os eventos, depois os
        // opcionais numa ordem sorteada, cada um com uma chance, enquanto houver orcamento
        var optional = new List<Mark>();
        var events = new List<Mark>();
        foreach (var mk in marks)
        {
            float feet = (mk.Row + 1) * K.T;
            switch (mk.Ch)
            {
                case 'x': Prop(PropKind.Barrel, X(mk.Tx), feet, ref r); break;
                case '$': Prop(PropKind.Cage, X(mk.Tx), feet, ref r); break;
                case 'r': Prop(PropKind.Hostage, X(mk.Tx), feet, ref r); break;
                case 'o': Prop(PropKind.Glorb, X(mk.Tx), feet - 8, ref r); break;
                case 'v':
                    if (style == BgStyle.City) S.Props.Add(new Prop { Id = Id(), Kind = PropKind.Car, X = X(mk.Tx), Y = feet, Hp = 5, Char = r.Int(0, 3) });
                    else if (r.Chance(0.5f)) Prop(PropKind.Barrel, X(mk.Tx), feet, ref r);
                    break;
                case 'E': Enemy(Grunt(ref r), mk.Tx, feet, true, ref r); break;
                case 'A': Enemy(Shooter(ref r), mk.Tx, feet, true, ref r); break;
                case 'G': Enemy(EnemyKind.Brute, mk.Tx, feet, true, ref r); break;
                case 'M': Enemy(c >= 2 ? EnemyKind.Turret : Shooter(ref r), mk.Tx, feet, true, ref r); break;
                case '!': case '^': events.Add(mk); break;
                default: optional.Add(mk); break;
            }
        }
        foreach (var mk in events)
            if (r.Chance(0.8f))
            {
                if (mk.Ch == '!') Ambush(mk.Tx, 1 + (r.Chance(0.4f + diff * 0.4f) ? 1 : 0), 0, Grunt(ref r));
                else Ambush(mk.Tx, 2 + (r.Chance(diff) ? 1 : 0), 1, EnemyKind.Soldier);
            }
        for (int i = optional.Count - 1; i > 0; i--) { int j = r.Int(0, i + 1); (optional[i], optional[j]) = (optional[j], optional[i]); }
        float chance = 0.75f + diff * 0.2f;
        foreach (var mk in optional)
        {
            if (!r.Chance(chance)) continue;
            float feet = (mk.Row + 1) * K.T;
            switch (mk.Ch)
            {
                case 'e': Enemy(Grunt(ref r), mk.Tx, feet, false, ref r); break;
                case 'a': Enemy(Shooter(ref r), mk.Tx, feet, false, ref r); break;
                case 'g': if (c >= 3) Enemy(EnemyKind.Brute, mk.Tx, feet, false, ref r); break;
                case 'm': if (c >= 2) Enemy(EnemyKind.Turret, mk.Tx, feet, false, ref r); break;
                case 'w': if (c >= 2) Enemy(EnemyKind.Flyer, mk.Tx, feet - 8, false, ref r); break;
            }
        }

        // nenhuma escada inutil: precisa de chao e de alcancar alguma coisa
        for (int col = -1; col <= K.CHUNK; col++) ValidateLadderCol(baseCol + col);

        // o que sobrou do orcamento vira patrulha aerea (a partir do 3o chunk, fora da chegada)
        if (c >= 2 && act != Act.Arrival)
        {
            int flyers = budget >= 1 && r.Chance(0.15f + diff * 0.35f) ? 1 + (budget >= 3 && r.Chance(diff * 0.3f) ? 1 : 0) : 0;
            for (int i = 0; i < flyers; i++)
                Enemy(EnemyKind.Flyer, baseCol + r.Int(4, K.CHUNK - 2), ground * K.T - r.Range(56, 100), false, ref r);
        }
    }

    /// <summary>Coluna de chao plano (so usada se nenhum modulo couber).</summary>
    void FlatColumn(int tx, int height)
    {
        for (int y = 0; y < K.ROWS; y++)
        {
            ushort v = y < height ? (ushort)0
                : y == K.ROWS - 1 ? Terrain.Make(Terrain.BEDROCK, 0, (int)Hash.H(tx, y), 2)
                : Terrain.Make(Terrain.DIRT, 0, (int)Hash.H(tx, y), Math.Min(2, y - height));
            Ter.SetRaw(tx, y, v);
        }
        Ter.SetSurface(tx, height);
    }

    /// <summary>Sorteia o proximo modulo: tem de ser da parte certa do level, do cenario certo, caber nas colunas
    /// que faltam e manter o chao dentro da faixa. Evita repetir o anterior, prefere um respiro depois de um trecho
    /// intenso e puxa a altura do chao de volta para o meio.</summary>
    static (int index, bool mirror) PickModule(Act act, int room, int h, int prevMod, int prevInt, BgStyle style, ref Rng r)
    {
        var flag = AtoOf(act);
        var list = Modulos.Lista;
        var weight = new float[list.Length * 2];
        for (int pass = 0; pass < 2; pass++)
        {
            float total = 0;
            Array.Clear(weight);
            for (int i = 0; i < list.Length; i++)
            {
                var m = list[i];
                if ((m.Atos & flag) == 0) continue;
                if (m.Cenarios != null && Array.IndexOf(m.Cenarios, style) < 0) continue;
                int w = m.Largura;
                if (w > room || room - w != 0 && room - w < 10) continue;
                if (pass == 0 && i == prevMod) continue;
                float wt = m.Peso;
                if (room == K.CHUNK && w < K.CHUNK) wt *= 0.2f;                          // a base e o modulo inteiro; meios sao a excecao
                if (prevInt >= 3 && act != Act.Fortress)
                    wt *= m.Intensidade <= 1 ? 2f : m.Intensidade >= 3 ? 0.5f : 1f;       // respiro depois de trecho intenso
                if (m.Recompensa) wt *= prevInt >= 3 ? 3f : 0.1f;                         // premio logo depois do desafio
                for (int mi = 0; mi < 2; mi++)
                {
                    bool mirror = mi == 1;
                    if (mirror && !m.Espelhar) continue;
                    int entry = mirror ? m.Saida : m.Entrada, exit = mirror ? m.Entrada : m.Saida;
                    int off = h - entry, nh = exit + off;
                    if (nh < GroundMin || nh > GroundMax) continue;
                    if (off + m.Topo < 1 || off + m.Altura - 1 > K.ROWS - 2) continue;
                    // puxa o chao de volta para a altura normal
                    int dNew = Math.Abs(nh - Ground0), dOld = Math.Abs(h - Ground0);
                    float steer = dNew < dOld ? 2f : dNew > dOld ? 0.5f : 1f;
                    float v = wt * steer * (m.Espelhar ? 0.5f : 1f);
                    weight[i * 2 + mi] = v;
                    total += v;
                }
            }
            if (total <= 0) continue;
            float pick = r.F() * total;
            for (int j = 0; j < weight.Length; j++)
            {
                if (weight[j] <= 0) continue;
                if (pick < weight[j]) return (j / 2, j % 2 == 1);
                pick -= weight[j];
            }
            for (int j = weight.Length - 1; j >= 0; j--) if (weight[j] > 0) return (j / 2, j % 2 == 1);
        }
        return (-1, false);
    }

    static bool IsEntity(char ch) => "eEaAgGmMwx$rov!^".IndexOf(ch) >= 0;
    static bool IsBack(char ch) => ch is '.' or ',' or ';' or ':' or '_';

    /// <summary>Copia um modulo para o mapa a partir da coluna tx0, com o chao da entrada na linha h.
    /// Sorteia as variacoes, cria salas e paredes de fundo e anota inimigos/objetos/eventos em marks.
    /// Retorna a linha do chao na saida.</summary>
    int PlaceModule(Modulo m, bool mirror, int tx0, int h, ref Rng r, List<Mark> marks)
    {
        int W = m.Largura, Hh = m.Altura;
        int entry = mirror ? m.Saida : m.Entrada, exit = mirror ? m.Entrada : m.Saida;
        int off = h - entry;
        char Src(int x, int y) => m.Mapa[y][mirror ? W - 1 - x : x];

        // fundo de uma celula de objeto/variacao vazia, olhando os fundos vizinhos na mesma linha (sem atravessar
        // paredes). Objetos ficam no fundo mais perto (ex.: dentro da sala); um bloco opcional que sumiu numa
        // parede vira abertura para fora (janela), a nao ser que tenha sala dos dois lados.
        char BackAt(int x, int y, bool opening)
        {
            (char ch, int d) Scan(int dir)
            {
                for (int xx = x + dir; xx >= 0 && xx < W; xx += dir)
                {
                    char ch = Src(xx, y);
                    if (IsBack(ch)) return (ch, Math.Abs(xx - x));
                    if (!IsEntity(ch) && ch is not ('c' or 'b' or (>= '1' and <= '6'))) break;     // parede: para
                }
                return ('\0', 0);
            }
            var (l, dl) = Scan(-1);
            var (rr, dr) = Scan(1);
            if (l == '\0') return rr == '\0' ? '.' : rr;
            if (rr == '\0' || l == rr) return l;
            if (opening) return l is ',' or ';' ? rr : l;
            return dl <= dr ? l : rr;
        }

        // 1) resolve as variacoes: cada celula vira terreno ou fundo
        bool[] grp = new bool[7];
        for (int i = 1; i <= 6; i++) grp[i] = r.Chance(0.5f);
        var g = new char[W, Hh];
        for (int y = 0; y < Hh; y++)
            for (int x = 0; x < W; x++)
            {
                char ch = Src(x, y);
                if (IsEntity(ch))
                {
                    marks.Add(new Mark(ch, tx0 + x, y + off));
                    ch = BackAt(x, y, false);
                }
                else if (ch == 'c') ch = r.Chance(0.5f) ? 'C' : BackAt(x, y, true);
                else if (ch == 'b') ch = r.Chance(0.5f) ? 'B' : BackAt(x, y, true);
                else if (ch is >= '1' and <= '3') ch = grp[ch - '0'] ? 'B' : BackAt(x, y, true);
                else if (ch is >= '4' and <= '6') ch = grp[ch - '0'] ? '#' : BackAt(x, y, true);
                g[x, y] = ch;
            }
        char G(int x, int y) => x < 0 || x >= W || y < 0 || y >= Hh ? '.' : g[x, y];
        static bool Inside(char ch) => ch is ',' or ';';
        bool HouseLadder(int x, int y) => G(x, y) == 'H' && (Inside(G(x - 1, y)) || Inside(G(x + 1, y)));

        // 2) colunas: terreno, superficie e fundos
        for (int x = 0; x < W; x++)
        {
            int tx = tx0 + x;
            // superficie = primeiro bloco de terra (uma escada dentro da terra conta como terra)
            int surf = -1;
            for (int y = 0; y < Hh && surf < 0; y++)
            {
                char ch = g[x, y];
                if (ch is '#' or '_' || ch == 'H' && (G(x - 1, y) == '#' || G(x + 1, y) == '#')) surf = y;
            }
            char last = g[x, Hh - 1];
            Ter.SetSurface(tx, surf < 0 ? K.ROWS : surf + off);
            for (int wy = 0; wy < K.ROWS; wy++)
            {
                int my = wy - off;
                char ch = my < 0 ? '.' : my < Hh ? g[x, my] : last;     // a ultima linha se repete ate o fundo
                if (my >= Hh && ch is 'H' or ',' or ';' or ':' or 'D' or 'T') ch = '.';
                bool bottom = wy == K.ROWS - 1;
                ushort Tile(int type) => Terrain.Make(type, 0, (int)Hash.H(tx, wy));
                ushort v = 0;
                int back = Terrain.BACK_NONE;
                switch (ch)
                {
                    case '#':
                    {
                        // aparencia: grama na superficie (menos dentro de casa), terra logo abaixo, terra funda
                        int look = surf < 0 ? 2 : Math.Clamp(my - surf, 0, 2);
                        if (look == 0 && G(x, my - 1) is ',' or ';' or ':') look = 1;
                        v = bottom ? Terrain.Make(Terrain.BEDROCK, 0, (int)Hash.H(tx, wy), 2)
                                   : Terrain.Make(Terrain.DIRT, 0, (int)Hash.H(tx, wy), look);
                        break;
                    }
                    case 'B': v = bottom ? Tile(Terrain.BEDROCK) : Tile(Terrain.BRICK); break;
                    case 'S': v = Tile(Terrain.STEEL); break;
                    case 'C': v = bottom ? Tile(Terrain.BEDROCK) : Tile(Terrain.CRATE); break;
                    case 'T': v = Tile(Terrain.ROOF); break;
                    case 'D': v = Terrain.Make(Terrain.DOOR, 0, 0, G(x, my - 1) == 'D' ? 1 : 0); break;
                    case 'H': v = Tile(Terrain.LADDER); break;
                    case '=': v = Tile(Terrain.BRIDGE); break;
                    case '-': case '|': v = Tile(Terrain.CONCRETE); break;
                    case ',': case ';': back = Terrain.BACK_HOUSE; break;
                    case ':': back = Terrain.BACK_WALL; break;
                    case '_': back = Terrain.BACK_EARTH; break;
                }
                Ter.SetRaw(tx, wy, v);
                if (back != Terrain.BACK_NONE) Ter.SetBack(tx, wy, back);
            }
        }

        // 3) escadas sempre com parede atras; parede de casa atras das paredes e do teto das casas
        //    (aparece quando o bloco da frente e destruido)
        for (int x = 0; x < W; x++)
            for (int y = 0; y < Hh; y++)
            {
                int tx = tx0 + x, wy = y + off;
                char ch = g[x, y];
                if (ch == 'H')
                    Ter.SetBack(tx, wy, HouseLadder(x, y) ? Terrain.BACK_HOUSE : Ter.InGround(tx, wy) ? Terrain.BACK_EARTH : Terrain.BACK_WALL);
                else if (ch is 'B' or 'S' or 'D' or 'C')
                {
                    bool near = false;
                    for (int dy = -1; dy <= 1 && !near; dy++)
                        for (int dx = -1; dx <= 1 && !near; dx++)
                            near = Inside(G(x + dx, y + dy)) || HouseLadder(x + dx, y + dy);
                    if (near) Ter.SetBack(tx, wy, Terrain.BACK_HOUSE);
                }
            }

        // 4) salas escuras: retangulos de ',' (e escadas dentro delas), linha por linha, juntando linhas iguais
        bool Dark(int x, int y) => g[x, y] == ',' || g[x, y] == 'H' && (G(x - 1, y) == ',' || G(x + 1, y) == ',');
        var open = new List<(int x0, int x1, int y0, int y1)>();
        for (int y = 0; y <= Hh; y++)
        {
            var runs = new List<(int, int)>();
            if (y < Hh)
                for (int x = 0; x < W; x++)
                {
                    if (!Dark(x, y)) continue;
                    int x0 = x;
                    while (x + 1 < W && Dark(x + 1, y)) x++;
                    runs.Add((x0, x));
                }
            for (int i = open.Count - 1; i >= 0; i--)
            {
                var o = open[i];
                int match = runs.IndexOf((o.x0, o.x1));
                if (match >= 0) { open[i] = (o.x0, o.x1, o.y0, y); runs.RemoveAt(match); continue; }
                S.Rooms.Add(new Room { X0 = tx0 + o.x0, X1 = tx0 + o.x1, Y0 = o.y0 + off, Y1 = o.y1 + off });
                open.RemoveAt(i);
            }
            foreach (var (a, b) in runs) open.Add((a, b, y, y));
        }

        return exit + off;
    }
}
