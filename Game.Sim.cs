using Raylib_cs;
using static JackassRun.Col;

namespace JackassRun;

public sealed partial class Game
{
    void Step(InputState i)
    {
        S.Frame++;
        EnsureChunks();
        UpdateCamera();
        UpdatePlayer(i);
        UpdateGhosts();
        UpdateEnemies();
        UpdateProps();
        UpdateBullets();
        UpdateExplosions();
        Cleanup();
        CheckEra();

        hist.Add(S.Clone());
        if (hist.Count > K.HISTORY) hist.RemoveAt(0);
        Ter.Trim(hist[0].Frame, (int)(hist[0].CamX / K.T) - 80);
    }

    void UpdateCamera()
    {
        // corrida infinita: a camera avanca sozinha e acelera com a distancia
        S.Speed = 34 + MathF.Min(62f, Meters * 0.03f);
        S.CamX += S.Speed * K.DT;
        if (!P.Dead && P.X > S.CamX + K.W * 0.58f) S.CamX = P.X - K.W * 0.58f;
    }

    void CheckEra()
    {
        int e = Eras.IndexForX(S.CamX + K.W * 0.5f);
        if (e != lastEra)
        {
            if (lastEra >= 0)
            {
                var era = Eras.All[e];
                eraBanner = era.Year; eraSub = era.Name; eraBannerT = 3f;
                flash = 0.8f; shake = 3; sfx.Play("warp");
            }
            else { eraBanner = Eras.All[e].Year; eraSub = Eras.All[e].Name; eraBannerT = 2.5f; }
            lastEra = e;
        }
    }

    // ------------------------------------------------------------------ fisica generica

    /// <summary>Move uma caixa (x = centro, y = pes) contra o terreno, eixo por eixo.</summary>
    bool MoveBody(ref float x, ref float y, ref float vx, ref float vy, float hw, float h, out bool hitWall)
    {
        hitWall = false;
        bool onGround = false;
        float dx = vx * K.DT, dy = vy * K.DT;
        int steps = Math.Max(1, (int)MathF.Ceiling(MathF.Max(MathF.Abs(dx), MathF.Abs(dy)) / 3f));
        float sx = dx / steps, sy = dy / steps;
        for (int s = 0; s < steps; s++)
        {
            if (sx != 0)
            {
                x += sx;
                if (Overlaps(x, y, hw, h))
                {
                    x -= sx;
                    // sobe degraus de 1-3px automaticamente
                    bool stepped = false;
                    for (int up = 1; up <= 3 && vy >= 0; up++)
                        if (!Overlaps(x + sx, y - up, hw, h)) { x += sx; y -= up; stepped = true; break; }
                    if (!stepped) { hitWall = true; vx = 0; sx = 0; }
                }
            }
            if (sy != 0)
            {
                y += sy;
                if (Overlaps(x, y, hw, h))
                {
                    if (sy > 0)
                    {
                        y = MathF.Floor(y / K.T) * K.T;
                        if (Overlaps(x, y, hw, h)) y -= sy;
                        onGround = true;
                    }
                    else
                    {
                        y -= sy;
                    }
                    vy = 0; sy = 0;
                }
            }
        }
        if (!onGround && vy >= 0 && Overlaps(x, y + 1, hw, h)) onGround = true;
        return onGround;
    }

    bool Overlaps(float x, float y, float hw, float h)
    {
        int x0 = (int)MathF.Floor((x - hw) / K.T), x1 = (int)MathF.Floor((x + hw - 0.01f) / K.T);
        int y0 = (int)MathF.Floor((y - h) / K.T), y1 = (int)MathF.Floor((y - 0.01f) / K.T);
        for (int tx = x0; tx <= x1; tx++)
            for (int ty = y0; ty <= y1; ty++)
                if (Ter.Solid(tx, ty)) return true;
        return false;
    }

    static bool Hit(float ax0, float ay0, float ax1, float ay1, float bx0, float by0, float bx1, float by1) =>
        ax0 < bx1 && ax1 > bx0 && ay0 < by1 && ay1 > by0;

    // ------------------------------------------------------------------ jogador

