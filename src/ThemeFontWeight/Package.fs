namespace ThemeFontWeight

open System.Runtime.InteropServices
open Microsoft.VisualStudio.Shell

module internal PackageIdentity =
    [<Literal>]
    let Guid = "89a14773-bca2-4e64-bf7e-042c93a72c09"

[<Guid(PackageIdentity.Guid)>]
[<PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)>]
[<ProvideSettingsManifest>]
type FontWeightPackage() =
    inherit AsyncPackage()
