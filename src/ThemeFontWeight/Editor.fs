namespace ThemeFontWeight

open System
open System.Collections.Generic
open System.ComponentModel.Composition
open System.Threading.Tasks
open System.Windows
open System.Windows.Threading
open Microsoft.VisualStudio.PlatformUI
open Microsoft.VisualStudio.Shell
open Microsoft.VisualStudio.Text.Classification
open Microsoft.VisualStudio.Text.Editor
open Microsoft.VisualStudio.Threading
open Microsoft.VisualStudio.Utilities
open Microsoft.VisualStudio.Utilities.UnifiedSettings

type internal ViewWeightController(view: IWpfTextView, formatMap: IClassificationFormatMap, reader: ISettingsReader) =
    let dispatcher = view.VisualElement.Dispatcher
    let mutable pending = false
    let mutable applying = false
    let mutable reportedMissingClassification = false
    let baselineDefault = FormatBaseline(formatMap.DefaultTextProperties, formatMap.DefaultTextProperties)
    let classificationBaselines = Dictionary<IClassificationType, FormatBaseline>()

    let apply () =
        if not view.IsClosed then
            match Settings.read reader with
            | Error message -> ActivityLog.LogError("Theme Font Weight", message) |> ignore
            | Ok settings ->
                let background = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey)
                let isDark = Policy.isDarkBackground background.R background.G background.B
                let weight = settings |> Policy.selectWeight isDark |> int |> FontWeight.FromOpenTypeWeight
                let currentDefault = formatMap.DefaultTextProperties

                if not (obj.ReferenceEquals(currentDefault, baselineDefault.Applied)) then
                    baselineDefault.Original <-
                        FontFormatting.restoreOwnedWeight baselineDefault.Original baselineDefault.Applied currentDefault

                let weightedDefault = FontFormatting.shiftDefault weight baselineDefault.Original
                let classificationChanges =
                    formatMap.CurrentPriorityOrder
                    |> Seq.choose (fun classification ->
                        if isNull classification then
                            if not reportedMissingClassification then
                                reportedMissingClassification <- true
                                ActivityLog.LogWarning(
                                    "Theme Font Weight",
                                    "Skipping format definitions whose classification types are not registered in this VS instance."
                                ) |> ignore

                            None
                        else
                            let current = formatMap.GetExplicitTextProperties classification
                            let baseline =
                                match classificationBaselines.TryGetValue classification with
                                | true, baseline ->
                                    if not (obj.ReferenceEquals(current, baseline.Applied)) then
                                        let original =
                                            FontFormatting.restoreOwnedWeight baseline.Original baseline.Applied current

                                        if not original.TypefaceEmpty || not original.BoldEmpty then
                                            let typeface =
                                                if original.TypefaceEmpty then baselineDefault.Original.Typeface
                                                else original.Typeface

                                            let originalWeight =
                                                if original.BoldEmpty then typeface.Weight
                                                elif original.Bold then FontWeights.Bold
                                                else FontWeights.Normal

                                            baseline.Effective <-
                                                original.SetTypeface(
                                                    System.Windows.Media.Typeface(
                                                        typeface.FontFamily, typeface.Style, originalWeight,
                                                        typeface.Stretch
                                                    )
                                                )

                                        baseline.Original <- original

                                    baseline
                                | false, _ ->
                                    let baseline = FormatBaseline(current, formatMap.GetTextProperties classification)
                                    classificationBaselines.Add(classification, baseline)
                                    baseline

                            let weighted = FontFormatting.shiftClassification weight baseline.Effective baseline.Original
                            baseline.Applied <- weighted
                            if obj.ReferenceEquals(current, weighted) then None else Some(classification, weighted))
                    |> Seq.toList

                baselineDefault.Applied <- weightedDefault

                if not (obj.ReferenceEquals(currentDefault, weightedDefault)) || not classificationChanges.IsEmpty then
                    applying <- true

                    try
                        formatMap.BeginBatchUpdate()

                        try
                            formatMap.DefaultTextProperties <- weightedDefault

                            for classification, properties in classificationChanges do
                                formatMap.SetExplicitTextProperties(classification, properties)
                        finally
                            formatMap.EndBatchUpdate()
                    finally
                        applying <- false

    let requestApply () =
        let schedule () =
            // VS rebuilds the format map during theme changes; apply after that rebuild.
            if not pending && not view.IsClosed && not dispatcher.HasShutdownStarted then
                pending <- true

                dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    Action(fun () ->
                        pending <- false
                        ThreadHelper.JoinableTaskFactory.RunAsync(
                            Func<Task>(fun () -> task { apply() } :> Task)
                        ).FileAndForget("ThemeFontWeight/ApplyWeight"))
                )
                |> ignore

        if dispatcher.CheckAccess() then
            schedule()
        elif not view.IsClosed && not dispatcher.HasShutdownStarted then
            dispatcher.BeginInvoke(Action schedule) |> ignore

    let themeChanged = ThemeChangedEventHandler(fun _ -> requestApply())
    let mappingChanged = EventHandler<EventArgs>(fun _ _ -> if not applying then requestApply())
    let settingsChanged =
        reader.SubscribeToChanges(
            Action<SettingsUpdate>(fun _ -> requestApply()),
            [| Policy.LightSetting; Policy.DarkSetting |]
        )

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
        settingsService: SettingsService
    ) =

    interface IWpfTextViewCreationListener with
        member _.TextViewCreated view =
            ThreadHelper.ThrowIfNotOnUIThread()
            ThreadHelper.JoinableTaskFactory.RunAsync(
                Func<Task>(fun () ->
                    task {
                        let! reader = settingsService.GetReaderAsync()
                        do! ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync()

                        if not view.IsClosed then
                            let controller =
                                new ViewWeightController(view, formatMapService.GetClassificationFormatMap view, reader)

                            view.Closed.Add(fun _ -> (controller :> IDisposable).Dispose())
                    } :> Task)
            ).FileAndForget("ThemeFontWeight/InitializeEditor")