    void UpdatePlayer(InputState i)
    {
        var p = P;
        if (p.Dead) return;
        var ch = Chars.All[p.Char];
        float dt = K.DT;
        p.AnimT += dt; p.FireT -= dt; p.InvulnT -= dt; p.MuzzleT -= dt; p.Coyote -= dt;
        p.Recoil = MathF.Max(0, p.Recoil - dt * 30);

        int dir = (i.Right ? 1 : 0) - (i.Left ? 1 : 0);
        bool fired = false, special = false;

        if (p.DashT > 0)
        {
            p.DashT -= dt;
            p.VY = 0;
            if (fx.Next(2) == 0) AddPart(PKind.Pixel, p.X, p.Y - fx.Next(2, 16), 0, 0, 0.25f, 3, A(ch.Look.Accent, 0.8f));
        }
        else
        {
            float accel = p.OnGround ? 1100 : 700;
            float target = dir * ch.Speed;
            p.VX = Approach(p.VX, target, accel * dt);
            if (dir != 0) p.Facing = dir;

            // escalar paredes segurando na direcao delas (como no Broforce)
            p.Climbing = false;
            if (!p.OnGround && dir != 0 && Overlaps(p.X + dir * 2, p.Y - 2, 4, 12))
            {
                p.Climbing = true; p.WallDir = dir;
                p.VY = MathF.Min(p.VY, -70);
                if (fx.Next(6) == 0) AddPart(PKind.Pixel, p.X + dir * 4, p.Y - 4, -dir * 20, 10, 0.3f, 1, Hex(0xc8b8a0));
            }

            if (i.Jump)
            {
                if (p.OnGround || p.Coyote > 0)
                {
                    p.VY = -238; p.OnGround = false; p.Coyote = 0;
                    sfx.Play("jump");
                    for (int k = 0; k < 5; k++) AddPart(PKind.Smoke, p.X, p.Y, fx.Next(-30, 30), -fx.Next(0, 15), 0.35f, 2, Hex(0xe8dcc8));
                }
                else if (p.Climbing)
                {
                    p.VY = -225; p.VX = -p.WallDir * 130; p.Facing = -p.WallDir;
                    sfx.Play("jump");
                }
            }
            if (!i.JumpHeld && p.VY < 0 && !p.Climbing) p.VY += K.GRAV * dt * 1.4f;
            p.VY = MathF.Min(p.VY + K.GRAV * dt, 420);
        }

        // atirar
        if (i.FireHeld && p.FireT <= 0)
        {
            p.FireT = ch.FireRate;
            FireWeapon(p.Char, p.X, p.Y, p.Facing, false);
            p.MuzzleT = 0.05f; p.Recoil = p.Char == 1 || p.Char == 3 ? 3 : 1;
            fired = true;
            if (p.Char == 1 && p.OnGround) p.VX -= p.Facing * 60;
        }
        // especial
        if (i.Special && p.Specials > 0)
        {
            p.Specials--;
            special = true;
            SpecialEffect(p.Char, p.X, p.Y, p.Facing, false);
            if (p.Char == 3) { p.VY = -340; p.OnGround = false; }
            if (p.Char == 4) { p.DashT = 0.16f; p.VX = p.Facing * 430; p.InvulnT = MathF.Max(p.InvulnT, 0.3f); }
        }

        bool wasGround = p.OnGround;
        p.OnGround = MoveBody(ref p.X, ref p.Y, ref p.VX, ref p.VY, 3.5f, 14, out _);
        if (wasGround && !p.OnGround && p.VY >= 0) p.Coyote = 0.08f;
        if (!wasGround && p.OnGround)
            for (int k = 0; k < 3; k++) AddPart(PKind.Smoke, p.X, p.Y, fx.Next(-25, 25), -5, 0.3f, 2, Hex(0xe8dcc8));

        // bordas da tela: a esquerda empurra; se ficar esmagado, morre
        if (p.X - 4 < S.CamX)
        {
            p.X = S.CamX + 4;
            if (p.VX < 0) p.VX = 0;
            if (Overlaps(p.X, p.Y, 3.5f, 14)) { KillPlayer(0, "ESMAGADO PELO TEMPO"); }
        }
        if (p.X > S.CamX + K.W - 6) { p.X = S.CamX + K.W - 6; p.VX = MathF.Min(p.VX, 0); }
        if (p.Y > K.ROWS * K.T + 30) { KillPlayer(0, "CAIU NO VAZIO TEMPORAL"); }

        rec?.Frames.Add(new GFrame
        {
            X = p.X, Y = p.Y, VX = p.VX, Facing = (sbyte)p.Facing, OnGround = p.OnGround, Climbing = p.Climbing,
            Fire = fired, Special = special,
        });
    }

    static float Approach(float v, float t, float d) => v < t ? MathF.Min(v + d, t) : MathF.Max(v - d, t);

    // ------------------------------------------------------------------ fantasmas (vidas passadas)

    void UpdateGhosts()
    {
        foreach (var g in ghosts)
        {
            int idx = S.Frame - g.StartFrame;
            if (idx >= 0 && idx < g.Frames.Count)
            {
                var f = g.Frames[idx];
                if (f.Fire) FireWeapon(g.Char, f.X, f.Y, f.Facing, true);
                if (f.Special) SpecialEffect(g.Char, f.X, f.Y, f.Facing, true);
            }
            else if (idx == g.Frames.Count && !g.Resolved && g.Frames.Count > 0)
            {
                g.Resolved = true;
                var last = g.Frames[^1];
                if (!g.Died)
                {
                    for (int k = 0; k < 12; k++) AddPart(PKind.Pixel, last.X, last.Y - fx.Next(0, 16), fx.Next(-20, 20), -fx.Next(10, 60), 0.5f, 2, Cyan);
                    continue;
                }
                // Salvou seu eu do passado? Se quem o matou ja morreu, ganha um escudo.
                bool killerAlive = g.KillerId != 0 && S.Enemies.Any(e => e.Id == g.KillerId && !e.Dead);
                if (g.KillerId != 0 && !killerAlive && !P.Dead)
                {
                    P.Shield = Math.Min(P.Shield + 1, 3);
                    sfx.Play("shield");
                    AddText(last.X, last.Y - 26, "EU DO PASSADO SALVO!", Cyan);
                    AddText(P.X, P.Y - 26, "+ESCUDO", Cyan);
                    for (int k = 0; k < 16; k++) AddPart(PKind.Spark, last.X, last.Y - 8, fx.Next(-80, 80), fx.Next(-80, 80), 0.4f, 1, Cyan);
                }
                else
                {
                    var look = Chars.All[g.Char].Look;
                    Gibs(last.X, last.Y - 8, A(look.Shirt, 0.6f), A(look.Skin, 0.6f), 14);
                }
            }
        }
    }

