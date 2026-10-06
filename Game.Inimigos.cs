using Raylib_cs;
using static JackassRun.Col;

namespace JackassRun;

/// <summary>Inimigos novos, cada um pede uma tatica diferente:
///  - ESCUDEIRO: o escudo para tiros pela frente; demora a virar, entao pule por cima e atire pelas costas
///    (ou use explosao, pisao, facada, o batarangue na volta).
///  - GRANADEIRO: joga granadas em arco que passam por cima das coberturas e acertam outras alturas.
///  - ATIRADOR DE ELITE: fica parado num posto alto; a mira laser vermelha segue o heroi e trava antes do
///    tiro (muito rapido, em qualquer direcao). Saia da linha de tiro ou quebre a linha de visao.
///  - LANCA-CHAMAS: anda ate perto e solta um jato de fogo curto; o tanque nas costas explode quando ele morre.
///  - CAO: rosna, corre muito e salta no heroi (a mordida mata). Pouca vida.</summary>
public sealed partial class Game
{
    /// <summary>Vida de cada tipo de inimigo.</summary>
    static int EnemyHp(EnemyKind k) => k switch
    {
        EnemyKind.Brute => 8, EnemyKind.Turret => 6, EnemyKind.Flyer => 2, EnemyKind.Rocketeer => 3,
        EnemyKind.Knife => 1, EnemyKind.Bomber => 1, EnemyKind.Shield => 3, EnemyKind.Grenadier => 2,
        EnemyKind.Sniper => 2, EnemyKind.Flamer => 3, EnemyKind.Dog => 1, _ => 2,
    };

    /// <summary>Pontos por abate (dobram se o inimigo estiver atordoado).</summary>
    static int EnemyPoints(EnemyKind k) => k switch
    {
        EnemyKind.Brute => 300, EnemyKind.Turret => 250, EnemyKind.Flyer => 150, EnemyKind.Rocketeer => 150,
        EnemyKind.Knife => 150, EnemyKind.Bomber => 200, EnemyKind.Shield => 200, EnemyKind.Grenadier => 150,
        EnemyKind.Sniper => 200, EnemyKind.Flamer => 200, EnemyKind.Dog => 100, _ => 100,
    };

    /// <summary>Vira o inimigo para o alvo. O escudeiro demora a virar (da tempo de pegar ele pelas costas).</summary>
    void Face(Enemy e, float tx)
    {
        int want = tx < e.X ? -1 : 1;
        if (e.Kind != EnemyKind.Shield || want == e.Facing) { e.Facing = want; e.TurnT = 0; return; }
        e.TurnT += K.DT;
        if (e.TurnT >= Tune.ShieldTurn) { e.Facing = want; e.TurnT = 0; }
    }

    /// <summary>O escudo do escudeiro para tiros que chegam pela frente (explosoes passam).</summary>
    static bool ShieldBlocks(Enemy e, float vx) =>
        e.Kind == EnemyKind.Shield && !e.Dead && e.StunT <= 0 && vx != 0 && MathF.Sign(vx) == -e.Facing;

    void ShieldSpark(Enemy e, float y)
    {
        sfx.Play("clank", 0.6f, 1.3f);
        for (int k = 0; k < 4; k++)
            AddPart(PKind.Spark, e.X + e.Facing * 5, y, e.Facing * R(30, 110), R(-70, 20), 0.2f, 1, k % 2 == 0 ? White : Yellow);
        if (!e.Alerted) { e.Alerted = true; e.FireT = 0.5f; }
    }

    /// <summary>Linha de visao livre de terreno solido (passos de 4px).</summary>
    bool LineOfSight(float x0, float y0, float x1, float y1)
    {
        float dx = x1 - x0, dy = y1 - y0, d = MathF.Sqrt(dx * dx + dy * dy);
        int n = (int)(d / 4);
        for (int i = 2; i < n; i++)
        {
            float k = i / (float)n;
            if (Ter.SolidAt(x0 + dx * k, y0 + dy * k)) return false;
        }
        return true;
    }

    // ------------------------------------------------------------------ granadeiro

    /// <summary>Joga a granada num arco que cai perto do alvo (tx, ty = pes do alvo).</summary>
    void ThrowGrenade(Enemy e, float tx, float ty)
    {
        float x = e.X + e.Facing * 4, y = e.Y - 17;
        float dx = tx - x, dy = ty - 4 - y;
        float t = Math.Clamp(MathF.Abs(dx) / 120f, 0.5f, 1.1f);
        float vx = Math.Clamp(dx / t, -170, 170);
        float vy = Math.Clamp((dy - 0.5f * Tune.GrenadeGrav * t * t) / t, -330, 60);
        var b = AddBullet(BulletKind.EGrenade, x, y, vx, vy, false, e.Id);
        b.Life = t + Tune.GrenadeFuse;
        sfx.Play("slash", 0.6f, 0.55f);
    }

