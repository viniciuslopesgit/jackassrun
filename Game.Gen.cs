namespace JackassRun;

/// <summary>Geracao procedural deterministica por chunk (mesma seed = mesmo chunk),
/// o que permite regenerar chunks com seguranca depois de rebobinar o tempo.
///
/// Estrutura inspirada no level design do Broforce: cada LEVEL tem 5 chunks com
/// um arco de tensao (chegada calma -> arredores -> subsolo -> posto avancado -> fortaleza). Cada chunk tem
/// uma PECA CENTRAL obrigatoria do seu ato, recheio sorteado por pesos (sem repetir), um ORCAMENTO de
/// inimigos (nada de inimigos espalhados a toa), RESPIROS depois de trechos intensos, armadilhas de barris
/// sempre visiveis ANTES dos inimigos, caminhos alternativos (por cima / por baixo) e RECOMPENSAS
/// (prisioneiros) depois de desafios.
/// Do Metal Slug: EMBOSCADAS (tropas correndo pela direita, paraquedistas) disparadas quando o heroi passa
/// por um ponto (ver Game.Slug.cs).</summary>
public sealed partial class Game
{
    /// <summary>Rastro opcional da geracao (chunk, ato, trechos) para testes; null = desligado.</summary>
    public List<string>? GenTrace;

    void EnsureChunks()
    {
        while (S.NextChunk * K.CHUNK * K.T < S.CamX + K.MaxViewW + 96)
            GenChunk(S.NextChunk++);
    }

    public const int Ground0 = 14;     // linha tipica da superficie
    int HeightFor(int c) => c <= 0 ? Ground0 : Ground0 - 1 + (int)(Hash.H(c, (int)seed) % 3);

    /// <summary>Ato do chunk dentro do level.</summary>
    enum Act { Arrival, Outskirts, Underground, Outpost, Fortress }

    enum Seg { Flat, Breather, Step, Pit, Cave, Tunnel, House, Platforms, Bunker, Camp, Crates, Cage, Tower, Bridge, Fortress }

    /// <summary>Largura maxima (em colunas) de cada trecho, para saber se cabe no chunk.</summary>
    static int MaxWidth(Seg s) => s switch
    {
        Seg.Flat => 4, Seg.Breather => 3, Seg.Step => 3, Seg.Pit => 7, Seg.Cave => 10, Seg.Tunnel => 12,
        Seg.House => 9, Seg.Platforms => 5, Seg.Bunker => 5, Seg.Camp => 7, Seg.Crates => 3, Seg.Cage => 3,
        Seg.Tower => 3, Seg.Bridge => 11, Seg.Fortress => 13, _ => 4,
    };

    /// <summary>Recheio de cada ato: (trecho, peso). A peca central e escolhida a parte.</summary>
    static (Seg s, int w)[] Fillers(Act a) => a switch
    {
        Act.Arrival => new[] { (Seg.Flat, 4), (Seg.Step, 3), (Seg.Pit, 2), (Seg.Crates, 2), (Seg.Platforms, 2), (Seg.Tower, 1) },
        Act.Outskirts => new[] { (Seg.Flat, 2), (Seg.Step, 2), (Seg.Pit, 2), (Seg.Crates, 1), (Seg.Tower, 2), (Seg.Platforms, 1), (Seg.Camp, 1) },
        Act.Underground => new[] { (Seg.Step, 2), (Seg.Flat, 1), (Seg.Pit, 1), (Seg.Crates, 1), (Seg.Tower, 1) },
        Act.Outpost => new[] { (Seg.Tower, 2), (Seg.Step, 2), (Seg.Flat, 1), (Seg.Crates, 1), (Seg.Platforms, 1), (Seg.Pit, 1) },
        _ => new[] { (Seg.Step, 2), (Seg.Flat, 2), (Seg.Crates, 1) },
    };

    /// <summary>Peca central de cada ato (uma por chunk).</summary>
    static Seg[] Anchors(Act a) => a switch
    {
        Act.Arrival => new[] { Seg.Platforms, Seg.Tower, Seg.House },
        Act.Outskirts => new[] { Seg.Bridge, Seg.House },
        Act.Underground => new[] { Seg.Cave, Seg.Tunnel },
        Act.Outpost => new[] { Seg.Bunker, Seg.Camp, Seg.Bridge },
        _ => new[] { Seg.Fortress },
    };

