open System
open System.Windows
open System.Windows.Media
open Microsoft.VisualStudio.Text.Formatting
open ThemeFontWeight

let check name condition =
    if not condition then failwith $"Failed: {name}"
    printfn "PASS %s" name

[<EntryPoint; STAThread>]
let main _ =
    let typeface = Typeface(FontFamily("Consolas"), FontStyles.Italic, FontWeights.Normal, FontStretches.Condensed)
    let original = TextFormattingRunProperties.CreateTextFormattingRunProperties(typeface, 17.0, Colors.Green)
    let regular = original.SetBold(false)
    let weighted = FontFormatting.withWeight FontWeights.SemiBold regular

    check "Semi Bold typeface is weight 600" (weighted.Typeface.Weight.ToOpenTypeWeight() = 600)
    check "Regular flag cannot override the selected weight" weighted.BoldEmpty
    check "Font family preserved" (weighted.Typeface.FontFamily = original.Typeface.FontFamily)
    check "Italic style preserved" (weighted.Typeface.Style = original.Typeface.Style)
    check "Font stretch preserved" (weighted.Typeface.Stretch = original.Typeface.Stretch)
    check "Font size preserved" (weighted.FontRenderingEmSize = original.FontRenderingEmSize)
    check "Foreground preserved" (weighted.ForegroundBrush = original.ForegroundBrush)

    let bold = original.SetBold(true)
    check "Explicit syntax bold preserved" (obj.ReferenceEquals(bold, FontFormatting.forClassification FontWeights.Light bold))

    let inherited = original.ClearTypeface().SetBold(false)
    let inheritedWeighted = FontFormatting.forClassification FontWeights.Light inherited
    check "Classification inherits weighted typeface" inheritedWeighted.TypefaceEmpty
    check "Classification regular flag cleared" inheritedWeighted.BoldEmpty
    check "Classification color preserved" (inheritedWeighted.ForegroundBrush = original.ForegroundBrush)

    for _, weight in Policy.weights do
        let selected = FontWeight.FromOpenTypeWeight(int weight)
        let result = FontFormatting.forClassification selected regular
        check $"{weight} produces exact requested weight" (result.Typeface.Weight.ToOpenTypeWeight() = int weight)
        check $"{weight} has no boolean weight override" result.BoldEmpty
        check $"{weight} update is idempotent" (obj.ReferenceEquals(result, FontFormatting.forClassification selected result))

    0
