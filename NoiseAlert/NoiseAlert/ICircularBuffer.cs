namespace NoiseAlert
{
    public interface ICircularBuffer<T>
    {
        void Put(T item);  // put an item
        T[] Read(); // provides the last "n" requests
    }
}