namespace ThemeFontWeight

open System
open System.ComponentModel.Composition
open System.Runtime.InteropServices
open System.Threading.Tasks
open Microsoft.VisualStudio.Shell
open Microsoft.VisualStudio.Threading
open Microsoft.VisualStudio.Utilities.UnifiedSettings

// The service is documented, but its marker type is in VS's internal interop assembly.
[<Guid("e3684f31-344e-42ea-9047-b620fdc7ac25")>]
type internal SVsUnifiedSettingsManager = interface end

module internal Settings =
    let private readWeight (reader: ISettingsReader) moniker =
        let result =
            reader.GetValue<string>(
                moniker,
                SettingReadOptions.RequireRegistration ||| SettingReadOptions.RequireValidation
            )

        if result.Outcome = SettingRetrievalOutcome.Success then
            Policy.parseWeight result.Value
        else
            Error $"Cannot read '{moniker}': {result.Outcome}. {result.Message}"

    let read reader =
        match readWeight reader Policy.LightSetting, readWeight reader Policy.DarkSetting with
        | Ok light, Ok dark -> Ok { Light = light; Dark = dark }
        | Error message, _
        | _, Error message -> Error message

[<Export>]
type SettingsService() =
    let reader =
        AsyncLazy<ISettingsReader>(
            Func<Task<ISettingsReader>>(fun () ->
                task {
                    let! service =
                        AsyncServiceProvider.GlobalProvider.GetServiceAsync(typeof<SVsUnifiedSettingsManager>)

                    let manager =
                        match service with
                        | :? ISettingsManager as manager -> manager
                        | _ -> invalidOp "Visual Studio's Unified Settings service is unavailable."

                    return manager.GetReader()
                }),
            ThreadHelper.JoinableTaskFactory
        )

    member _.GetReaderAsync() = reader.GetValueAsync()
