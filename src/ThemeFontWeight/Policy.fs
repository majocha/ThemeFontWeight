namespace ThemeFontWeight

type EditorFontWeight =
    | Thin = 100
    | ExtraLight = 200
    | Light = 300
    | Normal = 400
    | Medium = 500
    | SemiBold = 600
    | Bold = 700
    | ExtraBold = 800
    | Black = 900

type WeightSettings =
    { Light: EditorFontWeight
      Dark: EditorFontWeight }

module Policy =
    [<Literal>]
    let LightSetting = "environment.themeFontWeight.lightTheme"

    [<Literal>]
    let DarkSetting = "environment.themeFontWeight.darkTheme"

    let weights =
        [ "thin", EditorFontWeight.Thin
          "extraLight", EditorFontWeight.ExtraLight
          "light", EditorFontWeight.Light
          "normal", EditorFontWeight.Normal
          "medium", EditorFontWeight.Medium
          "semiBold", EditorFontWeight.SemiBold
          "bold", EditorFontWeight.Bold
          "extraBold", EditorFontWeight.ExtraBold
          "black", EditorFontWeight.Black ]

    let defaults =
        { Light = EditorFontWeight.Normal
          Dark = EditorFontWeight.Normal }

    let isDarkBackground (red: byte) (green: byte) (blue: byte) =
        let brightest = max red (max green blue) |> int
        let darkest = min red (min green blue) |> int
        brightest + darkest < 255

    let selectWeight isDark settings =
        if isDark then settings.Dark else settings.Light

    let isValidWeight weight =
        System.Enum.IsDefined(typeof<EditorFontWeight>, weight)

    let parseWeight value =
        match weights |> List.tryFind (fun (name, _) -> name = value) with
        | Some(_, weight) -> Ok weight
        | None -> Error $"Unsupported font weight '{value}'."
