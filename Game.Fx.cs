using Raylib_cs;
using static JackassRun.Col;

namespace JackassRun;

/// <summary>Efeitos no estilo Broforce: bolas de fogo feitas de particulas, fumaca densa,
/// destrocos em chamas, fogo no chao, sangue que mancha o cenario, inimigos que explodem
/// em pedacos e estruturas que desabam quando perdem a sustentacao.</summary>
public sealed partial class Game
{
    readonly List<Decal> decals = new();
    readonly List<(int x, int y)> collapseQ = new();

    static readonly Color BloodRed = Hex(0xc0121e), BloodDark = Hex(0x7a0a12), Oil = Hex(0x2a2638);
    static readonly Color SmokeDark = Hex(0x463e40), FireRed = Hex(0xd8401e);

    static Color BloodOf(Era e) => e.Style == BgStyle.Future ? Oil : BloodRed;

    // ------------------------------------------------------------------ particulas basicas

    Particle? AddPart(PKind k, float x, float y, float vx, float vy, float life, float size, Color c, float grav = 0)
    {
        if (parts.Count > 2400) return null;
        var p = new Particle { Kind = k, X = x, Y = y, VX = vx, VY = vy, Life = life, Max = life, Size = size, C = c, Grav = grav };
        parts.Add(p);
        return p;
    }

    void AddText(float x, float y, string s, Color c) => texts.Add(new FloatText { X = x, Y = y, Text = s, C = c, Life = 1.2f });

    float R(float a, float b) => a + (float)fx.NextDouble() * (b - a);

    void AddDecal(float x, float y, Color c)
    {
        if (decals.Count > 6000) decals.RemoveRange(0, 600);
        decals.Add(new Decal { X = (int)MathF.Floor(x), Y = (int)MathF.Floor(y), C = c });
    }

    // ------------------------------------------------------------------ sangue e pedacos

    void BloodSpray(float x, float y, float dir, int n, Color c, float power = 1)
    {
        for (int k = 0; k < n; k++)
        {
            float vx = dir != 0 ? dir * R(30, 170) * power + R(-25, 25) : R(-160, 160) * power;
            var col = k % 3 == 0 ? Col.Mul(c, 0.65f) : c;
            AddPart(PKind.Blood, x + R(-2, 2), y + R(-3, 3), vx, -R(20, 150) * power, 1.5f, k % 5 == 0 ? 2 : 1, col, 540);
        }
    }

    /// <summary>Corpo explodindo em pedacos (cabeca, tronco, bracos, pernas) + chuva de sangue.</summary>
    void Gibs(float x, float y, Color skin, Color shirt, Color pants, Color blood, int bloodN)
    {
        (Color c, int s)[] chunks = { (skin, 3), (shirt, 4), (shirt, 2), (skin, 2), (skin, 2), (pants, 3), (pants, 2) };
        foreach (var (c, s) in chunks)
        {
            var p = AddPart(PKind.Gib, x + R(-3, 3), y + R(-5, 5), R(-160, 160), -R(140, 320), 3f, s, c, 560);
            if (p != null) p.C2 = blood;
        }
        BloodSpray(x, y, 0, bloodN, blood, 1.3f);
        for (int k = 0; k < 4; k++) AddPart(PKind.Smoke, x + R(-4, 4), y + R(-4, 4), R(-20, 20), R(-20, 5), 0.5f, 3, A(blood, 0.55f));
        AddPart(PKind.Flash, x, y, 0, 0, 0.06f, 6, White);
    }

    void AddCorpse(float x, float y, Look look, int facing, float dir, bool flyer, Era? era)
    {
        if (dir == 0) dir = -facing;
        corpses.Add(new Corpse
        {
            X = x, Y = y, Look = look, Facing = facing, IsFlyer = flyer, Era = era,
            VX = dir * R(90, 160), VY = -R(160, 250), Spin = dir * R(500, 900) * facing,
        });
    }

    // ------------------------------------------------------------------ dano em inimigos