    // ------------------------------------------------------------------ lanca-chamas

    /// <summary>Solta o jato de fogo (chamado a cada quadro enquanto dura o jato).</summary>
    void EmitFlame(Enemy e)
    {
        if (S.Frame % 2 != 0) return;
        float mx = e.X + e.Facing * 9, my = e.Y - 8;
        AddBullet(BulletKind.EFlame, mx, my, e.Facing * S.Rng.Range(125, 155), S.Rng.Range(-16, 10), false, e.Id);
        if (S.Frame % 10 == 0) sfx.Play("rocket", 0.35f, 1.7f);
    }

    /// <summary>Fogo encostando num bloco: madeira (caixa, ponte, porta, telhado) as vezes queima.</summary>
    void BurnTile(int tx, int ty)
    {
        int t = Ter.Type(tx, ty);
        if (t is Terrain.CRATE or Terrain.BRIDGE or Terrain.DOOR or Terrain.ROOF && S.Rng.Chance(0.12f)) DamageTile(tx, ty, 1);
        if (fx.Next(3) == 0) AddPart(PKind.Smoke, tx * K.T + K.HT, ty * K.T + 4, R(-8, 8), -R(10, 25), 0.5f, 2, A(Hex(0x3a3236), 0.7f));
    }

    // ------------------------------------------------------------------ atirador de elite

    void UpdateSniper(Enemy e)
    {
        float dt = K.DT;
        if (StunTick(e)) { e.AimT = MathF.Min(e.AimT, 0); return; }
        if (EnterTick(e)) return;
        if (LadderTick(e)) return;
        e.VY = MathF.Min(e.VY + K.GRAV * dt, 400);
        e.VX = 0;
        if (e.AimT < 0) e.AimT = MathF.Min(0, e.AimT + dt);          // descansando depois do tiro
        bool hidden = InHiddenRoom(e.X, e.Y - 8);
        float my = e.Y - 8;
        float tx = 0, ty = 0;
        bool sees = !hidden && OnScreen(e) && FindTarget(e.X, my, Tune.SniperRange, Tune.SniperVRange, out tx, out ty)
                    && LineOfSight(e.X, my, tx, ty);
        if (sees)
        {
            bool locked = e.AimT > Tune.SniperAim - Tune.SniperLock;
            if (!locked) Face(e, tx);
            if (!e.Alerted) e.Alerted = true;
            if (e.AimT == 0)
            {
                e.AimT = 0.0001f; e.AimX = tx; e.AimY = ty;
                sfx.Play("alert", 0.5f, 1.7f);
            }
            if (e.AimT > 0)
            {
                e.AimT += dt;
                // a mira segue o alvo com atraso e trava nos ultimos instantes: da para escapar se mexendo
                if (!locked)
                {
                    float k = 1 - MathF.Exp(-Tune.SniperFollow * dt);
                    e.AimX += (tx - e.AimX) * k; e.AimY += (ty - e.AimY) * k;
                }
                if (e.AimT >= Tune.SniperAim && S.ShotCD <= 0)
                {
                    float mx = e.X + e.Facing * 11;
                    float dx = e.AimX - mx, dy = e.AimY - my, d = MathF.Max(1, MathF.Sqrt(dx * dx + dy * dy));
                    AddBullet(BulletKind.EBullet, mx, my, dx / d * Tune.SniperSpeed, dy / d * Tune.SniperSpeed, false, e.Id);
                    S.ShotCD = Tune.ShotGap;
                    e.AimT = -Tune.SniperRest;
                    e.MuzzleT = 0.1f;
                    sfx.Play("shotgun", 0.7f, 1.5f);
                    AddPart(PKind.Shell, e.X, my, -e.Facing * fx.Next(30, 60), -fx.Next(70, 120), 1.2f, 1, Hex(0xe0b040), 500);
                }
            }
        }
        else
        {
            if (e.AimT > 0) e.AimT = 0;                                 // perdeu o alvo: solta a mira
            e.StateT -= dt;
            if (e.StateT <= 0) { e.StateT = S.Rng.Range(1.5f, 3.5f); if (S.Rng.Chance(0.4f)) e.Facing = -e.Facing; }
        }
        bool was = e.OnGround;
        e.LandT -= dt;
        e.OnGround = MoveBody(ref e.X, ref e.Y, ref e.VX, ref e.VY, e.HalfW - 0.5f, e.Height - 2, out _);
        if (!was && e.OnGround) e.LandT = 0.1f;
    }

