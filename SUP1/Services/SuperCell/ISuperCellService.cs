using SUP.ViewModels;

namespace SUP.Services.SuperCell
{
    public interface ISuperCellService
    {
        bool TryAddSuperCell(IList<CellViewModel> cells);
        int GetRandomAvailableCell(IList<CellViewModel> cells);
        bool ShouldTriggerSuperCell();
    }
}