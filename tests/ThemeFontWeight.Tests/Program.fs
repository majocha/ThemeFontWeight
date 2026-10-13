open ThemeFontWeight
open System.IO
open System.Text.Json

let check name condition =
    if not condition then failwith $"Failed: {name}"
    printfn "PASS %s" name

let settings =
    { Light = EditorFontWeight.Light
      Dark = EditorFontWeight.SemiBold }

check "Light theme uses light setting" (Policy.selectWeight false settings = EditorFontWeight.Light)
check "Dark theme uses dark setting" (Policy.selectWeight true settings = EditorFontWeight.SemiBold)
check "Default weights preserve normal text" (Policy.defaults.Light = EditorFontWeight.Normal && Policy.defaults.Dark = EditorFontWeight.Normal)
check "Dark VS background" (Policy.isDarkBackground 37uy 37uy 38uy)
check "Light VS background" (not (Policy.isDarkBackground 245uy 245uy 245uy))
check "Blue VS theme uses light background" (not (Policy.isDarkBackground 239uy 239uy 242uy))
check "High contrast black" (Policy.isDarkBackground 0uy 0uy 0uy)
check "High contrast white" (not (Policy.isDarkBackground 255uy 255uy 255uy))
check "Dark threshold" (Policy.isDarkBackground 127uy 127uy 127uy)
check "Light threshold" (not (Policy.isDarkBackground 128uy 128uy 128uy))

for weight in System.Enum.GetValues<EditorFontWeight>() do
    check $"Valid weight {weight}" (Policy.isValidWeight weight)

check "Invalid weight rejected" (not (Policy.isValidWeight (enum<EditorFontWeight> 450)))

for name, weight in Policy.weights do
    check $"Parse registered weight {name}" (Policy.parseWeight name = Ok weight)

check "Unknown setting value rejected" (Result.isError (Policy.parseWeight "invalid"))
check "Null setting value rejected" (Result.isError (Policy.parseWeight null))

let registration = JsonDocument.Parse(File.ReadAllText(Path.Combine(System.AppContext.BaseDirectory, "registration.json")))
let root = registration.RootElement
let properties = root.GetProperty("properties")
let category = root.GetProperty("categories").GetProperty("environment.themeFontWeight")
check "Modern settings category is visible" (category.GetProperty("title").GetString() = "Theme Font Weight")

for moniker in [ Policy.LightSetting; Policy.DarkSetting ] do
    let setting = properties.GetProperty moniker
    let values = setting.GetProperty("enum").EnumerateArray() |> Seq.map _.GetString() |> Seq.toList
    check $"{moniker} matches all supported weights" (values = (Policy.weights |> List.map fst))
    check $"{moniker} has a label for every weight" (setting.GetProperty("enumItemLabels").GetArrayLength() = values.Length)
    check $"{moniker} default preserves normal text" (setting.GetProperty("default").GetString() = "normal")
    check $"{moniker} has no visibility gate" (not (setting.TryGetProperty("visibleWhen") |> fst))

registration.Dispose()
