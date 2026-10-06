using static JackassRun.Col;

namespace JackassRun;

/// <summary>Elementos inspirados no level design do Metal Slug: emboscadas (tropas que entram correndo pela
/// direita da tela e paraquedistas que caem do ceu quando o heroi passa por um ponto).</summary>
public sealed partial class Game
{
    // ------------------------------------------------------------------ emboscadas

    void UpdateAmbushes()
    {
        if (P.Dead) return;
        foreach (var a in S.Ambushes)
        {
            if (a.Fired || P.X < a.Col * K.T) continue;
            a.Fired = true;
            int spawned = 0;
            for (int i = 0; i < a.Count; i++)
            {
                var e = new Enemy { Id = S.NextId++, Kind = a.Enemy, Hp = EnemyHp(a.Enemy),
                    Facing = -1, Alerted = true, FireT = Tune.FirstShotDelay + i * 0.4f };
                if (a.Type == 0)
                {
                    // entra correndo pela direita, ao nivel do chao daquela coluna
                    e.X = S.CamX + ViewW + 10 + i * 16;
                    int row = Ter.Surface((int)MathF.Floor(e.X / K.T));
                    if (row <= 0) continue;
                    e.Y = row * K.T;
                    e.EnterT = 0.9f + i * 0.15f;
                }
                else
                {
                    // paraquedista: cai na frente do heroi, sobre chao firme
                    e.X = P.X + 50 + i * 34;
                    if (Ter.Surface((int)MathF.Floor(e.X / K.T)) <= 0) continue;
                    e.Y = S.CamY - 10 - i * 18;
                    e.Para = true;
                }
                e.BaseY = e.Y;
                S.Enemies.Add(e);
                spawned++;
            }
            if (spawned > 0)
            {
                sfx.Play("alert", 0.8f, 0.8f);
                if (a.Type == 1) AddText(P.X + 40, P.Y - 40, "PARAQUEDISTAS!", Red);
            }
        }
    }

    /// <summary>Movimento de entrada (correndo pela direita ou de paraquedas). Retorna true enquanto entra.</summary>
    bool EnterTick(Enemy e)
    {
        float dt = K.DT;
        if (e.Para)
        {
            e.VY = MathF.Min(e.VY + K.GRAV * dt, 48);
            e.VX = MathF.Sin(e.AnimT * 2.4f) * 10;
            e.OnGround = MoveBody(ref e.X, ref e.Y, ref e.VX, ref e.VY, e.HalfW - 0.5f, e.Height - 2, out _);
            if (e.OnGround) { e.Para = false; e.VX = 0; e.LandT = 0.1f; }
            return true;
        }
        if (e.EnterT <= 0) return false;
        e.EnterT -= dt;
        e.Facing = -1;
        e.VY = MathF.Min(e.VY + K.GRAV * dt, 400);
        e.VX = -Tune.WalkSpeed * 2.4f;
        if (e.OnGround && Ter.SolidAt(e.X - e.HalfW - 2, e.Y - 4)) e.VY = -215;      // pula obstaculos
        e.OnGround = MoveBody(ref e.X, ref e.Y, ref e.VX, ref e.VY, e.HalfW - 0.5f, e.Height - 2, out _);
        return true;
    }

    // ------------------------------------------------------------------ carros destruidos (cidade)

    /// <summary>Carro abandonado: aguenta alguns tiros, pega fogo e explode (explosao grande, em cadeia).
    /// A carcaca queimada (variacao 0) solta fumaca o tempo todo.</summary>
    void UpdateCar(Prop p)
    {
        float vx = 0, x = p.X, y = p.Y, vy = p.VY + K.GRAV * K.DT;
        MoveBody(ref x, ref y, ref vx, ref vy, 12f, 10, out _);
        p.X = x; p.Y = y; p.VY = vy;
        if (p.Y > K.ROWS * K.T + 30) { p.Done = true; return; }
        if (p.Char == 0 && fx.Next(7) == 0)
            AddPart(PKind.Smoke, p.X + fx.Next(-8, 8), p.Y - 10, fx.Next(-6, 10), -fx.Next(14, 30), 1.4f, 3, A(Hex(0x2a2a2e), 0.7f));
        if (p.Fuse < 0) return;
        // pegando fogo
        if (fx.Next(2) == 0) AddPart(PKind.Fire, p.X + fx.Next(-10, 10), p.Y - 9, fx.Next(-10, 10), -fx.Next(20, 50), 0.4f, 2, Orange);
        p.Fuse -= K.DT;
        if (p.Fuse >= 0) return;
        p.Done = true;
        Explode(p.X, p.Y - 6, 44, false, -p.Id);
        var metal = p.Char == 0 ? Hex(0x3a3634) : p.Char == 1 ? Hex(0xd8d6ce) : Hex(0xa83228);
        for (int k = 0; k < 10; k++)
            AddPart(PKind.Debris, p.X + R(-10, 10), p.Y - 8, R(-160, 160), -R(120, 280), 1.6f, fx.Next(2, 4), k % 3 == 0 ? Hex(0x1e1e22) : metal, 540);
        shake = MathF.Max(shake, 5);
    }

    /// <summary>Tiro acertou um carro? Amassa (perde vida) e, sem vida, comeca a pegar fogo.</summary>
    bool CarHit(Bullet b, float bx0, float by0, float bx1, float by1)
    {
        foreach (var p in S.Props)
        {
            if (p.Done || p.Kind != PropKind.Car || p.Fuse >= 0) continue;
            if (!Hit(bx0, by0, bx1, by1, p.X - 13, p.Y - 12, p.X + 13, p.Y)) continue;
            b.Dead = true;
            if (b.ExplodeR > 0) { Explode(b.X, b.Y, b.ExplodeR, b.FromPlayer, b.OwnerId); return true; }
            p.Hp -= Math.Max(1, b.Dmg);
            for (int k = 0; k < 3; k++) AddPart(PKind.Spark, b.X, b.Y, fx.Next(-60, 60), -fx.Next(20, 80), 0.18f, 1, k == 0 ? White : Yellow);
            sfx.Play("clank", 0.4f, 1.2f);
            if (p.Hp <= 0) p.Fuse = 0.9f;
            return true;
        }
        return false;
    }
}