    // ------------------------------------------------------------------ armas

    Bullet AddBullet(BulletKind k, float x, float y, float vx, float vy, bool fromPlayer, int owner, bool ghost = false)
    {
        var b = new Bullet
        {
            Id = S.NextId++, Kind = k, X = x, Y = y, VX = vx, VY = vy, FromPlayer = fromPlayer, OwnerId = owner, Ghost = ghost,
        };
        switch (k)
        {
            case BulletKind.Bullet: b.W = 5; b.H = 2; b.Life = 0.7f; b.Dmg = 1; break;
            case BulletKind.Pellet: b.W = 3; b.H = 3; b.Life = 0.22f; b.Dmg = 1; break;
            case BulletKind.Laser: b.W = 14; b.H = 3; b.Life = 0.5f; b.Dmg = 2; b.Pierce = true; break;
            case BulletKind.Rocket: b.W = 6; b.H = 4; b.Life = 1.6f; b.Dmg = 3; b.ExplodeR = 22; break;
            case BulletKind.Grenade: b.W = 4; b.H = 4; b.Life = 1.3f; b.Dmg = 0; b.Grav = 520; b.ExplodeR = 26; break;
            case BulletKind.Dynamite: b.W = 4; b.H = 6; b.Life = 1.1f; b.Dmg = 0; b.Grav = 520; b.ExplodeR = 40; break;
            case BulletKind.Slash: b.W = 22; b.H = 20; b.Life = 0.1f; b.Dmg = 3; b.Pierce = true; break;
            case BulletKind.EBullet: b.W = 4; b.H = 4; b.Life = 2.5f; b.Dmg = 1; break;
            case BulletKind.ERocket: b.W = 6; b.H = 4; b.Life = 3f; b.Dmg = 1; b.ExplodeR = 18; break;
            case BulletKind.EBomb: b.W = 5; b.H = 5; b.Life = 3f; b.Dmg = 1; b.Grav = 300; b.ExplodeR = 18; break;
        }
        S.Bullets.Add(b);
        return b;
    }

    /// <summary>Disparo da arma principal; usado pelo jogador e pelos fantasmas (replay).</summary>
    void FireWeapon(int ch, float x, float y, int f, bool ghost)
    {
        var look = Chars.All[ch].Look;
        float gy = y - 9, mx = x + f * (4 + look.GunLen);
        float vol = ghost ? 0.4f : 1f;
        switch (ch)
        {
            case 0:
                AddBullet(BulletKind.Bullet, mx, gy, f * 360, S.Rng.Range(-14, 14), true, K.PLAYER_ID, ghost);
                sfx.Play("shoot", vol);
                AddPart(PKind.Pixel, x, gy, -f * fx.Next(20, 50), -fx.Next(60, 110), 0.6f, 1, Yellow, 400);
                break;
            case 1:
                for (int k = 0; k < 6; k++)
                {
                    var b = AddBullet(BulletKind.Pellet, mx, gy, f * S.Rng.Range(280, 340), -75 + k * 30 + S.Rng.Range(-10, 10), true, K.PLAYER_ID, ghost);
                    b.Life += S.Rng.Range(0, 0.08f);
                }
                sfx.Play("shotgun", vol);
                if (!ghost) shake = MathF.Max(shake, 2);
                for (int k = 0; k < 4; k++) AddPart(PKind.Smoke, mx, gy, f * fx.Next(10, 50), fx.Next(-15, 15), 0.4f, 2, Hex(0xd0d0d0));
                break;
            case 2:
                AddBullet(BulletKind.Laser, mx + f * 4, gy, f * 560, 0, true, K.PLAYER_ID, ghost);
                sfx.Play("laser", vol);
                break;
            case 3:
                AddBullet(BulletKind.Rocket, mx, gy - 1, f * 90, 0, true, K.PLAYER_ID, ghost);
                sfx.Play("rocket", vol);
                if (!ghost) shake = MathF.Max(shake, 1.5f);
                for (int k = 0; k < 6; k++) AddPart(PKind.Smoke, x - f * 6, gy, -f * fx.Next(20, 70), fx.Next(-20, 10), 0.5f, 3, Hex(0xc8c8c8));
                break;
            case 4:
                AddBullet(BulletKind.Slash, x + f * 11, y - 9, 0, 0, true, K.PLAYER_ID, ghost);
                sfx.Play("slash", vol);
                break;
        }
        if (ch != 4) AddPart(PKind.Flash, mx + f * 2, gy, 0, 0, 0.06f, 4, White);
    }

    void SpecialEffect(int ch, float x, float y, int f, bool ghost)
    {
        float vol = ghost ? 0.5f : 1f;
        switch (ch)
        {
            case 0:
                AddBullet(BulletKind.Grenade, x + f * 4, y - 14, f * 160, -180, true, K.PLAYER_ID, ghost);
                sfx.Play("jump", vol, 0.6f);
                break;
            case 1:
                AddBullet(BulletKind.Dynamite, x + f * 4, y - 14, f * 130, -170, true, K.PLAYER_ID, ghost);
                sfx.Play("jump", vol, 0.5f);
                break;
            case 2:
                S.FreezeT = 3.5f;
                sfx.Play("freeze", vol);
                AddPart(PKind.Ring, x, y - 9, 0, 0, 0.6f, 120, Cyan);
                AddPart(PKind.Ring, x, y - 9, 0, 0, 0.4f, 60, White);
                flash = MathF.Max(flash, 0.3f);
                break;
            case 3:
                Explode(x, y + 2, 20, true, K.PLAYER_ID);
                break;
            case 4:
                var b = AddBullet(BulletKind.Slash, x + f * 34, y - 8, 0, 0, true, K.PLAYER_ID, ghost);
                b.W = 70; b.H = 18; b.Dmg = 4; b.Life = 0.14f;
                sfx.Play("slash", vol, 0.7f);
                for (int k = 0; k < 10; k++) AddPart(PKind.Pixel, x + f * k * 7, y - fx.Next(2, 16), 0, 0, 0.35f, 3, A(Chars.All[4].Look.Accent, 0.7f));
                break;
        }
    }