    void HitEnemy(Enemy e, int dmg, float kx, bool boom = false)
    {
        if (e.Dead) return;
        e.Hp -= dmg;
        e.HurtT = 0.1f;
        e.LastBoom = boom;
        if (kx != 0) e.HitDir = MathF.Sign(kx);
        if (!e.Alerted && e.Kind != EnemyKind.Flyer) { e.Alerted = true; e.FireT = 0.4f; }
        if (e.Kind != EnemyKind.Turret && e.Kind != EnemyKind.Flyer) e.X += kx;
        sfx.Play("hit", 0.5f);
        var era = Eras.ForX(e.X);
        float cy = e.Kind == EnemyKind.Flyer ? e.Y : e.Y - 11;
        bool metal = e.Kind == EnemyKind.Turret || era.Style == BgStyle.Future || e.Kind == EnemyKind.Flyer && era.Style == BgStyle.Jungle;
        if (metal)
            for (int k = 0; k < 5; k++) AddPart(PKind.Spark, e.X, cy, e.HitDir * R(30, 120), R(-90, 30), 0.25f, 1, k % 2 == 0 ? Yellow : White);
        if (e.Kind != EnemyKind.Turret) BloodSpray(e.X, cy, e.HitDir, 5, BloodOf(era));
        if (e.Hp <= 0) KillEnemy(e);
    }

    void KillEnemy(Enemy e)
    {
        e.Dead = true;
        int pts = e.Kind switch { EnemyKind.Brute => 300, EnemyKind.Turret => 250, EnemyKind.Flyer => 150, EnemyKind.Rocketeer => 150, _ => 100 };
        S.Score += pts; S.Kills++;
        var era = Eras.ForX(e.X);
        var blood = BloodOf(era);
        sfx.Play("edie");
        float cy = e.Kind == EnemyKind.Flyer ? e.Y : e.Y - 11;
        bool machine = e.Kind == EnemyKind.Turret || e.Kind == EnemyKind.Flyer && era.Style is BgStyle.Future or BgStyle.Jungle;
        if (machine)
        {
            Explode(e.X, cy, 16, true, K.PLAYER_ID);
            if (e.Kind == EnemyKind.Flyer) AddCorpse(e.X, cy, default, e.Facing, e.HitDir, true, era);
        }
        else if (e.Kind == EnemyKind.Flyer)
        {
            BloodSpray(e.X, cy, e.HitDir, 14, blood, 1.1f);
            AddCorpse(e.X, cy, default, e.Facing, e.HitDir, true, era);
        }
        else
        {
            var look = EnemyLook(era, e.Kind);
            if (e.LastBoom) Gibs(e.X, cy, look.Skin, look.Shirt, look.Pants, blood, e.Kind == EnemyKind.Brute ? 45 : 30);
            else
            {
                AddCorpse(e.X, e.Y - 10, look, e.Facing, e.HitDir, false, era);
                BloodSpray(e.X, cy, e.HitDir, e.Kind == EnemyKind.Brute ? 24 : 16, blood, 1.25f);
            }
            if (era.Style == BgStyle.Future)
                for (int k = 0; k < 8; k++) AddPart(PKind.Spark, e.X, cy, R(-120, 120), R(-140, 20), 0.35f, 1, Cyan);
        }
        AddText(e.X, cy - 16, "+" + pts, Yellow);
        shake = MathF.Max(shake, 2);
        if (e.Kind == EnemyKind.Brute) hitStop = 0.05f;
    }

    // ------------------------------------------------------------------ terreno

    /// <summary>Danifica um tile. Retorna true se ele foi destruido.</summary>
    bool DamageTile(int tx, int ty, int dmg)
    {
        byte b = Ter.Get(tx, ty);
        int type = Terrain.TypeOf(b);
        if (type == 0) return true;
        float cx = tx * K.T + 4, cy = ty * K.T + 4;
        if (type == Terrain.STEEL)
        {
            if (dmg < 99) sfx.Play("clank", 0.5f);
            for (int k = 0; k < 3; k++) AddPart(PKind.Spark, cx, cy, R(-70, 70), -R(20, 80), 0.18f, 1, k == 0 ? White : Yellow);
            return false;
        }
        int d = Terrain.DmgOf(b) + dmg;
        var era = Eras.ForCol(tx);
        var c = TileColor(type, era);
        var dust = Col.Lerp(c, White, 0.35f);
        if (d >= Terrain.HpOf(type))
        {
            Ter.Set(S.Frame, tx, ty, 0);
            bool boom = dmg >= 99;
            int n = boom ? 2 : 5;
            for (int k = 0; k < n; k++)
            {
                var kind = boom && k == 0 && fx.Next(2) == 0 ? PKind.BurnDebris : PKind.Debris;
                AddPart(kind, cx + R(-3, 3), cy + R(-3, 3), R(-90, 90) * (boom ? 1.8f : 1), -R(60, 190) * (boom ? 1.5f : 1),
                    boom ? 1.8f : 1.2f, fx.Next(2, 4), c, 540);
            }
            if (!boom || fx.Next(2) == 0) AddPart(PKind.Smoke, cx, cy, R(-12, 12), -R(5, 25), 0.7f, 3, A(dust, 0.85f));
            collapseQ.Add((tx - 1, ty)); collapseQ.Add((tx + 1, ty));
            collapseQ.Add((tx, ty - 1)); collapseQ.Add((tx, ty + 1));
            return true;
        }
        Ter.Set(S.Frame, tx, ty, Terrain.Make(type, d));
        for (int k = 0; k < 2; k++) AddPart(PKind.Debris, cx + R(-3, 3), cy + R(-3, 3), R(-50, 50), -R(30, 100), 0.6f, 1, c, 540);
        AddPart(PKind.Smoke, cx, cy, R(-8, 8), -R(4, 12), 0.35f, 1.5f, A(dust, 0.7f));
        return false;
    }

