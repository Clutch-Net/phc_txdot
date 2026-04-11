// Tabular Editor 3 C# Script
// Purpose: Create all measures from exported _Measures table if they do not already exist

using System;
using System.Collections.Generic;
using System.Linq;

// --------------------------------------------------
// GET _Measures TABLE
// --------------------------------------------------
var measuresTable = Model.Tables["_Measures"];
if (measuresTable == null)
{
    Error("Table '_Measures' not found.");
    return;
}

// --------------------------------------------------
// MEASURE DEF CLASS
// --------------------------------------------------
public class MeasureDef
{
    public string Name;
    public string Expression;
    public string Description;
    public string FormatString;
    public string DisplayFolder;
}

// --------------------------------------------------
// HELPER
// --------------------------------------------------
void AddMeasureIfMissing(MeasureDef def, ref int created, ref int skipped)
{
    var existing = measuresTable.Measures
        .FirstOrDefault(m => m.Name.Equals(def.Name, StringComparison.OrdinalIgnoreCase));

    if (existing != null)
    {
        skipped++;
        return;
    }

    var measure = measuresTable.AddMeasure(def.Name, def.Expression);

    if (!string.IsNullOrWhiteSpace(def.Description))
        measure.Description = def.Description;

    if (!string.IsNullOrWhiteSpace(def.FormatString))
        measure.FormatString = def.FormatString;

    if (!string.IsNullOrWhiteSpace(def.DisplayFolder))
        measure.DisplayFolder = def.DisplayFolder;

    created++;
}