    // ------------------------------------------------------------------ inimigos

    bool FindTarget(float x, float y, float range, float vrange, out float tx, out float ty)
    {
        tx = ty = 0;
        float bestD = float.MaxValue;
        if (!P.Dead && mode == Mode.Play)
        {
            float dx = P.X - x, dy = (P.Y - 8) - y;
            if (MathF.Abs(dx) < range && MathF.Abs(dy) < vrange) { bestD = MathF.Abs(dx); tx = P.X; ty = P.Y - 8; }
        }
        foreach (var g in ghosts)
        {
            int idx = S.Frame - g.StartFrame;
            if (idx < 0 || idx >= g.Frames.Count) continue;
            var f = g.Frames[idx];
            float dx = f.X - x, dy = (f.Y - 8) - y;
            if (MathF.Abs(dx) < range && MathF.Abs(dy) < vrange && MathF.Abs(dx) < bestD) { bestD = MathF.Abs(dx); tx = f.X; ty = f.Y - 8; }
        }
        return bestD < float.MaxValue;
    }

    void UpdateEnemies()
    {
        float dt = K.DT;
        bool frozen = S.FreezeT > 0;
        if (frozen) S.FreezeT -= dt;
        var era = Eras.ForX(S.CamX + K.W / 2f);

        for (int n = 0; n < S.Enemies.Count; n++)
        {
            var e = S.Enemies[n];
            if (e.Dead || e.X > S.CamX + K.W + 20) continue;
            e.HurtT -= dt; e.MuzzleT -= dt;
            if (frozen) continue;
            e.AnimT += dt;
            float gunY = e.Kind == EnemyKind.Flyer ? e.Y : e.Y - (e.Kind == EnemyKind.Brute ? 11 : 9);

            switch (e.Kind)
            {
                case EnemyKind.Flyer:
                {
                    e.Phase += dt * 3;
                    e.X -= 22 * dt;
                    e.Y = e.BaseY + MathF.Sin(e.Phase) * 10;
                    e.FireT -= dt;
                    if (FindTarget(e.X, e.Y, 18, 160, out float tx, out float ty) && ty > e.Y && e.FireT <= 0)
                    {
                        e.FireT = 1.1f;
                        AddBullet(BulletKind.EBomb, e.X, e.Y + 5, 0, 20, false, e.Id);
                        sfx.Play("eshoot", 0.7f, 0.6f);
                    }
                    e.Facing = FindTarget(e.X, e.Y, 200, 200, out tx, out _) && tx > e.X ? 1 : -1;
                    break;
                }
                case EnemyKind.Turret:
                {
                    if (FindTarget(e.X, e.Y - 6, 190, 50, out float tx, out float ty))
                    {
                        e.Facing = tx < e.X ? -1 : 1;
                        e.FireT -= dt;
                        if (e.FireT <= 0)
                        {
                            EnemyShoot(e, e.X + e.Facing * 10, e.Y - 8, tx, ty, 140, era);
                            e.Burst++;
                            e.FireT = e.Burst % 3 == 0 ? 1.8f : 0.16f;
                        }
                    }
                    break;
                }
                default:
                    UpdateGrunt(e, gunY, era);
                    break;
            }
            if (e.Y > K.ROWS * K.T + 40) e.Dead = true;
        }
    }

