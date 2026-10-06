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
                var e = new Enemy { Id = S.NextId++, Kind = a.Enemy, Hp = a.Enemy == EnemyKind.Brute ? 8 : a.Enemy == EnemyKind.Rocketeer ? 3 : a.Enemy is EnemyKind.Knife or EnemyKind.Bomber ? 1 : 2,
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
}
