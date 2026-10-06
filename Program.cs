using Raylib_cs;
using JackassRun;

Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.VSyncHint);
Raylib.InitWindow(1280, 720, "Jackass Run - Time Force Bros");
Raylib.SetWindowMinSize(K.W * 2, K.H * 2);
Raylib.SetExitKey(KeyboardKey.Null);
Raylib.SetTargetFPS(60);

new Game().Run();

Raylib.CloseWindow();