    void UpdateGrunt(Enemy e, float gunY, Era era)
    {
        float dt = K.DT;
        float walk = e.Kind == EnemyKind.Brute ? 12 : 20;
        e.VY = MathF.Min(e.VY + K.GRAV * dt, 400);

        if (!e.Alerted)
        {
            if (e.AlertT <= 0 && FindTarget(e.X, gunY, 150, 32, out float tx, out _) &&
                (MathF.Sign(tx - e.X) == e.Facing || MathF.Abs(tx - e.X) < 45))
            {
                e.AlertT = 0.5f; e.Facing = tx < e.X ? -1 : 1;
                sfx.Play("alert", 0.6f);
            }
            if (e.AlertT > 0)
            {
                e.AlertT -= dt; e.VX = 0;
                if (e.AlertT <= 0) { e.Alerted = true; e.FireT = 0.15f; e.AlertT = 0; }
            }
            else
            {
                e.StateT -= dt;
                if (e.StateT <= 0) { e.StateT = S.Rng.Range(0.8f, 2.6f); e.Walking = S.Rng.Chance(0.65f); if (S.Rng.Chance(0.3f)) e.Facing = -e.Facing; }
                e.VX = e.Walking ? e.Facing * walk : 0;
                if (e.OnGround && e.Walking)
                {
                    float ax = e.X + e.Facing * (e.HalfW + 2);
                    bool wall = Ter.SolidAt(ax, e.Y - 4);
                    bool ledge = !Ter.SolidAt(ax, e.Y + 2);
                    if (wall || ledge) { e.Facing = -e.Facing; e.VX = 0; }
                }
            }
        }
        else
        {
            if (FindTarget(e.X, gunY, 210, 48, out float tx, out float ty))
            {
                e.Facing = tx < e.X ? -1 : 1;
                e.VX = 0;
                if (e.Kind == EnemyKind.Brute && MathF.Abs(tx - e.X) > 60 && e.OnGround && !Ter.SolidAt(e.X + e.Facing * 8, e.Y - 4) && Ter.SolidAt(e.X + e.Facing * 8, e.Y + 2))
                    e.VX = e.Facing * walk;
                e.FireT -= dt;
                if (e.FireT <= 0)
                {
                    float mx = e.X + e.Facing * 11;
                    switch (e.Kind)
                    {
                        case EnemyKind.Soldier:
                            EnemyShoot(e, mx, gunY, tx, ty, 130, era);
                            e.Burst++;
                            e.FireT = e.Burst % 3 == 0 ? S.Rng.Range(1.3f, 2.2f) : 0.15f;
                            break;
                        case EnemyKind.Rocketeer:
                            AddBullet(BulletKind.ERocket, mx, gunY - 1, e.Facing * 60, 0, false, e.Id);
                            sfx.Play("rocket", 0.6f, 1.2f);
                            e.MuzzleT = 0.08f;
                            e.FireT = S.Rng.Range(2.2f, 3f);
                            break;
                        case EnemyKind.Brute:
                            for (int k = -1; k <= 1; k++)
                            {
                                var b = AddBullet(BulletKind.EBullet, mx, gunY, e.Facing * 120, k * 32, false, e.Id);
                            }
                            sfx.Play("eshoot", 0.9f, 0.7f);
                            e.MuzzleT = 0.08f;
                            e.FireT = S.Rng.Range(1.4f, 1.9f);
                            break;
                    }
                }
            }
            else
            {
                e.Alerted = false; e.StateT = 0;
            }
        }
        e.OnGround = MoveBody(ref e.X, ref e.Y, ref e.VX, ref e.VY, e.HalfW - 0.5f, e.Height - 2, out _);
    }

    void EnemyShoot(Enemy e, float x, float y, float tx, float ty, float speed, Era era)
    {
        float dx = tx - x, dy = ty - y;
        float d = MathF.Max(1, MathF.Sqrt(dx * dx + dy * dy));
        float vy = Math.Clamp(dy / d * speed, -45, 45);
        AddBullet(BulletKind.EBullet, x, y, MathF.Sign(dx) * speed, vy, false, e.Id);
        e.MuzzleT = 0.07f;
        sfx.Play("eshoot", 0.8f);
        AddPart(PKind.Flash, x, y, 0, 0, 0.06f, 3, Yellow);
    }

    void HitEnemy(Enemy e, int dmg, float kx)
    {
        if (e.Dead) return;
        e.Hp -= dmg;
        e.HurtT = 0.08f;
        if (!e.Alerted && e.Kind != EnemyKind.Flyer) { e.Alerted = true; e.FireT = 0.4f; }
        if (e.Kind != EnemyKind.Turret && e.Kind != EnemyKind.Flyer) e.X += kx;
        sfx.Play("hit", 0.5f);
        for (int k = 0; k < 3; k++) AddPart(PKind.Pixel, e.X, e.Y - 8, fx.Next(-60, 60), -fx.Next(20, 90), 0.4f, 1, Hex(0xd82a3a), 400);
        if (e.Hp <= 0) KillEnemy(e);
    }

    void KillEnemy(Enemy e)
    {
        e.Dead = true;
        int pts = e.Kind switch { EnemyKind.Brute => 300, EnemyKind.Turret => 250, EnemyKind.Flyer => 150, EnemyKind.Rocketeer => 150, _ => 100 };
        S.Score += pts; S.Kills++;
        var era = Eras.ForX(e.X);
        sfx.Play("edie");
        float cy = e.Kind == EnemyKind.Flyer ? e.Y : e.Y - 8;
        if (e.Kind is EnemyKind.Turret or EnemyKind.Flyer && era.Style is BgStyle.Future or BgStyle.Jungle)
            Explode(e.X, cy, 14, true, K.PLAYER_ID);
        else
            Gibs(e.X, cy, era.EShirt, era.ESkin, e.Kind == EnemyKind.Brute ? 26 : 16);
        AddText(e.X, cy - 14, "+" + pts, Yellow);
        shake = MathF.Max(shake, 2);
        if (e.Kind == EnemyKind.Brute) hitStop = 0.05f;
    }

    // ------------------------------------------------------------------ props