    /// <summary>Orcamento de inimigos de cada ato (antes de multiplicar pela dificuldade).</summary>
    static float Budget(Act a) => a switch
    {
        Act.Arrival => 2.3f, Act.Outskirts => 4f, Act.Underground => 4f, Act.Outpost => 6f, _ => 8f,
    };

    static float Cost(EnemyKind k) => k switch
    {
        EnemyKind.Brute => 2f, EnemyKind.Turret => 2f, EnemyKind.Rocketeer => 1.3f, EnemyKind.Bomber => 1.3f, _ => 1f,
    };

    void GenChunk(int c)
    {
        var r = new Rng(seed ^ ((uint)(c + 1) * 2246822519u));
        r.Next();
        int baseCol = c * K.CHUNK;
        int h = HeightFor(c), endH = HeightFor(c + 1);
        float diff = MathF.Min(1f, c / 26f);   // dificuldade sobe devagar ao longo da corrida
        // ato do pedaco dentro do level: o ultimo e sempre a fortaleza; os outros se espalham pelos 4 primeiros atos
        int n = GameConfig.PedacosPorLevel, pos = c % n;
        var act = pos == n - 1 ? Act.Fortress : (Act)Math.Min(3, pos * 4 / Math.Max(1, n - 1));
        float budget = Budget(act) * (0.7f + diff * 0.9f);
        bool city = Eras.ForCol(baseCol).Style == BgStyle.City;   // Sao Paulo: ruas com carros destruidos, viadutos, predios
        int safeUntil = int.MinValue;          // colunas logo depois de um pulo: sem inimigos (aterrissagem justa)
        int k = 0;
        int Id() => c * 1000 + (k++);

        // ---------------------------------------------------------------- ferramentas basicas

        // coluna de terra a partir de "height", com rocha-mae indestrutivel no fundo
        void Fill(int col, int height, int type = Terrain.DIRT)
        {
            int tx = baseCol + col;
            for (int y = 0; y < K.ROWS; y++)
            {
                ushort v = y < height ? (ushort)0
                    : y == K.ROWS - 1 ? Terrain.Make(Terrain.BEDROCK, 0, (int)Hash.H(tx, y), 2)
                    : Terrain.Make(type, 0, (int)Hash.H(tx, y), Math.Min(2, y - height));
                Ter.SetRaw(tx, y, v);
            }
            Ter.SetSurface(tx, height);
        }
        // buraco sem fundo (queda fatal)
        void Void(int col)
        {
            int tx = baseCol + col;
            for (int y = 0; y < K.ROWS; y++) Ter.SetRaw(tx, y, 0);
            Ter.SetSurface(tx, K.ROWS);
        }
        // escava um espaco no subsolo (tunel/caverna) sem mudar a superficie original da coluna
        void Carve(int col, int y0, int y1)
        {
            for (int y = Math.Max(0, y0); y <= Math.Min(K.ROWS - 2, y1); y++) Ter.SetRaw(baseCol + col, y, 0);
        }
        void Tile(int col, int row, int type) => Ter.SetRaw(baseCol + col, row, Terrain.Make(type, 0, (int)Hash.H(baseCol + col, row)));
        // escada de madeira da linha y0 (topo, serve de piso) ate y1 — sempre com parede de fundo atras
        void Ladder(int col, int y0, int y1)
        {
            for (int y = y0; y <= y1; y++)
            {
                Tile(col, y, Terrain.LADDER);
                Ter.SetBack(baseCol + col, y, Ter.InGround(baseCol + col, y) ? Terrain.BACK_EARTH : Terrain.BACK_WALL);
            }
        }
        // parede de fundo de construcao (fica quando os blocos da frente sao destruidos)
        void Back(int col, int y0, int y1) { for (int y = y0; y <= y1; y++) Ter.SetBack(baseCol + col, y, Terrain.BACK_WALL); }
        float X(int col) => (baseCol + col) * K.T + K.T / 2f;

        // inimigo so entra se couber no orcamento do ato e nao estiver na zona de aterrissagem
        bool Enemy(EnemyKind kind, int col, float y)
        {
            if (baseCol + col < safeUntil) return false;
            float cost = Cost(kind);
            if (cost > budget + 0.01f) return false;
            budget -= cost;
            int hp = kind switch { EnemyKind.Brute => 8, EnemyKind.Turret => 6, EnemyKind.Flyer => 2, EnemyKind.Rocketeer => 3,
                EnemyKind.Knife => 1, EnemyKind.Bomber => 1, _ => 2 };
            S.Enemies.Add(new Enemy
            {
                Id = Id(), Kind = kind, X = X(col), Y = y, BaseY = y, Hp = hp, Facing = -1,
                StateT = r.Range(0, 2), FireT = r.Range(0.5f, 1.5f), Phase = r.Range(0, 6),
            });
            return true;
        }
        // EMBOSCADA (Metal Slug): ao passar da coluna, entram tropas correndo pela direita (type 0)
        // ou caindo de paraquedas (type 1). Paga do mesmo orcamento de inimigos.
        void Ambush(int col, int count, int type, EnemyKind kind)
        {
            if (c < 2) return;
            int n = 0;
            while (n < count && Cost(kind) <= budget + 0.01f) { budget -= Cost(kind); n++; }
            if (n > 0) S.Ambushes.Add(new Ambush { Col = baseCol + col, Count = n, Type = type, Enemy = kind });
        }
        EnemyKind Grunt()
        {
            float v = r.F(), acc = 0;
            if (c >= 5 && v < (acc += 0.1f + diff * 0.1f)) return EnemyKind.Brute;
            if (c >= 2 && v < (acc += 0.12f + diff * 0.06f)) return EnemyKind.Knife;
            if (c >= 4 && v < (acc += 0.06f + diff * 0.06f)) return EnemyKind.Bomber;
            if (c >= 2 && v < (acc += 0.18f)) return EnemyKind.Rocketeer;
            return EnemyKind.Soldier;
        }
        // atirador de posto (sentinela, torre): nunca faca nem homem-bomba
        EnemyKind Shooter() => c >= 2 && r.Chance(0.3f) ? EnemyKind.Rocketeer : EnemyKind.Soldier;
        void Prop(PropKind kind, int col, float y) =>
            S.Props.Add(new Prop { Id = Id(), Kind = kind, X = X(col), Y = y, Char = r.Int(0, Chars.All.Length), T = r.Range(0, 5) });
        // carro destruido (2 blocos de largura, centrado na divisa entre col e col+1); explode se levar muitos tiros
        void Car(int col, int height, int variant = -1) =>
            S.Props.Add(new Prop { Id = Id(), Kind = PropKind.Car, X = X(col) + K.HT, Y = height * K.T, Hp = 5,
                Char = variant >= 0 ? variant : r.Int(0, 3) });
        void GlorbArc(int col, int len, int height)
        {
            for (int i = 0; i < len; i++)
            {
                float t = len == 1 ? 0.5f : i / (float)(len - 1);
                Prop(PropKind.Glorb, col + i, height * K.T - 14 - MathF.Sin(t * MathF.PI) * 20);
            }
        }

        // primeiro chunk: area segura de aquecimento
        if (c == 0)
        {
            for (int col = 0; col < K.CHUNK; col++) Fill(col, Ground0);
            GlorbArc(6, 3, Ground0);
            GlorbArc(12, 3, Ground0);
            for (int i = 0; i < 2; i++) Tile(15 + i, Ground0 - 3, Terrain.BRICK);
            Prop(PropKind.Barrel, 18, Ground0 * K.T);
            return;
        }

        // ---------------------------------------------------------------- trechos
        // Medidas em blocos de 16px: o heroi tem ~1 bloco de altura, pula ~2,8 blocos e escala paredes.
        // Cada trecho comeca em colI, avanca colI e devolve a sua intensidade (0 = calmo ... 5 = climax).
        int colI = 0;

        int Flat()
        {
            // terreno plano com patrulha
            int len = r.Int(2, 5);
            for (int i = 0; i < len; i++) Fill(colI + i, h);
            bool foe = r.Chance(0.35f + diff * 0.25f) && Enemy(Grunt(), colI + len / 2, h * K.T);
            if (city && len >= 3 && r.Chance(0.6f)) Car(colI, h);                       // carro abandonado na rua
            else if (!foe && r.Chance(0.5f)) GlorbArc(colI, Math.Min(3, len), h);
            if (act >= Act.Outpost && r.Chance(0.3f)) { Ambush(colI, 2, 0, Grunt()); foe = true; }
            colI += len;
            return foe ? 1 : 0;
        }

        int Breather()
        {
            // respiro depois de um trecho intenso: chao plano e premios, sem inimigos
            int len = r.Int(2, 4);
            for (int i = 0; i < len; i++) Fill(colI + i, h);
            GlorbArc(colI, len, h);
            if (city && len >= 3 && r.Chance(0.35f)) Car(colI, h, 0);                    // carcaca queimada soltando fumaca
            colI += len;
            return 0;
        }

        int Step()
        {
            // degrau de 1 ou 2 blocos; o inimigo fica no alto (vantagem de altura, como no Broforce)
            int dh = r.Int(1, 3) * (r.Chance(0.5f) ? 1 : -1);
            h = Math.Clamp(h + dh, Ground0 - 3, Ground0 + 2);
            int len = r.Int(2, 4);
            for (int i = 0; i < len; i++) Fill(colI + i, h);
            bool foe = dh < 0 && r.Chance(0.3f + diff * 0.3f) && Enemy(Shooter(), colI + len - 1, h * K.T);
            colI += len;
            return foe ? 1 : 0;
        }

        int Pit()
        {
            // buraco sem fundo: sempre com corrida antes (2 blocos) e aterrissagem livre depois
            int len = r.Int(1, 3 + (diff > 0.5f ? 1 : 0));
            Fill(colI, h); Fill(colI + 1, h);
            for (int i = 2; i < 2 + len; i++) Void(colI + i);
            if (len >= 2 && r.Chance(0.5f))
                for (int i = 2; i < 2 + len; i++) Tile(colI + i, h - 2, Terrain.BRICK);   // ponte de tijolos por cima
            else GlorbArc(colI + 2, len, h);
            Fill(colI + 2 + len, h);
            safeUntil = baseCol + colI + 2 + len + 3;
            colI += len + 3;
            return 1;
        }

        int Cave()
        {
            // CAVERNA: buraco que NAO mata. Cai-se num salao com premios e sai-se por um poco.
            int len = r.Int(7, 10), floor = Math.Min(h + r.Int(5, 7), K.ROWS - 3), roof = h + 2;
            for (int i = 0; i < len; i++) Fill(colI + i, h);
            Carve(colI + 1, h, floor - 1); Carve(colI + 2, h, floor - 1);             // entrada (2 de largura)
            for (int i = 3; i < len - 2; i++) Carve(colI + i, roof, floor - 1);        // salao
            Carve(colI + len - 2, h, floor - 1);                                       // poco de saida
            if (r.Chance(0.6f)) Ladder(colI + len - 2, h, floor - 1);
            else if (floor - h >= 5) Tile(colI + len - 2, floor - 3, Terrain.BRICK);
            GlorbArc(colI + 3, Math.Min(4, len - 5), floor);
            // recompensa escondida no fundo, guardada
            if (r.Chance(0.5f)) Prop(PropKind.Cage, colI + len - 4, floor * K.T);
            else Prop(PropKind.Barrel, colI + len - 4, floor * K.T);
            Enemy(EnemyKind.Soldier, colI + 4, floor * K.T);
            if (r.Chance(0.5f)) Enemy(Shooter(), colI + len - 1, h * K.T);              // vigia em cima
            colI += len;
            return 2;
        }

        int Tunnel()
        {
            // DOIS CAMINHOS: por cima (perigoso, exposto) ou pelo tunel (mais seguro, com premios)
            int len = r.Int(9, 12), top = h + 2, bottom = h + 4;
            for (int i = 0; i < len; i++) Fill(colI + i, h);
            Carve(colI, h, bottom);
            for (int i = 1; i < len - 1; i++) Carve(colI + i, top, bottom);
            Carve(colI + len - 1, h, bottom);
            if (r.Chance(0.5f)) Ladder(colI + len - 1, h, bottom);
            else Tile(colI + len - 1, bottom - 1, Terrain.BRICK);
            if (r.Chance(0.4f)) Ladder(colI, h, bottom);
            for (int i = 2; i < len - 2; i += 3) Prop(PropKind.Glorb, colI + i, (bottom + 1) * K.T - 12);
            if (r.Chance(0.5f)) Enemy(Grunt(), colI + len / 2, (bottom + 1) * K.T);
            if (r.Chance(0.3f)) Prop(PropKind.Cage, colI + len - 3, (bottom + 1) * K.T);
            // por cima: barricada com atiradores (o caminho "dificil")
            int mid = colI + len / 2;
            Tile(mid - 1, h - 1, Terrain.CRATE);
            Enemy(Shooter(), mid, h * K.T);
            if (r.Chance(0.6f)) Enemy(Shooter(), mid + 2, h * K.T);
            else if (act >= Act.Underground) Ambush(mid - 2, 2, 1, EnemyKind.Soldier);
            colI += len;
            return 3;
        }

        int House()
        {
            // PREDIO (Door Kickers): sala escura com porta. Chute a porta para atordoar quem esta atras.
            int w = r.Int(4, 7);
            bool two = r.Chance(city ? 0.7f : 0.35f);
            int A = colI + 1, B = A + w + 1, len = w + 3;
            for (int i = 0; i < len; i++) Fill(colI + i, h);
            void Floor(int y0)
            {
                // y0 = linha de cima do interior (2 de altura); teto em y0-1
                for (int x = A; x <= B; x++) Tile(x, y0 - 1, x == A || x == B ? Terrain.STEEL : Terrain.BRICK);
                for (int y = y0; y <= y0 + 1; y++) Tile(B, y, Terrain.BRICK);
                for (int y = y0; y <= y0 + 1; y++)
                    Ter.SetRaw(baseCol + A, y, Terrain.Make(Terrain.DOOR, 0, 0, y == y0 ? 0 : 1));
                for (int x = A + 1; x < B; x++) for (int y = y0; y <= y0 + 1; y++) Ter.SetRaw(baseCol + x, y, 0);
                S.Rooms.Add(new Room { X0 = baseCol + A + 1, Y0 = y0, X1 = baseCol + B - 1, Y1 = y0 + 1 });
                // dentro: inimigos virados para a porta; refem no fundo (nao atire nele!)
                int n = 1 + (r.Chance(0.35f + diff * 0.4f) ? 1 : 0);
                for (int k2 = 0; k2 < n; k2++) Enemy(Grunt(), A + 2 + k2 * 2, (y0 + 2) * K.T);
                if (r.Chance(0.6f)) Prop(PropKind.Hostage, B - 1, (y0 + 2) * K.T);
                else Prop(PropKind.Glorb, B - 1, (y0 + 2) * K.T - 10);
            }
            Floor(h - 2);
            if (two)
            {
                Floor(h - 5);
                if (r.Chance(0.6f)) Ladder(colI, h - 3, h - 1);
                else for (int y = h - 3; y < h; y++) Tile(colI, y, Terrain.BRICK);
            }
            if (r.Chance(0.4f)) Enemy(Shooter(), B - 1, (two ? h - 6 : h - 3) * K.T);   // vigia no telhado
            colI += len;
            return 3;
        }

        int Platforms()
        {
            // plataformas em degraus subindo, presas por aco, com premio no alto
            int len = 5;
            for (int i = 0; i < len; i++) Fill(colI + i, h);
            int py = h - 2;
            for (int i = 0; i < 3; i++) Tile(colI + i, py, i == 0 || i == 2 ? Terrain.STEEL : Terrain.BRICK);
            int py2 = py - 2;
            for (int i = 2; i < 5; i++) Tile(colI + i, py2, i == 2 || i == 4 ? Terrain.STEEL : Terrain.BRICK);
            bool foe = r.Chance(0.4f + diff * 0.3f) && Enemy(Shooter(), colI + 1, py * K.T);
            GlorbArc(colI + 2, 3, py2);
            if (r.Chance(0.2f)) Prop(PropKind.Cage, colI + 3, py2 * K.T);
            colI += len;
            return foe ? 2 : 1;
        }

        int Bunker()
        {
            // bunker: tijolos com teto de aco e torreta. Barril encostado na lateral = jeito esperto de abrir
            int len = 4, top = h - 2;
            Fill(colI, h);
            for (int i = 1; i < len; i++)
            {
                Fill(colI + i, h);
                for (int y = top; y < h; y++) Tile(colI + i, y, Terrain.BRICK);
                Tile(colI + i, top - 1, Terrain.STEEL);
                Back(colI + i, top - 1, h - 1);
            }
            if (r.Chance(0.6f)) Prop(PropKind.Barrel, colI, h * K.T);                    // telegrafado, antes do bunker
            if (r.Chance(0.35f)) Ladder(colI, top - 1, h - 1);
            if (!(c >= 2 && r.Chance(0.5f + diff * 0.4f) && Enemy(EnemyKind.Turret, colI + 2, (top - 1) * K.T)))
                Enemy(Shooter(), colI + 2, (top - 1) * K.T);
            Fill(colI + len, h);
            colI += len + 1;
            return 3;
        }

        int Camp()
        {
            // ACAMPAMENTO: barris colados (reacao em cadeia) vistos ANTES do grupo de inimigos, caixas de cobertura
            int len = r.Int(6, 8);                       // inimigos em colI+2..4, caixas na ultima coluna
            for (int i = 0; i < len; i++) Fill(colI + i, h);
            if (city) Car(colI + 1, h);                                                   // barricada de carro
            else { Prop(PropKind.Barrel, colI + 1, h * K.T); Prop(PropKind.Barrel, colI + 2, h * K.T); }
            int n = 2 + (r.Chance(0.3f + diff * 0.5f) ? 1 : 0);
            for (int i = 0; i < n; i++) Enemy(i == 0 ? Shooter() : Grunt(), colI + 2 + i, h * K.T);
            int stack = r.Int(1, 3);
            for (int y = 1; y <= stack; y++) Tile(colI + len - 1, h - y, Terrain.CRATE);
            if (r.Chance(0.5f)) Ambush(colI + 1, 1 + (r.Chance(diff) ? 1 : 0), 0, Grunt());     // reforcos
            colI += len;
            return 3;
        }

        int Crates()
        {
            // pilha de caixotes (cobertura que se destroi)
            int len = 3;
            for (int i = 0; i < len; i++) Fill(colI + i, h);
            int stack = r.Int(1, 3);
            for (int y = 1; y <= stack; y++) Tile(colI + 1, h - y, Terrain.CRATE);
            bool foe = r.Chance(0.4f) && Enemy(Grunt(), colI + 1, (h - stack) * K.T);
            colI += len;
            return foe ? 1 : 0;
        }

        int Cage()
        {
            // prisioneiro na jaula (+1 Time Out): recompensa depois de um desafio
            int len = 3;
            for (int i = 0; i < len; i++) Fill(colI + i, h);
            Prop(PropKind.Cage, colI + 1, h * K.T);
            if (r.Chance(0.4f)) Enemy(Grunt(), colI + 2, h * K.T);
            colI += len;
            return 0;
        }

        int Tower()
        {
            // TORRE: coluna de tijolos com sentinela no alto (sobe-se escalando); barril na base derruba tudo.
            // Sem escada: ela so daria acesso a um bloco.
            int len = 3, th = r.Int(3, 6);
            for (int i = 0; i < len; i++) Fill(colI + i, h);
            for (int y = 1; y <= th; y++) Tile(colI + 1, h - y, Terrain.BRICK);
            Back(colI + 1, h - th, h - 1);
            if (r.Chance(0.5f)) Prop(PropKind.Barrel, colI + 2, h * K.T);
            Enemy(Shooter(), colI + 1, (h - th) * K.T);
            if (th >= 4) GlorbArc(colI, 3, h - th);
            colI += len;
            return 2;
        }

        int Bridge()
        {
            // PONTE sobre um abismo, com inimigos em cima.
            // Madeira: atire nas tabuas para derruba-los, e nao pare em cima (a tabua pisada cai em 1 segundo).
            // Concreto: firme, so explosoes estragam; pilares descem ate o fundo do abismo.
            int gap = r.Int(5, 9);
            bool concrete = city || r.Chance(0.4f);                                       // na cidade: viadutos

            Fill(colI, h);
            for (int i = 1; i <= gap; i++)
            {
                Void(colI + i);
                Tile(colI + i, h, concrete ? Terrain.CONCRETE : Terrain.BRIDGE);
                if (concrete && (i % 4 == 2 || i == gap - 1 && gap >= 6 && i % 4 != 1))
                    for (int y = h + 1; y < K.ROWS; y++) Tile(colI + i, y, Terrain.CONCRETE);
            }
            Fill(colI + gap + 1, h); Fill(colI + gap + 2, h);
            int n = 1 + (r.Chance(0.55f + diff * 0.3f) ? 1 : 0) + (gap >= 7 && r.Chance(diff * 0.6f) ? 1 : 0);
            for (int k2 = 0; k2 < n; k2++) Enemy(Grunt(), colI + 2 + k2 * Math.Max(2, gap / n), h * K.T);
            if (r.Chance(0.35f)) GlorbArc(colI + 2, Math.Min(4, gap - 2), h - 2);
            else if (r.Chance(0.3f)) Prop(PropKind.Barrel, colI + gap, h * K.T);
            if (r.Chance(0.3f + diff * 0.3f)) Ambush(colI + 2, 1, 0, Grunt());              // vem gente do outro lado
            colI += gap + 3;
            return 3;
        }

        int Fortress()
        {
            // FORTALEZA (climax do level): torre de vigia com escada, barris na base da torre (cadeia),
            // patio com tropas, bunker com torreta e o prisioneiro guardado atras. As vezes um tunel por
            // baixo permite flanquear e sair no meio do patio.
            int len = 13;
            for (int i = 0; i < len; i++) Fill(colI + i, h);
            // torre de vigia com mirante de 3 blocos (aco nas pontas segura o tijolo do meio); a escada leva ao mirante
            int th = r.Int(4, 6), deck = h - th - 1;
            for (int y = 1; y <= th; y++) Tile(colI + 2, h - y, Terrain.BRICK);
            Tile(colI + 2, deck, Terrain.STEEL); Tile(colI + 3, deck, Terrain.BRICK); Tile(colI + 4, deck, Terrain.STEEL);
            Back(colI + 2, deck, h - 1);
            Ladder(colI + 1, deck, h - 1);
            Enemy(Shooter(), colI + 3, deck * K.T);
            Prop(PropKind.Barrel, colI + 3, h * K.T);
            Prop(PropKind.Barrel, colI + 4, h * K.T);
            // patio
            int n = 2 + (r.Chance(0.4f + diff * 0.5f) ? 1 : 0);
            int[] yard = { 5, 7, 6 };
            for (int i = 0; i < n; i++) Enemy(Grunt(), colI + yard[i], h * K.T);
            Tile(colI + 8, h - 1, Terrain.CRATE);
            // bunker com torreta
            for (int x = 9; x <= 10; x++)
            {
                for (int y = h - 2; y < h; y++) Tile(colI + x, y, Terrain.BRICK);
                Tile(colI + x, h - 3, Terrain.STEEL);
                Back(colI + x, h - 3, h - 1);
            }
            if (!(c >= 2 && Enemy(EnemyKind.Turret, colI + 10, (h - 3) * K.T))) Enemy(Shooter(), colI + 10, (h - 3) * K.T);
            // recompensa: prisioneiro atras do bunker
            Prop(PropKind.Cage, colI + 12, h * K.T);
            // ao chegar ao patio, chegam reforcos de paraquedas
            Ambush(colI + 4, 2 + (r.Chance(diff) ? 1 : 0), 1, EnemyKind.Soldier);
            // rota de flanco por baixo: desce antes da torre e sobe no meio do patio
            if (r.Chance(0.5f))
            {
                for (int x = 0; x <= 7; x++) Carve(colI + x, h + 2, h + 3);
                Carve(colI, h, h + 3); Ladder(colI, h, h + 3);
                Carve(colI + 7, h, h + 3); Ladder(colI + 7, h, h + 3);
                for (int x = 2; x <= 5; x++) Prop(PropKind.Glorb, colI + x, (h + 4) * K.T - 12);
            }
            colI += len;
            return 5;
        }

        int Build(Seg s) => s switch
        {
            Seg.Flat => Flat(), Seg.Breather => Breather(), Seg.Step => Step(), Seg.Pit => Pit(), Seg.Cave => Cave(),
            Seg.Tunnel => Tunnel(), Seg.House => House(), Seg.Platforms => Platforms(), Seg.Bunker => Bunker(),
            Seg.Camp => Camp(), Seg.Crates => Crates(), Seg.Cage => Cage(), Seg.Tower => Tower(), Seg.Bridge => Bridge(),
            Seg.Fortress => Fortress(), _ => Flat(),
        };

        // ---------------------------------------------------------------- montagem do chunk

        const int end = K.CHUNK - 1;                       // a ultima coluna fecha na altura do proximo chunk
        var anchors = Anchors(act);
        Seg anchor = anchors[r.Int(0, anchors.Length)];
        bool anchorDone = false;
        // a peca central entra logo no comeco ou depois de alguns trechos de aproximacao
        int anchorAt = r.Int(0, Math.Max(1, end - MaxWidth(anchor) - 1));
        Seg last = Seg.Breather;
        int lastIntensity = 0;
        bool rewardDue = false;
        var fill = Fillers(act);

        while (colI < end - 1)
        {
            Seg s;
            int room = end - colI;
            int reserve = anchorDone ? 0 : MaxWidth(anchor);      // espaco guardado para a peca central
            if (lastIntensity >= 3 && room - reserve >= 3)
                s = rewardDue && room >= 3 && r.Chance(0.35f) ? Seg.Cage : Seg.Breather;     // respiro ou recompensa
            else if (!anchorDone && (colI >= anchorAt || room - MaxWidth(anchor) < 3) && MaxWidth(anchor) <= room)
                s = anchor;
            else
            {
                // recheio: sorteio por peso, sem repetir o anterior, cabendo antes da peca central
                int limit = anchorDone || MaxWidth(anchor) > room ? room : room - MaxWidth(anchor);
                int total = 0;
                foreach (var (fs, w) in fill) if (fs != last && MaxWidth(fs) <= limit) total += w;
                if (total == 0) s = room >= 2 ? Seg.Breather : Seg.Flat;
                else
                {
                    int pick = r.Int(0, total);
                    s = fill[0].s;
                    foreach (var (fs, w) in fill)
                    {
                        if (fs == last || MaxWidth(fs) > limit) continue;
                        if (pick < w) { s = fs; break; }
                        pick -= w;
                    }
                }
            }
            if (s == anchor) anchorDone = true;
            if (s == Seg.Cage) rewardDue = false;
            GenTrace?.Add($"{c}|{act}|{s}|{colI}");
            lastIntensity = Build(s);
            if (lastIntensity >= 3 && s != Seg.Fortress) rewardDue = true;    // a fortaleza ja traz o seu prisioneiro
            last = s;
        }
        // fecha o chunk na altura do proximo
        for (int col = colI; col < K.CHUNK; col++) Fill(col, endH);
        // nenhuma escada inutil: precisa de chao e de alcancar alguma coisa
        for (int col = -1; col <= K.CHUNK; col++) ValidateLadderCol(baseCol + col);

        // o que sobrou do orcamento vira patrulha aerea (a partir do 3o chunk, fora da chegada)
        if (c >= 2 && act != Act.Arrival)
        {
            int flyers = budget >= 1 && r.Chance(0.25f + diff * 0.6f) ? 1 + (budget >= 2 && r.Chance(diff * 0.5f) ? 1 : 0) : 0;
            for (int i = 0; i < flyers; i++) Enemy(EnemyKind.Flyer, r.Int(4, K.CHUNK - 2), h * K.T - r.Range(56, 100));
        }
    }
}
