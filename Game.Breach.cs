using Raylib_cs;
using static JackassRun.Col;

namespace JackassRun;

/// <summary>Elementos inspirados em Door Kickers: Action Squad: portas que se chutam (atordoando quem esta
/// atras), pisao que atordoa, salas escuras, refens que nao podem ser atingidos, inimigos com faca e homens-bomba.</summary>
public sealed partial class Game
{
    const float StunTime = 2.4f;

    // ------------------------------------------------------------------ portas e pisao

    /// <summary>Correndo contra uma porta, o heroi a arromba com um chute.</summary>
    void TryKick(Player p, int dir)
    {
        if (dir == 0) return;
        int tx = (int)MathF.Floor((p.X + dir * 5.5f) / K.T);
        foreach (int dy in new[] { 4, 12 })
        {
            int ty = (int)MathF.Floor((p.Y - dy) / K.T);
            if (Ter.Type(tx, ty) == Terrain.DOOR) { KickDoor(tx, ty, dir); return; }
        }
    }

    void KickDoor(int tx, int ty, int dir)
    {
        int top = ty, bot = ty;
        while (Ter.Type(tx, top - 1) == Terrain.DOOR) top--;
        while (Ter.Type(tx, bot + 1) == Terrain.DOOR) bot++;
        var wood = Hex(0x9a6432);
        for (int y = top; y <= bot; y++)
        {
            Ter.Set(S.Frame, tx, y, 0);
            for (int k = 0; k < 6; k++)
                AddPart(PKind.Debris, tx * K.T + R(2, 14), y * K.T + R(2, 14), dir * R(80, 220), -R(40, 160), 1.3f, fx.Next(2, 4), k % 2 == 0 ? wood : Mul(wood, 0.7f), 540);
            AddPart(PKind.Smoke, tx * K.T + K.HT, y * K.T + K.HT, dir * R(10, 30), -R(5, 15), 0.6f, 3, A(Hex(0xe0d0b8), 0.8f));
        }
        sfx.Play("boom", 0.45f, 1.6f);
        sfx.Play("hit", 0.8f, 0.7f);
        shake = MathF.Max(shake, 2.5f);
        float cx = tx * K.T + K.HT, floorY = (bot + 1) * K.T;
        AddText(cx, top * K.T - 6, "BAM!", Yellow);
        S.Score += 50;
        // atordoa quem estava logo atras da porta
        foreach (var e in S.Enemies)
        {
            if (e.Dead || e.Kind is EnemyKind.Flyer or EnemyKind.Turret) continue;
            bool behind = MathF.Sign(e.X - cx) == dir || MathF.Abs(e.X - cx) < 10;
            if (behind && MathF.Abs(e.X - cx) < 64 && MathF.Abs(e.Y - floorY) < 20)
            {
                Stun(e);
                e.X += dir * 4;
            }
        }
        for (int y = top; y <= bot; y++) RevealNear(tx, y);
    }

    void Stun(Enemy e)
    {
        e.StunT = StunTime;
        e.AlertT = 0;
        e.FuseT = -1;
        e.HurtT = 0.12f;
        for (int k = 0; k < 6; k++) AddPart(PKind.Spark, e.X, e.Y - 20, R(-50, 50), -R(20, 60), 0.3f, 1, Yellow);
    }

    /// <summary>Cair em cima de um inimigo: atordoa e o heroi quica (como descer do teto no Door Kickers).</summary>
    void TryStomp(Player p, float preVY)
    {
        if (preVY < 60) return;
        foreach (var e in S.Enemies)
        {
            if (e.Dead || e.StunT > 0 || e.Kind is EnemyKind.Flyer or EnemyKind.Turret) continue;
            float top = e.Y - e.Height;
            if (MathF.Abs(p.X - e.X) < e.HalfW + 3 && p.Y >= top - 3 && p.Y <= top + 8)
            {
                Stun(e);
                p.Y = top - 1; p.VY = -190;
                AddText(e.X, top - 10, "POW!", Yellow);
                sfx.Play("hit", 0.9f, 0.8f);
                HitEnemy(e, 1, 0);
                return;
            }
        }
    }

    /// <summary>Retorna true (e cuida da fisica) se o inimigo estiver atordoado.</summary>
    bool StunTick(Enemy e)
    {
        if (e.StunT <= 0) return false;
        float dt = K.DT;
        e.StunT -= dt;
        e.VX = 0;
        e.VY = MathF.Min(e.VY + K.GRAV * dt, 400);
        e.OnGround = MoveBody(ref e.X, ref e.Y, ref e.VX, ref e.VY, e.HalfW - 0.5f, e.Height - 2, out _);
        if (e.StunT <= 0) { e.Alerted = true; e.FireT = Tune.FirstShotDelay; }
        return true;
    }

    // ------------------------------------------------------------------ salas escuras

    bool InHiddenRoom(float x, float y)
    {
        int tx = (int)MathF.Floor(x / K.T), ty = (int)MathF.Floor(y / K.T);
        foreach (var r in S.Rooms) if (!r.Revealed && r.Contains(tx, ty)) return true;
        return false;
    }

    void RevealNear(int tx, int ty)
    {
        foreach (var r in S.Rooms) if (!r.Revealed && r.Contains(tx, ty, 1)) r.Revealed = true;
    }

    void UpdateRooms()
    {
        int ptx = (int)MathF.Floor(P.X / K.T), pty = (int)MathF.Floor((P.Y - 6) / K.T);
        foreach (var r in S.Rooms)
        {
            if (!r.Revealed && !P.Dead && r.Contains(ptx, pty)) r.Revealed = true;
            if (r.Revealed && r.Fade > 0) r.Fade = MathF.Max(0, r.Fade - K.DT * 3);
        }
        S.Rooms.RemoveAll(r => (r.X1 + 1) * K.T < S.CamX - 80);
    }

