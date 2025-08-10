# HairCare+ — Known Recurrent Errors & Resolutions

> Purpose: catalog the most common pitfalls encountered during local development so that they are remembered and **never reproduced again**.

---

## Table of Contents
1. Build / MSBuild
2. Networking & Environment Variables
3. Data Synchronisation
4. Platform-specific Quirks

---

## 1. Build / MSBuild

| Error Message | Root Cause | Fix / Prevention |
|---------------|-----------|------------------|
| `CS1514 “{ expected” / CS1022 “type or namespace definition expected”` in **BuildConfig.g.cs** | `GenerateBuildConfig` used `Lines` task; semicolons inside C# code were treated as MSBuild list separators. | Escape `;` as `%3B` in the task or switch to a single-string approach. Already fixed in both client `.csproj`. |
| `MSB4064: "Text" parameter is not supported by "WriteLinesToFile"` | Attempted to use the `Text` parameter which isn’t implemented in `Xamarin.MacDev.Tasks`. | Reverted to `Lines` + escaped semicolons. |
| `XARDF7024: Directory not empty` (Android) | Stale obj/bin folders during incremental build. | Run `dotnet clean` or simply rebuild; the script now auto-cleans before fresh launch. |
| `MSB4018: GenerateDepsFile … IOException: file is being used by another process` | Simultaneous `dotnet build -t:Run` for Clinic & Patient touch the shared **Shared.Domain** DLL in parallel. | Introduced sequential build (Clinic then Patient) **or** use `dotnet clean` before parallel builds. The launcher cleans first – if it re-appears, run builds sequentially. |
| `MT1044: A device must be specified using --devname` (iOS physical) | msbuild/mtouch didn’t receive a device target; wrong property name or missing pairing. Some SDKs expect `_DeviceName` (legacy) rather than `DeviceName`. Also occurs when the device shows state “- Connecting” in Xcode/Device Services. | Pass device by UDID via `-p:_DeviceName=:v2:udid=<UDID>` for `-t:Run`. Ensure iPhone is unlocked, trusted, and visible in `xcrun devicectl list devices` (paired/connected). Open Xcode > Devices and wait until status is Ready. |
| `Please connect the device ':v2:udid=…'` from mlaunch | Device is detected but not fully connected/paired; dev services not ready yet. | Unlock the phone, confirm Trust, keep Xcode Devices window open until Ready. If needed: reconnect cable, `sudo killall -9 usbmuxd`, restart Xcode, or reboot Mac/iPhone. |
| `CSC : warning CS2002: Source file ... specified multiple times` | Leftover generated files in `obj` cause duplicate includes after generator changes. | Run `dotnet clean` or delete `obj/bin` for the affected project before rebuild. |

## 2. Networking & Environment Variables

| Symptom | Root Cause | Fix / Prevention |
|---------|-----------|------------------|
| Clients time-out on `http://192.168.1.6:5281` while server is at `192.168.1.36` | Old `CHAT_BASE_URL` baked into cached build artifacts. | Launcher now recalculates IP each run **and** performs `dotnet clean` before building. Ensure Wi-Fi IP hasn’t changed between build & deploy. |

## 3. Data Synchronisation

| Symptom | Root Cause | Fix / Prevention |
|---------|-----------|------------------|
| Clinic doesn’t show patient PhotoReports | Patient app lost full local file path when capturing photos (only filename stored) → Sync couldn’t upload. | Fixed: `LocalPath` now stores full path; `SyncService` has fallback & `TryUploadAndQueueAsync`. Migrate legacy DB rows if needed. |
| Warn: `PhotoReport file not found` | Legacy rows without valid `LocalPath`. | Write a one-off migration or delete old outbox items. |

## 4. Platform-specific Quirks

| Symptom | Root Cause | Fix / Prevention |
|---------|-----------|------------------|
| MAUI XAML error `XC0009` for `Border` `CornerRadius`  | `Border` doesn’t have `CornerRadius`. | Use `Border.StrokeShape` with `RoundRectangle` as per memory #74783. |
| `Shell.NavBarBackgroundColor` not recognised | Property invalid in MAUI. | Configure nav bar colour in `AppShell.xaml` globally (memory #74757). |

## 6. Performance & Linking (iOS)

| Symptom | Root Cause | Fix / Prevention |
|---------|------------|------------------|
| Debug builds take long time: `Optimizing assemblies for size...` | IL Linker/trimming runs by default for iOS and can slow inner-loop builds. | For local debug runs use `-p:MtouchLink=None -p:PublishTrimmed=false -p:PublishAot=false` to disable linking/AOT. Keep optimizations enabled for Release. |

## 5. Compile-time & XAML Warnings

| Warning Message                                                                                       | Root Cause                                                                                   | Fix / Prevention                                                                                                                                           |
|-------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------|
| `CS8767: Nullability of reference types in type of parameter 'value' ...`                             | Converter methods signatures not updated for nullable reference types                        | Update `Convert` and `ConvertBack` signatures to use `object?` parameters and return `object?`                                                             |
| `CS8602: Dereference of a possibly null reference`                                                   | Unchecked access to `e.Surface.Canvas` in SKCanvasView event handler                         | Guard with `if (e.Surface?.Canvas is not SKCanvas canvas) return;` or use null-conditional operators before use                                             |
| `CS8622: Nullability of reference types in type of parameter 'sender' ...`                           | Event handler signature does not match `EventHandler<T>` delegate nullability                | Annotate parameter as `object? sender` to match `EventHandler<SKPaintSurfaceEventArgs>`                                                                   |
| `XC0045: Binding: Property "<CommandName>" not found on "<Type>"` | Binding in XAML without proper `x:DataType` or `x:DataTypeArg` | Add `x:DataType` on the root view/DataTemplate or `x:DataTypeArg` on the `<Binding>` to enable compile-time binding resolution |
| `XC0022` / `XC0023: Property not found on target` | DataTemplate or view lacking `x:DataType` for compiled bindings | Add `x:DataType` attribute on `<DataTemplate>` or view root to specify binding context type |
| `NU1603: Dependency version mismatch` | Outdated or inconsistent preview package versions | Align `PackageReference` versions for SignalR.Client, EF Core, EF Core.Sqlite across projects |

---

### How to Contribute
Add new issues **immediately** after you diagnose them:
```md
## <Section>
| Error / Symptom | Root Cause | Fix / Prevention |
| … | … | … |
```
👍  Keep this list up to date – future you (and the AI pair-programmer) will thank you! 