# Theme Font Weight

The default Visual Studio editor font is too heavy, especially in dark themes.
This extension allows to make the editor text lighter. You set one weight for light
themes and one for dark themes. Written entirely in F#.

You need a variable weight font like JetBrains Mono.

Go to **Tools > Options > Environment > Theme Font Weight**. Changes apply
immediately. The settings sync to your other Visual Studio installations.

The weight is a shift from Normal (400). For example, Light (300) changes
regular text to 300 and bold text to 600, so bold text stays bold. The
extension does not change the font family, size or colors.

## Build and install

Requires Windows and the .NET 11 SDK. Targets .NET Framework 4.8 (the in-process
VS host) and **Visual Studio 2026**, including **Insiders**, on x64. The current
public VS SDK and VSSDK 18 build tools generate the VSIX and package registration
directly from the F# assembly. No C# bridge or source generator is used.

```powershell
dotnet build .\ThemeFontWeight.sln -c Release
dotnet run --project .\tests\ThemeFontWeight.Tests -c Release
.\tests\ThemeFontWeight.Formatting.Tests\bin\Release\net48\ThemeFontWeight.Formatting.Tests.exe
```

Versions are `Major.Minor.Commits`: `VersionMajorMinor` in `Directory.Build.props` (set by hand),
then the number of commits to HEAD (`Directory.Build.targets`). The VSIX and the file versions use
it; assembly versions stay at `Major.Minor.0.0`. Pass `-p:GitHeight=<n>` to set the last part.

Install `src\ThemeFontWeight\bin\Release\net48\ThemeFontWeight.vsix` using the
VSIX installer, then restart Visual Studio. Uninstall through **Extensions >
Manage Extensions** to return to Visual Studio's normal font-weight behavior.
Select your Insiders installation in the installer if you have multiple VS
instances.

## Publish

The GitHub workflow `.github/workflows/build.yml` builds and tests each push and pull request.
Each workflow run keeps the VSIX as an artifact.

- **Open VSIX Gallery**: each push to `main` uploads the VSIX to the
  [Open VSIX Gallery](https://www.vsixgallery.com/extension/ThemeFontWeight.89a14773-bca2-4e64-bf7e-042c93a72c09).
  This gives nightly builds. No account or token is necessary.
- **Visual Studio Marketplace**: put `[release]` in the commit message, or start the workflow
  manually. The workflow then publishes to the Marketplace and makes a GitHub release with
  the tag `v<version>`. The repository secret `VS_MARKETPLACE_PAT` must contain the token (see below).

To get the nightly builds in Visual Studio:

1. Select **Tools > Options > Environment > Extensions**.
2. Add an additional extension gallery with this URL:
   `https://www.vsixgallery.com/feed/extension/ThemeFontWeight.89a14773-bca2-4e64-bf7e-042c93a72c09`
3. Find the extension in **Extensions > Manage Extensions** in that gallery.

To publish to the Visual Studio Marketplace from your computer, commit all changes first. Then run:

```powershell
$env:VS_MARKETPLACE_PAT = '<token>'
.\Publish-Vsix.ps1
```

The token is an Azure DevOps personal access token with the **Marketplace (Manage)** scope and
**All accessible organizations**. The listing is in `src\ThemeFontWeight\publishManifest.json`,
and the Marketplace page is `overview.md` in the same folder. The Marketplace does not accept a
version again, so each upload needs a new commit.

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
Each setting declares `migration.roamingOnly` to connect Unified Settings to
Visual Studio's roaming store without introducing a legacy settings location
or a separate extension-managed store.

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

## License

[MIT](LICENSE).
