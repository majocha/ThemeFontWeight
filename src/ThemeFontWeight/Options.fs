namespace ThemeFontWeight

open System.ComponentModel
open System.Runtime.InteropServices
open Microsoft.VisualStudio.Shell

module internal Settings =
    let private changed = Event<unit>()
    let mutable private current = Policy.defaults

    let get () = current
    let Changed = changed.Publish

    let update value =
        if not (Policy.isValidWeight value.Light && Policy.isValidWeight value.Dark) then
            invalidArg (nameof value) "Choose a supported font weight."

        if value <> current then
            current <- value
            changed.Trigger()

[<Guid("4d7c0f12-a32d-4a75-9347-6d5b45fca3f0")>]
type FontWeightOptions() =
    inherit DialogPage()

    [<Category("Font weight")>]
    [<DisplayName("Light theme")>]
    [<Description("Base code editor font weight when the theme has a light background.")>]
    [<DefaultValue(EditorFontWeight.Normal)>]
    member val LightTheme = EditorFontWeight.Normal with get, set

    [<Category("Font weight")>]
    [<DisplayName("Dark theme")>]
    [<Description("Base code editor font weight when the theme has a dark background.")>]
    [<DefaultValue(EditorFontWeight.Normal)>]
    member val DarkTheme = EditorFontWeight.Normal with get, set

    member this.Publish() =
        Settings.update
            { Light = this.LightTheme
              Dark = this.DarkTheme }

    override this.OnApply args =
        base.OnApply args

        if args.ApplyBehavior = DialogPage.ApplyKind.Apply then
            this.Publish()
