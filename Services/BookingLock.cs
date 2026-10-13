namespace HotelApi.Services;

public interface IBookingLock
{
    Task<IDisposable> AcquireAsync();
}

// Chỉ cho một request tại một thời điểm vào đoạn "kiểm tra phòng trống rồi lưu booking"
public class BookingLock : IBookingLock
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<IDisposable> AcquireAsync()
    {
        await _semaphore.WaitAsync();          // chờ đến lượt, không chặn luồng
        return new Releaser(_semaphore);
    }

    private sealed class Releaser : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private int _released;

        public Releaser(SemaphoreSlim semaphore) => _semaphore = semaphore;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)   // chỉ nhả khóa một lần
                _semaphore.Release();
        }
    }
}