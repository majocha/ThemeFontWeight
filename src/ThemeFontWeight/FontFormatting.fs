namespace ThemeFontWeight

open System.Windows
open System.Windows.Media
open Microsoft.VisualStudio.Text.Formatting

module FontFormatting =
    let relativeWeight (selected: FontWeight) (original: FontWeight) =
        original.ToOpenTypeWeight() + selected.ToOpenTypeWeight() - FontWeights.Normal.ToOpenTypeWeight()
        |> max 100
        |> min 900
        |> FontWeight.FromOpenTypeWeight

    let private withTypefaceWeight weight (typeface: Typeface) (properties: TextFormattingRunProperties) =
        // IsBold=false forces weight 400 during VS's format merge, even with a weighted Typeface.
        let properties = properties.ClearBold()
        let weighted =
            Typeface(typeface.FontFamily, typeface.Style, weight, typeface.Stretch)

        if not properties.TypefaceEmpty && properties.Typeface.Equals weighted then
            properties
        else
            properties.SetTypeface weighted

    let shiftDefault selected (properties: TextFormattingRunProperties) =
        withTypefaceWeight (relativeWeight selected properties.Typeface.Weight) properties.Typeface properties

    let shiftClassification selected (effective: TextFormattingRunProperties) (properties: TextFormattingRunProperties) =
        if properties.TypefaceEmpty && properties.BoldEmpty then
            properties
        else
            withTypefaceWeight (relativeWeight selected effective.Typeface.Weight) effective.Typeface properties

    let restoreOwnedWeight
        (original: TextFormattingRunProperties)
        (applied: TextFormattingRunProperties)
        (current: TextFormattingRunProperties)
        =
        // Other extensions may update colors while retaining our weighted typeface.
        if current.TypefaceEmpty = applied.TypefaceEmpty
           && (current.TypefaceEmpty || current.Typeface.Equals applied.Typeface)
           && current.BoldEmpty = applied.BoldEmpty
           && (current.BoldEmpty || current.Bold = applied.Bold) then
            let restored =
                if original.TypefaceEmpty then current.ClearTypeface()
                else current.SetTypeface original.Typeface

            if original.BoldEmpty then restored.ClearBold() else restored.SetBold original.Bold
        else
            current

type internal FormatBaseline(original: TextFormattingRunProperties, effective: TextFormattingRunProperties) =
    member val Original = original with get, set
    member val Effective = effective with get, set
    member val Applied = original with get, set
