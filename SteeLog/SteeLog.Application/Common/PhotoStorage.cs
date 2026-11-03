using System.Threading.Tasks;
namespace SteeLog.Core.Application.Common
{
    public interface IPhotoStorage
    {
        Task<bool> WriteAsync(string path, byte[] bytes);
        Task<byte[]> ReadAsync(string path);
        Task<bool> DeleteAsync(string path);
    }
}
