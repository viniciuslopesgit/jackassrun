using System.Runtime.InteropServices;
using Raylib_cs;

namespace JackassRun;

/// <summary>Todos os sons sao sintetizados na inicializacao (nenhum arquivo de audio externo).</summary>
public sealed unsafe class Sfx
{
    const int SR = 22050;
    readonly Dictionary<string, Sound[]> snd = new();
    readonly Dictionary<string, int> rr = new();
    readonly Random rnd = new(7);
    readonly bool ok;
    Sound music;
    public bool MusicOn = true;

    public Sfx()
    {
        Raylib.InitAudioDevice();
        ok = Raylib.IsAudioDeviceReady();
        if (!ok) return;
        Build();
    }

    float N() => (float)rnd.NextDouble() * 2f - 1f;
    static float Sq(float ph) => (ph - MathF.Floor(ph)) < 0.5f ? 1f : -1f;
    static float Saw(float ph) => 2f * (ph - MathF.Floor(ph)) - 1f;
    static float Tri(float ph) { float f = ph - MathF.Floor(ph); return 4f * MathF.Abs(f - 0.5f) - 1f; }

    /// <summary>Gera amostras: f(t, progresso 0..1).</summary>
    float[] Gen(float dur, Func<float, float, float> f)
    {
        int n = (int)(dur * SR);
        var b = new float[n];
        for (int i = 0; i < n; i++) b[i] = f(i / (float)SR, i / (float)n);
        return b;
    }

    /// <summary>Oscilador com frequencia variavel (integra a fase).</summary>
    float[] Sweep(float dur, Func<float, float> freq, Func<float, float> wave, Func<float, float> env, float noise = 0)
    {
        int n = (int)(dur * SR); var b = new float[n]; float ph = 0;
        for (int i = 0; i < n; i++)
        {
            float p = i / (float)n;
            ph += freq(p) / SR;
            b[i] = (wave(ph) * (1 - noise) + N() * noise) * env(p);
        }
        return b;
    }

    static float[] LowPass(float[] b, float a)
    {
        float y = 0;
        for (int i = 0; i < b.Length; i++) { y += a * (b[i] - y); b[i] = y; }
        return b;
    }

    Sound Load(float[] buf, float gain)
    {
        short* d = (short*)NativeMemory.Alloc((nuint)(buf.Length * 2));
        for (int i = 0; i < buf.Length; i++) d[i] = (short)(Math.Clamp(buf[i] * gain, -1f, 1f) * 32000);
        var w = new Wave { FrameCount = (uint)buf.Length, SampleRate = SR, SampleSize = 16, Channels = 1, Data = d };
        var s = Raylib.LoadSoundFromWave(w);
        NativeMemory.Free(d);
        return s;
    }

    void Add(string name, float[] buf, int voices = 1, float gain = 1)
    {
        var baseS = Load(buf, gain);
        var arr = new Sound[voices];
        arr[0] = baseS;
        for (int v = 1; v < voices; v++) arr[v] = Raylib.LoadSoundAlias(baseS);
        snd[name] = arr;
    }

    void Build()
    {
        Add("shoot", Sweep(0.08f, p => 950 - 600 * p, Sq, p => (1 - p) * (1 - p), 0.35f), 6, 0.35f);
        Add("eshoot", Sweep(0.09f, p => 520 - 300 * p, Sq, p => (1 - p), 0.25f), 4, 0.25f);
        Add("shotgun", LowPass(Gen(0.3f, (t, p) => N() * MathF.Exp(-p * 7)), 0.35f), 3, 0.9f);
        Add("laser", Sweep(0.22f, p => 1500 * MathF.Pow(0.15f, p), Saw, p => 1 - p), 3, 0.35f);
        Add("rocket", LowPass(Gen(0.4f, (t, p) => (N() * 0.7f + Sq(t * 90) * 0.3f) * (1 - p)), 0.25f), 3, 0.6f);
        Add("slash", LowPass(Gen(0.12f, (t, p) => N() * MathF.Sin(p * MathF.PI)), 0.6f), 3, 0.6f);
        Add("jump", Sweep(0.12f, p => 280 + 500 * p, Sq, p => 1 - p), 2, 0.22f);
        Add("hit", Gen(0.05f, (t, p) => N() * (1 - p)), 4, 0.35f);
        Add("clank", Sweep(0.05f, p => 2200, Sq, p => 1 - p, 0.3f), 3, 0.18f);
        Add("edie", Sweep(0.2f, p => 400 - 300 * p, Sq, p => 1 - p, 0.5f), 4, 0.35f);
        Add("alert", Gen(0.16f, (t, p) => Sq(t * (p < 0.5f ? 880 : 1320)) * (1 - p) * 0.8f), 3, 0.25f);
        Add("glorb", Sweep(0.14f, p => p < 0.5f ? 1320 : 1760, Tri, p => 1 - p), 3, 0.35f);
        Add("rescue", Sweep(0.6f, p => new[] { 523f, 659f, 784f, 1046f }[Math.Min(3, (int)(p * 4))], Sq, p => 1 - p * 0.6f), 1, 0.25f);
        Add("pdie", Sweep(0.7f, p => new[] { 784f, 659f, 523f, 392f, 262f }[Math.Min(4, (int)(p * 5))], Sq, p => 1 - p), 1, 0.3f);
        Add("rewind", Sweep(1.2f, p => 300 + 220 * MathF.Sin(p * 70) + 400 * (1 - p), Saw, p => 0.8f * (1 - p * 0.5f)), 1, 0.25f);
        Add("select", Sweep(0.06f, p => 660, Sq, p => 1 - p), 3, 0.2f);
        Add("spawn", Sweep(0.45f, p => 200 + 1600 * p * p, Tri, p => 1 - p * 0.7f), 1, 0.35f);
        Add("warp", Sweep(1.2f, p => 150 + 1800 * p, (ph) => Tri(ph) * 0.7f + Sq(ph * 0.5f) * 0.3f, p => MathF.Sin(p * MathF.PI)), 1, 0.3f);
        Add("freeze", Gen(0.8f, (t, p) => (MathF.Sin(t * 6.283f * 880) + MathF.Sin(t * 6.283f * 1320) + MathF.Sin(t * 6.283f * 1760)) / 3f * (1 - p)), 1, 0.3f);
        Add("shield", Sweep(0.3f, p => 600 + 600 * p, Tri, p => 1 - p), 1, 0.3f);
        var boom = Gen(0.9f, (t, p) =>
            N() * MathF.Exp(-p * 4.5f) + MathF.Sin(t * 6.283f * (70 - 40 * p)) * MathF.Exp(-p * 6) * 0.8f);
        Add("boom", LowPass(boom, 0.18f), 5, 1.6f);
        BuildMusic();
    }

