namespace ThemeFontWeight

open System
open System.ComponentModel.Composition
open System.Windows
open System.Windows.Media
open System.Windows.Threading
open Microsoft.VisualStudio
open Microsoft.VisualStudio.PlatformUI
open Microsoft.VisualStudio.Shell
open Microsoft.VisualStudio.Shell.Interop
open Microsoft.VisualStudio.Text.Classification
open Microsoft.VisualStudio.Text.Editor
open Microsoft.VisualStudio.Utilities

type internal ViewWeightController(view: IWpfTextView, formatMap: IClassificationFormatMap) =
    let dispatcher = view.VisualElement.Dispatcher
    let mutable pending = false
    let mutable applying = false

    let apply () =
        if not view.IsClosed then
            let background = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey)
            let isDark = Policy.isDarkBackground background.R background.G background.B
            let weight = Settings.get() |> Policy.selectWeight isDark |> int |> FontWeight.FromOpenTypeWeight
            let properties = formatMap.DefaultTextProperties

            if properties.Typeface.Weight <> weight then
                applying <- true

                try
                    let original = properties.Typeface
                    let typeface = Typeface(original.FontFamily, original.Style, weight, original.Stretch)
                    formatMap.DefaultTextProperties <- properties.SetTypeface typeface
                finally
                    applying <- false

    let requestApply () =
        // VS rebuilds the format map during theme changes; apply after that rebuild.
        if not pending && not view.IsClosed then
            pending <- true

            dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                Action(fun () ->
                    pending <- false
                    apply())
            )
            |> ignore

    let themeChanged = ThemeChangedEventHandler(fun _ -> requestApply())
    let mappingChanged = EventHandler<EventArgs>(fun _ _ -> if not applying then requestApply())
    let settingsChanged = Settings.Changed.Subscribe(fun () -> requestApply())

    do
        VSColorTheme.add_ThemeChanged themeChanged
        formatMap.ClassificationFormatMappingChanged.AddHandler mappingChanged
        requestApply()

    interface IDisposable with
        member _.Dispose() =
            VSColorTheme.remove_ThemeChanged themeChanged
            formatMap.ClassificationFormatMappingChanged.RemoveHandler mappingChanged
            settingsChanged.Dispose()

[<Export(typeof<IWpfTextViewCreationListener>)>]
[<ContentType("text")>]
[<TextViewRole(PredefinedTextViewRoles.Document)>]
type FontWeightViewListener
    [<ImportingConstructor>]
    (
        formatMapService: IClassificationFormatMapService,
        [<Import(typeof<SVsServiceProvider>)>] serviceProvider: IServiceProvider
    ) =

    interface IWpfTextViewCreationListener with
        member _.TextViewCreated view =
            ThreadHelper.ThrowIfNotOnUIThread()
            let shell = serviceProvider.GetService(typeof<SVsShell>) :?> IVsShell
            let mutable packageGuid = Guid PackageIdentity.Guid
            let mutable package = Unchecked.defaultof<IVsPackage>
            ErrorHandler.ThrowOnFailure(shell.LoadPackage(&packageGuid, &package)) |> ignore

            let controller = new ViewWeightController(view, formatMapService.GetClassificationFormatMap view)
            view.Closed.Add(fun _ -> (controller :> IDisposable).Dispose())