    void UpdateProps()
    {
        float dt = K.DT;
        foreach (var p in S.Props)
        {
            if (p.Done) continue;
            p.T += dt;
            switch (p.Kind)
            {
                case PropKind.Barrel:
                    if (p.Fuse >= 0)
                    {
                        p.Fuse -= dt;
                        if (p.Fuse < 0) { p.Done = true; Explode(p.X, p.Y - 5, 30, false, -p.Id); }
                    }
                    else
                    {
                        float vx = 0;
                        float x = p.X, y = p.Y, vy = p.VY + K.GRAV * dt;
                        MoveBody(ref x, ref y, ref vx, ref vy, 3.5f, 9, out _);
                        p.X = x; p.Y = y; p.VY = vy;
                        if (p.Y > K.ROWS * K.T + 30) p.Done = true;
                    }
                    break;
                case PropKind.Cage:
                    if (!P.Dead && Hit(P.X - 4, P.Y - 14, P.X + 4, P.Y, p.X - 9, p.Y - 24, p.X + 9, p.Y))
                    {
                        p.Done = true;
                        timeOuts++;
                        S.Score += 500;
                        sfx.Play("rescue");
                        AddText(p.X, p.Y - 34, "RESGATADO! +1 TIME OUT", Green);
                        for (int k = 0; k < 20; k++) AddPart(PKind.Pixel, p.X, p.Y - 12, fx.Next(-90, 90), -fx.Next(40, 160), 0.8f, 2, k % 2 == 0 ? Hex(0x9a9aa8) : Yellow, 400);
                        flash = 0.25f;
                    }
                    break;
                case PropKind.Glorb:
                    if (!P.Dead && MathF.Abs(P.X - p.X) < 9 && MathF.Abs(P.Y - 8 - p.Y) < 12)
                    {
                        p.Done = true;
                        S.Score += 50;
                        glorbCount++;
                        sfx.Play("glorb");
                        AddPart(PKind.Ring, p.X, p.Y, 0, 0, 0.25f, 10, Cyan);
                        if (glorbCount >= 12)
                        {
                            glorbCount = 0; P.Specials = Math.Min(P.Specials + 1, 9);
                            AddText(P.X, P.Y - 26, "+1 ESPECIAL", Magenta);
                        }
                    }
                    break;
            }
        }
    }

    // ------------------------------------------------------------------ projeteis

    void UpdateBullets()
    {
        float dt = K.DT;
        bool frozen = S.FreezeT > 0;
        int count = S.Bullets.Count;
        for (int n = 0; n < count; n++)
        {
            var b = S.Bullets[n];
            if (b.Dead) continue;
            if (frozen && !b.FromPlayer) continue;   // balas inimigas paradas no tempo
            b.T += dt;
            b.Life -= dt;
            if (b.Life <= 0)
            {
                b.Dead = true;
                if (b.ExplodeR > 0) Explode(b.X, b.Y, b.ExplodeR, b.FromPlayer, b.OwnerId);
                continue;
            }
            if (b.Kind == BulletKind.Rocket) b.VX = MathF.Sign(b.VX) * MathF.Min(MathF.Abs(b.VX) + 500 * dt, 320);
            if (b.Kind == BulletKind.ERocket) b.VX = MathF.Sign(b.VX) * MathF.Min(MathF.Abs(b.VX) + 120 * dt, 170);
            if (b.Kind is BulletKind.Rocket or BulletKind.ERocket && fx.Next(2) == 0)
                AddPart(PKind.Smoke, b.X - MathF.Sign(b.VX) * 4, b.Y, fx.Next(-10, 10), fx.Next(-10, 5), 0.45f, 2, Hex(0xbcbcbc));
            if (b.Kind == BulletKind.Dynamite && fx.Next(2) == 0)
                AddPart(PKind.Spark, b.X, b.Y - 4, fx.Next(-30, 30), -fx.Next(20, 60), 0.2f, 1, Yellow);
            b.VY += b.Grav * dt;

            if (b.Kind == BulletKind.Slash) { SlashHits(b); continue; }

            float mdx = b.VX * dt, mdy = b.VY * dt;
            int steps = Math.Max(1, (int)MathF.Ceiling(MathF.Max(MathF.Abs(mdx), MathF.Abs(mdy)) / 3f));
            for (int s = 0; s < steps && !b.Dead; s++)
            {
                float ox = b.X, oy = b.Y;
                b.X += mdx / steps; b.Y += mdy / steps;
                int tx = (int)MathF.Floor(b.X / K.T), ty = (int)MathF.Floor(b.Y / K.T);
                if (Ter.Solid(tx, ty))
                {
                    if (b.Kind is BulletKind.Grenade or BulletKind.Dynamite)
                    {
                        // quica
                        bool hx = Ter.SolidAt(b.X, oy), hy = Ter.SolidAt(ox, b.Y);
                        b.X = ox; b.Y = oy;
                        if (hy || !hx) { b.VY *= -0.4f; b.VX *= 0.7f; }
                        if (hx) b.VX *= -0.5f;
                        mdx = mdy = 0;
                        continue;
                    }
                    if (b.FromPlayer)
                    {
                        if (b.ExplodeR > 0) { b.Dead = true; Explode(b.X, b.Y, b.ExplodeR, true, b.OwnerId); break; }
                        DamageTile(tx, ty, b.Dmg);
                        // o laser atravessa terreno (menos aco)
                        if (b.Kind == BulletKind.Laser && Ter.Type(tx, ty) != Terrain.STEEL) continue;
                        b.Dead = true;
                        break;
                    }
                    b.Dead = true;
                    if (b.ExplodeR > 0) Explode(b.X, b.Y, b.ExplodeR, false, b.OwnerId);
                    else AddPart(PKind.Spark, b.X, b.Y, fx.Next(-40, 40), -fx.Next(10, 50), 0.15f, 1, Orange);
                    break;
                }
                BulletEntityHits(b);
            }
            if (b.X < S.CamX - 40 || b.X > S.CamX + K.W + 60 || b.Y > K.ROWS * K.T + 20 || b.Y < -120) b.Dead = true;
        }
    }