    void BuildMusic()
    {
        // Loop chiptune de 4 compassos, 150 BPM, tom de La menor.
        float step = 60f / 150f / 4f;
        int steps = 64;
        int n = (int)(step * steps * SR);
        var b = new float[n];
        float[] bassRoots = { 110f, 110f, 87.31f, 98f };          // A2 A2 F2 G2
        float[] arp = { 1f, 1.189f, 1.498f, 2f };               // tonica, terca menor, quinta, oitava
        int[] lead = { 0, -1, 7, -1, 5, 7, -1, 12, 10, -1, 7, 5, 3, -1, 5, -1 };
        float bph = 0, aph = 0, lph = 0;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR;
            int s = (int)(t / step); if (s >= steps) s = steps - 1;
            float st = t - s * step;              // tempo dentro do passo
            int bar = s / 16, inBar = s % 16;
            float root = bassRoots[bar];
            // baixo pulsante em colcheias com oitava alternada
            float bf = root * ((inBar % 4 == 2) ? 2 : 1);
            bph += bf / SR;
            float bass = Sq(bph) * 0.22f * MathF.Exp(-st * 6);
            // arpejo rapido
            float af = root * 4 * arp[(s * 2 + (st > step / 2 ? 1 : 0)) % 4];
            aph += af / SR;
            float arpS = Sq(aph * 1.0f) * 0.06f;
            // melodia
            int ln = lead[(s + bar * 3) % 16];
            float leadS = 0;
            if (ln >= 0)
            {
                lph += root * 4 * MathF.Pow(2, ln / 12f) / SR;
                leadS = Tri(lph) * 0.13f * MathF.Exp(-st * 3);
            }
            // bateria
            float drum = 0;
            if (inBar % 4 == 0) drum += MathF.Sin(st * 6.283f * (120 - st * 600)) * MathF.Exp(-st * 25) * 0.5f;
            if (inBar % 8 == 4) drum += N() * MathF.Exp(-st * 22) * 0.28f;
            if (inBar % 2 == 1) drum += N() * MathF.Exp(-st * 90) * 0.08f;
            b[i] = bass + arpS + leadS + drum;
        }
        music = Load(b, 0.9f);
        snd["music"] = new[] { music };
    }

    public void Play(string n, float vol = 1, float pitch = 1)
    {
        if (!ok || !snd.TryGetValue(n, out var a)) return;
        int i = rr.GetValueOrDefault(n);
        rr[n] = (i + 1) % a.Length;
        var s = a[i];
        Raylib.SetSoundVolume(s, vol);
        Raylib.SetSoundPitch(s, pitch * (0.94f + (float)rnd.NextDouble() * 0.12f));
        Raylib.PlaySound(s);
    }

    /// <summary>Mantem a trilha tocando; a velocidade/pitch muda durante o rebobinar.</summary>
    public void UpdateMusic(float pitch, float vol)
    {
        if (!ok) return;
        if (!MusicOn) { if (Raylib.IsSoundPlaying(music)) Raylib.StopSound(music); return; }
        Raylib.SetSoundPitch(music, pitch);
        Raylib.SetSoundVolume(music, vol);
        if (!Raylib.IsSoundPlaying(music)) Raylib.PlaySound(music);
    }

    public void Close()
    {
        if (ok) Raylib.CloseAudioDevice();
    }
}
