using System.Numerics;
using Raylib_cs;
using static JackassRun.Col;
using static JackassRun.Gfx;

namespace JackassRun;

/// <summary>Comboio abandonado na selva: cabos eletricos partidos que soltam faiscas (pendurados no teto dos
/// vagoes e tuneis, ou caindo de postes tortos ao lado da linha).</summary>
public sealed partial class Game
{
    /// <summary>Ponta do cabo (onde saem as faiscas). Char 0 = pendurado no bloco de cima; 1 = poste.</summary>
    Vector2 WireTip(Prop p)
    {
        float sway = MathF.Sin(p.T * 1.7f + p.Id) * 1.5f;
        return p.Char == 0 ? new Vector2(p.X + sway, p.Y - 5) : new Vector2(p.X + 8 + sway * 0.6f, p.Y - 13);
    }

    /// <summary>Faiscas de vez em quando (so efeito: o cabo nao machuca ninguem).</summary>
    void UpdateWire(Prop p)
    {
        if (p.Fuse > 0) p.Fuse -= K.DT;
        if (p.X < S.CamX - 30 || p.X > S.CamX + ViewW + 30) return;
        var tip = WireTip(p);
        if (fx.Next(14) == 0)
            AddPart(PKind.Spark, tip.X, tip.Y, R(-30, 30), -R(0, 40), 0.15f, 1, fx.Next(2) == 0 ? Yellow : White, 300);
        if (fx.Next(75) == 0)
        {
            // estalo: rajada de faiscas que caem quicando, clarao azulado e zumbido
            p.Fuse = 0.14f;
            int n = fx.Next(6, 12);
            for (int k = 0; k < n; k++)
                AddPart(PKind.Spark, tip.X, tip.Y, R(-90, 90), -R(20, 130), R(0.2f, 0.5f), 1, k % 3 == 0 ? Cyan : k % 3 == 1 ? Yellow : White, 500);
            AddPart(PKind.Flash, tip.X, tip.Y, 0, 0, 0.06f, 4, Hex(0xd8f4ff));
            if (fx.Next(3) == 0) AddPart(PKind.Smoke, tip.X, tip.Y, R(-5, 5), -R(5, 15), 0.5f, 1.5f, A(Hex(0x8a8a92), 0.5f));
            if (P.Dead || MathF.Abs(P.X - p.X) < 200) { sfx.Play("clank", 0.22f, 2.6f); sfx.Play("laser", 0.08f, 2.2f); }
        }
    }

    void DrawWire(Prop p, int x, int y)
    {
        var tip = WireTip(p);
        int tx = SX(tip.X), ty = SY(tip.Y);
        var cable = Hex(0x18181c); var cableL = Hex(0x3a3a42);
        if (p.Char == 0)
        {
            // pendurado: desce do teto balancando
            int top = y - K.T;
            for (int i = 0; i <= 10; i++)
            {
                float k = i / 10f;
                int cx = x + (int)MathF.Round((tx - x) * k * k);
                Rect(cx, top + i, 1, 1, cable);
            }
            Rect(x - 1, top, 3, 1, cableL);
        }
        else
        {
            // poste de madeira torto com travessa, isoladores e um cabo partido caindo
            var wood = Hex(0x6a4a2a); var woodD = Hex(0x3e2a18);
            for (int i = 0; i < 44; i++)
            {
                int lean = i / 11;
                Rect(x - 1 + lean, y - i, 3, 1, i % 9 == 0 ? woodD : wood);
                Rect(x + 1 + lean, y - i, 1, 1, woodD);
            }
            int topX = x + 4, topY = y - 42;
            Rect(topX - 7, topY, 15, 2, woodD);
            Rect(topX - 6, topY - 2, 2, 2, Hex(0xd8d4c8)); Rect(topX + 5, topY - 2, 2, 2, Hex(0xd8d4c8));
            // cabo inteiro saindo da tela (para o poste seguinte) e o cabo partido caindo ate a ponta
            for (int i = 0; i < 18; i++) Rect(topX - 6 - i, topY - 2 + (i * i) / 40, 1, 1, cable);
            for (int i = 0; i <= 12; i++)
            {
                float k = i / 12f;
                int cx = topX + 6 + (int)MathF.Round((tx - (topX + 6)) * k);
                int cy = topY - 1 + (int)MathF.Round((ty - topY + 1) * k * k);
                Rect(cx, cy, 1, 1, cable);
            }
        }
        // ponta de cobre desencapada; brilha no estalo
        Rect(tx, ty, 1, 2, Hex(0xd88a3a));
        if (p.Fuse > 0)
        {
            float a = p.Fuse / 0.14f;
            PixelCircle(tx, ty + 1, 7, A(Hex(0x9ad8ff), 0.25f * a));
            PixelCircle(tx, ty + 1, 3, A(White, 0.8f * a));
        }
        else if ((int)(time * 9 + p.Id) % 7 == 0) Rect(tx - 1, ty + 1, 3, 1, A(Yellow, 0.8f));
    }
}
