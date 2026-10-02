namespace Enxada.Inventory
{
    public static class HotbarMath
    {
        /// <summary>Anda delta posições na barra, dando a volta nas pontas.</summary>
        public static int Wrap(int index, int delta, int size)
        {
            if (size <= 0)
                return 0;

            return ((index + delta) % size + size) % size;
        }
    }
}
