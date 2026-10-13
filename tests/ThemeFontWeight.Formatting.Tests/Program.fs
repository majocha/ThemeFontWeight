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
    let weighted = FontFormatting.shiftDefault FontWeights.SemiBold regular

    check "Semi Bold typeface is weight 600" (weighted.Typeface.Weight.ToOpenTypeWeight() = 600)
    check "Regular flag cannot override the selected weight" weighted.BoldEmpty
    check "Font family preserved" (weighted.Typeface.FontFamily = original.Typeface.FontFamily)
    check "Italic style preserved" (weighted.Typeface.Style = original.Typeface.Style)
    check "Font stretch preserved" (weighted.Typeface.Stretch = original.Typeface.Stretch)
    check "Font size preserved" (weighted.FontRenderingEmSize = original.FontRenderingEmSize)
    check "Foreground preserved" (weighted.ForegroundBrush = original.ForegroundBrush)

    let bold = original.SetBold(true)
    let effectiveBold = original.SetTypeface(Typeface(typeface.FontFamily, typeface.Style, FontWeights.Bold, typeface.Stretch))
    let lighterBold = FontFormatting.shiftClassification FontWeights.Light effectiveBold bold
    check "Light shifts bold from 700 to 600" (lighterBold.Typeface.Weight = FontWeights.SemiBold)
    check "Boolean bold cannot override the relative weight" lighterBold.BoldEmpty
    check "Bold italic remains italic" (lighterBold.Typeface.Style = FontStyles.Italic)
    check "Bold color preserved" (lighterBold.ForegroundBrush = bold.ForegroundBrush)

    let inheritedBold = original.ClearTypeface().SetBold(true)
    let lighterInheritedBold = FontFormatting.shiftClassification FontWeights.Light effectiveBold inheritedBold
    check "Bold-only classification also shifts to 600" (lighterInheritedBold.Typeface.Weight = FontWeights.SemiBold)
    check "Bold-only classification keeps its effective italic" (lighterInheritedBold.Typeface.Style = FontStyles.Italic)

    let inherited = original.ClearTypeface().SetBold(false)
    let inheritedWeighted = FontFormatting.shiftClassification FontWeights.Light original inherited
    check "Regular override becomes weight 300" (inheritedWeighted.Typeface.Weight = FontWeights.Light)
    check "Classification regular flag cleared" inheritedWeighted.BoldEmpty
    check "Classification color preserved" (inheritedWeighted.ForegroundBrush = original.ForegroundBrush)

    let inheritedItalic = original.ClearTypeface().SetItalic(true)
    let inheritedItalicWeighted = FontFormatting.shiftClassification FontWeights.Light original inheritedItalic
    check "Italic-only classification continues inheriting its typeface" inheritedItalicWeighted.TypefaceEmpty
    check "Explicit italic flag retained" (inheritedItalicWeighted.Italic && not inheritedItalicWeighted.ItalicEmpty)
    check "Inherited properties are not made explicit" (obj.ReferenceEquals(inheritedItalic, inheritedItalicWeighted))

    let decorated = regular.SetItalic(true).SetTextDecorations(TextDecorations.Underline)
    let decoratedWeighted = FontFormatting.shiftClassification FontWeights.Light original decorated
    check "Italic flag preserved while weight shifts" (decoratedWeighted.Italic && not decoratedWeighted.ItalicEmpty)
    check "Underline decorations preserved" (decoratedWeighted.TextDecorations = decorated.TextDecorations)

    let coloredWeighted = weighted.SetForeground(Colors.Red)
    let restored = FontFormatting.restoreOwnedWeight regular weighted coloredWeighted
    check "Color-only changes restore the original weight baseline" (restored.Typeface.Weight = FontWeights.Normal)
    check "Restoring baseline preserves external color changes" (restored.ForegroundBrush = coloredWeighted.ForegroundBrush)
    check "Restoring baseline restores the original bold flag" (not restored.BoldEmpty && not restored.Bold)

    let externallyBold = coloredWeighted.SetBold(true)
    check "External bold change is not undone" (
        obj.ReferenceEquals(externallyBold, FontFormatting.restoreOwnedWeight regular weighted externallyBold))

    check "Thin shifts original bold to regular" (FontFormatting.relativeWeight FontWeights.Thin FontWeights.Bold = FontWeights.Normal)
    check "Normal preserves original bold" (FontFormatting.relativeWeight FontWeights.Normal FontWeights.Bold = FontWeights.Bold)
    check "Upper bound clamps at 900" (FontFormatting.relativeWeight FontWeights.Black FontWeights.Bold = FontWeights.Black)
    check "Lower bound clamps at 100" (FontFormatting.relativeWeight FontWeights.Thin FontWeights.Light = FontWeights.Thin)

    for _, weight in Policy.weights do
        let selected = FontWeight.FromOpenTypeWeight(int weight)
        let result = FontFormatting.shiftClassification selected original regular
        check $"{weight} produces exact requested weight" (result.Typeface.Weight.ToOpenTypeWeight() = int weight)
        check $"{weight} has no boolean weight override" result.BoldEmpty
        check $"{weight} reapplying original baseline is idempotent" (
            obj.ReferenceEquals(result, FontFormatting.shiftClassification selected original regular))
        let boldResult = FontFormatting.shiftClassification selected effectiveBold bold
        let expectedBold = min 900 (int weight + 300)
        check $"{weight} retains original bold offset" (boldResult.Typeface.Weight.ToOpenTypeWeight() = expectedBold)

    let custom = original.SetTypeface(Typeface(typeface.FontFamily, typeface.Style, FontWeights.Medium, typeface.Stretch))
    let customShifted = FontFormatting.shiftClassification FontWeights.Light custom custom
    check "Custom 500-weight classification shifts to 400" (customShifted.Typeface.Weight = FontWeights.Normal)
    check "Normal restores original 500-weight classification" (
        (FontFormatting.shiftClassification FontWeights.Normal custom custom).Typeface.Weight = FontWeights.Medium)

    0