    static Color TileColor(int type, Era e) => type switch
    {
        Terrain.DIRT => e.Dirt, Terrain.BRICK => e.Brick, Terrain.STEEL => e.Steel, _ => Hex(0xa8743a),
    };

    /// <summary>Estruturas sem apoio (sem chao embaixo nem aco segurando) desabam em blocos.</summary>
    void ProcessCollapse()
    {
        if (collapseQ.Count == 0) return;
        var stack = new Stack<(int, int)>();
        var seen = new HashSet<(int, int)>();
        var comp = new List<(int x, int y)>();
        foreach (var start in collapseQ)
        {
            if (!Ter.Solid(start.x, start.y) || Ter.Type(start.x, start.y) == Terrain.STEEL) continue;
            stack.Clear(); seen.Clear(); comp.Clear();
            stack.Push(start); seen.Add(start);
            bool supported = false;
            while (stack.Count > 0 && !supported)
            {
                var (x, y) = stack.Pop();
                comp.Add((x, y));
                if (y >= K.ROWS - 1 || comp.Count > 180) { supported = true; break; }
                foreach (var n in new[] { (x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1) })
                {
                    if (!Ter.Solid(n.Item1, n.Item2)) continue;
                    if (Ter.Type(n.Item1, n.Item2) == Terrain.STEEL) { supported = true; break; }
                    if (seen.Add(n)) stack.Push(n);
                }
            }
            if (supported) continue;
            foreach (var (x, y) in comp)
            {
                byte b = Ter.Get(x, y);
                Ter.Set(S.Frame, x, y, 0);
                S.Falling.Add(new FallingBlock { X = x * K.T, Y = y * K.T, Tile = b, VY = R(-10, 20) });
            }
            sfx.Play("boom", 0.35f, 0.5f);
            shake = MathF.Max(shake, 2.5f);
        }
        collapseQ.Clear();
    }

    void UpdateFalling()
    {
        float dt = K.DT;
        foreach (var f in S.Falling)
        {
            if (f.Dead) continue;
            f.VY = MathF.Min(f.VY + K.GRAV * dt, 420);
            float ny = f.Y + f.VY * dt;
            int tx = (int)MathF.Floor((f.X + 4) / K.T);
            int below = (int)MathF.Floor((ny + K.T) / K.T);
            if (Ter.Solid(tx, below))
            {
                int ty = below - 1;
                if (ty >= 0 && !Ter.Solid(tx, ty)) Ter.Set(S.Frame, tx, ty, f.Tile);
                f.Dead = true;
                var c = TileColor(Terrain.TypeOf(f.Tile), Eras.ForCol(tx));
                for (int k = 0; k < 3; k++) AddPart(PKind.Smoke, f.X + R(0, 8), ty * K.T + 8, R(-30, 30), -R(5, 20), 0.6f, 2.5f, A(Col.Lerp(c, White, 0.35f), 0.8f));
                AddPart(PKind.Debris, f.X + 4, ty * K.T + 6, R(-60, 60), -R(40, 100), 0.8f, 2, c, 540);
                sfx.Play("hit", 0.4f, 0.5f);
                shake = MathF.Max(shake, 1.2f);
                continue;
            }
            if (ny > K.ROWS * K.T + 16) { f.Dead = true; continue; }
            f.Y = ny;
            if (f.VY < 120) continue;
            // esmaga quem estiver embaixo
            foreach (var e in S.Enemies)
            {
                if (e.Dead || e.Kind == EnemyKind.Flyer) continue;
                var (x0, y0, x1, y1) = e.Box;
                if (Hit(f.X, f.Y, f.X + 8, f.Y + 8, x0, y0, x1, y1)) { e.HitDir = fx.Next(2) == 0 ? -1 : 1; HitEnemy(e, 20, 0, true); }
            }
            foreach (var p in S.Props)
                if (!p.Done && p.Kind == PropKind.Barrel && p.Fuse < 0 && Hit(f.X, f.Y, f.X + 8, f.Y + 8, p.X - 4, p.Y - 10, p.X + 4, p.Y))
                    p.Fuse = 0.05f;
            if (!P.Dead && Hit(f.X, f.Y, f.X + 8, f.Y + 8, P.X - 3, P.Y - 16, P.X + 3, P.Y))
                KillPlayer(0, "ESMAGADO POR ESCOMBROS", true);
        }
    }

