namespace ThemeFontWeight

open System.Runtime.InteropServices
open Microsoft.VisualStudio.Shell

module internal PackageIdentity =
    [<Literal>]
    let Guid = "89a14773-bca2-4e64-bf7e-042c93a72c09"

[<Guid(PackageIdentity.Guid)>]
[<PackageRegistration(UseManagedResourcesOnly = true)>]
[<ProvideOptionPage(typeof<FontWeightOptions>, "Theme Font Weight", "General", 0s, 0s, false)>]
type FontWeightPackage() =
    inherit Package()

    override this.Initialize() =
        base.Initialize()
        let options = this.GetDialogPage(typeof<FontWeightOptions>) :?> FontWeightOptions
        options.Publish()
