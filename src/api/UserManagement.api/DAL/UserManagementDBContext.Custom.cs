using Microsoft.EntityFrameworkCore;
using UserManagement.DAL.Entities;

namespace UserManagement.DAL;

public partial class UserManagementDBContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {

        base.OnModelCreating(modelBuilder);
    }
}
