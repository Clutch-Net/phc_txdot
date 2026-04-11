// Tabular Editor 3 C# Script
// Purpose: Update Calendar table description, column descriptions, and format strings

using System;
using System.Collections.Generic;
using System.Linq;

// --------------------------------------------------
// GET TABLE
// --------------------------------------------------
var table = Model.Tables["Calendar"];
if (table == null)
{
    Error("Table 'Calendar' not found!");
    return;
}

// --------------------------------------------------
// TABLE DESCRIPTION
// --------------------------------------------------
table.Description = @"Conformed calendar dimension used for reporting, filtering, and time intelligence across the semantic model.";

// --------------------------------------------------
// COLUMN DESCRIPTIONS
// --------------------------------------------------
var columnDescriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    { "Calendar Date", "The atomic calendar date represented by the row." },
    { "Date Key", "Integer surrogate key for the date in YYYYMMDD format." },
    { "Day Name", "Full day name for the date." },
    { "Day Name Short", "Abbreviated day name for the date." },
    { "Day Of Month", "Day number within the month." },
    { "Day Of Week ISO", "ISO day of week number, where Monday is 1 and Sunday is 7." },
    { "Quarter", "Calendar quarter number." },
    { "Quarter Label", "Formatted calendar quarter label." },
    { "Month", "Calendar month number." },
    { "Year", "Calendar year." },
    { "Day Of Year", "Day number within the calendar year." },
    { "Week Of Year", "Calendar week number within the year." },
    { "First Day Of Month", "The first calendar day of the month for this date." },
    { "Last Day Of Month", "The last calendar day of the month for this date." },
    { "First Day Of Quarter", "The first calendar day of the quarter for this date." },
    { "Last Day Of Quarter", "The last calendar day of the quarter for this date." },
    { "First Day Of Year", "The first calendar day of the year for this date." },
    { "Last Day Of Year", "The last calendar day of the year for this date." },
    { "Month Name", "Full month name for the date." },
    { "Month Name Short", "Abbreviated month name for the date." },
    { "Month Year", "Formatted month and year label." },
    { "Period", "Formatted period label used for reporting." },
    { "Year Quarter", "Formatted year and quarter label." },
    { "Year Month Key", "Integer key for the year and month, typically YYYYMM." },
    { "Month Sort", "Sort helper used to order month-based labels chronologically." },
    { "Flag Weekday", "Flag indicating whether the date is a weekday." },
    { "Flag Last Day Of Month", "Flag indicating whether the date is the last day of the month." },
    { "Fiscal Year", "Fiscal year associated with the date." },
    { "Fiscal Quarter", "Fiscal quarter number associated with the date." },
    { "Fiscal Year Label", "Formatted fiscal year label." },
    { "Fiscal Quarter Label", "Formatted fiscal quarter label." },
    { "Is Holiday", "Flag indicating whether the date is a holiday." },
    { "Holiday Key", "Integer identifier for the holiday record, when applicable." },
    { "Holiday Name", "Holiday name when the date is a holiday." },
    { "Holiday Description", "Description of the holiday when applicable." },
    { "Holiday Country Code", "Country code associated with the holiday definition." },
    { "Holiday Group", "Grouping classification for the holiday." },
    { "Relative Day", "Relative day offset from today, where today is zero." },
    { "Relative Month", "Relative month offset from the current month, where the current month is zero." },
    { "Relative Year", "Relative year offset from the current year, where the current year is zero." },
    { "Is YTD Flag", "Flag indicating whether the date falls within the current year-to-date period." },
    { "Is QTD Flag", "Flag indicating whether the date falls within the current quarter-to-date period." },
    { "Is MTD Flag", "Flag indicating whether the date falls within the current month-to-date period." }
};

// --------------------------------------------------
// FORMAT STRINGS
// --------------------------------------------------
var columnFormats = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    { "Calendar Date", "yyyy-mm-dd" },
    { "Date Key", "0;(0)" },
    { "Day Of Month", "#,0;(#,0)" },
    { "Day Of Week ISO", "#,0;(#,0)" },
    { "Quarter", "#,0;(#,0)" },
    { "Month", "#,0;(#,0)" },
    { "Year", "#,0;(#,0)" },
    { "Day Of Year", "#,0;(#,0)" },
    { "Week Of Year", "#,0;(#,0)" },
    { "First Day Of Month", "yyyy-mm-dd" },
    { "Last Day Of Month", "yyyy-mm-dd" },
    { "First Day Of Quarter", "yyyy-mm-dd" },
    { "Last Day Of Quarter", "yyyy-mm-dd" },
    { "First Day Of Year", "yyyy-mm-dd" },
    { "Last Day Of Year", "yyyy-mm-dd" },
    { "Year Month Key", "0;(0)" },
    { "Month Sort", "#,0;(#,0)" },
    { "Flag Weekday", "#,0;(#,0)" },
    { "Flag Last Day Of Month", "#,0;(#,0)" },
    { "Fiscal Year", "#,0;(#,0)" },
    { "Fiscal Quarter", "#,0;(#,0)" },
    { "Is Holiday", "#,0;(#,0)" },
    { "Holiday Key", "0;(0)" },
    { "Relative Day", "#,0;(#,0)" },
    { "Relative Month", "#,0;(#,0)" },
    { "Relative Year", "#,0;(#,0)" },
    { "Is YTD Flag", "#,0;(#,0)" },
    { "Is QTD Flag", "#,0;(#,0)" },
    { "Is MTD Flag", "#,0;(#,0)" }
};

// --------------------------------------------------
// APPLY UPDATES
// --------------------------------------------------
var missingColumns = new List<string>();
var updatedDescriptions = 0;
var updatedFormats = 0;

foreach (var kvp in columnDescriptions)
{
    var col = table.Columns[kvp.Key];
    if (col == null)
    {
        missingColumns.Add(kvp.Key);
        continue;
    }

    col.Description = kvp.Value;
    updatedDescriptions++;
}

foreach (var kvp in columnFormats)
{
    var col = table.Columns[kvp.Key];
    if (col == null)
    {
        if (!missingColumns.Contains(kvp.Key))
            missingColumns.Add(kvp.Key);

        continue;
    }

    col.FormatString = kvp.Value;
    updatedFormats++;
}

// --------------------------------------------------
// WARN ON MISSING COLUMNS
// --------------------------------------------------
if (missingColumns.Any())
{
    Warning(
        "These mapped Calendar columns were not found in the model:\n - " +
        string.Join("\n - ", missingColumns.Distinct())
    );
}

// --------------------------------------------------
// DONE
// --------------------------------------------------
Info(
    $"Updated table description for '{table.Name}'.\n" +
    $"Updated column descriptions: {updatedDescriptions}\n" +
    $"Updated format strings: {updatedFormats}"
);