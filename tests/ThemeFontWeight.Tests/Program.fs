open ThemeFontWeight

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
