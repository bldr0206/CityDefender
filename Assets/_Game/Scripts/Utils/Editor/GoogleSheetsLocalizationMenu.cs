using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.Google;
using UnityEditor.Localization.Reporting;
using UnityEngine;

public static class GoogleSheetsLocalizationMenu
{
    const string LocalizationSpreadsheetUrl =
        "https://docs.google.com/spreadsheets/d/1dX83tKmFMUtj05dDsez1nypWqbxx_cZC8s9Ij1mbia8/edit?gid=2126037230#gid=2126037230";

    [MenuItem("Localization/Open Google Spreadsheet")]
    public static void OpenGoogleSpreadsheet()
    {
        Application.OpenURL(LocalizationSpreadsheetUrl);
    }

    [MenuItem("Localization/Pull from Google Sheets")]
    public static void PullFromGoogleSheets()
    {
        foreach (GoogleSheetsExtension ext in LocalizationEditorSettings.GetStringTableCollections()
                     .SelectMany(c => c.Extensions)
                     .OfType<GoogleSheetsExtension>())
        {
            Pull(ext);
        }
    }

    [MenuItem("Localization/Push to Google Sheets")]
    public static void PushToGoogleSheets()
    {
        foreach (GoogleSheetsExtension ext in LocalizationEditorSettings.GetStringTableCollections()
                     .SelectMany(c => c.Extensions)
                     .OfType<GoogleSheetsExtension>())
        {
            Push(ext);
        }
    }

    static void Pull(GoogleSheetsExtension googleExtension)
    {
        var googleSheets = new GoogleSheets(googleExtension.SheetsServiceProvider);
        googleSheets.SpreadSheetId = googleExtension.SpreadsheetId;
        googleSheets.PullIntoStringTableCollection(
            googleExtension.SheetId,
            googleExtension.TargetCollection as StringTableCollection,
            googleExtension.Columns,
            googleExtension.RemoveMissingPulledKeys,
            new ProgressBarReporter(),
            createUndo: true);
    }

    static void Push(GoogleSheetsExtension googleExtension)
    {
        var googleSheets = new GoogleSheets(googleExtension.SheetsServiceProvider);
        googleSheets.SpreadSheetId = googleExtension.SpreadsheetId;
        googleSheets.PushStringTableCollection(
            googleExtension.SheetId,
            googleExtension.TargetCollection as StringTableCollection,
            googleExtension.Columns,
            new ProgressBarReporter());
    }
}
