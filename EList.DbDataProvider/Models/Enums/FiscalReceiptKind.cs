using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models.Enums
{
    public enum FiscalReceiptKind
    {
        [MapValue(Value = "income")]
        Income = 0,

        [MapValue(Value = "income_return")]
        IncomeReturn = 1
    }
}
