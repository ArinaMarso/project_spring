using Microsoft.EntityFrameworkCore;
using StoreG5G11.api.ef;
using StoreG5G11.models.ef.entities;
using StoreG5G11.src.models.ef.entities;

namespace StoreG5G11.src.modelViews;  

public class InstrumentViewModel : AViewModel<Instrument>
{
    public Array InstrumentTypes => Enum.GetValues(typeof(InstrumentType));

    protected override DbSet<Instrument> GetEntities(ApplicationContext context)
    {
        return context.Instruments;
    }
}