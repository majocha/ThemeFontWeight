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