    // ------------------------------------------------------------------ explosoes

    void Explode(float x, float y, float r, bool fromPlayer, int owner)
    {
        S.Explosions.Add(new Explosion { X = x, Y = y, R = r, FromPlayer = fromPlayer, Dur = 0.35f + r / 80f });
        sfx.Play("boom", MathF.Min(1, r / 28f));
        shake = MathF.Max(shake, r / 4f);
        flash = MathF.Max(flash, r / 100f);
        if (r >= 30) hitStop = MathF.Max(hitStop, 0.035f);

        // destroi terreno
        int r2 = (int)(r / K.T) + 1;
        int cx = (int)MathF.Floor(x / K.T), cy = (int)MathF.Floor(y / K.T);
        for (int tx = cx - r2; tx <= cx + r2; tx++)
            for (int ty = cy - r2; ty <= cy + r2; ty++)
            {
                float dx = tx * K.T + 4 - x, dy = ty * K.T + 4 - y;
                if (dx * dx + dy * dy > r * r * 0.8f) continue;
                if (Ter.Solid(tx, ty)) DamageTile(tx, ty, 99);
            }
        foreach (var e in S.Enemies)
        {
            if (e.Dead) continue;
            var (x0, y0, x1, y1) = e.Box;
            float ex = (x0 + x1) / 2, ey = (y0 + y1) / 2;
            if ((ex - x) * (ex - x) + (ey - y) * (ey - y) < (r + 6) * (r + 6))
            {
                e.HitDir = MathF.Sign(ex - x + 0.01f);
                HitEnemy(e, 6, 0, true);
                if (!e.Dead && e.Kind != EnemyKind.Turret && e.Kind != EnemyKind.Flyer) { e.VY = -170; e.X += MathF.Sign(ex - x) * 4; }
            }
        }
        foreach (var p in S.Props)
            if (!p.Done && p.Kind == PropKind.Barrel && p.Fuse < 0 && MathF.Abs(p.X - x) < r + 4 && MathF.Abs(p.Y - 5 - y) < r + 4)
                p.Fuse = 0.12f;
        // explosoes de inimigos e barris machucam o jogador (como no Broforce)
        if (!fromPlayer && !P.Dead)
        {
            float dx = P.X - x, dy = P.Y - 8 - y;
            if (dx * dx + dy * dy < r * r * 0.75f) KillPlayer(owner > 0 ? owner : 0, "EXPLODIDO", true);
        }

        // ---- visual
        AddPart(PKind.Flash, x, y, 0, 0, 0.07f, r * 0.45f, White);
        AddPart(PKind.Ring, x, y, 0, 0, 0.25f, r * 1.7f, White);
        float lifeK = 0.6f + r / 50f;
        // nucleo da bola de fogo
        for (int k = 0; k < 6; k++)
            AddPart(PKind.Fire, x + R(-r, r) * 0.2f, y + R(-r, r) * 0.2f, R(-20, 20), R(-40, 0), R(0.6f, 0.95f) * lifeK, R(r * 0.3f, r * 0.45f), Orange);
        // labaredas espalhadas
        int nFire = (int)(r * 1.5f);
        for (int k = 0; k < nFire; k++)
        {
            float a = R(0, MathF.Tau), sp = R(0.2f, 1f) * r * 4.2f;
            AddPart(PKind.Fire, x + MathF.Cos(a) * r * 0.2f, y + MathF.Sin(a) * r * 0.2f, MathF.Cos(a) * sp, MathF.Sin(a) * sp * 0.8f - r,
                R(0.4f, 0.9f) * lifeK, R(r * 0.1f, r * 0.24f) + 1, Orange);
        }
        // coluna de fumaca escura
        for (int k = 0; k < (int)(r / 2.5f); k++)
            AddPart(PKind.Smoke, x + R(-r, r) * 0.5f, y + R(-r, r) * 0.4f, R(-25, 25), -R(15, 50), R(1.4f, 2.6f), R(r * 0.14f, r * 0.28f) + 1, A(SmokeDark, 0.9f));
        // destrocos em chamas
        for (int k = 0; k < 3 + (int)(r / 8); k++)
            AddPart(PKind.BurnDebris, x, y, R(-220, 220), -R(160, 340), R(1.2f, 2f), fx.Next(2, 4), Hex(0x4a3428), 540);
        // fagulhas
        for (int k = 0; k < (int)(r / 1.5f); k++)
            AddPart(PKind.Ember, x + R(-4, 4), y + R(-4, 4), R(-170, 170), -R(60, 280), R(0.6f, 1.5f), 1, k % 2 == 0 ? Yellow : Orange, 240);
        // fogo que fica queimando no chao
        for (int k = 0; k < 2 + (int)(r / 10); k++)
        {
            float px = x + R(-r, r);
            int tx = (int)MathF.Floor(px / K.T);
            for (int ty = (int)((y - r) / K.T); ty <= (int)((y + r) / K.T) + 1; ty++)
                if (Ter.Solid(tx, ty) && !Ter.Solid(tx, ty - 1))
                {
                    AddPart(PKind.Flame, px, ty * K.T, 0, 0, R(1.5f, 3.2f), R(2, 3.5f), Orange);
                    break;
                }
        }
        // marcas de queimado no terreno
        for (int k = 0; k < (int)(r * 2.5f); k++)
        {
            float a = R(0, MathF.Tau), d = R(0, r * 1.15f);
            float px = x + MathF.Cos(a) * d, py = y + MathF.Sin(a) * d;
            if (Ter.SolidAt(px, py)) AddDecal(px, py, A(Ink, R(0.35f, 0.7f)));
        }
    }

