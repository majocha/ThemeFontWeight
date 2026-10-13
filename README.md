# Theme Font Weight

A Visual Studio VSIX written entirely in F#. Choose independent **code editor
font weights** for light and dark themes at **Tools > Options > Theme Font
Weight > General**. Settings persist in Visual Studio and changes apply to open
document editors without a restart.

Both settings initially use Normal. Available weights range from Thin to Black.
The extension changes only the editor's base font weight, not the font family,
size, colors, or the Visual Studio UI. Explicit syntax-classification font
overrides (such as bold keywords) remain under Fonts and Colors control. The
selected font must support the desired weight; WPF may choose the nearest face
or synthesize a weight.

Theme selection follows `dotnet/fsharp`'s editor: the brightness of the themed
tool-window background determines light versus dark. This also handles Blue,
custom, and high-contrast themes rather than hard-coding theme IDs.

## Build and install

Requires Windows and the .NET 8 SDK. Targets .NET Framework 4.7.2 and Visual
Studio 2022/2026 (64-bit), including **VS 2026 Insiders**. No C# bridge or XAML
code-behind is used.

```powershell
dotnet build .\src\ThemeFontWeight\ThemeFontWeight.fsproj -c Release
dotnet run --project .\tests\ThemeFontWeight.Tests -c Release
```

Install `src\ThemeFontWeight\bin\Release\net472\ThemeFontWeight.vsix` using the
VSIX installer, then restart Visual Studio. Uninstall through **Extensions >
Manage Extensions** to return to Visual Studio's normal font-weight behavior.
Select your Insiders installation in the installer if you have multiple VS
instances. The options page is a classic `DialogPage`, not a unified-settings
provider.

To locate an Insiders installation from scripts, include `-prerelease` when
calling `vswhere.exe`. To debug without changing your main VS profile, use an
experimental instance (`devenv.exe /RootSuffix Exp`) and install the extension
there rather than in your normal instance.

## Implementation

The structure follows the F# MEF exports and theme-change handlers in
[`dotnet/fsharp`](https://github.com/dotnet/fsharp/tree/main/vsintegration/src/FSharp.Editor).
An F# `Package` supplies a persisted `DialogPage`; an F# MEF
`IWpfTextViewCreationListener` loads it when the first document view opens.
Per-view classification format maps are updated on theme, settings, and format
mapping changes, and event handlers are removed when views close.

Package/options registration lives in a checked-in `.pkgdef`, following the
explicit registration approach used by the F# tools' VSIX. The VS SDK build
tools produce the VSIX; the only bundled runtime dependency is FSharp.Core.