    void BulletEntityHits(Bullet b)
    {
        float bx0 = b.X - b.W / 2, by0 = b.Y - b.H / 2, bx1 = b.X + b.W / 2, by1 = b.Y + b.H / 2;
        if (b.FromPlayer)
        {
            foreach (var e in S.Enemies)
            {
                if (e.Dead || e.Id == b.LastHit || e.X > S.CamX + K.W + 20) continue;
                var (x0, y0, x1, y1) = e.Box;
                if (!Hit(bx0, by0, bx1, by1, x0, y0, x1, y1)) continue;
                if (b.ExplodeR > 0) { b.Dead = true; Explode(b.X, b.Y, b.ExplodeR, true, b.OwnerId); return; }
                HitEnemy(e, b.Dmg, MathF.Sign(b.VX) * 1.5f);
                AddPart(PKind.Flash, b.X, b.Y, 0, 0, 0.05f, 3, White);
                if (b.Pierce) b.LastHit = e.Id; else { b.Dead = true; return; }
            }
            foreach (var p in S.Props)
            {
                if (p.Done || p.Kind != PropKind.Barrel || p.Fuse >= 0) continue;
                if (Hit(bx0, by0, bx1, by1, p.X - 4, p.Y - 10, p.X + 4, p.Y))
                {
                    p.Fuse = 0.08f;
                    if (b.ExplodeR > 0) { b.Dead = true; Explode(b.X, b.Y, b.ExplodeR, true, b.OwnerId); return; }
                    if (!b.Pierce) { b.Dead = true; return; }
                }
            }
        }
        else
        {
            if (!P.Dead && mode == Mode.Play && Hit(bx0, by0, bx1, by1, P.X - 3, P.Y - 13, P.X + 3, P.Y))
            {
                if (P.InvulnT > 0 || P.DashT > 0) return;
                b.Dead = true;
                if (b.ExplodeR > 0) Explode(b.X, b.Y, b.ExplodeR, false, b.OwnerId);
                KillPlayer(b.OwnerId, b.Kind == BulletKind.EBullet ? "BALEADO" : "EXPLODIDO");
                return;
            }
            foreach (var p in S.Props)
            {
                if (p.Done || p.Kind != PropKind.Barrel || p.Fuse >= 0) continue;
                if (Hit(bx0, by0, bx1, by1, p.X - 4, p.Y - 10, p.X + 4, p.Y)) { p.Fuse = 0.08f; b.Dead = true; return; }
            }
        }
    }

    void SlashHits(Bullet b)
    {
        float bx0 = b.X - b.W / 2, by0 = b.Y - b.H / 2, bx1 = b.X + b.W / 2, by1 = b.Y + b.H / 2;
        foreach (var e in S.Enemies)
        {
            if (e.Dead || e.X > S.CamX + K.W + 20) continue;
            var (x0, y0, x1, y1) = e.Box;
            if (Hit(bx0, by0, bx1, by1, x0, y0, x1, y1) && e.HurtT <= 0) HitEnemy(e, b.Dmg, 0);
        }
        // a katana rebate balas inimigas
        foreach (var o in S.Bullets)
        {
            if (o.Dead || o.FromPlayer) continue;
            if (Hit(bx0, by0, bx1, by1, o.X - 2, o.Y - 2, o.X + 2, o.Y + 2))
            {
                o.FromPlayer = true; o.VX = -o.VX * 1.6f; o.VY = -o.VY; o.OwnerId = K.PLAYER_ID; o.Life = 1.2f; o.Grav = 0;
                sfx.Play("clank");
                AddPart(PKind.Spark, o.X, o.Y, fx.Next(-60, 60), fx.Next(-60, 60), 0.2f, 1, White);
            }
        }
        // corta tiles na frente
        int tx0 = (int)MathF.Floor(bx0 / K.T), tx1 = (int)MathF.Floor(bx1 / K.T);
        int ty0 = (int)MathF.Floor(by0 / K.T), ty1 = (int)MathF.Floor((by1 - 4) / K.T);
        if (b.T <= K.DT * 1.5f)
            for (int tx = tx0; tx <= tx1; tx++) for (int ty = ty0; ty <= ty1; ty++)
                if (Ter.Solid(tx, ty)) DamageTile(tx, ty, 1);
    }

    /// <summary>Danifica um tile. Retorna true se ele foi destruido.</summary>
    bool DamageTile(int tx, int ty, int dmg)
    {
        byte b = Ter.Get(tx, ty);
        int type = Terrain.TypeOf(b);
        if (type == 0) return true;
        float cx = tx * K.T + 4, cy = ty * K.T + 4;
        if (type == Terrain.STEEL)
        {
            sfx.Play("clank", 0.5f);
            AddPart(PKind.Spark, cx, cy, fx.Next(-60, 60), -fx.Next(20, 70), 0.15f, 1, Yellow);
            return false;
        }
        int d = Terrain.DmgOf(b) + dmg;
        var era = Eras.ForCol(tx);
        var c = TileColor(type, era);
        if (d >= Terrain.HpOf(type))
        {
            Ter.Set(S.Frame, tx, ty, 0);
            for (int k = 0; k < 4; k++) AddPart(PKind.Debris, cx + fx.Next(-3, 4), cy + fx.Next(-3, 4), fx.Next(-70, 70), -fx.Next(40, 140), 0.9f, fx.Next(1, 3), c, 500);
            AddPart(PKind.Smoke, cx, cy, 0, -10, 0.4f, 3, A(Col.Mul(c, 1.3f), 0.7f));
            return true;
        }
        Ter.Set(S.Frame, tx, ty, Terrain.Make(type, d));
        AddPart(PKind.Debris, cx, cy, fx.Next(-40, 40), -fx.Next(20, 80), 0.5f, 1, c, 500);
        return false;
    }