    /// <summary>Ponto onde termina a mira laser (bate no terreno ou vai ate o alcance maximo).</summary>
    (float x, float y) LaserEnd(Enemy e)
    {
        float x0 = e.X + e.Facing * 11, y0 = e.Y - 8;
        float dx = e.AimX - x0, dy = e.AimY - y0, d = MathF.Max(1, MathF.Sqrt(dx * dx + dy * dy));
        dx /= d; dy /= d;
        for (float s = 4; s < Tune.SniperRange + 60; s += 2)
        {
            float x = x0 + dx * s, y = y0 + dy * s;
            if (Ter.SolidAt(x, y)) return (x, y);
        }
        return (x0 + dx * (Tune.SniperRange + 60), y0 + dy * (Tune.SniperRange + 60));
    }

    // ------------------------------------------------------------------ cao de ataque

    void UpdateDog(Enemy e)
    {
        float dt = K.DT;
        if (StunTick(e)) return;
        if (EnterTick(e)) return;
        e.VY = MathF.Min(e.VY + K.GRAV * dt, 400);
        if (e.AimT > 0) e.AimT -= dt;                                  // recarga do salto
        bool hidden = InHiddenRoom(e.X, e.Y - 6);
        if (!e.Alerted)
        {
            if (e.AlertT <= 0 && !hidden && OnScreen(e) && FindTarget(e.X, e.Y - 6, Tune.DogSee, 32, out float tx, out _) &&
                (MathF.Sign(tx - e.X) == e.Facing || MathF.Abs(tx - e.X) < 40))
            {
                e.AlertT = Tune.DogGrowl; e.Facing = tx < e.X ? -1 : 1;
                sfx.Play("alert", 0.5f, 0.55f);
            }
            if (e.AlertT > 0)
            {
                e.AlertT -= dt; e.VX = 0;
                if (e.AlertT <= 0) { e.Alerted = true; e.AlertT = 0; sfx.Play("alert", 0.7f, 0.8f); }
            }
            else
            {
                e.StateT -= dt;
                if (e.StateT <= 0) { e.StateT = S.Rng.Range(1f, 2.5f); e.Walking = S.Rng.Chance(0.6f); if (S.Rng.Chance(0.3f)) e.Facing = -e.Facing; }
                e.VX = e.Walking ? e.Facing * 24 : 0;
                if (e.OnGround && e.Walking && (Ter.SolidAt(e.X + e.Facing * (e.HalfW + 2), e.Y - 4) || !Ter.StandAt(e.X + e.Facing * (e.HalfW + 2), e.Y + 2)))
                { e.Facing = -e.Facing; e.VX = 0; }
            }
        }
        else if (!hidden && FindTarget(e.X, e.Y - 6, 260, 80, out float tx, out float ty))
        {
            if (e.OnGround)
            {
                e.Facing = tx < e.X ? -1 : 1;
                e.VX = e.Facing * Tune.DogSpeed;
                float ax = e.X + e.Facing * (e.HalfW + 2);
                int col = (int)MathF.Floor(ax / K.T);
                if (Ter.SolidAt(ax, e.Y - 4)) e.VY = -215;                  // pula obstaculos
                else if (!Ter.StandAt(ax, e.Y + 2))
                {
                    // beirada: salta o buraco se houver chao logo adiante, senao espera
                    bool land = false;
                    for (int k = 1; k <= 3 && !land; k++) land = Ter.Surface(col + e.Facing * k) >= 0;
                    if (land) { e.VY = -200; e.VX = e.Facing * 150; }
                    else e.VX = 0;
                }
                if (MathF.Abs(tx - e.X) < 46 && MathF.Abs(ty - (e.Y - 6)) < 28 && e.AimT <= 0)
                {
                    e.VY = -170; e.VX = e.Facing * 190; e.AimT = 0.9f;        // bote!
                    sfx.Play("alert", 0.8f, 1.1f);
                }
            }
        }
        else e.Alerted = false;

        bool was = e.OnGround;
        e.LandT -= dt;
        e.OnGround = MoveBody(ref e.X, ref e.Y, ref e.VX, ref e.VY, e.HalfW - 0.5f, e.Height - 2, out _);
        if (!was && e.OnGround) { e.LandT = 0.1f; if (e.Alerted) e.VX = e.Facing * Tune.DogSpeed; }

        // a mordida mata no contato
        if (!P.Dead && mode == Mode.Play)
        {
            var (x0, y0, x1, y1) = e.Box;
            if (Hit(x0, y0, x1, y1, P.X - 3.5f, P.Y - PH, P.X + 3.5f, P.Y)) KillPlayer(e.Id, "MORDIDO");
        }
        if (e.Y > K.ROWS * K.T + 40) e.Dead = true;
    }

    /// <summary>Quadro da folha do cao: 0 parado, 1-4 correndo, 5 salto.</summary>
    static int DogFrame(Enemy e) =>
        !e.OnGround ? 5 : MathF.Abs(e.VX) > 1 ? 1 + (int)(e.AnimT * (MathF.Abs(e.VX) > 60 ? 16 : 9)) % 4 : 0;
}
