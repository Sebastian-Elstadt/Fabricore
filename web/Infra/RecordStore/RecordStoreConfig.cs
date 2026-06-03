namespace Infra.RecordStore;

public record RecordStoreConfig(
    string ConnectionString
)
{
    public void EnsureValid()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            throw new ArgumentException("RecordStoreConfig ConnectionString is required");
    }
}