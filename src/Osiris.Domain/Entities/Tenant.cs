using Osiris.Domain.Common;

namespace Osiris.Domain.Entities;

public sealed class Tenant : BaseEntity
{
    private Tenant()
    {
    }

    public Tenant(string name)
    {
        Name = name;
    }

    public string Name { get; private set; } = string.Empty;
}
