namespace FileStorage;

public interface IFileStorage
{
    public Task Save(string name, byte[] content);
    public Task<byte[]> Get(string name);
    public Task Delete(string name);
}