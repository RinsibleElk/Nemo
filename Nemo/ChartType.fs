namespace Nemo

type ChartType =
    /// Cumulative Value vs Cumulative Weight in descending order.
    | CumValues
    /// Mean Y vs Median X in ascending order.
    | PredResp
    /// Cumulative Distribution Function.
    | Cdf
    /// Probability Density Function.
    | Pdf
    /// Line.
    | Line
    /// Cumulative Y vs X.
    | CumulativeLine
