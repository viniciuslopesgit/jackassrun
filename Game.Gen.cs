namespace JackassRun;

/// <summary>Geracao procedural deterministica por chunk (mesma seed = mesmo chunk),
/// o que permite regenerar chunks com seguranca depois de rebobinar o tempo.</summary>
public sealed partial class Game
{
    void EnsureChunks()
    {
        while (S.NextChunk * K.CHUNK * K.T < S.CamX + K.MaxViewW + 96)
            GenChunk(S.NextChunk++);
    }

    public const int Ground0 = 14;     // linha tipica da superficie
    int HeightFor(int c) => c <= 0 ? Ground0 : Ground0 - 1 + (int)(Hash.H(c, (int)seed) % 3);

    void GenChunk(int c)
    {
        var r = new Rng(seed ^ ((uint)(c + 1) * 2246822519u));
        r.Next();
        int baseCol = c * K.CHUNK;
        int h = HeightFor(c), endH = HeightFor(c + 1);
        float diff = MathF.Min(1f, c / 26f);   // dificuldade sobe mais devagar
        int k = 0;
        int Id() => c * 1000 + (k++);

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
        float X(int col) => (baseCol + col) * K.T + K.T / 2f;

        void Enemy(EnemyKind kind, int col, float y)
        {
            int hp = kind switch { EnemyKind.Brute => 8, EnemyKind.Turret => 6, EnemyKind.Flyer => 2, EnemyKind.Rocketeer => 3,
                EnemyKind.Knife => 1, EnemyKind.Bomber => 1, _ => 2 };
            S.Enemies.Add(new Enemy
            {
                Id = Id(), Kind = kind, X = X(col), Y = y, BaseY = y, Hp = hp, Facing = -1,
                StateT = r.Range(0, 2), FireT = r.Range(0.5f, 1.5f), Phase = r.Range(0, 6),
            });
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
        void Prop(PropKind kind, int col, float y) =>
            S.Props.Add(new Prop { Id = Id(), Kind = kind, X = X(col), Y = y, Char = r.Int(0, Chars.All.Length), T = r.Range(0, 5) });
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

        // Medidas em blocos de 16px: o heroi tem ~1 bloco de altura, pula ~2,8 blocos e escala paredes.
        int colI = 0;
        while (colI < K.CHUNK - 3)
        {
            int roll = r.Int(0, 100);
            if (roll < 16)
            {
                // terreno plano com patrulha
                int len = r.Int(2, 5);
                for (int i = 0; i < len; i++) Fill(colI + i, h);
                if (r.Chance(0.25f + diff * 0.25f)) Enemy(Grunt(), colI + len / 2, h * K.T);
                if (r.Chance(0.3f)) GlorbArc(colI, Math.Min(3, len), h);
                colI += len;
            }
            else if (roll < 28)
            {
                // degrau de 1 ou 2 blocos
                int dh = r.Int(1, 3) * (r.Chance(0.5f) ? 1 : -1);
                h = Math.Clamp(h + dh, Ground0 - 3, Ground0 + 2);
                int len = r.Int(2, 4);
                for (int i = 0; i < len; i++) Fill(colI + i, h);
                if (r.Chance(0.18f + diff * 0.25f)) Enemy(Grunt(), colI + len - 1, h * K.T);
                colI += len;
            }
            else if (roll < 36)
            {
                // buraco sem fundo (queda fatal), as vezes com ponte de tijolos
                int len = r.Int(1, 3 + (diff > 0.5f ? 1 : 0));
                Fill(colI, h);
                for (int i = 1; i <= len; i++) Void(colI + i);
                if (len >= 2 && r.Chance(0.6f))
                    for (int i = 1; i <= len; i++) Tile(colI + i, h - 2, Terrain.BRICK);
                else GlorbArc(colI + 1, len, h);
                Fill(colI + len + 1, h);
                colI += len + 2;
            }
            else if (roll < 50 && colI < K.CHUNK - 9)
            {
                // CAVERNA: um buraco que NAO mata. Cai-se numa caverna com premios e sai-se por um poco.
                int len = r.Int(7, 10), floor = h + r.Int(5, 7), roof = h + 2;
                floor = Math.Min(floor, K.ROWS - 3);
                for (int i = 0; i < len; i++) Fill(colI + i, h);
                Carve(colI + 1, h, floor - 1); Carve(colI + 2, h, floor - 1);             // entrada (2 de largura)
                for (int i = 3; i < len - 2; i++) Carve(colI + i, roof, floor - 1);        // salao
                Carve(colI + len - 2, h, floor - 1);                                       // poco de saida
                if (floor - h >= 5) Tile(colI + len - 2, floor - 3, Terrain.BRICK);        // degrau para sair pulando
                // premios e perigos la dentro
                GlorbArc(colI + 3, Math.Min(4, len - 5), floor);
                if (r.Chance(0.45f)) Prop(PropKind.Cage, colI + len - 4, floor * K.T);
                else if (r.Chance(0.5f)) Prop(PropKind.Barrel, colI + len - 4, floor * K.T);
                if (r.Chance(0.35f + diff * 0.35f)) Enemy(EnemyKind.Soldier, colI + 4, floor * K.T);
                if (r.Chance(0.4f)) Enemy(Grunt(), colI + len - 1, h * K.T);            // vigia em cima
                colI += len;
            }
            else if (roll < 60 && colI < K.CHUNK - 11)
            {
                // TUNEL: caminho alternativo por baixo da superficie, com entrada e saida em pocos
                int len = r.Int(9, 12), top = h + 2, bottom = h + 4;
                for (int i = 0; i < len; i++) Fill(colI + i, h);
                Carve(colI, h, bottom);                                                      // entrada
                for (int i = 1; i < len - 1; i++) Carve(colI + i, top, bottom);
                Carve(colI + len - 1, h, bottom);                                            // saida
                Tile(colI + len - 1, bottom - 1, Terrain.BRICK);                             // degrau
                for (int i = 2; i < len - 2; i += 3) Prop(PropKind.Glorb, colI + i, (bottom + 1) * K.T - 12);
                if (r.Chance(0.5f + diff * 0.3f)) Enemy(Grunt(), colI + len / 2, (bottom + 1) * K.T);
                if (r.Chance(0.3f)) Prop(PropKind.Cage, colI + len - 3, (bottom + 1) * K.T);
                // na superficie, perigo por cima
                if (r.Chance(0.5f)) Enemy(Grunt(), colI + len / 2 + 1, h * K.T);
                colI += len;
            }
            else if (roll < 66 && c >= 1 && colI < K.CHUNK - 10)
            {
                // PREDIO (Door Kickers): sala escura com porta. Chute a porta para atordoar quem esta atras.
                int w = r.Int(4, 7);
                bool two = r.Chance(0.35f);
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
                    // dentro: inimigos virados para a porta, as vezes um refem no fundo
                    int n = 1 + (r.Chance(0.35f + diff * 0.4f) ? 1 : 0);
                    for (int k2 = 0; k2 < n; k2++) Enemy(Grunt(), A + 2 + k2 * 2, (y0 + 2) * K.T);
                    if (r.Chance(0.6f)) Prop(PropKind.Hostage, B - 1, (y0 + 2) * K.T);
                    else Prop(PropKind.Glorb, B - 1, (y0 + 2) * K.T - 10);
                }
                Floor(h - 2);
                if (two)
                {
                    Floor(h - 5);
                    // pilar do lado de fora para alcancar a porta de cima (escale)
                    for (int y = h - 3; y < h; y++) Tile(colI, y, Terrain.BRICK);
                }
                if (r.Chance(0.4f)) Enemy(Grunt(), B - 1, (two ? h - 6 : h - 3) * K.T);   // vigia no telhado
                colI += len;
            }
            else if (roll < 70)
            {
                // plataformas em degraus subindo para o ceu, presas por aco
                int len = 5;
                for (int i = 0; i < len; i++) Fill(colI + i, h);
                int py = h - 2;
                for (int i = 0; i < 3; i++) Tile(colI + i, py, i == 0 || i == 2 ? Terrain.STEEL : Terrain.BRICK);
                int py2 = py - 2;
                for (int i = 2; i < 5; i++) Tile(colI + i, py2, i == 2 || i == 4 ? Terrain.STEEL : Terrain.BRICK);
                if (r.Chance(0.4f + diff * 0.3f)) Enemy(Grunt(), colI + 1, py * K.T);
                GlorbArc(colI + 2, 3, py2);
                if (r.Chance(0.25f)) Prop(PropKind.Cage, colI + 3, py2 * K.T);
                colI += len;
            }
            else if (roll < 78)
            {
                // bunker: bloco de tijolos com teto de aco e torreta
                int len = 4, top = h - 2;
                Fill(colI, h);
                for (int i = 1; i < len; i++)
                {
                    Fill(colI + i, h);
                    for (int y = top; y < h; y++) Tile(colI + i, y, Terrain.BRICK);
                    Tile(colI + i, top - 1, Terrain.STEEL);
                }
                if (c >= 2 && r.Chance(0.5f + diff * 0.4f)) Enemy(EnemyKind.Turret, colI + 2, (top - 1) * K.T);
                else Enemy(Grunt(), colI + 2, (top - 1) * K.T);
                Fill(colI + len, h);
                if (r.Chance(0.5f)) Prop(PropKind.Barrel, colI + len, h * K.T);
                colI += len + 1;
            }
            else if (roll < 84)
            {
                // barris explosivos perto de inimigos
                int len = 4;
                for (int i = 0; i < len; i++) Fill(colI + i, h);
                Prop(PropKind.Barrel, colI + 1, h * K.T);
                Enemy(Grunt(), colI + 2, h * K.T);
                if (r.Chance(0.15f + diff * 0.35f)) Enemy(EnemyKind.Soldier, colI + 3, h * K.T);
                colI += len;
            }
            else if (roll < 89)
            {
                // pilha de caixotes
                int len = 3;
                for (int i = 0; i < len; i++) Fill(colI + i, h);
                int stack = r.Int(1, 3);
                for (int y = 1; y <= stack; y++) Tile(colI + 1, h - y, Terrain.CRATE);
                if (r.Chance(0.5f)) Enemy(Grunt(), colI + 1, (h - stack) * K.T);
                colI += len;
            }
            else if (roll < 94 && r.Chance(0.55f))
            {
                // prisioneiro na jaula (+1 Time Out)
                int len = 3;
                for (int i = 0; i < len; i++) Fill(colI + i, h);
                Prop(PropKind.Cage, colI + 1, h * K.T);
                if (r.Chance(0.4f)) Enemy(Grunt(), colI + 2, h * K.T);
                colI += len;
            }
            else
            {
                // torre alta de tijolos com sentinela e premio no topo (escale as paredes)
                int len = 3, th = r.Int(3, 6);
                for (int i = 0; i < len; i++) Fill(colI + i, h);
                for (int y = 1; y <= th; y++) Tile(colI + 1, h - y, Terrain.BRICK);
                Enemy(Grunt(), colI + 1, (h - th) * K.T);
                if (th >= 4) GlorbArc(colI, 3, h - th);
                colI += len;
            }
        }
        // fecha o chunk na altura do proximo
        for (int col = colI; col < K.CHUNK; col++) Fill(col, endH);

        // voadores no ceu
        int flyers = r.Chance(0.25f + diff * 0.6f) ? 1 + (r.Chance(diff * 0.5f) ? 1 : 0) : 0;
        if (c < 2) flyers = 0;
        for (int i = 0; i < flyers; i++) Enemy(EnemyKind.Flyer, r.Int(4, K.CHUNK - 2), h * K.T - r.Range(56, 100));
    }
}