    // ------------------------------------------------------------------ inimigos que avancam (faca e homem-bomba)

    void UpdateCharger(Enemy e)
    {
        if (StunTick(e)) return;
        float dt = K.DT;
        e.VY = MathF.Min(e.VY + K.GRAV * dt, 400);
        bool knife = e.Kind == EnemyKind.Knife;

        if (e.FuseT >= 0)
        {
            // pavio aceso: para e explode (machuca todo mundo por perto, inclusive o heroi)
            e.FuseT -= dt; e.VX = 0;
            if (e.FuseT < 0) { e.Dead = true; Explode(e.X, e.Y - 8, 32, false, e.Id); return; }
        }
        else if (!InHiddenRoom(e.X, e.Y - 8) && OnScreen(e) &&
                 FindTarget(e.X, e.Y - 8, e.Alerted ? 220 : Tune.SeeRange, 40, out float tx, out float ty))
        {
            e.Facing = tx < e.X ? -1 : 1;
            if (!e.Alerted)
            {
                if (e.AlertT <= 0) { e.AlertT = Tune.AlertTime * 0.7f; sfx.Play("alert", 0.6f); }
                e.AlertT -= dt; e.VX = 0;
                if (e.AlertT <= 0) { e.Alerted = true; e.AlertT = 0; }
            }
            else
            {
                e.VX = e.Facing * (knife ? 72 : 42);
                // pula obstaculos de 1 bloco
                if (e.OnGround && Ter.SolidAt(e.X + e.Facing * (e.HalfW + 2), e.Y - 4)) e.VY = -215;
                if (!knife && MathF.Abs(tx - e.X) < 26 && MathF.Abs(ty - (e.Y - 8)) < 22)
                {
                    e.FuseT = 0.6f;
                    sfx.Play("alert", 1f, 1.4f);
                    AddText(e.X, e.Y - 30, "!!!", Red);
                }
            }
        }
        else
        {
            e.Alerted = false; e.AlertT = 0;
            e.StateT -= dt;
            if (e.StateT <= 0) { e.StateT = S.Rng.Range(1f, 2.5f); e.Walking = S.Rng.Chance(0.5f); if (S.Rng.Chance(0.3f)) e.Facing = -e.Facing; }
            e.VX = e.Walking ? e.Facing * 16 : 0;
            if (e.OnGround && e.Walking && (Ter.SolidAt(e.X + e.Facing * (e.HalfW + 2), e.Y - 4) || !Ter.SolidAt(e.X + e.Facing * (e.HalfW + 2), e.Y + 2)))
            { e.Facing = -e.Facing; e.VX = 0; }
        }

        bool was = e.OnGround;
        e.LandT -= dt;
        e.OnGround = MoveBody(ref e.X, ref e.Y, ref e.VX, ref e.VY, e.HalfW - 0.5f, e.Height - 2, out _);
        if (!was && e.OnGround) e.LandT = 0.1f;

        // a faca mata no contato
        if (knife && !P.Dead && mode == Mode.Play)
        {
            var (x0, y0, x1, y1) = e.Box;
            if (Hit(x0, y0, x1, y1, P.X - 3.5f, P.Y - PH, P.X + 3.5f, P.Y)) KillPlayer(e.Id, "ESFAQUEADO");
        }
        if (e.Y > K.ROWS * K.T + 40) e.Dead = true;
    }

    // ------------------------------------------------------------------ refens

    void UpdateHostage(Prop p)
    {
        float vx = p.Hp == 2 ? -58 : 0, x = p.X, y = p.Y, vy = p.VY + K.GRAV * K.DT;
        MoveBody(ref x, ref y, ref vx, ref vy, 3.5f, 14, out bool wall);
        if (wall && p.Hp == 2 && vy == 0) vy = -200;                // pula obstaculos ao fugir
        p.X = x; p.Y = y; p.VY = vy;
        if (p.Hp == 1 && !P.Dead && Hit(P.X - 4, P.Y - PH, P.X + 4, P.Y, p.X - 5, p.Y - 14, p.X + 5, p.Y))
        {
            p.Hp = 2;
            S.Score += 300;
            glorbCount += 4;
            if (glorbCount >= 12) { glorbCount -= 12; P.Specials = Math.Min(P.Specials + 1, 9); AddText(P.X, P.Y - 34, "+1 ESPECIAL", Magenta); }
            sfx.Play("rescue");
            AddText(p.X, p.Y - 26, "REFEM SALVO! +300", Green);
        }
        if (p.Hp == 2 && p.X < S.CamX - 30) p.Done = true;
        if (p.Y > K.ROWS * K.T + 30) p.Done = true;
    }

    /// <summary>Refem atingido. Se foi o jogador, perde pontos (como perder as estrelas no Door Kickers).</summary>
    void HitHostage(Prop p, bool byPlayer)
    {
        if (p.Done) return;
        p.Done = true;
        BloodSpray(p.X, p.Y - 9, 0, 18, BloodRed, 1.1f);
        AddCorpse(p.X, p.Y + Gfx.Pivot, Art.Hostage, 1, fx.Next(2) == 0 ? -1 : 1, false, null);
        sfx.Play("edie", 0.8f, 1.2f);
        if (byPlayer)
        {
            S.Score = Math.Max(0, S.Score - 500);
            AddText(p.X, p.Y - 28, "REFEM ATINGIDO! -500", Red);
            flash = MathF.Max(flash, 0.25f);
        }
    }
}
