namespace ThemeFontWeight

open System.Windows
open System.Windows.Media
open Microsoft.VisualStudio.Text.Formatting

module FontFormatting =
    let withWeight (weight: FontWeight) (properties: TextFormattingRunProperties) =
        // IsBold=false forces weight 400 during VS's format merge, even with a weighted Typeface.
        let properties = properties.ClearBold()

        if properties.TypefaceEmpty || properties.Typeface.Weight = weight then
            properties
        else
            let original = properties.Typeface
            properties.SetTypeface(Typeface(original.FontFamily, original.Style, weight, original.Stretch))

    let forClassification weight (properties: TextFormattingRunProperties) =
        if properties.Bold then properties else withWeight weight properties
