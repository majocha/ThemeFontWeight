# Theme Font Weight

A Visual Studio VSIX written entirely in F#. Choose independent **code editor
font weights** for light and dark themes in the modern **Tools > Options**
window. Search for **Theme Font Weight**, or browse **Environment > Theme Font
Weight** under All Settings. Both controls are native Unified Settings dropdowns,
not a classic property-grid options page. Settings persist in Visual Studio and
changes apply to open document editors without a restart.

Both settings initially use Normal. Available weights range from Thin to Black.
The selected weight is a **relative shift** from Normal (400), applied to the
original editor and classification weights. For example, Light (300) changes
regular text from 400 to 300 and bold text from 700 to 600. Thin (100) changes
regular text to 100 and bold text to 400. Custom classification weights shift
by the same amount, clamped to 100-900; distinctions may converge at those limits.
Normal restores the original weights.

Font family, size, colors, italic styles, underlines, and other classification
formatting remain unchanged. Updates use original formatting baselines, not
previously adjusted weights, so changing settings repeatedly cannot compound
the shift. The
selected font must support the desired weight; WPF may choose the nearest face
or synthesize a weight.

The brightness of the themed tool-window background determines light versus
dark. This handles custom and high-contrast themes rather than hard-coding theme
IDs.

## Build and install

Requires Windows and the .NET 10 SDK. Targets .NET Framework 4.8 (the in-process
VS host) and **Visual Studio 2026**, including **Insiders**, on x64. The current
public VS SDK and VSSDK 18 build tools generate the VSIX and package registration
directly from the F# assembly. No C# bridge or source generator is used.

```powershell
dotnet build .\ThemeFontWeight.sln -c Release
dotnet run --project .\tests\ThemeFontWeight.Tests -c Release
.\tests\ThemeFontWeight.Formatting.Tests\bin\Release\net48\ThemeFontWeight.Formatting.Tests.exe
```

Install `src\ThemeFontWeight\bin\Release\net48\ThemeFontWeight.vsix` using the
VSIX installer, then restart Visual Studio. Uninstall through **Extensions >
Manage Extensions** to return to Visual Studio's normal font-weight behavior.
Select your Insiders installation in the installer if you have multiple VS
instances.

## Experimental instance and debugging

Open `ThemeFontWeight.sln` in VS 2026 Insiders, choose the extension project as
the startup project, and press F5. Builds inside VS deploy to the **Exp** profile
and launch `devenv.exe /RootSuffix Exp /Log`. Command-line `dotnet build` produces
a VSIX without installing it.

Alternatively, close the experimental instance and run:

```powershell
.\Deploy-Experimental.ps1 -Launch
```

The script discovers VS 2026 installations including Insiders, uses the native
VSIX installer to install into Exp, then runs `/UpdateConfiguration` to merge
package and Unified Settings registrations. It leaves normal VS profiles
untouched and refuses to deploy while Exp is running. F5 uses this same
installation path after building, rather than SDK raw-copy deployment, which
does not reliably register a new extension in the VS 2026 extension cache.
Installing a VSIX into
your normal profile does **not** install it into Exp, and launching Exp alone
does **not** deploy an extension.

## Implementation

`registration.json` declares a searchable Unified Settings category, validated
enum values, defaults, labels, and descriptions. `ProvideSettingsManifest` on an
F# `AsyncPackage` generates the manifest registration automatically at build
time. The settings UI does not require loading the package or opening a document.

An F# MEF `IWpfTextViewCreationListener` acquires the native Unified Settings
service asynchronously. It reads effective values through `ISettingsReader`
and subscribes to live setting changes. There is no synchronous `LoadPackage`
call, duplicated settings store, or custom mutable settings singleton.

In-process `IClassificationFormatMap` remains necessary for changing the WPF
editor's typefaces. Default and classification-specific weights are shifted from
their original values: VS's `IsBold` flags force weight 400 or 700 and would
otherwise discard relative weights during the formatting merge. Those flags are
represented by explicitly weighted typefaces without changing italic flags or
Baselines are captured once per format map, shared by all views using it, and
retain the original bold distinction. They are refreshed when VS or another
extension changes formatting. All changes are batched. Per-view maps update
changes, with UI-thread marshaling and subscription cleanup when views close.
Settings retrieval errors go to the VS Activity Log without replacing them with
success-shaped defaults.

The public `ISettingsManager` API documents the Unified Settings service, but
its marker type is only in VS's internal interop assembly. A GUID-only local
service marker requests that service without referencing internal assemblies.

### API references

- [Unified Settings registration (`ProvideSettingsManifest`)](https://learn.microsoft.com/dotnet/api/microsoft.visualstudio.shell.providesettingsmanifestattribute)
- [Native Unified Settings manager](https://learn.microsoft.com/dotnet/api/microsoft.visualstudio.utilities.unifiedsettings.isettingsmanager)
- [Effective values and change subscriptions (`ISettingsReader`)](https://learn.microsoft.com/dotnet/api/microsoft.visualstudio.utilities.unifiedsettings.isettingsreader)
- [Production Unified Settings registration in Roslyn/Razor](https://github.com/dotnet/roslyn/blob/main/src/Razor/src/Razor/src/Microsoft.VisualStudio.RazorExtension/UnifiedSettings/razor.registration.json)
- [VSSDK-compatible modern extensibility and in-process APIs](https://learn.microsoft.com/visualstudio/extensibility/visualstudio.extensibility/get-started/in-proc-extensions)