// --------------------------------------------------
// MEASURE DEFINITIONS
// --------------------------------------------------
var measures = new List<MeasureDef>
{
    new MeasureDef {
        Name = "Bid",
        Description = "Number of distinct projects where the focus vendor competed.",
        Expression = @"
CALCULATE(
    DISTINCTCOUNT('Project Bid Role Comparison'[Project ID]),
    'Project Bid Role Comparison'[Focus Vendor Competed Flag] = TRUE
)",
        FormatString = "#,0;(#,0)",
        DisplayFolder = @"Cards\Focus Vendor"
    },
    new MeasureDef {
        Name = "Won",
        Description = "Number of distinct projects won by the focus vendor.",
        Expression = @"
CALCULATE(
    DISTINCTCOUNT('Project Bid Role Comparison'[Project ID]),
    'Project Bid Role Comparison'[Focus Vendor Won Flag] = TRUE
)",
        FormatString = "#,0;(#,0)",
        DisplayFolder = @"Cards\Focus Vendor"
    },
    new MeasureDef {
        Name = "Lost",
        Description = "Projects bid but not won by the focus vendor.",
        Expression = @"[Bid] - [Won]",
        FormatString = "#,0;(#,0)",
        DisplayFolder = @"Cards\Focus Vendor"
    },
    new MeasureDef {
        Name = "Bid - Won - Lost",
        Description = "Text summary showing bid, won, and lost project counts for the focus vendor.",
        Expression = @"
VAR b = COALESCE([Bid], 0)
VAR w = COALESCE([Won], 0)
VAR l = COALESCE([Lost], 0)
RETURN
    IF(
        b = 0 && w = 0 && l = 0,
        BLANK(),
        FORMAT(b, ""#,0"")
            & "" - ""
            & FORMAT(w, ""#,0"")
            & "" - ""
            & FORMAT(l, ""#,0"")
    )",
        DisplayFolder = @"Cards\Focus Vendor"
    },
    new MeasureDef {
        Name = "Win Rate",
        Description = "Win rate for the focus vendor.",
        Expression = @"DIVIDE([Won], [Bid])",
        FormatString = "0.00%;(0.00%)",
        DisplayFolder = @"Cards\Focus Vendor"
    },
    new MeasureDef {
        Name = "Total Contract Awards",
        Description = "Total awarded contract value for projects won by the focus vendor.",
        Expression = @"
SUMX(
    CALCULATETABLE(
        VALUES('Project Bid Role Comparison'[Project ID]),
        'Project Bid Role Comparison'[Focus Vendor Won Flag] = TRUE
    ),
    CALCULATE(
        MAX('Project Bid Role Comparison'[Lowest Bid Amount In Project])
    )
)",
        FormatString = "$#,0;($#,0)",
        DisplayFolder = @"Cards\Focus Vendor"
    },
    new MeasureDef {
        Name = "Average Contract Award",
        Description = "Average awarded contract value for projects won by the focus vendor.",
        Expression = @"DIVIDE([Total Contract Awards], [Won]) + 0",
        FormatString = "$#,0;($#,0)",
        DisplayFolder = @"Cards\Focus Vendor"
    },
    new MeasureDef {
        Name = "Focus Vendor",
        Description = "Configured focus vendor name in the current filter context.",
        Expression = @"
SELECTEDVALUE(
    'Project Bid Role Comparison'[Configured Focus Vendor Name],
    """"
) & """"",
        DisplayFolder = @"Cards\Focus Vendor"
    },
    new MeasureDef {
        Name = "Underbid Exposure",
        Description = "For projects won by the focus vendor, total positive difference between benchmark bid total and focus vendor bid total.",
        Expression = @"
SUMX(
    CALCULATETABLE(
        VALUES('Project Bid Role Comparison'[Project ID]),
        'Project Bid Role Comparison'[Focus Vendor Won Flag] = TRUE
    ),
    VAR FocusBid =
        CALCULATE(
            MAX('Project Bid Role Comparison'[Focus Bid Total Amount])
        )
    VAR BenchmarkBid =
        CALCULATE(
            MAX('Project Bid Role Comparison'[Benchmark Bid Total Amount])
        )
    VAR Diff = BenchmarkBid - FocusBid
    RETURN
        IF(Diff > 0, Diff)
)",
        FormatString = "$#,0;($#,0)",
        DisplayFolder = @"Cards\Focus Vendor"
    },
    new MeasureDef {
        Name = "Underbid Exposure Ratio",
        Description = "Underbid exposure as a percentage of total focus vendor bids on won projects.",
        Expression = @"
DIVIDE(
    [Underbid Exposure],
    SUMX(
        CALCULATETABLE(
            VALUES('Project Bid Role Comparison'[Project ID]),
            'Project Bid Role Comparison'[Focus Vendor Won Flag] = TRUE
        ),
        CALCULATE(
            MAX('Project Bid Role Comparison'[Focus Bid Total Amount])
        )
    )
)",
        FormatString = "0.00%;(0.00%)",
        DisplayFolder = @"Cards\Focus Vendor"
    },
    new MeasureDef {
        Name = "Overbid",
        Description = "For projects lost by the focus vendor, total positive difference between focus vendor bid total and benchmark bid total.",
        Expression = @"
SUMX(
    CALCULATETABLE(
        VALUES('Project Bid Role Comparison'[Project ID]),
        'Project Bid Role Comparison'[Focus Vendor Competed Flag] = TRUE,
        'Project Bid Role Comparison'[Focus Vendor Won Flag] <> TRUE
    ),
    VAR FocusBid =
        CALCULATE(
            MAX('Project Bid Role Comparison'[Focus Bid Total Amount])
        )
    VAR BenchmarkBid =
        CALCULATE(
            MAX('Project Bid Role Comparison'[Benchmark Bid Total Amount])
        )
    VAR Diff = FocusBid - BenchmarkBid
    RETURN
        IF(Diff > 0, Diff)
)",
        FormatString = "$#,0;($#,0)",
        DisplayFolder = @"Cards\Focus Vendor"
    },
    new MeasureDef {
        Name = "Overbid Ratio",
        Description = "Overbid as a percentage of total focus vendor bids on lost projects.",
        Expression = @"
DIVIDE(
    [Overbid],
    SUMX(
        CALCULATETABLE(
            VALUES('Project Bid Role Comparison'[Project ID]),
            'Project Bid Role Comparison'[Focus Vendor Competed Flag] = TRUE,
            'Project Bid Role Comparison'[Focus Vendor Won Flag] <> TRUE
        ),
        CALCULATE(
            MAX('Project Bid Role Comparison'[Focus Bid Total Amount])
        )
    )
)",
        FormatString = "0.00%;(0.00%)",
        DisplayFolder = @"Cards\Focus Vendor"
    },
    new MeasureDef {
        Name = "Benchmark Vendor",
        Description = "Name of the benchmark vendor in the current filter context.",
        Expression = @"
SELECTEDVALUE(
    'Project Bid Role Comparison'[Benchmark Vendor Name],
    """"
) & """"",
        DisplayFolder = @"Cards\Benchmark Vendor"
    },
    new MeasureDef {
        Name = "Benchmark Vendor Contract Awards",
        Description = "Total awarded contract value on projects where the benchmark vendor beat the focus vendor.",
        Expression = @"
SUMX(
    CALCULATETABLE(
        VALUES('Project Bid Role Comparison'[Project ID]),
        'Project Bid Role Comparison'[Focus Vendor Lost Flag] = TRUE,
        NOT ISBLANK('Project Bid Role Comparison'[Benchmark Vendor Name])
    ),
    CALCULATE(
        MAX('Project Bid Role Comparison'[Lowest Bid Amount In Project])
    )
)",
        FormatString = "$#,0;($#,0)",
        DisplayFolder = @"Cards\Benchmark Vendor"
    },
    new MeasureDef {
        Name = "Focus vs Benchmark Contract Award Delta",
        Description = "Difference between total contract awards won by the focus vendor and total contract awards won by benchmark vendors against the focus vendor.",
        Expression = @"[Total Contract Awards] - [Benchmark Vendor Contract Awards]",
        FormatString = "$#,0;($#,0)",
        DisplayFolder = @"Cards\Benchmark Vendor"
    },
    new MeasureDef {
        Name = "1st Place Vendor",
        Description = "Name of the winning vendor in the current filter context.",
        Expression = @"
SELECTEDVALUE(
    'Project Bid Role Comparison'[Winner Vendor Name],
    """"
) & """"",
        DisplayFolder = @"Cards\Any Vendor"
    },
    new MeasureDef {
        Name = "2nd Place Vendor",
        Description = "Name of the second place vendor in the current filter context.",
        Expression = @"
SELECTEDVALUE(
    'Project Bid Role Comparison'[Second Vendor Name],
    """"
) & """"",
        DisplayFolder = @"Cards\Any Vendor"
    },
    new MeasureDef {
        Name = "Any Vendor",
        Description = "Name of the selected vendor in the current filter context.",
        Expression = @"SELECTEDVALUE('Vendor'[Vendor Name], """") & """"",
        DisplayFolder = @"Cards\Any Vendor"
    },
    new MeasureDef {
        Name = "Any Vendor Bid",
        Description = "Number of distinct projects bid by the selected vendor.",
        Expression = @"DISTINCTCOUNT('Project Bids'[Project ID])",
        FormatString = "#,0;(#,0)",
        DisplayFolder = @"Cards\Any Vendor"
    },
    new MeasureDef {
        Name = "Any Vendor Won",
        Description = "Number of distinct projects won by the selected vendor.",
        Expression = @"
CALCULATE(
    DISTINCTCOUNT('Project Bids'[Project ID]),
    'Project Bids'[Is Low Bidder] = TRUE
)",
        FormatString = "#,0;(#,0)",
        DisplayFolder = @"Cards\Any Vendor"
    },
    new MeasureDef {
        Name = "Any Vendor Lost",
        Description = "Number of distinct projects bid but not won by the selected vendor.",
        Expression = @"[Any Vendor Bid] - [Any Vendor Won]",
        FormatString = "#,0;(#,0)",
        DisplayFolder = @"Cards\Any Vendor"
    },
    new MeasureDef {
        Name = "Any Vendor Total Bids",
        Description = "Total amount bid by the selected vendor across all visible projects.",
        Expression = @"
SUMX(
    VALUES('Project Bids'[Project ID]),
    CALCULATE(
        MAX('Project Bids'[Max Bid Total Amount])
    )
)",
        FormatString = "$#,0;($#,0)",
        DisplayFolder = @"Cards\Any Vendor"
    },
    new MeasureDef {
        Name = "Any Vendor Total Contract Awards",
        Description = "Total contract value awarded to the selected vendor based on projects won.",
        Expression = @"
SUMX(
    CALCULATETABLE(
        VALUES('Project Bids'[Project ID]),
        'Project Bids'[Is Low Bidder] = TRUE
    ),
    CALCULATE(
        MAX('Project Bids'[Max Bid Total Amount])
    )
)",
        FormatString = "$#,0;($#,0)",
        DisplayFolder = @"Cards\Any Vendor"
    },
    new MeasureDef {
        Name = "Any Vendor % of Total Awarded",
        Description = "Share of total awarded contract value represented by the selected vendor in the current selection.",
        Expression = @"
DIVIDE(
    [Any Vendor Total Contract Awards],
    CALCULATE(
        SUMX(
            VALUES('Project Bids'[Project ID]),
            CALCULATE(
                MAX('Project Bids'[Max Bid Total Amount])
            )
        ),
        REMOVEFILTERS('Vendor'),
        'Project Bids'[Is Low Bidder] = TRUE
    )
)",
        FormatString = "0.00%;(0.00%)",
        DisplayFolder = @"Cards\Any Vendor"
    },
    new MeasureDef {
        Name = "Any Vendor Win Rate",
        Description = "Win rate for the selected vendor.",
        Expression = @"DIVIDE([Any Vendor Won], [Any Vendor Bid])",
        FormatString = "0.00%;(0.00%)",
        DisplayFolder = @"Cards\Any Vendor"
    },
    new MeasureDef {
        Name = "Any Vendor Weighted Win Ratio",
        Description = "Weighted win ratio combining the selected vendor's win rate and share of total awarded contract value in the current selection.",
        Expression = @"[Any Vendor Win Rate] * [Any Vendor % of Total Awarded]",
        FormatString = "0.00%;(0.00%)",
        DisplayFolder = @"Cards\Any Vendor"
    },
    new MeasureDef {
        Name = "Any Vendor Average Bid",
        Description = "Average bid amount per project for the selected vendor.",
        Expression = @"DIVIDE([Any Vendor Total Bids], [Any Vendor Bid])",
        FormatString = "$#,0;($#,0)",
        DisplayFolder = @"Cards\Any Vendor"
    },
    new MeasureDef {
        Name = "Any Vendor Average Contract Award",
        Description = "Average contract award amount for projects won by the selected vendor.",
        Expression = @"DIVIDE([Any Vendor Total Contract Awards], [Any Vendor Won])",
        FormatString = "$#,0;($#,0)",
        DisplayFolder = @"Cards\Any Vendor"
    },
    new MeasureDef {
        Name = "Any Vendor Bid - Won - Lost",
        Description = "Text summary showing bid, won, and lost project counts for the selected vendor.",
        Expression = @"
FORMAT([Any Vendor Bid], ""#,0"")
    & "" - ""
    & FORMAT([Any Vendor Won], ""#,0"")
    & "" - ""
    & FORMAT([Any Vendor Lost], ""#,0"")",
        DisplayFolder = @"Cards\Any Vendor"
    },
    new MeasureDef {
        Name = "Times Competed Against Vendor",
        Description = "Number of projects where the focus vendor competed against the selected vendor.",
        Expression = @"
CALCULATE(
    DISTINCTCOUNT('Project Bid Role Comparison'[Project ID]),
    'Project Bid Role Comparison'[Focus Vendor Competed Flag] = TRUE,
    'Project Bid Role Comparison'[Benchmark Vendor Name]
        IN VALUES('Vendor'[Vendor Name])
)",
        FormatString = "#,0;(#,0)",
        DisplayFolder = @"Cards\Competitor"
    },
    new MeasureDef {
        Name = "Times Beat Vendor",
        Description = "Number of projects where the focus vendor beat the selected vendor.",
        Expression = @"
CALCULATE(
    DISTINCTCOUNT('Project Bid Role Comparison'[Project ID]),
    'Project Bid Role Comparison'[Focus Vendor Won Flag] = TRUE,
    'Project Bid Role Comparison'[Benchmark Vendor Name]
        IN VALUES('Vendor'[Vendor Name])
)",
        FormatString = "#,0;(#,0)",
        DisplayFolder = @"Cards\Competitor"
    },
    new MeasureDef {
        Name = "Times Lost to Vendor",
        Description = "Number of projects where the focus vendor lost to the selected vendor.",
        Expression = @"
CALCULATE(
    DISTINCTCOUNT('Project Bid Role Comparison'[Project ID]),
    'Project Bid Role Comparison'[Focus Vendor Lost Flag] = TRUE,
    'Project Bid Role Comparison'[Benchmark Vendor Name]
        IN VALUES('Vendor'[Vendor Name])
)",
        FormatString = "#,0;(#,0)",
        DisplayFolder = @"Cards\Competitor"
    },
    new MeasureDef {
        Name = "Win Rate vs Vendor",
        Description = "Win rate of the focus vendor against the selected vendor.",
        Expression = @"DIVIDE([Times Beat Vendor], [Times Competed Against Vendor])",
        FormatString = "0.00%;(0.00%)",
        DisplayFolder = @"Cards\Competitor"
    },
    new MeasureDef {
        Name = "Selected Vendor Contract Awards",
        Description = "Total contract awards for the selected vendor.",
        Expression = @"
SUMX(
    CALCULATETABLE(
        VALUES('Project Bid Role Comparison'[Project ID]),
        'Project Bid Role Comparison'[Winner Vendor Name]
            IN VALUES('Vendor'[Vendor Name])
    ),
    CALCULATE(
        MAX('Project Bid Role Comparison'[Lowest Bid Amount In Project])
    )
)",
        FormatString = "$#,0;($#,0)",
        DisplayFolder = @"Cards\Competitor"
    },
    new MeasureDef {
        Name = "Selected Vendor Won",
        Description = "Number of projects won by the selected vendor.",
        Expression = @"
CALCULATE(
    DISTINCTCOUNT('Project Bid Role Comparison'[Project ID]),
    'Project Bid Role Comparison'[Winner Vendor Name]
        IN VALUES('Vendor'[Vendor Name])
)",
        FormatString = "#,0;(#,0)",
        DisplayFolder = @"Cards\Competitor"
    },
    new MeasureDef {
        Name = "Selected Vendor Win Rate",
        Description = "Win rate for the selected vendor.",
        Expression = @"DIVIDE([Selected Vendor Won], [Any Vendor Bid])",
        FormatString = "0.00%;(0.00%)",
        DisplayFolder = @"Cards\Competitor"
    },
    new MeasureDef {
        Name = "Net Win Rate vs Vendor",
        Description = "Net head-to-head win rate of the focus vendor against the selected vendor. Positive means the focus vendor wins more often; negative means the selected vendor wins more often.",
        Expression = @"
DIVIDE(
    [Times Beat Vendor] - [Times Lost to Vendor],
    [Times Competed Against Vendor]
)",
        FormatString = "0.00%;-0.00%;0.00%",
        DisplayFolder = @"Cards\Competitor"
    },
    new MeasureDef {
        Name = "Net Wins vs Vendor",
        Description = "Net wins of the focus vendor against the selected vendor.",
        Expression = @"[Times Beat Vendor] - [Times Lost to Vendor]",
        FormatString = "#,0;(#,0)",
        DisplayFolder = @"Cards\Competitor"
    },
    new MeasureDef {
        Name = "Distinct Project Count by Specification",
        Description = "Distinct count of projects at the current bid item/specification filter context.",
        Expression = @"DISTINCTCOUNT('Project Bid Items'[Project ID])",
        FormatString = "#,0;(#,0)",
        DisplayFolder = @"Analysis\Item Comparison"
    },
    new MeasureDef {
        Name = "Focus vs Benchmark",
        Description = "Sum of focus-vs-benchmark amount at the item comparison grain.",
        Expression = @"SUM('Item Comparison'[Focus Vs Benchmark Amount])",
        FormatString = "$#,0;($#,0)",
        DisplayFolder = @"Analysis\Item Comparison"
    },
    new MeasureDef {
        Name = "ABS Focus vs Benchmark",
        Description = "Absolute summed focus-vs-benchmark amount.",
        Expression = @"SUM('Item Comparison'[ABS Focus Vs Benchmark Amount])",
        FormatString = "$#,0;($#,0)",
        DisplayFolder = @"Analysis\Item Comparison"
    },
    new MeasureDef {
        Name = "ABS % of Total Focus vs Benchmark",
        Description = "At Item Work Category level, shows share of total absolute focus-vs-benchmark amount across selected categories. At Specification level, shows share within the parent work category.",
        Expression = @"
VAR _value = [ABS Focus vs Benchmark]
RETURN
    SWITCH(
        TRUE(),

        ISINSCOPE('Item Comparison'[Specification]),
            DIVIDE(
                _value,
                CALCULATE(
                    [ABS Focus vs Benchmark],
                    ALLSELECTED('Item Comparison'[Specification])
                )
            ),

        ISINSCOPE('Item Comparison'[Item Work Category]),
            DIVIDE(
                _value,
                CALCULATE(
                    [ABS Focus vs Benchmark],
                    ALLSELECTED('Item Comparison'[Item Work Category])
                )
            ),

        DIVIDE(
            _value,
            CALCULATE(
                [ABS Focus vs Benchmark],
                ALLSELECTED('Item Comparison')
            )
        )
    )",
        FormatString = "0.00%;(0.00%)",
        DisplayFolder = @"Analysis\Item Comparison"
    },
    new MeasureDef {
        Name = "ABS % by Over Under",
        Description = "Percent contribution of over/under bidding within each work category.",
        Expression = @"
DIVIDE(
    [ABS Focus vs Benchmark],
    CALCULATE(
        [ABS Focus vs Benchmark],
        ALLEXCEPT(
            'Item Comparison',
            'Item Comparison'[Item Work Category],
            'Item Comparison'[Over Under Flag]
        )
    )
)",
        FormatString = "0.00%;(0.00%)",
        DisplayFolder = @"Analysis\Item Comparison"
    },
    new MeasureDef {
        Name = "Overbid Amount",
        Description = "Absolute amount where focus vendor overbid benchmark.",
        Expression = @"
CALCULATE(
    [ABS Focus vs Benchmark],
    'Item Comparison'[Over Under Flag] = ""Overbid""
)",
        FormatString = "$#,0;($#,0)",
        DisplayFolder = @"Analysis\Item Comparison"
    },
    new MeasureDef {
        Name = "Underbid Amount",
        Description = "Absolute amount where focus vendor underbid benchmark.",
        Expression = @"
CALCULATE(
    [ABS Focus vs Benchmark],
    'Item Comparison'[Over Under Flag] = ""Underbid""
)",
        FormatString = "$#,0;($#,0)",
        DisplayFolder = @"Analysis\Item Comparison"
    },
    new MeasureDef {
        Name = "Net Focus vs Benchmark",
        Description = "Net difference between focus and benchmark.",
        Expression = @"SUM('Item Comparison'[Focus Vs Benchmark Amount])",
        FormatString = "$#,0;($#,0)",
        DisplayFolder = @"Analysis\Item Comparison"
    },
    new MeasureDef {
        Name = "Underbid %",
        Description = "Percent of total impact where focus vendor underbid benchmark.",
        Expression = @"DIVIDE([Underbid Amount], [ABS Focus vs Benchmark])",
        FormatString = "0.00%;(0.00%)",
        DisplayFolder = @"Analysis\Item Comparison"
    },
    new MeasureDef {
        Name = "Underbid Target %",
        Description = "Target underbid percentage.",
        Expression = @"0.51",
        FormatString = "0.00%;(0.00%)",
        DisplayFolder = @"Analysis\Item Comparison"
    },
    new MeasureDef {
        Name = "Underbid Variance %",
        Description = "Difference between actual and target underbid percentage.",
        Expression = @"[Underbid %] - [Underbid Target %]",
        FormatString = "0.00%;(0.00%)",
        DisplayFolder = @"Analysis\Item Comparison"
    },
    new MeasureDef {
        Name = "Bidding Strategy",
        Description = "Status of underbid rate relative to target band.",
        Expression = @"
VAR v = [Underbid %]
RETURN
    SWITCH(
        TRUE(),
        v < 0.51, ""Too Conservative"",
        v > 0.55, ""Too Aggressive"",
        ""On Target""
    )",
        DisplayFolder = @"Analysis\Item Comparison"
    },
    new MeasureDef {
        Name = "Underbid Color",
        Description = "Color indicator for underbid gauge.",
        Expression = @"
VAR v = [Underbid %]
RETURN
    SWITCH(
        TRUE(),
        v < 0.51, ""#D9534F"",
        v > 0.55, ""#D9534F"",
        ""#5CB85C""
    )",
        DisplayFolder = @"Analysis\Item Comparison"
    },
    new MeasureDef {
        Name = "Underbid Target Mid",
        Description = "Midpoint of target range.",
        Expression = @"0.53",
        FormatString = "0.00%;(0.00%)",
        DisplayFolder = @"Analysis\Item Comparison"
    },
    new MeasureDef {
        Name = "Missed Opportunities",
        Description = "Count of project opportunities not pursued by the focus vendor.",
        Expression = @"
CALCULATE(
    DISTINCTCOUNT('Project Bid Role Comparison'[Project ID]),
    'Project Bid Role Comparison'[Is Project Opportunity Flag] = TRUE,
    'Project Bid Role Comparison'[Focus Vendor Competed Flag] = FALSE
)",
        FormatString = "#,0;(#,0)",
        DisplayFolder = @"Analysis\Opportunities"
    },
    new MeasureDef {
        Name = "Opportunity Participation Count",
        Description = "Count of opportunities where the focus vendor participated.",
        Expression = @"
CALCULATE(
    DISTINCTCOUNT('Project Bid Role Comparison'[Project ID]),
    'Project Bid Role Comparison'[Focus Vendor Participated Opportunity Flag] = TRUE
)",
        FormatString = "#,0;(#,0)",
        DisplayFolder = @"Analysis\Opportunities"
    },
    new MeasureDef {
        Name = "Opportunity Non-Participation Count",
        Description = "Count of opportunities where the focus vendor did not participate.",
        Expression = @"
CALCULATE(
    DISTINCTCOUNT('Project Bid Role Comparison'[Project ID]),
    'Project Bid Role Comparison'[Focus Vendor Missed Opportunity Flag] = TRUE
)",
        FormatString = "#,0;(#,0)",
        DisplayFolder = @"Analysis\Opportunities"
    },
    new MeasureDef {
        Name = "Opportunity Participation Rate",
        Description = "Participation rate on project opportunities.",
        Expression = @"
DIVIDE(
    [Opportunity Participation Count],
    [Missed Opportunities]
)",
        FormatString = "0.0%;(0.0%)",
        DisplayFolder = @"Analysis\Opportunities"
    },
    new MeasureDef {
        Name = "Map Status Code",
        Description = "Numeric map status code by location.",
        Expression = @"
VAR _Bid = [Bid]
VAR _Won = [Won]
RETURN
    SWITCH(
        TRUE(),
        _Bid = 0, 0,
        _Bid > 0 && _Won = 0, 1,
        _Won = 1, 2,
        _Won > 1, 3,
        BLANK()
    )",
        FormatString = "#,0;(#,0)",
        DisplayFolder = @"Map"
    },
    new MeasureDef {
        Name = "Map Status",
        Description = "Text map status by location.",
        Expression = @"
SWITCH(
    [Map Status Code],
    0, ""Never Bid"",
    1, ""Bid, Never Won"",
    2, ""Won Once"",
    3, ""Won Multiple Times"",
    BLANK()
)",
        DisplayFolder = @"Map"
    },
    new MeasureDef {
        Name = "Is Target Focus Bid Code",
        Description = "Indicator for whether the current focus bid code is in the targeted bid-code list.",
        Expression = @"
VAR _Code =
    SELECTEDVALUE ( 'Item Comparison'[Focus Bid Code] )
RETURN
    IF (
        _Code IN {
            ""678-7010"",""658-7088"",""658-7104"",""658-7091"",""677-7010"",""403-7001"",""465-7070"",""496-7007"",""416-7024"",""432-7003"",
            ""420-7057"",""658-7031"",""658-7103"",""506-7046"",""644-7031"",""618-7090"",""678-7016"",""3003-7001"",""543-7017"",""662-7092"",
            ""545-7004"",""678-7025"",""467-7325"",""677-7022"",""668-7093"",""666-7363"",""454-7005"",""506-7003"",""666-7243"",""432-7055"",
            ""425-7004"",""6013-7006"",""479-7002"",""416-7028"",""636-7003"",""734-7002"",""668-7103"",""544-7003"",""420-7044"",""730-7019"",
            ""644-7040"",""503-7001"",""618-7060"",""432-7029"",""514-7038"",""644-7028"",""662-7075"",""666-7354"",""425-7002"",""666-7242"",
            ""512-7049"",""540-7015"",""666-7356"",""544-7001"",""677-7015"",""438-7013"",""506-7011"",""506-7024"",""432-7001"",""662-7082"",
            ""425-7001"",""658-7012"",""666-7239"",""514-7004"",""545-7014"",""400-7011"",""636-7004"",""542-7002"",""104-7001"",""168-7001"",
            ""650-7041"",""104-7006"",""624-7002"",""496-7004"",""100-7002"",""672-7006"",""276-7343"",""260-7001"",""164-7007"",""467-7348"",
            ""540-7001"",""110-7001"",""668-7135"",""110-7002"",""658-7018"",""164-7015"",""500-7001"",""512-7025"",""506-7040"",""164-7065"",
            ""467-7380"",""260-7007"",""505-7003"",""666-7238"",""467-7366"",""506-7020"",""166-7001"",""624-7006"",""162-7002"",""105-7055"",
            ""104-7005"",""104-7036"",""479-7006"",""506-7041"",""512-7013"",""542-7001"",""658-7090"",""132-7006"",""432-7013"",""636-7002"",
            ""464-7007"",""420-7022"",""464-7005"",""618-7053"",""636-7005"",""496-7002"",""647-7001"",""506-7044"",""450-7024"",""467-7326"",
            ""666-7353"",""506-7039"",""678-7009"",""662-7074"",""650-7193"",""658-7032"",""543-7037"",""425-7005"",""402-7001"",""668-7091"",
            ""425-7006"",""647-7003"",""760-7002"",""662-7097"",""400-7010"",""465-7317"",""464-7010"",""514-7001"",""678-7006"",""636-7006"",
            ""650-7048"",""677-7009"",""644-7001"",""618-7052"",""292-7014"",""454-7004"",""677-7006"",""678-7002"",""678-7004"",""644-7004"",
            ""401-7001"",""502-7001"",""662-7061"",""543-7038"",""662-7064"",""738-7029"",""420-7038"",""506-7002"",""425-7043"",""677-7002"",
            ""545-7006"",""465-7332"",""677-7004"",""432-7005"",""662-7072"",""360-7006"",""275-7001"",""666-7270"",""422-7003"",""666-7266"",
            ""678-7005"",""6064-7003"",""666-7347"",""514-7053"",""677-7005"",""416-7007""
        },
        1,
        0
    )",
        FormatString = "0",
        DisplayFolder = @"Analysis\Item Comparison"
    },
    new MeasureDef {
        Name = "Min Let Date",
        Expression = @"
CALCULATE (
    MIN ( 'Project'[Project Actual Let Date] ),
    REMOVEFILTERS ()
)",
        FormatString = "yyyy-mm-dd"
    },
    new MeasureDef {
        Name = "Max Let Date",
        Expression = @"
CALCULATE (
    MAX ( 'Project'[Project Actual Let Date] ),
    REMOVEFILTERS ()
)",
        FormatString = "yyyy-mm-dd"
    },
    new MeasureDef {
        Name = "Is In Let Date Range",
        Expression = @"
VAR CurrentDate =
    MAX ( 'Calendar'[Calendar Date] )
RETURN
    IF (
        CurrentDate >= [Min Let Date]
            && CurrentDate <= [Max Let Date],
        1,
        0
    )",
        FormatString = "0"
    },
    new MeasureDef {
        Name = "Date In Active Range",
        Expression = @"
VAR MinDate =
    CALCULATE ( MIN ( 'Project'[Project Actual Let Date] ), REMOVEFILTERS() )
VAR MaxDate =
    CALCULATE ( MAX ( 'Project'[Project Actual Let Date] ), REMOVEFILTERS() )
VAR CurrentDate =
    MAX ( 'Calendar'[Calendar Date] )
RETURN
    INT ( CurrentDate >= MinDate && CurrentDate <= MaxDate )",
        FormatString = "0"
    },
    new MeasureDef {
        Name = "Eng Est",
        Description = "Total engineer's estimate across all bid items. Represents the baseline expected project cost used for comparison against submitted bids.",
        Expression = @"SUM('Item Comparison'[Estimate Extended Price])",
        FormatString = "#,0;(#,0)",
        DisplayFolder = @"Closed Project Detail"
    },
    new MeasureDef {
        Name = "1st",
        Description = "Total value of the lowest (winning) bid across all items. This represents the awarded contract amount for the project.",
        Expression = @"SUM('Item Comparison'[Winner Extended Price])",
        FormatString = "#,0;(#,0)",
        DisplayFolder = @"Closed Project Detail"
    },
    new MeasureDef {
        Name = "2nd",
        Description = "Total value of the second-lowest bid across all items. Useful for understanding bid competitiveness and spread between top bidders.",
        Expression = @"SUM('Item Comparison'[Second Extended Price])",
        FormatString = "#,0;(#,0)",
        DisplayFolder = @"Closed Project Detail"
    },
    new MeasureDef {
        Name = "1v2",
        Description = "Difference between the winning bid and the second-lowest bid. Indicates how close the competition was for the awarded project.",
        Expression = @"SUM('Item Comparison'[Winner Vs Second Amount])",
        FormatString = "#,0;(#,0)",
        DisplayFolder = @"Closed Project Detail"
    },
    new MeasureDef {
        Name = "Focus",
        Description = "Total value of the focus vendor’s bid across all items. Used to evaluate performance and competitiveness of the selected vendor.",
        Expression = @"SUM('Item Comparison'[Focus Extended Price])",
        FormatString = "#,0;(#,0)",
        DisplayFolder = @"Closed Project Detail"
    },
    new MeasureDef {
        Name = "1vFocus",
        Description = "Difference between the focus vendor’s bid and the benchmark (typically the winning bid). Shows how far the focus vendor was from winning.",
        Expression = @"SUM('Item Comparison'[Focus Vs Benchmark Amount])",
        FormatString = "#,0;(#,0)",
        DisplayFolder = @"Closed Project Detail"
    }
};

// --------------------------------------------------
// CREATE MEASURES
// --------------------------------------------------
var created = 0;
var skipped = 0;

foreach (var m in measures)
{
    AddMeasureIfMissing(m, ref created, ref skipped);
}

// --------------------------------------------------
// DONE
// --------------------------------------------------
Info(
    "Measure creation complete.\n" +
    "Created: " + created + "\n" +
    "Skipped (already existed): " + skipped
);