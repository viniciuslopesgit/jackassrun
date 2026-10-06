namespace JackassRun;

/// <summary>Geracao procedural deterministica por chunk (mesma seed = mesmo chunk),
/// o que permite regenerar chunks com seguranca depois de rebobinar o tempo.</summary>
public sealed partial class Game
{
    void EnsureChunks()
    {
        while (S.NextChunk * K.CHUNK * K.T < S.CamX + K.W + 96)
            GenChunk(S.NextChunk++);
    }

    int HeightFor(int c) => c <= 0 ? 17 : 15 + (int)(Hash.H(c, (int)seed) % 4);

    void GenChunk(int c)
    {
        var r = new Rng(seed ^ ((uint)(c + 1) * 2246822519u));
        r.Next();
        int baseCol = c * K.CHUNK;
        int h = HeightFor(c), endH = HeightFor(c + 1);
        float diff = MathF.Min(1f, c / 16f);
        int k = 0;
        int Id() => c * 1000 + (k++);

        void Fill(int col, int height, int type = Terrain.DIRT)
        {
            int tx = baseCol + col;
            for (int y = 0; y < K.ROWS; y++)
                Ter.SetRaw(tx, y, y >= height ? Terrain.Make(type) : (byte)0);
        }
        void Tile(int col, int row, int type) => Ter.SetRaw(baseCol + col, row, Terrain.Make(type));
        float X(int col) => (baseCol + col) * K.T + K.T / 2f;

        void Enemy(EnemyKind kind, int col, float y)
        {
            int hp = kind switch { EnemyKind.Brute => 8, EnemyKind.Turret => 6, EnemyKind.Flyer => 2, EnemyKind.Rocketeer => 3, _ => 2 };
            S.Enemies.Add(new Enemy
            {
                Id = Id(), Kind = kind, X = X(col), Y = y, BaseY = y, Hp = hp, Facing = -1,
                StateT = r.Range(0, 2), FireT = r.Range(0.5f, 1.5f), Phase = r.Range(0, 6),
            });
        }
        EnemyKind Grunt()
        {
            float v = r.F();
            if (c >= 5 && v < 0.12f + diff * 0.12f) return EnemyKind.Brute;
            if (c >= 2 && v < 0.35f + diff * 0.1f) return EnemyKind.Rocketeer;
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
            for (int col = 0; col < K.CHUNK; col++) Fill(col, 17);
            GlorbArc(14, 5, 17);
            GlorbArc(26, 5, 17);
            for (int i = 0; i < 4; i++) Tile(33 + i, 13, Terrain.BRICK);
            Prop(PropKind.Barrel, 37, 17 * K.T);
            return;
        }

        int colI = 0;
        while (colI < K.CHUNK - 5)
        {
            int roll = r.Int(0, 100);
            if (roll < 20)
            {
                // terreno plano com patrulha
                int len = r.Int(4, 8);
                for (int i = 0; i < len; i++) Fill(colI + i, h);
                if (r.Chance(0.45f + diff * 0.35f)) Enemy(Grunt(), colI + len / 2, h * K.T);
                if (r.Chance(0.3f)) GlorbArc(colI + 1, Math.Min(4, len - 1), h);
                colI += len;
            }
            else if (roll < 34)
            {
                // degrau
                int dh = r.Int(1, 4) * (r.Chance(0.5f) ? 1 : -1);
                h = Math.Clamp(h + dh, 12, 19);
                int len = r.Int(3, 6);
                for (int i = 0; i < len; i++) Fill(colI + i, h);
                if (r.Chance(0.35f + diff * 0.3f)) Enemy(Grunt(), colI + len - 1, h * K.T);
                colI += len;
            }
            else if (roll < 48)
            {
                // buraco no espaco-tempo
                int len = r.Int(2, 4 + (int)(diff * 2));
                Fill(colI, h);
                for (int i = 1; i <= len; i++) Fill(colI + i, K.ROWS + 5);
                if (len >= 4 && r.Chance(0.6f))
                    for (int i = 2; i < len; i++) Tile(colI + i, h - 3, Terrain.BRICK);
                else GlorbArc(colI + 1, len, h);
                Fill(colI + len + 1, h); Fill(colI + len + 2, h);
                colI += len + 3;
            }
            else if (roll < 60)
            {
                // plataforma flutuante
                int len = 8;
                for (int i = 0; i < len; i++) Fill(colI + i, h);
                int py = h - r.Int(4, 6);
                for (int i = 1; i < 7; i++) Tile(colI + i, py, i == 1 || i == 6 ? Terrain.STEEL : Terrain.BRICK);
                if (r.Chance(0.55f + diff * 0.3f)) Enemy(Grunt(), colI + 4, py * K.T);
                else GlorbArc(colI + 2, 4, py);
                if (r.Chance(0.4f)) Enemy(EnemyKind.Soldier, colI + 6, h * K.T);
                colI += len;
            }
            else if (roll < 71)
            {
                // bunker: bloco de tijolos com teto de aco e torreta
                int len = 7, top = h - 4;
                Fill(colI, h);
                for (int i = 1; i < len; i++)
                {
                    Fill(colI + i, h);
                    for (int y = top; y < h; y++) Tile(colI + i, y, Terrain.BRICK);
                    Tile(colI + i, top - 1, Terrain.STEEL);
                }
                if (c >= 2 && r.Chance(0.5f + diff * 0.4f)) Enemy(EnemyKind.Turret, colI + 4, (top - 1) * K.T);
                else Enemy(Grunt(), colI + 4, (top - 1) * K.T);
                if (r.Chance(0.5f)) Prop(PropKind.Barrel, colI + len, h * K.T);
                Fill(colI + len, h);
                colI += len + 1;
            }
            else if (roll < 80)
            {
                // barris explosivos perto de inimigos
                int len = 7;
                for (int i = 0; i < len; i++) Fill(colI + i, h);
                Prop(PropKind.Barrel, colI + 2, h * K.T);
                if (r.Chance(0.6f)) Prop(PropKind.Barrel, colI + 3, h * K.T);
                Enemy(Grunt(), colI + 4, h * K.T);
                if (r.Chance(0.3f + diff * 0.4f)) Enemy(EnemyKind.Soldier, colI + 6, h * K.T);
                colI += len;
            }
            else if (roll < 88)
            {
                // pilha de caixotes
                int len = 6;
                for (int i = 0; i < len; i++) Fill(colI + i, h);
                int stack = r.Int(1, 4);
                for (int i = 2; i < 4; i++) for (int y = 1; y <= stack; y++) Tile(colI + i, h - y, Terrain.CRATE);
                if (r.Chance(0.5f)) Enemy(Grunt(), colI + 3, (h - stack) * K.T);
                colI += len;
            }
            else if (roll < 94 && r.Chance(0.55f))
            {
                // prisioneiro na jaula (+1 Time Out)
                int len = 6;
                for (int i = 0; i < len; i++) Fill(colI + i, h);
                Prop(PropKind.Cage, colI + 3, h * K.T);
                if (r.Chance(0.6f)) Enemy(Grunt(), colI + 5, h * K.T);
                colI += len;
            }
            else
            {
                // torre de tijolos com sentinela
                int len = 5, th = r.Int(3, 6);
                for (int i = 0; i < len; i++) Fill(colI + i, h);
                for (int i = 1; i < 3; i++) for (int y = 1; y <= th; y++) Tile(colI + i, h - y, Terrain.BRICK);
                Enemy(Grunt(), colI + 2, (h - th) * K.T);
                colI += len;
            }
        }
        // fecha o chunk na altura do proximo
        for (int col = colI; col < K.CHUNK; col++) Fill(col, endH);

        // voadores
        int flyers = r.Chance(0.25f + diff * 0.6f) ? 1 + (r.Chance(diff * 0.5f) ? 1 : 0) : 0;
        if (c < 2) flyers = 0;
        for (int i = 0; i < flyers; i++) Enemy(EnemyKind.Flyer, r.Int(8, K.CHUNK - 4), r.Range(28, 70));
    }
}
