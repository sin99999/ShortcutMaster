using System.Runtime.InteropServices;
using ShortcutMaster.Core.Parsing;

namespace ShortcutMaster.Interop;

public sealed record SendOutcome(bool Success, string Message)
{
    public static readonly SendOutcome Ok = new(true, "");
}

/// <summary>
/// ショートカットを前面アプリへ SendInput で送信する。
/// 同時実行は直列化し、各ステップで前面ウィンドウを再確認する。
/// </summary>
public sealed class KeyInjector : IDisposable
{
    private static readonly KeyDef[] PhysicalModifiers =
    {
        new(0xA0, false),
        new(0xA1, false),
        new(0xA2, false),
        new(0xA3, true),
        new(0xA4, false),
        new(0xA5, true),
        new(0x5B, true),
        new(0x5C, true),
    };

    private const int StepDelayMs = 130;
    private readonly SemaphoreSlim _sendGate = new(1, 1);
    private int _inFlight;

    public bool IsBusy => Volatile.Read(ref _inFlight) > 0;

    public async Task<SendOutcome> SendAsync(ForegroundInfo target, IReadOnlyList<ChordStep> steps)
    {
        await _sendGate.WaitAsync().ConfigureAwait(false);
        Interlocked.Increment(ref _inFlight);
        try
        {
            if (!NativeMethods.IsWindow(target.Hwnd))
                return new SendOutcome(false, "対象のウィンドウが見つかりません。");

            if (NativeMethods.GetForegroundWindow() != target.Hwnd)
                return new SendOutcome(false, "対象のアプリが切り替わったため中止しました。");

            if (ImeHelper.IsImeComposing(target.Hwnd))
                return new SendOutcome(false, "日本語入力の変換中は送信できません。確定してからお試しください。");

            NativeMethods.GetWindowThreadProcessId(target.Hwnd, out var pid);
            if (!ElevationChecker.IsCurrentProcessElevated() && ElevationChecker.IsProcessElevated(pid) == true)
                return new SendOutcome(false, "管理者権限で動作中のアプリには送信できません。");

            return await Task.Run(() => DoSend(target.Hwnd, steps)).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Decrement(ref _inFlight);
            _sendGate.Release();
        }
    }

    private static SendOutcome DoSend(IntPtr targetHwnd, IReadOnlyList<ChordStep> steps)
    {
        if (NativeMethods.GetForegroundWindow() != targetHwnd)
            return new SendOutcome(false, "対象のアプリが切り替わったため中止しました。");

        ReleaseStuckModifiers(steps);
        Thread.Sleep(30);

        if (ImeHelper.IsImeComposing(targetHwnd))
            return new SendOutcome(false, "日本語入力の変換中は送信できません。確定してからお試しください。");

        for (var i = 0; i < steps.Count; i++)
        {
            if (NativeMethods.GetForegroundWindow() != targetHwnd)
                return new SendOutcome(false, "送信中にアプリが切り替わったため中止しました。");

            if (ImeHelper.IsImeComposing(targetHwnd))
                return new SendOutcome(false, "日本語入力の変換中は送信できません。確定してからお試しください。");

            if (i > 0) Thread.Sleep(StepDelayMs);

            var inputs = BuildInputs(steps[i]);
            var sent = NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeMethods.INPUT>());
            if (sent != inputs.Length)
                return new SendOutcome(false, "送信に失敗しました。対象アプリの権限をご確認ください。");
        }

        return SendOutcome.Ok;
    }

    private static void ReleaseStuckModifiers(IReadOnlyList<ChordStep> steps)
    {
        var releaseKeys = new HashSet<ushort>();
        foreach (var step in steps)
        {
            if (step.IsModifierTap) continue;
            foreach (var mod in step.Modifiers)
                releaseKeys.Add(mod.VirtualKey);
        }

        if (releaseKeys.Count == 0) return;

        var ups = new List<NativeMethods.INPUT>();
        foreach (var mod in PhysicalModifiers)
        {
            if (!releaseKeys.Contains(mod.VirtualKey)) continue;
            if ((NativeMethods.GetAsyncKeyState(mod.VirtualKey) & 0x8000) != 0)
                ups.Add(MakeInput(mod, keyUp: true));
        }

        if (ups.Count > 0)
            NativeMethods.SendInput((uint)ups.Count, ups.ToArray(), Marshal.SizeOf<NativeMethods.INPUT>());
    }

    private static NativeMethods.INPUT[] BuildInputs(ChordStep step)
    {
        var inputs = new List<NativeMethods.INPUT>();

        if (step.IsModifierTap)
        {
            inputs.Add(MakeInput(step.Key, keyUp: false));
            inputs.Add(MakeInput(step.Key, keyUp: true));
            return inputs.ToArray();
        }

        foreach (var mod in step.Modifiers)
            inputs.Add(MakeInput(mod, keyUp: false));

        inputs.Add(MakeInput(step.Key, keyUp: false));
        inputs.Add(MakeInput(step.Key, keyUp: true));

        for (var i = step.Modifiers.Count - 1; i >= 0; i--)
            inputs.Add(MakeInput(step.Modifiers[i], keyUp: true));

        return inputs.ToArray();
    }

    private static NativeMethods.INPUT MakeInput(KeyDef key, bool keyUp)
    {
        var flags = 0u;
        if (key.IsExtended) flags |= NativeMethods.KEYEVENTF_EXTENDEDKEY;
        if (keyUp) flags |= NativeMethods.KEYEVENTF_KEYUP;

        return new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_KEYBOARD,
            U = new NativeMethods.InputUnion
            {
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = key.VirtualKey,
                    wScan = (ushort)NativeMethods.MapVirtualKey(key.VirtualKey, NativeMethods.MAPVK_VK_TO_VSC),
                    dwFlags = flags,
                },
            },
        };
    }

    public void Dispose() => _sendGate.Dispose();
}
