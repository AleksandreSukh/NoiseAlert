namespace NoiseAlert
{
    public class ConcuurentCircularBuffer<T> : ICircularBuffer<T>
    {

        private T[] _buffer;
        private int _last = 0;
        private int _size;
        private object _lockObject = new object();

        public ConcuurentCircularBuffer(int size)
        {
            // array index starts at 1
            this._size = size;
            _buffer = new T[size + 1];
        }

        public void Put(T item)
        {
            lock (_lockObject)
            {
                _last++;
                _last = _last > _size ? 1 : _last;
                _buffer[_last] = item;
            }
        }

        public T[] Read()
        {
            T[] arr = new T[_size];

            lock (_lockObject)
            {
                int iterator = 0;
                for (int read = 0; read < _size; read++)
                {
                    int index = _last - iterator;
                    index = index <= 0 ? (_size + index) : index;
                    if (_buffer[index] != null)
                    {
                        arr[iterator] = _buffer[index];
                    }
                    else
                    {
                        break;
                    }
                    iterator++;
                }
            }
            return arr;
        }
    }
}