    void UpdateExplosions()
    {
        foreach (var e in S.Explosions) e.T += K.DT;
    }

    // ------------------------------------------------------------------ atualizacao das particulas

    void UpdateFx()
    {
        float dt = K.DT;
        int count = parts.Count;
        for (int i = count - 1; i >= 0; i--)
        {
            var p = parts[i];
            p.Life -= dt;
            if (p.Life <= 0) { parts.RemoveAt(i); continue; }
            switch (p.Kind)
            {
                case PKind.Fire:
                    p.VX *= 0.9f; p.VY = p.VY * 0.9f - 110 * dt;
                    p.X += p.VX * dt; p.Y += p.VY * dt;
                    continue;
                case PKind.Smoke:
                    p.VX *= 0.97f; p.VY = p.VY * 0.985f - 8 * dt;
                    p.X += p.VX * dt; p.Y += p.VY * dt;
                    continue;
                case PKind.Flame:
                    if (!Ter.SolidAt(p.X, p.Y + 2)) { p.Life = MathF.Min(p.Life, 0.1f); continue; }
                    if (fx.Next(10) == 0) AddPart(PKind.Smoke, p.X + R(-2, 2), p.Y - 4, R(-6, 6), -R(15, 30), R(0.6f, 1.1f), R(1.5f, 2.5f), A(SmokeDark, 0.6f));
                    if (fx.Next(16) == 0) AddPart(PKind.Ember, p.X, p.Y - 3, R(-20, 20), -R(30, 70), R(0.4f, 0.8f), 1, Yellow, 60);
                    continue;
                case PKind.Blood:
                {
                    p.VY += p.Grav * dt;
                    float nx = p.X + p.VX * dt, ny = p.Y + p.VY * dt;
                    if (Ter.SolidAt(nx, ny))
                    {
                        AddDecal(nx, ny, p.C);
                        if (p.Size > 1) AddDecal(nx + 1, ny, p.C);
                        parts.RemoveAt(i);
                        continue;
                    }
                    p.X = nx; p.Y = ny;
                    continue;
                }
                case PKind.Spark:
                case PKind.Flash:
                case PKind.Ring:
                    p.VY += p.Grav * dt;
                    p.X += p.VX * dt; p.Y += p.VY * dt;
                    continue;
                case PKind.Ember:
                    p.VX *= 0.98f; p.VY += p.Grav * dt;
                    p.X += p.VX * dt; p.Y += p.VY * dt;
                    continue;
            }

            // corpos solidos que quicam: destrocos, pedacos, cartuchos, pixels com gravidade
            if (p.Kind == PKind.BurnDebris)
            {
                if (fx.Next(2) == 0) AddPart(PKind.Smoke, p.X, p.Y, R(-5, 5), -R(5, 15), R(0.4f, 0.7f), R(0.8f, 1.4f), A(Hex(0x7a7070), 0.45f));
                if (fx.Next(3) == 0) AddPart(PKind.Fire, p.X, p.Y, R(-10, 10), -R(5, 20), 0.18f, 1.5f, Orange);
            }
            if (p.Kind == PKind.Gib && MathF.Abs(p.VX) + MathF.Abs(p.VY) > 60 && fx.Next(3) == 0)
                AddPart(PKind.Blood, p.X, p.Y, p.VX * 0.1f, p.VY * 0.1f, 0.8f, 1, p.C2, 540);

            p.VY += p.Grav * dt;
            float bx = p.X + p.VX * dt, by = p.Y + p.VY * dt;
            if (p.Grav > 0 && Ter.SolidAt(bx, by))
            {
                if (!Ter.SolidAt(p.X, by)) { p.VX *= -0.45f; bx = p.X; }
                else
                {
                    if (p.Kind == PKind.Gib && MathF.Abs(p.VY) > 40) AddDecal(bx, by, p.C2);
                    if (p.Kind == PKind.Shell && MathF.Abs(p.VY) > 60 && fx.Next(2) == 0) sfx.Play("clank", 0.12f, 1.6f);
                    p.VY *= -0.35f; p.VX *= 0.6f; by = p.Y;
                }
            }
            p.X = bx; p.Y = by;
        }
        for (int i = corpses.Count - 1; i >= 0; i--)
        {
            var c = corpses[i];
            c.T += dt; c.Life -= dt;
            if (c.Life <= 0 || c.Y > K.ROWS * K.T + 40) { corpses.RemoveAt(i); continue; }
            if (c.Rest) continue;
            c.VY += K.GRAV * dt;
            c.Angle += c.Spin * dt;
            if (c.IsFlyer && fx.Next(3) == 0) AddPart(PKind.Smoke, c.X, c.Y, 0, -10, 0.6f, 2, SmokeDark);
            if (!c.IsFlyer && fx.Next(4) == 0) AddPart(PKind.Blood, c.X, c.Y, R(-20, 20), 0, 0.8f, 1, c.Era != null ? BloodOf(c.Era) : BloodRed, 540);
            float nx = c.X + c.VX * dt, ny = c.Y + c.VY * dt;
            if (Ter.SolidAt(nx, c.Y)) { c.VX *= -0.4f; nx = c.X; }
            if (c.VY > 0 && Ter.SolidAt(nx, ny + 4))
            {
                ny = MathF.Floor((ny + 4) / K.T) * K.T - 4;
                c.Bounces++;
                c.VY *= -0.35f; c.VX *= 0.55f; c.Spin *= 0.5f;
                AddPart(PKind.Smoke, nx, ny + 4, 0, -6, 0.3f, 1.5f, Hex(0xe8dcc8));
                if (!c.IsFlyer)
                    for (int k = 0; k < 3; k++) AddDecal(nx + R(-4, 4), ny + 4 + R(0, 2), c.Era != null ? BloodOf(c.Era) : BloodRed);
                if (c.Bounces >= 2 || MathF.Abs(c.VY) < 40)
                {
                    c.Rest = true;
                    c.Angle = c.Facing * (c.VX >= 0 ? 90 : -90);
                }
            }
            if (c.VY < 0 && Ter.SolidAt(nx, ny - 4)) { c.VY = 0; ny = c.Y; }
            c.X = nx; c.Y = ny;
        }
        for (int i = trails.Count - 1; i >= 0; i--)
        {
            trails[i].Life -= dt;
            if (trails[i].Life <= 0) trails.RemoveAt(i);
        }
        for (int i = texts.Count - 1; i >= 0; i--)
        {
            var t = texts[i];
            t.Life -= dt; t.Y -= 18 * dt;
            if (t.Life <= 0) texts.RemoveAt(i);
        }
    }
}
