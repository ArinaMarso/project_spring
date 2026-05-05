using Microsoft.EntityFrameworkCore;
using StoreG5G11.api.ef;
using StoreG5G11.models.ef.entities;

namespace StoreG5G11.src.modelViews;  

public class EmployeeViewModel : AViewModel<Employee>
{
    protected override DbSet<Employee> GetEntities(ApplicationContext context)
    {
        return context.Employees;
    }
}