    static Color TileColor(int type, Era e) => type switch
    {
        Terrain.DIRT => e.Dirt, Terrain.BRICK => e.Brick, Terrain.STEEL => e.Steel, _ => Hex(0xa8743a),
    };

    // ------------------------------------------------------------------ explosoes

    void Explode(float x, float y, float r, bool fromPlayer, int owner)
    {
        S.Explosions.Add(new Explosion { X = x, Y = y, R = r, FromPlayer = fromPlayer, Dur = 0.35f + r / 80f });
        sfx.Play("boom", MathF.Min(1, r / 30f));
        shake = MathF.Max(shake, r / 6f);
        flash = MathF.Max(flash, r / 120f);

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
                HitEnemy(e, 6, 0);
                if (!e.Dead && e.Kind != EnemyKind.Turret && e.Kind != EnemyKind.Flyer) { e.VY = -150; e.X += MathF.Sign(ex - x) * 4; }
            }
        }
        foreach (var p in S.Props)
            if (!p.Done && p.Kind == PropKind.Barrel && p.Fuse < 0 && MathF.Abs(p.X - x) < r + 4 && MathF.Abs(p.Y - 5 - y) < r + 4)
                p.Fuse = 0.12f;
        // explosoes de inimigos e barris machucam o jogador (como no Broforce)
        if (!fromPlayer && !P.Dead)
        {
            float dx = P.X - x, dy = P.Y - 7 - y;
            if (dx * dx + dy * dy < r * r * 0.75f) KillPlayer(owner > 0 ? owner : 0, "EXPLODIDO");
        }
        // particulas
        for (int k = 0; k < (int)(r * 0.8f); k++)
        {
            float a = (float)(fx.NextDouble() * Math.PI * 2), sp = fx.Next(30, (int)(r * 6));
            AddPart(PKind.Spark, x, y, MathF.Cos(a) * sp, MathF.Sin(a) * sp, 0.25f + (float)fx.NextDouble() * 0.2f, 1, k % 3 == 0 ? White : Yellow);
        }
        for (int k = 0; k < (int)(r / 3); k++)
            AddPart(PKind.Smoke, x + fx.Next(-(int)r / 2, (int)r / 2), y + fx.Next(-(int)r / 2, (int)r / 2), fx.Next(-20, 20), -fx.Next(10, 40), 0.9f + (float)fx.NextDouble() * 0.6f, r / 5f, Hex(0x4a3a3a));
        AddPart(PKind.Ring, x, y, 0, 0, 0.3f, r * 1.3f, White);
    }

    void UpdateExplosions()
    {
        foreach (var e in S.Explosions) e.T += K.DT;
    }

    void Cleanup()
    {
        float left = S.CamX - 80;
        S.Enemies.RemoveAll(e => e.Dead || e.X < left);
        S.Bullets.RemoveAll(b => b.Dead);
        S.Props.RemoveAll(p => p.Done || p.X < left);
        S.Explosions.RemoveAll(e => e.T > e.Dur);
    }

    // ------------------------------------------------------------------ particulas / texto

    void AddPart(PKind k, float x, float y, float vx, float vy, float life, float size, Color c, float grav = 0)
    {
        if (parts.Count > 900) return;
        parts.Add(new Particle { Kind = k, X = x, Y = y, VX = vx, VY = vy, Life = life, Max = life, Size = size, C = c, Grav = grav });
    }

    void AddText(float x, float y, string s, Color c) => texts.Add(new FloatText { X = x, Y = y, Text = s, C = c, Life = 1.2f });

    void Gibs(float x, float y, Color a, Color b, int n)
    {
        for (int k = 0; k < n; k++)
        {
            var c = k % 4 == 0 ? Hex(0xb81e2e) : k % 2 == 0 ? a : b;
            AddPart(PKind.Debris, x + fx.Next(-3, 4), y + fx.Next(-4, 4), fx.Next(-110, 110), -fx.Next(60, 220), 1.4f, fx.Next(1, 4), c, 520);
        }
        AddPart(PKind.Flash, x, y, 0, 0, 0.08f, 8, White);
    }

    void UpdateFx()
    {
        float dt = K.DT;
        for (int i = parts.Count - 1; i >= 0; i--)
        {
            var p = parts[i];
            p.Life -= dt;
            if (p.Life <= 0) { parts.RemoveAt(i); continue; }
            p.VY += p.Grav * dt;
            float nx = p.X + p.VX * dt, ny = p.Y + p.VY * dt;
            if (p.Kind == PKind.Debris || p.Grav > 0)
            {
                if (Ter.SolidAt(nx, ny))
                {
                    if (!Ter.SolidAt(p.X, ny)) { p.VX *= -0.5f; nx = p.X; }
                    else { p.VY *= -0.35f; p.VX *= 0.6f; ny = p.Y; }
                }
            }
            if (p.Kind == PKind.Smoke) { p.VX *= 0.95f; p.VY -= 12 * dt; }
            p.X = nx; p.Y = ny;
        }
        for (int i = texts.Count - 1; i >= 0; i--)
        {
            var t = texts[i];
            t.Life -= dt; t.Y -= 18 * dt;
            if (t.Life <= 0) texts.RemoveAt(i);
        }
    }
}
