namespace Nemo.WPF.Model

type ExpenseReport =
    { Name : string
      Department : string
      ExpenseLineItems : seq<Expense>}

