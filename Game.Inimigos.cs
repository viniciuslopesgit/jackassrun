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
        float t = Math.Clamp(MathF.Abs(dx) / 140f, 0.5f, 1f);
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
        bool air = NavAir(e);
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
                // corre ate o heroi: pula obstaculos, salta buracos e desce de beiradas
                Chase(e, tx, ty + 10, Tune.DogSpeed);
                if (MathF.Abs(tx - e.X) < 40 && MathF.Abs(ty - (e.Y - 6)) < 28 && e.AimT <= 0)
                {
                    e.VY = -130; e.VX = e.Facing * 160; e.AimT = 0.9f;        // bote baixo, na altura do heroi
                    sfx.Play("alert", 0.8f, 1.1f);
                }
            }
        }
        else e.Alerted = false;

        if (air && e.AimT <= 0.5f) e.VX = e.NavVX;
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

    // ------------------------------------------------------------------ navegacao: perseguir o heroi pelo terreno

    /// <summary>Quantos blocos o inimigo sobe pulando (os pesados sobem 1 a menos).</summary>
    static int MaxClimb(Enemy e) => e.Kind is EnemyKind.Brute or EnemyKind.Shield
        ? Math.Max(1, GameConfig.InimigosSobemBlocos - 1) : Math.Max(1, GameConfig.InimigosSobemBlocos);

    /// <summary>Escala paredes de qualquer altura (como o heroi).</summary>
    static bool Climber(Enemy e) => e.Kind == EnemyKind.Knife;

    bool FreeRows(int col, int r0, int r1)
    {
        for (int r = r0; r <= r1; r++) if (Ter.Solid(col, r)) return false;
        return true;
    }

    /// <summary>Quantos blocos ate o chao descendo pela coluna (0 = chao no mesmo nivel), ou -1 se for um buraco
    /// mais fundo que GameConfig.InimigosDescemBlocos (o inimigo nao pula).</summary>
    int DropDepth(int col, int floorRow)
    {
        for (int d = 0; d <= GameConfig.InimigosDescemBlocos; d++)
        {
            int r = floorRow + d;
            if (r >= K.ROWS) break;
            if (Ter.Solid(col, r) || Ter.Platform(col, r)) return d;
        }
        return -1;
    }

    void NavJump(Enemy e, float vy, float vx, bool climb = false)
    {
        e.VY = vy; e.VX = vx; e.NavVX = vx; e.NavT = 0.8f; e.NavClimb = climb; e.OnGround = false;
    }

    /// <summary>No ar depois de um pulo de navegacao: continua empurrando para o lado (para pousar em cima do
    /// obstaculo) e, se estiver escalando, sobe pela parede. Retorna true enquanto dura.</summary>
    bool NavAir(Enemy e)
    {
        if (e.NavT <= 0) return false;
        if (e.OnGround && e.VY >= 0) { e.NavT = 0; e.NavClimb = false; return false; }
        e.NavT -= K.DT;
        if (e.NavClimb && Ter.SolidAt(e.X + MathF.Sign(e.NavVX) * (e.HalfW + 1), e.Y - 4))
        {
            e.VY = MathF.Min(e.VY, -Tune.EnemyClimb);
            e.NavT = MathF.Max(e.NavT, 0.15f);
            if (fx.Next(6) == 0) AddPart(PKind.Pixel, e.X + MathF.Sign(e.NavVX) * 4, e.Y - 4, 0, 10, 0.3f, 1, Hex(0xc8b8a0));
        }
        return true;
    }

    /// <summary>Persegue o alvo pelo terreno: pula caixas, degraus e muros baixos (ate MaxClimb blocos), salta
    /// buracos pequenos, desce de beiradas para chegar ao heroi e (a faca) escala paredes. (tx, ty) = pes do alvo.
    /// Retorna false se nao ha como continuar (o inimigo fica parado).</summary>
    bool Chase(Enemy e, float tx, float ty, float speed)
    {
        if (!e.OnGround) return true;
        if (MathF.Abs(tx - e.X) < 6) { e.VX = 0; return true; }       // logo abaixo/acima do alvo: espera
        int dir = tx < e.X ? -1 : 1;
        Face(e, tx);
        if (e.Facing != dir) { e.VX = 0; return true; }               // escudeiro ainda virando
        float ax = e.X + dir * (e.HalfW + 2);
        int col = (int)MathF.Floor(ax / K.T), here = (int)MathF.Floor(e.X / K.T);
        int floor = (int)MathF.Floor((e.Y + 1) / K.T);               // linha do bloco em que o inimigo pisa
        bool below = ty > e.Y + 8;

        // obstaculo na frente: mede a altura e pula (ou escala) se der para pousar em cima
        if (Ter.Solid(col, floor - 1))
        {
            int h = 0;
            while (h < 8 && Ter.Solid(col, floor - 1 - h)) h++;
            bool landing = FreeRows(col, floor - 2 - h, floor - 1 - h);
            bool head = FreeRows(here, floor - 2 - h, floor - 2);
            if (landing && head && h <= MaxClimb(e))
            {
                NavJump(e, -MathF.Sqrt(2 * K.GRAV * (h * K.T + 8)), dir * MathF.Max(speed, 45));
                return true;
            }
            if (landing && head && Climber(e))
            {
                NavJump(e, -Tune.EnemyClimb, dir * speed, true);
                return true;
            }
            e.VX = 0;
            return false;
        }

        // beirada na frente
        if (!Ter.StandAt(ax, e.Y + 2))
        {
            int drop = DropDepth(col, floor);
            if (below && drop > 0) { e.VX = dir * speed; return true; }        // o heroi esta embaixo: desce
            // vao: salta se houver onde pousar logo adiante (mesmo nivel, 1 acima ou ate 2 abaixo)
            if (FreeRows(here, floor - 3, floor - 1))
                for (int k = 1; k <= 3; k++)
                {
                    int c2 = col + dir * k;
                    for (int dh = 0; dh >= -1; dh--)
                        foreach (int lr in dh == 0 ? new[] { floor, floor + 1, floor + 2 } : new[] { floor - 1 })
                        {
                            if (!(Ter.Solid(c2, lr) || Ter.Platform(c2, lr)) || !FreeRows(c2, lr - 2, lr - 1)) continue;
                            // impulso so o necessario: sobe ate um pouco acima do ponto de pouso e cai nele
                            float rise = MathF.Max(14, (floor - lr) * K.T + 14);
                            float fall = rise - (floor - lr) * K.T;
                            float airT = MathF.Sqrt(2 * rise / K.GRAV) + MathF.Sqrt(2 * fall / K.GRAV);
                            NavJump(e, -MathF.Sqrt(2 * K.GRAV * rise), dir * MathF.Max(speed, (k + 1) * K.T / airT + 8));
                            return true;
                        }
                }
            if (drop > 0 && drop <= MaxClimb(e)) { e.VX = dir * speed; return true; }   // degrau para baixo
            e.VX = 0;
            return false;
        }
        e.VX = dir * speed;
        return true;
    }

    /// <summary>Quadro da folha do cao: 0 parado, 1-4 correndo, 5 salto.</summary>
    static int DogFrame(Enemy e) =>
        !e.OnGround ? 5 : MathF.Abs(e.VX) > 1 ? 1 + (int)(e.AnimT * (MathF.Abs(e.VX) > 60 ? 16 : 9)) % 4 : 0;
}
